/**
 *  Box.cs
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

namespace PDFjet.NET {
/**
 *  Used to create rectangular boxes on a page.
 *  Also used to for layout purposes. See the PlaceIn method in the Image and TextLine classes.
 *
 */
public class Box : IDrawable {
    internal float x;
    internal float y;

    private float w;
    private float h;

    private int color = Color.black;
    private float width = 0f;
    private String pattern = "[] 0";
    private bool fillShape = false;

    private String language = null;
    private String actualText = Single.space;
    private String altDescription = Single.space;

    internal String uri = null;
    internal String key = null;

    /**
     *  The default constructor.
     *
     */
    public Box() {
    }

    /**
     *  Creates a box object.
     *
     *  @param x the x coordinate of the top left corner of this box when drawn on the page.
     *  @param y the y coordinate of the top left corner of this box when drawn on the page.
     *  @param w the width of this box.
     *  @param h the height of this box.
     */
    public Box(double x, double y, double w, double h) : this((float) x, (float) y, (float) w, (float) h) {
    }

    /**
     *  Creates a box object.
     *
     *  @param x the x coordinate of the top left corner of this box when drawn on the page.
     *  @param y the y coordinate of the top left corner of this box when drawn on the page.
     *  @param w the width of this box.
     *  @param h the height of this box.
     */
    public Box(float x, float y, float w, float h) {
        this.x = x;
        this.y = y;
        this.w = w;
        this.h = h;
    }

    /**
     *  Sets the position of this box on the page.
     *
     *  @param x the x coordinate of the top left corner of this box when drawn on the page.
     *  @param y the y coordinate of the top left corner of this box when drawn on the page.
     */
    public void SetPosition(double x, double y) {
        SetPosition((float) x, (float) y);
    }

    /**
     *  Sets the position of this box on the page.
     *
     *  @param x the x coordinate of the top left corner of this box when drawn on the page.
     *  @param y the y coordinate of the top left corner of this box when drawn on the page.
     */
    public void SetPosition(float x, float y) {
        SetLocation(x, y);
    }

    public void SetXY(float x, float y) {
        SetLocation(x, y);
    }

    /**
     *  Sets the location of this box on the page.
     *
     *  @param x the x coordinate of the top left corner of this box when drawn on the page.
     *  @param y the y coordinate of the top left corner of this box when drawn on the page.
     */
    public void SetLocation(float x, float y) {
        this.x = x;
        this.y = y;
    }

    /**
     *  Sets the size of this box.
     *
     *  @param w the width of this box.
     *  @param h the height of this box.
     */
    public void SetSize(double w, double h) {
        SetSize((float) w, (float) h);
    }

    /**
     *  Sets the size of this box.
     *
     *  @param w the width of this box.
     *  @param h the height of this box.
     */
    public void SetSize(float w, float h) {
        this.w = w;
        this.h = h;
    }

    /**
     *  Sets the color for this box.
     *
     *  @param color the color specified as an integer.
     */
    public void SetColor(int color) {
        this.color = color;
    }

    /**
     *  Sets the width of this line.
     *
     *  @param width the width.
     */
    public void SetLineWidth(double width) {
        this.width = (float) width;
    }

    /**
     *  Sets the width of this line.
     *
     *  @param width the width.
     */
    public void SetLineWidth(float width) {
        this.width = width;
    }

    /**
     *  Sets the URI for the "click box" action.
     *
     *  @param uri the URI
     */
    public void SetURIAction(String uri) {
        this.uri = uri;
    }

    /**
     *  Sets the destination key for the action.
     *
     *  @param key the destination name.
     */
    public void SetGoToAction(String key) {
        this.key = key;
    }

    /**
     *  Sets the alternate description of this box.
     *
     *  @param altDescription the alternate description of the box.
     *  @return this Box.
     */
    public Box SetAltDescription(String altDescription) {
        this.altDescription = altDescription;
        return this;
    }

    /**
     *  Sets the actual text for this box.
     *
     *  @param actualText the actual text for the box.
     *  @return this Box.
     */
    public Box SetActualText(String actualText) {
        this.actualText = actualText;
        return this;
    }

    /**
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
     *  Sets the private fillShape variable.
     *  If the value of fillShape is true - the box is filled with the current brush color.
     *
     *  @param fillShape the value used to set the private fillShape variable.
     */
    public void SetFillShape(bool fillShape) {
        this.fillShape = fillShape;
    }

    /**
     *  Places this box in the another box.
     *
     *  @param box the other box.
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
     *  Places this box in the another box.
     *
     *  @param box the other box.
     *  @param xOffset the x offset from the top left corner of the box.
     *  @param yOffset the y offset from the top left corner of the box.
     */
    public void PlaceIn(
            Box box,
            float xOffset,
            float yOffset) {
        this.x = box.x + xOffset;
        this.y = box.y + yOffset;
    }

    /**
     *  Scales this box by the spacified factor.
     *
     *  @param factor the factor used to scale the box.
     */
    public void ScaleBy(double factor) {
        ScaleBy((float) factor);
    }

    /**
     *  Scales this box by the spacified factor.
     *
     *  @param factor the factor used to scale the box.
     */
    public void ScaleBy(float factor) {
        this.x *= factor;
        this.y *= factor;
    }

    /**
     *  Draws this box on the specified page.
     *
     *  @param page the page to draw on.
     *  @return x and y coordinates of the bottom right corner of this component.
     *  @throws Exception
     */
    public float[] DrawOn(Page page) {
        page.AddBMC(StructElem.P, language, actualText, altDescription);
        page.SetPenWidth(width);
        page.SetLinePattern(pattern);
        if (fillShape) {
            page.SetBrushColor(color);
        } else {
            page.SetPenColor(color);
        }
        page.MoveTo(x, y);
        page.LineTo(x + w, y);
        page.LineTo(x + w, y + h);
        page.LineTo(x, y + h);
        if (fillShape) {
            page.FillPath();
        } else {
            page.ClosePath();
        }
        page.AddEMC();

        if (uri != null || key != null) {
            page.AddAnnotation(new Annotation(
                    uri,
                    key,    // The destination name
                    x,
                    y,
                    x + w,
                    y + h,
                    language,
                    actualText,
                    altDescription));
        }

        return new float[] {x + w, y + h + width};
    }
}   // End of Box.cs
}   // End of namespace PDFjet.NET
