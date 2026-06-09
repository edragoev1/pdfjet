/**
 * Page.cs
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
using System;
using System.IO;
using System.Text;
using System.Collections.Generic;

/**
 * Used to create PDF page objects.
 *
 * Please note:
 * <pre>
 * The coordinate (0.0f, 0.0f) is the top left corner of the page.
 * The size of the pages are represented in points.
 * 1 point is 1/72 inches.
 * </pre>
 */
namespace PDFjet.NET {
public class Page {
    public static bool DETACHED = false;

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
    private float[] penCMYK = {0f, 0f, 0f, 1f};
    private float[] brushCMYK = {0f, 0f, 0f, 1f};

    private float[] tmx = {1f, 0f, 0f, 1f};
    private byte[] tm0;
    private byte[] tm1;
    private byte[] tm2;
    private byte[] tm3;

    internal float penWidth = 0.5f;

    internal CapStyle lineCapStyle = CapStyle.BUTT;
    internal JoinStyle lineJoinStyle = JoinStyle.MITER;
    internal String strokePattern = "[] 0";

    internal float rotateDegrees = 0f;

    internal readonly List<Int32> contents;
    internal readonly List<Annotation> annots;
    internal readonly List<Destination> destinations;

    private int mcid;

    /**
     * Creates page object and add it to the PDF document.
     *
     * Please note:
     * <pre>
     * The coordinate (0.0, 0.0) is the top left corner of the page.
     * The size of the pages are represented in points.
     * 1 point is 1/72 inches.
     * </pre>
     *
     * @param pdf the pdf object.
     * @param pageSize the page size of this page.
     */
    public Page(PDF pdf, float[] pageSize) : this(pdf, pageSize, true) {
    }

    /**
     * Creates page object and add it to the PDF document.
     *
     * Please note:
     * <pre>
     * The coordinate (0.0, 0.0) is the top left corner of the page.
     * The size of the pages are represented in points.
     * 1 point is 1/72 inches.
     * </pre>
     *
     * @param pdf the pdf object.
     * @param pageSize the page size of this page.
     * @param addPageToPDF bool flag.
     */
    public Page(PDF pdf, float[] pageSize, bool addPageToPDF) {
        this.pdf = pdf;
        this.contents = new List<Int32>();
        this.annots = new List<Annotation>();
        this.destinations = new List<Destination>();
        this.width = pageSize[0];
        this.height = pageSize[1];
        this.buf = new MemoryStream(8192);
        this.tm0 = FastFloat.ToByteArray(tmx[0]);
        this.tm1 = FastFloat.ToByteArray(tmx[1]);
        this.tm2 = FastFloat.ToByteArray(tmx[2]);
        this.tm3 = FastFloat.ToByteArray(tmx[3]);
        if (addPageToPDF) {
            pdf.AddPage(this);
        }
    }

    public Page(PDF pdf, PDFobj pageObj) {
        this.pdf = pdf;
        this.pageObj = RemoveComments(pageObj);
        this.width = pageObj.GetPageSize()[0];
        this.height = pageObj.GetPageSize()[1];
        this.buf = new MemoryStream(8192);
        this.tm0 = FastFloat.ToByteArray(tmx[0]);
        this.tm1 = FastFloat.ToByteArray(tmx[1]);
        this.tm2 = FastFloat.ToByteArray(tmx[2]);
        this.tm3 = FastFloat.ToByteArray(tmx[3]);
        Append("q\n");
        if (pageObj.gsNumber != -1) {
            Append("/GS");
            Append(pageObj.gsNumber + 1);
            Append(" gs\n");
        }
    }

