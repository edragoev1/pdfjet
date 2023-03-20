/**
 *  DonutChart.java
 *
Copyright 2023 Innovatics Inc.

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.
*/
package com.pdfjet;

import java.util.*;


public class DonutChart {

	Font f1;
    Font f2;
	Float xc;
    Float yc;
    Float r1;
    Float r2;
	List<Float> angles;
	List<Integer> colors;
    boolean isDonutChart = true;
    
    public DonutChart(Font f1, Font f2, boolean isDonutChart) {
	    this.f1 = f1;
	    this.f2 = f2;
	    this.isDonutChart = isDonutChart;
        this.angles = new ArrayList<Float>();
        this.colors = new ArrayList<Integer>();
    }

    public void setLocation(Float xc, Float yc) {
        this.xc = xc;
        this.yc = yc;
    }

    public void setR1AndR2(Float r1, Float r2) {
        this.r1 = r1;
        this.r2 = r2;
        if (this.r1 < 1.0) {
            this.isDonutChart = false;
        }
    }
    
    private List<Point> getCurvePoints(Float xc, Float yc, Float r, Float angle1, Float angle2) {
        angle1 *= -1f;
        angle2 *= -1f;

        // Start point coordinates
        Float x1 = xc + r*((float) Math.cos(angle1*Math.PI/180.0));
        Float y1 = yc + r*((float) Math.sin(angle1*Math.PI/180.0));
        // End point coordinates
        Float x4 = xc + r*((float) Math.cos(angle2*Math.PI/180.0));
        Float y4 = yc + r*((float) Math.sin(angle2*Math.PI/180.0));
    
        Float ax = x1 - xc;
        Float ay = y1 - yc;
        Float bx = x4 - xc;
        Float by = y4 - yc;
        Float q1 = ax*ax + ay*ay;
        Float q2 = q1 + ax*bx + ay*by;
    
        Float k2 = 4f/3f * (((float) Math.sqrt(2f*q1*q2)) - q2) / (ax*by - ay*bx);
    
        Float x2 = xc + ax - k2*ay;
        Float y2 = yc + ay + k2*ax;
        Float x3 = xc + bx + k2*by;
        Float y3 = yc + by - k2*bx;
    
        List<Point> list = new ArrayList<Point>();
        list.add(new Point(x1, y1));
        list.add(new Point(x2, y2, Point.CONTROL_POINT));
        list.add(new Point(x3, y3, Point.CONTROL_POINT));
        list.add(new Point(x4, y4));
    
        return list;
    }

    private Float[] getControlPoints(
            Float xc, Float yc,
            Float x0, Float y0,
            Float x3, Float y3) {
        Float ax = x0 - xc;
        Float ay = y0 - yc;
        Float bx = x3 - xc;
        Float by = y3 - yc;
        Float q1 = ax*ax + ay*ay;
        Float q2 = q1 + ax*bx + ay*by;
        Float k2 = 4f/3f * (((float) Math.sqrt(2f*q1*q2)) - q2) / (ax*by - ay*bx);

        // Control points coordinates
        Float x1 = xc + ax - k2*ay;
        Float y1 = yc + ay + k2*ax;
        Float x2 = xc + bx + k2*by;
        Float y2 = yc + by - k2*bx;

        return new Float[] {x1, y1, x2, y2};
    }

