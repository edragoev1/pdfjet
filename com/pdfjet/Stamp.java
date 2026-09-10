/*
 * Stamp.java
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
package com.pdfjet;

import java.io.*;
import java.util.*;
import java.nio.charset.StandardCharsets;

/**
 * Content that is drawn once, written as a PDF form XObject, and placed on pages with drawOn.
 * Please see Example_35.
 */
public class Stamp implements Drawable {
    /** The object number of this stamp. */
    protected int objNumber;

    private PDF pdf;
    private float x;
    private float y;
    private float width;
    private float height;
    private float[] fillColor;
    private float[] strokeColor;
    private float strokeWidth = 1f;
    private float rotateDegrees = 0f;
    private ByteArrayOutputStream buf = new ByteArrayOutputStream();
    private List<Font> fonts = new ArrayList<Font>();

    /**
     * Creates a stamp for the specified document.
     *
     * @param pdf the PDF document.
     */
    public Stamp(PDF pdf) {
        this.pdf = pdf;
    }

    /**
     * Sets the size of this stamp.
     *
     * @param width the width.
     * @param height the height.
     * @return this Stamp object.
     */
    public Stamp withSize(float width, float height) {
        this.width = width;
        this.height = height;
        return this;
    }

    /**
     * Adds a font used by the text on this stamp.
     *
     * @param font the font.
     * @return this Stamp object.
     */
    public Stamp withFont(Font font) {
        fonts.add(font);
        return this;
    }

    public Stamp setLocation(float x, float y) {
        this.x = x;
        this.y = y;
        return this;
    }

    /**
     * Sets the location of the top left corner of this stamp on the page.
     *
     * @param x the x coordinate.
     * @param y the y coordinate.
     * @return this Stamp object.
     */
    public Stamp setLocation(double x, double y) {
        return setLocation((float) x, (float) y);
    }

    private void append(float value) {
        byte[] bytes = FastFloat.toByteArray(value);
        buf.write(bytes, 0, bytes.length);
    }

    private void append(String str) {
        byte[] bytes = str.getBytes(StandardCharsets.UTF_8);
        buf.write(bytes, 0, bytes.length);
    }

    /**
     * Sets the fill color for the content drawn after it.
     *
     * @param rgbColor the red, green and blue components, from 0.0 to 1.0.
     * @return this Stamp object.
     */
    public Stamp setFillColor(float[] rgbColor) {
        append(rgbColor[0]);
        append(" ");
        append(rgbColor[1]);
        append(" ");
        append(rgbColor[2]);
        append(" rg\n");
        this.fillColor = rgbColor;
        return this;
    }

    /**
     * Sets the fill color for the content drawn after it.
     *
     * @param color the color as a 0xRRGGBB value, for example Color.blue.
     * @return this Stamp object.
     */
    public Stamp setFillColor(int color) {
        float r = ((color >> 16) & 0xff)/255f;
        float g = ((color >>  8) & 0xff)/255f;
        float b = ((color)       & 0xff)/255f;
        append(r);
        append(" ");
        append(g);
        append(" ");
        append(b);
        append(" rg\n");
        this.fillColor = new float[] {r, g, b};
        return this;
    }

    /**
     * Sets the stroke color for the content drawn after it.
     *
     * @param rgbColor the red, green and blue components, from 0.0 to 1.0.
     * @return this Stamp object.
     */
    public Stamp setStrokeColor(float[] rgbColor) {
        append(rgbColor[0]);
        append(" ");
        append(rgbColor[1]);
        append(" ");
        append(rgbColor[2]);
        append(" RG\n");
        this.strokeColor = rgbColor;
        return this;
    }

    /**
     * Sets the stroke color for the content drawn after it.
     *
     * @param color the color as a 0xRRGGBB value, for example Color.blue.
     * @return this Stamp object.
     */
    public Stamp setStrokeColor(int color) {
        float r = ((color >> 16) & 0xff)/255f;
        float g = ((color >>  8) & 0xff)/255f;
        float b = ((color)       & 0xff)/255f;
        append(r);
        append(" ");
        append(g);
        append(" ");
        append(b);
        append(" RG\n");
        this.fillColor = new float[] {r, g, b};
        return this;
    }

    /**
     * Sets the stroke width for the content drawn after it.
     *
     * @param width the stroke width.
     * @return this Stamp object.
     */
    public Stamp setStrokeWidth(float width) {
        append(width);
        append(" w\n");
        this.strokeWidth = width;
        return this;
    }

    /**
     * Begins a new path at the specified point.
     *
     * @param x the x coordinate.
     * @param y the y coordinate.
     * @return this Stamp object.
     */
    public Stamp moveTo(float x, float y) {
        append(x);
        append(" ");
        append(height - y);
        append(" m\n");
        return this;
    }

    /**
     * Adds a straight line from the current point to the specified point.
     *
     * @param x the x coordinate.
     * @param y the y coordinate.
     * @return this Stamp object.
     */
    public Stamp lineTo(float x, float y) {
        append(x);
        append(" ");
        append(height - y);
        append(" l\n");
        return this;
    }