    public void Complete(List<PDFobj> objects) {
        Append("Q\n");
        pageObj.AddContent(GetContent(), objects);
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

    public Font AddResource(int coreFont, List<PDFobj> objects) {
        return pageObj.AddResource(coreFont, objects);
    }

    public void AddResource(Image image, List<PDFobj> objects) {
        pageObj.AddResource(image, objects);
    }

    public void AddResource(Font font, List<PDFobj> objects) {
        pageObj.AddResource(font, objects);
    }

    /**
     * Adds destination to this page.
     *
     * @param name The destination name.
     * @param xPosition The horizontal position of the destination on this page.
     * @param yPosition The vertical position of the destination on this page.
     *
     * @return the destination.
     */
    public Destination AddDestination(String name, float xPosition, float yPosition) {
        Destination dest = new Destination(name, xPosition, height - yPosition);
        destinations.Add(dest);
        return dest;
    }

    /**
     * Adds destination to this page.
     *
     * @param name The destination name.
     * @param yPosition The vertical position of the destination on this page.
     * @return the destination.
     */
    public Destination AddDestination(String name, float yPosition) {
        Destination dest = new Destination(name, 0f, height - yPosition);
        destinations.Add(dest);
        return dest;
    }

    /**
     * Sets the page CropBox.
     * See page 77 of the PDF32000_2008.pdf specification.
     *
     * @param upperLeftX the top left X coordinate of the CropBox.
     * @param upperLeftY the top left Y coordinate of the CropBox.
     * @param lowerRightX the bottom right X coordinate of the CropBox.
     * @param lowerRightY the bottom right Y coordinate of the CropBox.
     */
    public void SetCropBox(
            float upperLeftX, float upperLeftY, float lowerRightX, float lowerRightY) {
        this.cropBox = new float[] {upperLeftX, upperLeftY, lowerRightX, lowerRightY};
    }

    /**
     * Sets the page BleedBox.
     * See page 77 of the PDF32000_2008.pdf specification.
     *
     * @param upperLeftX the top left X coordinate of the BleedBox.
     * @param upperLeftY the top left Y coordinate of the BleedBox.
     * @param lowerRightX the bottom right X coordinate of the BleedBox.
     * @param lowerRightY the bottom right Y coordinate of the BleedBox.
     */
    public void SetBleedBox(
            float upperLeftX, float upperLeftY, float lowerRightX, float lowerRightY) {
        this.bleedBox = new float[] {upperLeftX, upperLeftY, lowerRightX, lowerRightY};
    }

    /**
     * Sets the page TrimBox.
     * See page 77 of the PDF32000_2008.pdf specification.
     *
     * @param upperLeftX the top left X coordinate of the TrimBox.
     * @param upperLeftY the top left Y coordinate of the TrimBox.
     * @param lowerRightX the bottom right X coordinate of the TrimBox.
     * @param lowerRightY the bottom right Y coordinate of the TrimBox.
     */
    public void SetTrimBox(
            float upperLeftX, float upperLeftY, float lowerRightX, float lowerRightY) {
        this.trimBox = new float[] {upperLeftX, upperLeftY, lowerRightX, lowerRightY};
    }

    /**
     * Sets the page ArtBox.
     * See page 77 of the PDF32000_2008.pdf specification.
     *
     * @param upperLeftX the top left X coordinate of the ArtBox.
     * @param upperLeftY the top left Y coordinate of the ArtBox.
     * @param lowerRightX the bottom right X coordinate of the ArtBox.
     * @param lowerRightY the bottom right Y coordinate of the ArtBox.
     */
    public void SetArtBox(
            float upperLeftX, float upperLeftY, float lowerRightX, float lowerRightY) {
        this.artBox = new float[] {upperLeftX, upperLeftY, lowerRightX, lowerRightY};
    }

    public float[] AddHeader(TextLine textLine) {
        return AddHeader(textLine, 1.5f*textLine.font.GetAscent(textLine.fontSize));
    }

    public float[] AddHeader(TextLine textLine, float offset) {
        textLine.SetLocation((GetWidth() - textLine.GetWidth())/2, offset);
        float[] xy = textLine.DrawOn(this);
        xy[1] += textLine.font.GetDescent();
        return xy;
    }

    public float[] AddFooter(TextLine textLine) {
        return AddFooter(textLine, textLine.font.GetAscent());
    }

    public float[] AddFooter(TextLine textLine, float offset) {
        textLine.SetLocation((GetWidth() - textLine.GetWidth())/2, GetHeight() - offset);
        return textLine.DrawOn(this);
    }

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
        watermark.SetTextDirection((int) (angle * (180.0 / Math.PI)));
        watermark.DrawOn(this);
    }

    public void RotateBy(double rotateDegrees) {
        if (rotateDegrees == 0 ||
            rotateDegrees == 90 ||
            rotateDegrees == 180 ||
            rotateDegrees == 270) {
            this.rotateDegrees = (float) rotateDegrees;
        }
    }

    public void InvertYAxis() {
        Append("1 0 0 -1 0 ");
        Append(this.height);
        Append(" cm\n");
    }

    public byte[] GetContent() {
        return buf.ToArray();
    }

    /**
     * Returns the width of this page.
     *
     * @return the width of the page.
     */
    public float GetWidth() {
        return width;
    }

    /**
     * Returns the height of this page.
     *
     * @return the height of the page.
     */
    public float GetHeight() {
        return height;
    }

    public String GetStrokePattern() {
        return this.strokePattern;
    }

    /**
     * Draws a line on the page, using the current color, between the points (x1, y1) and (x2, y2).
     *
     * @param x1 the first point's x coordinate.
     * @param y1 the first point's y coordinate.
     * @param x2 the second point's x coordinate.
     * @param y2 the second point's y coordinate.
     */
    public void DrawLine(
            double x1,
            double y1,
            double x2,
            double y2) {
        MoveTo(x1, y1);
        LineTo(x2, y2);
        StrokePath();
    }

    public void DrawString(
            Font font,
            Font fallbackFont,
            float fontSize,
            String str,
            float x,
            float y) {
        DrawString(font, fallbackFont, fontSize, str, x, y, new float[] {0f, 0f, 0f}, null);
    }

    public void DrawString(
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

    /**
     * Draws the text given by the specified string,
     * using the specified Thai or Hebrew font and the current brush color.
     * If the font is missing some glyphs - the fallback font is used.
     * The baseline of the leftmost character is at position (x, y) on the page.
     *
     * @param font1 the Thai or Hebrew font.
     * @param font2 the fallback font.
     * @param str the string to be drawn.
     * @param x the x coordinate.
     * @param y the y coordinate.
     */
    public void DrawString(
            Font font,
            Font fallbackFont,
            float fontSize,
            String str,
            float x,
            float y,
            float[] textColor,
            Dictionary<String, Int32> colors) {
        if (font.isCoreFont || font.isCJK || fallbackFont == null || fallbackFont.isCoreFont || fallbackFont.isCJK) {
            DrawString(font, fontSize, str, x, y, textColor, colors);
        } else {
            Font activeFont = font;
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < str.Length; i++) {
                int ch = str[i];
                if (activeFont.unicodeToGID[ch] == 0) {
                    DrawString(activeFont, fontSize, sb.ToString(), x, y, textColor, colors);
                    x += activeFont.StringWidth(fontSize, sb.ToString());
                    sb.Length = 0;
                    // Switch the font
                    if (activeFont == font) {
                        activeFont = fallbackFont;
                    } else {
                        activeFont = font;
                    }
                }
                sb.Append((char) ch);
            }
            DrawString(activeFont, fontSize, sb.ToString(), x, y, textColor, colors);
        }
    }

    /**
     * Draws the text given by the specified string,
     * using the specified font and the current brush color.
     * The baseline of the leftmost character is at position (x, y) on the page.
     *
     * @param font the font to use.
     * @param str the string to be drawn.
     * @param x the x coordinate.
     * @param y the y coordinate.
     */
    public void DrawString(
            Font font,
            double fontSize,
            String str,
            double x,
            double y) {
        DrawString(font, (float) fontSize, str, (float) x, (float) y);
    }

