/*
 * Font.cs
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
using System;
using System.Globalization;
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
    internal GlyphMap unicodeToGID;
    internal int[] unicodeOf;           // The character of each glyph, see GlyphMap.UnicodeOfGlyphs.
    // The glyph and the advance width of each printable ASCII character,
    // U+0020 to U+007E, that the font has, from SetASCIIGlyphs: glyph 0 for one
    // it lacks, which is looked up as any other character. Most text is
    // these characters, and they skip the tests that GlyphOf and
    // AdvanceWidthOf make: a table of 2,546 pages took 37% longer in 9.0.7
    // than in 9.0.1 (15 October 2026).
    internal readonly ushort[] asciiGID = new ushort[95];
    internal readonly ushort[] asciiAdvance = new ushort[95];
    // True for a printable ASCII character drawn once already, with its own
    // glyph, so that it is not drawn as another character the next time
    // either: the first character drawn with a glyph never changes.
    internal readonly bool[] asciiPlain = new bool[95];
    // Where the marks go, from the GPOS table of the font, read when a mark
    // is first drawn, or null for a font without one.
    internal GPOS gpos;
    // The pairs the font kerns, from its GPOS table, read when the kerning is
    // turned on, or null for a font without one.
    internal Kerning kernTables;
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
    // not embedded: the PDF names it and the viewer supplies it, so a document
    // in it is a few kilobytes, where an embedded CJK font adds hundreds. The
    // viewer needs the Adobe Asian font pack, or a font of its own in its
    // place, and the text takes the shapes of the font it finds; Latin letters
    // and spaces are drawn full width; and a PDF with it cannot be PDF/A or
    // PDF/UA. See Example_05. To ship the glyphs with the document, use an
    // embedded font, such as IBM Plex Sans JP, KR, SC or TC: see Example_02
    // and Example_03.
    /// <summary>Creates a Chinese, Japanese or Korean font and adds it to the
    /// PDF, not embedded: the viewer supplies it.</summary>
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
        String ordering, supplement;
        if (fontName.StartsWith("AdobeMingStd", StringComparison.Ordinal)) {
            ordering = "CNS1"; supplement = "4";
        } else if (fontName.StartsWith("AdobeSongStd", StringComparison.Ordinal)
                || fontName.StartsWith("STHeitiSC", StringComparison.Ordinal)) {
            ordering = "GB1"; supplement = "4";
        } else if (fontName.StartsWith("KozMinPro", StringComparison.Ordinal)) {
            ordering = "Japan1"; supplement = "4";
        } else if (fontName.StartsWith("AdobeMyungjoStd", StringComparison.Ordinal)) {
            ordering = "Korea1"; supplement = "1";
        } else {
            throw new ArgumentException("Unsupported font: " + fontName);
        }
        pdf.Append("/CIDSystemInfo <<\n");
        pdf.Append("/Registry ");
        pdf.AppendLiteralString("Adobe");
        pdf.Append("\n");
        pdf.Append("/Ordering ");
        pdf.AppendLiteralString(ordering);
        pdf.Append("\n");
        pdf.Append("/Supplement ");
        pdf.Append(supplement);
        pdf.Append("\n");
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
            throw new ArgumentException("Unsupported font: " + fontName);
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
    /// Constructor for OpenType and TrueType fonts, .otf and .ttf files, added
    /// to the objects of an existing PDF. A path to one of the fonts PDFjet
    /// carries, such as the constants of the font classes, is read from the
    /// library when there is no such file, as by the constructor that takes a
    /// PDF and a path.
    /// </summary>
    /// <param name="objects">the list of objects</param>
    /// <param name="fontPath">the font path</param>
    /// <exception cref="System.IO.FileNotFoundException">thrown if the font is not found.</exception>
    public Font(List<PDFobj> objects, String fontPath) {
        using (Stream inputStream = Open(FontFileOf(fontPath))) {
            OpenTypeFont.Register(objects, this, inputStream);
        }
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
    /// to one of the fonts PDFjet carries, such as the constants of the font
    /// classes, fonts/IBMPlexSans/IBMPlexSans-Regular.ttf for one, is read from
    /// the folder resources/fonts, else from the library, when there is no
    /// such file; the CJK fonts, such as IBMPlexSansJP.Regular, are in the
    /// NuGet package PDFjet.Fonts.CJK, which a program adds next to PDFjet,
    /// and a CJK font not found says so. A path to a .ttf.stream or
    /// .otf.stream file, the fonts PDFjet shipped before 9.0.5, opens the .ttf
    /// or else the .otf file of the same name beside it.
    /// </summary>
    /// <param name="pdf">the pdf object.</param>
    /// <param name="fontPath">the font path.</param>
    /// <exception cref="System.Exception">thrown if the font file is not found.</exception>
    public Font(PDF pdf, String fontPath) {
        this.pdf = pdf;
        fontPath = FontFileOf(fontPath);
        using (Stream inputStream = Open(fontPath)) {
            OpenTypeFont.Register(pdf, this, inputStream);
        }
        SetSize(size);
    }

    // Opens the font file of the path, or, when there is no such file, the
    // font of that name that PDFjet carries; a font it does not carry throws.
    internal static Stream Open(String fontPath) {
        if (!File.Exists(fontPath)) {
            Stream bundled = BundledFonts.Open(fontPath);
            if (bundled != null) {
                return bundled;
            }
            String cjk = BundledFonts.CjkMissing(fontPath);
            if (cjk != null) {
                throw new FileNotFoundException(cjk, fontPath);
            }
            String path = fontPath.Replace('\\', '/');
            if (path.StartsWith("fonts/", StringComparison.Ordinal) || path.Contains("/fonts/")) {
                throw new FileNotFoundException(fontPath + " is not a file, nor one of the fonts PDFjet carries, "
                        + "in resources/fonts or in PDFjet.dll: use a font file of your own.", fontPath);
            }
        }
        return new FileStream(fontPath, FileMode.Open, FileAccess.Read);
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
                // The font PDFjet carries of that name, when there is no file
                foreach (String ext in new String[] {".ttf", ".otf"}) {
                    using (Stream bundled = BundledFonts.Open(basePath + ext)) {
                        if (bundled != null) {
                            return basePath + ext;
                        }
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
        // Each block of 256 characters the font has glyphs in, its number
        // and the glyph of each character, 0 for those the font has not; the
        // blocks it has none in, most of them, are left out.
        if (font.unicodeToGID != null) {
            int[][] blocks = font.unicodeToGID.blocks;
            for (int b = 0; b < blocks.Length; b++) {
                if (blocks[b] == null) {
                    continue;
                }
                hash = Fold(hash, b);
                foreach (int gid in blocks[b]) {
                    hash = Fold(hash, gid);
                }
            }
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

    /// <summary>
    /// Turns the kerning of the font on or off: the pairs of letters that go
    /// closer together or further apart, as AV and To. A standard font kerns
    /// with the pairs of its metrics; an OpenType or TrueType font with the
    /// kern feature of its GPOS table. Off by default, as text that does not
    /// ask for it is as fast as it was. A kerned font measures its text as it
    /// draws it: StringWidth, the wrapping and the alignment of TextLine,
    /// TextBlock and the table cells count the pairs. Right to left text and
    /// marks are not kerned.
    /// </summary>
    /// <param name="kernPairs">true to kern the text drawn with the font.</param>
    /// <returns>this Font object.</returns>
    public Font SetKernPairs(bool kernPairs) {
        // The pairs are read now, so that measuring text does not change the font
        if (kernPairs && kernTables != null) {
            kernTables.Read(this);
        }
        this.kernPairs = kernPairs;
        return this;
    }

    // Returns true if the font is an embedded font whose kerning is on and
    // that has pairs to kern with, from its GPOS table.
    internal bool Kerns() {
        return kernPairs && kernTables != null && !isCoreFont && !isCJK;
    }

    // Returns how far the second glyph moves after the first, in font units,
    // when both are drawn one after the other: 0 for a pair with a mark or a
    // right to left letter, which shaping places, and for a glyph of no
    // character, as .notdef.
    internal int GlyphKerning(int first, int second) {
        return kernTables.Read(this).GlyphPair(first, second);
    }

    // Returns true if the character is drawn, with the glyph it gives: an
    // RLM, LRM, byte order mark or a character IsIgnorable returns true for is
    // not, nor a ZWNJ or ZWJ the font has no glyph for. Page draws the glyphs
    // it gives, and the kerning measures the pairs of the same glyphs, so the
    // two decide it in this one place.
    internal bool DrawnGlyph(int c, out int gid) {
        if (c >= 0x20 && c < 0x7F && asciiGID[c - 0x20] != 0) {
            gid = asciiGID[c - 0x20];
            return true;
        }
        gid = 0;
        if (c == 0x200E || c == 0x200F || c == 0xFEFF || IsIgnorable(c)) {
            return false;
        }
        if ((c == 0x200C || c == 0x200D) && unicodeToGID.Get(c) == 0) {
            return false;
        }
        gid = Page.GlyphOf(this, c);
        return true;
    }

    // Returns the kerning of the text, in font units: the sum of the pairs of
    // the glyphs drawn one after the other.
    internal int TextKerning(String str) {
        int total = 0;
        int previous = -1;
        for (int i = 0; i < str.Length; ) {
            int c = Util.CodePointAt(str, i);
            i += (c > 0xFFFF) ? 2 : 1;
            int gid;
            if (!DrawnGlyph(c, out gid)) {
                continue;
            }
            if (previous >= 0) {
                total += GlyphKerning(previous, gid);
            }
            previous = gid;
        }
        return total;
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
        if (program != null) {
            program.used.Add(gid);
        }
    }

    // Records that the glyph is drawn for the character, so that the ToUnicode
    // map gives the glyph the character the document drew, where the font
    // draws several characters with one glyph: it gave the glyph the lowest of
    // them, and a Persian ۱ was copied as an Arabic ١ (review of 11 October
    // 2026, #8). The first character drawn with the glyph is kept; the others
    // drawn with it are given their own as actual text, see DrawnAsAnother.
    internal void UseChar(int c, int gid) {
        if (program == null || gid <= 0 || unicodeToGID.Get(c) != gid) {
            return;
        }
        program.FirstChar(gid, c);
    }

    // Returns true if the glyph of the character is drawn for another
    // character too, which the ToUnicode map gives the glyph: the character is
    // then drawn in a span with itself as its actual text. A document that drew
    // "١٢٣" and then "۱۲۳" copied the second as "١۲٣" (review of 12 October
    // 2026, R15). A character copied as the other is not, as an Arabic letter
    // and its form: ل and its isolated form both copy as ل, and Arabic text
    // had three times the spans it needed, its content 27% larger (review of
    // 12 October 2026, T2).
    internal bool DrawnAsAnother(int c) {
        if (program == null) {
            return false;
        }
        return IsAnother(program.FirstChar(unicodeToGID.Get(c)), c);
    }

    // UseChar, then DrawnAsAnother, with one lookup of the character's glyph:
    // the plain text of a page asks for each character drawn, and the two
    // calls made 100 pages of text 9% slower in Java (12 October 2026).
    internal bool UseCharDrawnAsAnother(int c, int gid) {
        if (program == null) {
            return false;
        }
        int own = unicodeToGID.Get(c);
        if (gid > 0 && own == gid) {
            return IsAnother(program.FirstChar(gid, c), c);
        }
        return IsAnother(program.FirstChar(own), c);
    }

    // Returns true if the character first drawn with a glyph, or -1, is
    // another than the character, which copies as other text.
    private static bool IsAnother(int first, int c) {
        return first >= 0 && first != c && !SameLetters(first, c);
    }

    // Returns true if the two characters copy as the same text: the letters of
    // a presentation form, as the ToUnicode map gives them.
    internal static bool SameLetters(int a, int b) {
        string la = Bidi.LettersOf(a) ?? char.ConvertFromUtf32(a);
        string lb = Bidi.LettersOf(b) ?? char.ConvertFromUtf32(b);
        return la == lb;
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
            // Every glyph of a CJK font is as wide as the font size, and the
            // characters not drawn take no width.
            int count = 0;
            for (int i = 0; i < str.Length; i += (Util.CodePointAt(str, i) > 0xFFFF) ? 2 : 1) {
                if (!IsNotDrawn(Util.CodePointAt(str, i))) {
                    count++;
                }
            }
            return count * fontSize;
        }

        if (isCoreFont) {
            str = WithoutIgnorables(str);
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
                if (codePoint >= 0x20 && codePoint < 0x7F && asciiGID[codePoint - 0x20] != 0) {
                    width += asciiAdvance[codePoint - 0x20];
                } else {
                    width += AdvanceWidthOf(codePoint);
                }
            }
            if (Kerns()) {
                width += TextKerning(str);
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
        if (IsJoinerOrRLM(codePoint) || codePoint == 0xFEFF || IsIgnorable(codePoint)) {
            return 0;
        }
        return GlyphAdvance(Page.GlyphOf(this, codePoint));
    }

    // Reads the glyph and the advance width of each printable ASCII character
    // the font has into asciiGID and asciiAdvance, once its character map and
    // its widths are read. They are those GlyphOf and AdvanceWidthOf give:
    // none of these characters is a control character, an ignorable one or a
    // mark.
    internal void SetASCIIGlyphs() {
        if (advanceWidth == null || advanceWidth.Length == 0) {
            return;
        }
        for (int c = 0x20; c < 0x7F; c++) {
            if (!Lacks(c)) {
                int gid = unicodeToGID.Get(c);
                asciiGID[c - 0x20] = (ushort) gid;
                asciiAdvance[c - 0x20] = (ushort) GlyphAdvance(gid);
            }
        }
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
        return codePoint < firstChar || codePoint > lastChar || unicodeToGID.Get(codePoint) == 0;
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
            if (c >= firstChar && c <= lastChar && unicodeToGID.Get(c) != 0) {
                return unicodeToGID.Get(c);
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
        if (i > 0 && CharUnicodeInfo.GetUnicodeCategory(cp) ==
                UnicodeCategory.NonSpacingMark && active.HasGlyph(cp)) {
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
            // Every glyph of a CJK font is as wide as the font size, and the
            // characters not drawn take no width. A width of more characters
            // than the text has fits all of it: (int) of 1e30 or of an
            // infinity is not a number of characters, and none fit (review of
            // 12 October 2026, T6).
            float room = width / size;
            if (!(room > 0f)) {
                return 0;
            }
            if (room >= str.Length) {
                return str.Length;
            }
            int fit = (int) room;
            int k = 0;
            while (k < str.Length) {
                int codePoint = Util.CodePointAt(str, k);
                if (!IsNotDrawn(codePoint)) {
                    if (fit == 0) {
                        break;
                    }
                    fit--;
                }
                k += (codePoint > 0xFFFF) ? 2 : 1;
            }
            return k;
        }
        if (isCoreFont) {
            return GetCoreFontFitChars(str, w);
        }
        bool kerned = Kerns();
        int previous = -1;
        int i = 0;
        while (i < str.Length) {
            int codePoint = Util.CodePointAt(str, i);
            int advance = AdvanceWidthOf(codePoint);
            // A character fits with its kerning with the one drawn before it,
            // as StringWidth measures it: a pair kerned wider takes its room
            // only when its second character is there.
            int gid;
            if (kerned && DrawnGlyph(codePoint, out gid)) {
                if (previous >= 0) {
                    advance += GlyphKerning(previous, gid);
                }
                previous = gid;
            }
            w -= advance;
            if (w < 0) {
                break;
            }
            i += (codePoint > 0xFFFF) ? 2 : 1;
        }
        return i;
    }

    private int GetCoreFontFitChars(String str, float width) {
        float w = width;
        int previous = -1;

        int i = 0;
        while (i < str.Length) {
            int cp = Util.CodePointAt(str, i);
            int next = i + ((cp > 0xFFFF) ? 2 : 1);
            if (NotDrawn(cp)) {
                i = next;
                continue;
            }
            int c1 = CoreFontCode(cp);
            float advance = metrics[c1 - 32][1];
            // The kerning is with the character drawn before, as
            // StringWidth's is, and takes its room only when this one is there.
            if (kernPairs && previous >= 0) {
                advance += Kerning(previous, c1);
            }
            previous = c1;
            w -= advance;
            if (w < 0) {
                return i;
            }

            i = next;
        }

        return i;
    }

    // Returns the text without the characters a core font of WinAnsi does not
    // draw or measure, as no font does: the soft hyphen, as WinAnsi has a
    // hyphen at its code, and a line broken at a soft hyphen ends with a hyphen
    // of its own (review of 11 October 2026, #21), and the other characters not
    // drawn, the marks of direction, the joiners, the byte order mark and those
    // IsIgnorable returns true for, which were drawn as spaces (review of 12
    // October 2026, R14). Symbol and ZapfDingbats keep theirs, their codes
    // being their characters.
    internal String WithoutIgnorables(String str) {
        if (symbolic) {
            return str;
        }
        System.Text.StringBuilder sb = null;
        int i = 0;
        while (i < str.Length) {
            int cp = Util.CodePointAt(str, i);
            int next = i + ((cp > 0xFFFF) ? 2 : 1);
            if (NotDrawn(cp)) {
                if (sb == null) {
                    sb = new System.Text.StringBuilder(str, 0, i, str.Length);
                }
            } else if (sb != null) {
                sb.Append(str, i, next - i);
            }
            i = next;
        }
        return (sb == null) ? str : sb.ToString();
    }

    // Returns true for a character that a core font of WinAnsi does not draw
    // or measure, see WithoutIgnorables.
    private bool NotDrawn(int cp) {
        return !symbolic && IsNotDrawn(cp);
    }

    // Returns true for a character a font that is not embedded, a core font of
    // WinAnsi or a CJK font, does not draw or measure: the marks of direction,
    // the joiners, the byte order mark and those IsIgnorable returns true for, the
    // soft hyphen among them. A CJK font drew each 1 em wide (review of 12
    // October 2026, R20).
    internal static bool IsNotDrawn(int cp) {
        return IsIgnorable(cp) || IsJoinerOrRLM(cp) || cp == 0xFEFF;
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

    // Returns true for a character that Unicode says is not drawn when a font
    // has no glyph for it, Default_Ignorable_Code_Point, but those that Page
    // treats on their own: the marks of direction, the joiners and the byte
    // order mark. A variation selector after ™ or ©, as text pasted from a
    // phone has, a zero width space or a word joiner was drawn as a box with a
    // width (review of 11 October 2026, #9); it takes no width, and nothing is
    // drawn for it. The soft hyphen is one too: a line broken at it ends with
    // a hyphen that TextBlock, TextFrame and a table's cells put there, and it
    // was drawn as a hyphen wherever it was (#21).
    internal static bool IsIgnorable(int ch) {
        if (ch == 0x00AD) {
            return true;
        }
        if (ch < 0x034F || (ch > 0x034F && ch < 0x061C) || (ch > 0x3164 && ch < 0xFE00)) {
            return false;   // Greek, Cyrillic, Hebrew, the CJK ideographs and Hangul at once
        }
        return ch == 0x034F || ch == 0x061C || ch == 0x115F || ch == 0x1160 ||
                ch == 0x17B4 || ch == 0x17B5 || (ch >= 0x180B && ch <= 0x180F) ||
                ch == 0x200B || (ch >= 0x202A && ch <= 0x202E) ||
                (ch >= 0x2060 && ch <= 0x206F) || ch == 0x3164 ||
                (ch >= 0xFE00 && ch <= 0xFE0F) || ch == 0xFFA0 ||
                (ch >= 0xFFF0 && ch <= 0xFFF8) || (ch >= 0x1BCA0 && ch <= 0x1BCA3) ||
                (ch >= 0x1D173 && ch <= 0x1D17A) || (ch >= 0xE0000 && ch <= 0xE0FFF);
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
