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
///
/// The Type0 font, the object pages refer to, holds these objects until they
/// are filled in, and they hold the font, so that the font is written though
/// the caller no longer holds it; neither the font nor these objects hold the
/// Type0 font, so that objects never added to a PDF free the font and its
/// program (the review of 9 October 2026: the font held its objects, and the
/// Type0 font the font, a cycle that kept the font program until the objects
/// were added, and forever when they were not).
final class FontObjects {
    let font: Font
    var metadata = 0    // The number of the font's metadata object
    let file = PDFobj()
    let descriptor = PDFobj()
    let cidFont = PDFobj()
    let toUnicode = PDFobj()

    private init(_ font: Font) {
        self.font = font
    }

    // Numbers the objects of the font, added to the objects of an existing
    // PDF; the Type0 font, the one pages refer to, is the last.
    static func register(_ objects: inout [PDFobj], _ font: Font) {
        let objs = FontObjects(font)
        objs.metadata = addMetadataObject(&objects, font)
        let type0 = PDFobj()
        for obj in [objs.file, objs.descriptor, objs.cidFont, objs.toUnicode, type0] {
            obj.number = objects.count + 1
            objects.append(obj)
        }
        type0.fontObjects = objs
        font.fileObjNumber = objs.file.number
        font.fontDescriptorObjNumber = objs.descriptor.number
        font.cidFontDictObjNumber = objs.cidFont.number
        font.toUnicodeCMapObjNumber = objs.toUnicode.number
        font.objNumber = type0.number
    }

    // Fills in the objects of the fonts added to the objects, when the pages
    // are drawn and the glyphs they use are known.
    static func complete(_ objects: [PDFobj]) {
        for obj in objects {
            if let objs = obj.fontObjects {
                obj.fontObjects = nil
                objs.complete(obj)
            }
        }
    }

    // Fills in the objects of the font: its program, a subset of the glyphs
    // drawn unless it is to be whole, the descriptor and the CID font under the
    // name of the subset, the widths and the ToUnicode map of the glyphs it
    // keeps, and the Type0 font.
    private func complete(_ type0: PDFobj) {
        let program = Subset.embeddedProgram(font)
        let baseFont = font.baseFont

        var compressed = [UInt8]()
        FlateEncode(&compressed, program)
        var obj = file
        obj.dict.append("<<")
        obj.dict.append("/Metadata")
        obj.dict.append(String(metadata))
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

        FontObjects.completeFontDescriptor(descriptor, font, baseFont)
        FontObjects.completeCIDFontDictionary(cidFont, font, baseFont, font.kept)

        var cmap = [UInt8]()
        FlateEncode(&cmap, Array(FontWriter.toUnicodeCMap(font, font.kept).utf8))
        obj = toUnicode
        obj.dict.append("<<")
        obj.dict.append("/Filter")
        obj.dict.append("/FlateDecode")
        obj.dict.append("/Length")
        obj.dict.append(String(cmap.count))
        obj.dict.append(">>")
        obj.setStream(&cmap)

        obj = type0
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

        // The program is no longer needed.
        font.program = nil
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
