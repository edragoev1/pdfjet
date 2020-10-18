/**
 *  Point.cs
 *
Copyright 2020 Innovatics Inc.

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


namespace PDFjet.NET {
/**
 *  Used to create point objects with different shapes and draw them on a page.
 *  Please note: When we are mentioning (x, y) coordinates of a point - we are talking about the coordinates of the center of the point.
 *
 *  Please see Example_05.
 */
public class Point : IDrawable {

    public static readonly int INVISIBLE = -1;
    public static readonly int CIRCLE = 0;
    public static readonly int DIAMOND = 1;
    public static readonly int BOX = 2;
    public static readonly int PLUS = 3;
    public static readonly int H_DASH = 4;
    public static readonly int V_DASH = 5;
    public static readonly int MULTIPLY = 6;
    public static readonly int STAR = 7;
    public static readonly int X_MARK = 8;
    public static readonly int UP_ARROW = 9;
    public static readonly int DOWN_ARROW = 10;
    public static readonly int LEFT_ARROW = 11;
    public static readonly int RIGHT_ARROW = 12;

    public static bool CONTROL_POINT = true;

    internal float x;
    internal float y;
    internal float r = 2f;
    internal int shape = Point.CIRCLE;
    internal int color = Color.black;
    internal int align = Align.RIGHT;
    internal float lineWidth = 0.3f;
    internal String linePattern = "[] 0";
    internal bool fillShape = false;
    internal bool isControlPoint = false;
    internal bool drawPath = false;

    private String text;
    private int textColor;
    private int textDirection;
    private String uri;
    private float xBox;
    private float yBox;


    /**
     *  The default constructor.
     *
     */
    public Point() {
    }


    /**
     *  Constructor for creating point objects.
     *
     *  @param x the x coordinate of this point when drawn on the page.
     *  @param y the y coordinate of this point when drawn on the page.
     */
    public Point(double x, double y) : this((float) x, (float) y) {
    }


    /**
     *  Constructor for creating point objects.
     *
     *  @param x the x coordinate of this point when drawn on the page.
     *  @param y the y coordinate of this point when drawn on the page.
     */
    public Point(float x, float y) {
        this.x = x;
        this.y = y;
    }


    /**
     *  Constructor for creating point objects.
     *
     *  @param x the x coordinate of this point when drawn on the page.
     *  @param y the y coordinate of this point when drawn on the page.
     *  @param isControlPoint true if this point is one of the points specifying a curve.
     */
    public Point(double x, double y, bool isControlPoint) : this((float) x, (float) y, isControlPoint) {
    }


    /**
     *  Constructor for creating point objects.
     *
     *  @param x the x coordinate of this point when drawn on the page.
     *  @param y the y coordinate of this point when drawn on the page.
     *  @param isControlPoint true if this point is one of the points specifying a curve.
     */
    public Point(float x, float y, bool isControlPoint) {
        this.x = x;
        this.y = y;
        this.isControlPoint = isControlPoint;
    }


    /**
     *  Sets the position (x, y) of this point.
     *
     *  @param x the x coordinate of this point when drawn on the page.
     *  @param y the y coordinate of this point when drawn on the page.
     */
    public void SetPosition(double x, double y) {
        SetPosition((float) x, (float) y);
    }


    /**
     *  Sets the position (x, y) of this point.
     *
     *  @param x the x coordinate of this point when drawn on the page.
     *  @param y the y coordinate of this point when drawn on the page.
     */
    public void SetPosition(float x, float y) {
        SetLocation(x, y);
    }


    public void SetXY(float x, float y) {
        SetLocation(x, y);
    }


    /**
     *  Sets the location (x, y) of this point.
     *
     *  @param x the x coordinate of this point when drawn on the page.
     *  @param y the y coordinate of this point when drawn on the page.
     */
    public void SetLocation(float x, float y) {
        this.x = x;
        this.y = y;
    }


    /**
     *  Sets the x coordinate of this point.
     *
     *  @param x the x coordinate of this point when drawn on the page.
     */
    public void SetX(double x) {
        this.x = (float) x;
    }


    /**
     *  Sets the x coordinate of this point.
     *
     *  @param x the x coordinate of this point when drawn on the page.
     */
    public void SetX(float x) {
        this.x = x;
    }


    /**
     *  Returns the x coordinate of this point.
     *
     *  @return the x coordinate of this point.
     */
    public float GetX() {
        return x;
    }


    /**
     *  Sets the y coordinate of this point.
     *
     *  @param y the y coordinate of this point when drawn on the page.
     */
    public void SetY(double y) {
        this.y = (float) y;
    }


    /**
     *  Sets the y coordinate of this point.
     *
     *  @param y the y coordinate of this point when drawn on the page.
     */
    public void SetY(float y) {
        this.y = y;
    }


    /**
     *  Returns the y coordinate of this point.
     *
     *  @return the y coordinate of this point.
     */
    public float GetY() {
        return y;
    }


    /**
     *  Sets the radius of this point.
     *
     *  @param r the radius.
     */
    public void SetRadius(double r) {
        this.r = (float) r;
    }


    /**
     *  Sets the radius of this point.
     *
     *  @param r the radius.
     */
    public void SetRadius(float r) {
        this.r = r;
    }


    /**
     *  Returns the radius of this point.
     *
     *  @return the radius of this point.
     */
    public float GetRadius() {
        return r;
    }


