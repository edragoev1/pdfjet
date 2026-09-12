/**
 * PDFobj.swift
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
import Foundation

///
/// Used to create Java or .NET objects that represent the objects in PDF document.
/// See the PDF specification for more information.
///
public final class PDFobj {
    var number = 0                  // The object number
    var offset = 0                  // The object offset
    final var dict = [String]()
    var streamOffset = 0
    var stream: [UInt8]?            // The compressed stream
    final var data = [UInt8]()      // The decompressed data
    var gsNumber = -1

    /// Creates an empty PDF object.
    init() {

    }

    /// Returns the object number.
    ///
    /// - Returns: the object number.
    public final func getNumber() -> Int {
        return self.number
    }

    ///
    /// Returns the object dictionary.
    ///
    /// - Returns: the object dictionary.
    ///
    public final func getDict() -> [String] {
        return self.dict
    }

    ///
    /// Returns the uncompressed stream data.
    ///
    /// - Returns: the uncompressed stream data.
    ///
    public final func getData() -> [UInt8] {
        return self.data
    }

    /// Sets the decompressed data.
    final func setData(_ data: inout [UInt8]) {
        self.data = data
    }

    /// Returns the compressed stream.
    final func getStream() -> [UInt8]? {
        return self.stream
    }

    ///
    /// Copies the stream from the buffer, decrypts it when the PDF is encrypted,
    /// and decodes it with the filters of its /Filter entry. The decrypted
    /// stream replaces the encrypted one, so that it can be copied.
    ///
    final func setStreamAndData(
            _ buffer: inout [UInt8], _ length: Int, _ decryptor: Decryptor? = nil) throws {
        if stream == nil {
            var copied = Array(buffer[streamOffset..<streamOffset + length])
            if let decryptor = decryptor {
                copied = decryptor.decryptStream(self, copied)
                setLength(copied.count)
            }
            stream = copied
            data = try decode(copied)
        }
    }

    // Sets the /Length of the stream, replacing a reference to the length.
    private final func setLength(_ length: Int) {
        guard let i = dict.firstIndex(of: "/Length"), i + 1 < dict.count else {
            return
        }
        if i + 3 < dict.count && dict[i + 3] == "R" {
            dict.removeSubrange((i + 2)...(i + 3))
        }
        dict[i + 1] = String(length)
    }

    // Decodes the stream with each filter of its /Filter entry in turn. A
    // filter that is not supported, like DCTDecode, ends the decoding, and the
    // data is what the filters before it decoded.
    private final func decode(_ stream: [UInt8]) throws -> [UInt8] {
        var decoded = stream
        for (i, filter) in getValues("/Filter").enumerated() {
            switch filter {
            case "/FlateDecode", "/Fl":
                var input = decoded
                var output = [UInt8]()
                _ = try Puff(output: &output, input: &input)
                decoded = applyDecodeParms(output, i)
            case "/LZWDecode", "/LZW":
                decoded = applyDecodeParms(lzwDecode(decoded), i)
            case "/ASCIIHexDecode", "/AHx":
                decoded = asciiHexDecode(decoded)
            case "/ASCII85Decode", "/A85":
                decoded = ascii85Decode(decoded)
            case "/RunLengthDecode", "/RL":
                decoded = runLengthDecode(decoded)
            default:
                return decoded
            }
        }
        return decoded
    }

    // Returns the elements of the array that is the value of the key, or the
    // value itself when it is not an array.
    private final func getValues(_ key: String) -> [String] {
        guard let k = dict.firstIndex(of: key), k + 1 < dict.count else {
            return []
        }
        if dict[k + 1] != "[" {
            return [dict[k + 1]]
        }
        var values = [String]()
        var i = k + 2
        while i < dict.count && dict[i] != "]" {
            values.append(dict[i])
            i += 1
        }
        return values
    }

    // Undoes the predictor in the parameters of the filter at the index.
    // Images keep it, as they are copied with their stream, and their data is
    // not used.
    private final func applyDecodeParms(_ decoded: [UInt8], _ index: Int) -> [UInt8] {
        if getValue("/Subtype") == "/Image" {
            return decoded
        }
        let parms = getDecodeParms(index)
        return applyPredictor(
                decoded,
                getDecodeParm(parms, "/Predictor", 1),
                getDecodeParm(parms, "/Colors", 1),
                getDecodeParm(parms, "/BitsPerComponent", 8),
                getDecodeParm(parms, "/Columns", 1))
    }

    // Returns the tokens of the parameters dictionary of the filter at the
    // index. /DecodeParms is a dictionary for a single filter, or an array with
    // a dictionary or null for each filter.
    private final func getDecodeParms(_ index: Int) -> [String] {
        guard let k = dict.firstIndex(of: "/DecodeParms"), k + 1 < dict.count else {
            return []
        }
        var i = k + 1
        let array = dict[i] == "["
        if array {
            i += 1
        }
        var element = 0
        while i < dict.count && dict[i] != "]" {
            let start = i
            if dict[i] == "<<" {
                var level = 0
                repeat {
                    let token = dict[i]
                    i += 1
                    if token == "<<" {
                        level += 1
                    } else if token == ">>" {
                        level -= 1
                    }
                } while level > 0 && i < dict.count
            } else if i + 2 < dict.count && dict[i + 2] == "R" {
                i += 3      // A reference to a dictionary, which is not supported.
            } else {
                i += 1      // null
            }
            if element == index {
                return (dict[start] == "<<") ? Array(dict[start..<i]) : []
            }
            if !array {
                break
            }
            element += 1
        }
        return []
    }

    // Returns the integer value of the key in the parameters dictionary. A
    // value that is not a 32-bit integer gets the default, as in the Java and
    // C# ports.
    private final func getDecodeParm(_ parms: [String], _ key: String, _ defaultValue: Int) -> Int {
        if let i = parms.firstIndex(of: key), i + 1 < parms.count, let value = Int32(parms[i + 1]) {
            return Int(value)
        }
        return defaultValue
    }

    /// Sets the stream.
    final func setStream(_ stream: inout [UInt8]) {
        self.stream = stream
    }

    final func setNumber(_ number: Int) {
        self.number = number
    }

    ///
    /// Returns the dictionary value for the specified key.
    ///
    /// - Parameter key: the specified key.
    ///
    /// - Returns: the value.
    ///
    public final func getValue(_ key: String) -> String {
        var i = 0
        while i < dict.count {
            if key == dict[i] {
                let token = dict[i + 1]
                if token == "<<" {
                    var buffer = String()
                    buffer.append("<< ")
                    i += 2
                    while dict[i] != ">>" {
                        buffer.append(dict[i])
                        buffer.append(" ")
                        i += 1
                    }
                    buffer.append(">>")
                    return buffer
                } else if token == "[" {
                    var buffer = String()
                    buffer.append("[ ")
                    i += 2
                    while dict[i] != "]" {
                        buffer.append(dict[i])
                        buffer.append(" ")
                        i += 1
                    }
                    buffer.append("]")
                    return buffer
                } else {
                    return token
                }
            }
            i += 1
        }
        return ""
    }

    final func getObjectNumbers(_ key: String) -> [Int] {
        var numbers = [Int]()
        var i = 0
        while i < dict.count {
            let token = dict[i]
            if token == key {
                i += 1
                if dict[i] == "[" {
                    while true {
                        i += 1
                        if dict[i] == "]" {
                            break
                        }
                        numbers.append(Int(dict[i])!)
                        i += 1  // 0
                        i += 1  // R
                    }
                } else {
                    numbers.append(Int(dict[i])!)
                }

                break
            }
            i += 1
        }
        return numbers
    }

    /// Returns the width and height from the /MediaBox of this page.
    ///
    /// - Returns: the page size.
    public final func getPageSize() -> [Float] {
        for i in 0..<dict.count {
            if dict[i] == "/MediaBox" {
                return [Float(dict[i + 4])!, Float(dict[i + 5])!]
            }
        }
        return Letter.PORTRAIT
    }

    final func getLength(_ objects: inout [PDFobj]) -> Int? {
        for i in 0..<dict.count {
            if dict[i] == "/Length" {
                let number = Int(dict[i + 1])!
                if dict[i + 2] == "0" &&
                        dict[i + 3] == "R" {
                    return getLength(number, from: &objects)
                } else {
                    return number
                }
            }
        }
        return nil
    }

    private final func getLength(
            _ number: Int,
            from objects: inout [PDFobj]) -> Int? {
        return Int(objects[number - 1].dict[3])
    }

    private final func getObject(
            number: Int,
            from objects: inout [PDFobj]) -> PDFobj? {
        return objects[number - 1]
    }

    ///
    /// Returns the content object of this page.
    /// The content of a page can be split into several streams, listed in an
    /// array that is either in the page dictionary or an object of its own.
    /// Together they are one content stream, so they are returned joined in
    /// a new object, which is not in the objects list.
    ///
    /// - Returns: the content object, or nil if the page has no contents.
    ///
    public final func getContentObject(_ objects: inout [PDFobj]) -> PDFobj? {
        var numbers = getObjectNumbers("/Contents")
        if numbers.count == 1 {
            let object = objects[numbers[0] - 1]
            if object.stream != nil {
                return object
            }
            // "/Contents 39 0 R" where the object is "[ 41 0 R 43 0 R ]"
            numbers = [Int]()
            var i = object.dict.firstIndex(of: "[") ?? -1
            while i != -1 && i + 3 < object.dict.count && object.dict[i + 3] == "R" {
                numbers.append(Int(object.dict[i + 1])!)
                i += 3
            }
        }
        if numbers.isEmpty {
            return nil
        }
        if numbers.count == 1 {
            return objects[numbers[0] - 1]
        }
        let content = PDFobj()
        for number in numbers {
            content.data.append(contentsOf: objects[number - 1].data)
            content.data.append(UInt8(ascii: "\n"))     // A stream can end in the middle of a line.
        }
        return content
    }

    /// Returns the resources object of this page.
    ///
    /// - Parameter objects: the objects of the PDF.
    /// - Returns: the resources object, or nil if the page has none.
    public final func getResourcesObject(_ objects: inout [PDFobj]) -> PDFobj? {
        var i = 0
        while i < dict.count {
            if dict[i] == "/Resources" {
                let token = dict[i + 1]
                if token == "<<" {
                    return self
                }
                return getObject(number: Int(token)!, from: &objects)
            }
            i += 1
        }
        return nil
    }

    /// Adds a core font to the resources of this page.
    ///
    /// - Parameter coreFont: the core font.
    /// - Parameter objects: the objects of the PDF.
    /// - Returns: the font.
    public final func addResource(
            _ coreFont: Int,
            _ objects: inout [PDFobj]) -> Font {
        let font = Font(coreFont)
        font.fontID = font.name.replacingOccurrences(of: "-", with: "_").uppercased()

        let obj = PDFobj()
        obj.number = objects.last!.number + 1
        obj.dict.append("<<")
        obj.dict.append("/Type")
        obj.dict.append("/Font")
        obj.dict.append("/Subtype")
        obj.dict.append("/Type1")
        obj.dict.append("/BaseFont")
        obj.dict.append("/" + font.name)
        if font.name != "Symbol" && font.name != "ZapfDingbats" {
            obj.dict.append("/Encoding")
            obj.dict.append("/WinAnsiEncoding")
        }
        obj.dict.append(">>")
        objects.append(obj)

        var i = 0
        while i < dict.count {
            if dict[i] == "/Resources" {
                i += 1
                let token = dict[i]
                if token == "<<" {                  // Direct resources object
                    addFontResource(self, &objects, font.fontID!, obj.number)
                } else if firstCharIsDigit(token) {   // Indirect resources object
                    let object = getObject(number: Int(token)!, from: &objects)!
                    addFontResource(object, &objects, font.fontID!, obj.number)
                }
            }
            i += 1
        }

        return font
    }

    private final func addFontResource(
            _ obj: PDFobj,
            _ objects: inout [PDFobj],
            _ fontID: String,
            _ number: Int) {
        var fonts: Bool = false
        var i = 0
        while i < obj.dict.count {
            if obj.dict[i] == "/Font" {
                fonts = true
                break
            }
            i += 1
        }
        if !fonts {
            i = 0
            while i < obj.dict.count {
                if obj.dict[i] == "/Resources" {
                    obj.dict.insert("/Font", at: i + 2)
                    obj.dict.insert("<<", at: i + 3)
                    obj.dict.insert(">>", at: i + 4)
                    break
                }
                i += 1
            }
        }

        i = 0
        while i < obj.dict.count {
            if obj.dict[i] == "/Font" {
                let token = obj.dict[i + 1]
                if token == "<<" {
                    obj.dict.insert("/" + fontID, at: i + 2)
                    obj.dict.insert(String(number), at: i + 3)
                    obj.dict.insert("0", at: i + 4)
                    obj.dict.insert("R", at: i + 5)
                    return
                } else if firstCharIsDigit(token) {
                    let o2 = getObject(number: Int(token)!, from: &objects)!
                    var j = 0
                    while j < o2.dict.count {
                        if o2.dict[j] == "<<" {
                            o2.dict.insert("/" + fontID, at: j + 1)
                            o2.dict.insert(String(number), at: j + 2)
                            o2.dict.insert("0", at: j + 3)
                            o2.dict.insert("R", at: j + 4)
                            return
                        }
                        j += 1
                    }
                }
            }
            i += 1
        }
    }

    private final func insertNewObject(
            _ dict: inout [String],
            _ list: inout [String],
            _ type: String) {
        for i in 0..<dict.count {
            if dict[i] == type {
                dict.insert(contentsOf: list, at: i + 2)
                return
            }
        }
        if dict[3] == "<<" {
            dict.insert(contentsOf: list, at: 4)
            return
        }
    }

    private final func addResource(
            _ type: String,
            _ obj: PDFobj,
            _ objects: inout [PDFobj],
            _ objNumber: Int) {
        let tag = (type == "/Font") ? "/F" : "/Im"
        let number = String(objNumber)
        var list = [tag + number, number, "0", "R"]
        for i in 0..<obj.dict.count {
            if obj.dict[i] == type {
                let token = obj.dict[i + 1]
                if token == "<<" {
                    insertNewObject(&obj.dict, &list, type)
                } else {
                    let object = getObject(number: Int(token)!, from: &objects)!
                    insertNewObject(&object.dict, &list, type)
                }
                return
            }
        }

        // Handle the case where the page originally does not have any font resources.
        list = [type, "<<", tag + number, number, "0", "R", ">>"]
        for i in 0..<obj.dict.count {
            if obj.dict[i] == "/Resources" {
                obj.dict.insert(contentsOf: list, at: i + 2)
                return
            }
        }
        for i in 0..<obj.dict.count {
            if obj.dict[i] == "<<" {
                obj.dict.insert(contentsOf: list, at: i + 1)
                return
            }
        }
    }

    /// Adds an image to the resources of this page.
    ///
    /// - Parameter image: the image.
    /// - Parameter objects: the objects of the PDF.
    public final func addResource(
            _ image: Image,
            _ objects: inout [PDFobj]) {
        for i in 0..<dict.count {
            if dict[i] == "/Resources" {
                let token = dict[i + 1]
                if token == "<<" {      // Direct resources object
                    addResource("/XObject", self, &objects, image.objNumber!)
                } else {                  // Indirect resources object
                    let object = getObject(number: Int(token)!, from: &objects)!
                    addResource("/XObject", object, &objects, image.objNumber!)
                }
                return
            }
        }
    }

    /// Adds a font to the resources of this page.
    ///
    /// - Parameter font: the font.
    /// - Parameter objects: the objects of the PDF.
    public final func addResource(
            _ font: Font,
            _ objects: inout [PDFobj]) {
        for i in 0..<dict.count {
            if dict[i] == "/Resources" {
                let token = dict[i + 1]
                if token == "<<" {      // Direct resources object
                    addResource("/Font", self, &objects, font.objNumber)
                } else {                  // Indirect resources object
                    let object = getObject(number: Int(token)!, from: &objects)!
                    addResource("/Font", object, &objects, font.objNumber)
                }
                return
            }
        }
    }

    private final func firstCharIsDigit(_ str: String) -> Bool {
        if let scalar = str.unicodeScalars.first {
            return scalar >= "0" && scalar <= "9"
        }
        return false
    }

    /// Adds a content stream to this page.
    public final func addContent(_ content: inout [UInt8], _ objects: inout [PDFobj]) {
        let obj = PDFobj()
        obj.setNumber(objects.last!.number + 1)
        obj.setStream(&content)
        objects.append(obj)

        let objNumber = String(obj.number)
        var i = 0
        while i < dict.count {
            if dict[i] == "/Contents" {
                i += 1
                var token = dict[i]
                if token == "[" {
                    while true {
                        i += 1
                        token = dict[i]
                        if token == "]" {
                            dict.insert("R", at: i)
                            dict.insert("0", at: i)
                            dict.insert(objNumber, at: i)
                            return
                        }
                        i += 2  // Skip the 0 and R
                    }
                } else {
                    // Single content object
                    let obj2 = objects[Int(token)! - 1]
                    if obj2.data.count == 0 && obj2.stream == nil {
                        // This is not a stream object!
                        var j = 0
                        while j < obj2.dict.count {
                            if obj2.dict[j] == "]" {
                                obj2.dict.insert("R", at: j)
                                obj2.dict.insert("0", at: j)
                                obj2.dict.insert(objNumber, at: j)
                                return
                            }
                            j += 1
                        }
                    }
                    dict.insert("[", at: i)
                    dict.insert("]", at: i + 4)
                    dict.insert("R", at: i + 4)
                    dict.insert("0", at: i + 4)
                    dict.insert(objNumber, at: i + 4)
                    return
                }
            }
            i += 1
        }
    }

    ///
    /// Adds new content object before the existing content objects.
    /// The original code was provided by Stefan Ostermann author of ScribMaster and HandWrite Pro.
    /// Additional code to handle PDFs with indirect array of stream objects was written by EDragoev.
    ///
    /// - Parameter content: the content stream to add.
    /// - Parameter objects: the objects of the PDF, which receive the new content object.
    ///
    public final func addPrefixContent(_ content: inout [UInt8], _ objects: inout [PDFobj]) {
        let obj = PDFobj()
        obj.setNumber(objects.last!.number + 1)
        obj.setStream(&content)
        objects.append(obj)

        let objNumber = String(obj.number)
        var i = 0
        while i < dict.count {
            if dict[i] == "/Contents" {
                i += 1
                let token = dict[i]
                if token == "[" {
                    // Array of content object streams
                    i += 1
                    dict.insert("R", at: i)
                    dict.insert("0", at: i)
                    dict.insert(objNumber, at: i)
                    return
                } else {
                    // Single content object
                    let obj2 = objects[Int(token)! - 1]
                    if obj2.data.count == 0 && obj2.stream == nil {
                        // This is not a stream object!
                        var j = 0
                        while j < obj2.dict.count {
                            if obj2.dict[j] == "[" {
                                j += 1
                                obj2.dict.insert("R", at: j)
                                obj2.dict.insert("0", at: j)
                                obj2.dict.insert(objNumber, at: j)
                                return
                            }
                            j += 1
                        }
                    }
                    dict.insert("[", at: i)
                    dict.insert("]", at: i + 4)
                    i += 1
                    dict.insert("R", at: i)
                    dict.insert("0", at: i)
                    dict.insert(objNumber, at: i)
                    return
                }
            }
            i += 1
        }
    }

    private final func getMaxGSNumber(_ obj: PDFobj) -> Int {
        var maxGSNumber = 0
        for token in obj.dict {
            // The names are like /GS1, so they start with /GS, not equal it.
            if token.hasPrefix("/GS"), let number = Int(token.dropFirst(3)) {
                maxGSNumber = max(maxGSNumber, number)
            }
        }
        return maxGSNumber
    }

    /// Adds the graphics state to the resources of this page.
    public final func setGraphicsState(_ gs: GraphicsState, _ objects: inout [PDFobj]) {
        var obj: PDFobj?
        var index = -1
        var i = 0
        while i < dict.count {
            if dict[i] == "/Resources" {
                let token = dict[i + 1]
                if token == "<<" {
                    obj = self
                    index = i + 2
                } else {
                    obj = objects[Int(token)! - 1]
                    var j = 0
                    while j < obj!.dict.count {
                        if obj!.dict[j] == "<<" {
                            index = j + 1
                            break
                        }
                        j += 1
                    }
                }
                break
            }
            i += 1
        }
        if obj == nil || index == -1 {
            return
        }
        gsNumber = getMaxGSNumber(obj!)
        if gsNumber == 0 {                              // No existing ExtGState dictionary
            obj!.dict.insert("/ExtGState", at: index)   // Add ExtGState dictionary
            index += 1
            obj!.dict.insert("<<", at: index)
        } else {
            while index < obj!.dict.count {
                let token = obj!.dict[index]
                if token == "/ExtGState" {
                    index += 1
                    break
                }
                index += 1
            }
        }
        index += 1
        obj!.dict.insert("/GS" + String(gsNumber + 1), at: index)
        index += 1
        obj!.dict.insert("<<", at: index)
        index += 1
        obj!.dict.insert("/CA", at: index)
        index += 1
        obj!.dict.insert(String(gs.getAlphaStroking()), at: index)
        index += 1
        obj!.dict.insert("/ca", at: index)
        index += 1
        obj!.dict.insert(String(gs.getAlphaNonStroking()), at: index)
        index += 1
        obj!.dict.insert(">>", at: index)
        if gsNumber == 0 {
            index += 1
            obj!.dict.insert(">>", at: index)
        }

        var buf = String()
        buf.append("q\n")
        buf.append("/GS" + String(gsNumber + 1) + " gs\n")
        var array = Array(buf.utf8)
        addPrefixContent(&array, &objects)
    }
}