    public void drawSlice(
            Page page,
            int fillColor,
            Float xc, Float yc,
            Float r1, Float r2,     // r1 > r2
            Float a1, Float a2) {
        page.setBrushColor(fillColor);
        Float angle1 = a1 - 90f;
        Float angle2 = a2 - 90f;

        // Start point coordinates
        Float x0 = xc + r1*((float) Math.cos(angle1*Math.PI/180.0));
        Float y0 = yc + r1*((float) Math.sin(angle1*Math.PI/180.0));
        // End point coordinates
        Float x3 = xc + r1*((float) Math.cos(angle2*Math.PI/180.0));
        Float y3 = yc + r1*((float) Math.sin(angle2*Math.PI/180.0));

        Float[] control1 = getControlPoints(xc, yc, x0, y0, x3, y3);
        // Float x1 = control[0];
        // Float y1 = control[1];
        // Float x2 = control[2];
        // Float y2 = control[3];

        // Start point coordinates
        Float x4 = xc + r2*((float) Math.cos(angle1*Math.PI/180.0));
        Float y4 = yc + r2*((float) Math.sin(angle1*Math.PI/180.0));
        // End point coordinates
        Float x7 = xc + r2*((float) Math.cos(angle2*Math.PI/180.0));
        Float y7 = yc + r2*((float) Math.sin(angle2*Math.PI/180.0));

        Float[] control2 = getControlPoints(xc, yc, x4, y4, x7, y7);
        // Float x5 = control[0];
        // Float y5 = control[1];
        // Float x6 = control[2];
        // Float y6 = control[3];

        page.moveTo(x0, y0);
        // page.curveTo(x1, y1, x2, y2, x3, y3);
        page.curveTo(control1[0], control1[1], control1[2], control1[3], x3, y3);
        page.lineTo(x7, y7);
        // page.curveTo(x6, y6, x5, y5, x4, y4);
        page.curveTo(control2[2], control2[3], control2[0], control2[1], x4, y4);
        page.fillPath();
    }

    // GetArcPoints calculates a list of points for a given arc of a circle
    // @param xc the x-coordinate of the circle's centre.
    // @param yc the y-coordinate of the circle's centre
    // @param r the radius of the circle.
    // @param angle1 the start angle of the arc in degrees.
    // @param angle2 the end angle of the arc in degrees.
    // @param includeOrigin whether the origin should be included in the list (thus creating a pie shape).
    private List<Point> getArcPoints(
            Float xc, Float yc, Float r, Float angle1, Float angle2, boolean includeOrigin) {
        List<Point> list = new ArrayList<Point>();

        if (includeOrigin) {
            list.add(new Point(xc, yc));
        }

        float startAngle;
        float endAngle;
        if (angle1 <= angle2) {
            startAngle = angle1;
            endAngle = angle1 + 90;
            while (endAngle < angle2) {
                list.addAll(getCurvePoints(xc, yc, r, startAngle, endAngle));
                startAngle += 90;
                endAngle += 90;
            }
            endAngle -= 90;
            list.addAll(getCurvePoints(xc, yc, r, endAngle, angle2));
        }
        else {
            startAngle = angle1;
            endAngle = angle1 - 90;
            while (endAngle > angle2) {
                list.addAll(getCurvePoints(xc, yc, r, startAngle, endAngle));
                startAngle -= 90;
                endAngle -= 90;
            }
            endAngle += 90;
            list.addAll(getCurvePoints(xc, yc, r, endAngle, angle2));
        }

        return list;
    }

    // GetDonutPoints calculates a list of points for a given donut sector of a circle.
    // @param xc the x-coordinate of the circle's centre.
    // @param yc the y-coordinate of the circle's centre.
    // @param r1 the inner radius of the donut.
    // @param r2 the outer radius of the donut.
    // @param angle1 the start angle of the donut sector in degrees.
    // @param angle2 the end angle of the donut sector in degrees.
    private List<Point> getDonutPoints(Float xc, Float yc, Float r1, Float r2, Float angle1, Float angle2) {
        List<Point> list = new ArrayList<Point>();
        list.addAll(getArcPoints(xc, yc, r1, angle1, angle2, false));
        list.addAll(getArcPoints(xc, yc, r2, angle2, angle1, false));
        return list;
    }

    // AddSector -- TODO:
    public void addSector(Float angle, int color) {
        this.angles.add(angle);
        this.colors.add(color);
    }

    // Draws donut chart on the specified page.
    public void drawOn(Page page) throws Exception {
        drawSlice(page, Color.blue, 300f, 300f, 200f, 100f, 0f, 90f);
    }
}
