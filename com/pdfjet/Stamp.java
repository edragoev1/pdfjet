/**
 * Stamp.java
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
package com.pdfjet;

import java.io.*;
import java.util.*;
import java.nio.charset.StandardCharsets;

public class Stamp implements Drawable {
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

    public Stamp(PDF pdf) {
        this.pdf = pdf;
    }

    public Stamp withSize(float width, float height) {
        this.width = width;
        this.height = height;
        return this;
    }

    public Stamp withFont(Font font) {
        fonts.add(font);
        return this;
    }

    public void setPosition(float x, float y) {
        this.x = x;
        this.y = y;
    }

    public Stamp setLocation(float x, float y) {
        this.x = x;
        this.y = y;
        return this;
    }

    public Stamp setLocation(double x, double y) {
        this.x = (float)x;
        this.y = (float)y;
        return this;
    }

    private void append(float value) {
        byte[] bytes = FastFloat.toByteArray(value);
        buf.write(bytes, 0, bytes.length);
    }

    private void append(String str) {
        byte[] bytes = str.getBytes(StandardCharsets.UTF_8);
        buf.write(bytes, 0, bytes.length);
    }

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

    public Stamp setStrokeWidth(float width) {
        append(width);
        append(" w\n");
        this.strokeWidth = width;
        return this;
    }

    public Stamp moveTo(float x, float y) {
        append(x);
        append(" ");
        append(height - y);
        append(" m\n");
        return this;
    }

    public Stamp lineTo(float x, float y) {
        append(x);
        append(" ");
        append(height - y);
        append(" l\n");
        return this;
    }

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

    public Stamp strokePath() {
        append("S\n");
        return this;
    }

    public Stamp closePath() {
        append("s\n");
        return this;
    }

    public Stamp fillPath() {
        append("f\n");
        return this;
    }

    // TODO:
    public Stamp rectangle() {
        return this;
    }

    public Stamp draw() {
        return this;
    }

    public Stamp drawRect(float x, float y, float w, float h) {
        moveTo(x, y);
        lineTo(x + w, y);
        lineTo(x + w, y + h);
        lineTo(x, y + h);
        closePath();
        return this;
    }

    public Stamp fillRect(float x, float y, float w, float h) {
        moveTo(x, y);
        lineTo(x + w, y);
        lineTo(x + w, y + h);
        lineTo(x, y + h);
        fillPath();
        return this;
    }

//     public Stamp drawText(TextParameters parameters) {
//         return drawText(parameters.font, parameters.fontSize, parameters.x, parameters.y, parameters.text);
//     }

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

    /// <summary>
    /// Sets the rotation angle.
    /// </summary>
    /// <param name="degrees">The rotation angle in degrees.</param>
    public Stamp rotate(double degrees) {
        this.rotateDegrees = (float)degrees;
        return this;
    }

    /// <summary>
    /// Sets the rotation angle.
    /// </summary>
    /// <param name="degrees">The rotation angle in degrees.</param>
    public Stamp setRotation(double degrees) {
        this.rotateDegrees = (float)degrees;
        return this;
    }

    /// <summary>
    /// Sets clockwise rotation.
    /// </summary>
    /// <param name="degrees">The rotation angle in degrees (clockwise).</param>
    public Stamp setRotationClockwise(double degrees) {
        this.rotateDegrees = (float)-degrees;
        return this;
    }

    /// <summary>
    /// Sets counter-clockwise rotation.
    /// </summary>
    /// <param name="degrees">The rotation angle in degrees (counter-clockwise).</param>
    public Stamp setRotationCounterClockwise(double degrees) {
        this.rotateDegrees = (float)degrees;
        return this;
    }

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
        final int length = str.length();    // number of UTF‑16 code units
        int i = 0;
        while (i < length) {
            int codePoint = str.codePointAt(i);   // full Unicode scalar value
            i += Character.charCount(codePoint);  // advance 1 or 2 char positions
            // Skip the Byte Order Mark (U+FEFF)
            if (codePoint != 0xFEFF) {
                int gid = (codePoint < font.firstChar || codePoint > font.lastChar)
                    ? font.unicodeToGID[0x0020] // Use space fallback if outside the font's supported range
                    : font.unicodeToGID[codePoint];
                appendCodePointAsHex(gid);
            }
        }
    }

    private void append(Point point) {
        append(point.x);
        append(" ");
        append(height - point.y);
        append(" ");
    }

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
        page.append("q\n"); // Save the graphics state

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

        page.append("Q\n"); // Restore the graphics state
        // page.addEMC();

        return new float[] { this.x + width, this.y + height };
    }
}   // End of Stamp.java

