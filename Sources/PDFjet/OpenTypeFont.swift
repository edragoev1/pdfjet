/**
 *  OpenTypeFont.swift
 *
Copyright 2020 Innovatics Inc.
*/
import Foundation

class OpenTypeFont {
    internal static func register(
            _ pdf: PDF, _ font: Font, _ stream: InputStream) {
        let otf = OTF(stream)

        font.name = otf.fontName!
        font.firstChar = otf.firstChar!
        font.lastChar = otf.lastChar!
        font.unicodeToGID = otf.unicodeToGID
        font.unitsPerEm = otf.unitsPerEm!
        font.bBoxLLx = otf.bBoxLLx!
        font.bBoxLLy = otf.bBoxLLy!
        font.bBoxURx = otf.bBoxURx!
        font.bBoxURy = otf.bBoxURy!
        font.advanceWidth = otf.advanceWidth!
        font.glyphWidth = otf.glyphWidth
        font.fontAscent = otf.ascent!
        font.fontDescent = otf.descent!
        font.fontUnderlinePosition = otf.underlinePosition!
        font.fontUnderlineThickness = otf.underlineThickness!
        font.setSize(font.size)

        embedFontFile(pdf, font, otf)
        addFontDescriptorObject(pdf, font, otf)
        addCIDFontDictionaryObject(pdf, font, otf)
        addToUnicodeCMapObject(pdf, font, otf)

        // Type0 Font Dictionary
        pdf.newobj()
        pdf.append("<<\n")
        pdf.append("/Type /Font\n")
        pdf.append("/Subtype /Type0\n")
        pdf.append("/BaseFont /")
        pdf.append(otf.fontName!)
        pdf.append("\n")
        pdf.append("/Encoding /Identity-H\n")
        pdf.append("/DescendantFonts [")
        pdf.append(font.cidFontDictObjNumber)
        pdf.append(" 0 R]\n")

        pdf.append("/ToUnicode ")
        pdf.append(font.toUnicodeCMapObjNumber)
        pdf.append(" 0 R\n")

        pdf.append(">>\n")
        pdf.endobj()

        font.objNumber = pdf.getObjNumber()
        pdf.fonts.append(font)
    }


    private static func embedFontFile(_ pdf: PDF, _ font: Font, _ otf: OTF) {
        // Check if the font file is already embedded
        for f in pdf.fonts {
            if f.fileObjNumber != 0 && f.name == otf.fontName {
                font.fileObjNumber = f.fileObjNumber
                return
            }
        }

        let metadataObjNumber = pdf.addMetadataObject(otf.fontInfo!, true)
        if metadataObjNumber != -1 {
            pdf.append("/Metadata ")
            pdf.append(metadataObjNumber)
            pdf.append(" 0 R\n")
        }

        pdf.newobj()
        pdf.append("<<\n")
        if otf.cff {
            pdf.append("/Subtype /CIDFontType0C\n")
        }
        pdf.append("/Filter /FlateDecode\n")
        // pdf.append("/Filter /LZWDecode\n")

        pdf.append("/Length ")
        pdf.append(otf.dos.count)      // The compressed size
        pdf.append("\n")

        if !otf.cff {
            pdf.append("/Length1 ")
            pdf.append(otf.buf.count)   // The uncompressed size
            pdf.append("\n")
        }

        pdf.append(">>\n")
        pdf.append("stream\n")
        pdf.append(otf.dos)
        pdf.append("\nendstream\n")
        pdf.endobj()

        font.fileObjNumber = pdf.getObjNumber()
    }


    private static func addFontDescriptorObject(
            _ pdf: PDF,
            _ font: Font,
            _ otf: OTF) {
        for f in pdf.fonts {
            if f.fontDescriptorObjNumber != 0 && f.name == otf.fontName {
                font.fontDescriptorObjNumber = f.fontDescriptorObjNumber
                return
            }
        }

        let factor = Float(1000.0) / Float(otf.unitsPerEm!)

        pdf.newobj()
        pdf.append("<<\n")
        pdf.append("/Type /FontDescriptor\n")
        pdf.append("/FontName /")
        pdf.append(otf.fontName!)
        pdf.append("\n")
        if otf.cff {
            pdf.append("/FontFile3 ")
        }
        else {
            pdf.append("/FontFile2 ")
        }
        pdf.append(font.fileObjNumber)
        pdf.append(" 0 R\n")
        pdf.append("/Flags 32\n")
        pdf.append("/FontBBox [")
        pdf.append(Int32(Float(otf.bBoxLLx!) * factor))
        pdf.append(" ")
        pdf.append(Int32(Float(otf.bBoxLLy!) * factor))
        pdf.append(" ")
        pdf.append(Int32(Float(otf.bBoxURx!) * factor))
        pdf.append(" ")
        pdf.append(Int32(Float(otf.bBoxURy!) * factor))
        pdf.append("]\n")
        pdf.append("/Ascent ")
        pdf.append(Int32(Float(otf.ascent!) * factor))
        pdf.append("\n")
        pdf.append("/Descent ")
        pdf.append(Int32(Float(otf.descent!) * factor))
        pdf.append("\n")
        pdf.append("/ItalicAngle 0\n")
        pdf.append("/CapHeight ")
        pdf.append(Int32(Float(otf.capHeight!) * factor))
        pdf.append("\n")
        pdf.append("/StemV 79\n")
        pdf.append(">>\n")
        pdf.endobj()

        font.fontDescriptorObjNumber = pdf.getObjNumber()
    }


