/*
 * Font.cs
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
using System;
using System.IO;
using System.Collections.Generic;

namespace PDFjet.NET {
/// <summary>
/// Used to create font objects.
/// The font objects must be added to the PDF before they can be used to draw text.
/// </summary>
public class Font {

    internal String name;
    internal String info;
    internal int objNumber;
    internal String fontID;
    // The PDF the font was added to, or null for a font of an existing PDF.
    internal PDF pdf;

    // The object number of the embedded font file
    internal int fileObjNumber;
    internal int fontDescriptorObjNumber;
    internal int cidFontDictObjNumber;
    internal int toUnicodeCMapObjNumber;
    internal int cidSetObjNumber;           // The CIDSet of a subset in PDF/A-1
    internal String baseFont;               // The name it is embedded under, with the tag of a subset
    internal Subset.Program program;        // A TrueType font program, until Complete()
    internal FontObjects objects;           // Of a font added to an existing PDF, until they are added
    internal bool[] kept;                   // The glyphs its subset keeps

    // Font attributes
    internal int unitsPerEm = 1000;     // The default for core fonts.
    internal int fontAscent;
    internal int fontDescent;
    internal int fontLineGap;          // The space the font puts between its lines.
    internal int italicAngle;          // The italic angle of the post table, as a 16.16 fixed number.
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
    // The offsets of the marks that attach to other marks, in font units, by
    // the glyph IDs of the two marks, or null. A font has them from its GPOS
    // table, read from a .otf or .ttf file or from a stream that keeps them.
    internal Dictionary<int, int[]> markToMarkOffsets;
    // The anchors of the marks, by glyph ID, for each MarkToBase and
    // MarkToLigature lookup subtable, or null: the class and anchor of each
    // mark. A font has them from its GPOS table, read from a .otf or .ttf file
    // or from a stream that keeps them.
    internal List<Dictionary<int, int[]>> markAnchors;
    // The anchors of the letters and ligatures that the marks go on, by glyph
    // ID, for each subtable in markAnchors, or null: 1 and an anchor, or 0 and
    // no anchor, for each class of marks.
    internal List<Dictionary<int, int[]>> baseAnchors;
    internal bool cff;
    internal int[][] metrics;           // Only used for core fonts.
    // Tells the font program this font was read from apart from every other,
    // so that a PDF embeds each one once.
    internal ulong checksum;

    // Don't change the following default values!
    internal float size = 12.0f;
    internal bool isCoreFont = false;
    internal bool isCJK = false;
    internal bool skew15 = false;
    internal bool kernPairs = false;
    // True for Symbol and ZapfDingbats, whose characters are the codes of
    // their own encodings, not WinAnsi.
    private bool symbolic = false;

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
        this.pdf = pdf;
        CoreFont font = new CoreFont(coreFont);
        this.isCoreFont = true;
        this.name = font.name;
        this.bBoxLLx = font.bBoxLLx;
        this.bBoxLLy = font.bBoxLLy;
        this.bBoxURx = font.bBoxURx;
        this.bBoxURy = font.bBoxURy;
        this.metrics = font.metrics;
        this.symbolic = font.name.Equals("Symbol") || font.name.Equals("ZapfDingbats");
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
        this.symbolic = font.name.Equals("Symbol") || font.name.Equals("ZapfDingbats");
        this.fontUnderlinePosition = font.underlinePosition;
        this.fontUnderlineThickness = font.underlineThickness;
        this.fontAscent = font.bBoxURy;
        this.fontDescent = font.bBoxLLy;
        SetSize(size);
    }

    // Constructor for CJK - Chinese, Japanese and Korean fonts. The font is
    // not embedded: the viewer needs the Adobe Asian font pack, and a PDF with
    // it cannot be PDF/A or PDF/UA. Embedded fonts, such as IBM Plex Sans JP,
    // KR, SC and TC, are better: see Example_02 and Example_04.
    /// <summary>Creates a Chinese, Japanese or Korean font and adds it to the PDF.</summary>
    [Obsolete("Not embedded: the viewer needs the Adobe Asian font pack, and the PDF cannot be PDF/A or PDF/UA. Latin letters and spaces are drawn full width in it. Use an embedded font, such as IBM Plex Sans JP, KR, SC or TC. To be removed in v10.")]
    public Font(PDF pdf, CJKFont font) {
        this.pdf = pdf;
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
        if (fontName.StartsWith("AdobeMingStd", StringComparison.Ordinal)) {
            pdf.Append("/Ordering (CNS1)\n");
            pdf.Append("/Supplement 4\n");
        } else if (fontName.StartsWith("AdobeSongStd", StringComparison.Ordinal)
                || fontName.StartsWith("STHeitiSC", StringComparison.Ordinal)) {
            pdf.Append("/Ordering (GB1)\n");
            pdf.Append("/Supplement 4\n");
        } else if (fontName.StartsWith("KozMinPro", StringComparison.Ordinal)) {
            pdf.Append("/Ordering (Japan1)\n");
            pdf.Append("/Supplement 4\n");
        } else if (fontName.StartsWith("AdobeMyungjoStd", StringComparison.Ordinal)) {
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
        if (fontName.StartsWith("AdobeMingStd", StringComparison.Ordinal)) {
            pdf.Append(fontName + "-UniCNS-UTF16-H\n");
            pdf.Append("/Encoding /UniCNS-UTF16-H\n");
        } else if (fontName.StartsWith("AdobeSongStd", StringComparison.Ordinal)
                || fontName.StartsWith("STHeitiSC", StringComparison.Ordinal)) {
            pdf.Append(fontName + "-UniGB-UTF16-H\n");
            pdf.Append("/Encoding /UniGB-UTF16-H\n");
        } else if (fontName.StartsWith("KozMinPro", StringComparison.Ordinal)) {
            pdf.Append(fontName + "-UniJIS-UCS2-H\n");
            pdf.Append("/Encoding /UniJIS-UCS2-H\n");
        } else if (fontName.StartsWith("AdobeMyungjoStd", StringComparison.Ordinal)) {
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

    /// <summary>
    /// Creates a font from an OpenType or TrueType font, a .otf or a .ttf, and
    /// adds it to the objects of an existing PDF. It is embedded as a subset of
    /// the glyphs drawn, when the objects are added to the PDF, unless
    /// SetSubset(false) keeps it whole.
    /// </summary>
    public Font(List<PDFobj> objects, Stream inputStream) {
        OpenTypeFont.Register(objects, this, inputStream);
        inputStream.Dispose();
        SetSize(size);
    }

    /// <summary>
    /// Constructor for OpenType and TrueType fonts, .otf and .ttf.
    /// </summary>
    /// <param name="pdf">the PDF object that requires this font.</param>
    /// <param name="inputStream">the input stream to read this font from.</param>
    public Font(PDF pdf, System.IO.Stream inputStream) {
        this.pdf = pdf;
        OpenTypeFont.Register(pdf, this, inputStream);
        SetSize(size);
    }

    /// <summary>
    /// Constructor for OpenType and TrueType fonts, .otf and .ttf files. A path
    /// to a .ttf.stream or .otf.stream file, the fonts PDFjet shipped before
    /// 9.0.5, opens the .ttf or else the .otf file of the same name beside it.
    /// </summary>
    /// <param name="pdf">the pdf object.</param>
    /// <param name="fontPath">the font path.</param>
    /// <exception cref="System.Exception">thrown if the font file is not found.</exception>
    public Font(PDF pdf, String fontPath) {
        this.pdf = pdf;
        fontPath = FontFileOf(fontPath);
        using (FileStream inputStream = new FileStream(fontPath, FileMode.Open, FileAccess.Read)) {
            OpenTypeFont.Register(pdf, this, inputStream);
        }
        SetSize(size);
    }

    // Returns the path of the font file: the path given, or, for a .ttf.stream
    // or .otf.stream file, the .ttf or else the .otf file of the same name
    // beside it. PDFjet reads .otf and .ttf fonts alone from 9.0.5, and its
    // fonts are subset from their .ttf files: a path written for the .stream
    // files PDFjet shipped before is the same font.
    internal static String FontFileOf(String fontPath) {
        foreach (String stream in new String[] {".ttf.stream", ".otf.stream"}) {
            if (fontPath.EndsWith(stream, StringComparison.Ordinal)) {
                String basePath = fontPath.Substring(0, fontPath.Length - stream.Length);
                foreach (String ext in new String[] {".ttf", ".otf"}) {
                    if (File.Exists(basePath + ext)) {
                        return basePath + ext;
                    }
                }
            }
        }
        return fontPath;
    }

    /// <summary>
    /// Sets the size of this font.
    /// </summary>
    /// <param name="fontSize">specifies the size of this font.</param>
    /// <returns>the font.</returns>
    // Returns a number that identifies the font program: the units it is drawn
    // in, its advance widths and its character map, which say what its glyphs
    // are and which glyph each character has. Two fonts of one name that give
    // the same number are the same program, whether it was read from a .otf
    // or a .ttf file, and the PDF embeds it once and writes one
    // descriptor, one CID font and one ToUnicode map for both. The name is
    // not enough on its own: PDFjet ships subsets of the Noto CJK fonts under
    // the name of the whole font, and the text of the one embedded second was
    // drawn with the glyphs of the first.
    internal static ulong ChecksumOf(Font font) {
        ulong hash = 0xcbf29ce484222325UL;
        hash = Fold(hash, font.unitsPerEm);
        hash = Fold(hash, font.advanceWidth.Length);
        foreach (int width in font.advanceWidth) {
            hash = Fold(hash, width);
        }
        foreach (int gid in font.unicodeToGID) {
            hash = Fold(hash, gid);
        }
        return hash;
    }

    // One step of the FNV-1a hash, which is the same in the four ports.
    internal static ulong Fold(ulong hash, int value) {
        return (hash ^ (uint) value) * 0x100000001B3UL;
    }

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

    /// <summary>
    /// Sets whether this font is embedded as a subset, the outlines of the
    /// glyphs the document does not draw left out, which is the default for a
    /// .ttf or a .otf. A font whose license does not allow subsetting, by the
    /// fsType of its OS/2 table, is embedded whole. Fonts read from one file are
    /// one font program in the PDF: kept whole for one, the program is whole
    /// for all of them. It must be called before Complete(), and for a font
    /// added to the objects of an existing PDF, before the objects are added to
    /// the PDF.
    /// </summary>
    /// <param name="subset">false to embed the font whole.</param>
    /// <returns>this Font object.</returns>
    public Font SetSubset(bool subset) {
        if (program != null) {
            program.whole = !subset;
        }
        return this;
    }

    // Records that the glyph is drawn with the font.
    internal void UseGlyph(int gid) {
        if (program != null && gid >= 0 && gid < program.used.Length) {
            program.used[gid] = true;
        }
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
            // Every glyph of a CJK font is as wide as the font size.
            int count = 0;
            for (int i = 0; i < str.Length; i += (Util.CodePointAt(str, i) > 0xFFFF) ? 2 : 1) {
                count++;
            }
            return count * fontSize;
        }

        if (isCoreFont) {
            for (int i = 0; i < str.Length; ) {
                int cp = Util.CodePointAt(str, i);
                i += (cp > 0xFFFF) ? 2 : 1;
                int c1 = CoreFontCode(cp);
                width += metrics[c1 - 32][1];
                if (kernPairs && i < str.Length) {
                    width += Kerning(c1, CoreFontCode(Util.CodePointAt(str, i)));
                }
            }
        } else {
            for (int i = 0; i < str.Length; ) {
                int codePoint = Util.CodePointAt(str, i);
                i += (codePoint > 0xFFFF) ? 2 : 1;
                width += AdvanceWidthOf(codePoint);
            }
        }

        return width * fontSize / unitsPerEm;
    }

    // Returns the advance width, in font units, of the glyph that Page draws
    // for the character, the one GlyphOf gives: none for an RLM, LRM, ZWNJ,
    // ZWJ or byte order mark, which are not drawn, that of a space for a
    // control character and that of the glyph MissingGlyph gives for a
    // character the font does not have.
    private int AdvanceWidthOf(int codePoint) {
        if (IsJoinerOrRLM(codePoint) || codePoint == 0xFEFF) {
            return 0;
        }
        return GlyphAdvance(Page.GlyphOf(this, codePoint));
    }

    // Returns the advance width of the glyph, in font units. A font can list
    // fewer advance widths than it has glyphs, and the glyphs past the end have
    // the width of the last one, as OpenType says and the PDF font's /DW does.
    internal int GlyphAdvance(int gid) {
        return advanceWidth[Math.Min(gid, advanceWidth.Length - 1)];
    }

    // Returns true if the font has a glyph for the character, so that it is
    // not drawn with .notdef and the fallback font is not needed for it. A
    // character past the end of the glyph table of the font, like an emoji,
    // has none. A control character is drawn as a space, and a core font has
    // the characters of its encoding.
    internal bool HasGlyph(int codePoint) {
        if (IsControl(codePoint)) {
            codePoint = 0x20;
        }
        if (isCoreFont) {
            return codePoint == 32 || CoreFontCode(codePoint) != 32;
        }
        return !isCJK && !Lacks(codePoint);
    }

    // Returns true if the font has no glyph for the character, which is not a
    // control character. The character map of the font is read from its first
    // to its last character, so a character outside that range has no glyph.
    internal bool Lacks(int codePoint) {
        if (IsControl(codePoint)) {
            return false;
        }
        return codePoint < firstChar || codePoint > lastChar || unicodeToGID[codePoint] == 0;
    }

    // Returns the glyph a character the font does not have is drawn with:
    // .notdef, glyph 0, in a document of no compliance. PDF/UA and PDF/A forbid
    // .notdef -- ISO 14289-1 7.21.8 and ISO 19005-2 6.2.11.8 -- so a compliant
    // document draws the replacement character U+FFFD of the font, or a
    // question mark, or a space, the first the font has, and the character is
    // its actual text. A font of a PDF that was read is of no compliance.
    internal int MissingGlyph() {
        if (pdf == null || pdf.compliance == Compliance.PDF_1_7) {
            return 0;
        }
        foreach (int c in new int[] {0xFFFD, '?', ' '}) {
            if (c >= firstChar && c <= lastChar && unicodeToGID[c] != 0) {
                return unicodeToGID[c];
            }
        }
        return 0;
    }

    // Returns the font, of the font and its fallback font, that draws the
    // character at index i of the string, when the character before it is
    // drawn with the active font: the font when it has a glyph for the
    // character, else the fallback font when that has one, else the font. A
    // combining mark stays with the character before it when that font has it,
    // and an RLM, LRM, ZWNJ or ZWJ goes with the character after it.
    internal static Font FontOf(Font font, Font fallbackFont, Font active, String str, int i) {
        int cp = Util.CodePointAt(str, i);
        if (i > 0 && System.Globalization.CharUnicodeInfo.GetUnicodeCategory(cp) ==
                System.Globalization.UnicodeCategory.NonSpacingMark && active.HasGlyph(cp)) {
            return active;
        }
        int next = i + ((cp > 0xFFFF) ? 2 : 1);
        if (IsJoinerOrRLM(cp) && next < str.Length) {
            cp = Util.CodePointAt(str, next);
        }
        if (font.HasGlyph(cp) || !fallbackFont.HasGlyph(cp)) {
            return font;
        }
        return fallbackFont;
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

    /// <summary>
    /// Returns the line gap at the specified font size: the space the font puts
    /// between the descent of a line and the ascent of the next one. It is 0 for
    /// most fonts, which leave that space in their ascent and descent, and 1 em
    /// for the Japanese and Chinese IBM Plex fonts, whose ascent and descent add
    /// up to 1 em.
    /// </summary>
    public float GetLineGap(float fontSize) {
        if (isCJK) {
            return 0f;
        }
        return fontLineGap * fontSize / unitsPerEm;
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

    /// <summary>Returns the underline thickness at the current font size.</summary>
    public float GetUnderlineThickness() {
        return this.underlineThickness;
    }

    /// <summary>Returns the underline position at the current font size.</summary>
    public float GetUnderlinePosition() {
        return this.underlinePosition;
    }

    /// <summary>Returns the underline thickness at the specified font size.</summary>
    public float GetUnderlineThickness(float fontSize) {
        return (fontUnderlineThickness * fontSize / unitsPerEm);
    }

    /// <summary>Returns the underline position at the specified font size.</summary>
    public float GetUnderlinePosition(float fontSize) {
        return -(fontUnderlinePosition * fontSize / unitsPerEm) + GetUnderlineThickness(fontSize) / 2.0f;
    }

    /// <summary>Returns how many characters of the string fit within the specified width,
    /// counted in the UTF-16 chars of the string, so that it can be passed to Substring.</summary>
    public int GetFitChars(String str, float width) {
        // Text of no size has no width, and all of it fits in any width.
        if (size <= 0f) {
            return (width < 0f) ? 0 : str.Length;
        }
        float w = width * unitsPerEm / size;
        if (isCJK) {
            // Every glyph of a CJK font is as wide as the font size.
            int fit = Math.Max(0, (int) (width / size));
            int k = 0;
            while (k < str.Length && fit > 0) {
                k += (Util.CodePointAt(str, k) > 0xFFFF) ? 2 : 1;
                fit--;
            }
            return k;
        }
        if (isCoreFont) {
            return GetCoreFontFitChars(str, w);
        }
        int i = 0;
        while (i < str.Length) {
            int codePoint = Util.CodePointAt(str, i);
            w -= AdvanceWidthOf(codePoint);
            if (w < 0) {
                break;
            }
            i += (codePoint > 0xFFFF) ? 2 : 1;
        }
        return i;
    }

    private int GetCoreFontFitChars(String str, float width) {
        float w = width;

        int i = 0;
        while (i < str.Length) {
            int cp = Util.CodePointAt(str, i);
            int next = i + ((cp > 0xFFFF) ? 2 : 1);
            int c1 = CoreFontCode(cp);
            w -= metrics[c1 - 32][1];
            if (w < 0) {
                return i;
            }
            if (kernPairs && next < str.Length) {
                w -= Kerning(c1, CoreFontCode(Util.CodePointAt(str, next)));
                if (w < 0) {
                    return i;
                }
            }

            i = next;
        }

        return i;
    }

    /// <summary>
    /// Returns the code of the character in the encoding of this core font:
    /// WinAnsi, which puts ’ at 146, “ and ” at 147 and 148, the dashes at 150
    /// and 151 and € at 128, or, for Symbol and ZapfDingbats, the character
    /// itself. A character that is not in the encoding is drawn as a space.
    /// </summary>
    internal int CoreFontCode(int cp) {
        if (!symbolic) {
            cp = WinAnsiCode(cp);
        }
        return (cp < 32 || cp > 255) ? 32 : cp;
    }

    // The WinAnsi code of the character: the character itself below 127 and
    // from 160 to 255, the code of the character WinAnsi puts from 128 to 159,
    // and a space for any other character, the controls U+007F to U+009F too.
    // WinAnsi draws a bullet at 127, ISO 32000, Annex D, where the widths of
    // the core fonts have a space.
    private static int WinAnsiCode(int cp) {
        if (cp < 0x7F || (cp >= 0xA0 && cp <= 0xFF)) {
            return cp;
        }
        switch (cp) {
            case 0x20AC: return 128;    // €
            case 0x201A: return 130;    // ‚
            case 0x0192: return 131;    // ƒ
            case 0x201E: return 132;    // „
            case 0x2026: return 133;    // …
            case 0x2020: return 134;    // †
            case 0x2021: return 135;    // ‡
            case 0x02C6: return 136;    // ˆ
            case 0x2030: return 137;    // ‰
            case 0x0160: return 138;    // Š
            case 0x2039: return 139;    // ‹
            case 0x0152: return 140;    // Œ
            case 0x017D: return 142;    // Ž
            case 0x2018: return 145;    // ‘
            case 0x2019: return 146;    // ’
            case 0x201C: return 147;    // “
            case 0x201D: return 148;    // ”
            case 0x2022: return 149;    // •
            case 0x2013: return 150;    // –
            case 0x2014: return 151;    // —
            case 0x02DC: return 152;    // ˜
            case 0x2122: return 153;    // ™
            case 0x0161: return 154;    // š
            case 0x203A: return 155;    // ›
            case 0x0153: return 156;    // œ
            case 0x017E: return 158;    // ž
            case 0x0178: return 159;    // Ÿ
            default: return 32;
        }
    }

    /// <summary>
    /// Returns the kerning of a pair of characters of this core font, in 1/1000
    /// of the font size: negative when the second character moves closer to the
    /// first, and 0 when the font has no kerning for the pair.
    /// </summary>
    internal int Kerning(int c1, int c2) {
        int[] row = metrics[c1 - 32];
        for (int j = 2; j < row.Length; j += 2) {
            if (row[j] == c2) {
                return row[j + 1];
            }
        }
        return 0;
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

    // Returns true for the right-to-left and left-to-right marks and the zero
    // width non-joiner and joiner, which are not drawn: Page gives the glyph
    // before or after them an actual text.
    internal static bool IsJoinerOrRLM(int ch) {
        return ch == 0x200F || ch == 0x200E || ch == 0x200C || ch == 0x200D;
    }

    // Returns true for the C0 and C1 control characters and DEL, U+0000 to
    // U+001F and U+007F to U+009F, which have no glyph to draw: they are drawn
    // as a space.
    internal static bool IsControl(int ch) {
        return ch < 0x20 || (ch >= 0x7F && ch <= 0x9F);
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
        return StringWidth(fallbackFont, fontSize, fontSize, str);
    }

    // Returns the width of a string drawn using two fonts, with the characters
    // in the fallback font at the fallback font size.
    internal float StringWidth(Font fallbackFont, float fontSize, float fallbackFontSize, String str) {
        float width = 0f;

        // A fallback font that is the font itself measures every character
        // with the font, as with no fallback font
        if (str == null || this.isCJK || fallbackFont == null || fallbackFont.isCJK ||
                (fallbackFont == this && fallbackFontSize == fontSize)) {
            return StringWidth(fontSize, str);
        }

        Font activeFont = this;
        // The runs of text drawn with one font are the pieces of the string
        // between the characters that switch fonts, so measuring them copies
        // nothing: a string that is all in the primary font is one piece.
        int start = 0;
        for (int i = 0; i < str.Length; i += Util.CharCount(str, i)) {
            Font charFont = FontOf(this, fallbackFont, activeFont, str, i);
            if (charFont != activeFont) {
                width += activeFont.StringWidth(
                        activeFont == this ? fontSize : fallbackFontSize, str.Substring(start, i - start));
                start = i;
                activeFont = charFont;
            }
        }
        width += activeFont.StringWidth(
                activeFont == this ? fontSize : fallbackFontSize, str.Substring(start));

        return width;
    }
}   // End of Font.cs
}   // End of namespace PDFjet.NET
