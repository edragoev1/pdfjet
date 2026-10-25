/*
 * Font.java
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
package com.pdfjet;

import java.io.BufferedInputStream;
import java.io.File;
import java.io.FileInputStream;
import java.io.InputStream;
import java.util.List;

/**
 * Used to create font objects.
 * The font objects must be added to the PDF before they can be used to draw text.
 */
final public class Font {
    /** The name of the font. */
    protected String name;
    /** The usage terms of the font, written to the font metadata. */
    protected String info;
    /** The object number of the font. */
    protected int objNumber;
    // The PDF the font was added to, or null for a font of an existing PDF.
    PDF pdf;
    /** The resource name of a font read from an existing PDF, or null. */
    protected String fontID;

    /** The object number of the embedded font file. */
    protected int fileObjNumber;
    /** The object number of the font descriptor. */
    protected int fontDescriptorObjNumber;
    /** The object number of the CID font dictionary. */
    protected int cidFontDictObjNumber;
    /** The object number of the ToUnicode CMap. */
    protected int toUnicodeCMapObjNumber;
    int cidSetObjNumber;        // The CIDSet of a subset in PDF/A-1
    String baseFont;            // The name it is embedded under, with the tag of a subset
    Subset.Program program;     // A TrueType font program, until complete()
    FontObjects objects;        // Of a font added to an existing PDF, until they are added
    boolean[] kept;             // The glyphs its subset keeps

    // Font attributes
    /** The number of font units per em. */
    protected int unitsPerEm = 1000; // The default for core fonts.
    /** The ascent in font units. */
    protected int fontAscent;
    /** The descent in font units. */
    protected int fontDescent;
    /** The line gap in font units: the space the font puts between its lines. */
    protected int fontLineGap;
    /** The italic angle of the post table, as a 16.16 fixed number. */
    protected int italicAngle;
    /** The x coordinate of the lower left corner of the font bounding box. */
    protected int bBoxLLx;
    /** The y coordinate of the lower left corner of the font bounding box. */
    protected int bBoxLLy;
    /** The x coordinate of the upper right corner of the font bounding box. */
    protected int bBoxURx;
    /** The y coordinate of the upper right corner of the font bounding box. */
    protected int bBoxURy;
    /** The first character code. */
    protected int firstChar = 32; // The default for core fonts.
    /** The last character code. */
    protected int lastChar = 255; // The default for core fonts.
    /** The cap height in font units. */
    protected int capHeight;
    /** The underline position in font units. */
    protected int fontUnderlinePosition;
    /** The underline thickness in font units. */
    protected int fontUnderlineThickness;
    /** The advance width of each glyph. */
    protected int[] advanceWidth;
    /** Maps Unicode code points to glyph IDs. */
    GlyphMap unicodeToGID;
    /** The character of each glyph, see GlyphMap.unicodeOfGlyphs. */
    int[] unicodeOf;
    /**
     * The glyph and the advance width of each printable ASCII character,
     * U+0020 to U+007E, that the font has, from setASCIIGlyphs: glyph 0 for one
     * it lacks, which is looked up as any other character. Most text is these
     * characters, and they skip the tests that glyphOf and advanceWidthOf
     * make: a table of 2,546 pages took 37% longer in 9.0.7 than in 9.0.1
     * (15 October 2026).
     */
    final int[] asciiGID = new int[95];
    final int[] asciiAdvance = new int[95];
    /**
     * True for a printable ASCII character drawn once already, with its own
     * glyph, so that it is not drawn as another character the next time
     * either: the first character drawn with a glyph never changes.
     */
    final boolean[] asciiPlain = new boolean[95];
    /**
     * Where the marks go, from the GPOS table of the font, read when a mark
     * is first drawn, or null for a font without one.
     */
    GPOS gpos;
    /**
     * How far apart the pairs of glyphs go, from the GPOS or kern table of
     * the font, read when a kerned font first measures or draws, or null.
     */
    Kerning kernTables;
    /** True if the glyph outlines are in CFF format. */
    protected boolean cff;
    /** The character metrics of a core font. */
    protected int[][] metrics; // Only used for core fonts.
    /**
     * A number that tells the font program this font was read from apart from
     * every other, so that a PDF embeds each one once.
     */
    long checksum;

    // Don't change the following default values!
    /** The font size. */
    protected float size = 12.0f;
    /** True if this is one of the 14 standard fonts. */
    protected boolean isCoreFont = false;
    /** True if this is a Chinese, Japanese or Korean font. */
    protected boolean isCJK = false;
    /** True if the text is skewed to simulate italic. */
    protected boolean skew15 = false;
    /** True if kerning pairs are applied. */
    protected boolean kernPairs = false;
    // True for Symbol and ZapfDingbats, whose characters are the codes of
    // their own encodings, not WinAnsi.
    private boolean symbolic = false;

    // These attributes depend on the font size.
    /** The ascent at the current font size. */
    protected float ascent;
    /** The descent at the current font size. */
    protected float descent;
    /** The body height at the current font size. */
    protected float bodyHeight;
    /** The underline position at the current font size. */
    protected float underlinePosition;
    /** The underline thickness at the current font size. */
    protected float underlineThickness;

