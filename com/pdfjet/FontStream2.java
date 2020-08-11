/**
 *  FontStream2.java
 *
Copyright 2020 Innovatics Inc.

Redistribution and use in source and binary forms, with or without modification,
are permitted provided that the following conditions are met:

    * Redistributions of source code must retain the above copyright notice,
      this list of conditions and the following disclaimer.

    * Redistributions in binary form must reproduce the above copyright notice,
      this list of conditions and the following disclaimer in the documentation
      and / or other materials provided with the distribution.

THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS
"AS IS" AND ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT
LIMITED TO, THE IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR
A PARTICULAR PURPOSE ARE DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT OWNER OR
CONTRIBUTORS BE LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL,
EXEMPLARY, OR CONSEQUENTIAL DAMAGES (INCLUDING, BUT NOT LIMITED TO,
PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES; LOSS OF USE, DATA, OR
PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND ON ANY THEORY OF
LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT (INCLUDING
NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS
SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
*/
package com.pdfjet;

import java.io.*;
import java.util.*;


class FontStream2 {

    protected static void register(
            List<PDFobj> objects,
            Font font,
            InputStream inputStream) throws Exception {
        FontStream1.getFontData(font, inputStream);

        embedFontFile(objects, font, inputStream);
        addFontDescriptorObject(objects, font);
        addCIDFontDictionaryObject(objects, font);
        addToUnicodeCMapObject(objects, font);

        // Type0 Font Dictionary
        PDFobj obj = new PDFobj();
        obj.dict.add("<<");
        obj.dict.add("/Type");
        obj.dict.add("/Font");
        obj.dict.add("/Subtype");
        obj.dict.add("/Type0");
        obj.dict.add("/BaseFont");
        obj.dict.add("/" + font.name);
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
        obj.number = objects.size() + 1;
        objects.add(obj);
        font.objNumber = obj.number;
    }


