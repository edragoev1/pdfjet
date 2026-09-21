/*
 * Page.cs
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
/// Used to create PDF page objects.
///
/// Please note:
/// <code>
/// The coordinate (0.0f, 0.0f) is the top left corner of the page.
/// The size of the pages are represented in points.
/// 1 point is 1/72 inches.
/// </code>
/// </summary>
public class Page {
    /// <summary>Pass to the Page constructor to create a page that is not added to the PDF right away.</summary>
    public const bool DETACHED = false;

    /// <summary>Index of the horizontal scale in an Android Matrix value array.</summary>
    public const int MSCALE_X = 0;
    /// <summary>Index of the horizontal skew in an Android Matrix value array.</summary>
    public const int MSKEW_X = 1;
    /// <summary>Index of the horizontal translation in an Android Matrix value array.</summary>
    public const int MTRANS_X = 2;
    /// <summary>Index of the vertical skew in an Android Matrix value array.</summary>
    public const int MSKEW_Y = 3;
    /// <summary>Index of the vertical scale in an Android Matrix value array.</summary>
    public const int MSCALE_Y = 4;
    /// <summary>Index of the vertical translation in an Android Matrix value array.</summary>
    public const int MTRANS_Y = 5;

    internal PDF pdf;
    internal PDFobj pageObj;
    internal int objNumber;

    internal MemoryStream buf;
    internal float width;
    internal float height;

    internal int renderingMode = 0;

    internal float[] cropBox;
    internal float[] bleedBox;
    internal float[] trimBox;
    internal float[] artBox;

    private float[] brushColor = {0f, 0f, 0f};
    private float[] penColor = {0f, 0f, 0f};
    // A color passed to the methods that take an array, filled from a packed
    // color each time; those methods copy the colors they keep.
    private readonly float[] packedRGB = new float[3];
    // True when the content has set the brush or the pen to the RGB color above,
    // so setting it again writes nothing. False on a new page, whose colors are
    // not written yet, and after a CMYK color. The pen width and the font of the
    // text are kept the same way, and q and Q save and restore all of them, as
    // they save and restore the graphics state of the PDF.
    private bool brushColorWritten = false;
    private bool penColorWritten = false;
    private bool penWidthWritten = false;
    private Font writtenFont = null;        // The font of the last Tf, or null
    private float writtenFontSize = 0f;

    private float[] tmx = {1f, 0f, 0f, 1f};
    private float textFontSize = 0f;    // The font size of the text drawn last
    private float textRise = 0f;
    // The bytes of the text matrix, written for every string while the page
    // has a text rotation. SetTextRotation fills them, and DrawString reads
    // them only where tmx is not the identity, which is only after it ran.
    private byte[] tm0;
    private byte[] tm1;
    private byte[] tm2;
    private byte[] tm3;

    internal float penWidth = 1f;   // The PDF default, as no w is written first

    internal CapStyle lineCapStyle = CapStyle.BUTT;
    internal JoinStyle lineJoinStyle = JoinStyle.MITER;
    internal String strokeDashPattern = "[] 0";
    // The states that SaveGraphicsState saved and RestoreGraphicsState restores.
    private readonly List<State> savedStates = new List<State>();
    // The AddBDC and AddArtifactBMC calls that AddEMC has not ended yet.
    private int markedContentDepth = 0;
    // The markedContentDepth of the outermost artifact, or 0 outside of one.
    // Content inside an artifact is not tagged, so an AddBDC there writes
    // nothing, and neither does its AddEMC.
    private int artifactDepth = 0;
    // The structure element that the elements of AddBDC and AddAnnotation
    // become the kids of, like a table cell, or null for the Document element.
    internal StructElement structParent = null;
    // True once the page is added to its PDF.
    internal bool added = false;
    // The dictionary of a page merged from a document that was read, with the
    // object numbers of this document, or null for a page drawn with PDFjet.
    internal List<String> mergedDict = null;

    internal float rotateDegrees = 0f;

    internal readonly List<Int32> contents = new List<Int32>();
    internal readonly List<Annotation> annots = new List<Annotation>();
    internal readonly List<Destination> destinations = new List<Destination>();
    internal List<StructElement> structures = new List<StructElement>();
    // The object number of the element of each marked content, which the
    // parent tree is written from. The elements themselves are written with
    // the page and let go of.
    internal List<int> mcidNumbers = new List<int>();

    private int mcid;

    /// <summary>
    /// Creates page object and add it to the PDF document.
    ///
    /// Please note:
    /// <code>
    /// The coordinate (0.0, 0.0) is the top left corner of the page.
    /// The size of the pages are represented in points.
    /// 1 point is 1/72 inches.
    /// </code>
    /// </summary>
    /// <param name="pdf">the pdf object.</param>
    /// <param name="pageSize">the page size of this page.</param>
    public Page(PDF pdf, PageSize pageSize) : this(pdf, pageSize, true) {
    }

    /// <summary>
    /// Creates page object and add it to the PDF document.
    ///
    /// Please note:
    /// <code>
    /// The coordinate (0.0, 0.0) is the top left corner of the page.
    /// The size of the pages are represented in points.
    /// 1 point is 1/72 inches.
    /// </code>
    /// </summary>
    /// <param name="pdf">the pdf object.</param>
    /// <param name="pageSize">the page size of this page.</param>
    /// <param name="addPageToPDF">bool flag.</param>
    public Page(PDF pdf, PageSize pageSize, bool addPageToPDF) {
        this.pdf = pdf;
        this.width = pageSize.GetWidth();
        this.height = pageSize.GetHeight();
        if (pdf.completed) {
            pdf.Fail(new InvalidOperationException("The PDF was already completed."));
        }
        // The page size limits of the PDF specification, in Annex C.
        if (!(width >= 3f && width <= 14400f && height >= 3f && height <= 14400f)) {
            pdf.Fail(new ArgumentException("A page must be from 3 to 14400 points wide and high."));
        }
        pdf.pagesCreated++;
        this.buf = new MemoryStream(8192);
        if (addPageToPDF) {
            pdf.AddPage(this);
        }
    }

    /// <summary>Creates a page from a page object read from an existing PDF.</summary>
    public Page(PDF pdf, PDFobj pageObj) {
        this.pdf = pdf;
        this.pageObj = RemoveComments(pageObj);
        PageSize pageSize = pageObj.GetPageSize();
        this.width = pageSize.GetWidth();
        this.height = pageSize.GetHeight();
        this.buf = new MemoryStream(8192);
        SaveGraphicsState();
        if (pageObj.gsNumber != -1) {
            Append("/GS");
            Append(pageObj.gsNumber + 1);
            Append(" gs\n");
        }
    }

    /// <summary>Finishes a page read from an existing PDF by adding its new content to the objects.</summary>
    public void Complete(List<PDFobj> objects) {
        RestoreGraphicsState();
        CheckBalanced();
        pageObj.AddContent(GetContent(), objects);
    }

    // Checks that every SaveGraphicsState has its RestoreGraphicsState and every
    // AddBDC or AddArtifactBMC its AddEMC, before the page content is written.
    internal void CheckBalanced() {
        if (savedStates.Count > 0) {
            pdf.Fail(new InvalidOperationException("A page ends with a SaveGraphicsState that has no RestoreGraphicsState."));
        }
        if (markedContentDepth != 0) {
            pdf.Fail(new InvalidOperationException("A page ends with an AddBDC or AddArtifactBMC that has no AddEMC."));
        }
    }

    // The content of a page that was written to the PDF. Drawing on it fails,
    // as the drawing would be lost.
    internal sealed class WrittenContent : MemoryStream {
        private readonly PDF pdf;

        internal WrittenContent(PDF pdf) : base(0) {
            this.pdf = pdf;
        }

        public override void Write(byte[] buffer, int offset, int count) {
            Fail();
        }

        public override void Write(ReadOnlySpan<byte> buffer) {
            Fail();
        }

        public override void WriteByte(byte value) {
            Fail();
        }

        private void Fail() {
            pdf.Fail(new InvalidOperationException("The page was already written to the PDF: "
                    + "draw on a page before creating the next page or completing the PDF."));
        }
    }

    // A page merged from a document that was read. Its dictionary is written
    // when the PDF is completed, under the object number reserved for it, and
    // nothing can be drawn on it.
    internal Page(PDF pdf, int objNumber, List<String> mergedDict) {
        this.pdf = pdf;
        this.objNumber = objNumber;
        this.mergedDict = mergedDict;
        this.added = true;
        this.buf = new WrittenContent(pdf);
    }

    private PDFobj RemoveComments(PDFobj obj) {
        List<String> list = new List<String>();
        bool comment = false;
        foreach (String token in obj.dict) {
            if (token.Equals("%")) {
                comment = true;
            } else {
                if (token.StartsWith("/")) {
                    comment = false;
                    list.Add(token);
                } else {
                    if (!comment) {
                        list.Add(token);
                    }
                }
            }
        }
        obj.dict = list;
        return obj;
    }

    /// <summary>Adds a core font to the resources of this page and returns the font.</summary>
    public Font AddResource(int coreFont, List<PDFobj> objects) {
        return pageObj.AddResource(coreFont, objects);
    }

    /// <summary>Adds an image to the resources of this page.</summary>
    public void AddResource(Image image, List<PDFobj> objects) {
        pageObj.AddResource(image, objects);
    }

    /// <summary>Adds a font to the resources of this page.</summary>
    public void AddResource(Font font, List<PDFobj> objects) {
        pageObj.AddResource(font, objects);
    }

    /// <summary>
    /// Adds destination to this page.
    /// </summary>
    /// <param name="name">The destination name.</param>
    /// <param name="xPosition">The horizontal position of the destination on this page.</param>
    /// <param name="yPosition">The vertical position of the destination on this page.</param>
    /// <returns>the destination.</returns>
    public Destination AddDestination(String name, float xPosition, float yPosition) {
        Destination dest = new Destination(name, xPosition, height - yPosition);
        destinations.Add(dest);
        return dest;
    }

    /// <summary>
    /// Adds destination to this page.
    /// </summary>
    /// <param name="name">The destination name.</param>
    /// <param name="yPosition">The vertical position of the destination on this page.</param>
    /// <returns>the destination.</returns>
    public Destination AddDestination(String name, float yPosition) {
        Destination dest = new Destination(name, 0f, height - yPosition);
        destinations.Add(dest);
        return dest;
    }

    /// <summary>
    /// Sets the page CropBox.
    /// See page 77 of the PDF32000_2008.pdf specification.
    /// </summary>
    /// <param name="upperLeftX">the top left X coordinate of the CropBox.</param>
    /// <param name="upperLeftY">the top left Y coordinate of the CropBox.</param>
    /// <param name="lowerRightX">the bottom right X coordinate of the CropBox.</param>
    /// <param name="lowerRightY">the bottom right Y coordinate of the CropBox.</param>
    /// <returns>this Page object.</returns>
    public Page SetCropBox(
            float upperLeftX, float upperLeftY, float lowerRightX, float lowerRightY) {
        this.cropBox = new float[] {upperLeftX, upperLeftY, lowerRightX, lowerRightY};
        return this;
    }

    /// <summary>
    /// Sets the page BleedBox.
    /// See page 77 of the PDF32000_2008.pdf specification.
    /// </summary>
    /// <param name="upperLeftX">the top left X coordinate of the BleedBox.</param>
    /// <param name="upperLeftY">the top left Y coordinate of the BleedBox.</param>
    /// <param name="lowerRightX">the bottom right X coordinate of the BleedBox.</param>
    /// <param name="lowerRightY">the bottom right Y coordinate of the BleedBox.</param>
    /// <returns>this Page object.</returns>
    public Page SetBleedBox(
            float upperLeftX, float upperLeftY, float lowerRightX, float lowerRightY) {
        this.bleedBox = new float[] {upperLeftX, upperLeftY, lowerRightX, lowerRightY};
        return this;
    }

    /// <summary>
    /// Sets the page TrimBox.
    /// See page 77 of the PDF32000_2008.pdf specification.
    /// </summary>
    /// <param name="upperLeftX">the top left X coordinate of the TrimBox.</param>
    /// <param name="upperLeftY">the top left Y coordinate of the TrimBox.</param>
    /// <param name="lowerRightX">the bottom right X coordinate of the TrimBox.</param>
    /// <param name="lowerRightY">the bottom right Y coordinate of the TrimBox.</param>
    /// <returns>this Page object.</returns>
    public Page SetTrimBox(
            float upperLeftX, float upperLeftY, float lowerRightX, float lowerRightY) {
        this.trimBox = new float[] {upperLeftX, upperLeftY, lowerRightX, lowerRightY};
        return this;
    }

    /// <summary>
    /// Sets the page ArtBox.
    /// See page 77 of the PDF32000_2008.pdf specification.
    /// </summary>
    /// <param name="upperLeftX">the top left X coordinate of the ArtBox.</param>
    /// <param name="upperLeftY">the top left Y coordinate of the ArtBox.</param>
    /// <param name="lowerRightX">the bottom right X coordinate of the ArtBox.</param>
    /// <param name="lowerRightY">the bottom right Y coordinate of the ArtBox.</param>
    /// <returns>this Page object.</returns>
    public Page SetArtBox(
            float upperLeftX, float upperLeftY, float lowerRightX, float lowerRightY) {
        this.artBox = new float[] {upperLeftX, upperLeftY, lowerRightX, lowerRightY};
        return this;
    }

    /// <summary>Draws the text line centered at the top of the page.</summary>
    public float[] AddHeader(TextLine textLine) {
        return AddHeader(textLine, 1.5f*textLine.font.GetAscent(textLine.fontSize));
    }

    /// <summary>Draws the text line centered at the top of the page, with its baseline at the specified offset.</summary>
    public float[] AddHeader(TextLine textLine, float offset) {
        textLine.SetLocation((GetWidth() - textLine.GetWidth())/2, offset);
        float[] xy = textLine.DrawOn(this);
        xy[1] += textLine.font.GetDescent(textLine.fontSize);
        return xy;
    }

    /// <summary>Draws the text line centered at the bottom of the page.</summary>
    public float[] AddFooter(TextLine textLine) {
        return AddFooter(textLine, textLine.font.GetAscent(textLine.fontSize));
    }

    /// <summary>Draws the text line centered at the bottom of the page, with its baseline at the specified offset from the bottom.</summary>
    public float[] AddFooter(TextLine textLine, float offset) {
        textLine.SetLocation((GetWidth() - textLine.GetWidth())/2, GetHeight() - offset);
        return textLine.DrawOn(this);
    }

    /// <summary>Draws the text as a watermark diagonally across the page.</summary>
    public void AddWatermark(Font font, String text) {
        float hypotenuse = (float)
                Math.Sqrt(this.height * this.height + this.width * this.width);
        float stringWidth = font.StringWidth(text);
        float offset = (hypotenuse - stringWidth) / 2f;
        double angle = Math.Atan(this.height / this.width);
        TextLine watermark = new TextLine(font);
        watermark.SetText(text);
        watermark.SetTextColor(Color.lightgrey);
        watermark.SetLocation(
                (float) (offset * Math.Cos(angle)),
                (this.height - (float) (offset * Math.Sin(angle))));
        watermark.SetTextRotation(-(int) (angle * (180.0 / Math.PI)));
        watermark.DrawOn(this);
    }

    /// <summary>Rotates this page clockwise when it is displayed, as every rotation in PDFjet turns and as /Rotate does, by 0, 90, 180 or 270 degrees; other angles are ignored.</summary>
    public Page SetRotation(int degrees) {
        if (degrees == 0 || degrees == 90 || degrees == 180 || degrees == 270) {
            this.rotateDegrees = degrees;
        }
        return this;
    }

    /// <summary>Flips the y axis, so the origin is the top left corner of the page and y grows downward.</summary>
    public void InvertYAxis() {
        Append("1 0 0 -1 0 ");
        Append(this.height);
        Append(" cm\n");
    }

    /// <summary>
    /// Concatenates a transformation matrix to the current one. Save the graphics
    /// state before and restore it after. The page height is divided by the
    /// vertical scale.
    /// </summary>
    /// <param name="values">the scale x, skew x, translate x, skew y, scale y and
    /// translate y at the MSCALE_X to MTRANS_Y indices, the first six values of an
    /// Android Matrix.getValues() array.</param>
    public void Transform(float[] values) {
        float scalex = values[MSCALE_X];
        float scaley = values[MSCALE_Y];
        float transx = values[MTRANS_X];
        float transy = values[MTRANS_Y];

        Append(scalex);
        Append(Token.Space);
        Append(values[MSKEW_X]);
        Append(Token.Space);
        Append(values[MSKEW_Y]);
        Append(Token.Space);
        Append(scaley);
        Append(Token.Space);

        if (Math.Asin(values[MSKEW_Y]) != 0f) {
            transx -= values[MSKEW_Y] * height / scaley;
        }

        Append(transx);
        Append(Token.Space);
        Append(-transy);
        Append(" cm\n");

        this.height = this.height / scaley;
    }

    /// <summary>Returns the content stream of this page.</summary>
    public byte[] GetContent() {
        return buf.ToArray();
    }

    /// <summary>
    /// Returns the width of this page.
    /// </summary>
    /// <returns>the width of the page.</returns>
    public float GetWidth() {
        return width;
    }

    /// <summary>
    /// Returns the height of this page.
    /// </summary>
    /// <returns>the height of the page.</returns>
    public float GetHeight() {
        return height;
    }

    /// <summary>
    /// Draws a line on the page, using the current color, between the points (x1, y1) and (x2, y2).
    /// </summary>
    /// <param name="x1">the first point's x coordinate.</param>
    /// <param name="y1">the first point's y coordinate.</param>
    /// <param name="x2">the second point's x coordinate.</param>
    /// <param name="y2">the second point's y coordinate.</param>
    public void DrawLine(
            float x1,
            float y1,
            float x2,
            float y2) {
        MoveTo(x1, y1);
        LineTo(x2, y2);
        StrokePath();
    }

    /// <summary>Draws the string in black. The fallback font is used for characters the main font does not have.</summary>
    public void DrawString(
            Font font,
            Font fallbackFont,
            float fontSize,
            String str,
            float x,
            float y) {
        DrawString(font, fallbackFont, fontSize, str, x, y, new float[] {0f, 0f, 0f}, null);
    }

    /// <summary>
    /// Draws the string in the specified 0xRRGGBB color, highlighting the words in the colors map.
    /// The fallback font is used for characters the main font does not have.
    /// </summary>
    internal void DrawString(
            Font font,
            Font fallbackFont,
            float fontSize,
            String str,
            float x,
            float y,
            Int32 color,
            Dictionary<String, Int32> colors) {
        float r = ((color >> 16) & 0xff)/255f;
        float g = ((color >>  8) & 0xff)/255f;
        float b = ((color)       & 0xff)/255f;
        DrawString(font, fallbackFont, fontSize, str, x, y, new float[] {r, g, b}, colors);
    }

    /// <summary>
    /// Draws the text given by the specified string,
    /// using the specified Thai or Hebrew font and the current brush color.
    /// If the font is missing some glyphs - the fallback font is used.
    /// The baseline of the leftmost character is at position (x, y) on the page.
    /// </summary>
    /// <param name="font">the Thai or Hebrew font.</param>
    /// <param name="fallbackFont">the fallback font.</param>
    /// <param name="fontSize">the font size.</param>
    /// <param name="str">the string to be drawn.</param>
    /// <param name="x">the x coordinate.</param>
    /// <param name="y">the y coordinate.</param>
    /// <param name="textColor">the text color as an RGB array.</param>
    /// <param name="colors">the words to highlight and their colors.</param>
    internal void DrawString(
            Font font,
            Font fallbackFont,
            float fontSize,
            String str,
            float x,
            float y,
            float[] textColor,
            Dictionary<String, Int32> colors) {
        if (str == null || font.isCJK || fallbackFont == null || fallbackFont.isCJK) {
            DrawString(font, fontSize, str, x, y, textColor, colors);
        } else {
            // Each run of the characters drawn with one font is drawn after the run before it.
            Font activeFont = font;
            int start = 0;
            for (int i = 0; i < str.Length; i += Util.CharCount(str, i)) {
                Font charFont = Font.FontOf(font, fallbackFont, activeFont, str, i);
                if (charFont != activeFont) {
                    String run = str.Substring(start, i - start);
                    DrawString(activeFont, fontSize, run, x, y, textColor, colors);
                    x += activeFont.StringWidth(fontSize, run);
                    start = i;
                    activeFont = charFont;
                }
            }
            DrawString(activeFont, fontSize, str.Substring(start), x, y, textColor, colors);
        }
    }

    /// <summary>Draws the string in black with its baseline at the specified location.</summary>
    public void DrawString(
            Font font,
            float fontSize,
            String str,
            float x,
            float y) {
        DrawString(font, fontSize, str, x, y, new float[] {0f, 0f, 0f}, null);
    }

    /// <summary>
    /// Draws the text given by the specified string,
    /// using the specified font and the current brush color.
    /// The baseline of the leftmost character is at position (x, y) on the page.
    /// </summary>
    /// <param name="font">the font to use.</param>
    /// <param name="fontSize">the font size.</param>
    /// <param name="str">the string to be drawn.</param>
    /// <param name="x">the x coordinate.</param>
    /// <param name="y">the y coordinate.</param>
    /// <param name="textColor">the text color as an RGB array.</param>
    /// <param name="highlightColors">the words to highlight and their colors.</param>
    internal void DrawString(
            Font font,
            float fontSize,
            String str,
            float x,
            float y,
            float[] textColor,
            Dictionary<String, Int32> highlightColors) {
        if (str == null || str.Equals("")) {
            return;
        }

        Append("BT\n");
        SetTextFont(font, fontSize);
        if (renderingMode != 0) {
            Append(renderingMode);
            Append(" Tr\n");
        }

        if (font.skew15 &&
                tmx[0] == 1f &&
                tmx[1] == 0f &&
                tmx[2] == 0f &&
                tmx[3] == 1f) {
            float skew = 0.26f;
            Append(tmx[0]);
            Append(' ');
            Append(tmx[1]);
            Append(' ');
            Append(tmx[2] + skew);
            Append(' ');
            Append(tmx[3]);
            Append(' ');
            Append(x);
            Append(' ');
            Append(height - y);
            Append(" Tm\n");
        } else if (tmx[0] == 1f && tmx[1] == 0f && tmx[2] == 0f && tmx[3] == 1f) {
            // BT sets the line matrix to the identity, which is the text matrix
            // here, so Td puts the text where Tm would, in fewer bytes.
            SetTextLocation(x, y);
        } else {
            Append(tm0);
            Append(' ');
            Append(tm1);
            Append(' ');
            Append(tm2);
            Append(' ');
            Append(tm3);
            Append(' ');
            Append(x);
            Append(' ');
            Append(height - y);
            Append(" Tm\n");
        }

        if (highlightColors == null) {
            SetBrushColor(textColor);
            if (font.isCoreFont) {
                Append("[<");
                DrawASCIIString(font, str);
                Append(">] TJ\n");
            } else {
                Append("<");
                DrawUnicodeString(font, str);
                Append("> Tj\n");
            }
        } else {
            DrawColoredString(font, str, textColor, highlightColors);
        }
        Append("ET\n");
    }

    private void AppendByteAsHex(int b) {
        Span<byte> digits = stackalloc byte[2];
        digits[0] = HEX[(b >> 4) & 0xF];
        digits[1] = HEX[b & 0xF];
        buf.Write(digits);
    }

    private void DrawASCIIString(Font font, String str) {
        for (int i = 0; i < str.Length; ) {
            int cp = Util.CodePointAt(str, i);
            i += (cp > 0xFFFF) ? 2 : 1;
            int c1 = font.CoreFontCode(cp);
            AppendByteAsHex(c1);
            if (font.kernPairs && i < str.Length) {
                int kerning = font.Kerning(c1, font.CoreFontCode(Util.CodePointAt(str, i)));
                if (kerning != 0) {
                    Append(">");
                    Append(-kerning);
                    Append("<");
                }
            }
        }
    }

    internal void DrawUnicodeString(Font font, String str) {
        if (str == null || str.Length == 0) {
            return;
        }
        if (font.isCJK) {
            int i = 0;
            while (i < str.Length) {
                int codePoint = char.ConvertToUtf32(str, i);
                if (codePoint != 0xFEFF) {                  // BOM
                    if (codePoint < font.firstChar || codePoint > font.lastChar) {
                        AppendCodePointAsHex(0x0020);       // Space fallback
                    } else {
                        AppendCodePointAsHex(codePoint);
                    }
                }
                i += char.IsHighSurrogate(str[i]) ? 2 : 1;  // Proper surrogate handling
            }
        } else if (!NeedsShaping(font, str)) {
            int i = 0;
            while (i < str.Length) {
                int codePoint = char.ConvertToUtf32(str, i);
                if (codePoint != 0xFEFF) {                  // BOM
                    AppendCodePointAsHex(GlyphOf(font, codePoint));
                }
                i += char.IsHighSurrogate(str[i]) ? 2 : 1;  // Proper surrogate handling
            }
        } else {
            // The marks are moved to where the GPOS table of the font puts
            // them, the characters Bidi mirrored, each after an RLM, are given
            // the characters they stand for as actual text, and a zero width
            // non-joiner or joiner that the font has no glyph for is put in the
            // actual text of the glyph before it. A font that has a glyph for
            // it draws the glyph, which has no width.
            int[] codePoints = new int[str.Length];
            int[] gids = new int[str.Length];
            bool[] mirrored = null;
            int[] joiners = null;
            bool[] runEdge = null;      // An LRM before the glyph at the index
            int n = 0;
            bool hasMarks = false;
            bool afterRLM = false;
            int i = 0;
            while (i < str.Length) {
                int codePoint = char.ConvertToUtf32(str, i);
                if (codePoint == 0x200F) {                  // RLM
                    afterRLM = true;
                } else if (codePoint == 0x200E) {           // LRM
                    // Bidi puts an LRM on each side of the brackets of a right
                    // to left pair around left to right text; the glyphs
                    // between the two are drawn in one span, see AppendRun.
                    if (runEdge == null) {
                        runEdge = new bool[str.Length + 1];
                    }
                    runEdge[n] = true;
                } else if ((codePoint == 0x200C || codePoint == 0x200D) && font.unicodeToGID[codePoint] == 0) {
                    // A ZWNJ or ZWJ the font has no glyph for goes with the
                    // glyph before it. One with nothing before it is left out.
                    if (n > 0) {
                        if (joiners == null) {
                            joiners = new int[str.Length];
                        }
                        joiners[n - 1] = codePoint;
                    }
                } else if (codePoint != 0xFEFF) {           // BOM
                    if (afterRLM && Bidi.Mirrored(codePoint).HasValue) {
                        if (mirrored == null) {
                            mirrored = new bool[str.Length];
                        }
                        mirrored[n] = true;
                    }
                    afterRLM = false;
                    codePoints[n] = codePoint;
                    gids[n] = GlyphOf(font, codePoint);
                    hasMarks |= IsMark(codePoint);
                    n++;
                }
                i += char.IsHighSurrogate(str[i]) ? 2 : 1;  // Proper surrogate handling
            }
            int[] offsets = null;
            if (hasMarks && font.markData != null) {
                FontStream1.ReadMarks(font);
            }
            if (hasMarks && font.markAnchors != null) {
                offsets = MarkOffsets(font, codePoints, gids, n);
            } else if (mirrored == null && joiners == null && runEdge == null) {
                for (int k = 0; k < n; k++) {
                    AppendCodePointAsHex(gids[k]);
                }
                return;
            }
            i = 0;
            while (i < n) {
                if (runEdge != null && runEdge[i]) {
                    int j = i + 1;
                    while (j < n && !runEdge[j]) {
                        j++;
                    }
                    AppendRun(font, codePoints, gids, offsets, i, j);
                    runEdge[j] = false;
                    i = j;
                    continue;
                }
                int end = i + 1;
                if (codePoints[i] != 0x20) {
                    while (end < n && codePoints[end] != 0x20 && (runEdge == null || !runEdge[end])) {
                        end++;
                    }
                }
                // A glyph with a joiner after it is drawn in a span of its own,
                // and so are the mirrored characters at the ends of a word with
                // moved marks, like its brackets: MuPDF leaves out or repeats
                // text when the glyphs of a span do not match its actual text
                // one by one.
                int k = i;
                while (k < end) {
                    int j = k;
                    while (j < end && (joiners == null || joiners[j] == 0)) {
                        j++;
                    }
                    AppendWord(font, codePoints, gids, mirrored, offsets, k, j);
                    if (j < end) {
                        AppendGlyphWithActualText(font, codePoints, gids, offsets, mirrored, joiners, j);
                    }
                    k = j + 1;
                }
                i = end;
            }
        }
    }

    // Draws the glyphs of a left to right run in brackets, nested in right to
    // left text, in a marked content span with the text of the run between
    // two left-to-right marks as its actual text. Poppler and MuPDF then keep
    // the brackets with the run when the text is copied; with the brackets in
    // spans of their own, or mirrored back like the other brackets, they come
    // out the wrong way round. No glyph stands in for the marks: MuPDF moves
    // the first bracket to the right to left text when one does.
    private void AppendRun(Font font, int[] codePoints, int[] gids, int[] offsets, int start, int end) {
        StringBuilder text = new StringBuilder("\u200E");
        for (int k = start; k < end; k++) {
            text.Append(TextOf(font, codePoints[k]));
        }
        text.Append("\u200E");
        Append("> Tj\n/Span <</ActualText <");
        Append(ToUTF16Hex(text.ToString()));
        Append(">>> BDC\n<");
        for (int k = start; k < end; k++) {
            if (offsets == null || (offsets[2*k] == 0 && offsets[2*k + 1] == 0)) {
                AppendCodePointAsHex(gids[k]);
            } else {
                AppendMovedGlyph(font, gids[k], offsets[2*k], offsets[2*k + 1]);
            }
        }
        Append("> Tj\nEMC\n<");
    }

    // Draws the glyphs of a word, or of the part of a word before a joiner,
    // in a marked content span with the text of the word when it has moved
    // marks, with the mirrored characters at its ends in spans of their own.
    private void AppendWord(
            Font font, int[] codePoints, int[] gids, bool[] mirrored, int[] offsets, int i, int end) {
        int wordStart = i;
        int wordEnd = end;
        while (mirrored != null && wordStart < wordEnd && mirrored[wordStart]) {
            wordStart++;
        }
        while (mirrored != null && wordEnd > wordStart && mirrored[wordEnd - 1]) {
            wordEnd--;
        }
        if (offsets != null && IsMoved(offsets, wordStart, wordEnd)) {
            AppendGlyphs(font, codePoints, gids, mirrored, i, wordStart);
            AppendWordWithMovedMarks(font, codePoints, gids, offsets, wordStart, wordEnd);
            AppendGlyphs(font, codePoints, gids, mirrored, wordEnd, end);
        } else {
            AppendGlyphs(font, codePoints, gids, mirrored, i, end);
        }
    }

    private void AppendGlyphs(
            Font font, int[] codePoints, int[] gids, bool[] mirrored, int start, int end) {
        for (int k = start; k < end; k++) {
            if (mirrored != null && mirrored[k]) {
                AppendGlyphWithActualText(font, codePoints, gids, null, mirrored, null, k);
            } else {
                AppendCodePointAsHex(gids[k]);
            }
        }
    }

    // Draws a glyph in a marked content span that has the text it stands for
    // as its actual text: the character Bidi mirrored, since text extraction
    // reverses right to left text but does not mirror the brackets back, or
    // the text of the glyph followed by the zero width non-joiner or joiner
    // after it, which the font has no glyph for. A space glyph that takes no
    // room stands in for the joiner, so that the span has a glyph for each
    // character of its actual text but the last: MuPDF pairs the characters
    // with the glyphs, and repeats text when a glyph matches its character
    // after one that does not. Poppler takes the actual text as it is.
    private void AppendGlyphWithActualText(
            Font font, int[] codePoints, int[] gids, int[] offsets, bool[] mirrored, int[] joiners, int k) {
        int codePoint = codePoints[k];
        int joiner = (joiners != null) ? joiners[k] : 0;
        StringBuilder text = new StringBuilder();
        if (mirrored != null && mirrored[k]) {
            text.Append(char.ConvertFromUtf32(Bidi.Mirrored(codePoint).Value));
        } else {
            text.Append(TextOf(font, codePoint));
        }
        if (joiner != 0) {
            text.Append(char.ConvertFromUtf32(joiner));
        }
        Append("> Tj\n/Span <</ActualText <");
        Append(ToUTF16Hex(text.ToString()));
        Append(">>> BDC\n");
        if (offsets != null && (offsets[2*k] != 0 || offsets[2*k + 1] != 0)) {
            Append("<");
            AppendMovedGlyph(font, gids[k], offsets[2*k], offsets[2*k + 1]);
            Append("> Tj\n");
        } else {
            Append("<");
            AppendCodePointAsHex(gids[k]);
            Append("> Tj\n");
        }
        if (joiner != 0) {
            int space = font.unicodeToGID[0x0020];
            Append("[<");
            AppendCodePointAsHex(space);
            Append("> ");
            Append(1000f * font.GlyphAdvance(space) / font.unitsPerEm);
            Append("] TJ\n");
        }
        Append("EMC\n<");
    }

    // Returns the text the glyph of the code point maps to: a glyph missing
    // from the font is a space, and an Arabic letter form is its letter.
    private static String TextOf(Font font, int codePoint) {
        String letters = Bidi.LettersOf(codePoint);
        if (codePoint < font.firstChar || codePoint > font.lastChar) {
            return " ";
        } else if (letters != null) {
            return letters;
        }
        return char.ConvertFromUtf32(codePoint);
    }

    private static int GlyphOf(Font font, int codePoint) {
        if (codePoint < font.firstChar || codePoint > font.lastChar) {
            return font.unicodeToGID[0x0020];               // Space fallback
        }
        return font.unicodeToGID[codePoint];
    }

    // Returns the offsets that move the marks to where the GPOS table of the
    // font puts them, dx and dy in font units for each glyph. A mark goes on
    // the letter or ligature before it. Right to left text is drawn in visual
    // order, with the marks before their letter, so a Hebrew or Arabic mark
    // goes on the letter after it. A mark that attaches to the mark before it,
    // like a Thai tone mark above an upper vowel, goes on that mark instead.
    // The marks of a letter are taken in logical order, sorted as HarfBuzz
    // sorts them, so a fatha goes above a shadda whichever was typed first.
    private static int[] MarkOffsets(Font font, int[] codePoints, int[] gids, int n) {
        // Where each glyph is drawn before it is moved, in font units.
        int[] x = new int[n];
        for (int i = 1; i < n; i++) {
            x[i] = x[i - 1] + font.GlyphAdvance(gids[i - 1]);
        }
        int[] offsets = new int[2*n];
        for (int i = 0; i < n; i++) {
            if (!IsMark(codePoints[i])) {
                continue;
            }
            int step = IsRightToLeft(codePoints[i]) ? 1 : -1;
            int baseIndex = i + step;
            while (baseIndex >= 0 && baseIndex < n && IsMark(codePoints[baseIndex])) {
                baseIndex += step;
            }
            if (baseIndex >= 0 && baseIndex < n) {
                int[] offset = MarkToBaseOffset(font, codePoints[baseIndex], gids[baseIndex], gids[i]);
                if (offset != null) {
                    offsets[2*i] = x[baseIndex] + offset[0] - x[i];
                    offsets[2*i + 1] = offset[1];
                }
            }
        }
        int[] order = new int[n];
        int start = 0;
        while (start < n) {
            if (!IsMark(codePoints[start])) {
                start++;
                continue;
            }
            // The marks from start to end - 1 go on the same letter.
            bool rightToLeft = IsRightToLeft(codePoints[start]);
            int end = start + 1;
            while (end < n && IsMark(codePoints[end]) && IsRightToLeft(codePoints[end]) == rightToLeft) {
                end++;
            }
            int count = end - start;
            for (int k = 0; k < count; k++) {
                int mark = rightToLeft ? end - 1 - k : start + k;
                // Moves the mark back past the marks that sort after it. A mark
                // of order 0 is never moved, and no mark moves past it.
                int markOrder = MarkOrder(codePoints[mark]);
                int l = k - 1;
                while (markOrder != 0 && l >= 0 && MarkOrder(codePoints[order[l]]) > markOrder) {
                    order[l + 1] = order[l];
                    l--;
                }
                order[l + 1] = mark;
            }
            for (int k = 1; k < count; k++) {
                PlaceOnMark(font, gids, x, offsets, order[k], order[k - 1]);
            }
            start = end;
        }
        return offsets;
    }

    // Returns the offset of the mark from the letter or ligature it goes on, in
    // font units, or null. A font can have the anchors of an isolated Arabic
    // letter form only for its letter, which looks the same.
    private static int[] MarkToBaseOffset(Font font, int baseCodePoint, int baseGID, int markGID) {
        int[] offset = AnchorOffset(font, baseGID, markGID);
        if (offset == null) {
            int letter = Bidi.LetterOfIsolatedForm(baseCodePoint);
            if (letter != 0) {
                offset = AnchorOffset(font, font.unicodeToGID[letter], markGID);
            }
        }
        return offset;
    }

    // Returns the offset of the mark from the glyph it goes on, from the first
    // MarkToBase or MarkToLigature subtable that has an anchor for both, or null.
    private static int[] AnchorOffset(Font font, int baseGID, int markGID) {
        for (int i = 0; i < font.markAnchors.Count; i++) {
            int[] mark;
            if (!font.markAnchors[i].TryGetValue(markGID, out mark)) {
                continue;
            }
            int[] anchors;
            font.baseAnchors[i].TryGetValue(baseGID, out anchors);
            int c = 3*mark[0];
            if (anchors != null && c + 2 < anchors.Length && anchors[c] == 1) {
                return new int[] {anchors[c + 1] - mark[1], anchors[c + 2] - mark[2]};
            }
        }
        return null;
    }

    // Moves the mark onto the other mark, if the font has an offset for the two.
    private static void PlaceOnMark(
            Font font, int[] gids, int[] x, int[] offsets, int mark, int other) {
        int[] offset;
        if (font.markToMarkOffsets.TryGetValue((gids[other] << 16) | gids[mark], out offset)) {
            offsets[2*mark] = offsets[2*other] + x[other] + offset[0] - x[mark];
            offsets[2*mark + 1] = offsets[2*other + 1] + offset[1];
        }
    }

    // Returns true if the text has a character that is more than a glyph: an
    // RLM, LRM, ZWNJ or ZWJ, or a mark that the GPOS table of the font puts in
    // place. Vectorized scans settle text before U+0300, where there is
    // neither, and Greek and Cyrillic text; from there each character is one
    // bit of a table, so that CJK text costs a load and a test for each.
    private static bool NeedsShaping(Font font, string str) {
        ReadOnlySpan<char> span = str.AsSpan();
        if (font.markAnchors == null && font.markData == null) {
            return span.IndexOfAnyInRange('\u200C', '\u200F') >= 0;
        }
        int start = span.IndexOfAnyInRange('\u0300', '\uFFFF');
        if (start < 0) {
            return false;
        }
        span = span.Slice(start);
        if (span.IndexOfAnyExceptInRange('\u0000', '\u04FF') < 0) {
            // Latin, Greek and Cyrillic, whose only marks are the combining
            // diacritical marks and the Cyrillic ones: three vectorized scans.
            return span.IndexOfAnyInRange('\u0300', '\u036F') >= 0 ||
                    span.IndexOfAnyInRange('\u0483', '\u0489') >= 0;
        }
        byte[] table = shapingChars;
        for (int i = start; i < str.Length; i++) {
            char c = str[i];
            if ((table[c >> 3] & (1 << (c & 7))) == 0) {
                continue;
            }
            if (!char.IsHighSurrogate(c)) {
                return true;    // A joiner or a mark
            }
            // A pair of surrogates is one code point outside the plane, which
            // is looked up; either half alone is a surrogate, never a mark.
            if (i + 1 < str.Length && char.IsLowSurrogate(str[i + 1]) &&
                    IsMark(char.ConvertToUtf32(c, str[i + 1]))) {
                return true;
            }
        }
        return false;
    }

    // One bit for each character of the Basic Multilingual Plane that can
    // make text need shaping: the joiners, the marks and the high surrogates,
    // each of which starts a character outside the plane.
    private static readonly byte[] shapingChars = ShapingChars();

    private static byte[] ShapingChars() {
        byte[] table = new byte[8192];
        for (int c = 0x0300; c < 0x10000; c++) {
            bool set = (c >= 0x200C && c <= 0x200F) || (c >= 0xD800 && c <= 0xDBFF);
            if (!set && (c < 0xD800 || c > 0xDFFF)) {
                System.Globalization.UnicodeCategory category =
                        System.Globalization.CharUnicodeInfo.GetUnicodeCategory(c);
                set = category == System.Globalization.UnicodeCategory.NonSpacingMark ||
                        category == System.Globalization.UnicodeCategory.EnclosingMark;
            }
            if (set) {
                table[c >> 3] |= (byte) (1 << (c & 7));
            }
        }
        return table;
    }

    private static bool IsMark(int codePoint) {
        // The category of a code point, without a string made for it.
        System.Globalization.UnicodeCategory category =
                System.Globalization.CharUnicodeInfo.GetUnicodeCategory(codePoint);
        return category == System.Globalization.UnicodeCategory.NonSpacingMark ||
                category == System.Globalization.UnicodeCategory.EnclosingMark;
    }

    // Returns true if the character is in the blocks of the right to left scripts.
    private static bool IsRightToLeft(int codePoint) {
        return (codePoint >= 0x0590 && codePoint <= 0x08FF) ||
                (codePoint >= 0xFB1D && codePoint <= 0xFDFF) ||
                (codePoint >= 0xFE70 && codePoint <= 0xFEFF) ||
                (codePoint >= 0x10800 && codePoint <= 0x10FFF) ||
                (codePoint >= 0x1E800 && codePoint <= 0x1EFFF);
    }

    // Returns the order of a mark among the marks of its letter, or 0 for a
    // mark that stays where it is: the canonical combining class of the mark,
    // changed as in HarfBuzz to put the Hebrew points in the order of the SBL
    // Hebrew manual, and the Arabic shadda before the other Arabic vowel marks.
    private static int MarkOrder(int codePoint) {
        if (codePoint >= 0x05B0 && codePoint <= 0x05C7) {   // Hebrew points
            return hebrewMarkOrder[codePoint - 0x05B0];
        }
        if (codePoint >= 0x064B && codePoint <= 0x0652) {   // Arabic fathatan to sukun
            return arabicMarkOrder[codePoint - 0x064B];
        }
        switch (codePoint) {
            case 0x0670: return 35;     // ARABIC LETTER SUPERSCRIPT ALEF
            case 0x0E38: case 0x0E39: return 103;   // THAI SARA U, SARA UU
            case 0x0E3A: return 9;      // THAI PHINTHU
            case 0x0E48: case 0x0E49: case 0x0E4A: case 0x0E4B: return 107;  // THAI tone marks
            case 0x0EB8: case 0x0EB9: return 118;   // LAO VOWEL SIGN U, UU
            case 0x0EC8: case 0x0EC9: case 0x0ECA: case 0x0ECB: return 122;  // LAO tone marks
            default: return 0;
        }
    }

    private static readonly int[] hebrewMarkOrder = {
        22, 15, 16, 17, 23, 18, 19, 20,     // U+05B0 sheva to U+05B7 patah
        21, 14, 14, 24, 12, 25, 0, 13,      // U+05B8 qamats to U+05BF rafe
        0, 10, 11, 0, 230, 220, 0, 21       // U+05C0 to U+05C7 qamats qatan
    };

    private static readonly int[] arabicMarkOrder = {
        28, 29, 30, 31, 32, 33, 27, 34      // U+064B fathatan to U+0652 sukun
    };

    private static bool IsMoved(int[] offsets, int start, int end) {
        for (int k = start; k < end; k++) {
            if (offsets[2*k] != 0 || offsets[2*k + 1] != 0) {
                return true;
            }
        }
        return false;
    }

    // Draws a word with moved marks in a marked content span that has the text
    // of the word as its actual text. Text extraction would otherwise take a
    // moved mark for text above or below the line, and break the word there.
    // Poppler puts the actual text where the first glyph of the span is drawn,
    // so a word that starts with a moved mark, like a Hebrew or Arabic word
    // drawn in visual order, starts with a space drawn back over the space
    // before it, and its actual text starts with a space. MuPDF takes the
    // glyphs that match the actual text as they are, and leaves out a space
    // drawn over a space.
    private void AppendWordWithMovedMarks(
            Font font, int[] codePoints, int[] gids, int[] offsets, int start, int end) {
        bool leadingSpace = IsMoved(offsets, start, start + 1);
        StringBuilder text = new StringBuilder(leadingSpace ? " " : "");
        for (int k = start; k < end; k++) {
            text.Append(TextOf(font, codePoints[k]));     // The text the glyphs map to
        }
        Append("> Tj\n/Span <</ActualText <");
        Append(ToUTF16Hex(text.ToString()));
        Append(">>> BDC\n");
        if (leadingSpace) {
            int space = font.unicodeToGID[0x0020];
            Append("[");
            Append(1000f * font.GlyphAdvance(space) / font.unitsPerEm);
            Append(" <");
            AppendCodePointAsHex(space);
            Append(">] TJ\n");
        }
        Append("<");
        for (int k = start; k < end; k++) {
            if (offsets[2*k] == 0 && offsets[2*k + 1] == 0) {
                AppendCodePointAsHex(gids[k]);
            } else {
                AppendMovedGlyph(font, gids[k], offsets[2*k], offsets[2*k + 1]);
            }
        }
        Append("> Tj\nEMC\n<");
    }

    // Ends the string of glyphs, draws the glyph moved by dx and dy font units,
    // and starts the string again.
    private void AppendMovedGlyph(Font font, int gid, int dx, int dy) {
        float fontSize = (textFontSize != 0f) ? textFontSize : font.size;
        Append("> Tj\n");
        Append(textRise + dy * fontSize / font.unitsPerEm);
        Append(" Ts\n");
        if (dx == 0) {
            Append("<");
            AppendCodePointAsHex(gid);
            Append("> Tj\n");
        } else {
            float adjustment = 1000f * dx / font.unitsPerEm;
            Append("[");
            Append(-adjustment);
            Append(" <");
            AppendCodePointAsHex(gid);
            Append("> ");
            Append(adjustment);
            Append("] TJ\n");
        }
        Append(textRise);
        Append(" Ts\n<");
    }

    /// <summary>
    /// Sets the brush color.
    /// </summary>
    /// <param name="color">the color. See the Color class for predefined values or define your own using 0x00RRGGBB packed integers.</param>
    /// <returns>this Page object.</returns>
    public Page SetBrushColor(int color) {
        float r = ((color >> 16) & 0xff)/255f;
        float g = ((color >>  8) & 0xff)/255f;
        float b = ((color)       & 0xff)/255f;
        if (brushColorWritten && brushColor[0] == r && brushColor[1] == g && brushColor[2] == b) {
            return this;    // The content is drawn with this color already
        }
        return SetBrushColor(new float[] {r, g, b});
    }

    // Returns the components of a 0xRRGGBB color in an array that the next call fills again.
    internal float[] PackedToRGB(int color) {
        packedRGB[0] = ((color >> 16) & 0xff)/255f;
        packedRGB[1] = ((color >>  8) & 0xff)/255f;
        packedRGB[2] = ((color)       & 0xff)/255f;
        return packedRGB;
    }

    /// <summary>
    /// Sets the color for brush operations.
    /// </summary>
    /// <param name="rgbColor">the color.</param>
    /// <returns>this Page object.</returns>
    public Page SetBrushColor(float[] rgbColor) {
        if (rgbColor == null) {
            return this; // Early exit if null
        }

        if (rgbColor[0] < 0f || rgbColor[0] > 1f ||
            rgbColor[1] < 0f || rgbColor[1] > 1f ||
            rgbColor[2] < 0f || rgbColor[2] > 1f) {
            Console.Error.WriteLine("Warning: RGB color values must be between 0f and 1f. Ignoring request.");
            return this; // Early exit if out of range
        }

        // Now set the brush color, to a copy that the caller cannot change
        if (brushColorWritten && brushColor[0] == rgbColor[0] && brushColor[1] == rgbColor[1] && brushColor[2] == rgbColor[2]) {
            return this;    // The content is drawn with this color already
        }
        this.brushColor = (float[]) rgbColor.Clone();
        brushColorWritten = true;

        // Proceed with setting the color (example)
        Append(rgbColor[0]);
        Append(Token.Space);
        Append(rgbColor[1]);
        Append(Token.Space);
        Append(rgbColor[2]);
        Append(" rg\n");
        return this;
    }

    /// <summary>
    /// Returns a copy of the current brush color as an RGB float array.
    /// </summary>
    /// <returns>
    /// A <c>float[]</c> containing the red, green, and blue components (0.0f to 1.0f) of the brush color.
    /// </returns>
    public float[] GetBrushColor() {
        return (float[]) brushColor.Clone();
    }

    /// <summary>
    /// Sets the pen color using a packed 0x00RRGGBB integer value.
    /// </summary>
    /// <param name="color">
    /// The color value, where each component (red, green, blue) is packed into a 24-bit integer.
    /// You can use predefined colors from the <see cref="Color"/> class or define your own.
    /// </param>
    public Page SetPenColor(int color) {
        float r = ((color >> 16) & 0xff)/255f;
        float g = ((color >>  8) & 0xff)/255f;
        float b = ((color)       & 0xff)/255f;
        if (penColorWritten && penColor[0] == r && penColor[1] == g && penColor[2] == b) {
            return this;    // The content is drawn with this color already
        }
        return SetPenColor(new float[] {r, g, b});
    }

    /// <summary>
    /// Sets the pen color using an RGB color array.
    /// </summary>
    /// <param name="rgbColor">
    /// An array of three <see cref="float"/> values for red, green, and blue components (0.0f to 1.0f).
    /// </param>
    /// <remarks>
    /// Logs a warning and does not set the color if the values are out of range or the array is null.
    /// </remarks>
    public Page SetPenColor(float[] rgbColor) {
        if (rgbColor == null) {
            return this; // Early exit if null
        }

        if (rgbColor[0] < 0f || rgbColor[0] > 1f ||
            rgbColor[1] < 0f || rgbColor[1] > 1f ||
            rgbColor[2] < 0f || rgbColor[2] > 1f) {
            Console.Error.WriteLine("Warning: RGB color values must be between 0f and 1f. Ignoring request.");
            return this; // Early exit if out of range
        }

        // Now set the pen color, to a copy that the caller cannot change
        if (penColorWritten && penColor[0] == rgbColor[0] && penColor[1] == rgbColor[1] && penColor[2] == rgbColor[2]) {
            return this;    // The content is drawn with this color already
        }
        this.penColor = (float[]) rgbColor.Clone();
        penColorWritten = true;

        // Proceed with setting the color (example)
        Append(rgbColor[0]);
        Append(Token.Space);
        Append(rgbColor[1]);
        Append(Token.Space);
        Append(rgbColor[2]);
        Append(" RG\n");
        return this;
    }

    /// <summary>
    /// Gets a copy of the current pen color as an RGB float array.
    /// </summary>
    /// <returns>
    /// A <c>float[]</c> with three elements: red, green, and blue components (0.0f to 1.0f).
    /// </returns>
    public float[] GetPenColor() {
        return (float[]) penColor.Clone();
    }

    /// <summary>
    /// Sets the color for brush operations using CMYK.
    /// This is the color used when drawing regular text and filling shapes.
    /// </summary>
    /// <param name="c">the cyan component is float value from 0.0f to 1.0f.</param>
    /// <param name="m">the magenta component is float value from 0.0f to 1.0f.</param>
    /// <param name="y">the yellow component is float value from 0.0f to 1.0f.</param>
    /// <param name="k">the black component is float value from 0.0f to 1.0f.</param>
    /// <returns>this Page object.</returns>
    public Page SetBrushColorCMYK(float c, float m, float y, float k) {
        Append(c);
        Append(' ');
        Append(m);
        Append(' ');
        Append(y);
        Append(' ');
        Append(k);
        Append(" k\n");
        brushColor = CmykToRGB(c, m, y, k);
        brushColorWritten = false;
        return this;
    }

    /// <summary>
    /// Sets the color for stroking operations using CMYK.
    /// The pen color is used when drawing lines and splines.
    /// </summary>
    /// <param name="c">the cyan component is float value from 0.0f to 1.0f.</param>
    /// <param name="m">the magenta component is float value from 0.0f to 1.0f.</param>
    /// <param name="y">the yellow component is float value from 0.0f to 1.0f.</param>
    /// <param name="k">the black component is float value from 0.0f to 1.0f.</param>
    /// <returns>this Page object.</returns>
    public Page SetPenColorCMYK(float c, float m, float y, float k) {
        Append(c);
        Append(' ');
        Append(m);
        Append(' ');
        Append(y);
        Append(' ');
        Append(k);
        Append(" K\n");
        penColor = CmykToRGB(c, m, y, k);
        penColorWritten = false;
        return this;
    }

    // Returns a CMYK color converted to RGB the way the PDF specification
    // converts DeviceCMYK to DeviceRGB, for GetPenColor and GetBrushColor.
    private static float[] CmykToRGB(float c, float m, float y, float k) {
        return new float[] {
                1f - Math.Min(1f, c + k),
                1f - Math.Min(1f, m + k),
                1f - Math.Min(1f, y + k)};
    }

    /// <summary>
    /// Sets the line width to the default.
    /// The default is the finest line width.
    /// </summary>
    /// <returns>this Page object.</returns>
    public Page SetDefaultPenWidth() {
        return SetPenWidth(0f);
    }

    /// <summary>
    /// The stroke dash pattern controls the pattern of dashes and gaps used to stroke paths.
    /// It is specified by a dash array and a dash phase.
    /// The elements of the dash array are positive numbers that specify the lengths of
    /// alternating dashes and gaps.
    /// The dash phase specifies the distance into the dash pattern at which to start the dash.
    /// The elements of both the dash array and the dash phase are expressed in user space units.
    /// <code>
    /// Examples of line dash patterns:
    ///
    ///     "[Array] Phase"     Appearance          Description
    ///     _______________     _________________   ____________________________________
    ///
    ///     "[] 0"              -----------------   Solid line
    ///     "[3] 0"             ---   ---   ---     3 units on, 3 units off, ...
    ///     "[2] 1"             -  --  --  --  --   1 on, 2 off, 2 on, 2 off, ...
    ///     "[2 1] 0"           -- -- -- -- -- --   2 on, 1 off, 2 on, 1 off, ...
    ///     "[3 5] 6"             ---     ---       2 off, 3 on, 5 off, 3 on, 5 off, ...
    ///     "[2 3] 11"          -   --   --   --    1 on, 3 off, 2 on, 3 off, 2 on, ...
    /// </code>
    /// </summary>
    /// <param name="strokeDashPattern">the stroke dash pattern.</param>
    /// <returns>this Page object.</returns>
    public Page SetStrokeDashPattern(String strokeDashPattern) {
        if (!IsDashPattern(strokeDashPattern)) {
            pdf.Fail(new ArgumentException("The dash pattern \"" + strokeDashPattern
                    + "\" is not an array of non-negative numbers, not all zero, "
                    + "followed by a phase, such as \"[3 3] 0\"."));
        }
        this.strokeDashPattern = strokeDashPattern;
        Append(strokeDashPattern);
        Append(" d\n");
        return this;
    }

    private const String WS = "[ \\t\\n\\f\\r\\v]";
    private const String NUMBER = "[+-]?(?:[0-9]+(?:\\.[0-9]*)?|\\.[0-9]+)";
    private static readonly System.Text.RegularExpressions.Regex DASH_PATTERN =
            new System.Text.RegularExpressions.Regex(
                    "^" + WS + "*\\[" + WS + "*(" + NUMBER + "(?:" + WS + "+" + NUMBER + ")*)?" + WS + "*\\]" + WS + "*" + NUMBER + WS + "*\\z");

    // Returns true for a dash array of non-negative numbers, not all zero, and a
    // phase, like "[3 3] 0"; "[] 0" is a solid line.
    internal static bool IsDashPattern(String pattern) {
        if (pattern == null) {
            return false;
        }
        System.Text.RegularExpressions.Match m = DASH_PATTERN.Match(pattern);
        if (!m.Success) {
            return false;
        }
        String lengths = m.Groups[1].Value.Trim();
        if (lengths.Length == 0) {
            return true;
        }
        bool notAllZero = false;
        foreach (String length in System.Text.RegularExpressions.Regex.Split(lengths, WS + "+")) {
            if (length.StartsWith("-")) {
                return false;
            }
            if (Double.Parse(length, System.Globalization.CultureInfo.InvariantCulture) != 0.0) {
                notAllZero = true;
            }
        }
        return notAllZero;
    }

    /// <summary>
    /// Sets the default stroke pattern to be solid line or curve.
    /// </summary>
    /// <returns>this Page object.</returns>
    public Page SetDefaultStrokeDashPattern() {
        this.strokeDashPattern = "[] 0";
        Append(strokeDashPattern);
        Append(" d\n");
        return this;
    }

    /// <summary>
    /// Sets the pen width that will be used to draw lines and splines on this page.
    /// </summary>
    /// <param name="width">the pen width.</param>
    /// <returns>this Page object.</returns>
    public Page SetPenWidth(float width) {
        if (width < 0f) {
            pdf.Fail(new ArgumentException("The pen width cannot be negative."));
        }
        if (penWidthWritten && width == this.penWidth) {
            return this;    // The content is drawn with this width already
        }
        this.penWidth = width;
        penWidthWritten = true;
        Append(width);
        Append(" w\n");
        return this;
    }

    /// <summary>Returns the current pen width.</summary>
    public float GetPenWidth() {
        return this.penWidth;
    }

    /// <summary>
    /// Sets the current line cap style.
    /// </summary>
    /// <param name="style">the cap style of the current line.
    /// Supported values: CapStyle.BUTT, CapStyle.ROUND and CapStyle.PROJECTING_SQUARE</param>
    /// <returns>this Page object.</returns>
    public Page SetLineCapStyle(CapStyle style) {
        this.lineCapStyle = style;
        Append((Int32) style);
        Append(" J\n");
        return this;
    }

    /// <summary>
    /// Sets the line join style.
    /// </summary>
    /// <param name="style">the line join style code.
    /// Supported values: JoinStyle.MITER, JoinStyle.ROUND and JoinStyle.BEVEL</param>
    /// <returns>this Page object.</returns>
    public Page SetLineJoinStyle(JoinStyle style) {
        this.lineJoinStyle = style;
        Append((Int32) style);
        Append(" j\n");
        return this;
    }

    /// <summary>
    /// Moves the pen to the absolute point (x, y) on the page.
    /// </summary>
    /// <param name="x">Horizontal coordinate of the new position.</param>
    /// <param name="y">Vertical coordinate (before Y‑axis flip).</param>
    /// <remarks>
    /// Converts Y to <c>height - y</c> because the renderer uses a top‑left origin.
    /// Appends a move‑to command in the form “x y m\n”.
    /// </remarks>
    public void MoveTo(float x, float y) {
        Append(x);
        Append(' ');
        Append(height - y);
        Append(" m\n");
    }

    /// <summary>
    /// Draws a line from the current pen position to the point with coordinates (x, y),
    /// </summary>
    /// <param name="x">The horizontal offset for the line endpoint.</param>
    /// <param name="y">The vertical offset for the line endpoint (before Y‑axis inversion).</param>
    public void LineTo(float x, float y) {
        Append(x);
        Append(' ');
        Append(height - y);
        Append(" l\n");
    }

    /// <summary>
    /// Strokes the current path using the active pen color.
    /// </summary>
    public void StrokePath() {
        Append("S\n");
    }

    /// <summary>
    /// Closes the current path and then strokes it using the active pen color.
    /// </summary>
    public void ClosePath() {
        Append("s\n");
    }

    /// <summary>
    /// Closes the current path and fills it using the active brush color.
    /// </summary>
    public void FillPath() {
        Append("f\n");
    }

    /// <summary>
    /// Draws the outline of a rectangle using the current pen color.
    /// </summary>
    /// <param name="x">X‑coordinate of the top left corner.</param>
    /// <param name="y">Y‑coordinate of the top left corner.</param>
    /// <param name="w">Rectangle width.</param>
    /// <param name="h">Rectangle height.</param>
    public void DrawRect(float x, float y, float w, float h) {
        MoveTo(x, y);
        LineTo(x + w, y);
        LineTo(x + w, y + h);
        LineTo(x, y + h);
        ClosePath();          // close and stroke
    }

    /// <summary>
    /// Fills a rectangle using the current brush color.
    /// </summary>
    /// <param name="x">X‑coordinate of the top left corner.</param>
    /// <param name="y">Y‑coordinate of the top left corner.</param>
    /// <param name="w">Rectangle width.</param>
    /// <param name="h">Rectangle height.</param>
    public void FillRect(float x, float y, float w, float h) {
        FillRectBetween(x, y, x + w, y + h);
    }

    // Fills the rectangle between the corners (x1, y1) and (x2, y2), for a
    // caller that has the corners, so its edges are not rounded from a width
    // and a height.
    internal void FillRectBetween(float x1, float y1, float x2, float y2) {
        float left = x1;
        float right = x2;
        float top = height - y1;
        float bottom = height - y2;
        if (Math.Abs(left) < 100000f && Math.Abs(right) < 100000f
                && Math.Abs(top) < 100000f && Math.Abs(bottom) < 100000f) {
            // One re operator, where four path operators drew the rectangle.
            // The corners are rounded as the path wrote them, and the width
            // and the height are their differences, so the edges stay where
            // the path put them; below 100000 a float holds hundredths
            // closely enough to be written back as the same hundredths.
            int xLeft = FastFloat.ToHundredths(left);
            int yBottom = FastFloat.ToHundredths(bottom);
            Append(xLeft / 100f);
            Append(' ');
            Append(yBottom / 100f);
            Append(' ');
            Append((FastFloat.ToHundredths(right) - xLeft) / 100f);
            Append(' ');
            Append((FastFloat.ToHundredths(top) - yBottom) / 100f);
            Append(" re\nf\n");
        } else {
            // Outside the page by far, or NaN, which MoveTo refuses.
            MoveTo(x1, y1);
            LineTo(x2, y1);
            LineTo(x2, y2);
            LineTo(x1, y2);
            FillPath();
        }
    }

    /// <summary>
    /// Draws a path consisting of multiple points using the specified path operator.
    /// Supports both straight line segments and Bézier curve segments with control points.
    /// </summary>
    /// <param name="path">The list of points defining the path. The first point sets the starting position,
    /// subsequent points define line segments or curve control points. Fewer than two points paint nothing.</param>
    /// <param name="pathOperator">The path painting operator to apply, for example PathOperator.STROKE or PathOperator.FILL.</param>
    /// <remarks>
    /// <para>
    /// The method processes points as follows:
    /// <list type="bullet">
    /// <item>Starts with MoveTo() to position at the first point</item>
    /// <item>For each subsequent point:
    /// <list type="bullet">
    /// <item>If the point has a controlPoint set: stores it for curve definition</item>
    /// <item>If the point has no controlPoint:
    /// <list type="bullet">
    /// <item>If previous point had controlPoint: completes curve definition</item>
    /// <item>Otherwise: creates straight line segment using LineTo()</item>
    /// </list>
    /// </item>
    /// </list>
    /// </item>
    /// <item>Applies the specified path operator at the end</item>
    /// </list>
    /// </para>
    /// <para>
    /// This method supports both simple polygonal paths and complex paths with Bézier curves.
    /// Control points should have their controlPoint property set to indicate curve type
    /// (e.g., 'C' for cubic Bézier, 'Q' for quadratic Bézier).
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// // Drawing a rectangle with straight lines
    /// List&lt;Point&gt; rect = new List&lt;Point&gt; {
    ///     new Point(100, 100),
    ///     new Point(200, 100),
    ///     new Point(200, 200),
    ///     new Point(100, 200)
    /// };
    /// DrawPath(rect, PathOperator.STROKE);
    /// </code>
    ///
    /// <code>
    /// // Drawing a cubic Bézier curve
    /// List&lt;Point&gt; curve = new List&lt;Point&gt; {
    ///     new Point(100, 100),           // Start point
    ///     new Point(150, 50, Point.CONTROL_POINT_C),    // First control point
    ///     new Point(250, 150, Point.CONTROL_POINT_C),   // Second control point
    ///     new Point(300, 100)            // End point
    /// };
    /// DrawPath(curve, PathOperator.STROKE);
    /// </code>
    /// </example>
    /// <seealso cref="Point"/>
    /// <seealso cref="PathOperator"/>
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
        Append(pathOperator.ToOperator());
        Append('\n');
    }

    /// <summary>
    /// Draws an ellipse on the page using the current pen color.
    /// </summary>
    /// <param name="x">the x coordinate of the center of the ellipse to be drawn.</param>
    /// <param name="y">the y coordinate of the center of the ellipse to be drawn.</param>
    /// <param name="r1">the horizontal radius of the ellipse to be drawn.</param>
    /// <param name="r2">the vertical radius of the ellipse to be drawn.</param>
    public void DrawEllipse(
            float x,
            float y,
            float r1,
            float r2) {
        DrawEllipse(x, y, r1, r2, PathOperator.STROKE);
    }

    /// <summary>
    /// Draws a circle on the page.
    /// The outline of the circle is drawn using the current pen color.
    /// </summary>
    /// <param name="x">the x coordinate of the center of the circle to be drawn.</param>
    /// <param name="y">the y coordinate of the center of the circle to be drawn.</param>
    /// <param name="r">the radius of the circle to be drawn.</param>
    public void DrawCircle(float x, float y, float r) {
        DrawEllipse(x, y, r, r, PathOperator.STROKE);
    }

    /// <summary>
    /// Fills an ellipse on the page using the current brush color.
    /// </summary>
    /// <param name="x">the x coordinate of the center of the ellipse to be drawn.</param>
    /// <param name="y">the y coordinate of the center of the ellipse to be drawn.</param>
    /// <param name="r1">the horizontal radius of the ellipse to be drawn.</param>
    /// <param name="r2">the vertical radius of the ellipse to be drawn.</param>
    public void FillEllipse(float x, float y, float r1, float r2) {
        DrawEllipse(x, y, r1, r2, PathOperator.FILL);
    }

    /// <summary>
    /// Fills a circle on the page using the current brush color.
    /// </summary>
    /// <param name="x">the x coordinate of the center of the circle to be filled.</param>
    /// <param name="y">the y coordinate of the center of the circle to be filled.</param>
    /// <param name="r">the radius of the circle to be filled.</param>
    public void FillCircle(float x, float y, float r) {
        DrawEllipse(x, y, r, r, PathOperator.FILL);
    }

    /// <summary>
    /// Draws an ellipse on the page and fills it using the current brush color.
    /// </summary>
    /// <param name="x">the x coordinate of the center of the ellipse to be drawn.</param>
    /// <param name="y">the y coordinate of the center of the ellipse to be drawn.</param>
    /// <param name="r1">the horizontal radius of the ellipse to be drawn.</param>
    /// <param name="r2">the vertical radius of the ellipse to be drawn.</param>
    /// <param name="pathOperator">the path operator.</param>
    internal void DrawEllipse(
            float x,
            float y,
            float r1,
            float r2,
            PathOperator pathOperator) {
        // The best 4-spline magic number
        float m4 = 0.55228f;
        // Starting point
        MoveTo(x, y - r2);

        AppendPointXY(x + m4*r1, y - r2);
        AppendPointXY(x + r1, y - m4*r2);
        AppendPointXY(x + r1, y);
        Append("c\n");

        AppendPointXY(x + r1, y + m4*r2);
        AppendPointXY(x + m4*r1, y + r2);
        AppendPointXY(x, y + r2);
        Append("c\n");

        AppendPointXY(x - m4*r1, y + r2);
        AppendPointXY(x - r1, y + m4*r2);
        AppendPointXY(x - r1, y);
        Append("c\n");

        AppendPointXY(x - r1, y - m4*r2);
        AppendPointXY(x - m4*r1, y - r2);
        AppendPointXY(x, y - r2);
        Append("c\n");

        Append(pathOperator.ToOperator());
        Append('\n');
    }

    /// <summary>
    /// Draws a point on the page using the current pen color.
    /// </summary>
    /// <param name="p">the point.</param>
    public void DrawPoint(Point p) {
        if (p.shape != Shape.INVISIBLE) {
            List<Point> list;
            if (p.shape == Shape.CIRCLE) {
                DrawEllipse(p.x, p.y, p.r, p.r, p.GetPathOperator());
            } else if (p.shape == Shape.DIAMOND) {
                list = new List<Point>();
                list.Add(new Point(p.x, (float) (p.y - p.r*1.2)));
                list.Add(new Point((float) (p.x + p.r*1.2), p.y));
                list.Add(new Point(p.x, (float) (p.y + p.r*1.2)));
                list.Add(new Point((float) (p.x - p.r*1.2), p.y));
                DrawPath(list, p.GetPathOperator());
            } else if (p.shape == Shape.BOX) {
                list = new List<Point>();
                list.Add(new Point((float) (p.x - p.r*0.886), (float) (p.y - p.r*0.886)));
                list.Add(new Point((float) (p.x + p.r*0.886), (float) (p.y - p.r*0.886)));
                list.Add(new Point((float) (p.x + p.r*0.886), (float) (p.y + p.r*0.886)));
                list.Add(new Point((float) (p.x - p.r*0.886), (float) (p.y + p.r*0.886)));
                DrawPath(list, p.GetPathOperator());
            } else if (p.shape == Shape.PLUS) {
                DrawLine(p.x - p.r, p.y, p.x + p.r, p.y);
                DrawLine(p.x, p.y - p.r, p.x, p.y + p.r);
            } else if (p.shape == Shape.UP_ARROW) {
                list = new List<Point>();
                list.Add(new Point(p.x, p.y - p.r));
                list.Add(new Point(p.x + p.r, p.y + p.r));
                list.Add(new Point(p.x - p.r, p.y + p.r));
                list.Add(new Point(p.x, p.y - p.r));
                DrawPath(list, p.GetPathOperator());
            } else if (p.shape == Shape.DOWN_ARROW) {
                list = new List<Point>();
                list.Add(new Point(p.x - p.r, p.y - p.r));
                list.Add(new Point(p.x + p.r, p.y - p.r));
                list.Add(new Point(p.x, p.y + p.r));
                list.Add(new Point(p.x - p.r, p.y - p.r));
                DrawPath(list, p.GetPathOperator());
            } else if (p.shape == Shape.LEFT_ARROW) {
                list = new List<Point>();
                list.Add(new Point(p.x + p.r, p.y + p.r));
                list.Add(new Point(p.x - p.r, p.y));
                list.Add(new Point(p.x + p.r, p.y - p.r));
                list.Add(new Point(p.x + p.r, p.y + p.r));
                DrawPath(list, p.GetPathOperator());
            } else if (p.shape == Shape.RIGHT_ARROW) {
                list = new List<Point>();
                list.Add(new Point(p.x - p.r, p.y - p.r));
                list.Add(new Point(p.x + p.r, p.y));
                list.Add(new Point(p.x - p.r, p.y + p.r));
                list.Add(new Point(p.x - p.r, p.y - p.r));
                DrawPath(list, p.GetPathOperator());
            } else if (p.shape == Shape.HORIZONTAL_DASH) {
                DrawLine(p.x - p.r, p.y, p.x + p.r, p.y);
            } else if (p.shape == Shape.VERTICAL_DASH) {
                DrawLine(p.x, p.y - p.r, p.x, p.y + p.r);
            } else if (p.shape == Shape.X_MARK) {
                DrawLine(p.x - p.r, p.y - p.r, p.x + p.r, p.y + p.r);
                DrawLine(p.x - p.r, p.y + p.r, p.x + p.r, p.y - p.r);
            } else if (p.shape == Shape.MULTIPLY) {
                DrawLine(p.x - p.r, p.y - p.r, p.x + p.r, p.y + p.r);
                DrawLine(p.x - p.r, p.y + p.r, p.x + p.r, p.y - p.r);
                DrawLine(p.x - p.r, p.y, p.x + p.r, p.y);
                DrawLine(p.x, p.y - p.r, p.x, p.y + p.r);
            } else if (p.shape == Shape.STAR) {
                list = new List<Point>();
                for (int i = 0; i < 10; i++) {
                    double theta = i * 36 * (Math.PI / 180.0);
                    double radius = (i % 2 == 0) ? p.r*1.147 : p.r*0.38196*1.147;
                    double x = p.x + radius * Math.Sin(theta);
                    double y = p.y - radius * Math.Cos(theta);  // minus because y grows down
                    list.Add(new Point((float) x, (float) y));
                }
                DrawPath(list, p.GetPathOperator());
            }
        }
    }

    /// <summary>
    /// Sets the text rendering mode.
    /// </summary>
    /// <param name="mode">the rendering mode.</param>
    /// <returns>this Page object.</returns>
    public Page SetTextRenderingMode(int mode) {
        if (mode >= 0 && mode <= 7) {
            this.renderingMode = mode;
        } else {
            throw new Exception("Invalid text rendering mode: " + mode);
        }
        return this;
    }

    /// <summary>
    /// Sets the rotation of the text that is drawn next. A positive angle turns
    /// clockwise, as every rotation in PDFjet turns, and a negative angle
    /// counterclockwise.
    /// </summary>
    /// <param name="degrees">the angle.</param>
    /// <returns>this Page object.</returns>
    public Page SetTextRotation(int degrees) {
        // The text matrix turns counterclockwise, as PDF does.
        degrees = -degrees;
        if (degrees > 360) degrees %= 360;
        if (degrees < 0) degrees = degrees % 360 + 360;
        if (degrees == 0) {
            tmx = new float[] {1f,  0f,  0f,  1f};
        } else if (degrees == 90) {
            tmx = new float[] {0f,  1f, -1f,  0f};
        } else if (degrees == 180) {
            tmx = new float[] {-1f,  0f,  0f, -1f};
        } else if (degrees == 270) {
            tmx = new float[] {0f, -1f,  1f,  0f};
        } else if (degrees == 360) {
            tmx = new float[] {1f,  0f,  0f,  1f};
        } else {
            float sinOfAngle = (float) Math.Sin(degrees * (Math.PI / 180));
            float cosOfAngle = (float) Math.Cos(degrees * (Math.PI / 180));
            tmx = new float[] {cosOfAngle, sinOfAngle, -sinOfAngle, cosOfAngle};
        }
        tm0 = Number(tmx[0]);
        tm1 = Number(tmx[1]);
        tm2 = Number(tmx[2]);
        tm3 = Number(tmx[3]);
        return this;
    }

    /// <summary>
    /// Draws a cubic bezier curve starting from the current point to the end point p3
    /// </summary>
    /// <param name="x1">first control point x</param>
    /// <param name="y1">first control point y</param>
    /// <param name="x2">second control point x</param>
    /// <param name="y2">second control point y</param>
    /// <param name="x3">end point x</param>
    /// <param name="y3">end point y</param>
    public void CurveTo(
            float x1, float y1, float x2, float y2, float x3, float y3) {
        Append(x1);
        Append(' ');
        Append(height - y1);
        Append(' ');
        Append(x2);
        Append(' ');
        Append(height - y2);
        Append(' ');
        Append(x3);
        Append(' ');
        Append(height - y3);
        Append(" c\n");
    }

    /// <summary>
    /// Adds a circular arc to the current path.
    /// </summary>
    /// <param name="x">the x coordinate of the center.</param>
    /// <param name="y">the y coordinate of the center.</param>
    /// <param name="r">the radius.</param>
    /// <param name="startAngle">the start angle in degrees.</param>
    /// <param name="sweepDegrees">the sweep angle in degrees.</param>
    /// <returns>the control points and the end point of the last curve segment.</returns>
    public float[] AddCircularArcToPath(
            float x, float y, float r, float startAngle, float sweepDegrees) {
        return AddArcToPath(x, y, r, r, startAngle, sweepDegrees);
    }

    /// <summary>
    /// Adds an elliptical arc to the current path.
    /// </summary>
    /// <param name="x">the x coordinate of the center.</param>
    /// <param name="y">the y coordinate of the center.</param>
    /// <param name="rx">the horizontal radius.</param>
    /// <param name="ry">the vertical radius.</param>
    /// <param name="startAngle">the start angle in degrees.</param>
    /// <param name="sweepDegrees">the sweep angle in degrees.</param>
    /// <returns>the control points and the end point of the last curve segment.</returns>
    public float[] AddArcToPath(
            float x,
            float y,
            float rx,
            float ry,
            float startAngle,
            float sweepDegrees) {
        float x1 = 0f;
        float y1 = 0f;
        float x2 = 0f;
        float y2 = 0f;
        float x3 = 0f;
        float y3 = 0f;

        int numSegments = (int)Math.Ceiling(Math.Abs(sweepDegrees) / 90.0);
        double angleRad = startAngle * Math.PI / 180.0;
        double deltaPerSeg = (sweepDegrees / numSegments) * Math.PI / 180.0;
        for (int i = 0; i < numSegments; i++) {
            double segStart = angleRad;
            double segEnd   = angleRad + deltaPerSeg;
            double deltaRad = segEnd - segStart; // guaranteed ≤ ±π/2

            // Calculate safe κ
            float k = (float)(4.0 / 3.0 * Math.Tan(deltaRad / 4.0));

            float cosStart = (float)Math.Cos(segStart);
            float sinStart = (float)Math.Sin(segStart);
            float cosEnd   = (float)Math.Cos(segEnd);
            float sinEnd   = (float)Math.Sin(segEnd);

            // End points
            float x0 = x + rx * cosStart;
            float y0 = y + ry * sinStart;
            x3 = x + rx * cosEnd;
            y3 = y + ry * sinEnd;

            // Control points
            x1 = x0 - (k * rx * sinStart);
            y1 = y0 + (k * ry * cosStart);
            x2 = x3 + (k * rx * sinEnd);
            y2 = y3 - (k * ry * cosEnd);

            if (i == 0) {
                MoveTo(x0, y0);
            }
            CurveTo(x1, y1, x2, y2, x3, y3);

            angleRad = segEnd;
        }

        return new float[6] { x1, y1, x2, y2, x3, y3 };
    }

    /// <summary>
    /// Draws a bezier curve starting from the current point.
    /// <strong>Please note:</strong> You must call the StrokePath,
    /// ClosePath or FillPath methods after the last BezierCurveTo call.
    /// <para><i>Author:</i> <strong>Pieter Libin</strong>, pieter@emweb.be</para>
    /// </summary>
    /// <param name="p1">this first control point.</param>
    /// <param name="p2">this second control point.</param>
    /// <param name="p3">this end point.</param>
    public void BezierCurveTo(Point p1, Point p2, Point p3) {
        Append(p1);
        Append(p2);
        Append(p3);
        Append("c\n");
    }

    /// <summary>Sets the font and font size used to draw text.</summary>
    internal Page SetTextFont(Font font, float fontSize) {
        if (font.pdf != null && font.pdf != pdf) {
            pdf.Fail(new ArgumentException("The font belongs to another PDF."));
        }
        this.textFontSize = fontSize;
        if (writtenFont == font && writtenFontSize == fontSize) {
            return this;    // The text is drawn in this font already
        }
        writtenFont = font;
        writtenFontSize = fontSize;
        if (font.fontID != null) {
            Append('/');
            Append(font.fontID);
        } else {
            Append("/F");
            Append(font.objNumber);
        }
        Append(Token.Space);
        Append(fontSize);
        Append(" Tf\n");
        return this;
    }

    // Code provided by:
    // Dominique Andre Gunia <contact@dgunia.de>
    // <<
    /// <summary>Draws the outline of a rectangle with rounded corners using the current pen color.</summary>
    /// <param name="x">the x coordinate of the top left corner.</param>
    /// <param name="y">the y coordinate of the top left corner.</param>
    /// <param name="w">the width.</param>
    /// <param name="h">the height.</param>
    /// <param name="r1">the horizontal radius of the corners.</param>
    /// <param name="r2">the vertical radius of the corners.</param>
    public void DrawRoundedRect(float x, float y, float w, float h, float r1, float r2) {
        DrawRoundedRect(x, y, w, h, r1, r2, PathOperator.STROKE);
    }

    /// <summary>Fills a rectangle with rounded corners using the current brush color.</summary>
    /// <param name="x">the x coordinate of the top left corner.</param>
    /// <param name="y">the y coordinate of the top left corner.</param>
    /// <param name="w">the width.</param>
    /// <param name="h">the height.</param>
    /// <param name="r1">the horizontal radius of the corners.</param>
    /// <param name="r2">the vertical radius of the corners.</param>
    public void FillRoundedRect(float x, float y, float w, float h, float r1, float r2) {
        DrawRoundedRect(x, y, w, h, r1, r2, PathOperator.FILL);
    }

    // Draws a rectangle with rounded corners with the path operator.
    private void DrawRoundedRect(
            float x,
            float y,
            float w,
            float h,
            float r1,
            float r2,
            PathOperator pathOperator) {
        // The best 4-spline magic number
        float m4 = 0.55228f;
        List<Point> points = new List<Point>();

        // Starting point
        points.Add(new Point(x + w - r1, y));
        points.Add(new Point(x + w - r1 + m4*r1, y, Point.CONTROL_POINT_C));
        points.Add(new Point(x + w, y + r2 - m4*r2, Point.CONTROL_POINT_C));
        points.Add(new Point(x + w, y + r2));

        points.Add(new Point(x + w, y + h - r2));
        points.Add(new Point(x + w, y + h - r2 + m4*r2, Point.CONTROL_POINT_C));
        points.Add(new Point(x + w - m4*r1, y + h, Point.CONTROL_POINT_C));
        points.Add(new Point(x + w - r1, y + h));

        points.Add(new Point(x + r1, y + h));
        points.Add(new Point(x + r1 - m4*r1, y + h, Point.CONTROL_POINT_C));
        points.Add(new Point(x, y + h - m4*r2, Point.CONTROL_POINT_C));
        points.Add(new Point(x, y + h - r2));

        points.Add(new Point(x, y + r2));
        points.Add(new Point(x, y + r2 - m4*r2, Point.CONTROL_POINT_C));
        points.Add(new Point(x + m4*r1, y, Point.CONTROL_POINT_C));
        points.Add(new Point(x + r1, y));
        points.Add(new Point(x + w - r1, y));

        DrawPath(points, pathOperator);
    }

    /// <summary>
    /// Clips the path.
    /// </summary>
    public void ClipPath() {
        Append("W\n");
        Append("n\n");  // Close the path without painting it.
    }

    /// <summary>Sets the clipping path to the specified rectangle.</summary>
    public void ClipRect(float x, float y, float w, float h) {
        MoveTo(x, y);
        LineTo(x + w, y);
        LineTo(x + w, y + h);
        LineTo(x, y + h);
        ClipPath();
    }

    /// <summary>
    /// Saves the graphics state. Please see Example_31.
    /// </summary>
    public void SaveGraphicsState() {
        savedStates.Add(new State(brushColor, brushColorWritten, penColor, penColorWritten,
                penWidth, penWidthWritten, writtenFont, writtenFontSize,
                lineCapStyle, lineJoinStyle, strokeDashPattern));
        Append("q\n");
    }

    /// <summary>
    /// Sets the graphics state. Please see Example_31.
    /// </summary>
    /// <param name="gs">the graphics state to use.</param>
    /// <returns>this Page object.</returns>
    public Page SetGraphicsState(GraphicsState gs) {
        // The alphas are written like the other numbers of the PDF: with a
        // dot whatever the locale, at most two decimals and no exponent.
        String state = "/CA " +
                Encoding.ASCII.GetString(FastFloat.ToByteArray(gs.GetAlphaStroking())) +
                " /ca " +
                Encoding.ASCII.GetString(FastFloat.ToByteArray(gs.GetAlphaNonStroking()));
        Int32 n;
        if (pdf.states.ContainsKey(state)) {
            n = pdf.states[state];
        } else {
            n = pdf.states.Count + 1;
            pdf.states[state] = n;
        }
        Append("/GS");
        Append(n);
        Append(" gs\n");
        return this;
    }

    /// <summary>
    /// Restores the graphics state. Please see Example_31.
    /// </summary>
    public void RestoreGraphicsState() {
        if (savedStates.Count == 0) {
            pdf.Fail(new InvalidOperationException("RestoreGraphicsState was called without a matching SaveGraphicsState."));
        }
        if (savedStates.Count > 0) {
            State state = savedStates[savedStates.Count - 1];
            savedStates.RemoveAt(savedStates.Count - 1);
            brushColor = state.GetBrushColor();
            brushColorWritten = state.GetBrushColorWritten();
            penColor = state.GetPenColor();
            penColorWritten = state.GetPenColorWritten();
            penWidth = state.GetPenWidth();
            penWidthWritten = state.GetPenWidthWritten();
            writtenFont = state.GetWrittenFont();
            writtenFontSize = state.GetWrittenFontSize();
            lineCapStyle = state.GetLineCapStyle();
            lineJoinStyle = state.GetLineJoinStyle();
            strokeDashPattern = state.GetStrokeDashPattern();
        }
        Append("Q\n");
    }

    internal void AppendPointXY(float x, float y) {
        Append(x);
        Append(' ');
        Append(height - y);
        Append(' ');
    }

    internal void Append(Point point) {
        Append(point.x);
        Append(' ');
        Append(height - point.y);
        Append(' ');
    }

    internal void Append(String str) {
        if (str.Length <= 256) {
            // An operator or a short string is encoded on the stack, without
            // an array of its own. A char takes at most 3 bytes in UTF-8.
            Span<byte> bytes = stackalloc byte[str.Length * 3];
            buf.Write(bytes.Slice(0, System.Text.Encoding.UTF8.GetBytes(str, bytes)));
        } else {
            byte[] bytes = System.Text.Encoding.UTF8.GetBytes(str);
            buf.Write(bytes, 0, bytes.Length);
        }
    }

    internal void Append(int num) {
        Span<byte> digits = stackalloc byte[11];
        num.TryFormat(digits, out int length, default, System.Globalization.CultureInfo.InvariantCulture);
        buf.Write(digits.Slice(0, length));
    }

    internal void Append(float f) {
        if (!FastFloat.IsWritable(f)) {
            pdf.Fail(new ArgumentException(FastFloat.NOT_WRITABLE));
        }
        Span<byte> bytes = stackalloc byte[FastFloat.MAX_LENGTH];
        buf.Write(bytes.Slice(0, FastFloat.Write(f, bytes)));
    }

    // Returns the bytes of the number, after checking that a PDF can hold it.
    private byte[] Number(float f) {
        if (!FastFloat.IsWritable(f)) {
            pdf.Fail(new ArgumentException(FastFloat.NOT_WRITABLE));
        }
        return FastFloat.ToByteArray(f);
    }

    internal void Append(char ch) {
        buf.WriteByte((byte) ch);
    }

    internal void Append(byte b) {
        buf.WriteByte(b);
    }

    /// <summary>
    /// Appends the specified array of bytes to the page.
    /// </summary>
    internal void Append(byte[] buffer) {
        buf.Write(buffer, 0, buffer.Length);
    }

    internal static readonly byte[] HEX = {
        (byte)'0', (byte)'1', (byte)'2', (byte)'3', (byte)'4', (byte)'5', (byte)'6', (byte)'7', (byte)'8', (byte)'9',
        (byte)'A', (byte)'B', (byte)'C', (byte)'D', (byte)'E', (byte)'F'
    };
    // A character of a string, four hexadecimal digits for the Basic
    // Multilingual Plane and six above it, where the largest code point is
    // 0x10FFFF. This is the innermost loop of every string drawn, so the
    // digits go on the stack, as they do in Append(float) and Append(int).
    internal void AppendCodePointAsHex(int codePoint) {
        if (codePoint <= 0xFFFF) {
            Span<byte> digits = stackalloc byte[4];
            digits[0] = HEX[(codePoint >> 12) & 0xF];
            digits[1] = HEX[(codePoint >> 8)  & 0xF];
            digits[2] = HEX[(codePoint >> 4)  & 0xF];
            digits[3] = HEX[(codePoint)       & 0xF];
            buf.Write(digits);
        } else {
            Span<byte> digits = stackalloc byte[6];
            digits[0] = HEX[(codePoint >> 20) & 0xF];
            digits[1] = HEX[(codePoint >> 16) & 0xF];
            digits[2] = HEX[(codePoint >> 12) & 0xF];
            digits[3] = HEX[(codePoint >> 8)  & 0xF];
            digits[4] = HEX[(codePoint >> 4)  & 0xF];
            digits[5] = HEX[(codePoint)       & 0xF];
            buf.Write(digits);
        }
    }

    private void DrawWord(
            Font font,
            StringBuilder buf,
            float[] color,
            Dictionary<String, Int32> highlightColors) {
        if (buf.Length > 0) {
            String str = buf.ToString();
            // A keyword is matched as written, or ignoring case when the map
            // holds it in lower case, as TextBlock.SetHighlightColors does
            int highlight;
            if (highlightColors.TryGetValue(str, out highlight) ||
                    highlightColors.TryGetValue(str.ToLower(), out highlight)) {
                SetBrushColor(highlight);
            } else {
                SetBrushColor(color);
            }
            if (font.isCoreFont) {
                Append("[<");
                DrawASCIIString(font, str);
                Append(">] TJ\n");
            } else {
                Append("<");
                DrawUnicodeString(font, str);
                Append("> Tj\n");
            }
            buf.Length = 0;
        }
    }

    internal void DrawColoredString(
            Font font,
            String str,
            float[] color,
            Dictionary<String, Int32> highlightColors) {
        StringBuilder buf1 = new StringBuilder();
        StringBuilder buf2 = new StringBuilder();
        for (int i = 0; i < str.Length; ) {
            int count = (Util.CodePointAt(str, i) > 0xFFFF) ? 2 : 1;
            if (Char.IsLetterOrDigit(str, i)) {
                DrawWord(font, buf2, color, highlightColors);
                buf1.Append(str, i, count);
            } else {
                DrawWord(font, buf1, color, highlightColors);
                buf2.Append(str, i, count);
            }
            i += count;
        }
        DrawWord(font, buf1, color, highlightColors);
        DrawWord(font, buf2, color, highlightColors);
    }

    internal void SetStructElementsPageObjNumber(int pageObjNumber) {
        foreach (StructElement element in structures) {
            element.pageObjNumber = pageObjNumber;
        }
    }

    /// <summary>Begins a marked content sequence with the structure, actual text and alternate description.</summary>
    public void AddBDC(
            StructElem structure,
            String actualText,
            String altDescription) {
        AddBDC(structure, null, actualText, altDescription);
    }

    /// <summary>Begins a marked content sequence with the structure, language, actual text and alternate description.</summary>
    public void AddBDC(
            StructElem structure,
            String language,
            String actualText,
            String altDescription) {
        AddBDC(structure, language, actualText, altDescription, null);
    }

    // Begins the marked content of a structure element that has attributes of
    // its own, like a table cell that holds its text and nothing else and so
    // needs no paragraph under it.
    internal void AddBDC(
            StructElem structure,
            String language,
            String actualText,
            String altDescription,
            String attributes) {
        markedContentDepth++;
        if (pdf.compliance == Compliance.PDF_UA_1 && artifactDepth == 0) {
            StructElement element = new StructElement();
            element.structure = structure.Type();
            element.mcid = mcid;
            element.language = language;
            element.actualText = actualText;
            element.altDescription = altDescription;
            element.attributes = attributes;
            AddStructure(element, structParent);

            Append("/");
            Append(structure.Type());
            Append(" <</MCID ");
            Append(mcid++);
            Append(">>\n");
            Append("BDC\n");
        }
    }

    /// <summary>Begins an artifact marked content sequence.</summary>
    public void AddArtifactBMC() {
        markedContentDepth++;
        if (artifactDepth == 0) {
            artifactDepth = markedContentDepth;
            if (pdf.compliance == Compliance.PDF_UA_1) {
                Append("/Artifact BMC\n");
            }
        }
    }

    /// <summary>Ends the current marked content sequence.</summary>
    public void AddEMC() {
        if (markedContentDepth == 0) {
            pdf.Fail(new InvalidOperationException("AddEMC was called without a matching AddBDC or AddArtifactBMC."));
        }
        if (artifactDepth == 0 || artifactDepth == markedContentDepth) {
            artifactDepth = 0;
            if (pdf.compliance == Compliance.PDF_UA_1) {
                Append("EMC\n");
            }
        }
        markedContentDepth--;
    }

    // Adds a structure element that groups the elements added after it, like
    // a table row, as a kid of the parent, or of the Document element when the
    // parent is null. Returns null when the document is not tagged or the
    // content drawn is an artifact.
    internal StructElement AddStructElement(
            StructElement parent, StructElem structure, String attributes) {
        return AddStructElement(parent, structure, attributes, false);
    }

    // Adds a structure element that groups its kids. An open element is one a
    // drawable goes on adding to after the page it was made on is written,
    // like the Table of a table that runs over pages.
    internal StructElement AddStructElement(
            StructElement parent, StructElem structure, String attributes, bool open) {
        if (pdf.compliance != Compliance.PDF_UA_1 || artifactDepth != 0) {
            return null;
        }
        StructElement element = new StructElement();
        element.structure = structure.Type();
        element.mcid = -1;
        element.attributes = attributes;
        element.open = open;
        AddStructure(element, parent);
        return element;
    }

    // Gives the element its object number, which its parent and its kids refer
    // to before it is written, and adds it to its parent and to the page.
    private void AddStructure(StructElement element, StructElement parent) {
        pdf.ReserveStructTreeNumbers();
        element.objNumber = pdf.ReserveObjNumber();
        element.pageObjNumber = this.objNumber;
        element.parent = parent;
        if (parent != null) {
            parent.kids.Add(element.objNumber);
        }
        this.structures.Add(element);
    }

    internal void BeginTransform(
            float x, float y, float xScale, float yScale) {
        SaveGraphicsState();

        Append(xScale);
        Append(" 0 0 ");
        Append(yScale);
        Append(' ');
        Append(x);
        Append(' ');
        Append(y);
        Append(" cm\n");

        Append(xScale);
        Append(" 0 0 ");
        Append(yScale);
        Append(' ');
        Append(x);
        Append(' ');
        Append(y);
        Append(" Tm\n");
    }

    internal void EndTransform() {
        RestoreGraphicsState();
    }

    /// <summary>Draws the content stream scaled and placed at the specified location.</summary>
    public void DrawContents(
            byte[] content,
            float h,    // The height of the graphics object in points.
            float x,
            float y,
            float xScale,
            float yScale) {
        BeginTransform(x, (this.height - yScale * h) - y, xScale, yScale);
        Append(content);
        Append(Token.Newline);      // The content can end with an operator, like ET.
        EndTransform();
    }

    /// <summary>
    /// Draws each character of <paramref name="str"/> at successive X positions,
    /// advancing by <paramref name="dx"/> after every glyph.
    /// </summary>
    /// <param name="font">Font used for rendering.</param>
    /// <param name="fontSize">Size of the font.</param>
    /// <param name="str">Text to draw.</param>
    /// <param name="x">Starting X coordinate.</param>
    /// <param name="y">Baseline Y coordinate.</param>
    /// <param name="dx">Horizontal offset applied after each character.</param>
    public void DrawString(
            Font font, float fontSize, String str, float x, float y, float dx) {
        float x1 = x;
        for (int i = 0; i < str.Length; ) {
            int count = (Util.CodePointAt(str, i) > 0xFFFF) ? 2 : 1;
            DrawString(font, fontSize, str.Substring(i, count), x1, y);
            x1 += dx;
            i += count;
        }
    }

    /// <summary>
    /// Sets the text location.
    /// </summary>
    /// <param name="x">the x coordinate of new text location.</param>
    /// <param name="y">the y coordinate of new text location.</param>
    internal void SetTextLocation(float x, float y) {
        Append(x);
        Append(Token.Space);
        Append(height - y);
        Append(" Td\n");
    }

    /// <summary>
    /// Sets the text leading.
    /// </summary>
    /// <param name="leading">the leading.</param>
    internal void SetTextLeading(float leading) {
        Append(leading);
        Append(" TL\n");
    }

    /// <summary>
    /// Advance to the next line.
    /// </summary>
    internal void NextLine() {
        Append("T*\n");
    }

    internal void SetTextScaling(float scaling) {
        Append(scaling);
        Append(" Tz\n");
    }

    internal void SetTextRise(float rise) {
        Append(rise);
        Append(" Ts\n");
        this.textRise = rise;
    }

    /// <summary>
    /// Draws a string at the specified location.
    /// </summary>
    /// <param name="font">the font.</param>
    /// <param name="str">the string.</param>
    /// <param name="x">the x coordinate.</param>
    /// <param name="y">the y coordinate.</param>
    internal void DrawTextLine(Font font, String str, float x, float y) {
        Append("BT\n");
        SetTextLocation(x, y);
        SetTextFont(font, font.size);
        if (font.isCoreFont) {
            Append("[<");
            DrawASCIIString(font, str);
            Append(">] TJ\n");
        } else {
            Append("<");
            DrawUnicodeString(font, str);
            Append("> Tj\n");
        }
        Append("ET\n");
    }

    internal void RotateAroundCenter(float centerX, float centerY, float degrees) {
        Append("1 0 0 1 ");
        Append(centerX);
        Append(" ");
        Append(centerY);
        Append(" cm\n");

        double radians = degrees * Math.PI / 180;
        float cos = (float)Math.Cos(radians);
        float sin = (float)Math.Sin(radians);
        Append(cos);
        Append(" ");
        Append(sin);
        Append(" ");
        Append(-sin);
        Append(" ");
        Append(cos);
        Append(" 0 0 cm\n");

        Append("1 0 0 1 ");
        Append(-centerX);
        Append(" ");
        Append(-centerY);
        Append(" cm\n");
    }

    internal void AddAnnotation(Annotation annotation) {
        annotation.y1 = this.height - annotation.y1;
        annotation.y2 = this.height - annotation.y2;
        annots.Add(annotation);
        if (pdf.compliance == Compliance.PDF_UA_1) {
            StructElement element = new StructElement();
            // PDF/UA puts a link in a Link element, and any other annotation in an Annot element.
            element.structure = annotation.annotationType.Equals(Annotation.Link) ?
                    StructElem.LINK.Type() : StructElem.ANNOT.Type();
            element.language = annotation.language;
            element.actualText = annotation.actualText;
            element.altDescription = annotation.altDescription;
            element.annotation = annotation;
            AddStructure(element, structParent);
        }
    }

    internal void DrawTextBlock(
            Font font,
            Font fallbackFont,
            float fontSize,
            float fallbackFontSize,
            TextLine[] textLines,
            float x,
            float y,
            float leading,
            float[] color,
            Dictionary<String, Int32> highlightColors,
            String language) {
        if (textLines == null || textLines.Length == 0) {
            return;
        }
        // The fallback font is used as DrawString uses it.
        bool hasFallbackFont = fallbackFont != null && fallbackFont != font &&
                !font.isCJK && !fallbackFont.isCJK;

        // A span gives the language of the text, for screen readers and text extraction.
        bool hasLanguage = !String.IsNullOrEmpty(language);
        if (hasLanguage) {
            Append("/Span <</Lang <");
            Append(ToUTF16Hex(language));
            Append(">>> BDC\n");
        }
        Append("BT\n");
        SetBrushColor(color);
        SetTextFont(font, fontSize);
        float yText = y;
        foreach (TextLine textLine in textLines) {
            Append("1 0 0 1 ");
            Append(x + textLine.xOffset);
            Append(' ');
            Append(height - (yText + font.GetAscent(fontSize)));
            Append(" Tm\n");
            if (hasFallbackFont) {
                DrawTextBlockLine(font, fallbackFont, fontSize, fallbackFontSize, textLine.text, color, highlightColors);
            } else if (highlightColors == null) {
                if (font.isCoreFont) {
                    Append("[<");
                    DrawASCIIString(font, textLine.text);
                    Append(">] TJ\n");
                } else {
                    Append("<");
                    DrawUnicodeString(font, textLine.text);
                    Append("> Tj\n");
                }
            } else {
                DrawColoredString(font, textLine.text, color, highlightColors);
            }
            yText += leading;
        }
        Append("ET\n");
        if (hasLanguage) {
            Append("EMC\n");
        }

        float yLine = y + font.GetBodyHeight(fontSize);
        float yStrike = y + font.GetAscent(fontSize) - font.GetBodyHeight(fontSize) / 4f;
        bool decorated = false;
        foreach (TextLine textLine in textLines) {
            if (textLine.underline || textLine.strikeout) {
                decorated = true;
                break;
            }
        }
        if (decorated) {
            // The lines are drawn in the color of the text and as thick as the
            // font says they are, as TextLine draws them.
            SetPenWidth(font.GetUnderlineThickness(fontSize));
            SetPenColor(color);
        }
        foreach (TextLine textLine in textLines) {
            if (textLine.underline || textLine.strikeout) {
                float width = hasFallbackFont ?
                        font.StringWidth(fallbackFont, fontSize, fallbackFontSize, textLine.text) :
                        font.StringWidth(fontSize, textLine.text);
                if (textLine.underline) {
                    MoveTo(x + textLine.xOffset, yLine);
                    LineTo(x + textLine.xOffset + width, yLine);
                    StrokePath();
                }
                if (textLine.strikeout) {
                    MoveTo(x + textLine.xOffset, yStrike);
                    LineTo(x + textLine.xOffset + width, yStrike);
                    StrokePath();
                }
            }
            yLine += leading;
            yStrike += leading;
        }
    }

    // Draws a line of a text block at the text position, with the characters the
    // font has no glyph for in the fallback font, as DrawString draws them.
    private void DrawTextBlockLine(
            Font font,
            Font fallbackFont,
            float fontSize,
            float fallbackFontSize,
            String str,
            float[] color,
            Dictionary<String, Int32> highlightColors) {
        Font activeFont = font;
        int start = 0;
        for (int i = 0; i < str.Length; i += Util.CharCount(str, i)) {
            Font charFont = Font.FontOf(font, fallbackFont, activeFont, str, i);
            if (charFont != activeFont) {
                DrawTextBlockRun(activeFont, str.Substring(start, i - start), color, highlightColors);
                start = i;
                activeFont = charFont;
                SetTextFont(activeFont, (activeFont == font) ? fontSize : fallbackFontSize);
            }
        }
        DrawTextBlockRun(activeFont, str.Substring(start), color, highlightColors);
        if (activeFont != font) {
            SetTextFont(font, fontSize);    // The next line starts in the font
        }
    }

    // Draws the string at the text position in the font set last.
    private void DrawTextBlockRun(
            Font font, String str, float[] color, Dictionary<String, Int32> highlightColors) {
        if (str.Length == 0) {
            return;
        }
        if (highlightColors != null) {
            DrawColoredString(font, str, color, highlightColors);
        } else if (font.isCoreFont) {
            Append("[<");
            DrawASCIIString(font, str);
            Append(">] TJ\n");
        } else {
            Append("<");
            DrawUnicodeString(font, str);
            Append("> Tj\n");
        }
    }

    // Returns the string as a PDF text string, in UTF-16BE with a byte order
    // mark, written in hexadecimal.
    private static String ToUTF16Hex(String str) {
        StringBuilder sb = new StringBuilder("FEFF");
        foreach (char ch in str) {
            sb.Append(((int) ch).ToString("X4"));
        }
        return sb.ToString();
    }
}   // End of Page.cs
}   // End of namespace PDFjet.NET