    /**
     *  Sets the shape of this point.
     *
     *  @param shape the shape of this point. Supported values:
     *  <pre>
     *  Point.INVISIBLE
     *  Point.CIRCLE
     *  Point.DIAMOND
     *  Point.BOX
     *  Point.PLUS
     *  Point.H_DASH
     *  Point.V_DASH
     *  Point.MULTIPLY
     *  Point.STAR
     *  Point.X_MARK
     *  Point.UP_ARROW
     *  Point.DOWN_ARROW
     *  Point.LEFT_ARROW
     *  Point.RIGHT_ARROW
     *  </pre>
     */
    public void SetShape(int shape) {
        this.shape = shape;
    }


    /**
     *  Returns the point shape code value.
     *
     *  @return the shape code value.
     */
    public int GetShape() {
        return shape;
    }


    /**
     *  Sets the private fillShape variable.
     *
     *  @param fillShape if true - fill the point with the specified brush color.
     */
    public void SetFillShape(bool fillShape) {
        this.fillShape = fillShape;
    }


    /**
     *  Returns the value of the fillShape private variable.
     *
     *  @return the value of the private fillShape variable.
     */
    public bool GetFillShape() {
        return fillShape;
    }


    /**
     *  Sets the pen color for this point.
     *
     *  @param color the color specified as an integer.
     *  @return the point.
     */
    public Point SetColor(int color) {
        this.color = color;
        return this;
    }


    /**
     *  Returns the point color as an integer.
     *
     *  @return the color.
     */
    public int GetColor() {
        return color;
    }


    /**
     *  Sets the width of the lines of this point.
     *
     *  @param lineWidth the line width.
     */
    public void SetLineWidth(double lineWidth) {
        this.lineWidth = (float) lineWidth;
    }


    /**
     *  Sets the width of the lines of this point.
     *
     *  @param lineWidth the line width.
     */
    public void SetLineWidth(float lineWidth) {
        this.lineWidth = lineWidth;
    }


    /**
     *  Returns the width of the lines used to draw this point.
     *
     *  @return the width of the lines used to draw this point.
     */
    public float GetLineWidth() {
        return lineWidth;
    }


    /**
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
     *  @param linePattern the line dash pattern.
     */
    public void SetLinePattern(String linePattern) {
        this.linePattern = linePattern;
    }


    /**
     *  Returns the line dash pattern.
     *
     *  @return the line dash pattern.
     */
    public String GetLinePattern() {
        return linePattern;
    }


    /**
     *  Sets this point as the start of a path that will be drawn on the chart.
     *
     *  @return the point.
     */
    public Point SetDrawPath() {
        this.drawPath = true;
        return this;
    }


    /**
     *  Sets the URI for the "click point" action.
     *
     *  @param uri the URI
     */
    public void SetURIAction(String uri) {
        this.uri = uri;
    }


    /**
     *  Returns the URI for the "click point" action.
     *
     *  @return the URI for the "click point" action.
     */
    public String GetURIAction() {
        return uri;
    }


    /**
     *  Sets the point text.
     *
     *  @param text the text.
     */
    public void SetText(String text) {
        this.text = text;
    }


    /**
     *  Returns the text associated with this point.
     *
     *  @return the text.
     */
    public String GetText() {
        return text;
    }


    /**
     *  Sets the point's text color.
     *
     *  @param textColor the text color.
     */
    public void SetTextColor(int textColor) {
        this.textColor = textColor;
    }


    /**
     *  Returns the point's text color.
     *
     *  @return the text color.
     */
    public int GetTextColor() {
        return this.textColor;
    }


    /**
     *  Sets the point's text direction.
     *
     *  @param textDirection the text direction.
     */
    public void SetTextDirection(int textDirection) {
        this.textDirection = textDirection;
    }


    /**
     *  Returns the point's text direction.
     *
     *  @return the text direction.
     */
    public int GetTextDirection() {
        return this.textDirection;
    }


    /**
     *  Sets the point alignment.
     *
     *  @param align the alignment value.
     */
    public void SetAlignment(int align) {
        this.align = align;
    }


    /**
     *  Returns the point alignment.
     *
     *  @return align the alignment value.
     */
    public int GetAlignment() {
        return this.align;
    }


    /**
     *  Places this point in the specified box at position (0f, 0f).
     *
     *  @param box the specified box.
     */
    public void PlaceIn(Box box) {
        PlaceIn(box, 0f, 0f);
    }


    /**
     *  Places this point in the specified box.
     *
     *  @param box the specified box.
     *  @param xOffset the x offset from the top left corner of the box.
     *  @param yOffset the y offset from the top left corner of the box.
     */
    public void PlaceIn(
            Box box,
            double xOffset,
            double yOffset) {
        PlaceIn(box, (float) xOffset, (float) yOffset);
    }


    /**
     *  Places this point in the specified box.
     *
     *  @param box the specified box.
     *  @param xOffset the x offset from the top left corner of the box.
     *  @param yOffset the y offset from the top left corner of the box.
     */
    public void PlaceIn(
            Box box,
            float xOffset,
            float yOffset) {
        xBox = box.x + xOffset;
        yBox = box.y + yOffset;
    }


    /**
     *  Draws this point on the specified page.
     *
     *  @param page the page to draw on.
     *  @return x and y coordinates of the bottom right corner of this component.
     *  @throws Exception
     */
    public float[] DrawOn(Page page) {
        page.SetPenWidth(lineWidth);
        page.SetLinePattern(linePattern);

        if (fillShape) {
            page.SetBrushColor(color);
        }
        else {
            page.SetPenColor(color);
        }

        x += xBox;
        y += yBox;
        page.DrawPoint(this);
        x -= xBox;
        y -= yBox;

        return new float[] {x + xBox +  r, y + yBox + r};
    }

}   // End of Point.cs
}   // End of namespace PDFjet.NET