    public void DrawString(
            Font font,
            float fontSize,
            String str,
            float x,
            float y) {
        DrawString(font, fontSize, str, x, y, new float[] {0f, 0f, 0f}, null);
    }

    /**
     * Draws the text given by the specified string,
     * using the specified font and the current brush color.
     * The baseline of the leftmost character is at position (x, y) on the page.
     *
     * @param font the font to use.
     * @param str the string to be drawn.
     * @param x the x coordinate.
     * @param y the y coordinate.
     */
    public void DrawString(
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
        } else {
            Append(tm0);
            Append(' ');
            Append(tm1);
            Append(' ');
            Append(tm2);
            Append(' ');
            Append(tm3);
        }
        Append(' ');
        Append(x);
        Append(' ');
        Append(height - y);
        Append(" Tm\n");

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

    private void DrawASCIIString(Font font, String str) {
        int len = str.Length;
        for (int i = 0; i < len; i++) {
            int c1 = str[i];
            if (c1 < font.firstChar || c1 > font.lastChar) {
                Append(0x20.ToString("X2"));
                continue;
            }
            Append(c1.ToString("X2"));
            if (font.isCoreFont && font.kernPairs && i < (str.Length - 1)) {
                c1 -= 32;
                int c2 = str[i + 1];
                if (c2 < font.firstChar || c2 > font.lastChar) {
                    c2 = 32;
                }
                for (int j = 2; j < font.metrics[c1].Length; j += 2) {
                    if (font.metrics[c1][j] == c2) {
                        Append(">");
                        Append(-font.metrics[c1][j + 1]);
                        Append("<");
                        break;
                    }
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
        } else {
            int i = 0;
            while (i < str.Length) {
                int codePoint = char.ConvertToUtf32(str, i);
                if (codePoint != 0xFEFF) {                  // BOM
                    if (codePoint < font.firstChar || codePoint > font.lastChar) {
                        AppendCodePointAsHex(font.unicodeToGID[0x0020]); // Space fallback
                    } else {
                        AppendCodePointAsHex(font.unicodeToGID[codePoint]);
                    }
                }
                i += char.IsHighSurrogate(str[i]) ? 2 : 1;  // Proper surrogate handling
            }
        }
    }

//    /**
//     * Sets the color for brush operations.
//     * This is the color used when drawing regular text and filling shapes.
//     *
//     * @param r the red component is float value from 0.0 to 1.0.
//     * @param g the green component is float value from 0.0 to 1.0.
//     * @param b the blue component is float value from 0.0 to 1.0.
//     */
//    public void SetBrushColor(double r, double g, double b) {
//        SetBrushColor(new float[] { (float) r, (float) g, (float) b });
//    }
//
//    /**
//     * Sets the color for stroking operations.
//     * The pen color is used when drawing lines and splines.
//     *
//     * @param r the red component is float value from 0.0 to 1.0.
//     * @param g the green component is float value from 0.0 to 1.0.
//     * @param b the blue component is float value from 0.0 to 1.0.
//     */
//    public void SetPenColor(
//            double r, double g, double b) {
//        SetPenColor(new float[] { (float) r, (float) g, (float) b });
//    }
//
//    /**
//     * Sets the color for brush operations.
//     * This is the color used when drawing regular text and filling shapes.
//     *
//     * @param r the red component is float value from 0.0f to 1.0f.
//     * @param g the green component is float value from 0.0f to 1.0f.
//     * @param b the blue component is float value from 0.0f to 1.0f.
//     */
//    [Obsolete("This method is now obsolete. Use SetBrushColor(float[] rgbColor) instead.")]
//    public void SetBrushColor(float r, float g, float b) {
//        SetBrushColor(new float[] {r, g, b}); // Call the second method with an array
//    }

    /**
     * Sets the brush color.
     *
     * @param color the color. See the Color class for predefined values or define your own using 0x00RRGGBB packed integers.
     * @throws IOException
     */
    public void SetBrushColor(int color) {
        float r = ((color >> 16) & 0xff)/255f;
        float g = ((color >>  8) & 0xff)/255f;
        float b = ((color)       & 0xff)/255f;
        Append(r);
        Append(Token.Space);
        Append(g);
        Append(Token.Space);
        Append(b);
        Append(" rg\n");
        this.brushColor = new float[] {r, g, b};
    }

    /**
     * Sets the color for brush operations.
     *
     * @param color the color.
     * @throws IOException
     */
    public void SetBrushColor(float[] rgbColor) {
        if (rgbColor == null) {
            return; // Early exit if null
        }

        if (rgbColor[0] < 0f || rgbColor[0] > 1f ||
            rgbColor[1] < 0f || rgbColor[1] > 1f ||
            rgbColor[2] < 0f || rgbColor[2] > 1f) {
            Console.WriteLine("Warning: RGB color values must be between 0f and 1f. Ignoring request.");
            return; // Early exit if out of range
        }

        // Now set the brush color
        this.brushColor = rgbColor;

        // Proceed with setting the color (example)
        Append(rgbColor[0]);
        Append(Token.Space);
        Append(rgbColor[1]);
        Append(Token.Space);
        Append(rgbColor[2]);
        Append(" rg\n");
    }

    /// <summary>
    /// Returns the current brush color as an RGB float array.
    /// </summary>
    /// <returns>
    /// A <see cref="float[]"/> containing the red, green, and blue components (0.0f to 1.0f) of the brush color.
    /// </returns>
    public float[] GetBrushColor() {
        return brushColor;
    }

    /// <summary>
    /// Sets the pen color using a packed 0x00RRGGBB integer value.
    /// </summary>
    /// <param name="color">
    /// The color value, where each component (red, green, blue) is packed into a 24-bit integer.
    /// You can use predefined colors from the <see cref="Color"/> class or define your own.
    /// </param>
    /// <exception cref="IOException">
    public void SetPenColor(int color) {
        float r = ((color >> 16) & 0xff)/255f;
        float g = ((color >>  8) & 0xff)/255f;
        float b = ((color)       & 0xff)/255f;
        Append(r);
        Append(Token.Space);
        Append(g);
        Append(Token.Space);
        Append(b);
        Append(" RG\n");
        // Set the pen color
        this.penColor = new float[] {r, g, b};
    }

    /// <summary>
    /// Sets the pen color using individual red, green, and blue components.
    /// </summary>
    /// <param name="r">The red component as a float value from 0.0f to 1.0f.</param>
    /// <param name="g">The green component as a float value from 0.0f to 1.0f.</param>
    /// <param name="b">The blue component as a float value from 0.0f to 1.0f.</param>
    /// <remarks>
    /// <b>Deprecated</b>: This method is now obsolete. Use SetPenColor(float[] rgbColor) instead.
    /// </remarks>
    [Obsolete("This method is now obsolete. Use SetPenColor(float[] rgbColor) instead.")]
    public void SetPenColor(float r, float g, float b) {
        // Call the newer method using an array
        SetPenColor(new float[] { r, g, b });
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
    public void SetPenColor(float[] rgbColor) {
        if (rgbColor == null) {
            return; // Early exit if null
        }

        if (rgbColor[0] < 0f || rgbColor[0] > 1f ||
            rgbColor[1] < 0f || rgbColor[1] > 1f ||
            rgbColor[2] < 0f || rgbColor[2] > 1f) {
            Console.WriteLine("Warning: RGB color values must be between 0f and 1f. Ignoring request.");
            return; // Early exit if out of range
        }

        // Now set the pen color
        this.penColor = rgbColor;

        // Proceed with setting the color (example)
        Append(rgbColor[0]);
        Append(Token.Space);
        Append(rgbColor[1]);
        Append(Token.Space);
        Append(rgbColor[2]);
        Append(" RG\n");
    }

    /// <summary>
    /// Gets the current pen color as an RGB float array.
    /// </summary>
    /// <returns>
    /// A <see cref="float[]"/> with three elements: red, green, and blue components (0.0f to 1.0f).
    /// </returns>
    public float[] GetPenColor() {
        return penColor;
    }

    /**
     * Sets the color for brush operations using CMYK.
     * This is the color used when drawing regular text and filling shapes.
     *
     * @param c the cyan component is float value from 0.0f to 1.0f.
     * @param m the magenta component is float value from 0.0f to 1.0f.
     * @param y the yellow component is float value from 0.0f to 1.0f.
     * @param k the black component is float value from 0.0f to 1.0f.
     */
    public void SetBrushColorCMYK(float c, float m, float y, float k) {
        this.brushCMYK = new float[] {c, m, y, k};
        Append(c);
        Append(' ');
        Append(m);
        Append(' ');
        Append(y);
        Append(' ');
        Append(k);
        Append(" k\n");
    }

    /**
     * Sets the color for stroking operations using CMYK.
     * The pen color is used when drawing lines and splines.
     *
     * @param c the cyan component is float value from 0.0f to 1.0f.
     * @param m the magenta component is float value from 0.0f to 1.0f.
     * @param y the yellow component is float value from 0.0f to 1.0f.
     * @param k the black component is float value from 0.0f to 1.0f.
     */
    public void SetPenColorCMYK(float c, float m, float y, float k) {
        this.penCMYK = new float[] {c, m, y, k};
        Append(c);
        Append(' ');
        Append(m);
        Append(' ');
        Append(y);
        Append(' ');
        Append(k);
        Append(" K\n");
    }

    /**
     * Sets the line width to the default.
     * The default is the finest line width.
     */
    public void SetDefaultStrokeWidth() {
        Append("0 w\n");
    }

    /**
     * The stroke dash pattern controls the pattern of dashes and gaps used to stroke paths.
     * It is specified by a dash array and a dash phase.
     * The elements of the dash array are positive numbers that specify the lengths of
     * alternating dashes and gaps.
     * The dash phase specifies the distance into the dash pattern at which to start the dash.
     * The elements of both the dash array and the dash phase are expressed in user space units.
     * <pre>
     * Examples of line dash patterns:
     *
     *     "[Array] Phase"     Appearance          Description
     *     _______________     _________________   ____________________________________
     *
     *     "[] 0"              -----------------   Solid line
     *     "[3] 0"             ---   ---   ---     3 units on, 3 units off, ...
     *     "[2] 1"             -  --  --  --  --   1 on, 2 off, 2 on, 2 off, ...
     *     "[2 1] 0"           -- -- -- -- -- --   2 on, 1 off, 2 on, 1 off, ...
     *     "[3 5] 6"             ---     ---       2 off, 3 on, 5 off, 3 on, 5 off, ...
     *     "[2 3] 11"          -   --   --   --    1 on, 3 off, 2 on, 3 off, 2 on, ...
     * </pre>
     *
     * @param pattern the line dash pattern.
     */
    public void SetStrokeDashPattern(String pattern) {
        this.strokePattern = pattern;
        Append(pattern);
        Append(" d\n");
    }

    /**
     * Sets the default stroke pattern to be solid line or curve.
     */
    public void SetDefaultStrokePattern() {
        Append("[] 0");
        Append(" d\n");
    }

    /**
     * Sets the pen width that will be used to draw lines and splines on this page.
     *
     * @param width the pen width.
     */
    public void SetPenWidth(double width) {
        SetPenWidth((float) width);
    }

    /**
     * Sets the pen width that will be used to draw lines and splines on this page.
     *
     * @param width the pen width.
     */
    public void SetPenWidth(float width) {
        this.penWidth = width;
        Append(width);
        Append(" w\n");
    }

    public float GetPenWidth() {
        return this.penWidth;
    }

    /**
     * Sets the current line cap style.
     *
     * @param style the cap style of the current line.
     * Supported values: CapStyle.BUTT, CapStyle.ROUND and CapStyle.PROJECTING_SQUARE
     */
    public void SetLineCapStyle(CapStyle style) {
        this.lineCapStyle = style;
        Append((Int32) style);
        Append(" J\n");
    }

    /**
     * Sets the line join style.
     *
     * @param style the line join style code.
     * Supported values: JoinStyle.MITER, JoinStyle.ROUND and JoinStyle.BEVEL
     */
    public void SetLineJoinStyle(JoinStyle style) {
        this.lineJoinStyle = style;
        Append((Int32) style);
        Append(" j\n");
    }

    /// <summary>
    /// Convenience overload that accepts double‑precision coordinates and
    /// forwards them to the core <see cref="MoveTo(float,float)"/> method.
    /// </summary>
    /// <param name="x">Horizontal coordinate (double).</param>
    /// <param name="y">Vertical coordinate (double).</param>
    public void MoveTo(double x, double y) {
        MoveTo((float) x, (float) y);
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
    /// Convenience overload that accepts double‑precision coordinates and
    /// forwards them to the core <see cref="LineTo(float,float)"/> method.
    /// </summary>
    /// <param name="x">Horizontal coordinate (double).</param>
    /// <param name="y">Vertical coordinate (double).</param>
    public void LineTo(double x, double y) {
        LineTo((float) x, (float) y);
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
    /// <param name="x">X‑coordinate of the lower‑left corner.</param>
    /// <param name="y">Y‑coordinate of the lower‑left corner.</param>
    /// <param name="w">Rectangle width.</param>
    /// <param name="h">Rectangle height.</param>
    public void DrawRect(double x, double y, double w, double h) {
        MoveTo(x, y);
        LineTo(x + w, y);
        LineTo(x + w, y + h);
        LineTo(x, y + h);
        ClosePath();          // close and stroke
    }

    /// <summary>
    /// Fills a rectangle using the current brush color.
    /// </summary>
    /// <param name="x">X‑coordinate of the lower‑left corner.</param>
    /// <param name="y">Y‑coordinate of the lower‑left corner.</param>
    /// <param name="w">Rectangle width.</param>
    /// <param name="h">Rectangle height.</param>
    public void FillRect(double x, double y, double w, double h) {
        MoveTo(x, y);
        LineTo(x + w, y);
        LineTo(x + w, y + h);
        LineTo(x, y + h);
        FillPath();           // close and fill
    }

    /// <summary>
    /// Draws a path consisting of multiple points using the specified path operator.
    /// Supports both straight line segments and Bézier curve segments with control points.
    /// </summary>
    /// <param name="path">The list of points defining the path. The first point sets the starting position,
    /// subsequent points define line segments or curve control points. Must contain at least 2 points.</param>
    /// <param name="pathOperator">The path painting operator to apply (e.g., "S" for stroke, "f" for fill).
    /// Use constants from the PathOperator class for standard operators.</param>
    /// <exception cref="Exception">Thrown when the path contains fewer than 2 points.</exception>
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
    ///     new Point(150, 50, Point.ControlPointC),    // First control point
    ///     new Point(250, 150, Point.ControlPointC),   // Second control point
    ///     new Point(300, 100)            // End point
    /// };
    /// DrawPath(curve, PathOperator.STROKE);
    /// </code>
    /// </example>
    /// <see also cref="Point"/>
    /// <see also cref="PathOperator"/>
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
        Append(pathOperator);
        Append('\n');
    }

    /**
     * Strokes a bezier curve and draws it using the current pen.
     * @deprecated  As of v4.00 replaced by {@link #drawPath(List, char)}
     *
     * @param list the list of points that define the bezier curve.
     */
    public void DrawBezierCurve(List<Point> list) {
        DrawBezierCurve(list, PathOperator.Stroke);
    }

    /**
     * Draws a bezier curve and fills it using the current brush.
     * @deprecated  As of v4.00 replaced by {@link #drawPath(List, char)}
     *
     * @param list the list of points that define the bezier curve.
     * @param operation must be Operation.STROKE or Operation.FILL.
     */
    public void DrawBezierCurve(List<Point> list, String pathOperator) {
        Point point = list[0];
        MoveTo(point.x, point.y);
        for (int i = 1; i < list.Count; i++) {
            point = list[i];
            Append(point);
            if (i % 3 == 0) {
                Append("c\n");
            }
        }
        Append(pathOperator);
        Append('\n');
    }

    /**
     * Draws an ellipse on the page and fills it using the current brush color.
     *
     * @param x the x coordinate of the center of the ellipse to be drawn.
     * @param y the y coordinate of the center of the ellipse to be drawn.
     * @param rx the horizontal radius of the ellipse to be drawn.
     * @param ry the vertical radius of the ellipse to be drawn.
     * @param operation must be: Operation.FILL
     */
    public void DrawEllipse(
            float x,
            float y,
            float rx,
            float ry) {
        DrawArc(x, y, rx, ry, 0f, 360f);
    }

    internal void DrawCircle(float x, float y, float r, string pathOperator) {
        DrawEllipse(x, y, r, r, pathOperator);
    }

    /**
     * Draws an ellipse on the page and fills it using the current brush color.
     *
     * @param x the x coordinate of the center of the ellipse to be drawn.
     * @param y the y coordinate of the center of the ellipse to be drawn.
     * @param r1 the horizontal radius of the ellipse to be drawn.
     * @param r2 the vertical radius of the ellipse to be drawn.
     * @param operation the operation.
     */
    internal void DrawEllipse(
            float x,
            float y,
            float r1,
            float r2,
            string pathOperator) {
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

        Append(pathOperator);
        Append('\n');
    }

    /**
     * Draws a point on the page using the current pen color.
     *
     * @param p the point.
     */
    public void DrawPoint(Point p) {
        if (p.shape != Point.INVISIBLE) {
            List<Point> list;
            if (p.shape == Point.CIRCLE) {
                DrawCircle(p.x, p.y, p.r, p.GetPathOperator());
            } else if (p.shape == Point.DIAMOND) {
                list = new List<Point>();
                list.Add(new Point(p.x, p.y - p.r*1.2));
                list.Add(new Point(p.x + p.r*1.2, p.y));
                list.Add(new Point(p.x, p.y + p.r*1.2));
                list.Add(new Point(p.x - p.r*1.2, p.y));
                DrawPath(list, p.GetPathOperator());
            } else if (p.shape == Point.BOX) {
                list = new List<Point>();
                list.Add(new Point(p.x - p.r*0.886, p.y - p.r*0.886));
                list.Add(new Point(p.x + p.r*0.886, p.y - p.r*0.886));
                list.Add(new Point(p.x + p.r*0.886, p.y + p.r*0.886));
                list.Add(new Point(p.x - p.r*0.886, p.y + p.r*0.886));
                DrawPath(list, p.GetPathOperator());
            } else if (p.shape == Point.PLUS) {
                DrawLine(p.x - p.r, p.y, p.x + p.r, p.y);
                DrawLine(p.x, p.y - p.r, p.x, p.y + p.r);
            } else if (p.shape == Point.UP_ARROW) {
                list = new List<Point>();
                list.Add(new Point(p.x, p.y - p.r));
                list.Add(new Point(p.x + p.r, p.y + p.r));
                list.Add(new Point(p.x - p.r, p.y + p.r));
                list.Add(new Point(p.x, p.y - p.r));
                DrawPath(list, p.GetPathOperator());
            } else if (p.shape == Point.DOWN_ARROW) {
                list = new List<Point>();
                list.Add(new Point(p.x - p.r, p.y - p.r));
                list.Add(new Point(p.x + p.r, p.y - p.r));
                list.Add(new Point(p.x, p.y + p.r));
                list.Add(new Point(p.x - p.r, p.y - p.r));
                DrawPath(list, p.GetPathOperator());
            } else if (p.shape == Point.LEFT_ARROW) {
                list = new List<Point>();
                list.Add(new Point(p.x + p.r, p.y + p.r));
                list.Add(new Point(p.x - p.r, p.y));
                list.Add(new Point(p.x + p.r, p.y - p.r));
                list.Add(new Point(p.x + p.r, p.y + p.r));
                DrawPath(list, p.GetPathOperator());
            } else if (p.shape == Point.RIGHT_ARROW) {
                list = new List<Point>();
                list.Add(new Point(p.x - p.r, p.y - p.r));
                list.Add(new Point(p.x + p.r, p.y));
                list.Add(new Point(p.x - p.r, p.y + p.r));
                list.Add(new Point(p.x - p.r, p.y - p.r));
                DrawPath(list, p.GetPathOperator());
            } else if (p.shape == Point.H_DASH) {
                DrawLine(p.x - p.r, p.y, p.x + p.r, p.y);
            } else if (p.shape == Point.V_DASH) {
                DrawLine(p.x, p.y - p.r, p.x, p.y + p.r);
            } else if (p.shape == Point.X_MARK) {
                DrawLine(p.x - p.r, p.y - p.r, p.x + p.r, p.y + p.r);
                DrawLine(p.x - p.r, p.y + p.r, p.x + p.r, p.y - p.r);
            } else if (p.shape == Point.MULTIPLY) {
                DrawLine(p.x - p.r, p.y - p.r, p.x + p.r, p.y + p.r);
                DrawLine(p.x - p.r, p.y + p.r, p.x + p.r, p.y - p.r);
                DrawLine(p.x - p.r, p.y, p.x + p.r, p.y);
                DrawLine(p.x, p.y - p.r, p.x, p.y + p.r);
            } else if (p.shape == Point.STAR) {
                list = new List<Point>();
                for (int i = 0; i < 10; i++) {
                    double theta = i * 36 * (Math.PI / 180.0);
                    double radius = (i % 2 == 0) ? p.r*1.147f : p.r*0.38196f*1.147f;
                    double x = p.x + radius * Math.Sin(theta);
                    double y = p.y - radius * Math.Cos(theta);  // minus because y grows down
                    list.Add(new Point(x, y));
                }
                DrawPath(list, p.GetPathOperator());
            }
        }
    }

    /**
     * Sets the text rendering mode.
     *
     * @param mode the rendering mode.
     */
    public void SetTextRenderingMode(int mode) {
        if (mode >= 0 && mode <= 7) {
            this.renderingMode = mode;
        } else {
            throw new Exception("Invalid text rendering mode: " + mode);
        }
    }

    /**
     * Sets the text direction.
     *
     * @param degrees the angle.
     */
    public void SetTextDirection(int degrees) {
        if (degrees > 360) degrees %= 360;
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
        tm0 = FastFloat.ToByteArray(tmx[0]);
        tm1 = FastFloat.ToByteArray(tmx[1]);
        tm2 = FastFloat.ToByteArray(tmx[2]);
        tm3 = FastFloat.ToByteArray(tmx[3]);
    }

    /**
     * Draws a cubic bezier curve starting from the current point to the end point p3
     *
     * @param x1 first control point x
     * @param y1 first control point y
     * @param x2 second control point x
     * @param y2 second control point y
     * @param x3 end point x
     * @param y3 end point y
     */
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

    public float[] DrawCircularArc(
            float x, float y, float r, float startAngle, float sweepDegrees) {
        return DrawArc(x, y, r, r, startAngle, sweepDegrees);
    }

    public float[] DrawArc(
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

    /**
     * Draws a bezier curve starting from the current point.
     * <strong>Please note:</strong> You must call the StrokePath,
     * ClosePath or FillPath methods after the last BezierCurveTo call.
     * <p><i>Author:</i> <strong>Pieter Libin</strong>, pieter@emweb.be</p>
     *
     * @param p1 this first control point.
     * @param p2 this second control point.
     * @param p3 this end point.
     */
    public void BezierCurveTo(Point p1, Point p2, Point p3) {
        Append(p1);
        Append(p2);
        Append(p3);
        Append("c\n");
    }

    public void SetTextFont(Font font, float fontSize) {
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
    }

    // Code provided by:
    // Dominique Andre Gunia <contact@dgunia.de>
    // <<
    public void DrawRectRoundCorners(
            float x,
            float y,
            float w,
            float h,
            float r1,
            float r2,
            float[] fillColor,
            float strokeWidth,
            float[] strokeColor) {
        // The best 4-spline magic number
        float m4 = 0.55228f;
        List<Point> points = new List<Point>();

        // Starting point
        points.Add(new Point(x + w - r1, y));
        points.Add(new Point(x + w - r1 + m4*r1, y, Point.CONTROL_POINT));
        points.Add(new Point(x + w, y + r2 - m4*r2, Point.CONTROL_POINT));
        points.Add(new Point(x + w, y + r2));

        points.Add(new Point(x + w, y + h - r2));
        points.Add(new Point(x + w, y + h - r2 + m4*r2, Point.CONTROL_POINT));
        points.Add(new Point(x + w - m4*r1, y + h, Point.CONTROL_POINT));
        points.Add(new Point(x + w - r1, y + h));

        points.Add(new Point(x + r1, y + h));
        points.Add(new Point(x + r1 - m4*r1, y + h, Point.CONTROL_POINT));
        points.Add(new Point(x, y + h - m4*r2, Point.CONTROL_POINT));
        points.Add(new Point(x, y + h - r2));

        points.Add(new Point(x, y + r2));
        points.Add(new Point(x, y + r2 - m4*r2, Point.CONTROL_POINT));
        points.Add(new Point(x + m4*r1, y, Point.CONTROL_POINT));
        points.Add(new Point(x + r1, y));
        points.Add(new Point(x + w - r1, y));

        if (fillColor != null && strokeColor == null) {
            DrawPath(points, PathOperator.Fill);
        } else if (fillColor == null && strokeColor != null) {
            DrawPath(points, PathOperator.Stroke);
        } else if (fillColor != null && strokeColor != null) {
            DrawPath(points, PathOperator.FillAndStroke);
        }
    }

    /**
     * Clips the path.
     */
    public void ClipPath() {
        Append("W\n");
        Append("n\n");  // Close the path without painting it.
    }

    public void ClipRect(float x, float y, float w, float h) {
        MoveTo(x, y);
        LineTo(x + w, y);
        LineTo(x + w, y + h);
        LineTo(x, y + h);
        ClipPath();
    }

    [Obsolete("This method is deprecated. Use SaveGraphicsState() instead.")]
    public void Save() {
        SaveGraphicsState();
    }

    [Obsolete("This method is deprecated. Use RestoreGraphicsState() instead.")]
    public void Restore() {
        RestoreGraphicsState();
    }

    /**
     * Saves the graphics state. Please see Example_31.
     */
    public void SaveGraphicsState() {
        Append("q\n");
    }

    /**
     * Sets the graphics state. Please see Example_31.
     *
     * @param gs the graphics state to use.
     */
    public void SetGraphicsState(GraphicsState gs) {
        StringBuilder sb = new StringBuilder();
        sb.Append("/CA ");
        sb.Append(gs.GetAlphaStroking());
        sb.Append(" ");
        sb.Append("/ca ");
        sb.Append(gs.GetAlphaNonStroking());
        String state = sb.ToString();
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
    }

    /**
     * Restores the graphics state. Please see Example_31.
     */
    public void RestoreGraphicsState() {
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
        byte[] bytes = System.Text.Encoding.UTF8.GetBytes(str);
        buf.Write(bytes, 0, bytes.Length);
    }

    internal void Append(int num) {
        Append(num.ToString());
    }

    internal void Append(float f) {
        Append(FastFloat.ToByteArray(f));
    }

    internal void Append(char ch) {
        buf.WriteByte((byte) ch);
    }

    internal void Append(byte b) {
        buf.WriteByte(b);
    }

    /**
     * Appends the specified array of bytes to the page.
     */
    internal void Append(byte[] buffer) {
        buf.Write(buffer, 0, buffer.Length);
    }

    internal static readonly byte[] HEX = {
        (byte)'0', (byte)'1', (byte)'2', (byte)'3', (byte)'4', (byte)'5', (byte)'6', (byte)'7', (byte)'8', (byte)'9',
        (byte)'A', (byte)'B', (byte)'C', (byte)'D', (byte)'E', (byte)'F'
    };
    internal void AppendCodePointAsHex(int codePoint) {
        if (codePoint <= 0xFFFF) {
            // Basic Multilingual Plane (BMP) character
            buf.WriteByte(HEX[(codePoint >> 12) & 0xF]);
            buf.WriteByte(HEX[(codePoint >> 8)  & 0xF]);
            buf.WriteByte(HEX[(codePoint >> 4)  & 0xF]);
            buf.WriteByte(HEX[(codePoint)       & 0xF]);
        } else {
            // Supplementary character (needs surrogate pair in UTF-16)
            // Write as 6 hex digits (max Unicode code point is 0x10FFFF)
            buf.WriteByte(HEX[(codePoint >> 20) & 0xF]);
            buf.WriteByte(HEX[(codePoint >> 16) & 0xF]);
            buf.WriteByte(HEX[(codePoint >> 12) & 0xF]);
            buf.WriteByte(HEX[(codePoint >> 8)  & 0xF]);
            buf.WriteByte(HEX[(codePoint >> 4)  & 0xF]);
            buf.WriteByte(HEX[(codePoint)       & 0xF]);
        }
    }

    private void DrawWord(
            Font font,
            StringBuilder buf,
            float[] color,
            Dictionary<String, Int32> highlightColors) {
        if (buf.Length > 0) {
            String str = buf.ToString();
            if (highlightColors.ContainsKey(str.ToLower())) {
                SetBrushColor(highlightColors[str.ToLower()]);
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
        for (int i = 0; i < str.Length; i++) {
            char ch = str[i];
            if (Char.IsLetterOrDigit(ch)) {
                DrawWord(font, buf2, color, highlightColors);
                buf1.Append(ch);
            } else {
                DrawWord(font, buf1, color, highlightColors);
                buf2.Append(ch);
            }
        }
        DrawWord(font, buf1, color, highlightColors);
        DrawWord(font, buf2, color, highlightColors);
    }

    internal void SetStructElementsPageObjNumber(int pageObjNumber) {
        foreach (StructElem element in pdf.structElements) {
            element.pageObjNumber = pageObjNumber;
        }
    }

    internal void AddBMC(
            String structure,
            String actualText,
            String altDescription) {
        AddBMC(structure, "en-US", actualText, altDescription);
    }

    internal void AddBMC(
            String structure,
            String language,
            String actualText,
            String altDescription) {
        if (pdf.compliance == Compliance.PDF_UA_1) {
            if (actualText != null && altDescription != null) {
                StructElem element = new StructElem();
                element.structure = structure;
                element.mcid = mcid;
                element.language = language;
                element.actualText = actualText;
                element.altDescription = altDescription;
                pdf.structElements.Add(element);

                Append("/");
                Append(structure);
                Append(" <</MCID ");
                Append(mcid++);
                Append(">>\n");
                Append("BDC\n");
            } else {
                Append("/Artifact BMC\n");
            }
        }
    }

    internal void AddArtifactBMC() {
        if (pdf.compliance == Compliance.PDF_UA_1) {
            Append("/Artifact BMC\n");
        }
    }

    internal void AddEMC() {
        if (pdf.compliance == Compliance.PDF_UA_1) {
            Append("EMC\n");
        }
    }

    internal void BeginTransform(
            float x, float y, float xScale, float yScale) {
        Append("q\n");

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
        Append("Q\n");
    }

    public void DrawContents(
            byte[] content,
            float h,    // The height of the graphics object in points.
            float x,
            float y,
            float xScale,
            float yScale) {
        BeginTransform(x, (this.height - yScale * h) - y, xScale, yScale);
        Append(content);
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
        for (int i = 0; i < str.Length; i++) {
            DrawString(font, fontSize, str.Substring(i, 1), x1, y);
            x1 += dx;
        }
    }

    /**
     * Sets the text location.
     *
     * @param x the x coordinate of new text location.
     * @param y the y coordinate of new text location.
     */
    internal void SetTextLocation(float x, float y) {
        Append(x);
        Append(Token.Space);
        Append(height - y);
        Append(" Td\n");
    }

    /**
     * Sets the text leading.
     * @param leading the leading.
     */
    internal void SetTextLeading(float leading) {
        Append(leading);
        Append(" TL\n");
    }

    /**
     * Advance to the next line.
     */
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
    }

    /**
     * Draws a string at the specified location.
     * @param str the string.
     */
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

    internal void ScaleAndRotate(float x, float y, float w, float h, float degrees) {
        // PDF transformations apply LAST-TO-FIRST (like a stack: last command = first applied)

        // [FINAL POSITIONING - Applied Last]
        // Moves rotated/scaled image to target (x,y) on page
        Append("1 0 0 1 ");
        Append(x + w/2);
        Append(" ");
        Append((height - y) - h/2);
        Append(" cm\n");

        // [ROTATION - Applied Second]
        // Rotates around current origin (0,0) by 'degrees'
        double radians = degrees * (Math.PI / 180);
        float cos = (float)Math.Cos(radians);
        float sin = (float)Math.Sin(radians);
        Append(FastFloat.ToByteArray(cos));
        Append(" ");
        Append(FastFloat.ToByteArray(sin));
        Append(" ");
        Append(FastFloat.ToByteArray(-sin));
        Append(" ");
        Append(FastFloat.ToByteArray(cos));
        Append(" 0 0 cm\n");

        // [ORIGIN SETUP - Applied First]
        // Centers image at (0,0) and sets scale
        Append(w);
        Append(" 0 0 ");
        Append(h);
        Append(" ");
        Append(-w/2);
        Append(" ");
        Append(-h/2);
        Append(" cm\n");
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
        Append(FastFloat.ToByteArray(cos));
        Append(" ");
        Append(FastFloat.ToByteArray(sin));
        Append(" ");
        Append(FastFloat.ToByteArray(-sin));
        Append(" ");
        Append(FastFloat.ToByteArray(cos));
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
            StructElem element = new StructElem();
            element.structure = StructElem.LINK;
            element.language = annotation.language;
            element.actualText = annotation.actualText;
            element.altDescription = annotation.altDescription;
            element.annotation = annotation;
            pdf.structElements.Add(element);
        }
    }

    internal void DrawTextBlock(
            Font font,
            float fontSize,
            TextLine[] textLines,
            float x,
            float y,
            float leading,
            float[] color,
            Dictionary<String, Int32> highlightColors) {
        if (textLines == null || textLines.Length == 0) {
            return;
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
            if (highlightColors == null) {
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

        float yLine = y + font.GetBodyHeight(fontSize);
        foreach (TextLine textLine in textLines) {
            if (textLine.underline) {
                MoveTo(x + textLine.xOffset, yLine);
                LineTo(x + textLine.xOffset + font.StringWidth(fontSize, textLine.text), yLine);
                StrokePath();
            }
            yLine += leading;
        }
    }
}   // End of Page.cs
}   // End of namespace PDFjet.NET
