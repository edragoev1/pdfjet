/*
 * FontStream1.java
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
package com.pdfjet;

import com.pdfjet.encryption.*;
import java.io.*;
import java.nio.charset.StandardCharsets;
import java.util.*;

class FontStream1 {
    protected static void register(
            PDF pdf,
            Font font,
            InputStream inputStream) throws Exception {
        getFontData(font, inputStream);
        embedFontFile(pdf, font, inputStream);
        addFontDescriptorObject(pdf, font);
        addCIDFontDictionaryObject(pdf, font);
        addToUnicodeCMapObject(pdf, font);

        // Type0 Font Dictionary
        pdf.newObj();
        pdf.append(Token.BEGIN_DICTIONARY);
        pdf.append("/Type /Font\n");
        pdf.append("/Subtype /Type0\n");
        pdf.append("/BaseFont /");
        pdf.append(font.name.getBytes(StandardCharsets.UTF_8));
        pdf.append(Token.NEWLINE);
        pdf.append("/Encoding /Identity-H\n");
        pdf.append("/DescendantFonts [");
        pdf.append(font.cidFontDictObjNumber);
        pdf.append(" 0 R]\n");
        pdf.append("/ToUnicode ");
        pdf.append(font.toUnicodeCMapObjNumber);
        pdf.append(" 0 R\n");
        pdf.append(Token.END_DICTIONARY);
        pdf.endObj();
        font.objNumber = pdf.getObjNumber();
        pdf.fonts.add(font);
    }

    private static void embedFontFile(
            PDF pdf, Font font, InputStream inputStream) throws Exception {
        // Check if the font file is already embedded
        for (Font f : pdf.fonts) {
            if (f.fileObjNumber != 0 && f.name.equals(font.name)) {
                font.fileObjNumber = f.fileObjNumber;
                return;
            }
        }

        int metadataObjNumber = pdf.addMetadataObject(font.info, true);

        pdf.newObj();
        pdf.append(Token.BEGIN_DICTIONARY);

        pdf.append("/Metadata ");
        pdf.append(metadataObjNumber);
        pdf.append(" 0 R\n");

        if (font.cff) {
            pdf.append("/Subtype /CIDFontType0C\n");
        } else {
            pdf.append("/Length1 ");
            pdf.append(font.uncompressedSize);
            pdf.append(Token.NEWLINE);
        }
        pdf.append("/Filter /FlateDecode\n");

        byte[] compressed = null;
        byte[] encrypted = null;
        try {
            compressed = readBytes(inputStream, font.compressedSize);
        } finally {
            inputStream.close();
        }
        if (pdf.encryption != null) {
            encrypted = AES256.encrypt(compressed, pdf.encryption.getKey());
        }

        pdf.append("/Length ");
        if (pdf.encryption != null) {
            pdf.append(encrypted.length);
        } else {
            pdf.append(font.compressedSize);
        }
        pdf.append(Token.NEWLINE);
        pdf.append(Token.END_DICTIONARY);
        pdf.append(Token.STREAM);
        if (pdf.encryption != null) {
            pdf.append(encrypted);
        } else {
            pdf.append(compressed);
        }
        pdf.append(Token.END_STREAM);
        pdf.endObj();

        font.fileObjNumber = pdf.getObjNumber();
    }

    private static void addFontDescriptorObject(PDF pdf, Font font) throws Exception {
        for (Font f : pdf.fonts) {
            if (f.fontDescriptorObjNumber != 0 && f.name.equals(font.name)) {
                font.fontDescriptorObjNumber = f.fontDescriptorObjNumber;
                return;
            }
        }

        pdf.newObj();
        pdf.append("<<\n");
        pdf.append("/Type /FontDescriptor\n");
        pdf.append("/FontName /");
        pdf.append(font.name);
        pdf.append('\n');
        if (font.cff) {
            pdf.append("/FontFile3 ");
        } else {
            pdf.append("/FontFile2 ");
        }
        pdf.append(font.fileObjNumber);
        pdf.append(" 0 R\n");
        pdf.append("/Flags 32\n");
        pdf.append("/FontBBox [");
        pdf.append(OpenTypeFont.toGlyphSpace(font.bBoxLLx, font.unitsPerEm));
        pdf.append(' ');
        pdf.append(OpenTypeFont.toGlyphSpace(font.bBoxLLy, font.unitsPerEm));
        pdf.append(' ');
        pdf.append(OpenTypeFont.toGlyphSpace(font.bBoxURx, font.unitsPerEm));
        pdf.append(' ');
        pdf.append(OpenTypeFont.toGlyphSpace(font.bBoxURy, font.unitsPerEm));
        pdf.append("]\n");
        pdf.append("/Ascent ");
        pdf.append(OpenTypeFont.toGlyphSpace(font.fontAscent, font.unitsPerEm));
        pdf.append('\n');
        pdf.append("/Descent ");
        pdf.append(OpenTypeFont.toGlyphSpace(font.fontDescent, font.unitsPerEm));
        pdf.append('\n');
        pdf.append("/ItalicAngle 0\n");
        pdf.append("/CapHeight ");
        pdf.append(OpenTypeFont.toGlyphSpace(font.capHeight, font.unitsPerEm));
        pdf.append('\n');
        pdf.append("/StemV 79\n");
        pdf.append(">>\n");
        pdf.endObj();

        font.fontDescriptorObjNumber = pdf.getObjNumber();
    }

    private static void addToUnicodeCMapObject(PDF pdf, Font font) throws Exception {
        for (Font f : pdf.fonts) {
            if (f.toUnicodeCMapObjNumber != 0 && f.name.equals(font.name)) {
                font.toUnicodeCMapObjNumber = f.toUnicodeCMapObjNumber;
                return;
            }
        }

        StringBuilder sb = new StringBuilder();
        sb.append("/CIDInit /ProcSet findresource begin\n");
        sb.append("12 dict begin\n");
        sb.append("begincmap\n");
        sb.append("/CIDSystemInfo <</Registry (Adobe) /Ordering (Identity) /Supplement 0>> def\n");
        sb.append("/CMapName /Adobe-Identity def\n");
        sb.append("/CMapType 2 def\n");

        sb.append("1 begincodespacerange\n");
        sb.append("<0000> <FFFF>\n");
        sb.append("endcodespacerange\n");

        List<String> list = new ArrayList<String>();
        // A character the font does not contain is drawn with the .notdef
        // glyph. PDF/UA requires every glyph to map to Unicode, so map it to
        // the replacement character.
        list.add("<0000> <FFFD>\n");
        StringBuilder buf = new StringBuilder();
        int[] unicodeOf = unicodeOfGlyphs(font.unicodeToGID);
        for (int cid = 0; cid <= 0xffff; cid++) {
            int gid = font.unicodeToGID[cid];
            if (gid > 0 && unicodeOf[gid] == cid) {
                buf.append('<');
                buf.append(toHexString(gid));
                buf.append("> <");
                // A presentation form that the Bidi class puts in maps to the letters it stands for.
                String letters = Bidi.lettersOf(cid);
                if (letters == null) {
                    buf.append(toHexString(cid));
                } else {
                    for (int i = 0; i < letters.length(); i++) {
                        buf.append(toHexString(letters.charAt(i)));
                    }
                }
                buf.append(">\n");
                list.add(buf.toString());
                buf.setLength(0);
                if (list.size() == 100) {
                    writeListToBuffer(sb, list);
                }
            }
        }
        if (list.size() > 0) {
            writeListToBuffer(sb, list);
        }

        sb.append("endcmap\n");
        sb.append("CMapName currentdict /CMap defineresource pop\n");
        sb.append("end\nend");

        byte[] buf2 = sb.toString().getBytes(java.nio.charset.StandardCharsets.UTF_8);
        if (pdf.encryption != null) {
            buf2 = AES256.encrypt(buf2, pdf.encryption.getKey());
        }

        pdf.newObj();
        pdf.append("<<\n");
        pdf.append("/Length ");
        pdf.append(buf2.length);
        pdf.append("\n");
        pdf.append(">>\n");
        pdf.append("stream\n");
        pdf.append(buf2);
        pdf.append("\nendstream\n");
        pdf.endObj();

        font.toUnicodeCMapObjNumber = pdf.getObjNumber();
    }

    private static void addCIDFontDictionaryObject(PDF pdf, Font font) throws Exception {
        for (Font f : pdf.fonts) {
            if (f.cidFontDictObjNumber != 0 && f.name.equals(font.name)) {
                font.cidFontDictObjNumber = f.cidFontDictObjNumber;
                return;
            }
        }

        pdf.newObj();
        pdf.append("<<\n");
        pdf.append("/Type /Font\n");
        if (font.cff) {
            pdf.append("/Subtype /CIDFontType0\n");
        } else {
            pdf.append("/Subtype /CIDFontType2\n");
        }
        pdf.append("/BaseFont /");
        pdf.append(font.name);
        pdf.append('\n');

        byte[] registry = "Adobe".getBytes(java.nio.charset.StandardCharsets.UTF_8);
        byte[] ordering = "Identity".getBytes(java.nio.charset.StandardCharsets.UTF_8);
        if (pdf.encryption != null) {
            registry = AES256.encrypt(registry, pdf.encryption.getKey());
            ordering = AES256.encrypt(ordering, pdf.encryption.getKey());
        }
        pdf.append("/CIDSystemInfo <</Registry <");
        pdf.append(Util.toHexString(registry));
        pdf.append("> /Ordering <");
        pdf.append(Util.toHexString(ordering));
        pdf.append("> /Supplement 0>>\n");

        pdf.append("/FontDescriptor ");
        pdf.append(font.fontDescriptorObjNumber);
        pdf.append(" 0 R\n");

        final float k = 1000.0f / (float) font.unitsPerEm;
        // The width of the glyphs past the /W array: those past the advance
        // widths, which have the width of the last one.
        pdf.append("/DW ");
        pdf.append(Math.round(k * (float) font.advanceWidth[font.advanceWidth.length - 1]));
        pdf.append('\n');

        pdf.append("/W [0[\n");
        for (int width : font.advanceWidth) {
            pdf.append(Math.round(k * (float) width));
            pdf.append(' ');
        }
        pdf.append("]]\n");

        pdf.append("/CIDToGIDMap /Identity\n");
        pdf.append(">>\n");
        pdf.endObj();

        font.cidFontDictObjNumber = pdf.getObjNumber();
    }

    /**
     * Returns the character that each glyph maps to in a ToUnicode CMap. A
     * glyph can stand for several characters, like the space and the no-break
     * space, but viewers read a CMap with more than one entry for a glyph
     * differently. So a glyph maps to the first character that uses it, unless
     * that is one text seldom has and another character uses the glyph too,
     * like the modifier letter apostrophe and the right single quotation mark.
     */
    static int[] unicodeOfGlyphs(int[] unicodeToGID) {
        int[] unicode = new int[0x10000];
        Arrays.fill(unicode, -1);
        for (int cid = 0; cid <= 0xffff; cid++) {
            int gid = unicodeToGID[cid];
            if (gid > 0 && (unicode[gid] == -1 ||
                    (isSeldomText(unicode[gid]) && !isSeldomText(cid)))) {
                unicode[gid] = cid;
            }
        }
        return unicode;
    }

    // The soft hyphen, the spacing modifier letters, the combining marks, the
    // figure dash, the deprecated angle brackets, the CJK and Kangxi radicals,
    // which CJK fonts draw with the glyphs of the ideographs, and the private
    // use characters.
    private static boolean isSeldomText(int ch) {
        return ch == 0x00AD ||
                (ch >= 0x02B0 && ch <= 0x036F) ||
                (ch >= 0x1AB0 && ch <= 0x1AFF) ||
                (ch >= 0x1DC0 && ch <= 0x1DFF) ||
                ch == 0x2012 ||
                (ch >= 0x20D0 && ch <= 0x20FF) ||
                (ch >= 0x2329 && ch <= 0x232A) ||
                (ch >= 0x2E80 && ch <= 0x2FDF) ||
                (ch >= 0xE000 && ch <= 0xF8FF) ||
                (ch >= 0xFE20 && ch <= 0xFE2F);
    }

    protected static String toHexString(int code) {
        String str = Integer.toHexString(code);
        if (str.length() == 1) {
            return "000" + str;
        } else if (str.length() == 2) {
            return "00" + str;
        } else if (str.length() == 3) {
            return "0" + str;
        }
        return str;
    }

    protected static void writeListToBuffer(StringBuilder sb, List<String> list) {
        sb.append(list.size());
        sb.append(" beginbfchar\n");
        for (String str : list) {
            sb.append(str);
        }
        sb.append("endbfchar\n");
        list.clear();
    }

    // The most bytes the metrics of a stream font decode to, with its marks
    // compressed in them. IBM Plex Sans JP has 180 KB.
    private static final int MAX_FONT_METRICS_LENGTH = 16 * 1024 * 1024;

    private static IOException fontStreamError(String what) {
        return new IOException("Invalid font stream: " + what + ".");
    }

    // Returns true if the name can be written as a PDF name as it is:
    // printable ASCII, and none of the characters that end a name or start an
    // escape.
    private static boolean isFontName(byte[] name) {
        if (name.length == 0) {
            return false;
        }
        for (byte b : name) {
            if (b < 0x21 || b > 0x7E || "()<>[]{}/%#".indexOf(b) != -1) {
                return false;
            }
        }
        return true;
    }

    private static int getByte(InputStream stream) throws IOException {
        int b = stream.read();
        if (b == -1) {
            throw new EOFException("Unexpected end of the font stream.");
        }
        return b;
    }

    private static int getInt24(InputStream stream) throws IOException {
        return getByte(stream) << 16 | getByte(stream) << 8 | getByte(stream);
    }

    private static long getUInt32(InputStream stream) throws IOException {
        return (long) getByte(stream) << 24 | (long) getByte(stream) << 16 |
                (long) getByte(stream) << 8 | (long) getByte(stream);
    }

    // A size that must fit an int, which any font file does.
    private static int getSize(InputStream stream) throws IOException {
        long size = getUInt32(stream);
        if (size > Integer.MAX_VALUE) {
            throw fontStreamError("the size of the font file");
        }
        return (int) size;
    }

    // Reads the next length bytes. It reads them as they come, so a length
    // that the stream does not have takes no memory; a single read may return
    // fewer bytes than asked for.
    static byte[] readBytes(InputStream stream, long length) throws IOException {
        ByteArrayOutputStream buf = new ByteArrayOutputStream();
        byte[] buffer = new byte[4096];
        long remaining = length;
        while (remaining > 0) {
            int n = stream.read(buffer, 0, (int) Math.min(remaining, buffer.length));
            if (n <= 0) {
                throw new EOFException("Unexpected end of the font stream.");
            }
            buf.write(buffer, 0, n);
            remaining -= n;
        }
        return buf.toByteArray();
    }

    private static void skipFully(InputStream stream, long count) throws IOException {
        byte[] buffer = new byte[4096];
        while (count > 0) {
            int n = stream.read(buffer, 0, (int) Math.min(count, buffer.length));
            if (n <= 0) {
                throw new EOFException("Unexpected end of the font stream.");
            }
            count -= n;
        }
    }

    // Reads the metrics of a stream font from a byte array, after checking
    // that the bytes are there.
    private static final class Metrics {
        private final byte[] data;
        private final String what;
        private int pos = 0;

        Metrics(byte[] data, String what) {
            this.data = data;
            this.what = what;
        }

        int remaining() {
            return data.length - pos;
        }

        // Checks that count entries of size bytes follow.
        void need(int count, int size) throws IOException {
            if (count < 0 || count > remaining() / size) {
                throw fontStreamError(what + " end too soon");
            }
        }

        int getInt32() throws IOException {
            need(1, 4);
            int v = (data[pos] & 0xFF) << 24 | (data[pos + 1] & 0xFF) << 16 |
                    (data[pos + 2] & 0xFF) << 8 | (data[pos + 3] & 0xFF);
            pos += 4;
            return v;
        }

        // Reads the number of entries of size bytes that follow.
        int getCount(int size) throws IOException {
            int count = getInt32();
            need(count, size);
            return count;
        }

        int getUInt16() {
            int v = (data[pos] & 0xFF) << 8 | (data[pos + 1] & 0xFF);
            pos += 2;
            return v;
        }

        byte[] getBytes(int length) {
            byte[] bytes = Arrays.copyOfRange(data, pos, pos + length);
            pos += length;
            return bytes;
        }
    }

    // Reads where the marks go, which getFontData keeps compressed: for each
    // MarkToBase and MarkToLigature subtable the class and anchor of each mark
    // and the anchors of each letter, and then the offsets of the marks that go
    // on other marks. Done once, the first time a mark is drawn in the font.
    static void readMarks(Font font) throws Exception {
        Metrics stream = new Metrics(
                Decompressor.inflate(font.markData, MAX_FONT_METRICS_LENGTH), "the marks");
        int subTables = stream.getCount(8);
        List<Map<Integer, int[]>> markAnchors = new ArrayList<Map<Integer, int[]>>(subTables);
        List<Map<Integer, int[]>> baseAnchors = new ArrayList<Map<Integer, int[]>>(subTables);
        for (int i = 0; i < subTables; i++) {
            int count = stream.getCount(16);
            Map<Integer, int[]> marks = new HashMap<Integer, int[]>(2*count);
            for (int j = 0; j < count; j++) {
                int gid = stream.getInt32();
                int markClass = stream.getInt32();
                // The anchors of a letter are 3 ints for each mark class.
                if (markClass < 0 || markClass > 0xFFFF) {
                    throw fontStreamError("a mark class");
                }
                marks.put(gid, new int[] {markClass, stream.getInt32(), stream.getInt32()});
            }
            count = stream.getCount(8);
            Map<Integer, int[]> bases = new HashMap<Integer, int[]>(2*count);
            for (int j = 0; j < count; j++) {
                int gid = stream.getInt32();
                int[] anchors = new int[stream.getCount(4)];
                for (int k = 0; k < anchors.length; k++) {
                    anchors[k] = stream.getInt32();
                }
                bases.put(gid, anchors);
            }
            markAnchors.add(marks);
            baseAnchors.add(bases);
        }
        int count = stream.getCount(16);
        Map<Integer, int[]> markToMarkOffsets = new HashMap<Integer, int[]>(2*count);
        for (int j = 0; j < count; j++) {
            int other = stream.getInt32();
            int mark = stream.getInt32();
            markToMarkOffsets.put((other << 16) | mark, new int[] {stream.getInt32(), stream.getInt32()});
        }
        font.markAnchors = markAnchors;
        font.baseAnchors = baseAnchors;
        font.markToMarkOffsets = markToMarkOffsets;
        font.markData = null;
    }

    protected static void getFontData(Font font, InputStream inputStream) throws Exception {
        byte[] fontName = readBytes(inputStream, getByte(inputStream));
        if (!isFontName(fontName)) {
            throw fontStreamError("the font name");
        }
        font.name = UTF8.decode(fontName);
        font.info = UTF8.decode(readBytes(inputStream, getInt24(inputStream)));

        Metrics metrics = new Metrics(Decompressor.inflate(
                readBytes(inputStream, getUInt32(inputStream)), MAX_FONT_METRICS_LENGTH), "the metrics");
        font.unitsPerEm = metrics.getInt32();
        font.bBoxLLx = metrics.getInt32();
        font.bBoxLLy = metrics.getInt32();
        font.bBoxURx = metrics.getInt32();
        font.bBoxURy = metrics.getInt32();
        font.fontAscent = metrics.getInt32();
        font.fontDescent = metrics.getInt32();
        font.firstChar = metrics.getInt32();
        font.lastChar = metrics.getInt32();
        font.capHeight = metrics.getInt32();
        font.fontUnderlinePosition = metrics.getInt32();
        font.fontUnderlineThickness = metrics.getInt32();
        // The range OpenType allows; the sizes of the text are divided by it.
        if (font.unitsPerEm < 16 || font.unitsPerEm > 16384) {
            throw fontStreamError("the units per em");
        }
        // A character in the range is looked up in unicodeToGID.
        if (font.firstChar < 0 || font.lastChar > 0xFFFF) {
            throw fontStreamError("the first or last character");
        }

        int len = metrics.getCount(2);
        if (len == 0) {
            throw fontStreamError("no advance widths");
        }
        font.advanceWidth = new int[len];
        for (int i = 0; i < len; i++) {
            font.advanceWidth[i] = metrics.getUInt16();
        }

        len = metrics.getCount(2);
        if (len != 0x10000) {
            throw fontStreamError("the character map");
        }
        font.unicodeToGID = new int[len];
        for (int i = 0; i < len; i++) {
            font.unicodeToGID[i] = metrics.getUInt16();
        }

        // Where the GPOS table of the font puts the marks, compressed on its
        // own after the metrics of a stream that has them. It is kept as it is
        // and read when a mark is drawn in the font; see readMarks. A font with
        // no marks has none, or 0 bytes of them.
        if (metrics.remaining() > 0) {
            byte[] markData = metrics.getBytes(metrics.getCount(1));
            if (markData.length > 0) {
                font.markData = markData;
            }
        }
        // The line gap of a font that has one follows the marks, where a library
        // that does not read it stops.
        if (metrics.remaining() > 0) {
            font.fontLineGap = metrics.getInt32();
        }

        int flag = getByte(inputStream);
        if (flag == 'R') {
            // The tables of an OpenType font that are not in its CFF data,
            // which keep the font whole; they are not embedded.
            skipFully(inputStream, getUInt32(inputStream));
            flag = getByte(inputStream);
        }
        font.cff = flag == 'Y';
        font.uncompressedSize = getSize(inputStream);
        font.compressedSize = getSize(inputStream);
    }
}   // End of FontStream1.java
