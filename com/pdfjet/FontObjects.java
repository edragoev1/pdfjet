/*
 * FontObjects.java
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
package com.pdfjet;

import java.nio.charset.StandardCharsets;
import java.util.*;

/**
 * The objects of a font added to an existing PDF. They are numbered when the
 * font is added, so that pages can refer to it, and filled in when the objects
 * are added to the PDF, after the pages are drawn: the font program a subset
 * of the glyphs drawn, as a font of a new PDF is at complete().
 */
class FontObjects {
    int metadata;   // The number of the font's metadata object
    PDFobj file;
    PDFobj descriptor;
    PDFobj cidFont;
    PDFobj toUnicode;
    PDFobj type0;

    // Numbers the objects of the font, added to the objects of an existing
    // PDF; the Type0 font, the one pages refer to, is the last.
    static void register(List<PDFobj> objects, Font font) throws Exception {
        FontObjects objs = new FontObjects();
        objs.metadata = addMetadataObject(objects, font);
        objs.file = appendEmptyObject(objects);
        objs.descriptor = appendEmptyObject(objects);
        objs.cidFont = appendEmptyObject(objects);
        objs.toUnicode = appendEmptyObject(objects);
        objs.type0 = appendEmptyObject(objects);
        objs.type0.font = font;
        font.fileObjNumber = objs.file.number;
        font.fontDescriptorObjNumber = objs.descriptor.number;
        font.cidFontDictObjNumber = objs.cidFont.number;
        font.toUnicodeCMapObjNumber = objs.toUnicode.number;
        font.objNumber = objs.type0.number;
        font.objects = objs;
    }

    private static PDFobj appendEmptyObject(List<PDFobj> objects) {
        PDFobj obj = new PDFobj();
        obj.number = objects.size() + 1;
        objects.add(obj);
        return obj;
    }

    // Fills in the objects of the fonts added to the objects, when the pages
    // are drawn and the glyphs they use are known.
    static void complete(List<PDFobj> objects) throws Exception {
        for (PDFobj obj : objects) {
            if (obj.font != null) {
                complete(obj.font);
                obj.font = null;
            }
        }
    }

    // Fills in the objects of the font: its program, a subset of the glyphs
    // drawn unless it is to be whole, the descriptor and the CID font under the
    // name of the subset, the widths and the ToUnicode map of the glyphs it
    // keeps, and the Type0 font.
    private static void complete(Font font) throws Exception {
        FontObjects objs = font.objects;
        byte[] program = Subset.embeddedProgram(font);
        String baseFont = font.baseFont;

        byte[] compressed = Compressor.deflate(program);
        PDFobj obj = objs.file;
        obj.dict.add("<<");
        obj.dict.add("/Metadata");
        obj.dict.add(String.valueOf(objs.metadata));
        obj.dict.add("0");
        obj.dict.add("R");
        obj.dict.add("/Filter");
        obj.dict.add("/FlateDecode");
        obj.dict.add("/Length");
        obj.dict.add(String.valueOf(compressed.length));
        if (font.cff) {
            obj.dict.add("/Subtype");
            obj.dict.add("/CIDFontType0C");
        } else {
            obj.dict.add("/Length1");
            obj.dict.add(String.valueOf(program.length));
        }
        obj.dict.add(">>");
        obj.setStream(compressed);

        completeFontDescriptor(objs.descriptor, font, baseFont);
        completeCIDFontDictionary(objs.cidFont, font, baseFont, font.kept);

        byte[] cmap = Compressor.deflate(
                FontWriter.toUnicodeCMap(font, font.kept).getBytes(StandardCharsets.UTF_8));
        obj = objs.toUnicode;
        obj.dict.add("<<");
        obj.dict.add("/Filter");
        obj.dict.add("/FlateDecode");
        obj.dict.add("/Length");
        obj.dict.add(String.valueOf(cmap.length));
        obj.dict.add(">>");
        obj.setStream(cmap);

        obj = objs.type0;
        obj.dict.add("<<");
        obj.dict.add("/Type");
        obj.dict.add("/Font");
        obj.dict.add("/Subtype");
        obj.dict.add("/Type0");
        obj.dict.add("/BaseFont");
        obj.dict.add("/" + baseFont);
        obj.dict.add("/Encoding");
        obj.dict.add("/Identity-H");
        obj.dict.add("/DescendantFonts");
        obj.dict.add("[");
        obj.dict.add(String.valueOf(font.cidFontDictObjNumber));
        obj.dict.add("0");
        obj.dict.add("R");
        obj.dict.add("]");
        obj.dict.add("/ToUnicode");
        obj.dict.add(String.valueOf(font.toUnicodeCMapObjNumber));
        obj.dict.add("0");
        obj.dict.add("R");
        obj.dict.add(">>");

        // The program and the objects are no longer needed.
        font.program = null;
        font.objects = null;
    }

