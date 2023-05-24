/**
 *  OpenTypeFont.cs
 *
Copyright 2023 Innovatics Inc.

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
class OpenTypeFont {
    internal static void Register(PDF pdf, Font font, Stream inputStream) {
        OTF otf = new OTF(inputStream);

        font.name = otf.fontName;
        font.firstChar = otf.firstChar;
        font.lastChar = otf.lastChar;
        font.unicodeToGID = otf.unicodeToGID;
        font.unitsPerEm = otf.unitsPerEm;
        font.bBoxLLx = otf.bBoxLLx;
        font.bBoxLLy = otf.bBoxLLy;
        font.bBoxURx = otf.bBoxURx;
        font.bBoxURy = otf.bBoxURy;
        font.advanceWidth = otf.advanceWidth;
        font.glyphWidth = otf.glyphWidth;
        font.fontAscent = otf.ascent;
        font.fontDescent = otf.descent;
        font.fontUnderlinePosition = otf.underlinePosition;
        font.fontUnderlineThickness = otf.underlineThickness;
        font.SetSize(font.size);

        EmbedFontFile(pdf, font, otf);
        AddFontDescriptorObject(pdf, font, otf);
        AddCIDFontDictionaryObject(pdf, font, otf);
        AddToUnicodeCMapObject(pdf, font, otf);

        // Type0 Font Dictionary
        pdf.Newobj();
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
        pdf.Endobj();

        font.objNumber = pdf.GetObjNumber();
        pdf.fonts.Add(font);
    }

    private static void EmbedFontFile(PDF pdf, Font font, OTF otf) {
        // Check if the font file is already embedded
        foreach (Font f in pdf.fonts) {
            if (f.fileObjNumber != 0 && f.name.Equals(otf.fontName)) {
                font.fileObjNumber = f.fileObjNumber;
                return;
            }
        }

        int metadataObjNumber = pdf.AddMetadataObject(otf.fontInfo, true);

        pdf.Newobj();
        pdf.Append("<<\n");
        if (otf.cff) {
            pdf.Append("/Subtype /CIDFontType0C\n");
        }
        pdf.Append("/Filter /FlateDecode\n");

        pdf.Append("/Length ");
        pdf.Append(otf.baos.Length);
        pdf.Append("\n");

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

        pdf.Append(">>\n");
        pdf.Append("stream\n");
        pdf.Append(otf.baos);
        pdf.Append("\nendstream\n");
        pdf.Endobj();

        font.fileObjNumber = pdf.GetObjNumber();
    }

    private static void AddFontDescriptorObject(PDF pdf, Font font, OTF otf) {
        foreach (Font f in pdf.fonts) {
            if (f.fontDescriptorObjNumber != 0 && f.name.Equals(otf.fontName)) {
                font.fontDescriptorObjNumber = f.fontDescriptorObjNumber;
                return;
            }
        }

        pdf.Newobj();
        pdf.Append("<<\n");
        pdf.Append("/Type /FontDescriptor\n");
        pdf.Append("/FontName /");
        pdf.Append(otf.fontName);
        pdf.Append('\n');
        if (otf.cff) {
            pdf.Append("/FontFile3 ");
        } else {
            pdf.Append("/FontFile2 ");
        }
        pdf.Append(font.fileObjNumber);
        pdf.Append(" 0 R\n");
        pdf.Append("/Flags 32\n");
        pdf.Append("/FontBBox [");
        pdf.Append(otf.bBoxLLx);
        pdf.Append(' ');
        pdf.Append(otf.bBoxLLy);
        pdf.Append(' ');
        pdf.Append(otf.bBoxURx);
        pdf.Append(' ');
        pdf.Append(otf.bBoxURy);
        pdf.Append("]\n");
        pdf.Append("/Ascent ");
        pdf.Append(otf.ascent);
        pdf.Append('\n');
        pdf.Append("/Descent ");
        pdf.Append(otf.descent);
        pdf.Append('\n');
        pdf.Append("/ItalicAngle 0\n");
        pdf.Append("/CapHeight ");
        pdf.Append(otf.capHeight);
        pdf.Append('\n');
        pdf.Append("/StemV 79\n");
        pdf.Append(">>\n");
        pdf.Endobj();

        font.fontDescriptorObjNumber = pdf.GetObjNumber();
    }

    private static void AddToUnicodeCMapObject(
            PDF pdf,
            Font font,
            OTF otf) {
        foreach (Font f in pdf.fonts) {
            if (f.toUnicodeCMapObjNumber != 0 && f.name.Equals(otf.fontName)) {
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
            int gid = otf.unicodeToGID[cid];
            if (gid > 0) {
                buf.Append('<');
                buf.Append(ToHexString(gid));
                buf.Append("> <");
                buf.Append(ToHexString(cid));
                buf.Append(">\n");
                list.Add(buf.ToString());
                buf.Length = 0;
                if (list.Count == 100) {
                    WriteListToBuffer(list, sb);
                }
            }
        }
        if (list.Count > 0) {
            WriteListToBuffer(list, sb);
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

    private static void AddCIDFontDictionaryObject(
            PDF pdf,
            Font font,
            OTF otf) {
        foreach (Font f in pdf.fonts) {
            if (f.cidFontDictObjNumber != 0 && f.name.Equals(otf.fontName)) {
                font.cidFontDictObjNumber = f.cidFontDictObjNumber;
                return;
            }
        }

        pdf.Newobj();
        pdf.Append("<<\n");
        pdf.Append("/Type /Font\n");
        if (otf.cff) {
            pdf.Append("/Subtype /CIDFontType0\n");
        } else {
            pdf.Append("/Subtype /CIDFontType2\n");
        }
        pdf.Append("/BaseFont /");
        pdf.Append(otf.fontName);
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
