/*
 * FontObjects.cs
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
using System;
using System.Globalization;
using System.Text;
using System.Collections.Generic;

namespace PDFjet.NET {
/// <summary>
/// The objects of a font added to an existing PDF. They are numbered when the
/// font is added, so that pages can refer to it, and filled in when the objects
/// are added to the PDF, after the pages are drawn: the font program a subset
/// of the glyphs drawn, as a font of a new PDF is at Complete().
/// </summary>
class FontObjects {
    int metadata;   // The number of the font's metadata object
    PDFobj file;
    PDFobj descriptor;
    PDFobj cidFont;
    PDFobj toUnicode;
    PDFobj type0;

    // Numbers the objects of the font, added to the objects of an existing
    // PDF; the Type0 font, the one pages refer to, is the last.
    internal static void Register(List<PDFobj> objects, Font font) {
        FontObjects objs = new FontObjects();
        objs.metadata = AddMetadataObject(objects, font);
        objs.file = AppendEmptyObject(objects);
        objs.descriptor = AppendEmptyObject(objects);
        objs.cidFont = AppendEmptyObject(objects);
        objs.toUnicode = AppendEmptyObject(objects);
        objs.type0 = AppendEmptyObject(objects);
        objs.type0.font = font;
        font.fileObjNumber = objs.file.number;
        font.fontDescriptorObjNumber = objs.descriptor.number;
        font.cidFontDictObjNumber = objs.cidFont.number;
        font.toUnicodeCMapObjNumber = objs.toUnicode.number;
        font.objNumber = objs.type0.number;
        font.objects = objs;
    }

    private static PDFobj AppendEmptyObject(List<PDFobj> objects) {
        PDFobj obj = new PDFobj();
        obj.number = objects.Count + 1;
        objects.Add(obj);
        return obj;
    }

    // Fills in the objects of the fonts added to the objects, when the pages
    // are drawn and the glyphs they use are known.
    internal static void Complete(List<PDFobj> objects) {
        foreach (PDFobj obj in objects) {
            if (obj.font != null) {
                Complete(obj.font);
                obj.font = null;
            }
        }
    }

    // Fills in the objects of the font: its program, a subset of the glyphs
    // drawn unless it is to be whole, the descriptor and the CID font under the
    // name of the subset, the widths and the ToUnicode map of the glyphs it
    // keeps, and the Type0 font.
    private static void Complete(Font font) {
        FontObjects objs = font.objects;
        byte[] program = Subset.EmbeddedProgram(font);
        String baseFont = font.baseFont;

        byte[] compressed = Compressor.Deflate(program);
        PDFobj obj = objs.file;
        obj.dict.Add("<<");
        obj.dict.Add("/Metadata");
        obj.dict.Add(objs.metadata.ToString(CultureInfo.InvariantCulture));
        obj.dict.Add("0");
        obj.dict.Add("R");
        obj.dict.Add("/Filter");
        obj.dict.Add("/FlateDecode");
        obj.dict.Add("/Length");
        obj.dict.Add(compressed.Length.ToString(CultureInfo.InvariantCulture));
        if (font.cff) {
            obj.dict.Add("/Subtype");
            obj.dict.Add("/CIDFontType0C");
        } else {
            obj.dict.Add("/Length1");
            obj.dict.Add(program.Length.ToString(CultureInfo.InvariantCulture));
        }
        obj.dict.Add(">>");
        obj.SetStream(compressed);

        CompleteFontDescriptor(objs.descriptor, font, baseFont);
        CompleteCIDFontDictionary(objs.cidFont, font, baseFont, font.kept);

        byte[] cmap = Compressor.Deflate(Encoding.UTF8.GetBytes(FontWriter.ToUnicodeCMap(font, font.kept)));
        obj = objs.toUnicode;
        obj.dict.Add("<<");
        obj.dict.Add("/Filter");
        obj.dict.Add("/FlateDecode");
        obj.dict.Add("/Length");
        obj.dict.Add(cmap.Length.ToString(CultureInfo.InvariantCulture));
        obj.dict.Add(">>");
        obj.SetStream(cmap);

        obj = objs.type0;
        obj.dict.Add("<<");
        obj.dict.Add("/Type");
        obj.dict.Add("/Font");
        obj.dict.Add("/Subtype");
        obj.dict.Add("/Type0");
        obj.dict.Add("/BaseFont");
        obj.dict.Add("/" + baseFont);
        obj.dict.Add("/Encoding");
        obj.dict.Add("/Identity-H");
        obj.dict.Add("/DescendantFonts");
        obj.dict.Add("[");
        obj.dict.Add(font.cidFontDictObjNumber.ToString(CultureInfo.InvariantCulture));
        obj.dict.Add("0");
        obj.dict.Add("R");
        obj.dict.Add("]");
        obj.dict.Add("/ToUnicode");
        obj.dict.Add(font.toUnicodeCMapObjNumber.ToString(CultureInfo.InvariantCulture));
        obj.dict.Add("0");
        obj.dict.Add("R");
        obj.dict.Add(">>");

        // The program and the objects are no longer needed.
        font.program = null;
        font.objects = null;
    }

    internal static int AddMetadataObject(List<PDFobj> objects, Font font) {

        StringBuilder sb = new StringBuilder();
        sb.Append("<?xpacket id=\"W5M0MpCehiHzreSzNTczkc9d\"?>\n");
        sb.Append("<x:xmpmeta xmlns:x=\"adobe:ns:meta/\">\n");
        sb.Append("<rdf:RDF xmlns:rdf=\"http://www.w3.org/1999/02/22-rdf-syntax-ns#\">\n");
        sb.Append("<rdf:Description rdf:about=\"\" xmlns:xmpRights=\"http://ns.adobe.com/xap/1.0/rights/\">\n");
        sb.Append("<xmpRights:UsageTerms>\n");
        sb.Append("<rdf:Alt>\n");
        sb.Append("<rdf:li xml:lang=\"x-default\">\n");
        sb.Append(PDF.EscapeXML(font.info));
        sb.Append("</rdf:li>\n");
        sb.Append("</rdf:Alt>\n");
        sb.Append("</xmpRights:UsageTerms>\n");
        sb.Append("</rdf:Description>\n");
        sb.Append("</rdf:RDF>\n");
        sb.Append("</x:xmpmeta>\n");
        sb.Append("<?xpacket end=\"r\"?>");

        byte[] xml = (new System.Text.UTF8Encoding()).GetBytes(sb.ToString());

        // This is the metadata object
        PDFobj obj = new PDFobj();
        obj.dict.Add("<<");
        obj.dict.Add("/Type");
        obj.dict.Add("/Metadata");
        obj.dict.Add("/Subtype");
        obj.dict.Add("/XML");
        obj.dict.Add("/Length");
        obj.dict.Add(xml.Length.ToString(CultureInfo.InvariantCulture));
        obj.dict.Add(">>");
        obj.SetStream(xml);
        obj.number = objects.Count + 1;
        objects.Add(obj);

        return obj.number;
    }

    private static void CompleteFontDescriptor(PDFobj obj, Font font, String fontName) {
        obj.dict.Add("<<");
        obj.dict.Add("/Type");
        obj.dict.Add("/FontDescriptor");
        obj.dict.Add("/FontName");
        obj.dict.Add("/" + fontName);
        obj.dict.Add("/FontFile" + (font.cff ? "3" : "2"));
        obj.dict.Add(font.fileObjNumber.ToString(CultureInfo.InvariantCulture));
        obj.dict.Add("0");
        obj.dict.Add("R");
        obj.dict.Add("/Flags");
        obj.dict.Add(OpenTypeFont.FlagsOf(font.italicAngle).ToString(CultureInfo.InvariantCulture));
        obj.dict.Add("/FontBBox");
        obj.dict.Add("[");
        obj.dict.Add(OpenTypeFont.ToGlyphSpace(font.bBoxLLx, font.unitsPerEm).ToString(CultureInfo.InvariantCulture));
        obj.dict.Add(OpenTypeFont.ToGlyphSpace(font.bBoxLLy, font.unitsPerEm).ToString(CultureInfo.InvariantCulture));
        obj.dict.Add(OpenTypeFont.ToGlyphSpace(font.bBoxURx, font.unitsPerEm).ToString(CultureInfo.InvariantCulture));
        obj.dict.Add(OpenTypeFont.ToGlyphSpace(font.bBoxURy, font.unitsPerEm).ToString(CultureInfo.InvariantCulture));
        obj.dict.Add("]");
        obj.dict.Add("/Ascent");
        obj.dict.Add(OpenTypeFont.ToGlyphSpace(font.fontAscent, font.unitsPerEm).ToString(CultureInfo.InvariantCulture));
        obj.dict.Add("/Descent");
        obj.dict.Add(OpenTypeFont.ToGlyphSpace(font.fontDescent, font.unitsPerEm).ToString(CultureInfo.InvariantCulture));
        obj.dict.Add("/ItalicAngle");
        obj.dict.Add(OpenTypeFont.ItalicAngleOf(font.italicAngle));
        obj.dict.Add("/CapHeight");
        obj.dict.Add(OpenTypeFont.ToGlyphSpace(font.capHeight, font.unitsPerEm).ToString(CultureInfo.InvariantCulture));
        obj.dict.Add("/StemV");
        obj.dict.Add("79");
        obj.dict.Add(">>");
    }

    private static void CompleteCIDFontDictionary(PDFobj obj, Font font, String baseFont, bool[] kept) {
        obj.dict.Add("<<");
        obj.dict.Add("/Type");
        obj.dict.Add("/Font");
        obj.dict.Add("/Subtype");
        obj.dict.Add("/CIDFontType" + (font.cff ? "0" : "2"));
        obj.dict.Add("/BaseFont");
        obj.dict.Add("/" + baseFont);
        obj.dict.Add("/CIDSystemInfo");
        obj.dict.Add("<<");
        obj.dict.Add("/Registry");
        obj.dict.Add("(Adobe)");
        obj.dict.Add("/Ordering");
        obj.dict.Add("(Identity)");
        obj.dict.Add("/Supplement");
        obj.dict.Add("0");
        obj.dict.Add(">>");
        obj.dict.Add("/FontDescriptor");
        obj.dict.Add(font.fontDescriptorObjNumber.ToString(CultureInfo.InvariantCulture));
        obj.dict.Add("0");
        obj.dict.Add("R");

        float k = 1000.0f / Convert.ToSingle(font.unitsPerEm);
        // The width of the glyphs past the /W array: those past the advance
        // widths, which have the width of the last one.
        obj.dict.Add("/DW");
        obj.dict.Add(((int) Math.Round(k * Convert.ToSingle(font.advanceWidth[font.advanceWidth.Length - 1]), MidpointRounding.AwayFromZero)).ToString(CultureInfo.InvariantCulture));
        obj.dict.Add("/W");
        obj.dict.Add(FontWriter.WidthsArray(font, kept));
        obj.dict.Add("/CIDToGIDMap");
        obj.dict.Add("/Identity");
        obj.dict.Add(">>");
    }
}   // End of FontObjects.cs
}   // End of namespace PDFjet.NET