    /**
     * Adds a cubic Bézier curve from the current point.
     *
     * @param x1 the x coordinate of the first control point.
     * @param y1 the y coordinate of the first control point.
     * @param x2 the x coordinate of the second control point.
     * @param y2 the y coordinate of the second control point.
     * @param x3 the x coordinate of the end point.
     * @param y3 the y coordinate of the end point.
     * @return this Stamp object.
     */
    public Stamp curveTo(
            float x1,
            float y1,
            float x2,
            float y2,
            float x3,
            float y3) {
        append(x1);
        append(" ");
        append(height - y1);
        append(" ");
        append(x2);
        append(" ");
        append(height - y2);
        append(" ");
        append(x3);
        append(" ");
        append(height - y3);
        append(" c\n");
        return this;
    }

    /**
     * Strokes the current path.
     *
     * @return this Stamp object.
     */
    public Stamp strokePath() {
        append("S\n");
        return this;
    }

    /**
     * Closes and strokes the current path.
     *
     * @return this Stamp object.
     */
    public Stamp closePath() {
        append("s\n");
        return this;
    }

    /**
     * Fills the current path.
     *
     * @return this Stamp object.
     */
    public Stamp fillPath() {
        append("f\n");
        return this;
    }

    /**
     * Closes, fills and strokes the current path.
     *
     * @return this Stamp object.
     */
    public Stamp closeFillAndStrokePath() {
        append("b\n");
        return this;
    }

    /**
     * Not implemented yet; does nothing.
     *
     * @return this Stamp object.
     */
    public Stamp rectangle() {
        return this;
    }

    /**
     * Not implemented yet; does nothing.
     *
     * @return this Stamp object.
     */
    public Stamp draw() {
        return this;
    }

    /**
     * Draws the outline of a rectangle.
     *
     * @param x the x coordinate of the top left corner.
     * @param y the y coordinate of the top left corner.
     * @param w the width.
     * @param h the height.
     * @return this Stamp object.
     */
    public Stamp drawRect(float x, float y, float w, float h) {
        moveTo(x, y);
        lineTo(x + w, y);
        lineTo(x + w, y + h);
        lineTo(x, y + h);
        closePath();
        return this;
    }

    /**
     * Draws a filled rectangle.
     *
     * @param x the x coordinate of the top left corner.
     * @param y the y coordinate of the top left corner.
     * @param w the width.
     * @param h the height.
     * @return this Stamp object.
     */
    public Stamp fillRect(float x, float y, float w, float h) {
        moveTo(x, y);
        lineTo(x + w, y);
        lineTo(x + w, y + h);
        lineTo(x, y + h);
        fillPath();
        return this;
    }

    /**
     * Draws text using the font, font size, location and text in the parameters.
     *
     * @param parameters the text parameters.
     * @return this Stamp object.
     */
    public Stamp drawText(TextParameters parameters) {
        return drawText(parameters.font, parameters.fontSize, parameters.x, parameters.y, parameters.text);
    }

    /**
     * Draws text on this stamp.
     *
     * @param font the font. Add it with withFont too.
     * @param fontSize the font size.
     * @param x the x coordinate.
     * @param y the y coordinate.
     * @param text the text.
     * @return this Stamp object.
     */
    public Stamp drawText(Font font, float fontSize, float x, float y, String text) {
        append("BT\n");
        append("/F");
        append(font.objNumber);
        append(" ");
        append(fontSize);
        append(" Tf\n");
        append(x);
        append(" ");
        append(height - y);
        append(" Td\n");
        append("<");
        drawText(font, text);
        append("> Tj\n");
        append("ET\n");
        return this;
    }

    /**
     * Sets the rotation angle of this stamp.
     *
     * @param degrees the rotation angle in degrees.
     * @return this Stamp object.
     */
    public Stamp rotate(double degrees) {
        this.rotateDegrees = (float)degrees;
        return this;
    }

    /**
     * Sets the rotation angle of this stamp.
     *
     * @param degrees the rotation angle in degrees.
     * @return this Stamp object.
     */
    public Stamp setRotation(double degrees) {
        this.rotateDegrees = (float)degrees;
        return this;
    }

    /**
     * Sets a clockwise rotation.
     *
     * @param degrees the rotation angle in degrees, clockwise.
     * @return this Stamp object.
     */
    public Stamp setRotationClockwise(double degrees) {
        this.rotateDegrees = (float)-degrees;
        return this;
    }

    /**
     * Sets a counterclockwise rotation.
     *
     * @param degrees the rotation angle in degrees, counterclockwise.
     * @return this Stamp object.
     */
    public Stamp setRotationCounterClockwise(double degrees) {
        this.rotateDegrees = (float)degrees;
        return this;
    }