    private static int addMetadataObject(List<PDFobj> objects, Font font) throws Exception {

        StringBuilder sb = new StringBuilder();
        sb.append("<?xpacket begin='\uFEFF' id=\"W5M0MpCehiHzreSzNTczkc9d\"?>\n");
        sb.append("<x:xmpmeta xmlns:x=\"adobe:ns:meta/\">\n");
        sb.append("<rdf:RDF xmlns:rdf=\"http://www.w3.org/1999/02/22-rdf-syntax-ns#\">\n");
        sb.append("<rdf:Description rdf:about=\"\" xmlns:xmpRights=\"http://ns.adobe.com/xap/1.0/rights/\">\n");
        sb.append("<xmpRights:UsageTerms>\n");
        sb.append("<rdf:Alt>\n");
        sb.append("<rdf:li xml:lang=\"x-default\">\n");
        sb.append(font.info);
        sb.append("</rdf:li>\n");
        sb.append("</rdf:Alt>\n");
        sb.append("</xmpRights:UsageTerms>\n");
        sb.append("</rdf:Description>\n");
        sb.append("</rdf:RDF>\n");
        sb.append("</x:xmpmeta>\n");
        sb.append("<?xpacket end=\"w\"?>");

        byte[] xml = sb.toString().getBytes("UTF-8");

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


    private static void embedFontFile(
            List<PDFobj> objects,
            Font font,
            InputStream inputStream) throws Exception {

        int metadataObjNumber = addMetadataObject(objects, font);

        PDFobj obj = new PDFobj();
        obj.dict.add("<<");
        obj.dict.add("/Metadata");
        obj.dict.add(String.valueOf(metadataObjNumber));
        obj.dict.add("0");
        obj.dict.add("R");
        obj.dict.add("/Filter");
        obj.dict.add("/FlateDecode");
        obj.dict.add("/Length");
        obj.dict.add(String.valueOf(font.compressedSize));
        if (font.cff) {
            obj.dict.add("/Subtype");
            obj.dict.add("/CIDFontType0C");
        }
        else {
            obj.dict.add("/Length1");
            obj.dict.add(String.valueOf(font.uncompressedSize));
        }
        obj.dict.add(">>");
        ByteArrayOutputStream buf2 = new ByteArrayOutputStream();
        byte[] buf = new byte[4096];
        int len;
        while ((len = inputStream.read(buf, 0, buf.length)) > 0) {
            buf2.write(buf, 0, len);
        }
        inputStream.close();
        obj.setStream(buf2.toByteArray());
        obj.number = objects.size() + 1;
        objects.add(obj);
        font.fileObjNumber = obj.number;
    }


    private static void addFontDescriptorObject(List<PDFobj> objects, Font font) {
        PDFobj obj = new PDFobj();
        obj.dict.add("<<");
        obj.dict.add("/Type");
        obj.dict.add("/FontDescriptor");
        obj.dict.add("/FontName");
        obj.dict.add("/" + font.name);
        obj.dict.add("/FontFile" + (font.cff ? "3" : "2"));
        obj.dict.add(String.valueOf(font.fileObjNumber));
        obj.dict.add("0");
        obj.dict.add("R");
        obj.dict.add("/Flags");
        obj.dict.add("32");
        obj.dict.add("/FontBBox");
        obj.dict.add("[");
        obj.dict.add(String.valueOf(font.bBoxLLx));
        obj.dict.add(String.valueOf(font.bBoxLLy));
        obj.dict.add(String.valueOf(font.bBoxURx));
        obj.dict.add(String.valueOf(font.bBoxURy));
        obj.dict.add("]");
        obj.dict.add("/Ascent");
        obj.dict.add(String.valueOf(font.fontAscent));
        obj.dict.add("/Descent");
        obj.dict.add(String.valueOf(font.fontDescent));
        obj.dict.add("/ItalicAngle");
        obj.dict.add("0");
        obj.dict.add("/CapHeight");
        obj.dict.add(String.valueOf(font.capHeight));
        obj.dict.add("/StemV");
        obj.dict.add("79");
        obj.dict.add(">>");
        obj.number = objects.size() + 1;
        objects.add(obj);
        font.fontDescriptorObjNumber = obj.number;
    }


    private static void addToUnicodeCMapObject(
            List<PDFobj> objects, Font font) throws Exception {

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
        StringBuilder buf = new StringBuilder();
        for (int cid = 0; cid <= 0xffff; cid++) {
            int gid = font.unicodeToGID[cid];
            if (gid > 0) {
                buf.append('<');
                buf.append(FontStream1.toHexString(gid));
                buf.append("> <");
                buf.append(FontStream1.toHexString(cid));
                buf.append(">\n");
                list.add(buf.toString());
                buf.setLength(0);
                if (list.size() == 100) {
                    FontStream1.writeListToBuffer(sb, list);
                }
            }
        }
        if (list.size() > 0) {
            FontStream1.writeListToBuffer(sb, list);
        }
        sb.append("endcmap\n");
        sb.append("CMapName currentdict /CMap defineresource pop\n");
        sb.append("end\nend");

        PDFobj obj = new PDFobj();
        obj.dict.add("<<");
        obj.dict.add("/Length");
        obj.dict.add(String.valueOf(sb.length()));
        obj.dict.add(">>");
        obj.setStream(sb.toString().getBytes("UTF-8"));
        obj.number = objects.size() + 1;
        objects.add(obj);
        font.toUnicodeCMapObjNumber = obj.number;
    }


    private static void addCIDFontDictionaryObject(List<PDFobj> objects, Font font) {
        PDFobj obj = new PDFobj();
        obj.dict.add("<<");
        obj.dict.add("/Type");
        obj.dict.add("/Font");
        obj.dict.add("/Subtype");
        obj.dict.add("/CIDFontType" + (font.cff ? "0" : "2"));
        obj.dict.add("/BaseFont");
        obj.dict.add("/" + font.name);
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
        obj.dict.add("/DW");
        obj.dict.add(String.valueOf((int)
                ((1000f / font.unitsPerEm) * font.advanceWidth[0])));
        obj.dict.add("/W");
        obj.dict.add("[");
        obj.dict.add("0");
        obj.dict.add("[");
        for (int i = 0; i < font.advanceWidth.length; i++) {
            obj.dict.add(String.valueOf((int)
                    ((1000f / font.unitsPerEm) * font.advanceWidth[i])));
        }
        obj.dict.add("]");
        obj.dict.add("]");
        obj.dict.add("/CIDToGIDMap");
        obj.dict.add("/Identity");
        obj.dict.add(">>");
        obj.number = objects.size() + 1;
        objects.add(obj);
        font.cidFontDictObjNumber = obj.number;
    }

}   // End of FontStream2.java
