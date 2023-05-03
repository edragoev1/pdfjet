/**
 *  FontStream2.swift
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
import Foundation

class FontStream2 {
    enum StreamError: Error {
        case read
        case write
    }

    static func register(
            _ objects: inout [PDFobj],
            _ font: Font,
            _ stream: InputStream) throws {
        stream.open()
        try FontStream1.getFontData(font, stream)
        embedFontFile(&objects, font, stream)
        stream.close()

        addFontDescriptorObject(&objects, font)
        addCIDFontDictionaryObject(&objects, font)
        addToUnicodeCMapObject(&objects, font)

        // Type0 Font Dictionary
        let obj = PDFobj()
        obj.dict.append("<<")
        obj.dict.append("/Type")
        obj.dict.append("/Font")
        obj.dict.append("/Subtype")
        obj.dict.append("/Type0")
        obj.dict.append("/BaseFont")
        obj.dict.append("/" + font.name)
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
        obj.number = objects.count + 1
        objects.append(obj)

        font.objNumber = obj.number
    }

    private static func addMetadataObject(
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
        sb.append(String(font.info.utf8))
        sb.append("</rdf:li>\n")
        sb.append("</rdf:Alt>\n")
        sb.append("</xmpRights:UsageTerms>\n")
        sb.append("</rdf:Description>\n")
        sb.append("</rdf:RDF>\n")
        sb.append("</x:xmpmeta>\n")
        sb.append("<?xpacket end=\"w\"?>")

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


    private static func embedFontFile(
            _ objects: inout [PDFobj],
            _ font: Font,
            _ stream: InputStream) {
        let metadataObjNumber = addMetadataObject(&objects, font)

        let obj = PDFobj()
        obj.dict.append("<<")
        obj.dict.append("/Metadata")
        obj.dict.append(String(metadataObjNumber))
        obj.dict.append("0")
        obj.dict.append("R")
        obj.dict.append("/Filter")
        obj.dict.append("/FlateDecode")
        obj.dict.append("/Length")
        obj.dict.append(String(font.compressedSize!))
        if font.cff {
            obj.dict.append("/Subtype")
            obj.dict.append("/CIDFontType0C")
        } else {
            obj.dict.append("/Length1")
            obj.dict.append(String(font.uncompressedSize!))
        }
        obj.dict.append(">>")
        var buffer2 = [UInt8]()
        var buffer1 = [UInt8](repeating: 0, count: 4096)
        while stream.hasBytesAvailable {
            let count = stream.read(&buffer1, maxLength: buffer1.count)
            if count > 0 {
                buffer2.append(contentsOf: buffer1[0..<count])
            }
        }
        obj.setStream(&buffer2)
        obj.number = objects.count + 1
        objects.append(obj)

        font.fileObjNumber = obj.number
    }

    private static func addFontDescriptorObject(_ objects: inout [PDFobj], _ font: Font) {
        let obj = PDFobj()
        obj.dict.append("<<")
        obj.dict.append("/Type")
        obj.dict.append("/FontDescriptor")
        obj.dict.append("/FontName")
        obj.dict.append("/" + font.name)
        obj.dict.append("/FontFile" + (font.cff ? "3" : "2"))
        obj.dict.append(String(font.fileObjNumber))
        obj.dict.append("0")
        obj.dict.append("R")
        obj.dict.append("/Flags")
        obj.dict.append("32")
        obj.dict.append("/FontBBox")
        obj.dict.append("[")
        obj.dict.append(String(font.bBoxLLx))
        obj.dict.append(String(font.bBoxLLy))
        obj.dict.append(String(font.bBoxURx))
        obj.dict.append(String(font.bBoxURy))
        obj.dict.append("]")
        obj.dict.append("/Ascent")
        obj.dict.append(String(font.fontAscent))
        obj.dict.append("/Descent")
        obj.dict.append(String(font.fontDescent))
        obj.dict.append("/ItalicAngle")
        obj.dict.append("0")
        obj.dict.append("/CapHeight")
        obj.dict.append(String(font.capHeight))
        obj.dict.append("/StemV")
        obj.dict.append("79")
        obj.dict.append(">>")
        obj.number = objects.count + 1
        objects.append(obj)

        font.fontDescriptorObjNumber = obj.number
    }

    private static func addToUnicodeCMapObject(
            _ objects: inout [PDFobj],
            _ font: Font) {
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
            let gid = font.unicodeToGID![cid]
            if gid > 0 {
                buf.append("<")
                buf.append(toHexString(gid))
                buf.append("> <")
                buf.append(toHexString(Int(cid)))
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

        var stream = Array(sb.utf8)

        let obj = PDFobj()
        obj.dict.append("<<")
        obj.dict.append("/Length")
        obj.dict.append(String(sb.count))
        obj.dict.append(">>")
        obj.setStream(&stream)
        obj.number = objects.count + 1
        objects.append(obj)

        font.toUnicodeCMapObjNumber = obj.number
    }


    private static func addCIDFontDictionaryObject(
            _ objects: inout [PDFobj],
            _ font: Font) {
        let obj = PDFobj()
        obj.dict.append("<<")
        obj.dict.append("/Type")
        obj.dict.append("/Font")
        obj.dict.append("/Subtype")
        obj.dict.append("/CIDFontType" + (font.cff ? "0" : "2"))
        obj.dict.append("/BaseFont")
        obj.dict.append("/" + font.name)
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
        obj.dict.append("/DW")
        obj.dict.append(String(round(k * Float(font.advanceWidth![0]))))
        obj.dict.append("/W")
        obj.dict.append("[")
        obj.dict.append("0")
        obj.dict.append("[")
        for i in 0..<font.advanceWidth!.count {
            obj.dict.append(String(round(k * Float(font.advanceWidth![i]))))
        }
        obj.dict.append("]")
        obj.dict.append("]")

        obj.dict.append("/CIDToGIDMap")
        obj.dict.append("/Identity")
        obj.dict.append(">>")
        obj.number = objects.count + 1
        objects.append(obj)

        font.cidFontDictObjNumber = obj.number
    }

    private static func toHexString(_ code: Int) -> String {
        let str = String(code, radix: 16)
        if str.unicodeScalars.count == 1 {
            return "000" + str
        } else if str.unicodeScalars.count == 2 {
            return "00" + str
        } else if str.unicodeScalars.count == 3 {
            return "0" + str
        }
        return str
    }

    private static func writeListToBuffer(
            _ list: inout [String],
            _ sb: inout String) {
        sb.append(String(list.count))
        sb.append(" beginbfchar\n")
        for str in list {
            sb.append(str)
        }
        sb.append("endbfchar\n")
        list.removeAll()
    }

    private static func getUInt16(_ stream: InputStream) throws -> UInt16 {
        var buffer = [UInt8](repeating: 0, count: 2)
        if stream.read(&buffer, maxLength: 2) == 2 {
            var value = UInt16(buffer[0]) << 8
            value |= UInt16(buffer[1])
            return value
        }
        throw StreamError.read
    }

    private static func getInt8(_ stream: InputStream) throws -> Int {
        var buffer = [UInt8](repeating: 0, count: 1)
        if stream.read(&buffer, maxLength: 1) == 1 {
            return Int(buffer[0])
        }
        throw StreamError.read
    }
}   // End of FontStream2.swift
