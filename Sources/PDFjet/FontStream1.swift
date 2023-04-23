/**
 *  FontStream1.swift
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

class FontStream1 {
    enum StreamError: Error {
        case read
        case write
    }

    static func register(
            _ pdf: PDF,
            _ font: Font,
            _ stream: InputStream) throws {
        stream.open()
        try getFontData(font, stream)
        embedFontFile(pdf, font, stream)
        stream.close()
        addFontDescriptorObject(pdf, font)
        addCIDFontDictionaryObject(pdf, font)
        addToUnicodeCMapObject(pdf, font)

        // Type0 Font Dictionary
        pdf.newobj()
        pdf.append("<<\n")
        pdf.append("/Type /Font\n")
        pdf.append("/Subtype /Type0\n")
        pdf.append("/BaseFont /")
        pdf.append(font.name)
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

    private static func embedFontFile(
            _ pdf: PDF, _ font: Font, _ stream: InputStream) {
        // Check if the font file is already embedded
        for f in pdf.fonts {
            if f.fileObjNumber != 0 && f.name == font.name {
                font.fileObjNumber = f.fileObjNumber
                return
            }
        }

        let metadataObjNumber = pdf.addMetadataObject(font.info, true)
        pdf.newobj()
        pdf.append(Token.beginDictionary)
        pdf.append("/Metadata ")
        pdf.append(metadataObjNumber)
        pdf.append(" 0 R\n")
        if font.cff! {
            pdf.append("/Subtype /CIDFontType0C\n")
        }
        pdf.append("/Filter /FlateDecode\n")
        pdf.append("/Length ")
        pdf.append(font.compressedSize!)
        pdf.append("\n")

        if !font.cff! {
            pdf.append("/Length1 ")
            pdf.append(font.uncompressedSize!)
            pdf.append(Token.newline)
        }

        pdf.append(Token.endDictionary)
        pdf.append(Token.stream)
        var buffer = [UInt8](repeating: 0, count: 4096)
        while stream.hasBytesAvailable {
            let count = stream.read(&buffer, maxLength: buffer.count)
            if count > 0 {
                pdf.append(buffer, 0, count)
            }
        }
        pdf.append(Token.endstream)
        pdf.endobj()

        font.fileObjNumber = pdf.getObjNumber()
    }

    private static func addFontDescriptorObject(_ pdf: PDF, _ font: Font) {
        for f in pdf.fonts {
            if f.fontDescriptorObjNumber != 0 && f.name == font.name {
                font.fontDescriptorObjNumber = f.fontDescriptorObjNumber
                return
            }
        }

        pdf.newobj()
        pdf.append("<<\n")
        pdf.append("/Type /FontDescriptor\n")
        pdf.append("/FontName /")
        pdf.append(font.name)
        pdf.append("\n")
        if font.cff! {
            pdf.append("/FontFile3 ")
        }
        else {
            pdf.append("/FontFile2 ")
        }
        pdf.append(font.fileObjNumber)
        pdf.append(" 0 R\n")
        pdf.append("/Flags 32\n")
        pdf.append("/FontBBox [")
        pdf.append(Int32(font.bBoxLLx))
        pdf.append(" ")
        pdf.append(Int32(font.bBoxLLy))
        pdf.append(" ")
        pdf.append(Int32(font.bBoxURx))
        pdf.append(" ")
        pdf.append(Int32(font.bBoxURy))
        pdf.append("]\n")
        pdf.append("/Ascent ")
        pdf.append(Int32(font.fontAscent))
        pdf.append("\n")
        pdf.append("/Descent ")
        pdf.append(Int32(font.fontDescent))
        pdf.append("\n")
        pdf.append("/ItalicAngle 0\n")
        pdf.append("/CapHeight ")
        pdf.append(Int32(font.capHeight))
        pdf.append("\n")
        pdf.append("/StemV 79\n")
        pdf.append(">>\n")
        pdf.endobj()

        font.fontDescriptorObjNumber = pdf.getObjNumber()
    }

    private static func addToUnicodeCMapObject(_ pdf: PDF, _ font: Font) {
        for f in pdf.fonts {
            if f.toUnicodeCMapObjNumber != 0 && f.name == font.name {
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

        var list = Array<String>()
        var buf = String()
        for cid in 0...0xffff {
            let gid = font.unicodeToGID![cid]
            if gid > 0 {
                buf.append("<")
                buf.append(toHexString(Int32(gid)))
                buf.append("> <")
                buf.append(toHexString(Int32(cid)))
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

    private static func addCIDFontDictionaryObject(_ pdf: PDF, _ font: Font) {
        for f in pdf.fonts {
            if f.cidFontDictObjNumber != 0 && f.name == font.name {
                font.cidFontDictObjNumber = f.cidFontDictObjNumber
                return
            }
        }

        pdf.newobj()
        pdf.append(Token.beginDictionary)
        pdf.append("/Type /Font\n")
        if font.cff! {
            pdf.append("/Subtype /CIDFontType0\n")
        } else {
            pdf.append("/Subtype /CIDFontType2\n")
        }
        pdf.append("/BaseFont /")
        pdf.append(font.name)
        pdf.append(Token.newline)
        pdf.append("/CIDSystemInfo <</Registry (Adobe) /Ordering (Identity) /Supplement 0>>\n")
        pdf.append("/FontDescriptor ")
        pdf.append(font.fontDescriptorObjNumber)
        pdf.append(" 0 R\n")

        var k: Float = 1.0
        if font.unitsPerEm != 1000 {
            k = Float(1000.0) / Float(font.unitsPerEm)
        }
        pdf.append("/DW ")
        pdf.append(Int32(round(k * Float(font.advanceWidth![0]))))
        pdf.append(Token.newline)
        var buffer = String("")
        pdf.append("/W [0[\n")
        for i in 0..<font.advanceWidth!.count {
            buffer.append(String(UInt16(round(k * Float(font.advanceWidth![i])))))
            buffer.append(" ")
        }
        pdf.append(buffer)
        pdf.append("]]\n")

        pdf.append("/CIDToGIDMap /Identity\n")
        pdf.append(Token.endDictionary)
        pdf.endobj()

        font.cidFontDictObjNumber = pdf.getObjNumber()
    }

    private static func toHexString(_ code: Int32) -> String {
        let str = String(code, radix: 16)
        if str.count == 1 {
            return "000" + str
        } else if str.count == 2 {
            return "00" + str
        } else if str.count == 3 {
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

    private static func getUInt16(_ stream: InputStream) throws -> UInt16 {
        var buffer = [UInt8](repeating: 0, count: 2)
        if stream.read(&buffer, maxLength: 2) == 2 {
            return (UInt16(buffer[0]) << 8) | UInt16(buffer[1])
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

    private static func getInt24(_ stream: InputStream) throws -> Int {
        var buffer = [UInt8](repeating: 0, count: 3)
        if stream.read(&buffer, maxLength: 3) == 3 {
            return (Int(buffer[0]) << 16) | (Int(buffer[1]) << 8) | Int(buffer[2])
        }
        throw StreamError.read
    }

    private static func getInt32(_ stream: InputStream) throws -> Int32 {
        var buffer = [UInt8](repeating: 0, count: 4)
        if stream.read(&buffer, maxLength: 4) == 4 {
            return (Int32(buffer[0]) << 24) | (Int32(buffer[1]) << 16) |
                    (Int32(buffer[2]) << 8) | Int32(buffer[3])
        }
        throw StreamError.read
    }

    private static func getUInt16(
            _ buffer: [UInt8], _ offset: inout Int) -> UInt16 {
        let value = (UInt16(buffer[offset]) << 8) | UInt16(buffer[offset + 1])
        offset += 2
        return value
    }

    private static func getInt(
            _ buffer: [UInt8], _ offset: inout Int) -> Int {
        let value = (Int(buffer[offset]) << 8) | Int(buffer[offset + 1])
        offset += 2
        return value
    }

    private static func getInt32(
            _ buffer: [UInt8], _ offset: inout Int) -> Int32 {
        let value = (Int32(buffer[offset]) << 24) | (Int32(buffer[offset + 1]) << 16) |
                (Int32(buffer[offset + 2]) << 8) | Int32(buffer[offset + 3])
        offset += 4
        return value
    }

    static func getFontData(_ font: Font, _ stream: InputStream) throws {
        var len = try getInt8(stream)
        var fontName = [UInt8](repeating: 0, count: len)
        if stream.read(&fontName, maxLength: len) == len {
            font.name = String(bytes: fontName, encoding: .utf8)!
        }

        len = try getInt24(stream)
        var fontInfo = [UInt8](repeating: 0, count: len)
        if stream.read(&fontInfo, maxLength: len) == len {
            font.info = String(bytes: fontInfo, encoding: .utf8)!
        }

        let deflatedLength = Int(try getInt32(stream))
        var deflated = [UInt8](repeating: 0, count: deflatedLength)
        if stream.read(&deflated, maxLength: deflatedLength) == deflatedLength {
        }

        var inflated = [UInt8]()
        _ = try Puff(output: &inflated, input: &deflated)

        var offset = 0
        font.unitsPerEm = Int(getInt32(inflated, &offset))
        font.bBoxLLx = Int16(getInt32(inflated, &offset))
        font.bBoxLLy = Int16(getInt32(inflated, &offset))
        font.bBoxURx = Int16(getInt32(inflated, &offset))
        font.bBoxURy = Int16(getInt32(inflated, &offset))
        font.fontAscent = Int16(getInt32(inflated, &offset))
        font.fontDescent = Int16(getInt32(inflated, &offset))
        font.firstChar = Int(getInt32(inflated, &offset))
        font.lastChar = Int(getInt32(inflated, &offset))
        font.capHeight = Int16(getInt32(inflated, &offset))
        font.fontUnderlinePosition = Int16(getInt32(inflated, &offset))
        font.fontUnderlineThickness = Int16(getInt32(inflated, &offset))

        len = Int(getInt32(inflated, &offset))
        font.advanceWidth = [UInt16](repeating: 0, count: len)
        for i in 0..<len {
            font.advanceWidth![i] = getUInt16(inflated, &offset)
        }

        len = Int(getInt32(inflated, &offset))
        font.glyphWidth = [Int](repeating: 0, count: len)
        for i in 0..<len {
            font.glyphWidth![i] = getInt(inflated, &offset)
        }

        len = Int(getInt32(inflated, &offset))
        font.unicodeToGID = [Int](repeating: 0, count: len)
        for i in 0..<len {
            font.unicodeToGID![i] = getInt(inflated, &offset)
        }

        font.cff = false
        if String(try getInt8(stream)) == "Y" {
            font.cff = true
        }

        font.uncompressedSize = Int(try getInt32(stream))
        font.compressedSize = Int(try getInt32(stream))
    }
}   // End of FontStream1.swift
