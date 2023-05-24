/**
 *  Path.cs
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
using System;
using System.Collections.Generic;

/**
 *  Used to create path objects.
 *  The path objects may consist of lines, splines or both.
 *
 *  Please see Example_02.
 */
namespace PDFjet.NET {
public class Path : IDrawable {
    private int color = Color.black;
    private float width = 0.3f;
    private String pattern = "[] 0";
    private bool fillShape = false;
    private bool closePath = false;
    private List<Point> points = null;
    private float xBox;
    private float yBox;
    private CapStyle lineCapStyle = CapStyle.BUTT;
    private JoinStyle lineJoinStyle = JoinStyle.MITER;

    /**
     *  The default constructor.
     *
     *
     */
    public Path() {
        points = new List<Point>();
    }

    /**
     *  Adds a point to this path.
     *
     *  @param point the point to add.
     */
    public void Add(Point point) {
        points.Add(point);
    }

    /**
     *  Sets the line dash pattern for this path.
     *
     *  The line dash pattern controls the pattern of dashes and gaps used to stroke paths.
     *  It is specified by a dash array and a dash phase.
     *  The elements of the dash array are positive numbers that specify the lengths of
     *  alternating dashes and gaps.
     *  The dash phase specifies the distance into the dash pattern at which to start the dash.
     *  The elements of both the dash array and the dash phase are expressed in user space units.
     *  <pre>
     *  Examples of line dash patterns:
     *
     *      "[Array] Phase"     Appearance          Description
     *      _______________     _________________   ____________________________________
     *
     *      "[] 0"              -----------------   Solid line
     *      "[3] 0"             ---   ---   ---     3 units on, 3 units off, ...
     *      "[2] 1"             -  --  --  --  --   1 on, 2 off, 2 on, 2 off, ...
     *      "[2 1] 0"           -- -- -- -- -- --   2 on, 1 off, 2 on, 1 off, ...
     *      "[3 5] 6"             ---     ---       2 off, 3 on, 5 off, 3 on, 5 off, ...
     *      "[2 3] 11"          -   --   --   --    1 on, 3 off, 2 on, 3 off, 2 on, ...
     *  </pre>
     *
     *  @param pattern the line dash pattern.
     */
    public void SetPattern(String pattern) {
        this.pattern = pattern;
    }

    /**
     *  Sets the pen width that will be used to draw the lines and splines that are part of this path.
     *
     *  @param width the pen width.
     */
    public void SetWidth(double width) {
        this.width = (float) width;
    }

    /**
     *  Sets the pen width that will be used to draw the lines and splines that are part of this path.
     *
     *  @param width the pen width.
     */
    public void SetWidth(float width) {
        this.width = width;
    }

    /**
     *  Sets the pen color that will be used to draw this path.
     *
     *  @param color the color is specified as an integer.
     */
    public void SetColor(int color) {
        this.color = color;
    }

    /**
     *  Sets the closePath variable.
     *
     *  @param closePath if closePath is true a line will be draw between the first and last point of this path.
     */
    public void SetClosePath(bool closePath) {
        this.closePath = closePath;
    }

    /**
     *  Sets the fillShape private variable. If fillShape is true - the shape of the path will be filled with the current brush color.
     *
     *  @param fillShape the fillShape flag.
     */
    public void SetFillShape(bool fillShape) {
        this.fillShape = fillShape;
    }

    /**
     *  Sets the line cap style.
     *
     *  @param style the cap style of this path.
     *  Supported values: CapStyle.BUTT, CapStyle.ROUND and CapStyle.PROJECTING_SQUARE
     */
    public void SetLineCapStyle(CapStyle style) {
        this.lineCapStyle = style;
    }

    /**
     *  Returns the line cap style for this path.
     *
     *  @return the line cap style for this path.
     */
    public CapStyle GetLineCapStyle() {
        return this.lineCapStyle;
    }

    /**
     *  Sets the line join style.
     *
     *  @param style the line join style code. Supported values: JoinStyle.MITER, JoinStyle.ROUND and JoinStyle.BEVEL
     */
    public void SetLineJoinStyle(JoinStyle style) {
        this.lineJoinStyle = style;
    }

    /**
     *  Returns the line join style.
     *
     *  @return the line join style.
     */
    public JoinStyle GetLineJoinStyle() {
        return this.lineJoinStyle;
    }

    /**
     *  Places this path in the specified box at position (0.0, 0.0).
     *
     *  @param box the specified box.
     */
    public void PlaceIn(Box box) {
        PlaceIn(box, 0.0f, 0.0f);
    }