    /**
     * Writes this stamp to the document as a form XObject.
     * Call it once, after drawing the content and before drawOn.
     *
     * @throws Exception if an input or output exception occurred.
     */
    public void complete() throws Exception {
        pdf.newobj();
        pdf.append(Token.BEGIN_DICTIONARY);
        pdf.append("/Type /XObject\n");
        pdf.append("/Subtype /Form\n");

        pdf.append("/BBox [0 0 ");
        pdf.append(FastFloat.toByteArray(width));
        pdf.append(' ');
        pdf.append(FastFloat.toByteArray(height));
        pdf.append("]\n");

        pdf.append("/Resources <<\n");
        if (fonts.size() > 0) {
            pdf.append("/Font <<\n");
            for (Font font : fonts) {
                pdf.append("/F");
                pdf.append(font.objNumber);
                pdf.append(" ");
                pdf.append(font.objNumber);
                pdf.append(" 0 R\n");
            }
            pdf.append(">>\n");
        }
        pdf.append(">>\n");
        pdf.append("/Length ");
        pdf.append(buf.size());
        pdf.append(Token.NEWLINE);
        pdf.append(Token.END_DICTIONARY);   // End of XObject dictionary
        pdf.append(Token.STREAM);
        pdf.append(buf.toByteArray());
        pdf.append(Token.END_STREAM);
        pdf.endobj();
        pdf.stamps.add(this);
        objNumber = pdf.getObjNumber();
    }

    /**
     * Draws the supplied text using the given {@link Font}.
     *
     * @param font the font that supplies glyph information
     * @param str  the text to render
     */
    private void drawText(Font font, String str) {
        final int length = str.length();            // number of UTF‑16 code units
        int i = 0;
        while (i < length) {
            int codePoint = str.codePointAt(i);     // full Unicode scalar value
            i += Character.charCount(codePoint);    // advance 1 or 2 char positions

            if (codePoint == 0xFEFF) { continue; }  // Skip the BOM

            int gid = (codePoint < font.firstChar || codePoint > font.lastChar)
                ? font.unicodeToGID[0x0020]         // Use space fallback
                : font.unicodeToGID[codePoint];
            appendCodePointAsHex(gid);
        }
    }

    private void append(Point point) {
        append(point.x);
        append(" ");
        append(height - point.y);
        append(" ");
    }

    /**
     * Draws a path through the points.
     *
     * @param path the points. Control points define Bézier curves.
     * @param pathOperator the path operator, for example PathOperator.STROKE.
     * @throws Exception if the path has fewer than 2 points.
     */
    public void drawPath(List<Point> path, String pathOperator) throws Exception {
        if (path.size() < 2) {
            throw new Exception("The Path object must contain at least 2 points");
        }
        Point point = path.get(0);
        moveTo(point.x, point.y);
        char controlPoint = '\0';
        for (int i = 1; i < path.size(); i++) {
            point = path.get(i);
            if (point.controlPoint != '\0') {
                controlPoint = point.controlPoint;
                append(point);
            } else {
                if (controlPoint != '\0') {
                    append(point);
                    append(controlPoint);
                    append('\n');
                    controlPoint = '\0';
                } else {
                    lineTo(point.x, point.y);
                }
            }
        }
        append(pathOperator);
        append('\n');
    }

    private void appendCodePointAsHex(int codePoint) {
        buf.write(Page.HEX[(codePoint >> 12) & 0xF]);
        buf.write(Page.HEX[(codePoint >> 8)  & 0xF]);
        buf.write(Page.HEX[(codePoint >> 4)  & 0xF]);
        buf.write(Page.HEX[codePoint & 0xF]);
    }

    public float[] drawOn(Page page) {
        // page.addBMC(StructElem.Figure, language, actualText, altDescription);
        page.saveGraphicsState();

        float drawX = this.x;
        float drawY = (page.height - this.height) - this.y;

        // 5. POSITION: move to desired location on page
        page.append("1 0 0 1 ");
        page.append(drawX);
        page.append(' ');
        page.append(drawY);
        page.append(" cm\n");

        // 4. MOVE BACK: after rotation
        page.append("1 0 0 1 ");
        page.append(width/2);
        page.append(' ');
        page.append(height/2);
        page.append(" cm\n");

        // 3. ROTATE: rotate around origin
        double radians = rotateDegrees * (Math.PI / 180);
        float cos = (float)Math.cos(radians);
        float sin = (float)Math.sin(radians);
        page.append(FastFloat.toByteArray(cos));
        page.append(' ');
        page.append(FastFloat.toByteArray(sin));
        page.append(' ');
        page.append(FastFloat.toByteArray(-sin));
        page.append(' ');
        page.append(FastFloat.toByteArray(cos));
        page.append(" 0 0 cm\n");

        // 2. MOVE: move the center of the object to origin
        page.append("1 0 0 1 ");
        page.append(-width/2);
        page.append(' ');
        page.append(-height/2);
        page.append(" cm\n");

        // 1. DRAW: draw the object
        page.append("/Fm");
        page.append(objNumber);
        page.append(" Do\n");

        page.restoreGraphicsState();
        // page.addEMC();

        return new float[] { this.x + width, this.y + height };
    }
}   // End of Stamp.java

