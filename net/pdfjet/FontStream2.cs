/**
 *  FontStream2.cs
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
using System;
using System.IO;
using System.Text;
using System.Collections.Generic;


namespace PDFjet.NET {
class FontStream2 {

    internal static void Register(List<PDFobj> objects, Font font, Stream inputStream) {
        FontStream1.GetFontData(font, inputStream);

        EmbedFontFile(objects, font, inputStream);
        AddFontDescriptorObject(objects, font);
        AddCIDFontDictionaryObject(objects, font);
        AddToUnicodeCMapObject(objects, font);

        // Type0 Font Dictionary
        PDFobj obj = new PDFobj();
        obj.dict.Add("<<");
        obj.dict.Add("/Type");
        obj.dict.Add("/Font");
        obj.dict.Add("/Subtype");
        obj.dict.Add("/Type0");
        obj.dict.Add("/BaseFont");
        obj.dict.Add("/" + font.name);
        obj.dict.Add("/Encoding");
        obj.dict.Add("/Identity-H");
        obj.dict.Add("/DescendantFonts");
        obj.dict.Add("[");
        obj.dict.Add(font.cidFontDictObjNumber.ToString());
        obj.dict.Add("0");
        obj.dict.Add("R");
        obj.dict.Add("]");
        obj.dict.Add("/ToUnicode");
        obj.dict.Add(font.toUnicodeCMapObjNumber.ToString());
        obj.dict.Add("0");
        obj.dict.Add("R");
        obj.dict.Add(">>");
        obj.number = objects.Count + 1;
        font.objNumber = obj.number;
        objects.Add(obj);
    }


    private static int AddMetadataObject(List<PDFobj> objects, Font font) {

        StringBuilder sb = new StringBuilder();
        sb.Append("<?xpacket begin='\uFEFF' id=\"W5M0MpCehiHzreSzNTczkc9d\"?>\n");
        sb.Append("<x:xmpmeta xmlns:x=\"adobe:ns:meta/\">\n");
        sb.Append("<rdf:RDF xmlns:rdf=\"http://www.w3.org/1999/02/22-rdf-syntax-ns#\">\n");
        sb.Append("<rdf:Description rdf:about=\"\" xmlns:xmpRights=\"http://ns.adobe.com/xap/1.0/rights/\">\n");
        sb.Append("<xmpRights:UsageTerms>\n");
        sb.Append("<rdf:Alt>\n");
        sb.Append("<rdf:li xml:lang=\"x-default\">\n");
        sb.Append(font.info);
        sb.Append("</rdf:li>\n");
        sb.Append("</rdf:Alt>\n");
        sb.Append("</xmpRights:UsageTerms>\n");
        sb.Append("</rdf:Description>\n");
        sb.Append("</rdf:RDF>\n");
        sb.Append("</x:xmpmeta>\n");
        sb.Append("<?xpacket end=\"w\"?>");

        byte[] xml = (new System.Text.UTF8Encoding()).GetBytes(sb.ToString());

        // This is the metadata object
        PDFobj obj = new PDFobj();
        obj.dict.Add("<<");
        obj.dict.Add("/Type");
        obj.dict.Add("/Metadata");
        obj.dict.Add("/Subtype");
        obj.dict.Add("/XML");
        obj.dict.Add("/Length");
        obj.dict.Add(xml.Length.ToString());
        obj.dict.Add(">>");
        obj.SetStream(xml);
        obj.number = objects.Count + 1;
        objects.Add(obj);

        return obj.number;
    }


    private static void EmbedFontFile(
            List<PDFobj> objects,
            Font font,
            Stream inputStream) {

        int metadataObjNumber = AddMetadataObject(objects, font);

        PDFobj obj = new PDFobj();
        obj.dict.Add("<<");
        obj.dict.Add("/Metadata");
        obj.dict.Add(metadataObjNumber.ToString());
        obj.dict.Add("0");
        obj.dict.Add("R");
        obj.dict.Add("/Filter");
        obj.dict.Add("/FlateDecode");
        obj.dict.Add("/Length");
        obj.dict.Add(font.compressedSize.ToString());
        if (font.cff) {
            obj.dict.Add("/Subtype");
            obj.dict.Add("/CIDFontType0C");
        }
        else {
            obj.dict.Add("/Length1");
            obj.dict.Add(font.uncompressedSize.ToString());
        }
        obj.dict.Add(">>");
        MemoryStream buf2 = new MemoryStream();
        byte[] buf = new byte[4096];
        int len;
        while ((len = inputStream.Read(buf, 0, buf.Length)) > 0) {
            buf2.Write(buf, 0, len);
        }
        inputStream.Close();
        obj.SetStream(buf2.ToArray());
        obj.number = objects.Count + 1;
        objects.Add(obj);
        font.fileObjNumber = obj.number;
    }


    private static void AddFontDescriptorObject(List<PDFobj> objects, Font font) {
        PDFobj obj = new PDFobj();
        obj.dict.Add("<<");
        obj.dict.Add("/Type");
        obj.dict.Add("/FontDescriptor");
        obj.dict.Add("/FontName");
        obj.dict.Add("/" + font.name);
        obj.dict.Add("/FontFile" + (font.cff ? "3" : "2"));
        obj.dict.Add(font.fileObjNumber.ToString());
        obj.dict.Add("0");
        obj.dict.Add("R");
        obj.dict.Add("/Flags");
        obj.dict.Add("32");
        obj.dict.Add("/FontBBox");
        obj.dict.Add("[");
        obj.dict.Add(font.bBoxLLx.ToString());
        obj.dict.Add(font.bBoxLLy.ToString());
        obj.dict.Add(font.bBoxURx.ToString());
        obj.dict.Add(font.bBoxURy.ToString());
        obj.dict.Add("]");
        obj.dict.Add("/Ascent");
        obj.dict.Add(font.fontAscent.ToString());
        obj.dict.Add("/Descent");
        obj.dict.Add(font.fontDescent.ToString());
        obj.dict.Add("/ItalicAngle");
        obj.dict.Add("0");
        obj.dict.Add("/CapHeight");
        obj.dict.Add(font.capHeight.ToString());
        obj.dict.Add("/StemV");
        obj.dict.Add("79");
        obj.dict.Add(">>");
        obj.number = objects.Count + 1;
        objects.Add(obj);
        font.fontDescriptorObjNumber = obj.number;
    }


    private static void AddToUnicodeCMapObject(List<PDFobj> objects, Font font) {

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
                buf.Append(FontStream1.ToHexString(gid));
                buf.Append("> <");
                buf.Append(FontStream1.ToHexString(cid));
                buf.Append(">\n");
                list.Add(buf.ToString());
                buf.Length = 0;
                if (list.Count == 100) {
                    FontStream1.WriteListToBuffer(sb, list);
                }
            }
        }
        if (list.Count > 0) {
            FontStream1.WriteListToBuffer(sb, list);
        }
        sb.Append("endcmap\n");
        sb.Append("CMapName currentdict /CMap defineresource pop\n");
        sb.Append("end\nend");

        PDFobj obj = new PDFobj();
        obj.dict.Add("<<");
        obj.dict.Add("/Length");
        obj.dict.Add(sb.Length.ToString());
        obj.dict.Add(">>");
        obj.SetStream((new System.Text.UTF8Encoding()).GetBytes(sb.ToString()));
        obj.number = objects.Count + 1;
        objects.Add(obj);
        font.toUnicodeCMapObjNumber = obj.number;
    }


    private static void AddCIDFontDictionaryObject(List<PDFobj> objects, Font font) {

        PDFobj obj = new PDFobj();
        obj.dict.Add("<<");
        obj.dict.Add("/Type");
        obj.dict.Add("/Font");
        obj.dict.Add("/Subtype");
        obj.dict.Add("/CIDFontType" + (font.cff ? "0" : "2"));
        obj.dict.Add("/BaseFont");
        obj.dict.Add("/" + font.name);
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
        obj.dict.Add(font.fontDescriptorObjNumber.ToString());
        obj.dict.Add("0");
        obj.dict.Add("R");
        obj.dict.Add("/DW");
        obj.dict.Add(((Int32)
                ((1000f / font.unitsPerEm) * font.advanceWidth[0])).ToString());
        obj.dict.Add("/W");
        obj.dict.Add("[");
        obj.dict.Add("0");
        obj.dict.Add("[");
        for (int i = 0; i < font.advanceWidth.Length; i++) {
            obj.dict.Add(((int)
                    ((1000f / font.unitsPerEm) * font.advanceWidth[i])).ToString());
        }
        obj.dict.Add("]");
        obj.dict.Add("]");
        obj.dict.Add("/CIDToGIDMap");
        obj.dict.Add("/Identity");
        obj.dict.Add(">>");
        obj.number = objects.Count + 1;
        objects.Add(obj);
        font.cidFontDictObjNumber = obj.number;
    }

}   // End of FontStream2.cs
}   // End of namespace PDFjet.NET
