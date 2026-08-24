package com.pdfjet;

/**
 * DonutChart.java
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */

import java.util.ArrayList;
import java.util.Collections;
import java.util.List;

/**
 * Used to create Donut chart objects and draw them on a page.
 *
 * Please see Example_25.java
 */
public class DonutChart {
    Font f1;
    Font f2;
    float xc = 0.0f;
    float yc = 0.0f;
    float r1 = 0.0f;
    float r2 = 0.0f;
    List<Slice> slices;
    boolean isDonutChart = true;

    public DonutChart(Font f1, Font f2, boolean isDonutChart) {
        this.f1 = f1;
        this.f2 = f2;
        this.isDonutChart = isDonutChart;
        this.slices = new ArrayList<>();
    }

    public void setLocation(float xc, float yc) {
        this.xc = xc;
        this.yc = yc;
    }

    public void setR1AndR2(float r1, float r2) {
        this.r1 = r1;
        this.r2 = r2;
    }

    public void addSlice(Slice slice) {
        slices.add(slice);
    }

    private float[][] getControlPoints(
            float xc, float yc,
            float x0, float y0,
            float x3, float y3) {
        List<float[]> points = new ArrayList<>();

        float ax = x0 - xc;
        float ay = y0 - yc;
        float bx = x3 - xc;
        float by = y3 - yc;
        float q1 = ax * ax + ay * ay;
        float q2 = q1 + ax * bx + ay * by;
        float k2 = (float) (4.0 / 3.0 * (Math.sqrt(2.0 * q1 * q2) - q2) / (ax * by - ay * bx));

        // Control points coordinates
        float x1 = xc + ax - k2 * ay;
        float y1 = yc + ay + k2 * ax;
        float x2 = xc + bx + k2 * by;
        float y2 = yc + by - k2 * bx;

        points.add(new float[]{x0, y0});
        points.add(new float[]{x1, y1});
        points.add(new float[]{x2, y2});
        points.add(new float[]{x3, y3});

        return points.toArray(new float[0][]);
    }

    private float[] getPoint(float xc, float yc, float radius, float angle) {
        float x = xc + radius * ((float) Math.cos(angle * Math.PI / 180.0));
        float y = yc + radius * ((float) Math.sin(angle * Math.PI / 180.0));
        return new float[]{x, y};
    }

    private float drawSlice(
            Page page,
            int fillColor,
            float xc, float yc,
            float r1, float r2,                  // r1 > r2
            float a1, float a2) {                // a1 > a2
        page.setBrushColor(fillColor);

        float angle1 = a1 - 90.0f;
        float angle2 = a2 - 90.0f;

        List<float[]> points1 = new ArrayList<>();
        List<float[]> points2 = new ArrayList<>();
        while (true) {
            if (angle2 - angle1 <= 90.0f) {
                float[] p0 = getPoint(xc, yc, r1, angle1);   // Start point
                float[] p3 = getPoint(xc, yc, r1, angle2);   // End point
                points1.addAll(toList(getControlPoints(xc, yc, p0[0], p0[1], p3[0], p3[1])));
                p0 = getPoint(xc, yc, r2, angle1);           // Start point
                p3 = getPoint(xc, yc, r2, angle2);           // End point
                points2.addAll(toList(getControlPoints(xc, yc, p0[0], p0[1], p3[0], p3[1])));
                break;
            } else {
                float[] p0 = getPoint(xc, yc, r1, angle1);
                float[] p3 = getPoint(xc, yc, r1, angle1 + 90.0f);
                points1.addAll(toList(getControlPoints(xc, yc, p0[0], p0[1], p3[0], p3[1])));
                p0 = getPoint(xc, yc, r2, angle1);
                p3 = getPoint(xc, yc, r2, angle1 + 90.0f);
                points2.addAll(toList(getControlPoints(xc, yc, p0[0], p0[1], p3[0], p3[1])));
                angle1 += 90.0f;
            }
        }
        Collections.reverse(points2);

        page.moveTo(points1.get(0)[0], points1.get(0)[1]);
        int i = 0;
        while (i <= points1.size() - 4) {
            page.curveTo(
                    points1.get(i + 1)[0], points1.get(i + 1)[1],
                    points1.get(i + 2)[0], points1.get(i + 2)[1],
                    points1.get(i + 3)[0], points1.get(i + 3)[1]);
            i += 4;
        }
        page.lineTo(points2.get(0)[0], points2.get(0)[1]);
        i = 0;
        while (i <= points2.size() - 4) {
            page.curveTo(
                    points2.get(i + 1)[0], points2.get(i + 1)[1],
                    points2.get(i + 2)[0], points2.get(i + 2)[1],
                    points2.get(i + 3)[0], points2.get(i + 3)[1]);
            i += 4;
        }
        page.fillPath();

        return a2;
    }

