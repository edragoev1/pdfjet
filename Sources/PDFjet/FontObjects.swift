/**
 * FontObjects.swift
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
import Foundation

/// The objects of a font added to an existing PDF. They are numbered when the
/// font is added, so that pages can refer to it, and filled in when the objects
/// are added to the PDF, after the pages are drawn: the font program a subset
/// of the glyphs drawn, as a font of a new PDF is at complete().
final class FontObjects {
    var metadata = 0    // The number of the font's metadata object
    let file = PDFobj()
    let descriptor = PDFobj()
    let cidFont = PDFobj()
    let toUnicode = PDFobj()
    let type0 = PDFobj()

    // Numbers the objects of the font, added to the objects of an existing
    // PDF; the Type0 font, the one pages refer to, is the last.
    static func register(_ objects: inout [PDFobj], _ font: Font) {
        let objs = FontObjects()
        objs.metadata = addMetadataObject(&objects, font)
        for obj in [objs.file, objs.descriptor, objs.cidFont, objs.toUnicode, objs.type0] {
            obj.number = objects.count + 1
            objects.append(obj)
        }
        objs.type0.font = font
        font.fileObjNumber = objs.file.number
        font.fontDescriptorObjNumber = objs.descriptor.number
        font.cidFontDictObjNumber = objs.cidFont.number
        font.toUnicodeCMapObjNumber = objs.toUnicode.number
        font.objNumber = objs.type0.number
        font.objects = objs
    }

    // Fills in the objects of the fonts added to the objects, when the pages
    // are drawn and the glyphs they use are known.
    static func complete(_ objects: [PDFobj]) {
        for obj in objects {
            if let font = obj.font {
                complete(font)
                obj.font = nil
            }
        }
    }

    // Fills in the objects of the font: its program, a subset of the glyphs
    // drawn unless it is to be whole, the descriptor and the CID font under the
    // name of the subset, the widths and the ToUnicode map of the glyphs it
    // keeps, and the Type0 font.
    private static func complete(_ font: Font) {
        guard let objs = font.objects else {
            return
        }
        let program = Subset.embeddedProgram(font)
        let baseFont = font.baseFont

        var compressed = [UInt8]()
        FlateEncode(&compressed, program)
        var obj = objs.file
        obj.dict.append("<<")
        obj.dict.append("/Metadata")
        obj.dict.append(String(objs.metadata))
        obj.dict.append("0")
        obj.dict.append("R")
        obj.dict.append("/Filter")
        obj.dict.append("/FlateDecode")
        obj.dict.append("/Length")
        obj.dict.append(String(compressed.count))
        if font.cff {
            obj.dict.append("/Subtype")
            obj.dict.append("/CIDFontType0C")
        } else {
            obj.dict.append("/Length1")
            obj.dict.append(String(program.count))
        }
        obj.dict.append(">>")
        obj.setStream(&compressed)

        completeFontDescriptor(objs.descriptor, font, baseFont)
        completeCIDFontDictionary(objs.cidFont, font, baseFont, font.kept)

        var cmap = [UInt8]()
        FlateEncode(&cmap, Array(FontWriter.toUnicodeCMap(font, font.kept).utf8))
        obj = objs.toUnicode
        obj.dict.append("<<")
        obj.dict.append("/Filter")
        obj.dict.append("/FlateDecode")
        obj.dict.append("/Length")
        obj.dict.append(String(cmap.count))
        obj.dict.append(">>")
        obj.setStream(&cmap)

        obj = objs.type0
        obj.dict.append("<<")
        obj.dict.append("/Type")
        obj.dict.append("/Font")
        obj.dict.append("/Subtype")
        obj.dict.append("/Type0")
        obj.dict.append("/BaseFont")
        obj.dict.append("/" + baseFont)
        obj.dict.append("/Encoding")
        obj.dict.append("/Identity-H")
        obj.dict.append("/DescendantFonts")
        obj.dict.append("[")
        obj.dict.append(String(font.cidFontDictObjNumber))
        obj.dict.append("0")
        obj.dict.append("R")
        obj.dict.append("]")
        obj.dict.append("/ToUnicode")
        obj.dict.append(String(font.toUnicodeCMapObjNumber))
        obj.dict.append("0")
        obj.dict.append("R")
        obj.dict.append(">>")

        // The program and the objects are no longer needed.
        font.program = nil
        font.objects = nil
    }

    static func addMetadataObject(
            _ objects: inout [PDFobj],
            _ font: Font) -> Int {
        var sb = String()
        sb.append("<?xpacket id=\"W5M0MpCehiHzreSzNTczkc9d\"?>\n")
        sb.append("<x:xmpmeta xmlns:x=\"adobe:ns:meta/\">\n")
        sb.append("<rdf:RDF xmlns:rdf=\"http://www.w3.org/1999/02/22-rdf-syntax-ns#\">\n")
        sb.append("<rdf:Description rdf:about=\"\" xmlns:xmpRights=\"http://ns.adobe.com/xap/1.0/rights/\">\n")
        sb.append("<xmpRights:UsageTerms>\n")
        sb.append("<rdf:Alt>\n")
        sb.append("<rdf:li xml:lang=\"x-default\">\n")
        sb.append(PDF.escapeXML(font.info))
        sb.append("</rdf:li>\n")
        sb.append("</rdf:Alt>\n")
        sb.append("</xmpRights:UsageTerms>\n")
        sb.append("</rdf:Description>\n")
        sb.append("</rdf:RDF>\n")
        sb.append("</x:xmpmeta>\n")
        sb.append("<?xpacket end=\"r\"?>")

        var xml = Array(sb.utf8)

        // This is the metadata object
        let obj = PDFobj()
        obj.dict.append("<<")
        obj.dict.append("/Type")
        obj.dict.append("/Metadata")
        obj.dict.append("/Subtype")
        obj.dict.append("/XML")
        obj.dict.append("/Length")
        obj.dict.append(String(xml.count))
        obj.dict.append(">>")
        obj.setStream(&xml)
        obj.number = objects.count + 1
        objects.append(obj)

        return obj.number
    }

    private static func completeFontDescriptor(_ obj: PDFobj, _ font: Font, _ fontName: String) {
        obj.dict.append("<<")
        obj.dict.append("/Type")
        obj.dict.append("/FontDescriptor")
        obj.dict.append("/FontName")
        obj.dict.append("/" + fontName)
        obj.dict.append("/FontFile" + (font.cff ? "3" : "2"))
        obj.dict.append(String(font.fileObjNumber))
        obj.dict.append("0")
        obj.dict.append("R")
        obj.dict.append("/Flags")
        obj.dict.append(String(OpenTypeFont.flagsOf(font.italicAngle)))
        obj.dict.append("/FontBBox")
        obj.dict.append("[")
        obj.dict.append(String(OpenTypeFont.toGlyphSpace(font.bBoxLLx, font.unitsPerEm)))
        obj.dict.append(String(OpenTypeFont.toGlyphSpace(font.bBoxLLy, font.unitsPerEm)))
        obj.dict.append(String(OpenTypeFont.toGlyphSpace(font.bBoxURx, font.unitsPerEm)))
        obj.dict.append(String(OpenTypeFont.toGlyphSpace(font.bBoxURy, font.unitsPerEm)))
        obj.dict.append("]")
        obj.dict.append("/Ascent")
        obj.dict.append(String(OpenTypeFont.toGlyphSpace(font.fontAscent, font.unitsPerEm)))
        obj.dict.append("/Descent")
        obj.dict.append(String(OpenTypeFont.toGlyphSpace(font.fontDescent, font.unitsPerEm)))
        obj.dict.append("/ItalicAngle")
        obj.dict.append(OpenTypeFont.italicAngleOf(font.italicAngle))
        obj.dict.append("/CapHeight")
        obj.dict.append(String(OpenTypeFont.toGlyphSpace(font.capHeight, font.unitsPerEm)))
        obj.dict.append("/StemV")
        obj.dict.append("79")
        obj.dict.append(">>")
    }

    private static func completeCIDFontDictionary(
            _ obj: PDFobj, _ font: Font, _ baseFont: String, _ kept: [Bool]?) {
        obj.dict.append("<<")
        obj.dict.append("/Type")
        obj.dict.append("/Font")
        obj.dict.append("/Subtype")
        obj.dict.append("/CIDFontType" + (font.cff ? "0" : "2"))
        obj.dict.append("/BaseFont")
        obj.dict.append("/" + baseFont)
        obj.dict.append("/CIDSystemInfo")
        obj.dict.append("<<")
        obj.dict.append("/Registry")
        obj.dict.append("(Adobe)")
        obj.dict.append("/Ordering")
        obj.dict.append("(Identity)")
        obj.dict.append("/Supplement")
        obj.dict.append("0")
        obj.dict.append(">>")
        obj.dict.append("/FontDescriptor")
        obj.dict.append(String(font.fontDescriptorObjNumber))
        obj.dict.append("0")
        obj.dict.append("R")

        var k: Float = 1.0
        if font.unitsPerEm != 1000 {
            k = Float(1000.0) / Float(font.unitsPerEm)
        }
        // The width of the glyphs past the /W array: those past the advance
        // widths, which have the width of the last one.
        obj.dict.append("/DW")
        obj.dict.append(String(Int32(round(k * Float(font.advanceWidth[font.advanceWidth.count - 1])))))
        obj.dict.append("/W")
        obj.dict.append(FontWriter.widthsArray(font, kept))
        obj.dict.append("/CIDToGIDMap")
        obj.dict.append("/Identity")
        obj.dict.append(">>")
    }
}   // End of FontObjects.swift
