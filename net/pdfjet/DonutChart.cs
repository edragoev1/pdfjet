/**
 *  DonutChart.cs
 *
Copyright 2024 Innovatics Inc.

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
using System;
using System.Collections.Generic;

namespace PDFjet.NET {
public class DonutChart {
    Font f1;
    Font f2;
    float xc;
    float yc;
    float r1;
    float r2;
    List<Slice> slices;
    bool isDonutChart = true;
    
    public DonutChart(Font f1, Font f2, bool isDonutChart) {
        this.f1 = f1;
        this.f2 = f2;
        this.isDonutChart = isDonutChart;
        this.slices = new List<Slice>();
    }

    public void SetLocation(float xc, float yc) {
        this.xc = xc;
        this.yc = yc;
    }

    public void SetR1AndR2(float r1, float r2) {
        this.r1 = r1;
        this.r2 = r2;
    }

    public void AddSlice(Slice slice) {
        this.slices.Add(slice);
    }

    private List<float[]> GetControlPoints(
            float xc, float yc,
            float x0, float y0,
            float x3, float y3) {
        List<float[]> points = new List<float[]>();

        float ax = x0 - xc;
        float ay = y0 - yc;
        float bx = x3 - xc;
        float by = y3 - yc;
        float q1 = ax*ax + ay*ay;
        float q2 = q1 + ax*bx + ay*by;
        float k2 = 4f/3f * (((float) Math.Sqrt(2f*q1*q2)) - q2) / (ax*by - ay*bx);

        // Control points coordinates
        float x1 = xc + ax - k2*ay;
        float y1 = yc + ay + k2*ax;
        float x2 = xc + bx + k2*by;
        float y2 = yc + by - k2*bx;

        points.Add(new float[] {x0, y0});
        points.Add(new float[] {x1, y1});
        points.Add(new float[] {x2, y2});
        points.Add(new float[] {x3, y3});

        return points;
    }

    private float[] GetPoint(float xc, float yc, float radius, float angle) {
        float x = xc + radius*((float) Math.Cos(angle*Math.PI/180.0));
        float y = yc + radius*((float) Math.Sin(angle*Math.PI/180.0));
        return new float[] {x, y};
    }

    private float DrawSlice(
            Page page,
            int fillColor,
            float xc, float yc,
            float r1, float r2,     // r1 > r2
            float a1, float a2) {   // a1 > a2
        page.SetBrushColor(fillColor);

        float angle1 = a1 - 90f;
        float angle2 = a2 - 90f;

        List<float[]> points1 = new List<float[]>();
        List<float[]> points2 = new List<float[]>();
        while (true) {
            if ((angle2 - angle1) <= 90f) {
                float[] p0 = GetPoint(xc, yc, r1, angle1);          // Start point
                float[] p3 = GetPoint(xc, yc, r1, angle2);          // End point
                points1.AddRange(GetControlPoints(xc, yc, p0[0], p0[1], p3[0], p3[1]));
                p0 = GetPoint(xc, yc, r2, angle1);                  // Start point
                p3 = GetPoint(xc, yc, r2, angle2);                  // End point
                points2.AddRange(GetControlPoints(xc, yc, p0[0], p0[1], p3[0], p3[1]));
                break;
            } else {
                float[] p0 = GetPoint(xc, yc, r1, angle1);
                float[] p3 = GetPoint(xc, yc, r1, angle1 + 90f);
                points1.AddRange(GetControlPoints(xc, yc, p0[0], p0[1], p3[0], p3[1]));
                p0 = GetPoint(xc, yc, r2, angle1);
                p3 = GetPoint(xc, yc, r2, angle1 + 90f);
                points2.AddRange(GetControlPoints(xc, yc, p0[0], p0[1], p3[0], p3[1]));
                angle1 += 90f;
            }
        }
        points2.Reverse();

        page.MoveTo(points1[0][0], points1[0][1]);
        for (int i = 0; i <= (points1.Count - 4); i += 4) {
            page.CurveTo(
                    points1[i + 1][0], points1[i + 1][1],
                    points1[i + 2][0], points1[i + 2][1],
                    points1[i + 3][0], points1[i + 3][1]);
        }
        page.LineTo(points2[0][0], points2[0][1]);
        for (int i = 0; i <= (points2.Count - 4); i += 4) {
            page.CurveTo(
                    points2[i + 1][0], points2[i + 1][1],
                    points2[i + 2][0], points2[i + 2][1],
                    points2[i + 3][0], points2[i + 3][1]);
        }
        page.FillPath();

        return a2;
    }

    private void DrawLinePointer(
            Page page,
            int perColor,
            float xc, float yc,
            float r1, float r2,     // r1 > r2
            float a1, float a2) {   // a1 > a2
        page.SetPenColor(Color.black);
        float angle1 = a1 - 90f;
        float angle2 = a2 - 90f;
        if ((angle2 - angle1) <= 90f) {
            page.DrawLine(xc, yc, 500f, 500f);
        }
    }

    public void DrawOn(Page page) {
        float angle = 0f;
        foreach (Slice slice in slices) {
            angle = DrawSlice(
                    page, slice.color,
                    xc, yc,
                    r1, r2,
                    angle, angle + slice.angle);
/*
            DrawLinePointer(
                    page, slice.color,
                    xc, yc,
                    r1, r2,
                    angle, angle + slice.angle);
*/
            }
        }
    }
}