    private void drawLinePointer(
            Page page,
            String text,
            float xc, float yc,
            float r1,
            float a1, float a2) throws Exception {
        float midAngle = (a1 + a2) / 2.0f - 90.0f;

        // Point on the outer edge of the donut
        float[] p1 = getPoint(xc, yc, r1, midAngle);

        // Elbow point — 15pt beyond the outer edge
        float r3 = r1 + 15.0f;
        float[] p2 = getPoint(xc, yc, r3, midAngle);

        // Draw the pointer line: edge → elbow → horizontal end
        page.setPenColor(Color.black);
        page.setPenWidth(1.0f);
        page.moveTo(p1[0], p1[1]);
        page.lineTo(p2[0], p2[1]);

        if (f1 != null && !text.isEmpty()) {
            float textWidth = f1.stringWidth(text);
            boolean onRightSide = Math.cos(midAngle * Math.PI / 180.0) >= 0;

            float padding = 4.0f;
            float lineLength = textWidth + padding;

            float xEnd = onRightSide ? p2[0] + lineLength : p2[0] - lineLength;
            float yEnd = p2[1];

            // Continue the path to the horizontal end
            page.lineTo(xEnd, yEnd);
            page.strokePath();

            // Draw the label text just above the horizontal line
            TextLine label = new TextLine(f1, text);
            label.setTextColor(Color.black);
            if (onRightSide) {
                label.setLocation(p2[0] + 2.0f, yEnd - f1.getAscent() / 3.0f);
            } else {
                label.setLocation(xEnd + 2.0f, yEnd - f1.getAscent() / 3.0f);
            }
            label.drawOn(page);
        } else {
            // No text — short horizontal stub
            boolean onRightSide = Math.cos(midAngle * Math.PI / 180.0) >= 0;
            float xEnd = onRightSide ? p2[0] + 20.0f : p2[0] - 20.0f;
            page.lineTo(xEnd, p2[1]);
            page.strokePath();
        }
    }

    public void drawOn(Page page) throws Exception {
        if (slices == null || slices.isEmpty()) {
            return;
        }
        float innerR = isDonutChart ? r2 : 0.0f;
        float angle = 0.0f;
        for (Slice slice : slices) {
            angle = drawSlice(
                    page, slice.color,
                    xc, yc,
                    r1, innerR,
                    angle, angle + slice.angle);
            drawLinePointer(
                    page, slice.text,
                    xc, yc,
                    r1,
                    angle - slice.angle, angle);
            // In drawOn, after drawSlice and drawLinePointer:
            if (f2 != null && slice.angle >= 15.0f) {
                int pct = (int) (slice.angle / 360.0f * 100.0f);
                String pctStr = pct + "%";
                TextLine label = new TextLine(f2, pctStr);
                label.setTextColor(Color.white);
                float midAngle = angle - slice.angle / 2.0f - 90.0f;
                float midR = (r1 + innerR) / 2.0f;
                float[] pos = getPoint(xc, yc, midR, midAngle);
                label.setLocation(
                        pos[0] - f2.stringWidth(pctStr) / 2.0f,
                        pos[1] + f2.getAscent() / 3.0f);
                label.drawOn(page);
            }
        }
    }

    // Utility: convert float[][] into List<float[]> for convenient addAll
    private static List<float[]> toList(float[][] arr) {
        List<float[]> list = new ArrayList<>(arr.length);
        for (float[] a : arr) {
            list.add(a);
        }
        return list;
    }
}   // End of DonutChart.java