    private static func addToUnicodeCMapObject(
            _ pdf: PDF,
            _ font: Font,
            _ otf: OTF) {

        for f in pdf.fonts {
            if f.toUnicodeCMapObjNumber != 0 && f.name == otf.fontName {
                font.toUnicodeCMapObjNumber = f.toUnicodeCMapObjNumber
                return
            }
        }

        var sb = String()

        sb.append("/CIDInit /ProcSet findresource begin\n")
        sb.append("12 dict begin\n")
        sb.append("begincmap\n")
        sb.append("/CIDSystemInfo <</Registry (Adobe) /Ordering (Identity) /Supplement 0>> def\n")
        sb.append("/CMapName /Adobe-Identity def\n")
        sb.append("/CMapType 2 def\n")

        sb.append("1 begincodespacerange\n")
        sb.append("<0000> <FFFF>\n")
        sb.append("endcodespacerange\n")

        var list = [String]()
        var buf = String()
        for cid in 0...0xffff {
            let gid = otf.unicodeToGID[cid]
            if gid > 0 {
                buf.append("<")
                buf.append(toHexString(gid))
                buf.append("> <")
                buf.append(toHexString(cid))
                buf.append(">\n")
                list.append(buf)
                buf = ""
                if list.count == 100 {
                    writeListToBuffer(&list, &sb)
                }
            }
        }
        if list.count > 0 {
            writeListToBuffer(&list, &sb)
        }

        sb.append("endcmap\n")
        sb.append("CMapName currentdict /CMap defineresource pop\n")
        sb.append("end\nend")

        pdf.newobj()
        pdf.append("<<\n")
        pdf.append("/Length ")
        pdf.append(sb.count)
        pdf.append("\n")
        pdf.append(">>\n")
        pdf.append("stream\n")
        pdf.append(sb)
        pdf.append("\nendstream\n")
        pdf.endobj()

        font.toUnicodeCMapObjNumber = pdf.getObjNumber()
    }


    private static func addCIDFontDictionaryObject(
            _ pdf: PDF,
            _ font: Font,
            _ otf: OTF) {
        for f in pdf.fonts {
            if f.cidFontDictObjNumber != 0 && f.name == otf.fontName {
                font.cidFontDictObjNumber = f.cidFontDictObjNumber
                return
            }
        }

        pdf.newobj()
        pdf.append("<<\n")
        pdf.append("/Type /Font\n")
        if otf.cff {
            pdf.append("/Subtype /CIDFontType0\n")
        }
        else {
            pdf.append("/Subtype /CIDFontType2\n")
        }
        pdf.append("/BaseFont /")
        pdf.append(otf.fontName!)
        pdf.append("\n")
        pdf.append("/CIDSystemInfo <</Registry (Adobe) /Ordering (Identity) /Supplement 0>>\n")
        pdf.append("/FontDescriptor ")
        pdf.append(font.fontDescriptorObjNumber)
        pdf.append(" 0 R\n")
        pdf.append("/DW ")
        pdf.append(Int32((Float(1000.0) / Float(otf.unitsPerEm!)) * Float(otf.advanceWidth![0])))
        pdf.append("\n")
        pdf.append("/W [0[\n")
        for i in 0..<otf.advanceWidth!.count {
            pdf.append(Int32((Float(1000.0) / Float(otf.unitsPerEm!)) * Float(otf.advanceWidth![i])))
            pdf.append(" ")
        }
        pdf.append("]]\n")
        pdf.append("/CIDToGIDMap /Identity\n")
        pdf.append(">>\n")
        pdf.endobj()

        font.cidFontDictObjNumber = pdf.getObjNumber()
    }


    private static func toHexString(_ code: Int) -> String {
        let str = String(code, radix: 16)
        if str.unicodeScalars.count == 1 {
            return "000" + str
        }
        else if str.unicodeScalars.count == 2 {
            return "00" + str
        }
        else if str.unicodeScalars.count == 3 {
            return "0" + str
        }
        return str
    }


    private static func writeListToBuffer(
            _ list: inout [String], _ sb: inout String) {
        sb.append(String(list.count))
        sb.append(" beginbfchar\n")
        for str in list {
            sb.append(str)
        }
        sb.append("endbfchar\n")
        list.removeAll()
    }

}   // End of OpenTypeFont.swift
