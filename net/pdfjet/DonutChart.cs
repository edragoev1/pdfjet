using System;
using System.Collections.Generic;

namespace PDFjet.NET {
public class DonutChart {

	Font f1;
    Font f2;
	List<List<Point>> chartData;
	float xc;
    float yc;
    float r1;
    float r2;
	List<float> angles;
	List<Int32> colors;
    bool isDonutChart = true;
    

    public DonutChart(Font f1, Font f2, bool isDonutChart) {
	    this.f1 = f1;
	    this.f2 = f2;
	    this.isDonutChart = true;
    }

    public void SetLocation(float xc, float yc) {
        this.xc = xc;
        this.yc = yc;
    }

    public void SetR1AndR2(float r1, float r2) {
        this.r1 = r1;
        this.r2 = r2;
        if (this.r1 < 1.0) {
            this.isDonutChart = false;
        }
    }
    
    private List<Point> GetBezierCurvePoints(float xc, float yc, float r, float angle1, float angle2) {
        angle1 *= -1.0f;
        angle2 *= -1.0f;

        // Start point coordinates
        float x1 = xc + r*((float) (Math.Cos(angle1)*(Math.PI/180.0)));
        float y1 = yc + r*((float) (Math.Sin(angle1)*(Math.PI/180.0)));
        // End point coordinates
        float x4 = xc + r*((float) (Math.Cos(angle2)*(Math.PI/180.0)));
        float y4 = yc + r*((float) (Math.Sin(angle2)*(Math.PI/180.0)));
    
        float ax = x1 - xc;
        float ay = y1 - yc;
        float bx = x4 - xc;
        float by = y4 - yc;
        float q1 = ax*ax + ay*ay;
        float q2 = q1 + ax*bx + ay*by;
    
        float k2 = 4f/3f * (((float) Math.Sqrt(2f*q1*q2)) - q2) / (ax*by - ay*bx);
    
        float x2 = xc + ax - k2*ay;
        float y2 = yc + ay + k2*ax;
        float x3 = xc + bx + k2*by;
        float y3 = yc + by - k2*bx;
    
        List<Point> list = new List<Point>();
        list.Add(new Point(x1, y1));
        list.Add(new Point(x2, y2, Point.CONTROL_POINT));
        list.Add(new Point(x3, y3, Point.CONTROL_POINT));
        list.Add(new Point(x4, y4));
    
        return list;
    }

    // GetArcPoints calculates a list of points for a given arc of a circle
    // @param xc the x-coordinate of the circle's centre.
    // @param yc the y-coordinate of the circle's centre
    // @param r the radius of the circle.
    // @param angle1 the start angle of the arc in degrees.
    // @param angle2 the end angle of the arc in degrees.
    // @param includeOrigin whether the origin should be included in the list (thus creating a pie shape).
    public List<Point> GetArcPoints(float xc, float yc, float r, float angle1, float angle2, bool includeOrigin) {
        List<Point> list = new List<Point>();

        if (includeOrigin) {
            list.Add(new Point(xc, yc));
        }

        float startAngle;
        float endAngle;
        if (angle1 <= angle2) {
            startAngle = angle1;
            endAngle = angle1 + 90;
            while (endAngle < angle2) {
                list.AddRange(GetBezierCurvePoints(xc, yc, r, startAngle, endAngle));
                startAngle += 90;
                endAngle += 90;
            }
            endAngle -= 90;
            list.AddRange(GetBezierCurvePoints(xc, yc, r, endAngle, angle2));
        }
        else {
            startAngle = angle1;
            endAngle = angle1 - 90;
            while (endAngle > angle2) {
                list.AddRange(GetBezierCurvePoints(xc, yc, r, startAngle, endAngle));
                startAngle -= 90;
                endAngle -= 90;
            }
            endAngle += 90;
            list.AddRange(GetBezierCurvePoints(xc, yc, r, endAngle, angle2));
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
    private List<Point> GetDonutPoints(float xc, float yc, float r1, float r2, float angle1, float angle2) {
        List<Point> list = new List<Point>();
        list.AddRange(GetArcPoints(xc, yc, r1, angle1, angle2, false));
        list.AddRange(GetArcPoints(xc, yc, r2, angle2, angle1, false));
        return list;
    }

    // AddSector -- TODO:
    public void AddSector(float angle, int color) {
        this.angles.Add(angle);
        this.colors.Add(color);
    }

    // DrawOn draws donut chart on the specified page.
    public void DrawOn(Page page) {
        float startAngle = 0f;
        float endAngle = 0f;
        int lastColorIndex = 0;
        for (int i = 0; i < angles.Count; i++) {
            endAngle = startAngle + angles[i];
            List<Point> list = new List<Point>();
            if (isDonutChart) {
                list.AddRange(GetDonutPoints(xc, yc, r1, r2, startAngle, endAngle));
            }
            else {
                list.AddRange(GetArcPoints(xc, yc, r2, startAngle, endAngle, true));
            }
            // foreach (Point point in list) {
            // 	point.drawOn(page);
            // }
            page.SetBrushColor(colors[i]);
            page.DrawPath(list, Operation.FILL);
            startAngle = endAngle;
            lastColorIndex = i;
        }

        if (endAngle < 360f) {
            endAngle = 360f;
            List<Point> list = new List<Point>();
            if (isDonutChart) {
                list.AddRange(GetDonutPoints(xc, yc, r1, r2, startAngle, endAngle));
            }
            else {
                list.AddRange(GetArcPoints(xc, yc, r2, startAngle, endAngle, true));
            }
            // foreach (Point point in list) {
            // 	point.drawOn(page);
            // }
            page.SetBrushColor(colors[lastColorIndex + 1]);
            page.DrawPath(list, Operation.FILL);
        }
    }

}   // End of class DonutChart.cs
}   // End of namespace PDFjet.NET
