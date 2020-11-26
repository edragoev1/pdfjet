/**
 *  FontStream1.cs
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
        pdf.Newobj();
        pdf.Append("<<\n");
        pdf.Append("/Type /Font\n");
        pdf.Append("/Subtype /Type0\n");
        pdf.Append("/BaseFont /");
        pdf.Append(font.name);
        pdf.Append('\n');
        pdf.Append("/Encoding /Identity-H\n");
        pdf.Append("/DescendantFonts [");
        pdf.Append(font.cidFontDictObjNumber);
        pdf.Append(" 0 R]\n");
        pdf.Append("/ToUnicode ");
        pdf.Append(font.toUnicodeCMapObjNumber);
        pdf.Append(" 0 R\n");
        pdf.Append(">>\n");
        pdf.Endobj();

        font.objNumber = pdf.GetObjNumber();
        pdf.fonts.Add(font);
    }


    private static void EmbedFontFile(PDF pdf, Font font, Stream inputStream) {
        // Check if the font file is already embedded
        foreach (Font f in pdf.fonts) {
            if (f.fileObjNumber != 0 && f.name.Equals(font.name)) {
                font.fileObjNumber = f.fileObjNumber;
                return;
            }
        }

        int metadataObjNumber = pdf.AddMetadataObject(font.info, true);

        pdf.Newobj();
        pdf.Append("<<\n");

        pdf.Append("/Metadata ");
        pdf.Append(metadataObjNumber);
        pdf.Append(" 0 R\n");

        if (font.cff) {
            pdf.Append("/Subtype /CIDFontType0C\n");
        }
        pdf.Append("/Filter /FlateDecode\n");
        pdf.Append("/Length ");
        pdf.Append(font.compressedSize);
        pdf.Append("\n");

        if (!font.cff) {
            pdf.Append("/Length1 ");
            pdf.Append(font.uncompressedSize);
            pdf.Append('\n');
        }

        pdf.Append(">>\n");
        pdf.Append("stream\n");
        byte[] buf = new byte[4096];
        int len;
        while ((len = inputStream.Read(buf, 0, buf.Length)) > 0) {
            pdf.Append(buf, 0, len);
        }
        inputStream.Dispose();
        pdf.Append("\nendstream\n");
        pdf.Endobj();

        font.fileObjNumber = pdf.GetObjNumber();
    }


    private static void AddFontDescriptorObject(PDF pdf, Font font) {
        foreach (Font f in pdf.fonts) {
            if (f.fontDescriptorObjNumber != 0 && f.name.Equals(font.name)) {
                font.fontDescriptorObjNumber = f.fontDescriptorObjNumber;
                return;
            }
        }

        pdf.Newobj();
        pdf.Append("<<\n");
        pdf.Append("/Type /FontDescriptor\n");
        pdf.Append("/FontName /");
        pdf.Append(font.name);
        pdf.Append('\n');
        if (font.cff) {
            pdf.Append("/FontFile3 ");
        }
        else {
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
        pdf.Append(">>\n");
        pdf.Endobj();

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

        pdf.Newobj();
        pdf.Append("<<\n");
        pdf.Append("/Length ");
        pdf.Append(sb.Length);
        pdf.Append("\n");
        pdf.Append(">>\n");
        pdf.Append("stream\n");
        pdf.Append(sb.ToString());
        pdf.Append("\nendstream\n");
        pdf.Endobj();

        font.toUnicodeCMapObjNumber = pdf.GetObjNumber();
    }


    private static void AddCIDFontDictionaryObject(PDF pdf, Font font) {
        foreach (Font f in pdf.fonts) {
            if (f.cidFontDictObjNumber != 0 && f.name.Equals(font.name)) {
                font.cidFontDictObjNumber = f.cidFontDictObjNumber;
                return;
            }
        }

        pdf.Newobj();
        pdf.Append("<<\n");
        pdf.Append("/Type /Font\n");
        if (font.cff) {
            pdf.Append("/Subtype /CIDFontType0\n");
        }
        else {
            pdf.Append("/Subtype /CIDFontType2\n");
        }
        pdf.Append("/BaseFont /");
        pdf.Append(font.name);
        pdf.Append('\n');
        pdf.Append("/CIDSystemInfo <</Registry (Adobe) /Ordering (Identity) /Supplement 0>>\n");
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
        pdf.Endobj();

        font.cidFontDictObjNumber = pdf.GetObjNumber();
    }


    internal static String ToHexString(int code) {
        String str = Convert.ToString(code, 16);
        if (str.Length == 1) {
            return "000" + str;
        }
        else if (str.Length == 2) {
            return "00" + str;
        }
        else if (str.Length == 3) {
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
        inputStream.Read(fontName, 0, len);
        font.name = System.Text.Encoding.UTF8.GetString(fontName, 0, len);

        len = GetInt24(inputStream);
        byte[] fontInfo = new byte[len];
        inputStream.Read(fontInfo, 0, len);
        font.info = System.Text.Encoding.UTF8.GetString(fontInfo, 0, len);

        byte[] buf = new byte[GetInt32(inputStream)];
        inputStream.Read(buf, 0, buf.Length);
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
        font.glyphWidth = new int[len];
        for (int i = 0; i < len; i++) {
            font.glyphWidth[i] = GetInt16(stream);
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

}   // End of FontStream1.cs
}   // End of namespace PDFjet.NET