    /**
     * Constructor for the 14 standard fonts.
     * Creates a font object and adds it to the PDF.
     *
     * <pre>
     *  Examples:
     *      Font font1 = new Font(pdf, CoreFont.HELVETICA);
     *      Font font2 = new Font(pdf, CoreFont.TIMES_ITALIC);
     *      Font font3 = new Font(pdf, CoreFont.ZAPF_DINGBATS);
     *      ...
     * </pre>
     *
     * @param pdf      the PDF to add this font to.
     * @param coreFont the core font. Must be one the names defined in the CoreFont
     *                 class.
     * @throws Exception If an input or output exception occurred
     */
    public Font(PDF pdf, int coreFont) throws Exception {
        this.pdf = pdf;
        CoreFont font = new CoreFont(coreFont);
        this.isCoreFont = true;
        this.name = font.name;
        this.bBoxLLx = font.bBoxLLx;
        this.bBoxLLy = font.bBoxLLy;
        this.bBoxURx = font.bBoxURx;
        this.bBoxURy = font.bBoxURy;
        this.metrics = font.metrics;
        this.symbolic = font.name.equals("Symbol") || font.name.equals("ZapfDingbats");
        this.fontUnderlinePosition = font.underlinePosition;
        this.fontUnderlineThickness = font.underlineThickness;
        this.fontAscent = font.bBoxURy;
        this.fontDescent = font.bBoxLLy;
        setSize(size);

        pdf.newObj();
        pdf.append("<<\n");
        pdf.append("/Type /Font\n");
        pdf.append("/Subtype /Type1\n");
        pdf.append("/BaseFont /");
        pdf.append(this.name);
        pdf.append('\n');
        if (!this.name.equals("Symbol") && !this.name.equals("ZapfDingbats")) {
            pdf.append("/Encoding /WinAnsiEncoding\n");
        }
        pdf.append(">>\n");
        pdf.endObj();
        objNumber = pdf.getObjNumber();

        pdf.fonts.add(this);
    }

    // Used by PDFobj
    /**
     * Creates a core font object without adding it to a PDF.
     *
     * @param coreFont the core font, one of the constants in CoreFont.
     */
    protected Font(int coreFont) {
        CoreFont font = new CoreFont(coreFont);
        this.isCoreFont = true;
        this.name = font.name;
        this.bBoxLLx = font.bBoxLLx;
        this.bBoxLLy = font.bBoxLLy;
        this.bBoxURx = font.bBoxURx;
        this.bBoxURy = font.bBoxURy;
        this.metrics = font.metrics;
        this.symbolic = font.name.equals("Symbol") || font.name.equals("ZapfDingbats");
        this.fontUnderlinePosition = font.underlinePosition;
        this.fontUnderlineThickness = font.underlineThickness;
        this.fontAscent = font.bBoxURy;
        this.fontDescent = font.bBoxLLy;
        setSize(size);
    }

