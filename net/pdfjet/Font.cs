/*
 * Font.cs
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
using System;
using System.IO;
using System.Text;
using System.Collections.Generic;
using PDFjet.NET.CoreFonts;

namespace PDFjet.NET {
/// <summary>
/// Used to create font objects.
/// The font objects must be added to the PDF before they can be used to draw text.
/// </summary>
public class Font {
    /// <summary>
    ///  Is this a stream font?
    /// </summary>
    public const bool STREAM = true;

    internal String name;
    internal String info;
    internal int objNumber;
    internal String fontID;

    // The object number of the embedded font file
    internal int fileObjNumber;
    internal int fontDescriptorObjNumber;
    internal int cidFontDictObjNumber;
    internal int toUnicodeCMapObjNumber;

    // Font attributes
    internal int unitsPerEm = 1000;     // The default for core fonts.
    internal int fontAscent;
    internal int fontDescent;
    internal int bBoxLLx;
    internal int bBoxLLy;
    internal int bBoxURx;
    internal int bBoxURy;
    internal int firstChar = 32;        // The default for core fonts.
    internal int lastChar = 255;        // The default for core fonts.
    internal int capHeight;
    internal int fontUnderlinePosition;
    internal int fontUnderlineThickness;
    internal int[] advanceWidth;
    internal int[] unicodeToGID;
    internal bool cff;
    internal int compressedSize;
    internal int uncompressedSize;
    internal int[][] metrics;           // Only used for core fonts.

    // Don't change the following default values!
    internal float size = 12.0f;
    internal bool isCoreFont = false;
    internal bool isCJK = false;
    internal bool skew15 = false;
    internal bool kernPairs = false;

    internal float ascent;
    internal float descent;
    private float bodyHeight;
    private float underlinePosition;
    private float underlineThickness;

    /// <summary>
    /// Constructor for the 14 standard fonts.
    /// Creates a font object and adds it to the PDF.
    ///
    /// <code>
    /// Examples:
    ///     Font font1 = new Font(pdf, CoreFont.HELVETICA);
    ///     Font font2 = new Font(pdf, CoreFont.TIMES_ITALIC);
    ///     Font font3 = new Font(pdf, CoreFont.ZAPF_DINGBATS);
    ///      ...
    /// </code>
    /// </summary>
    /// <param name="pdf">the PDF to add this font to.</param>
    /// <param name="coreFont">the core font. Must be one the names defined in the CoreFont class.</param>
    public Font(PDF pdf, int coreFont) {
        CoreFont font = new CoreFont(coreFont);
        this.isCoreFont = true;
        this.name = font.name;
        this.bBoxLLx = font.bBoxLLx;
        this.bBoxLLy = font.bBoxLLy;
        this.bBoxURx = font.bBoxURx;
        this.bBoxURy = font.bBoxURy;
        this.metrics = font.metrics;
        this.fontUnderlinePosition = font.underlinePosition;
        this.fontUnderlineThickness = font.underlineThickness;
        this.fontAscent = font.bBoxURy;
        this.fontDescent = font.bBoxLLy;
        SetSize(size);

        pdf.NewObj();
        pdf.Append("<<\n");
        pdf.Append("/Type /Font\n");
        pdf.Append("/Subtype /Type1\n");
        pdf.Append("/BaseFont /");
        pdf.Append(this.name);
        pdf.Append('\n');
        if (!this.name.Equals("Symbol") && !this.name.Equals("ZapfDingbats")) {
            pdf.Append("/Encoding /WinAnsiEncoding\n");
        }
        pdf.Append(">>\n");
        pdf.EndObj();
        objNumber = pdf.GetObjNumber();

        pdf.fonts.Add(this);
    }

    // Used by PDFobj
    internal Font(int coreFont) {
        CoreFont font = new CoreFont(coreFont);
        this.isCoreFont = true;
        this.name = font.name;
        this.bBoxLLx = font.bBoxLLx;
        this.bBoxLLy = font.bBoxLLy;
        this.bBoxURx = font.bBoxURx;
        this.bBoxURy = font.bBoxURy;
        this.metrics = font.metrics;
        this.fontUnderlinePosition = font.underlinePosition;
        this.fontUnderlineThickness = font.underlineThickness;
        this.fontAscent = font.bBoxURy;
        this.fontDescent = font.bBoxLLy;
        SetSize(size);
    }

    // Constructor for CJK fonts
    /// <summary>Creates a Chinese, Japanese or Korean font and adds it to the PDF.</summary>
    public Font(PDF pdf, CJKFont font) {
        String fontName = null;
        if (font == CJKFont.ADOBE_MING_STD_LIGHT) {             // Chinese (Traditional) font
            fontName = "AdobeMingStd-Light";
        } else if (font == CJKFont.ST_HEITI_SC_LIGHT) {         // Chinese (Simplified) font
            fontName = "STHeitiSC-Light";
        } else if (font == CJKFont.KOZ_MIN_PRO_VI_REGULAR) {    // Japanese font
            fontName = "KozMinProVI-Regular";
        } else if (font == CJKFont.ADOBE_MYUNGJO_STD_MEDIUM) {  // Korean font
            fontName = "AdobeMyungjoStd-Medium";
        }
        this.name = fontName;
        this.isCJK = true;
        this.firstChar = 0x0020;
        this.lastChar = 0xFFEE;
        this.ascent = this.size;
        this.descent = this.size/4;
        this.bodyHeight = this.ascent + this.descent;

        // Font Descriptor
        pdf.NewObj();
        pdf.Append("<<\n");
        pdf.Append("/Type /FontDescriptor\n");
        pdf.Append("/FontName /");
        pdf.Append(fontName);
        pdf.Append('\n');
        pdf.Append("/Flags 4\n");
        pdf.Append("/FontBBox [0 0 0 0]\n");
        pdf.Append(">>\n");
        pdf.EndObj();

        // CIDFont Dictionary
        pdf.NewObj();
        pdf.Append(Token.BeginDictionary);
        pdf.Append("/Type /Font\n");
        pdf.Append("/Subtype /CIDFontType0\n");
        pdf.Append("/BaseFont /");
        pdf.Append(fontName);
        pdf.Append('\n');
        pdf.Append("/FontDescriptor ");
        pdf.Append(pdf.GetObjNumber() - 1);
        pdf.Append(" 0 R\n");
        pdf.Append("/CIDSystemInfo <<\n");
        pdf.Append("/Registry (Adobe)\n");
        if (fontName.StartsWith("AdobeMingStd")) {
            pdf.Append("/Ordering (CNS1)\n");
            pdf.Append("/Supplement 4\n");
        } else if (fontName.StartsWith("AdobeSongStd")
                || fontName.StartsWith("STHeitiSC")) {
            pdf.Append("/Ordering (GB1)\n");
            pdf.Append("/Supplement 4\n");
        } else if (fontName.StartsWith("KozMinPro")) {
            pdf.Append("/Ordering (Japan1)\n");
            pdf.Append("/Supplement 4\n");
        } else if (fontName.StartsWith("AdobeMyungjoStd")) {
            pdf.Append("/Ordering (Korea1)\n");
            pdf.Append("/Supplement 1\n");
        } else {
            throw new Exception("Unsupported font: " + fontName);
        }
        pdf.Append(">>\n");
        pdf.Append(Token.EndDictionary);
        pdf.EndObj();

        // Type0 Font Dictionary
        pdf.NewObj();
        pdf.Append(Token.BeginDictionary);
        pdf.Append("/Type /Font\n");
        pdf.Append("/Subtype /Type0\n");
        pdf.Append("/BaseFont /");
        if (fontName.StartsWith("AdobeMingStd")) {
            pdf.Append(fontName + "-UniCNS-UTF16-H\n");
            pdf.Append("/Encoding /UniCNS-UTF16-H\n");
        } else if (fontName.StartsWith("AdobeSongStd")
                || fontName.StartsWith("STHeitiSC")) {
            pdf.Append(fontName + "-UniGB-UTF16-H\n");
            pdf.Append("/Encoding /UniGB-UTF16-H\n");
        } else if (fontName.StartsWith("KozMinPro")) {
            pdf.Append(fontName + "-UniJIS-UCS2-H\n");
            pdf.Append("/Encoding /UniJIS-UCS2-H\n");
        } else if (fontName.StartsWith("AdobeMyungjoStd")) {
            pdf.Append(fontName + "-UniKS-UCS2-H\n");
            pdf.Append("/Encoding /UniKS-UCS2-H\n");
        } else {
            throw new Exception("Unsupported font: " + fontName);
        }
        pdf.Append("/DescendantFonts [");
        pdf.Append(pdf.GetObjNumber() - 1);
        pdf.Append(" 0 R]\n");
        pdf.Append(Token.EndDictionary);
        pdf.EndObj();
        objNumber = pdf.GetObjNumber();

        pdf.fonts.Add(this);
    }

    // Constructor for .ttf.stream fonts:
    /// <summary>Creates a font from a .ttf.stream or .otf.stream font and adds it to the PDF.</summary>
    public Font(PDF pdf, Stream inputStream, bool flag) {
        FontStream1.Register(pdf, this, inputStream);
        SetSize(size);
    }

    // Constructor for .ttf.stream fonts:
    /// <summary>Creates a font from a .ttf.stream or .otf.stream font and adds it to the objects of an existing PDF.</summary>
    public Font(List<PDFobj> objects, Stream inputStream, bool flag) {
        FontStream2.Register(objects, this, inputStream);
        SetSize(size);
    }

    /// <summary>
    /// Constructor for OpenType and TrueType fonts.
    /// </summary>
    /// <param name="pdf">the PDF object that requires this font.</param>
    /// <param name="inputStream">the input stream to read this font from.</param>
    public Font(PDF pdf, System.IO.Stream inputStream) {
        OpenTypeFont.Register(pdf, this, inputStream);
        SetSize(size);
    }

    /// <summary>
    /// Constructor for OpenType, TrueType and .otf.stream and .ttf.stream fonts.
    /// </summary>
    /// <param name="pdf">the pdf object.</param>
    /// <param name="fontPath">the font path.</param>
    /// <exception cref="System.Exception">thrown if the font file is not found.</exception>
    public Font(PDF pdf, String fontPath) {
        FileStream inputStream = new FileStream(fontPath, FileMode.Open, FileAccess.Read);
        if (fontPath.EndsWith(".stream")) {
            FontStream1.Register(pdf, this, inputStream);
        } else {
            OpenTypeFont.Register(pdf, this, inputStream);
        }
        SetSize(size);
    }

    /// <summary>
    /// Sets the size of this font.
    /// </summary>
    /// <param name="fontSize">specifies the size of this font.</param>
    /// <returns>the font.</returns>
    public Font SetSize(double fontSize) {
        return SetSize((float) fontSize);
    }

    /// <summary>
    /// Sets the size of this font.
    /// </summary>
    /// <param name="fontSize">specifies the size of this font.</param>
    /// <returns>the font.</returns>
    public Font SetSize(float fontSize) {
        this.size = fontSize;
        if (isCJK) {
            this.ascent = size;
            this.descent = size/4;
            this.bodyHeight = this.ascent + this.descent;
            return this;
        }
        this.ascent = fontAscent * size / unitsPerEm;
        this.descent = -fontDescent * size / unitsPerEm;
        this.bodyHeight = this.ascent + this.descent;
        this.underlineThickness = (fontUnderlineThickness * size / unitsPerEm);
        this.underlinePosition = -(fontUnderlinePosition * size / unitsPerEm) + underlineThickness / 2.0f;
        return this;
    }

    /// <summary>Returns the font size.</summary>
    public float GetSize() {
        return size;
    }

    /// <summary>Returns the name of the font.</summary>
    public String GetName() {
        return this.name;
    }

    /// <summary>Enables or disables kerning. Kerning is only supported for the 14 standard fonts.</summary>
    public Font SetKernPairs(bool kernPairs) {
        this.kernPairs = kernPairs;
        return this;
    }

    /// <summary>Returns the width of the string at the current font size.</summary>
    public float StringWidth(String str) {
        return StringWidth(this.size, str);
    }

    /// <summary>Returns the width of the string at the specified font size.</summary>
    public float StringWidth(float fontSize, String str) {
        float width = 0.0f;

        if (str == null) {
            return width;
        }

        if (isCJK) {
            return str.Length * ascent;
        }

        if (isCoreFont) {
            for (int i = 0; i < str.Length; i++) {
                int c1 = str[i];
                if (c1 < firstChar || c1 > lastChar) {
                    c1 = 0x20;
                }
                c1 -= 32;
                width += metrics[c1][1];
                if (kernPairs && i < (str.Length - 1)) {
                    int c2 = str[i + 1];
                    if (c2 < firstChar || c2 > lastChar) {
                        c2 = 32;
                    }
                    for (int j = 2; j < metrics[c1].Length; j += 2) {
                        if (metrics[c1][j] == c2) {
                            width += metrics[c1][j + 1];
                            break;
                        }
                    }
                }
            }
        } else {
            foreach (int c1 in str) {
                if (unicodeToGID[c1] < advanceWidth.Length) {
                    width += advanceWidth[unicodeToGID[c1]];
                } else {
                    width += advanceWidth[0];
                }
            }
        }

        return width * fontSize / unitsPerEm;
    }

    /// <summary>Returns the ascent at the current font size.</summary>
    public float GetAscent() {
        return ascent;
    }

    /// <summary>Returns the descent at the current font size.</summary>
    public float GetDescent() {
        return descent;
    }

    /// <summary>Returns the ascent at the specified font size.</summary>
    public float GetAscent(float fontSize) {
        if (isCJK) {
            return fontSize;
        }
        return fontAscent * fontSize / unitsPerEm;
    }

    /// <summary>Returns the descent at the specified font size.</summary>
    public float GetDescent(float fontSize) {
        if (isCJK) {
            return fontSize/4;
        }
        return -fontDescent * fontSize / unitsPerEm;
    }

    /// <summary>Returns the ascent plus the descent at the specified font size.</summary>
    public float GetBodyHeight(float fontSize) {
        return GetAscent(fontSize) + GetDescent(fontSize);
    }

    /// <summary>
    /// Returns the height of the body of this font.
    /// </summary>
    /// <returns>the height of the body of the font.</returns>
    public float GetBodyHeight() {
        return bodyHeight;
    }

    /// <summary>
    /// Returns the height of this font.
    /// </summary>
    /// <returns>the height of the font.</returns>
    public float GetHeight() {
        return ascent + descent;
    }

    /// <summary>Returns the underline thickness at the specified font size.</summary>
    public float GetUnderlineThickness(float fontSize) {
        return (fontUnderlineThickness * fontSize / unitsPerEm);
    }

    /// <summary>Returns the underline position at the specified font size.</summary>
    public float GetUnderlinePosition(float fontSize) {
        return  -(fontUnderlinePosition * fontSize / unitsPerEm) + underlineThickness / 2.0f;
    }

    /// <summary>Returns how many characters of the string fit within the specified width.</summary>
    public int GetFitChars(String str, double width) {
        return GetFitChars(str, (float) width);
    }

    /// <summary>Returns how many characters of the string fit within the specified width.</summary>
    public int GetFitChars(String str, float width) {
        float w = width * unitsPerEm / size;
        if (isCJK) {
            return (int) (w / ascent);
        }
        if (isCoreFont) {
            return GetCoreFontFitChars(str, w);
        }
        int i;
        for (i = 0; i < str.Length; i++) {
            int c1 = str[i];
            if (c1 < firstChar || c1 > lastChar) {
                w -= advanceWidth[0];
            } else {
                w -= advanceWidth[unicodeToGID[c1]];
            }
            if (w < 0) break;
        }
        return i;
    }

    private int GetCoreFontFitChars(String str, float width) {
        float w = width;

        int i = 0;
        while (i < str.Length) {
            int c1 = str[i];
            if (c1 < firstChar || c1 > lastChar) {
                c1 = 32;
            }

            c1 -= 32;
            w -= metrics[c1][1];
            if (w < 0) {
                return i;
            }
            if (kernPairs && i < (str.Length - 1)) {
                int c2 = str[i + 1];
                if (c2 < firstChar || c2 > lastChar) {
                    c2 = 32;
                }
                for (int j = 2; j < metrics[c1].Length; j += 2) {
                    if (metrics[c1][j] == c2) {
                        w -= metrics[c1][j + 1];
                        if (w < 0) {
                            return i;
                        }
                        break;
                    }
                }
            }

            i += 1;
        }

        return i;
    }

   /// <summary>
   /// Sets the skew15 private variable.
   /// When the variable is set to 'true' all glyphs in the font are skewed on 15 degrees.
   /// This makes a regular font look like an italic type font.
   /// Use this method when you don't have real italic font in the font family,
   /// or when you want to generate smaller PDF files.
   /// For example you could embed only the Regular and Bold fonts and synthesize the RegularItalic and BoldItalic.
   /// </summary>
   /// <param name="skew15">the skew flag.</param>
   /// <returns>this Font object.</returns>
    public Font SetItalic(bool skew15) {
        this.skew15 = skew15;
        return this;
    }

    /// <summary>
    /// Returns the width of the string at the current font size,
    /// using the fallback font for characters this font does not have.
    /// </summary>
    public float StringWidth(Font fallbackFont, String str) {
        return StringWidth(fallbackFont, this.size, str);
    }

    /// <summary>
    /// Returns the width of a string drawn using two fonts.
    /// </summary>
    /// <param name="fallbackFont">the fallback font.</param>
    /// <param name="fontSize">the font size.</param>
    /// <param name="str">the string.</param>
    /// <returns>the width.</returns>
    public float StringWidth(Font fallbackFont, float fontSize, String str) {
        float width = 0f;

        if (this.isCoreFont || this.isCJK || fallbackFont == null || fallbackFont.isCoreFont || fallbackFont.isCJK) {
            return StringWidth(fontSize, str);
        }

        Font activeFont = this;
        StringBuilder buf = new StringBuilder();
        foreach (int ch in str) {
            if (activeFont.unicodeToGID[ch] == 0) {
                width += activeFont.StringWidth(fontSize, buf.ToString());
                buf.Length = 0;
                // Switch the active font
                if (activeFont == this) {
                    activeFont = fallbackFont;
                } else {
                    activeFont = this;
                }
            }
            buf.Append((char) ch);
        }
        width += activeFont.StringWidth(fontSize, buf.ToString());

        return width;
    }
}   // End of Font.cs
}   // End of namespace PDFjet.NET
