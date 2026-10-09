/*
 * FontWriter.cs
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
using System;
using System.Text;
using System.Collections.Generic;

namespace PDFjet.NET {
/// <summary>
/// The objects of an embedded font that every font writes, whatever its
/// outlines: its descriptor, its CID font with the widths of its glyphs, and
/// its ToUnicode map.
/// </summary>
class FontWriter {
    // Writes the font descriptor with the name given, which is the font's own,
    // or that of a subset.
    internal static void AddFontDescriptorObject(PDF pdf, Font font, String fontName) {
        foreach (Font f in pdf.fonts) {
            if (f.fontDescriptorObjNumber != 0 && f.name.Equals(font.name) && f.checksum == font.checksum) {
                font.fontDescriptorObjNumber = f.fontDescriptorObjNumber;
                return;
            }
        }

        pdf.NewObj();
        pdf.Append(Token.BeginDictionary);
        pdf.Append("/Type /FontDescriptor\n");
        pdf.Append("/FontName /");
        pdf.Append(fontName);
        pdf.Append('\n');
        if (font.cff) {
            pdf.Append("/FontFile3 ");
        } else {
            pdf.Append("/FontFile2 ");
        }
        pdf.Append(font.fileObjNumber);
        pdf.Append(" 0 R\n");
        pdf.Append("/Flags ");
        pdf.Append(OpenTypeFont.FlagsOf(font.italicAngle));
        pdf.Append('\n');
        pdf.Append("/FontBBox [");
        pdf.Append(OpenTypeFont.ToGlyphSpace(font.bBoxLLx, font.unitsPerEm));
        pdf.Append(' ');
        pdf.Append(OpenTypeFont.ToGlyphSpace(font.bBoxLLy, font.unitsPerEm));
        pdf.Append(' ');
        pdf.Append(OpenTypeFont.ToGlyphSpace(font.bBoxURx, font.unitsPerEm));
        pdf.Append(' ');
        pdf.Append(OpenTypeFont.ToGlyphSpace(font.bBoxURy, font.unitsPerEm));
        pdf.Append("]\n");
        pdf.Append("/Ascent ");
        pdf.Append(OpenTypeFont.ToGlyphSpace(font.fontAscent, font.unitsPerEm));
        pdf.Append('\n');
        pdf.Append("/Descent ");
        pdf.Append(OpenTypeFont.ToGlyphSpace(font.fontDescent, font.unitsPerEm));
        pdf.Append('\n');
        pdf.Append("/ItalicAngle ");
        pdf.Append(OpenTypeFont.ItalicAngleOf(font.italicAngle));
        pdf.Append('\n');
        pdf.Append("/CapHeight ");
        pdf.Append(OpenTypeFont.ToGlyphSpace(font.capHeight, font.unitsPerEm));
        pdf.Append('\n');
        pdf.Append("/StemV 79\n");
        if (font.cidSetObjNumber != 0) {
            pdf.Append("/CIDSet ");
            pdf.Append(font.cidSetObjNumber);
            pdf.Append(" 0 R\n");
        }
        pdf.Append(Token.EndDictionary);
        pdf.EndObj();

        font.fontDescriptorObjNumber = pdf.GetObjNumber();
    }

    // Writes the ToUnicode map of the font: of every glyph that has a
    // character, or only of the glyphs kept, for a subset.
    internal static void AddToUnicodeCMapObject(PDF pdf, Font font, bool[] kept) {
        foreach (Font f in pdf.fonts) {
            if (f.toUnicodeCMapObjNumber != 0 && f.name.Equals(font.name) && f.checksum == font.checksum) {
                font.toUnicodeCMapObjNumber = f.toUnicodeCMapObjNumber;
                return;
            }
        }

        StringBuilder sb = new StringBuilder();

        sb.Append("/CIDInit /ProcSet findresource begin\n");
        sb.Append("12 dict begin\n");
        sb.Append("begincmap\n");
        sb.Append("/CIDSystemInfo <</Registry (Adobe) /Ordering (Identity) /Supplement 0>> def\n");
        sb.Append("/CMapName /Adobe-Identity def\n");
        sb.Append("/CMapType 2 def\n");

        sb.Append("1 begincodespacerange\n");
        sb.Append("<0000> <FFFF>\n");
        sb.Append("endcodespacerange\n");

        List<String> list = new List<String>();
        // A character the font does not contain is drawn with the .notdef
        // glyph. PDF/UA requires every glyph to map to Unicode, so map it to
        // the replacement character.
        list.Add("<0000> <FFFD>\n");
        StringBuilder buf = new StringBuilder();
        int[] unicodeOf = UnicodeOfGlyphs(font.unicodeToGID);
        for (int cid = 0; cid <= 0xffff; cid++) {
            int gid = font.unicodeToGID[cid];
            if (gid > 0 && unicodeOf[gid] == cid && (kept == null || (gid < kept.Length && kept[gid]))) {
                buf.Append('<');
                buf.Append(ToHexString(gid));
                buf.Append("> <");
                // A presentation form that the Bidi class puts in maps to the letters it stands for.
                String letters = Bidi.LettersOf(cid);
                if (letters == null) {
                    buf.Append(ToHexString(cid));
                } else {
                    foreach (char ch in letters) {
                        buf.Append(ToHexString(ch));
                    }
                }
                buf.Append(">\n");
                list.Add(buf.ToString());
                buf.Length = 0;
                if (list.Count == 100) {
                    WriteListToBuffer(sb, list);
                }
            }
        }
        if (list.Count > 0) {
            WriteListToBuffer(sb, list);
        }

        sb.Append("endcmap\n");
        sb.Append("CMapName currentdict /CMap defineresource pop\n");
        sb.Append("end\nend");

        AddCompressedStream(pdf, Encoding.UTF8.GetBytes(sb.ToString()));
        font.toUnicodeCMapObjNumber = pdf.GetObjNumber();
    }

    // Writes a stream object of the data, compressed.
    internal static void AddCompressedStream(PDF pdf, byte[] data) {
        byte[] compressed = Compressor.Deflate(data);
        if (pdf.encryption != null) {
            compressed = AES256.Encrypt(compressed, pdf.encryption.GetKey());
        }
        pdf.NewObj();
        pdf.Append("<<\n");
        pdf.Append("/Filter /FlateDecode\n");
        pdf.Append("/Length ");
        pdf.Append(compressed.Length);
        pdf.Append("\n");
        pdf.Append(">>\n");
        pdf.Append("stream\n");
        pdf.Append(compressed);
        pdf.Append("\nendstream\n");
        pdf.EndObj();
    }

    // Writes the CID font with the name given, which is the font's own, or
    // that of a subset, whose widths are those of the glyphs it keeps.
    internal static void AddCIDFontDictionaryObject(PDF pdf, Font font, String baseFont, bool[] kept) {
        foreach (Font f in pdf.fonts) {
            if (f.cidFontDictObjNumber != 0 && f.name.Equals(font.name) && f.checksum == font.checksum) {
                font.cidFontDictObjNumber = f.cidFontDictObjNumber;
                return;
            }
        }

        pdf.NewObj();
        pdf.Append("<<\n");
        pdf.Append("/Type /Font\n");
        if (font.cff) {
            pdf.Append("/Subtype /CIDFontType0\n");
        } else {
            pdf.Append("/Subtype /CIDFontType2\n");
        }
        pdf.Append("/BaseFont /");
        pdf.Append(baseFont);
        pdf.Append('\n');

        byte[] registry = Encoding.UTF8.GetBytes("Adobe");
        byte[] ordering = Encoding.UTF8.GetBytes("Identity");
        if (pdf.encryption != null) {
            registry = AES256.Encrypt(registry, pdf.encryption.GetKey());
            ordering = AES256.Encrypt(ordering, pdf.encryption.GetKey());
        }
        pdf.Append("/CIDSystemInfo <</Registry <");
        pdf.Append(Util.ToHexString(registry));
        pdf.Append("> /Ordering <");
        pdf.Append(Util.ToHexString(ordering));
        pdf.Append("> /Supplement 0>>\n");

        pdf.Append("/FontDescriptor ");
        pdf.Append(font.fontDescriptorObjNumber);
        pdf.Append(" 0 R\n");

        float k = 1000.0f / Convert.ToSingle(font.unitsPerEm);
        // The width of the glyphs past the /W array: those past the advance
        // widths, which have the width of the last one.
        pdf.Append("/DW ");
        pdf.Append((int) Math.Round(k * Convert.ToSingle(font.advanceWidth[font.advanceWidth.Length - 1]), MidpointRounding.AwayFromZero));
        pdf.Append('\n');

        if (kept == null) {
            pdf.Append("/W [0[\n");
            foreach (int width in font.advanceWidth) {
                pdf.Append((int) Math.Round(k * Convert.ToSingle(width), MidpointRounding.AwayFromZero));
                pdf.Append(' ');
            }
            pdf.Append("]]\n");
        } else {
            // Each run of kept glyphs: its first glyph and its widths.
            pdf.Append("/W [");
            int count = Math.Min(kept.Length, font.advanceWidth.Length);
            for (int gid = 0; gid < count; gid++) {
                if (!kept[gid]) {
                    continue;
                }
                pdf.Append('\n');
                pdf.Append(gid);
                pdf.Append('[');
                for (; gid < count && kept[gid]; gid++) {
                    pdf.Append((int) Math.Round(k * Convert.ToSingle(font.advanceWidth[gid]), MidpointRounding.AwayFromZero));
                    pdf.Append(' ');
                }
                pdf.Append(']');
            }
            pdf.Append("]\n");
        }

        pdf.Append("/CIDToGIDMap /Identity\n");
        pdf.Append(">>\n");
        pdf.EndObj();

        font.cidFontDictObjNumber = pdf.GetObjNumber();
    }

    /// <summary>
    /// Returns the character that each glyph maps to in a ToUnicode CMap. A
    /// glyph can stand for several characters, like the space and the no-break
    /// space, but viewers read a CMap with more than one entry for a glyph
    /// differently. So a glyph maps to the first character that uses it, unless
    /// that is one text seldom has and another character uses the glyph too,
    /// like the modifier letter apostrophe and the right single quotation mark.
    /// </summary>
    internal static int[] UnicodeOfGlyphs(int[] unicodeToGID) {
        int[] unicode = new int[0x10000];
        for (int i = 0; i < unicode.Length; i++) {
            unicode[i] = -1;
        }
        for (int cid = 0; cid <= 0xffff; cid++) {
            int gid = unicodeToGID[cid];
            if (gid > 0 && (unicode[gid] == -1 ||
                    (IsSeldomText(unicode[gid]) && !IsSeldomText(cid)))) {
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
    private static bool IsSeldomText(int ch) {
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

    internal static String ToHexString(int code) {
        String str = Convert.ToString(code, 16);
        if (str.Length == 1) {
            return "000" + str;
        } else if (str.Length == 2) {
            return "00" + str;
        } else if (str.Length == 3) {
            return "0" + str;
        }
        return str;
    }

    internal static void WriteListToBuffer(StringBuilder sb, List<String> list) {
        sb.Append(list.Count);
        sb.Append(" beginbfchar\n");
        foreach (String str in list) {
            sb.Append(str);
        }
        sb.Append("endbfchar\n");
        list.Clear();
    }

    // Returns true if the name can be written as a PDF name as it is:
    // printable ASCII, and none of the characters that end a name or start an
    // escape.
    internal static bool IsFontName(byte[] name) {
        if (name.Length == 0) {
            return false;
        }
        foreach (byte b in name) {
            if (b < 0x21 || b > 0x7E || "()<>[]{}/%#".IndexOf((char) b) != -1) {
                return false;
            }
        }
        return true;
    }
}   // End of FontWriter.cs
}   // End of namespace PDFjet.NET