    /**
     * Constructor for CJK - Chinese, Japanese and Korean fonts. The font is
     * not embedded: the PDF names it and the viewer supplies it, so a document
     * in it is a few kilobytes, where an embedded CJK font adds hundreds. The
     * viewer needs the Adobe Asian font pack, or a font of its own in its
     * place, and the text takes the shapes of the font it finds; Latin letters
     * and spaces are drawn full width; and a PDF with it cannot be PDF/A or
     * PDF/UA. See Example_05. To ship the glyphs with the document, use an
     * embedded font, such as IBM Plex Sans JP, KR, SC or TC: see Example_02
     * and Example_03.
     *
     * @param pdf  the PDF to add this font to.
     * @param font the CJK font.
     * @throws Exception If an input or output exception occurred
     */
    public Font(PDF pdf, CJKFont font) throws Exception {
        this.pdf = pdf;
        String fontName = null;
        if (font == CJKFont.ADOBE_MING_STD_LIGHT) { // Chinese (Traditional) font
            fontName = "AdobeMingStd-Light";
        } else if (font == CJKFont.ST_HEITI_SC_LIGHT) { // Chinese (Simplified) font
            fontName = "STHeitiSC-Light";
        } else if (font == CJKFont.KOZ_MIN_PRO_VI_REGULAR) { // Japanese font
            fontName = "KozMinProVI-Regular";
        } else if (font == CJKFont.ADOBE_MYUNGJO_STD_MEDIUM) { // Korean font
            fontName = "AdobeMyungjoStd-Medium";
        }
        this.name = fontName;
        this.isCJK = true;
        this.firstChar = 0x0020;
        this.lastChar = 0xFFEE;
        this.ascent = this.size;
        this.descent = this.size / 4;
        this.bodyHeight = this.ascent + this.descent;

        // Font Descriptor
        pdf.newObj();
        pdf.append("<<\n");
        pdf.append("/Type /FontDescriptor\n");
        pdf.append("/FontName /");
        pdf.append(fontName);
        pdf.append('\n');
        pdf.append("/Flags 4\n");
        pdf.append("/FontBBox [0 0 0 0]\n");
        pdf.append(">>\n");
        pdf.endObj();

        // CIDFont Dictionary
        pdf.newObj();
        pdf.append("<<\n");
        pdf.append("/Type /Font\n");
        pdf.append("/Subtype /CIDFontType0\n");
        pdf.append("/BaseFont /");
        pdf.append(fontName);
        pdf.append('\n');
        pdf.append("/FontDescriptor ");
        pdf.append(pdf.getObjNumber() - 1);
        pdf.append(" 0 R\n");
        String ordering;
        String supplement;
        if (fontName.startsWith("AdobeMingStd")) {
            ordering = "CNS1";
            supplement = "4";
        } else if (fontName.startsWith("AdobeSongStd")
                || fontName.startsWith("STHeitiSC")) {
            ordering = "GB1";
            supplement = "4";
        } else if (fontName.startsWith("KozMinPro")) {
            ordering = "Japan1";
            supplement = "4";
        } else if (fontName.startsWith("AdobeMyungjoStd")) {
            ordering = "Korea1";
            supplement = "1";
        } else {
            throw new Exception("Unsupported font: " + fontName);
        }
        pdf.append("/CIDSystemInfo <<\n");
        pdf.append("/Registry ");
        pdf.appendLiteralString("Adobe");
        pdf.append("\n");
        pdf.append("/Ordering ");
        pdf.appendLiteralString(ordering);
        pdf.append("\n");
        pdf.append("/Supplement ");
        pdf.append(supplement);
        pdf.append("\n");
        pdf.append(">>\n");
        pdf.append(">>\n");
        pdf.endObj();

        // Type0 Font Dictionary
        pdf.newObj();
        pdf.append("<<\n");
        pdf.append("/Type /Font\n");
        pdf.append("/Subtype /Type0\n");
        pdf.append("/BaseFont /");
        if (fontName.startsWith("AdobeMingStd")) {
            pdf.append(fontName + "-UniCNS-UTF16-H\n");
            pdf.append("/Encoding /UniCNS-UTF16-H\n");
        } else if (fontName.startsWith("AdobeSongStd")
                || fontName.startsWith("STHeitiSC")) {
            pdf.append(fontName + "-UniGB-UTF16-H\n");
            pdf.append("/Encoding /UniGB-UTF16-H\n");
        } else if (fontName.startsWith("KozMinPro")) {
            pdf.append(fontName + "-UniJIS-UCS2-H\n");
            pdf.append("/Encoding /UniJIS-UCS2-H\n");
        } else if (fontName.startsWith("AdobeMyungjoStd")) {
            pdf.append(fontName + "-UniKS-UCS2-H\n");
            pdf.append("/Encoding /UniKS-UCS2-H\n");
        } else {
            throw new Exception("Unsupported font: " + fontName);
        }
        pdf.append("/DescendantFonts [");
        pdf.append(pdf.getObjNumber() - 1);
        pdf.append(" 0 R]\n");
        pdf.append(">>\n");
        pdf.endObj();
        objNumber = pdf.getObjNumber();

        pdf.fonts.add(this);
    }

    /**
     * Constructor for OpenType and TrueType fonts, .otf and .ttf, added to the
     * objects of an existing PDF. The font is embedded as a subset of the
     * glyphs drawn, when the objects are added to the PDF, unless
     * setSubset(false) keeps it whole.
     *
     * @param objects     the list of objects
     * @param inputStream the input stream
     * @throws Exception is the font is not found
     */
    public Font(List<PDFobj> objects, InputStream inputStream) throws Exception {
        OpenTypeFont.register(objects, this, inputStream);
        setSize(size);
    }

    /**
     * Constructor for OpenType and TrueType fonts, .otf and .ttf.
     *
     * @param pdf         the PDF object that requires this font.
     * @param inputStream the input stream to read this font from.
     * @throws Exception If an input or output exception occurred
     */
    public Font(PDF pdf, InputStream inputStream) throws Exception {
        this.pdf = pdf;
        OpenTypeFont.register(pdf, this, inputStream);
        setSize(size);
    }

    /**
     * Constructor for OpenType and TrueType fonts, .otf and .ttf files. A path
     * to a .ttf.stream or .otf.stream file, the fonts PDFjet shipped before
     * 9.0.5, opens the .ttf or else the .otf file of the same name beside it.
     *
     * @param pdf      the pdf object.
     * @param fontPath the font path.
     * @throws Exception thrown of the font file is not found.
     */
    public Font(PDF pdf, String fontPath) throws Exception {
        this.pdf = pdf;
        fontPath = fontFileOf(fontPath);
        try (InputStream inputStream = new BufferedInputStream(new FileInputStream(fontPath))) {
            OpenTypeFont.register(pdf, this, inputStream);
        }
        setSize(size);
    }

    // Returns the path of the font file: the path given, or, for a .ttf.stream
    // or .otf.stream file, the .ttf or else the .otf file of the same name
    // beside it. PDFjet reads .otf and .ttf fonts alone from 9.0.5, and its
    // fonts are subset from their .ttf files: a path written for the .stream
    // files PDFjet shipped before is the same font.
    static String fontFileOf(String fontPath) {
        for (String stream : new String[] {".ttf.stream", ".otf.stream"}) {
            if (fontPath.endsWith(stream)) {
                String base = fontPath.substring(0, fontPath.length() - stream.length());
                for (String ext : new String[] {".ttf", ".otf"}) {
                    if (new File(base + ext).exists()) {
                        return base + ext;
                    }
                }
            }
        }
        return fontPath;
    }

