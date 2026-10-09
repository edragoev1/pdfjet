/*
 * OpenTypeFont.cs
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
using System;
using System.Globalization;
using System.IO;
using System.Text;
using System.Collections.Generic;

namespace PDFjet.NET {
class OpenTypeFont {
    internal static void Register(PDF pdf, Font font, Stream inputStream) {
        OTF otf = new OTF(inputStream);
        SetData(font, otf);

        if (!otf.cff) {
            // A TrueType font is written at Complete(), a subset of the glyphs
            // drawn; its pages refer to the number reserved for it.
            Subset.Share(pdf, font, otf.buf);
            font.objNumber = pdf.ReserveObjNumber();
            pdf.fonts.Add(font);
            return;
        }
        RegisterCFF(pdf, font, otf);
    }

    // Adds the font to the objects of an existing PDF, with its font program
    // whole.
    internal static void Register(List<PDFobj> objects, Font font, Stream inputStream) {
        OTF otf = new OTF(inputStream);
        SetData(font, otf);
        font.uncompressedSize = otf.buf.Length;
        FontObjects.Register(objects, font, otf.Compress());
    }

    // Gives the font the name, the metrics and the character map of the
    // OpenType or TrueType font.
    private static void SetData(Font font, OTF otf) {
        font.name = otf.fontName;
        font.firstChar = otf.firstChar;
        font.lastChar = otf.lastChar;

        font.unitsPerEm = otf.unitsPerEm;
        font.bBoxLLx = otf.bBoxLLx;
        font.bBoxLLy = otf.bBoxLLy;
        font.bBoxURx = otf.bBoxURx;
        font.bBoxURy = otf.bBoxURy;
        font.advanceWidth = otf.advanceWidth;
        font.unicodeToGID = otf.unicodeToGID;
        font.markToMarkOffsets = otf.markToMarkOffsets;
        font.markAnchors = otf.markAnchors;
        font.baseAnchors = otf.baseAnchors;
        font.fontAscent = otf.ascent;
        font.fontDescent = otf.descent;
        font.fontLineGap = otf.lineGap;
        font.italicAngle = (int) otf.italicAngle;
        font.fontUnderlinePosition = otf.underlinePosition;
        font.fontUnderlineThickness = otf.underlineThickness;
        font.info = otf.fontInfo;
        font.capHeight = otf.capHeight;
        font.cff = otf.cff;
        font.checksum = Font.ChecksumOf(font);
        font.SetSize(font.size);
    }

    // Writes a font with CFF outlines, which is embedded whole, as it is added.
    private static void RegisterCFF(PDF pdf, Font font, OTF otf) {
        EmbedFontFile(pdf, font, otf);
        FontWriter.AddFontDescriptorObject(pdf, font, font.name);
        FontWriter.AddCIDFontDictionaryObject(pdf, font, font.name, null);
        FontWriter.AddToUnicodeCMapObject(pdf, font, null);

        // Type0 Font Dictionary
        pdf.NewObj();
        pdf.Append("<<\n");
        pdf.Append("/Type /Font\n");
        pdf.Append("/Subtype /Type0\n");
        pdf.Append("/BaseFont /");
        pdf.Append(otf.fontName);
        pdf.Append('\n');
        pdf.Append("/Encoding /Identity-H\n");
        pdf.Append("/DescendantFonts [");
        pdf.Append(font.cidFontDictObjNumber);
        pdf.Append(" 0 R]\n");
        pdf.Append("/ToUnicode ");
        pdf.Append(font.toUnicodeCMapObjNumber);
        pdf.Append(" 0 R\n");
        pdf.Append(">>\n");
        pdf.EndObj();

        font.objNumber = pdf.GetObjNumber();
        pdf.fonts.Add(font);
    }

    private static void EmbedFontFile(PDF pdf, Font font, OTF otf) {
        // Check if the font file is already embedded
        foreach (Font f in pdf.fonts) {
            if (f.fileObjNumber != 0 && f.name.Equals(otf.fontName) && f.checksum == font.checksum) {
                font.fileObjNumber = f.fileObjNumber;
                return;
            }
        }

        int metadataObjNumber = pdf.AddMetadataObject(otf.fontInfo, true);

        pdf.NewObj();
        pdf.Append("<<\n");
        if (otf.cff) {
            pdf.Append("/Subtype /CIDFontType0C\n");
        }
        pdf.Append("/Filter /FlateDecode\n");

        if (!otf.cff) {
            pdf.Append("/Length1 ");
            pdf.Append(otf.buf.Length);
            pdf.Append('\n');
        }

        if (metadataObjNumber != -1) {
            pdf.Append("/Metadata ");
            pdf.Append(metadataObjNumber);
            pdf.Append(" 0 R\n");
        }

        byte[] buf = otf.Compress();
        if (pdf.encryption != null) {
            buf = AES256.Encrypt(buf, pdf.encryption.GetKey());
        }

        pdf.Append("/Length ");
        pdf.Append(buf.Length);
        pdf.Append("\n");

        pdf.Append(">>\n");
        pdf.Append("stream\n");
        pdf.Append(buf);
        pdf.Append("\nendstream\n");
        pdf.EndObj();

        font.fileObjNumber = pdf.GetObjNumber();
    }

    /// <summary>
    /// Converts a value in font units to the glyph space units of the font
    /// descriptor, 1/1000 em, rounded to the nearest integer with halves away
    /// from zero. The integer arithmetic gives the same value in every port.
    /// </summary>
    internal static int ToGlyphSpace(int value, int unitsPerEm) {
        int rounded = (2000 * Math.Abs(value) + unitsPerEm) / (2 * unitsPerEm);
        return (value < 0) ? -rounded : rounded;
    }

    /// <summary>
    /// Returns the flags of the font descriptor: Nonsymbolic, 32, and Italic,
    /// 64, for a font whose post table gives it an italic angle.
    /// </summary>
    internal static int FlagsOf(int italicAngle) {
        return (italicAngle != 0) ? 32 | 64 : 32;
    }

    /// <summary>
    /// Returns the italic angle of the font descriptor: the 16.16 fixed number
    /// of the post table in degrees, to two decimals with halves away from
    /// zero and no trailing zeros. The integer arithmetic gives the same text
    /// in every port.
    /// </summary>
    internal static String ItalicAngleOf(int angle) {
        long rounded = (200L * Math.Abs((long) angle) + 65536L) / (2L * 65536L);
        String text = (rounded / 100).ToString(CultureInfo.InvariantCulture);
        if (rounded % 100 != 0) {
            // The two decimals, as 100 to 199 without the 1, and no trailing zero.
            String decimals = (100 + rounded % 100).ToString(CultureInfo.InvariantCulture).Substring(1);
            text += "." + (decimals.EndsWith("0", StringComparison.Ordinal) ? decimals.Substring(0, 1) : decimals);
        }
        return (angle < 0 && rounded != 0) ? "-" + text : text;
    }

    private static String ToHexString(int code) {
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

    private static void WriteListToBuffer(List<String> list, StringBuilder sb) {
        sb.Append(list.Count);
        sb.Append(" beginbfchar\n");
        foreach (String str in list) {
            sb.Append(str);
        }
        sb.Append("endbfchar\n");
        list.Clear();
    }
}   // End of OpenTypeFont.cs
}   // End of namespace PDFjet.NET
