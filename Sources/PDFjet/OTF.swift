/**
 *  OTF.swift
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

struct FontTable {
    var name: String?
    var checkSum: UInt32?
    var offset: Int?
    var length: Int?
}

class OTF {

    var fontName: String?
    var fontInfo: String?
    var unitsPerEm: Int?
    var bBoxLLx: Int16?
    var bBoxLLy: Int16?
    var bBoxURx: Int16?
    var bBoxURy: Int16?
    var ascent: Int16?
    var descent: Int16?
    var advanceWidth: [UInt16]?
    var firstChar: Int?
    var lastChar: Int?
    var capHeight: Int16?
    var glyphWidth: [Int]?
    var postVersion: UInt32?
    var italicAngle: UInt32?
    var underlinePosition: Int16?
    var underlineThickness: Int16?

    var buf = [UInt8]()
    var dos = [UInt8]()
    var cff = false
    private var cffOff: Int?
    private var cffLen: Int?
    private var index = 0

    var unicodeToGID = [Int](repeating: 0, count: 0x10000)

    init(_ stream: InputStream) throws {
        buf = try Contents.getFromStream(stream)

        // Extract OTF metadata
        let version = readUInt32()

        if version == 0x00010000 ||     // Win OTF
            version == 0x74727565 ||    // Mac TTF
            version == 0x4F54544F {     // CFF OTF
            // We should be able to read this font
        }
        else {
            Swift.print("OTF version == \(version) is not supported.")
        }

        let numOfTables = readUInt16()      // numOfTables
        readUInt16()                        // searchRange
        readUInt16()                        // entrySelector
        readUInt16()                        // rangeShift

        var cmapTable: FontTable?
        for _ in 0..<numOfTables {
            var name = [UInt8](repeating: 0, count: 4)
            for i in 0..<4 {
                name[i] = readByte()
            }
            var table = FontTable()
            table.name = String(bytes: name, encoding: .utf8)
            table.checkSum = readUInt32()
            table.offset = Int(readUInt32())
            table.length = Int(readUInt32())

            let k = index   // Save the current index
            if      table.name == "head" { head(table) }
            else if table.name == "hhea" { hhea(table) }
            else if table.name == "OS/2" { OS_2(table) }
            else if table.name == "name" { n4me(table) }
            else if table.name == "hmtx" { hmtx(table) }
            else if table.name == "post" { post(table) }
            else if table.name == "CFF " { CFF_(table) }
            else if table.name == "cmap" { cmapTable = table }
            index = k       // Restore the index
        }

        // This table must be processed last
        cmap(cmapTable!)

        if cff {
            let bufSlice = Array(buf[cffOff!..<(cffOff! + cffLen!)])
            // LZWEncode(&dos, bufSlice)
            ZLibEncode(&dos, bufSlice)
        }
        else {
            // LZWEncode(&dos, buf)
            ZLibEncode(&dos, buf)
        }
    }

    private func head(_ table: FontTable) {
        self.index = table.offset! + 16
        readUInt16()                    // flags
        unitsPerEm = Int(readUInt16())
        self.index += 16
        self.bBoxLLx = readInt16()
        self.bBoxLLy = readInt16()
        self.bBoxURx = readInt16()
        self.bBoxURy = readInt16()
    }

    private func hhea(_ table: FontTable) {
        self.index = table.offset! + 4
        self.ascent  = readInt16()
        self.descent = readInt16()
        self.index += 26
        self.advanceWidth = [UInt16](repeating: 0, count: Int(readUInt16()))
    }

    private func OS_2(_ table: FontTable) {
        index = table.offset! + 64
        firstChar = Int(readUInt16())
        lastChar  = Int(readUInt16())
        index += 20
        capHeight = readInt16()
    }

    private func n4me(_ table: FontTable) {
        self.index = table.offset!
        readUInt16()                // format
        let count = readUInt16()
        let stringOffset = readUInt16()

        var macFontInfo = ""
        var winFontInfo = ""
        for _ in 0..<count {
            let platformID = readUInt16()
            let encodingID = readUInt16()
            let languageID = readUInt16()
            let nameID = readUInt16()
            let length = readUInt16()
            let offset = readUInt16()

            if platformID == 1 && encodingID == 0 && languageID == 0 {
                // Macintosh
                let index2 = Int(table.offset!) + Int(stringOffset) + Int(offset)
                let buffer = buf[index2..<(index2 + Int(length))]
                let str = String(bytes: buffer, encoding: .utf8)
                if nameID == 6 {
                    fontName = str
                }
                else {
                    macFontInfo.append(str!)
                    macFontInfo.append("\n")
                }
            }
            else if platformID == 3 && encodingID == 1 && languageID == 0x409 {
                // Windows
                let index2 = Int(table.offset!) + Int(stringOffset) + Int(offset)
                let buffer = buf[index2..<(index2 + Int(length))]
                let str = String(bytes: buffer, encoding: .utf16)
                if nameID == 6 {
                    fontName = str
                }
                else {
                    winFontInfo.append(str!)
                    winFontInfo.append("\n")
                }
            }
        }
        fontInfo = winFontInfo != "" ? winFontInfo : macFontInfo
    }

    private func cmap(_ table: FontTable) {
        self.index = table.offset!
        let tableOffset = index
        index += 2
        let numRecords = readUInt16()

        // Process the encoding records
        var format4subtable = false
        var subtableOffset = 0
        for _ in 0..<numRecords {
            let platformID = readUInt16()
            let encodingID = readUInt16()
            subtableOffset = Int(readUInt32())
            if platformID == 3 && encodingID == 1 {
                format4subtable = true
                break
            }
        }
        if !format4subtable {
            // TODO:
            Swift.print("Format 4 subtable not found in this font.")
        }

        self.index = tableOffset + subtableOffset

        readUInt16()        // format
        let tableLen = readUInt16()
        readUInt16()        // language
        let segCount = Int(readUInt16() / 2)

        index += 6          // Skip to the endCount[]
        var endCount = [Int](repeating: 0, count: Int(segCount))
        var i = 0
        while i < segCount {
            endCount[i] = Int(readUInt16())
            i += 1
        }

        index += 2          // Skip the reservedPad
        var startCount = [Int](repeating: 0, count: Int(segCount))
        i = 0
        while i < segCount {
            startCount[i] = Int(readUInt16())
            i += 1
        }

        var idDelta = [Int](repeating: 0, count: Int(segCount))
        i = 0
        while i < segCount {
            idDelta[i] = Int(readUInt16())
            i += 1
        }

        var idRangeOffset = [Int](repeating: 0, count: Int(segCount))
        i = 0
        while i < segCount {
            idRangeOffset[i] = Int(readUInt16())
            i += 1
        }

        var glyphIdArray = [Int](repeating: 0, count: Int((Int(tableLen) - Int(16 + 8*segCount)) / 2))
        i = 0
        while i < glyphIdArray.count {
            glyphIdArray[i] = Int(readUInt16())
            i += 1
        }

        let width = Int(advanceWidth![0])
        glyphWidth = [Int](repeating: width, count: Int(lastChar! + 1))

        for ch in firstChar!...lastChar! {
            let seg = getSegmentFor(ch, startCount, endCount, Int(segCount))
            if seg != -1 {
                var gid = 0
                var offset = idRangeOffset[seg]
                if offset == 0 {
                    gid = (idDelta[seg] + ch) % 65536
                }
                else {
                    offset /= 2
                    offset -= segCount - seg
                    gid = glyphIdArray[offset + (ch - startCount[seg])]
                    if gid != 0 {
                        gid += idDelta[seg] % 65536
                    }
                }

                if gid < advanceWidth!.count {
                    glyphWidth![ch] = Int(advanceWidth![gid])
                }

                unicodeToGID[ch] = gid
            }
        }
    }

    private func hmtx(_ table: FontTable) {
        self.index = table.offset!
        for i in 0..<advanceWidth!.count {
            advanceWidth![i] = readUInt16()
            index += 2
        }
    }

    private func post(_ table: FontTable) {
        self.index = table.offset!
        self.postVersion = readUInt32()
        self.italicAngle = readUInt32()
        self.underlinePosition  = readInt16()
        self.underlineThickness = readInt16()
    }

    private func CFF_(_ table: FontTable) {
        self.cff = true
        self.cffOff = table.offset!
        self.cffLen = table.length!
    }

    private func getSegmentFor(
            _ ch: Int,
            _ startCount: [Int],
            _ endCount: [Int],
            _ segCount: Int) -> Int {
        var segment = -1
        for i in 0..<segCount {
            if ch <= endCount[i] && ch >= startCount[i] {
                segment = i
                break
            }
        }
        return segment
    }

    private func readInt16() -> Int16 {
        var val = Int16(buf[index]) << 8
        index += 1
        val |= Int16(buf[index])
        index += 1
        return val
    }

    private func readByte() -> UInt8 {
        let val = buf[index]
        index += 1
        return val
    }

    @discardableResult
    private func readUInt16() -> UInt16 {
        var val = UInt16(buf[index]) << 8
        index += 1
        val |= UInt16(buf[index])
        index += 1
        return val
    }

    @discardableResult
    private func readUInt32() -> UInt32 {
        var val = UInt32(buf[index]) << 24
        index += 1
        val |= UInt32(buf[index]) << 16
        index += 1
        val |= UInt32(buf[index]) << 8
        index += 1
        val |= UInt32(buf[index])
        index += 1
        return val
    }

}   // End of OTF.swift