    // Returns a number that identifies the font program: the units it is
    // drawn in, its advance widths and its character map, which say what its
    // glyphs are and which glyph each character has. Two fonts of one name
    // that give the same number are the same program, whether it was read
    // from a .otf or a .ttf file, and the PDF embeds it once and
    // writes one descriptor, one CID font and one ToUnicode map for both.
    // The name is not enough on its own: PDFjet ships subsets of the Noto CJK
    // fonts under the name of the whole font, and the text of the one
    // embedded second was drawn with the glyphs of the first.
    static long checksumOf(Font font) {
        long hash = 0xcbf29ce484222325L;
        hash = fold(hash, font.unitsPerEm);
        hash = fold(hash, font.advanceWidth.length);
        for (int width : font.advanceWidth) {
            hash = fold(hash, width);
        }
        // Each block of 256 characters the font has glyphs in, its number
        // and the glyph of each character, 0 for those the font has not; the
        // blocks it has none in, most of them, are left out
        if (font.unicodeToGID != null) {
            int[][] blocks = font.unicodeToGID.blocks;
            for (int b = 0; b < blocks.length; b++) {
                if (blocks[b] == null) {
                    continue;
                }
                hash = fold(hash, b);
                for (int gid : blocks[b]) {
                    hash = fold(hash, gid);
                }
            }
        }
        return hash;
    }

    // One step of the FNV-1a hash, which is the same in the four ports.
    static long fold(long hash, int value) {
        return (hash ^ (value & 0xFFFFFFFFL)) * 0x100000001B3L;
    }

    /**
     * Sets the size of this font.
     *
     * @param fontSize specifies the size of this font.
     * @return the font.
     */
    public Font setSize(float fontSize) {
        this.size = fontSize;
        if (isCJK) {
            this.ascent = size;
            this.descent = ascent / 4;
            this.bodyHeight = this.ascent + this.descent;
            return this;
        }
        this.ascent = (float) fontAscent * size / (float) unitsPerEm;
        this.descent = -(float) fontDescent * size / (float) unitsPerEm;
        this.bodyHeight = this.ascent + this.descent;
        this.underlineThickness = ((float) fontUnderlineThickness * size / (float) unitsPerEm);
        this.underlinePosition = -((float) fontUnderlinePosition * size / (float) unitsPerEm)
                + underlineThickness / 2.0f;
        return this;
    }

    /**
     * Returns the current font size.
     *
     * @return the current size of the font.
     */
    public float getSize() {
        return size;
    }

    /**
     * Returns the name of this font.
     *
     * @return the name of the font.
     */
    public String getName() {
        return this.name;
    }

    /**
     * Returns the underline thickness of this font.
     *
     * @return the underline thickness.
     */
    public float getUnderlineThickness() {
        return this.underlineThickness;
    }

    /**
     * Returns the underline position of this font.
     *
     * @return the underline position.
     */
    public float getUnderlinePosition() {
        return this.underlinePosition;
    }

    /**
     * Turns the kerning of the font on or off: the pairs of letters that go
     * closer together or further apart, as AV and To. A standard font kerns
     * with the pairs of its metrics; an OpenType or TrueType font with the kern
     * feature of its GPOS table, or the kern table of an older font. Off by
     * default, as text that does not ask for it is as fast as it was. A kerned
     * font measures its text as it draws it: stringWidth, the wrapping and the
     * alignment of TextLine, TextBlock and the table cells count the pairs.
     * Right to left text and marks are not kerned.
     *
     * @param kernPairs true to kern the text of the font.
     * @return this Font object.
     */
    public Font setKernPairs(boolean kernPairs) {
        // The pairs are read now, so that measuring text does not change the font
        if (kernPairs && kernTables != null) {
            kernTables.read();
        }
        this.kernPairs = kernPairs;
        return this;
    }

    // Returns true if the font is an embedded font whose kerning is on and
    // that has pairs to kern with, from its GPOS or kern table.
    boolean kerns() {
        return kernPairs && kernTables != null && !isCoreFont && !isCJK;
    }

    // Returns how far the second glyph moves after the first, in font units,
    // when both are drawn one after the other: 0 for a pair with a mark or a
    // right to left letter, which shaping places, and for a glyph of no
    // character, as .notdef.
    int glyphKerning(int first, int second) {
        if (!kernable(first) || !kernable(second)) {
            return 0;
        }
        return kernTables.pair(first, second);
    }

    private boolean kernable(int gid) {
        if (gid <= 0 || gid >= unicodeOf.length) {
            return false;
        }
        int c = unicodeOf[gid];
        return c >= 0 && !Page.isMark(c) && !Page.isRightToLeft(c);
    }

    // Returns the glyph the character is drawn with, as Page draws it, or -1
    // when it is not drawn: an RLM, LRM, byte order mark or a character
    // isIgnorable returns true for is not, nor a ZWNJ or ZWJ the font has no
    // glyph for. The glyphs drawn are the pairs kerned.
    int kerningGlyph(int c) {
        if (c >= 0x20 && c < 0x7F && asciiGID[c - 0x20] != 0) {
            return asciiGID[c - 0x20];
        }
        if (c == 0x200E || c == 0x200F || c == 0xFEFF || isIgnorable(c)) {
            return -1;
        }
        if ((c == 0x200C || c == 0x200D) && unicodeToGID.get(c) == 0) {
            return -1;
        }
        return Page.glyphOf(this, c);
    }

