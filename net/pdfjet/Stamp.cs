/**
 * Stamp.cs
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
using System;
using System.IO;
using System.Text;
using System.Collections.Generic;

namespace PDFjet.NET {
public class Stamp : IDrawable {
    internal int objNumber;

    private PDF pdf;
    private float x;
    private float y;
    private float width;
    private float height;
    private float[] fillColor;
    private float[] strokeColor;
    private float strokeWidth = 1f;
    private float rotateDegrees = 0f;
    private MemoryStream buf = new MemoryStream();
    private List<Font> fonts = new List<Font>();

    public Stamp(PDF pdf) {
        this.pdf = pdf;
    }

    public Stamp WithSize(float width, float height) {
        this.width = width;
        this.height = height;
        return this;
    }

    public Stamp WithFont(Font font) {
        fonts.Add(font);
        return this;
    }

    public void SetPosition(float x, float y) {
        this.x = x;
        this.y = y;
    }

    public Stamp SetLocation(float x, float y) {
        this.x = x;
        this.y = y;
        return this;
    }

    public Stamp SetLocation(double x, double y) {
        return SetLocation((float) x, (float) y);
    }

    private void Append(float value) {
        byte[] bytes = FastFloat.ToByteArray(value);
        buf.Write(bytes, 0, bytes.Length);
    }

    private void Append(String str) {
        byte[] bytes = Encoding.UTF8.GetBytes(str);
        buf.Write(bytes, 0, bytes.Length);
    }

    public Stamp SetFillColor(float[] rgbColor) {
        Append(rgbColor[0]);
        Append(" ");
        Append(rgbColor[1]);
        Append(" ");
        Append(rgbColor[2]);
        Append(" rg\n");
        this.fillColor = rgbColor;
        return this;
    }

    public Stamp SetFillColor(int color) {
        float r = ((color >> 16) & 0xff)/255f;
        float g = ((color >>  8) & 0xff)/255f;
        float b = ((color)       & 0xff)/255f;
        Append(r);
        Append(" ");
        Append(g);
        Append(" ");
        Append(b);
        Append(" rg\n");
        this.fillColor = new float[] {r, g, b};
        return this;
    }

    public Stamp SetStrokeColor(float[] rgbColor) {
        Append(rgbColor[0]);
        Append(" ");
        Append(rgbColor[1]);
        Append(" ");
        Append(rgbColor[2]);
        Append(" RG\n");
        this.strokeColor = rgbColor;
        return this;
    }

    public Stamp SetStrokeColor(int color) {
        float r = ((color >> 16) & 0xff)/255f;
        float g = ((color >>  8) & 0xff)/255f;
        float b = ((color)       & 0xff)/255f;
        Append(r);
        Append(" ");
        Append(g);
        Append(" ");
        Append(b);
        Append(" RG\n");
        this.fillColor = new float[] {r, g, b};
        return this;
    }

    public Stamp SetStrokeWidth(float width) {
        Append(width);
        Append(" w\n");
        this.strokeWidth = width;
        return this;
    }

    public Stamp MoveTo(float x, float y) {
        Append(x);
        Append(" ");
        Append(height - y);
        Append(" m\n");
        return this;
    }

    public Stamp LineTo(float x, float y) {
        Append(x);
        Append(" ");
        Append(height - y);
        Append(" l\n");
        return this;
    }

    public Stamp CurveTo(
            float x1,
            float y1,
            float x2,
            float y2,
            float x3,
            float y3) {
        Append(x1);
        Append(" ");
        Append(height - y1);
        Append(" ");
        Append(x2);
        Append(" ");
        Append(height - y2);
        Append(" ");
        Append(x3);
        Append(" ");
        Append(height - y3);
        Append(" c\n");
        return this;
    }

    public Stamp StrokePath() {
        Append("S\n");
        return this;
    }

    public Stamp ClosePath() {
        Append("s\n");
        return this;
    }

    public Stamp FillPath() {
        Append("f\n");
        return this;
    }

    public Stamp CloseFillAndStrokePath() {
        Append("b\n");
        return this;
    }

    // TODO:
    public Stamp Rectangle() {
        return this;
    }

    public Stamp Draw() {
        return this;
    }

    public Stamp DrawRect(float x, float y, float w, float h) {
        MoveTo(x, y);
        LineTo(x + w, y);
        LineTo(x + w, y + h);
        LineTo(x, y + h);
        ClosePath();
        return this;
    }

    public Stamp FillRect(float x, float y, float w, float h) {
        MoveTo(x, y);
        LineTo(x + w, y);
        LineTo(x + w, y + h);
        LineTo(x, y + h);
        FillPath();
        return this;
    }

    public Stamp DrawText(TextParameters parameters) {
        return DrawText(parameters.font, parameters.fontSize, parameters.x, parameters.y, parameters.text);
    }

    public Stamp DrawText(Font font, float fontSize, float x, float y, String text) {
        Append("BT\n");
        Append("/F");
        Append(font.objNumber);
        Append(" ");
        Append(fontSize);
        Append(" Tf\n");
        Append(x);
        Append(" ");
        Append(height - y);
        Append(" Td\n");
        Append("<");
        DrawText(font, text);
        Append("> Tj\n");
        Append("ET\n");
        return this;
    }

    /// <summary>
    /// Sets the rotation angle.
    /// </summary>
    /// <param name="degrees">The rotation angle in degrees.</param>
    public Stamp Rotate(double degrees) {
        this.rotateDegrees = (float)degrees;
        return this;
    }

    /// <summary>
    /// Sets the rotation angle.
    /// </summary>
    /// <param name="degrees">The rotation angle in degrees.</param>
    public Stamp SetRotation(double degrees) {
        this.rotateDegrees = (float)degrees;
        return this;
    }

    /// <summary>
    /// Sets clockwise rotation.
    /// </summary>
    /// <param name="degrees">The rotation angle in degrees (clockwise).</param>
    public Stamp SetRotationClockwise(double degrees) {
        this.rotateDegrees = (float)-degrees;
        return this;
    }

    /// <summary>
    /// Sets counter-clockwise rotation.
    /// </summary>
    /// <param name="degrees">The rotation angle in degrees (counter-clockwise).</param>
    public Stamp SetRotationCounterClockwise(double degrees) {
        this.rotateDegrees = (float)degrees;
        return this;
    }

    public void Complete() {
        pdf.NewObj();
        pdf.Append(Token.BeginDictionary);
        pdf.Append("/Type /XObject\n");
        pdf.Append("/Subtype /Form\n");

        pdf.Append("/BBox [0 0 ");
        pdf.Append(FastFloat.ToByteArray(width));
        pdf.Append(' ');
        pdf.Append(FastFloat.ToByteArray(height));
        pdf.Append("]\n");

        pdf.Append("/Resources <<\n");
        if (fonts.Count > 0) {
            pdf.Append("/Font <<\n");
            foreach (Font font in fonts) {
                pdf.Append("/F");
                pdf.Append(font.objNumber);
                pdf.Append(" ");
                pdf.Append(font.objNumber);
                pdf.Append(" 0 R\n");
            }
            pdf.Append(">>\n");
        }
        pdf.Append(">>\n");
        pdf.Append("/Length ");
        pdf.Append(buf.Length);
        pdf.Append(Token.Newline);
        pdf.Append(Token.EndDictionary);    // End of XObject dictionary
        pdf.Append(Token.Stream);
        pdf.Append(buf.ToArray());
        pdf.Append(Token.EndStream);
        pdf.EndObj();
        pdf.stamps.Add(this);
        objNumber = pdf.GetObjNumber();
    }

    private void DrawText(Font font, string str) {
        foreach (char c in str) {
            int codePoint = c;
            if (codePoint != 0xFEFF) {          // Skip BOM
                int gid = (codePoint < font.firstChar || codePoint > font.lastChar)
                    ? font.unicodeToGID[0x0020] // Use space fallback if outside the font's supported range
                    : font.unicodeToGID[codePoint];
                AppendCodePointAsHex(gid);
            }
        }
    }

    private void Append(Point point) {
        Append(point.x);
        Append(" ");
        Append(height - point.y);
        Append(" ");
    }

    public void DrawPath(List<Point> path, string pathOperator) {
        if (path.Count < 2) {
            throw new Exception("The Path object must contain at least 2 points");
        }
        Point point = path[0];
        MoveTo(point.x, point.y);
        char controlPoint = '\0';
        for (int i = 1; i < path.Count; i++) {
            point = path[i];
            if (point.controlPoint != '\0') {
                controlPoint = point.controlPoint;
                Append(point);
            } else {
                if (controlPoint != '\0') {
                    Append(point);
                    Append(controlPoint);
                    Append('\n');
                    controlPoint = '\0';
                } else {
                    LineTo(point.x, point.y);
                }
            }
        }
        // Catch unflushed control point
        if (controlPoint != '\0') {
            throw new Exception(
                "Path ends with unconsumed control point(s). " +
                "Each 'c' requires 2 CPs + 1 endpoint, 'v'/'y' require 1 CP + 1 endpoint.");
        }
        Append(pathOperator);
        Append('\n');
    }

    private void AppendCodePointAsHex(int codePoint) {
        buf.WriteByte(Page.HEX[(codePoint >> 12) & 0xF]);
        buf.WriteByte(Page.HEX[(codePoint >> 8)  & 0xF]);
        buf.WriteByte(Page.HEX[(codePoint >> 4)  & 0xF]);
        buf.WriteByte(Page.HEX[codePoint & 0xF]);
    }

    public float[] DrawOn(Page page) {
        // page.AddBMC(StructElem.Figure, language, actualText, altDescription);
        page.Append("q\n"); // Save the graphics state

        float drawX = this.x;
        float drawY = (page.height - this.height) - this.y;

        // 5. POSITION: move to desired location on page
        page.Append("1 0 0 1 ");
        page.Append(drawX);
        page.Append(' ');
        page.Append(drawY);
        page.Append(" cm\n");

        // 4. MOVE BACK: after rotation
        page.Append("1 0 0 1 ");
        page.Append(width/2);
        page.Append(' ');
        page.Append(height/2);
        page.Append(" cm\n");

        // 3. ROTATE: rotate around origin
        double radians = rotateDegrees * (Math.PI / 180);
        float cos = (float)Math.Cos(radians);
        float sin = (float)Math.Sin(radians);
        page.Append(FastFloat.ToByteArray(cos));
        page.Append(' ');
        page.Append(FastFloat.ToByteArray(sin));
        page.Append(' ');
        page.Append(FastFloat.ToByteArray(-sin));
        page.Append(' ');
        page.Append(FastFloat.ToByteArray(cos));
        page.Append(" 0 0 cm\n");

        // 2. MOVE: move the center of the object to origin
        page.Append("1 0 0 1 ");
        page.Append(-width/2);
        page.Append(' ');
        page.Append(-height/2);
        page.Append(" cm\n");

        // 1. DRAW: draw the object
        page.Append("/Fm");
        page.Append(objNumber);
        page.Append(" Do\n");

        page.Append("Q\n"); // Restore the graphics state
        // page.AddEMC();

        return new float[] { this.x + width, this.y + height };
    }
}   // End of Stamp.cs
}   // End of PDFjet.NET