    static int addMetadataObject(List<PDFobj> objects, Font font) throws Exception {
        StringBuilder sb = new StringBuilder();
        sb.append("<?xpacket id=\"W5M0MpCehiHzreSzNTczkc9d\"?>\n");
        sb.append("<x:xmpmeta xmlns:x=\"adobe:ns:meta/\">\n");
        sb.append("<rdf:RDF xmlns:rdf=\"http://www.w3.org/1999/02/22-rdf-syntax-ns#\">\n");
        sb.append("<rdf:Description rdf:about=\"\" xmlns:xmpRights=\"http://ns.adobe.com/xap/1.0/rights/\">\n");
        sb.append("<xmpRights:UsageTerms>\n");
        sb.append("<rdf:Alt>\n");
        sb.append("<rdf:li xml:lang=\"x-default\">\n");
        sb.append(PDF.escapeXML(font.info));
        sb.append("</rdf:li>\n");
        sb.append("</rdf:Alt>\n");
        sb.append("</xmpRights:UsageTerms>\n");
        sb.append("</rdf:Description>\n");
        sb.append("</rdf:RDF>\n");
        sb.append("</x:xmpmeta>\n");
        sb.append("<?xpacket end=\"r\"?>");

        byte[] xml = sb.toString().getBytes(StandardCharsets.UTF_8);

        // This is the metadata object
        PDFobj obj = new PDFobj();
        obj.dict.add("<<");
        obj.dict.add("/Type");
        obj.dict.add("/Metadata");
        obj.dict.add("/Subtype");
        obj.dict.add("/XML");
        obj.dict.add("/Length");
        obj.dict.add(String.valueOf(xml.length));
        obj.dict.add(">>");
        obj.setStream(xml);
        obj.number = objects.size() + 1;
        objects.add(obj);

        return obj.number;
    }

    private static void completeFontDescriptor(PDFobj obj, Font font, String fontName) {
        obj.dict.add("<<");
        obj.dict.add("/Type");
        obj.dict.add("/FontDescriptor");
        obj.dict.add("/FontName");
        obj.dict.add("/" + fontName);
        obj.dict.add("/FontFile" + (font.cff ? "3" : "2"));
        obj.dict.add(String.valueOf(font.fileObjNumber));
        obj.dict.add("0");
        obj.dict.add("R");
        obj.dict.add("/Flags");
        obj.dict.add(String.valueOf(OpenTypeFont.flagsOf(font.italicAngle)));
        obj.dict.add("/FontBBox");
        obj.dict.add("[");
        obj.dict.add(String.valueOf(OpenTypeFont.toGlyphSpace(font.bBoxLLx, font.unitsPerEm)));
        obj.dict.add(String.valueOf(OpenTypeFont.toGlyphSpace(font.bBoxLLy, font.unitsPerEm)));
        obj.dict.add(String.valueOf(OpenTypeFont.toGlyphSpace(font.bBoxURx, font.unitsPerEm)));
        obj.dict.add(String.valueOf(OpenTypeFont.toGlyphSpace(font.bBoxURy, font.unitsPerEm)));
        obj.dict.add("]");
        obj.dict.add("/Ascent");
        obj.dict.add(String.valueOf(OpenTypeFont.toGlyphSpace(font.fontAscent, font.unitsPerEm)));
        obj.dict.add("/Descent");
        obj.dict.add(String.valueOf(OpenTypeFont.toGlyphSpace(font.fontDescent, font.unitsPerEm)));
        obj.dict.add("/ItalicAngle");
        obj.dict.add(OpenTypeFont.italicAngleOf(font.italicAngle));
        obj.dict.add("/CapHeight");
        obj.dict.add(String.valueOf(OpenTypeFont.toGlyphSpace(font.capHeight, font.unitsPerEm)));
        obj.dict.add("/StemV");
        obj.dict.add("79");
        obj.dict.add(">>");
    }

    private static void completeCIDFontDictionary(
            PDFobj obj, Font font, String baseFont, boolean[] kept) {
        obj.dict.add("<<");
        obj.dict.add("/Type");
        obj.dict.add("/Font");
        obj.dict.add("/Subtype");
        obj.dict.add("/CIDFontType" + (font.cff ? "0" : "2"));
        obj.dict.add("/BaseFont");
        obj.dict.add("/" + baseFont);
        obj.dict.add("/CIDSystemInfo");
        obj.dict.add("<<");
        obj.dict.add("/Registry");
        obj.dict.add("(Adobe)");
        obj.dict.add("/Ordering");
        obj.dict.add("(Identity)");
        obj.dict.add("/Supplement");
        obj.dict.add("0");
        obj.dict.add(">>");
        obj.dict.add("/FontDescriptor");
        obj.dict.add(String.valueOf(font.fontDescriptorObjNumber));
        obj.dict.add("0");
        obj.dict.add("R");

        final float k = 1000.0f / (float) font.unitsPerEm;
        // The width of the glyphs past the /W array: those past the advance
        // widths, which have the width of the last one.
        obj.dict.add("/DW");
        obj.dict.add(String.valueOf(Math.round(k * (float) font.advanceWidth[font.advanceWidth.length - 1])));
        obj.dict.add("/W");
        obj.dict.add(FontWriter.widthsArray(font, kept));
        obj.dict.add("/CIDToGIDMap");
        obj.dict.add("/Identity");
        obj.dict.add(">>");
    }
}   // End of FontObjects.java
