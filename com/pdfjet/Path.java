/**
 *  Path.java
 *
©2025 PDFjet Software

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

/**
 *  Used to create path objects.
 *  The path objects may consist of lines, splines or both.
 *
 *  Please see Example_02.
 */
public class Path implements Drawable {
    private int color = Color.black;
    private float width = 0.3f;
    private String pattern = "[] 0";
    private boolean fillShape = false;
    private boolean closePath = false;
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
        points = new ArrayList<Point>();
    }

    /**
     *  Adds a point to this path.
     *
     *  @param point the point to add.
     */
    public void add(Point point) {
        points.add(point);
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
    public void setPattern(String pattern) {
        this.pattern = pattern;
    }

    /**
     *  Sets the pen width that will be used to draw the lines and splines that are part of this path.
     *
     *  @param width the pen width.
     */
    public void setWidth(double width) {
        this.width = (float) width;
    }

    /**
     *  Sets the pen width that will be used to draw the lines and splines that are part of this path.
     *
     *  @param width the pen width.
     */
    public void setWidth(float width) {
        this.width = width;
    }

    /**
     *  Sets the pen color that will be used to draw this path.
     *
     *  @param color the color is specified as an integer.
     */
    public void setColor(int color) {
        this.color = color;
    }

    /**
     *  Sets the closePath variable.
     *
     *  @param closePath if closePath is true a line will be draw between the first and last point of this path.
     */
    public void setClosePath(boolean closePath) {
        this.closePath = closePath;
    }

    /**
     *  Sets the fillShape private variable. If fillShape is true - the shape of the path will be filled with the current brush color.
     *
     *  @param fillShape the fillShape flag.
     */
    public void setFillShape(boolean fillShape) {
        this.fillShape = fillShape;
    }

    /**
     *  Sets the line cap style.
     *
     *  @param style the cap style of this path.
     *  Supported values: CapStyle.BUTT, CapStyle.ROUND and CapStyle.PROJECTING_SQUARE
     */
    public void setLineCapStyle(CapStyle style) {
        this.lineCapStyle = style;
    }

    /**
     *  Returns the line cap style for this path.
     *
     *  @return the line cap style for this path.
     */
    public CapStyle getLineCapStyle() {
        return this.lineCapStyle;
    }

    /**
     *  Sets the line join style.
     *
     *  @param style the line join style code. Supported values: JoinStyle.MITER, JoinStyle.ROUND and JoinStyle.BEVEL
     */
    public void setLineJoinStyle(JoinStyle style) {
        this.lineJoinStyle = style;
    }

    /**
     *  Returns the line join style.
     *
     *  @return the line join style.
     */
    public JoinStyle getLineJoinStyle() {
        return this.lineJoinStyle;
    }

    /**
     *  Places this path in the specified box at position (0.0, 0.0).
     *
     *  @param box the specified box.
     */
    public void placeIn(Box box) {
        placeIn(box, 0.0f, 0.0f);
    }

    /**
     *  Places the path inside the spacified box at coordinates (xOffset, yOffset) of the top left corner.
     *
     *  @param box the specified box.
     *  @param xOffset the xOffset.
     *  @param yOffset the yOffset.
     */
    public void placeIn(
            Box box,
            double xOffset,
            double yOffset) {
        placeIn(box, (float) xOffset, (float) yOffset);
    }

    /**
     *  Places the path inside the spacified box at coordinates (xOffset, yOffset) of the top left corner.
     *
     *  @param box the specified box.
     *  @param xOffset the xOffset.
     *  @param yOffset the yOffset.
     */
    public void placeIn(
            Box box,
            float xOffset,
            float yOffset) {
        xBox = box.x + xOffset;
        yBox = box.y + yOffset;
    }

    /**
     * Sets the path position.
     *
     * @param x the x coordinate.
     * @param y the y coordinate.
     */
    public void setPosition(double x, double y) {
        setLocation((float) x, (float) y);
    }

    /**
     * Sets the path position.
     *
     * @param x the x coordinate.
     * @param y the y coordinate.
     */
    public void setPosition(float x, float y) {
        setLocation(x, y);
    }

    /**
     * Sets the path location.
     *
     * @param x the x coordinate.
     * @param y the y coordinate.
     * @return the path.
     */
    public Path setLocation(double x, double y) {
        return setLocation((float) x, (float) y);
    }

    /**
     * Sets the path location.
     *
     * @param x the x coordinate.
     * @param y the y coordinate.
     * @return the path.
     */
    public Path setLocation(float x, float y) {
        xBox += x;
        yBox += y;
        return this;
    }

    /**
     *  Scales the path using the specified factor.
     *
     *  @param factor the specified factor.
     */
    public void scaleBy(double factor) {
        scaleBy((float) factor);
    }

    /**
     *  Scales the path using the specified factor.
     *
     *  @param factor the specified factor.
     */
    public void scaleBy(float factor) {
        for (int i = 0; i < points.size(); i++) {
            Point point = points.get(i);
            point.x *= factor;
            point.y *= factor;
        }
    }

    /**
     *  Draws this path on the page using the current selected color, pen width, line pattern and line join style.
     *
     *  @param page the page to draw this path on.
     *  @return x and y coordinates of the bottom right corner of this component.
     *  @throws Exception  If an input or output exception occurred
     */
    public float[] drawOn(Page page) throws Exception {
        for (int i = 0; i < points.size(); i++) {
            Point point = points.get(i);
            point.x += xBox;
            point.y += yBox;
        }

        if (fillShape) {
            page.setBrushColor(color);
            page.drawPath(points, Operation.FILL);
        } else {
            page.setPenWidth(width);
            page.setPenColor(color);
            page.setLinePattern(pattern);
            page.setLineCapStyle(lineCapStyle);
            page.setLineJoinStyle(lineJoinStyle);
            if (closePath) {
                page.drawPath(points, Operation.CLOSE);
            } else {
                page.drawPath(points, Operation.STROKE);
            }
        }

        float xMax = 0f;
        float yMax = 0f;
        for (int i = 0; i < points.size(); i++) {
            Point point = points.get(i);
            if (point.x > xMax) { xMax = point.x; }
            if (point.y > yMax) { yMax = point.y; }
            point.x -= xBox;
            point.y -= yBox;
        }

        return new float[] {xMax, yMax};
    }
}   // End of Path.java