    /**
     *  Places the path inside the spacified box at coordinates (xOffset, yOffset) of the top left corner.
     *
     *  @param box the specified box.
     *  @param xOffset the xOffset.
     *  @param yOffset the yOffset.
     */
    public void PlaceIn(
            Box box,
            double xOffset,
            double yOffset) {
        PlaceIn(box, (float) xOffset, (float) yOffset);
    }

    /**
     *  Places the path inside the spacified box at coordinates (xOffset, yOffset) of the top left corner.
     *
     *  @param box the specified box.
     *  @param xOffset the xOffset.
     *  @param yOffset the yOffset.
     */
    public void PlaceIn(
            Box box,
            float xOffset,
            float yOffset) {
        xBox = box.x + xOffset;
        yBox = box.y + yOffset;
    }

    public void SetPosition(double x, double y) {
        SetLocation((float) x, (float) y);
    }

    public void SetPosition(float x, float y) {
        SetLocation(x, y);
    }

    public Path SetLocation(double x, double y) {
        return SetLocation((float) x, (float) y);
    }

    public Path SetLocation(float x, float y) {
        xBox += x;
        yBox += y;
        return this;
    }

    /**
     *  Scales the path using the specified factor.
     *
     *  @param factor the specified factor.
     */
    public void ScaleBy(double factor) {
        ScaleBy((float) factor);
    }

    /**
     *  Scales the path using the specified factor.
     *
     *  @param factor the specified factor.
     */
    public void ScaleBy(float factor) {
        for (int i = 0; i < points.Count; i++) {
            Point point = points[i];
            point.x *= factor;
            point.y *= factor;
        }
    }

    /**
     * Returns a list containing the start point, first control point, second control point and the end point of elliptical curve segment.
     * Please see Example_18.
     *
     * @param x the x coordinate of the center of the ellipse.
     * @param y the y coordinate of the center of the ellipse.
     * @param r1 the horizontal radius of the ellipse.
     * @param r2 the vertical radius of the ellipse.
     * @param segment the segment to draw - please see the Segment class.
     * @return
     * @throws Exception
     */
    public static List<Point> GetCurvePoints(
            float x,
            float y,
            float r1,
            float r2,
            int segment) {
        // The best 4-spline magic number
        float m4 = 0.551784f;
        List<Point> list = new List<Point>();

        if (segment == 0) {
            list.Add(new Point(x, y - r2));
            list.Add(new Point(x + m4*r1, y - r2, Point.CONTROL_POINT));
            list.Add(new Point(x + r1, y - m4*r2, Point.CONTROL_POINT));
            list.Add(new Point(x + r1, y));
        } else if (segment == 1) {
            list.Add(new Point(x + r1, y));
            list.Add(new Point(x + r1, y + m4*r2, Point.CONTROL_POINT));
            list.Add(new Point(x + m4*r1, y + r2, Point.CONTROL_POINT));
            list.Add(new Point(x, y + r2));
        } else if (segment == 2) {
            list.Add(new Point(x, y + r2));
            list.Add(new Point(x - m4*r1, y + r2, Point.CONTROL_POINT));
            list.Add(new Point(x - r1, y + m4*r2, Point.CONTROL_POINT));
            list.Add(new Point(x - r1, y));
        } else if (segment == 3) {
            list.Add(new Point(x - r1, y));
            list.Add(new Point(x - r1, y - m4*r2, Point.CONTROL_POINT));
            list.Add(new Point(x - m4*r1, y - r2, Point.CONTROL_POINT));
            list.Add(new Point(x, y - r2));
        }

        return list;
    }

    /**
     *  Draws this path on the page using the current selected color, pen width, line pattern and line join style.
     *
     *  @param page the page to draw on.
     *  @return x and y coordinates of the bottom right corner of this component.
     *  @throws Exception
     */
    public float[] DrawOn(Page page) {
        if (fillShape) {
            page.SetBrushColor(color);
        } else {
            page.SetPenColor(color);
        }
        page.SetPenWidth(width);
        page.SetLinePattern(pattern);
        page.SetLineCapStyle(lineCapStyle);
        page.SetLineJoinStyle(lineJoinStyle);

        for (int i = 0; i < points.Count; i++) {
            Point point = points[i];
            point.x += xBox;
            point.y += yBox;
        }

        if (fillShape) {
            page.DrawPath(points, 'f');
        } else {
            if (closePath) {
                page.DrawPath(points, 's');
            } else {
                page.DrawPath(points, 'S');
            }
        }

        float xMax = 0f;
        float yMax = 0f;
        for (int i = 0; i < points.Count; i++) {
            Point point = points[i];
            if (point.x > xMax) { xMax = point.x; }
            if (point.y > yMax) { yMax = point.y; }
            point.x -= xBox;
            point.y -= yBox;
        }

        return new float[] {xMax, yMax};
    }
}   // End of Path.cs
}   // End of namespace PDFjet.NET