    // Returns the kerning of the text, in font units: the sum of the pairs of
    // the glyphs drawn one after the other.
    int textKerning(String text) {
        int total = 0;
        int previous = -1;
        for (int i = 0; i < text.length(); ) {
            int c = text.codePointAt(i);
            i += Character.charCount(c);
            int gid = kerningGlyph(c);
            if (gid < 0) {
                continue;
            }
            if (previous >= 0) {
                total += glyphKerning(previous, gid);
            }
            previous = gid;
        }
        return total;
    }

    /**
     * Sets whether this font is embedded as a subset, the outlines of the
     * glyphs the document does not draw left out, which is the default for a
     * .ttf or a .otf. A font whose license does not allow subsetting, by the
     * fsType of its OS/2 table, is embedded whole. Fonts read from one file are
     * one font program in the PDF: kept whole for one, the program is whole for
     * all of them. It must be called before complete(), and for a font added to
     * the objects of an existing PDF, before the objects are added to the PDF.
     *
     * @param subset false to embed the font whole.
     * @return this Font object.
     */
    public Font setSubset(boolean subset) {
        if (program != null) {
            program.whole = !subset;
        }
        return this;
    }

    // Records that the glyph is drawn with the font.
    void useGlyph(int gid) {
        if (program != null) {
            program.used.add(gid);
        }
    }

    // Records that the glyph is drawn for the character, so that the ToUnicode
    // map gives the glyph the character the document drew, where the font
    // draws several characters with one glyph: it gave the glyph the lowest of
    // them, and a Persian ۱ was copied as an Arabic ١ (review of 11 October
    // 2026, #8). The first character drawn with the glyph is kept; the others
    // drawn with it are given their own as actual text, see drawnAsAnother.
    void useChar(int c, int gid) {
        if (program == null || gid <= 0 || unicodeToGID.get(c) != gid) {
            return;
        }
        program.firstChar(gid, c);
    }

    // Returns true if the glyph of the character is drawn for another
    // character too, which the ToUnicode map gives the glyph: the character is
    // then drawn in a span with itself as its actual text. A document that drew
    // "١٢٣" and then "۱۲۳" copied the second as "١۲٣" (review of 12 October
    // 2026, R15). A character copied as the other is not, as an Arabic letter
    // and its form: ل and its isolated form both copy as ل, and Arabic text
    // had three times the spans it needed, its content 27% larger (review of
    // 12 October 2026, T2).
    boolean drawnAsAnother(int c) {
        if (program == null) {
            return false;
        }
        return isAnother(program.firstChar(unicodeToGID.get(c)), c);
    }

    // useChar, then drawnAsAnother, with one lookup of the character's glyph:
    // the plain text of a page asks for each character drawn, and the two
    // calls made 100 pages of text 9% slower (12 October 2026).
    boolean useCharDrawnAsAnother(int c, int gid) {
        if (program == null) {
            return false;
        }
        int own = unicodeToGID.get(c);
        if (gid > 0 && own == gid) {
            return isAnother(program.firstChar(gid, c), c);
        }
        return isAnother(program.firstChar(own), c);
    }

    // Returns true if the character first drawn with a glyph, or -1, is
    // another than the character, which copies as other text.
    private static boolean isAnother(int first, int c) {
        return first >= 0 && first != c && !sameLetters(first, c);
    }

    // Returns true if the two characters copy as the same text: the letters of
    // a presentation form, as the ToUnicode map gives them.
    static boolean sameLetters(int a, int b) {
        String la = Bidi.lettersOf(a);
        String lb = Bidi.lettersOf(b);
        return (la != null ? la : new String(Character.toChars(a)))
                .equals(lb != null ? lb : new String(Character.toChars(b)));
    }

    /**
     * Returns the width of the string drawn with this font at its current size.
     *
     * @param str the string.
     * @return the width of the string.
     */
    public float stringWidth(String str) {
        return stringWidth(this.size, str);
    }

    /**
     * Returns the width of the specified string when drawn on the page with this
     * font using the current font size.
     *
     * @param fontSize the font size.
     * @param str the specified string.
     *
     * @return the width of the string when draw on the page with this font using
     *         the current selected size.
     */
    public float stringWidth(float fontSize, String str) {
        float width = 0f;

        if (str == null) {
            return width;
        }

        if (isCJK) {
            // Every glyph of a CJK font is as wide as the font size, and the
            // characters not drawn take no width.
            return str.codePoints().filter(cp -> !isNotDrawn(cp)).count() * fontSize;
        }

        if (isCoreFont) {
            str = withoutIgnorables(str);
            for (int i = 0; i < str.length(); ) {
                int c1 = coreFontCode(str.codePointAt(i));
                i += Character.charCount(str.codePointAt(i));
                width += metrics[c1 - 32][1];
                if (kernPairs && i < str.length()) {
                    width += kerning(c1, coreFontCode(str.codePointAt(i)));
                }
            }
        } else {
            for (int i = 0; i < str.length(); ) {
                int cp = str.codePointAt(i);
                i += Character.charCount(cp);
                if (cp >= 0x20 && cp < 0x7F && asciiGID[cp - 0x20] != 0) {
                    width += asciiAdvance[cp - 0x20];
                } else {
                    width += advanceWidthOf(cp);
                }
            }
            if (kerns()) {
                width += textKerning(str);
            }
        }

        return width * fontSize / unitsPerEm;
    }

