/*
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
/// <summary>
/// Content that is drawn once with the path and text methods of this class,
/// written to the document as a PDF form XObject by Complete, and placed on
/// pages with DrawOn, at a location, a rotation and a scale, as a Container
/// is.
///
/// What a stamp is good at: the content is in the file once, and each
/// placement is the q, the transform and the Do that name it -- 83 bytes for a
/// 200 by 50 point box with two lines of text, against the 367 a Container
/// writes into every page for the same drawing. From the fourth page on the
/// stamp is the smaller of the two: over 100 pages it saves 12,855 bytes of a
/// 104,530 byte file, and over 500 pages 66,052 of 275,553. The whole stamp is
/// one structure element in a PDF/UA document, with the alternate description,
/// the language and the actual text it is given, which is what a logo or a
/// watermark wants.
///
/// What it costs: a stamp draws with the methods of this class -- MoveTo,
/// LineTo, CurveTo, DrawRect, FillRect and DrawText -- and holds no
/// drawable of its own, so it has no images, no annotations, no tables and no
/// barcodes. Its text needs a font that is embedded in the document: a core
/// font and a CJK font are refused, since a stamp has no map from their
/// characters to their glyphs. It has to be completed before it is drawn, and
/// completed once. On fewer than four pages it is the larger of the two, since
/// its XObject costs about 300 bytes of its own.
///
/// Use a Container instead to group drawable elements -- Rect, TextLine,
/// Image, Table, a chart, a barcode, an annotation -- that are moved, rotated
/// and scaled together, and to lay a group out once for one page. A container
/// can hold a stamp, so a header written once can be placed inside a group
/// that is rotated with the rest of it.
///
/// Please see Example_35.
/// </summary>
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
    private float scaleX = 1f;
    private float scaleY = 1f;
    private MemoryStream buf = new MemoryStream();
    private List<Font> fonts = new List<Font>();
    private String language = null;
    private String actualText = null;
    private String altDescription = null;
    private bool completed = false;

    /// <summary>Creates a stamp for the specified document.</summary>
    public Stamp(PDF pdf) {
        this.pdf = pdf;
    }

    /// <summary>Sets the size of this stamp.</summary>
    public Stamp SetSize(float width, float height) {
        this.width = width;
        this.height = height;
        return this;
    }

    /// <summary>Adds a font used by the text on this stamp.</summary>
    public Stamp AddFont(Font font) {
        if (font.pdf != null && font.pdf != pdf) {
            pdf.Fail(new ArgumentException("The font belongs to another PDF."));
        }
        if (!fonts.Contains(font)) {
            fonts.Add(font);
        }
        return this;
    }

    IDrawable IDrawable.SetLocation(float x, float y) {
        return SetLocation(x, y);
    }

    /// <summary>Sets the location of the top left corner of this stamp on the page.</summary>
    public Stamp SetLocation(float x, float y) {
        this.x = x;
        this.y = y;
        return this;
    }

    /// <summary>Sets the language of this stamp, used for accessibility.</summary>
    public Stamp SetLanguage(String language) {
        this.language = language;
        return this;
    }

    /// <summary>Sets the alternate description of this stamp.</summary>
    public Stamp SetAltDescription(String altDescription) {
        this.altDescription = altDescription;
        return this;
    }

    /// <summary>Sets the actual text for this stamp.</summary>
    public Stamp SetActualText(String actualText) {
        this.actualText = actualText;
        return this;
    }

    private void Append(float value) {
        CheckNotCompleted();
        if (!FastFloat.IsWritable(value)) {
            pdf.Fail(new ArgumentException(FastFloat.NOT_WRITABLE));
        }
        byte[] bytes = FastFloat.ToByteArray(value);
        buf.Write(bytes, 0, bytes.Length);
    }

    private void Append(String str) {
        CheckNotCompleted();
        byte[] bytes = Encoding.UTF8.GetBytes(str);
        buf.Write(bytes, 0, bytes.Length);
    }

    // The content drawn after Complete() would be lost.
    private void CheckNotCompleted() {
        if (completed) {
            pdf.Fail(new InvalidOperationException("The stamp was already completed."));
        }
    }

    /// <summary>Sets the fill color for the content drawn after it, from an array of red, green and blue values.</summary>
    public Stamp SetFillColor(float[] rgbColor) {
        Append(rgbColor[0]);
        Append(" ");
        Append(rgbColor[1]);
        Append(" ");
        Append(rgbColor[2]);
        Append(" rg\n");
        this.fillColor = Util.CopyOf(rgbColor);
        return this;
    }

    /// <summary>Sets the fill color for the content drawn after it, as a 0xRRGGBB value.</summary>
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

    /// <summary>Sets the stroke color for the content drawn after it, from an array of red, green and blue values.</summary>
    public Stamp SetStrokeColor(float[] rgbColor) {
        Append(rgbColor[0]);
        Append(" ");
        Append(rgbColor[1]);
        Append(" ");
        Append(rgbColor[2]);
        Append(" RG\n");
        this.strokeColor = Util.CopyOf(rgbColor);
        return this;
    }

    /// <summary>Sets the stroke color for the content drawn after it, as a 0xRRGGBB value.</summary>
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
        this.strokeColor = new float[] {r, g, b};
        return this;
    }

    /// <summary>Sets the stroke width for the content drawn after it.</summary>
    public Stamp SetStrokeWidth(float width) {
        if (width < 0f) {
            pdf.Fail(new ArgumentException("The stroke width cannot be negative."));
        }
        Append(width);
        Append(" w\n");
        this.strokeWidth = width;
        return this;
    }

    /// <summary>Begins a new path at the specified point.</summary>
    public Stamp MoveTo(float x, float y) {
        Append(x);
        Append(" ");
        Append(height - y);
        Append(" m\n");
        return this;
    }

    /// <summary>Adds a straight line from the current point to the specified point.</summary>
    public Stamp LineTo(float x, float y) {
        Append(x);
        Append(" ");
        Append(height - y);
        Append(" l\n");
        return this;
    }

    /// <summary>Adds a cubic Bézier curve from the current point to x3, y3, using x1, y1 and x2, y2 as control points.</summary>
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

    /// <summary>Strokes the current path.</summary>
    public Stamp StrokePath() {
        Append("S\n");
        return this;
    }

    /// <summary>Closes and strokes the current path.</summary>
    public Stamp ClosePath() {
        Append("s\n");
        return this;
    }

    /// <summary>Fills the current path.</summary>
    public Stamp FillPath() {
        Append("f\n");
        return this;
    }

    /// <summary>Closes, fills and strokes the current path.</summary>
    public Stamp CloseFillAndStrokePath() {
        Append("b\n");
        return this;
    }

    /// <summary>Draws the outline of a rectangle.</summary>
    public Stamp DrawRect(float x, float y, float w, float h) {
        MoveTo(x, y);
        LineTo(x + w, y);
        LineTo(x + w, y + h);
        LineTo(x, y + h);
        ClosePath();
        return this;
    }

    /// <summary>Draws a filled rectangle.</summary>
    public Stamp FillRect(float x, float y, float w, float h) {
        MoveTo(x, y);
        LineTo(x + w, y);
        LineTo(x + w, y + h);
        LineTo(x, y + h);
        FillPath();
        return this;
    }

    /// <summary>Draws text using the font, font size, location and text in the parameters.</summary>
    public Stamp DrawText(TextParameters parameters) {
        return DrawText(parameters.font, parameters.fontSize, parameters.x, parameters.y, parameters.text);
    }

    /// <summary>Draws text on this stamp with an embedded font, which the stamp adds to its fonts.</summary>
    public Stamp DrawText(Font font, float fontSize, float x, float y, String text) {
        if (font == null || text == null) {
            pdf.Fail(new ArgumentException("Stamp text needs a font and a text."));
        }
        if (font.isCoreFont || font.isCJK) {
            pdf.Fail(new ArgumentException("A stamp draws text with an embedded font, not a core or CJK font."));
        }
        AddFont(font);
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
    /// Rotates this stamp around its center: clockwise for a positive angle, as
    /// every rotation in PDFjet turns, and counterclockwise for a negative angle.
    /// </summary>
    /// <param name="degrees">The rotation angle in degrees.</param>
    public Stamp SetRotation(float degrees) {
        // The rotation of the page turns counterclockwise.
        this.rotateDegrees = -degrees;
        return this;
    }

    /// <summary>Scales this stamp around its center when it is placed on a page; 1 is its size.</summary>
    public Stamp ScaleBy(float factor) {
        return ScaleBy(factor, factor);
    }

    /// <summary>Scales this stamp around its center when it is placed on a page.</summary>
    public Stamp ScaleBy(float sx, float sy) {
        this.scaleX = sx;
        this.scaleY = sy;
        return this;
    }

    /// <summary>
    /// Writes this stamp to the document as a form XObject.
    /// Call it once, after drawing the content and before DrawOn.
    /// </summary>
    public void Complete() {
        if (completed) {
            pdf.Fail(new InvalidOperationException("Complete() was already called on the stamp."));
        }
        completed = true;
        pdf.NewObj();
        pdf.Append(Token.BeginDictionary);
        pdf.Append("/Type /XObject\n");
        pdf.Append("/Subtype /Form\n");

        pdf.Append("/BBox [0 0 ");
        pdf.Append(width);
        pdf.Append(' ');
        pdf.Append(height);
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
        int i = 0;
        while (i < str.Length) {
            int codePoint = char.ConvertToUtf32(str, i);   // full Unicode scalar value
            i += char.IsHighSurrogate(str[i]) ? 2 : 1;      // advance 1 or 2 char positions

            if (codePoint == 0xFEFF) { continue; }  // Skip the BOM

            // The glyphs Page draws. The .notdef glyph of a character the font
            // does not have is drawn in a marked content span with the
            // character as its actual text, as on a page.
            int gid = Page.GlyphOf(font, codePoint);
            if (font.Lacks(codePoint)) {
                Append("> Tj\n/Span <</ActualText <");
                Append(Page.ToUTF16Hex(Page.TextOf(font, codePoint)));
                Append(">>> BDC\n<");
                AppendCodePointAsHex(gid);
                Append("> Tj\nEMC\n<");
            } else {
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

    /// <summary>
    /// Draws a path through the points. Control points define Bézier curves.
    /// Fewer than two points paint nothing.
    /// </summary>
    public void DrawPath(List<Point> path, PathOperator pathOperator) {
        if (path.Count < 2) {
            return; // A path needs two points to paint anything.
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
        Append(pathOperator.ToOperator());
        Append('\n');
    }

    private void AppendCodePointAsHex(int codePoint) {
        buf.WriteByte(Page.HEX[(codePoint >> 12) & 0xF]);
        buf.WriteByte(Page.HEX[(codePoint >> 8)  & 0xF]);
        buf.WriteByte(Page.HEX[(codePoint >> 4)  & 0xF]);
        buf.WriteByte(Page.HEX[codePoint & 0xF]);
    }

    /// <summary>Draws this stamp on the specified page and returns the x and y coordinates of its bottom right corner.</summary>
    public float[] DrawOn(Page page) {
        if (page == null) {
            return new float[] { this.x + width, this.y + height };  // Measured, not drawn
        }
        if (page.pdf != pdf) {
            page.pdf.Fail(new ArgumentException("The stamp belongs to another PDF."));
        }
        if (!completed) {
            page.pdf.Fail(new InvalidOperationException("Call Complete() on the stamp before drawing it."));
        }
        if (width == 0f || height == 0f || scaleX == 0f || scaleY == 0f) {
            return new float[] { this.x + width, this.y + height };  // Nothing to paint.
        }
        page.AddBDC(StructElem.P, language, actualText, altDescription);
        page.SaveGraphicsState();

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
        page.Append(cos);
        page.Append(' ');
        page.Append(sin);
        page.Append(' ');
        page.Append(-sin);
        page.Append(' ');
        page.Append(cos);
        page.Append(" 0 0 cm\n");

        // SCALE: around the center, like a Container
        if (scaleX != 1f || scaleY != 1f) {
            page.Append(scaleX);
            page.Append(" 0 0 ");
            page.Append(scaleY);
            page.Append(" 0 0 cm\n");
        }

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

        page.RestoreGraphicsState();
        page.AddEMC();

        return new float[] { this.x + width, this.y + height };
    }
}   // End of Stamp.cs
}   // End of PDFjet.NET
