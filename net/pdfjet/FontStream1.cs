/**
 * FontStream1.cs
 *
 * Copyright (c) 2025 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
using System;
using System.IO;
using System.Text;
using System.Collections.Generic;

namespace PDFjet.NET {
class FontStream1 {
    internal static void Register(
            PDF pdf,
            Font font,
            Stream inputStream) {
        GetFontData(font, inputStream);
        EmbedFontFile(pdf, font, inputStream);
        AddFontDescriptorObject(pdf, font);
        AddCIDFontDictionaryObject(pdf, font);
        AddToUnicodeCMapObject(pdf, font);

        // Type0 Font Dictionary
        pdf.NewObj();
        pdf.Append(Token.BeginDictionary);
        pdf.Append("/Type /Font\n");
        pdf.Append("/Subtype /Type0\n");
        pdf.Append("/BaseFont /");
        pdf.Append(Encoding.UTF8.GetBytes(font.name));
        pdf.Append(Token.Newline);
        pdf.Append("/Encoding /Identity-H\n");
        pdf.Append("/DescendantFonts [");
        pdf.Append(font.cidFontDictObjNumber);
        pdf.Append(" 0 R]\n");
        pdf.Append("/ToUnicode ");
        pdf.Append(font.toUnicodeCMapObjNumber);
        pdf.Append(" 0 R\n");
        pdf.Append(Token.EndDictionary);
        pdf.EndObj();

        font.objNumber = pdf.GetObjNumber();
        pdf.fonts.Add(font);
    }

    private static void EmbedFontFile(PDF pdf, Font font, Stream stream) {
        // Check if the font file is already embedded
        foreach (Font f in pdf.fonts) {
            if (f.fileObjNumber != 0 && f.name.Equals(font.name)) {
                font.fileObjNumber = f.fileObjNumber;
                return;
            }
        }

        int metadataObjNumber = pdf.AddMetadataObject(font.info, true);
        pdf.NewObj();
        pdf.Append(Token.BeginDictionary);

        pdf.Append("/Metadata ");
        pdf.Append(metadataObjNumber);
        pdf.Append(" 0 R\n");

        if (font.cff) {
            pdf.Append("/Subtype /CIDFontType0C\n");
        } else {
            pdf.Append("/Length1 ");
            pdf.Append(font.uncompressedSize);
            pdf.Append(Token.Newline);
        }
        pdf.Append("/Filter /FlateDecode\n");

        byte[] compressed = null;
        byte[] encrypted = null;
        using (var ms = new MemoryStream()) {
            stream.CopyTo(ms);
            compressed = ms.ToArray();
        }
        if (pdf.encryption != null) {
            encrypted = AES256.Encrypt(compressed, pdf.encryption.GetKey());
        }
        stream.Dispose();

        pdf.Append("/Length ");
        if (pdf.encryption != null) {
            pdf.Append(encrypted.Length);
        } else {
            pdf.Append(font.compressedSize);
        }
        pdf.Append(Token.Newline);
        pdf.Append(Token.EndDictionary);
        pdf.Append(Token.Stream);
        if (pdf.encryption != null) {
            pdf.Append(encrypted);
        } else {
            pdf.Append(compressed);
        }
        pdf.Append(Token.EndStream);
        pdf.EndObj();

        font.fileObjNumber = pdf.GetObjNumber();
    }

    private static void AddFontDescriptorObject(PDF pdf, Font font) {
        foreach (Font f in pdf.fonts) {
            if (f.fontDescriptorObjNumber != 0 && f.name.Equals(font.name)) {
                font.fontDescriptorObjNumber = f.fontDescriptorObjNumber;
                return;
            }
        }

        pdf.NewObj();
        pdf.Append(Token.BeginDictionary);
        pdf.Append("/Type /FontDescriptor\n");
        pdf.Append("/FontName /");
        pdf.Append(font.name);
        pdf.Append('\n');
        if (font.cff) {
            pdf.Append("/FontFile3 ");
        } else {
            pdf.Append("/FontFile2 ");
        }
        pdf.Append(font.fileObjNumber);
        pdf.Append(" 0 R\n");
        pdf.Append("/Flags 32\n");
        pdf.Append("/FontBBox [");
        pdf.Append(font.bBoxLLx);
        pdf.Append(' ');
        pdf.Append(font.bBoxLLy);
        pdf.Append(' ');
        pdf.Append(font.bBoxURx);
        pdf.Append(' ');
        pdf.Append(font.bBoxURy);
        pdf.Append("]\n");
        pdf.Append("/Ascent ");
        pdf.Append(font.fontAscent);
        pdf.Append('\n');
        pdf.Append("/Descent ");
        pdf.Append(font.fontDescent);
        pdf.Append('\n');
        pdf.Append("/ItalicAngle 0\n");
        pdf.Append("/CapHeight ");
        pdf.Append(font.capHeight);
        pdf.Append('\n');
        pdf.Append("/StemV 79\n");
        pdf.Append(Token.EndDictionary);
        pdf.EndObj();

        font.fontDescriptorObjNumber = pdf.GetObjNumber();
    }

    private static void AddToUnicodeCMapObject(PDF pdf, Font font) {
        foreach (Font f in pdf.fonts) {
            if (f.toUnicodeCMapObjNumber != 0 && f.name.Equals(font.name)) {
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
        StringBuilder buf = new StringBuilder();
        for (int cid = 0; cid <= 0xffff; cid++) {
            int gid = font.unicodeToGID[cid];
            if (gid > 0) {
                buf.Append('<');
                buf.Append(ToHexString(gid));
                buf.Append("> <");
                buf.Append(ToHexString(cid));
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

        byte[] buf2 = Encoding.UTF8.GetBytes(sb.ToString());
        if (pdf.encryption != null) {
            buf2 = AES256.Encrypt(buf2, pdf.encryption.GetKey());
        }

        pdf.NewObj();
        pdf.Append("<<\n");
        pdf.Append("/Length ");
        pdf.Append(buf2.Length);
        pdf.Append("\n");
        pdf.Append(">>\n");
        pdf.Append("stream\n");
        pdf.Append(buf2);
        pdf.Append("\nendstream\n");
        pdf.EndObj();

        font.toUnicodeCMapObjNumber = pdf.GetObjNumber();
    }

    private static void AddCIDFontDictionaryObject(PDF pdf, Font font) {
        foreach (Font f in pdf.fonts) {
            if (f.cidFontDictObjNumber != 0 && f.name.Equals(font.name)) {
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
        pdf.Append(font.name);
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
        pdf.Append("/DW ");
        pdf.Append((int) Math.Round(k * Convert.ToSingle(font.advanceWidth[0])));
        pdf.Append('\n');

        pdf.Append("/W [0[\n");
        for (int i = 0; i < font.advanceWidth.Length; i++) {
            pdf.Append((int) Math.Round(k * Convert.ToSingle(font.advanceWidth[i])));
            pdf.Append(' ');
        }
        pdf.Append("]]\n");

        pdf.Append("/CIDToGIDMap /Identity\n");
        pdf.Append(">>\n");
        pdf.EndObj();

        font.cidFontDictObjNumber = pdf.GetObjNumber();
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

    private static int GetInt16(Stream stream) {
        return stream.ReadByte() << 8 | stream.ReadByte();
    }

    private static int GetInt24(Stream stream) {
        return stream.ReadByte() << 16 |
                stream.ReadByte() << 8 | stream.ReadByte();
    }

    private static int GetInt32(Stream stream) {
        return stream.ReadByte() << 24 | stream.ReadByte() << 16 |
                stream.ReadByte() << 8 | stream.ReadByte();
    }

    internal static void GetFontData(Font font, Stream inputStream) {
        int len = inputStream.ReadByte();
        byte[] fontName = new byte[len];
        ReadFully(inputStream, fontName);
        font.name = System.Text.Encoding.UTF8.GetString(fontName, 0, len);

        len = GetInt24(inputStream);
        byte[] fontInfo = new byte[len];
        ReadFully(inputStream, fontInfo);
        font.info = System.Text.Encoding.UTF8.GetString(fontInfo, 0, len);

        byte[] buf = new byte[GetInt32(inputStream)];
        ReadFully(inputStream, buf);
        MemoryStream stream = new MemoryStream(Decompressor.inflate(buf));

        font.unitsPerEm = GetInt32(stream);
        font.bBoxLLx = GetInt32(stream);
        font.bBoxLLy = GetInt32(stream);
        font.bBoxURx = GetInt32(stream);
        font.bBoxURy = GetInt32(stream);
        font.fontAscent = GetInt32(stream);
        font.fontDescent = GetInt32(stream);
        font.firstChar = GetInt32(stream);
        font.lastChar = GetInt32(stream);
        font.capHeight = GetInt32(stream);
        font.fontUnderlinePosition = GetInt32(stream);
        font.fontUnderlineThickness = GetInt32(stream);

        len = GetInt32(stream);
        font.advanceWidth = new int[len];
        for (int i = 0; i < len; i++) {
            font.advanceWidth[i] = GetInt16(stream);
        }

        len = GetInt32(stream);
        font.unicodeToGID = new int[len];
        for (int i = 0; i < len; i++) {
            font.unicodeToGID[i] = GetInt16(stream);
        }

        font.cff = (inputStream.ReadByte() == 'Y') ? true : false;
        font.uncompressedSize = GetInt32(inputStream);
        font.compressedSize = GetInt32(inputStream);
    }

    internal static void ReadFully(Stream stream, byte[] buffer) {
        int totalBytesRead = 0;
        while (totalBytesRead < buffer.Length) {
            // Read the remaining bytes into the buffer
            int bytesRead = stream.Read(buffer, totalBytesRead, buffer.Length - totalBytesRead);
            if (bytesRead == 0) {
                // End of stream reached
                break;
            }
            totalBytesRead += bytesRead;
        }
    }
}   // End of FontStream1.cs
}   // End of namespace PDFjet.NET
