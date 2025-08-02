package com.pdfjet;

/**
 * Rect.java
 *
 * ©2025 PDFJet Software
 *
 * Permission is hereby granted, free of charge, to any person obtaining a copy
 * of this software and associated documentation files (the "Software"), to deal
 * in the Software without restriction, including without limitation the rights
 * to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
 * copies of the Software, and to permit persons to whom the Software is
 * furnished to do so, subject to the following conditions:
 *
 * The above copyright notice and this permission notice shall be included in all
 * copies or substantial portions of the Software.
 *
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
 * IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
 * FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
 * AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
 * LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
 * OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
 * SOFTWARE.
 */
import java.util.*;

public class Rect {
    private float x;
    private float y;
    private float w;
    private float h;
    private float r;
    private int color;
    private float width;
    private String pattern;
    private boolean fillShape;
    private String uri;
    private String key;
    private String language;
    private String altDescription;
    private String actualText;
    private String structureType;

    /**
     * Creates new Rect object.
     */
    public Rect() {
        this.color = Color.black;
        this.width = 0.0f;
        this.pattern = "[] 0";
        this.altDescription = Single.space;
        this.actualText = Single.space;
        this.structureType = "P"; // StructureType.P; TODO
    }

    /**
     * Creates a rect object.
     * @param x the x coordinate of the top left corner of this rect when drawn on the page.
     * @param y the y coordinate of the top left corner of this rect when drawn on the page.
     * @param w the width of this rect.
     * @param h the height of this rect.
     */
    public Rect(float x, float y, float w, float h) {
        this();
        this.x = x;
        this.y = y;
        this.w = w;
        this.h = h;
    }

    /**
     * Sets the location of this rect on the page.
     * @param x the x coordinate of the top left corner of this rect when drawn on the page.
     * @param y the y coordinate of the top left corner of this rect when drawn on the page.
     * @return this Rect.
     */
    public Rect setLocation(float x, float y) {
        this.x = x;
        this.y = y;
        return this;
    }

    /**
     * Sets the size of this rect.
     * @param w the width of this rect.
     * @param h the height of this rect.
     */
    public void setSize(float w, float h) {
        this.w = w;
        this.h = h;
    }

    /**
     * Sets the color for this rect.
     * @param color the color specified as an integer.
     */
    public void setBorderColor(int color) {
        this.color = color;
    }

    /**
     * Sets the width of this line.
     * @param width the width.
     */
    public void setLineWidth(float width) {
        this.width = width;
    }

    /**
     * Sets the corner radius.
     * @param r the radius.
     */
    public void setCornerRadius(float r) {
        this.r = r;
    }

    /**
     * Sets the URI for the "click rect" action.
     * @param uri the URI
     */
    public void setURIAction(String uri) {
        this.uri = uri;
    }

    /**
     * Sets the destination key for the action.
     * @param key the destination name.
     */
    public void setGoToAction(String key) {
        this.key = key;
    }

    /**
     * Sets the alternate description of this rect.
     * @param altDescription the alternate description of the rect.
     * @return this Rect.
     */
    public Rect setAltDescription(String altDescription) {
        this.altDescription = altDescription;
        return this;
    }

    /**
     * Sets the actual text for this rect.
     * @param actualText the actual text for the rect.
     * @return this Rect.
     */
    public Rect setActualText(String actualText) {
        this.actualText = actualText;
        return this;
    }

    /**
     * Sets the type of the structure.
     * @param structureType the structure type.
     * @return this Rect.
     */
    public Rect setStructureType(String structureType) {
        this.structureType = structureType;
        return this;
    }

    /**
     * Sets the line dash pattern that controls the pattern of dashes and gaps used to stroke paths.
     * @param pattern the line dash pattern.
     */
    public void setPattern(String pattern) {
        this.pattern = pattern;
    }

    /**
     * Sets the private fillShape variable.
     * If the value of fillShape is true - the rect is filled with the current brush color.
     * @param fillShape the value used to set the private fillShape variable.
     */
    public void setFillShape(boolean fillShape) {
        this.fillShape = fillShape;
    }

    /**
     * Places this rect in the another rect.
     * @param rect the other rect.
     * @param xOffset the x offset from the top left corner of the rect.
     * @param yOffset the y offset from the top left corner of the rect.
     */
    public void placeIn(Rect rect, float xOffset, float yOffset) {
        this.x = rect.x + xOffset;
        this.y = rect.y + yOffset;
    }

    /**
     * Scales this rect by the specified factor.
     * @param factor the factor used to scale the rect.
     */
    public void scaleBy(float factor) {
        this.x *= factor;
        this.y *= factor;
    }

    /**
     * Draws this rect on the specified page.
     * @param page the page to draw this rect on.
     * @return x and y coordinates of the bottom right corner of this component.
     */
    public float[] drawOn(Page page) throws Exception {
        final float k = 0.5517f;

        page.addBMC(this.structureType, this.language, this.actualText, this.altDescription);
        if (this.r == 0.0f) {
            page.moveTo(this.x, this.y);
            page.lineTo(this.x + this.w, this.y);
            page.lineTo(this.x + this.w, this.y + this.h);
            page.lineTo(this.x, this.y + this.h);
            if (this.fillShape) {
                page.setBrushColor(this.color);
                page.fillPath();
            } else {
                page.setPenWidth(this.width);
                page.setPenColor(this.color);
                page.setLinePattern(this.pattern);
                page.closePath();
            }
        } else {
            page.setPenWidth(this.width);
            page.setPenColor(this.color);
            page.setLinePattern(this.pattern);

            List<Point> points = new ArrayList<>();
            points.add(new Point((this.x + this.r), this.y, false));
            points.add(new Point((this.x + this.w) - this.r, this.y, false));
            points.add(new Point((this.x + this.w - this.r) + this.r * k, this.y, true));
            points.add(new Point((this.x + this.w), (this.y + this.r) - this.r * k, true));
            points.add(new Point((this.x + this.w), (this.y + this.r), false));
            points.add(new Point((this.x + this.w), (this.y + this.h) - this.r, false));
            points.add(new Point((this.x + this.w), ((this.y + this.h) - this.r) + this.r * k, true));
            points.add(new Point(((this.x + this.w) - this.r) + this.r * k, (this.y + this.h), true));
            points.add(new Point(((this.x + this.w) - this.r), (this.y + this.h), false));
            points.add(new Point((this.x + this.r), (this.y + this.h), false));
            points.add(new Point(((this.x + this.r) - this.r * k), (this.y + this.h), true));
            points.add(new Point(this.x, ((this.y + this.h) - this.r) + this.r * k, true));
            points.add(new Point(this.x, (this.y + this.h) - this.r, false));
            points.add(new Point(this.x, (this.y + this.r), false));
            points.add(new Point(this.x, (this.y + this.r) - this.r * k, true));
            points.add(new Point((this.x + this.r) - this.r * k, this.y, true));
            points.add(new Point((this.x + this.r), this.y, false));

            page.drawPath(points, Operation.STROKE);
        }
        page.addEMC();

        if (this.uri != null || this.key != null) {
            page.addAnnotation(new Annotation(
                    this.uri,
                    this.key, // The destination name
                    this.x,
                    this.y,
                    this.x + this.w,
                    this.y + this.h,
                    this.language,
                    this.actualText,
                    this.altDescription));
        }

        return new float[] { this.x + this.w, this.y + this.h };
    }
}