    // Returns the advance width, in font units, of the glyph that Page draws
    // for the character, the one glyphOf gives: none for an RLM, LRM, ZWNJ,
    // ZWJ or byte order mark, which are not drawn, that of a space for a
    // control character and that of the glyph missingGlyph gives for a
    // character the font does not have.
    private int advanceWidthOf(int cp) {
        if (isJoinerOrRLM(cp) || cp == 0xFEFF || isIgnorable(cp)) {
            return 0;
        }
        return glyphAdvance(Page.glyphOf(this, cp));
    }

    // Reads the glyph and the advance width of each printable ASCII character
    // the font has into asciiGID and asciiAdvance, once its character map and
    // its widths are read. They are those glyphOf and advanceWidthOf give: none
    // of these characters is a control character, an ignorable one or a mark.
    void setASCIIGlyphs() {
        if (advanceWidth == null || advanceWidth.length == 0) {
            return;
        }
        for (int c = 0x20; c < 0x7F; c++) {
            if (!lacks(c)) {
                int gid = unicodeToGID.get(c);
                asciiGID[c - 0x20] = gid;
                asciiAdvance[c - 0x20] = glyphAdvance(gid);
            }
        }
    }

    // Returns the advance width of the glyph, in font units. A font can list
    // fewer advance widths than it has glyphs, and the glyphs past the end have
    // the width of the last one, as OpenType says and the PDF font's /DW does.
    int glyphAdvance(int gid) {
        return advanceWidth[Math.min(gid, advanceWidth.length - 1)];
    }

    // Returns true if the font has a glyph for the character, so that it is
    // not drawn with .notdef and the fallback font is not needed for it. A
    // character past the end of the glyph table of the font, like an emoji,
    // has none. A control character is drawn as a space, and a core font has
    // the characters of its encoding.
    boolean hasGlyph(int cp) {
        if (isControl(cp)) {
            cp = 0x20;
        }
        if (isCoreFont) {
            return cp == 32 || coreFontCode(cp) != 32;
        }
        return !isCJK && !lacks(cp);
    }

    // Returns true if the font has no glyph for the character, which is not a
    // control character. The character map of the font is read from its first
    // to its last character, so a character outside that range has no glyph.
    boolean lacks(int cp) {
        if (isControl(cp)) {
            return false;
        }
        return cp < firstChar || cp > lastChar || unicodeToGID.get(cp) == 0;
    }

