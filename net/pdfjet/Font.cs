/**
 *  Font.cs
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
using System.IO;
using System.Text;
using System.Collections.Generic;


namespace PDFjet.NET {
public class Font {

    // Chinese (Traditional) font
    public const String AdobeMingStd_Light = "AdobeMingStd-Light";

    // Chinese (Simplified) font
    public const String STHeitiSC_Light = "STHeitiSC-Light";

    // Japanese font
    public const String KozMinProVI_Regular = "KozMinProVI-Regular";

    // Korean font
    public const String AdobeMyungjoStd_Medium = "AdobeMyungjoStd-Medium";

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
    internal int[] glyphWidth;
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
    internal float bodyHeight;
    internal float underlinePosition;
    internal float underlineThickness;


    /**
     *  Constructor for the 14 standard fonts.
     *  Creates a font object and adds it to the PDF.
     *
     *  <pre>
     *  Examples:
     *      Font font1 = new Font(pdf, CoreFont.HELVETICA);
     *      Font font2 = new Font(pdf, CoreFont.TIMES_ITALIC);
     *      Font font3 = new Font(pdf, CoreFont.ZAPF_DINGBATS);
     *      ...
     *  </pre>
     *
     *  @param pdf the PDF to add this font to.
     *  @param coreFont the core font. Must be one the names defined in the CoreFont class.
     */
    public Font(PDF pdf, CoreFont coreFont) {
        StandardFont font = StandardFont.GetInstance(coreFont);
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

        pdf.Newobj();
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
        pdf.Endobj();
        objNumber = pdf.GetObjNumber();

        pdf.fonts.Add(this);
    }


    // Used by PDFobj
    internal Font(CoreFont coreFont) {
        StandardFont font = StandardFont.GetInstance(coreFont);
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
    public Font(PDF pdf, String fontName) {
        this.name = fontName;
        this.isCJK = true;
        this.firstChar = 0x0020;
        this.lastChar = 0xFFEE;
        this.ascent = this.size;
        this.descent = this.size/4;
        this.bodyHeight = this.ascent + this.descent;

        // Font Descriptor
        pdf.Newobj();
        pdf.Append("<<\n");
        pdf.Append("/Type /FontDescriptor\n");
        pdf.Append("/FontName /");
        pdf.Append(fontName);
        pdf.Append('\n');
        pdf.Append("/Flags 4\n");
        pdf.Append("/FontBBox [0 0 0 0]\n");
        pdf.Append(">>\n");
        pdf.Endobj();

        // CIDFont Dictionary
        pdf.Newobj();
        pdf.Append("<<\n");
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
        pdf.Append(">>\n");
        pdf.Endobj();

        // Type0 Font Dictionary
        pdf.Newobj();
        pdf.Append("<<\n");
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
        pdf.Append(">>\n");
        pdf.Endobj();
        objNumber = pdf.GetObjNumber();

        pdf.fonts.Add(this);
    }


    // Constructor for .ttf.stream fonts:
    public Font(PDF pdf, Stream inputStream, bool flag) {
        FontStream1.Register(pdf, this, inputStream);
        SetSize(size);
    }


    // Constructor for .ttf.stream fonts:
    public Font(List<PDFobj> objects, Stream inputStream, bool flag) {
        FontStream2.Register(objects, this, inputStream);
        SetSize(size);
    }


    /**
     *  Constructor for OpenType and TrueType fonts.
     *  Please see Example_06 and Example_07.
     *
     *  @param pdf the PDF object that requires this font.
     *  @param inputStream the input stream to read this font from.
     */
/* TODO:
    public Font(PDF pdf, System.IO.Stream inputStream) {
        OpenTypeFont.Register(pdf, this, inputStream);
        SetSize(size);
    }
*/

    public Font SetSize(float fontSize) {
        this.size = fontSize;
        if (isCJK) {
            this.ascent = size;
            this.descent = ascent/4;
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


    public float GetSize() {
        return size;
    }


    public void SetKernPairs(bool kernPairs) {
        this.kernPairs = kernPairs;
    }


    public float StringWidth(String str) {
        if (str == null) {
            return 0.0f;
        }

        int width = 0;
        if (isCJK) {
            return str.Length * ascent;
        }

        for (int i = 0; i < str.Length; i++) {
            int c1 = str[i];
            if (isCoreFont) {
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
            else {
                if (c1 < firstChar || c1 > lastChar) {
                    width += advanceWidth[0];
                }
                else {
                    width += glyphWidth[c1];
                }
            }
        }

        return width * size / unitsPerEm;
    }


    public float GetAscent() {
        return ascent;
    }


    public float GetDescent() {
        return descent;
    }


    public float GetHeight() {
        return ascent + descent;
    }


    public float GetBodyHeight() {
        return bodyHeight;
    }


    public int GetFitChars(String str, double width) {
        return GetFitChars(str, (float) width);
    }


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
            }
            else {
                w -= glyphWidth[c1];
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


   /**
    * Sets the skew15 private variable.
    * When the variable is set to 'true' all glyphs in the font are skewed on 15 degrees.
    * This makes a regular font look like an italic type font.
    * Use this method when you don't have real italic font in the font family,
    * or when you want to generate smaller PDF files.
    * For example you could embed only the Regular and Bold fonts and synthesize the RegularItalic and BoldItalic.
    *
    * @param skew15 the skew flag.
    */
    public void SetItalic(bool skew15) {
        this.skew15 = skew15;
    }


    /**
     * Returns the width of a string drawn using two fonts.
     *
     * @param fallbackFont the fallback font.
     * @param str the string.
     * @return the width.
     */
    public float StringWidth(Font fallbackFont, String str) {
        float width = 0f;

        if (this.isCoreFont || this.isCJK || fallbackFont == null || fallbackFont.isCoreFont || fallbackFont.isCJK) {
            return StringWidth(str);
        }

        Font activeFont = this;
        StringBuilder buf = new StringBuilder();
        for (int i = 0; i < str.Length; i++) {
            int ch = str[i];
            if (activeFont.unicodeToGID[ch] == 0) {
                width += activeFont.StringWidth(buf.ToString());
                buf.Length = 0;
                // Switch the active font
                if (activeFont == this) {
                    activeFont = fallbackFont;
                }
                else {
                    activeFont = this;
                }
            }
            buf.Append((char) ch);
        }
        width += activeFont.StringWidth(buf.ToString());

        return width;
    }

}   // End of Font.cs
}   // End of namespace PDFjet.NET
