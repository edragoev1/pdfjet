/*
 * FontWriter.java
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
package com.pdfjet;

import com.pdfjet.encryption.*;
import java.util.*;

/**
 * The objects of an embedded font that every font writes, whatever its
 * outlines: its descriptor, its CID font with the widths of its glyphs, and
 * its ToUnicode map.
 */
class FontWriter {
    // Writes the font descriptor with the name given, which is the font's own,
    // or that of a subset.
    static void addFontDescriptorObject(PDF pdf, Font font, String fontName) throws Exception {
        for (Font f : pdf.fonts) {
            if (f.fontDescriptorObjNumber != 0 && f.name.equals(font.name) && f.checksum == font.checksum) {
                font.fontDescriptorObjNumber = f.fontDescriptorObjNumber;
                return;
            }
        }

        pdf.newObj();
        pdf.append("<<\n");
        pdf.append("/Type /FontDescriptor\n");
        pdf.append("/FontName /");
        pdf.append(fontName);
        pdf.append('\n');
        if (font.cff) {
            pdf.append("/FontFile3 ");
        } else {
            pdf.append("/FontFile2 ");
        }
        pdf.append(font.fileObjNumber);
        pdf.append(" 0 R\n");
        pdf.append("/Flags ");
        pdf.append(OpenTypeFont.flagsOf(font.italicAngle));
        pdf.append('\n');
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
        pdf.append("/ItalicAngle ");
        pdf.append(OpenTypeFont.italicAngleOf(font.italicAngle));
        pdf.append('\n');
        pdf.append("/CapHeight ");
        pdf.append(OpenTypeFont.toGlyphSpace(font.capHeight, font.unitsPerEm));
        pdf.append('\n');
        pdf.append("/StemV 79\n");
        if (font.cidSetObjNumber != 0) {
            pdf.append("/CIDSet ");
            pdf.append(font.cidSetObjNumber);
            pdf.append(" 0 R\n");
        }
        pdf.append(">>\n");
        pdf.endObj();

        font.fontDescriptorObjNumber = pdf.getObjNumber();
    }

    // Writes the ToUnicode map of the font: of every glyph that has a
    // character, or only of the glyphs kept, for a subset.
    static void addToUnicodeCMapObject(PDF pdf, Font font, boolean[] kept) throws Exception {
        for (Font f : pdf.fonts) {
            if (f.toUnicodeCMapObjNumber != 0 && f.name.equals(font.name) && f.checksum == font.checksum) {
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
            if (gid > 0 && unicodeOf[gid] == cid && (kept == null || (gid < kept.length && kept[gid]))) {
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

        addCompressedStream(pdf, sb.toString().getBytes(java.nio.charset.StandardCharsets.UTF_8));
        font.toUnicodeCMapObjNumber = pdf.getObjNumber();
    }

    // Writes a stream object of the data, compressed.
    static void addCompressedStream(PDF pdf, byte[] data) throws Exception {
        byte[] compressed = Compressor.deflate(data);
        if (pdf.encryption != null) {
            compressed = AES256.encrypt(compressed, pdf.encryption.getKey());
        }
        pdf.newObj();
        pdf.append("<<\n");
        pdf.append("/Filter /FlateDecode\n");
        pdf.append("/Length ");
        pdf.append(compressed.length);
        pdf.append("\n");
        pdf.append(">>\n");
        pdf.append("stream\n");
        pdf.append(compressed);
        pdf.append("\nendstream\n");
        pdf.endObj();
    }

    // Writes the CID font with the name given, which is the font's own, or
    // that of a subset, whose widths are those of the glyphs it keeps.
    static void addCIDFontDictionaryObject(PDF pdf, Font font, String baseFont, boolean[] kept) throws Exception {
        for (Font f : pdf.fonts) {
            if (f.cidFontDictObjNumber != 0 && f.name.equals(font.name) && f.checksum == font.checksum) {
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
        pdf.append(baseFont);
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

        if (kept == null) {
            pdf.append("/W [0[\n");
            for (int width : font.advanceWidth) {
                pdf.append(Math.round(k * (float) width));
                pdf.append(' ');
            }
            pdf.append("]]\n");
        } else {
            // Each run of kept glyphs: its first glyph and its widths.
            pdf.append("/W [");
            int count = Math.min(kept.length, font.advanceWidth.length);
            for (int gid = 0; gid < count; gid++) {
                if (!kept[gid]) {
                    continue;
                }
                pdf.append('\n');
                pdf.append(gid);
                pdf.append('[');
                for (; gid < count && kept[gid]; gid++) {
                    pdf.append(Math.round(k * (float) font.advanceWidth[gid]));
                    pdf.append(' ');
                }
                pdf.append(']');
            }
            pdf.append("]\n");
        }

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

    // The soft hyphen, the micro sign, the spacing modifier letters, the
    // combining marks, the figure dash, the ohm, kelvin and angstrom signs, the
    // deprecated angle brackets, the CJK and Kangxi radicals, which CJK fonts
    // draw with the glyphs of the ideographs, and the private use characters.
    // The micro sign and the three signs are the letters Unicode makes them,
    // mu, omega, K and A with a ring, which a font often draws them with: such
    // a glyph is copied as the letter, as Greek text needs it.
    private static boolean isSeldomText(int ch) {
        return ch == 0x00AD ||
                ch == 0x00B5 ||
                (ch >= 0x02B0 && ch <= 0x036F) ||
                (ch >= 0x1AB0 && ch <= 0x1AFF) ||
                (ch >= 0x1DC0 && ch <= 0x1DFF) ||
                ch == 0x2012 ||
                (ch >= 0x20D0 && ch <= 0x20FF) ||
                ch == 0x2126 || ch == 0x212A || ch == 0x212B ||
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

    // Returns true if the name can be written as a PDF name as it is:
    // printable ASCII, and none of the characters that end a name or start an
    // escape.
    static boolean isFontName(byte[] name) {
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
}