    // Returns the glyph a character the font does not have is drawn with:
    // .notdef, glyph 0, in a document of no compliance. PDF/UA and PDF/A
    // forbid .notdef -- ISO 14289-1 7.21.8 and ISO 19005-2 6.2.11.8 -- so a
    // compliant document draws the replacement character U+FFFD of the font,
    // or a question mark, or a space, the first the font has, and the
    // character is its actual text. A font of a PDF that was read is of no
    // compliance.
    int missingGlyph() {
        if (pdf == null || pdf.compliance == Compliance.PDF_1_7) {
            return 0;
        }
        for (int cp : new int[] {0xFFFD, '?', ' '}) {
            if (cp >= firstChar && cp <= lastChar && unicodeToGID.get(cp) != 0) {
                return unicodeToGID.get(cp);
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
    static Font fontOf(Font font, Font fallbackFont, Font active, String str, int i) {
        int cp = str.codePointAt(i);
        if (i > 0 && Character.getType(cp) == Character.NON_SPACING_MARK && active.hasGlyph(cp)) {
            return active;
        }
        int next = i + Character.charCount(cp);
        if (isJoinerOrRLM(cp) && next < str.length()) {
            cp = str.codePointAt(next);
        }
        if (font.hasGlyph(cp) || !fallbackFont.hasGlyph(cp)) {
            return font;
        }
        return fallbackFont;
    }

    /**
     * Returns the ascent of this font.
     *
     * @return the ascent of the font.
     */
    public float getAscent() {
        return ascent;
    }

    /**
     * Returns the descent of this font.
     *
     * @return the descent of the font.
     */
    public float getDescent() {
        return descent;
    }

    /**
     * Returns the ascent of this font at the specified size.
     *
     * @param fontSize the font size.
     * @return the ascent.
     */
    public float getAscent(float fontSize) {
        if (isCJK) {
            return fontSize;
        }
        return fontAscent * fontSize / unitsPerEm;
    }

    /**
     * Returns the descent of this font at the specified size.
     *
     * @param fontSize the font size.
     * @return the descent.
     */
    public float getDescent(float fontSize) {
        if (isCJK) {
            return fontSize/4;
        }
        return -fontDescent * fontSize / unitsPerEm;
    }

    /**
     * Returns the line gap of this font at the specified size: the space the
     * font puts between the descent of a line and the ascent of the next one.
     * It is 0 for most fonts, which leave that space in their ascent and descent,
     * and 1 em for the Japanese and Chinese IBM Plex fonts, whose ascent and
     * descent add up to 1 em.
     *
     * @param fontSize the font size.
     * @return the line gap.
     */
    public float getLineGap(float fontSize) {
        if (isCJK) {
            return 0f;
        }
        return fontLineGap * fontSize / unitsPerEm;
    }

    /**
     * Returns the ascent plus the descent of this font at the specified size.
     *
     * @param fontSize the font size.
     * @return the body height.
     */
    public float getBodyHeight(float fontSize) {
        return getAscent(fontSize) + getDescent(fontSize);
    }

    /**
     * Returns the underline thickness of this font at the specified size.
     *
     * @param fontSize the font size.
     * @return the underline thickness.
     */
    public float getUnderlineThickness(float fontSize) {
        return (fontUnderlineThickness * fontSize / unitsPerEm);
    }

    /**
     * Returns the underline position of this font at the specified size.
     *
     * @param fontSize the font size.
     * @return the underline position.
     */
    public float getUnderlinePosition(float fontSize) {
        return -(fontUnderlinePosition * fontSize / unitsPerEm) + getUnderlineThickness(fontSize) / 2.0f;
    }

    /**
     * Returns the height of the body of the font.
     *
     * @return float the height of the body of the font.
     */
    public float getBodyHeight() {
        return bodyHeight;
    }

    /**
     * Returns the number of characters from the specified string that will fit
     * within the specified width. They are counted in the chars of the string,
     * so that a character outside the Basic Multilingual Plane is two, and
     * str.substring(0, n) is the text that fits.
     *
     * @param str   the specified string.
     * @param width the specified width.
     *
     * @return the number of characters that will fit.
     */
    public int getFitChars(String str, float width) {
        // Text of no size has no width, and all of it fits in any width.
        if (size <= 0f) {
            return (width < 0f) ? 0 : str.length();
        }
        float w = width * unitsPerEm / size;

        if (isCJK) {
            // Every glyph of a CJK font is as wide as the font size, and the
            // characters not drawn take no width.
            int room = Math.max(0, (int) (width / size));
            int i = 0;
            while (i < str.length()) {
                int cp = str.codePointAt(i);
                if (!isNotDrawn(cp)) {
                    if (room == 0) {
                        break;
                    }
                    room--;
                }
                i += Character.charCount(cp);
            }
            return i;
        }

        if (isCoreFont) {
            return getCoreFontFitChars(str, w);
        }

        boolean kerned = kerns();
        int previous = -1;
        int i = 0;
        while (i < str.length()) {
            int cp = str.codePointAt(i);
            int advance = advanceWidthOf(cp);
            // A character fits with its kerning with the one drawn before it,
            // as stringWidth measures it: a pair kerned wider takes its room
            // only when its second character is there.
            if (kerned) {
                int gid = kerningGlyph(cp);
                if (gid >= 0) {
                    if (previous >= 0) {
                        advance += glyphKerning(previous, gid);
                    }
                    previous = gid;
                }
            }
            w -= advance;
            if (w < 0) {
                break;
            }
            i += Character.charCount(cp);
        }

        return i;
    }

    private int getCoreFontFitChars(String str, float width) {
        float w = width;
        int previous = -1;

        int i = 0;
        while (i < str.length()) {
            int cp = str.codePointAt(i);
            if (notDrawn(cp)) {
                i += Character.charCount(cp);
                continue;
            }
            int c1 = coreFontCode(cp);
            float advance = metrics[c1 - 32][1];
            // The kerning is with the character drawn before, as
            // stringWidth's is, and takes its room only when this one is there.
            if (kernPairs && previous >= 0) {
                advance += kerning(previous, c1);
            }
            previous = c1;
            w -= advance;
            if (w < 0) {
                return i;
            }

            i += Character.charCount(cp);
        }

        return i;
    }

    // Returns the text without the characters a core font of WinAnsi does not
    // draw or measure, as no font does: the soft hyphen, as WinAnsi has a
    // hyphen at its code, and a line broken at a soft hyphen ends with a hyphen
    // of its own (review of 11 October 2026, #21), and the other characters not
    // drawn, the marks of direction, the joiners, the byte order mark and those
    // isIgnorable returns true for, which were drawn as spaces (review of 12
    // October 2026, R14). Symbol and ZapfDingbats keep theirs, their codes
    // being their characters.
    String withoutIgnorables(String str) {
        if (symbolic || str.codePoints().noneMatch(this::notDrawn)) {
            return str;
        }
        StringBuilder sb = new StringBuilder(str.length());
        str.codePoints().filter(cp -> !notDrawn(cp)).forEach(sb::appendCodePoint);
        return sb.toString();
    }

    // Returns true for a character that a core font of WinAnsi does not draw
    // or measure, see withoutIgnorables.
    private boolean notDrawn(int cp) {
        return !symbolic && isNotDrawn(cp);
    }

    // Returns true for a character a font that is not embedded, a core font of
    // WinAnsi or a CJK font, does not draw or measure: the marks of direction,
    // the joiners, the byte order mark and those isIgnorable returns true for, the
    // soft hyphen among them. A CJK font drew each 1 em wide (review of 12
    // October 2026, R20).
    static boolean isNotDrawn(int cp) {
        return isIgnorable(cp) || isJoinerOrRLM(cp) || cp == 0xFEFF;
    }

    /**
     * Returns the code of the character in the encoding of this core font:
     * WinAnsi, which puts ’ at 146, “ and ” at 147 and 148, the dashes at 150
     * and 151 and € at 128, or, for Symbol and ZapfDingbats, the character
     * itself. A character that is not in the encoding is drawn as a space.
     *
     * @param cp the code point of the character.
     * @return the code, from 32 to 255.
     */
    int coreFontCode(int cp) {
        if (!symbolic) {
            cp = winAnsiCode(cp);
        }
        return (cp < 32 || cp > 255) ? 32 : cp;
    }

    // The WinAnsi code of the character: the character itself below 127 and
    // from 160 to 255, the code of the character WinAnsi puts from 128 to 159,
    // and a space for any other character, the controls U+007F to U+009F too.
    // WinAnsi draws a bullet at 127, ISO 32000, Annex D, where the widths of
    // the core fonts have a space.
    private static int winAnsiCode(int cp) {
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

    /**
     * Returns the kerning of a pair of characters of this core font, in 1/1000
     * of the font size: negative when the second character moves closer to the
     * first, and 0 when the font has no kerning for the pair.
     *
     * @param c1 the code of the first character, from 32 to 255.
     * @param c2 the code of the second character, from 32 to 255.
     * @return the kerning.
     */
    int kerning(int c1, int c2) {
        int[] row = metrics[c1 - 32];
        for (int j = 2; j < row.length; j += 2) {
            if (row[j] == c2) {
                return row[j + 1];
            }
        }
        return 0;
    }

    /**
     * Sets the skew15 private variable.
     * When the variable is set to 'true' all glyphs in the font are skewed on 15
     * degrees.
     * This makes a regular font look like an italic type font.
     * Use this method when you don't have real italic font in the font family,
     * or when you want to generate smaller PDF files.
     * For example you could embed only the Regular and Bold fonts and synthesize
     * the RegularItalic and BoldItalic.
     *
     * @param skew15 the skew flag.
     * @return this Font object.
     */
    public Font setItalic(boolean skew15) {
        this.skew15 = skew15;
        return this;
    }

    /**
     * Returns the width of the string at this font's current size, measuring the characters
     * this font does not have with the fallback font.
     *
     * @param fallbackFont the fallback font.
     * @param str the string.
     * @return the width of the string.
     */
    public float stringWidth(Font fallbackFont, String str) {
        return stringWidth(fallbackFont, this.size, str);
    }

    // Returns true for the right-to-left and left-to-right marks and the zero
    // width non-joiner and joiner, which are not drawn: Page gives the glyph
    // before or after them an actual text.
    static boolean isJoinerOrRLM(int ch) {
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
    static boolean isIgnorable(int ch) {
        if (ch == 0x00AD) {
            return true;
        }
        if (ch < 0x034F) {
            return false;
        }
        return ch == 0x034F || ch == 0x061C || ch == 0x115F || ch == 0x1160
                || ch == 0x17B4 || ch == 0x17B5
                || (ch >= 0x180B && ch <= 0x180F) || ch == 0x200B
                || (ch >= 0x202A && ch <= 0x202E)
                || (ch >= 0x2060 && ch <= 0x206F) || ch == 0x3164
                || (ch >= 0xFE00 && ch <= 0xFE0F) || ch == 0xFFA0
                || (ch >= 0xFFF0 && ch <= 0xFFF8)
                || (ch >= 0x1BCA0 && ch <= 0x1BCA3)
                || (ch >= 0x1D173 && ch <= 0x1D17A)
                || (ch >= 0xE0000 && ch <= 0xE0FFF);
    }

    // Returns true for the C0 and C1 control characters and DEL, U+0000 to
    // U+001F and U+007F to U+009F, which have no glyph to draw: they are drawn
    // as a space.
    static boolean isControl(int ch) {
        return ch < 0x20 || (ch >= 0x7F && ch <= 0x9F);
    }

    /**
     * Returns the width of a string drawn using two fonts.
     *
     * @param fallbackFont the fallback font.
     * @param fontSize the font size.
     * @param str the string.
     * @return the width.
     */
    public float stringWidth(Font fallbackFont, float fontSize, String str) {
        return stringWidth(fallbackFont, fontSize, fontSize, str);
    }

    // Returns the width of a string drawn using two fonts, with the characters
    // in the fallback font at the fallback font size.
    float stringWidth(Font fallbackFont, float fontSize, float fallbackFontSize, String str) {
        float width = 0f;

        // A fallback font that is the font itself measures every character
        // with the font, as with no fallback font
        if (str == null || this.isCJK || fallbackFont == null || fallbackFont.isCJK ||
                (fallbackFont == this && fallbackFontSize == fontSize)) {
            return stringWidth(fontSize, str);
        }

        Font activeFont = this;
        // The runs of text drawn with one font are the pieces of the string
        // between the characters that switch fonts, so measuring them copies
        // nothing: a string that is all in the primary font is one piece.
        int start = 0;
        for (int i = 0; i < str.length(); i += Character.charCount(str.codePointAt(i))) {
            Font charFont = fontOf(this, fallbackFont, activeFont, str, i);
            if (charFont != activeFont) {
                width += activeFont.stringWidth(
                        activeFont == this ? fontSize : fallbackFontSize, str.substring(start, i));
                start = i;
                activeFont = charFont;
            }
        }
        width += activeFont.stringWidth(
                activeFont == this ? fontSize : fallbackFontSize, str.substring(start));

        return width;
    }
} // End of Font.java
