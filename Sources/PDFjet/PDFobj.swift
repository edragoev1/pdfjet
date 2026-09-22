/**
 * PDFobj.swift
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
import Foundation

///
/// An object of a PDF that was read with PDF.read, which holds the tokens of
/// its dictionary and its stream. See Example_20, Example_37 and Example_50.
///
public final class PDFobj {
    var number = 0                  // The object number
    var offset = 0                  // The object offset
    ///
    /// The tokens of the object dictionary. Edit them in place to change an
    /// object that was read, for example the /MediaBox or /Rotate of a page:
    /// getDict returns a copy, since Swift arrays are values.
    ///
    final var dict = [String]()
    var streamOffset = 0
    var stream: [UInt8]?            // The compressed stream
    final var data = [UInt8]()      // The decompressed data
    var gsNumber = -1
    var root = false                // The catalog that the trailer's /Root names

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

    ///
    /// Copies the stream from the buffer, decrypts it when the PDF is encrypted,
    /// and decodes it with the filters of its /Filter entry. The decrypted
    /// stream replaces the encrypted one, so that it can be copied.
    ///
    final func setStreamAndData(
            _ buffer: inout [UInt8], _ length: Int, _ decryptor: Decryptor? = nil) throws {
        if stream == nil {
            var length = length
            let actual = PDFobj.streamLength(buffer, streamOffset, length)
            if actual != length {
                length = actual
                setLength(length)
            }
            // A /Length the PDF does not have is checked before the stream is
            // made, so that a file of a few bytes that says its stream is a
            // gigabyte takes no memory.
            if length < 0 || streamOffset < 0 || length > buffer.count - streamOffset {
                throw PDFjetError(message: "The stream of an object is not in the PDF.")
            }
            var copied = Array(buffer[streamOffset..<streamOffset + length])
            if let decryptor = decryptor {
                copied = decryptor.decryptStream(self, copied)
                setLength(copied.count)
            }
            stream = copied
            data = try decodeStream(copied)
        }
    }

    // Returns the decoded stream. A cross-reference stream or an object stream
    // that cannot be decoded throws, as the objects in it cannot be read. Any
    // other stream that cannot be decoded, like an image or the content of a
    // page that is cut short, has no data: the rest of the PDF is read, and a
    // merge copies the stream as it is.
    private final func decodeStream(_ stream: [UInt8]) throws -> [UInt8] {
        let type = getValue("/Type")
        if type == "/XRef" || type == "/ObjStm" {
            return try decode(stream)
        }
        do {
            return try decode(stream)
        } catch {
            return []
        }
    }

    // Returns the length of the stream that starts at the offset. It is the
    // /Length when the endstream keyword follows it, as it must; when it does
    // not -- a /Length that is missing, too short or too long -- the stream
    // ends at the end of line before the next endstream, as MuPDF and pdf.js
    // read it. A stream with no endstream after it keeps its /Length.
    private static func streamLength(_ buf: [UInt8], _ offset: Int, _ length: Int) -> Int {
        if offset < 0 || offset > buf.count {
            return length
        }
        let keyword = Array("endstream".utf8)
        if length >= 0 && length <= buf.count - offset {
            var i = offset + length
            while i < buf.count && isWhiteSpace(buf[i]) {
                i += 1
            }
            if startsWith(buf, i, keyword) {
                return length
            }
        }
        var end = offset
        while !startsWith(buf, end, keyword) {
            if end + keyword.count > buf.count {
                return length
            }
            end += 1
        }
        if end > offset && buf[end - 1] == 0x0A {
            end -= 1
        }
        if end > offset && buf[end - 1] == 0x0D {
            end -= 1
        }
        return end - offset
    }

    private static func isWhiteSpace(_ c: UInt8) -> Bool {
        return c == 0x00 || c == 0x09 || c == 0x0A || c == 0x0C || c == 0x0D || c == 0x20
    }

    private static func startsWith(_ buf: [UInt8], _ off: Int, _ str: [UInt8]) -> Bool {
        if off < 0 || off > buf.count - str.count {
            return false
        }
        for i in 0..<str.count where buf[off + i] != str[i] {
            return false
        }
        return true
    }

    // Sets the /Length of the stream, replacing a reference to the length, and
    // adds it to a stream dictionary that has none.
    private final func setLength(_ length: Int) {
        guard let i = dict.firstIndex(of: "/Length") else {
            if let open = dict.firstIndex(of: "<<") {
                dict.insert(contentsOf: ["/Length", String(length)], at: open + 1)
            }
            return
        }
        if i + 1 >= dict.count {
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
                decoded = applyDecodeParms(try inflate(decoded), i)
            case "/LZWDecode", "/LZW":
                decoded = applyDecodeParms(try lzwDecode(decoded), i)
            case "/ASCIIHexDecode", "/AHx":
                decoded = asciiHexDecode(decoded)
            case "/ASCII85Decode", "/A85":
                decoded = ascii85Decode(decoded)
            case "/RunLengthDecode", "/RL":
                decoded = try runLengthDecode(decoded)
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
                if i + 1 >= dict.count {
                    return ""
                }
                let token = dict[i + 1]
                if token == "<<" {
                    return valueUpTo(i + 2, ">>", "<< ")
                } else if token == "[" {
                    return valueUpTo(i + 2, "]", "[ ")
                } else {
                    return token
                }
            }
            i += 1
        }
        return ""
    }

    // Returns the tokens from the index up to the closing one, with the
    // opening before them and the closing after them.
    private final func valueUpTo(_ start: Int, _ closing: String, _ opening: String) -> String {
        var buffer = opening
        var i = start
        while i < dict.count && dict[i] != closing {
            buffer.append(dict[i])
            buffer.append(" ")
            i += 1
        }
        buffer.append(closing)
        return buffer
    }

    // Returns the object numbers of the key, which is one reference or an
    // array of them. A dictionary that ends in the middle of them, or a token
    // that is not a number where one belongs, ends the list: a PDF that was
    // read can hold anything.
    final func getObjectNumbers(_ key: String) -> [Int] {
        var numbers = [Int]()
        var i = 0
        while i < dict.count {
            let token = dict[i]
            if token == key {
                i += 1
                if i >= dict.count {
                    break
                }
                if dict[i] == "[" {
                    while true {
                        i += 1
                        if i >= dict.count || dict[i] == "]" {
                            break
                        }
                        guard let number = Int(dict[i]) else {
                            break
                        }
                        numbers.append(number)
                        i += 1  // 0
                        i += 1  // R
                    }
                } else if let number = Int(dict[i]) {
                    numbers.append(number)
                }

                break
            }
            i += 1
        }
        return numbers
    }

    /// Returns the width and height of the page, which its /MediaBox gives as
    /// the two corners of a rectangle, in either order. A page with no
    /// /MediaBox of its own, one that is not four numbers, or an empty one is
    /// letter size, as MuPDF and pdf.js draw it. PDF.getPageObjects gives a
    /// page the box it inherits from the page tree, which a page of another
    /// program's PDF often does not carry itself, and the numbers of a box
    /// that is an object of its own or refers to them.
    ///
    /// - Returns: the page size.
    public final func getPageSize() -> PageSize {
        for i in 0..<dict.count {
            if dict[i] == "/MediaBox" {
                if tokenAt(dict, i + 1) != "[" {
                    break
                }
                var box = [Float](repeating: 0.0, count: 4)
                for j in 0..<4 {
                    guard let value = Float(tokenAt(dict, i + 2 + j)) else {
                        return Letter.PORTRAIT
                    }
                    box[j] = value
                }
                let width = abs(box[2] - box[0])
                let height = abs(box[3] - box[1])
                if width == 0 || height == 0 {
                    return Letter.PORTRAIT
                }
                return PageSize(width, height)
            }
        }
        return Letter.PORTRAIT
    }

    // The /Length of the stream of this object, which is a number or a
    // reference to an object that holds one. A dictionary that ends where the
    // length is read, or a length that is not a number, throws, as it fails
    // in the other three ports.
    final func getLength(_ objects: [PDFobj]) throws -> Int {
        // The entry of the dictionary, and not a /Length inside another value or
        // that is the value of another entry, "/Height/Length", as pdf.js tests
        // it in issue19611.
        guard let open = dict.firstIndex(of: "<<") else {
            return 0
        }
        let key = PDF.entryIndex(Array(dict[open...]), "/Length")
        if key == -1 {
            return 0
        }
        let i = key + open
        guard i + 1 < dict.count, let number = Int(dict[i + 1]) else {
            throw PDFjetError(message: "The /Length of a stream is not a number.")
        }
        guard i + 2 < dict.count else {
            throw PDFjetError(message: "The dictionary ends after the /Length.")
        }
        if dict[i + 2] == "0" {
            guard i + 3 < dict.count else {
                throw PDFjetError(message: "The dictionary ends after the /Length.")
            }
            if dict[i + 3] == "R" {
                return try getLength(number, from: objects)
            }
        }
        return number
    }

    // Returns the length stored in the object with the number. The objects
    // are in the order of the cross-reference, not of their numbers.
    private final func getLength(
            _ number: Int,
            from objects: [PDFobj]) throws -> Int {
        for obj in objects where obj.number == number {
            guard obj.dict.count > 3, let length = Int(obj.dict[3]) else {
                throw PDFjetError(message: "The /Length of a stream is not a number.")
            }
            return length
        }
        return 0
    }

    // Returns the object with the number, or nil when the PDF that was read
    // does not have one: a reference can name an object that is not in the file.
    private final func getObject(
            number: Int,
            from objects: [PDFobj]) -> PDFobj? {
        if number < 1 || number > objects.count {
            return nil
        }
        return objects[number - 1]
    }

    // Returns the token at the index, or an empty string when the dictionary
    // of an object that was read ends before it.
    private final func tokenAt(_ dict: [String], _ index: Int) -> String {
        if index < 0 || index >= dict.count {
            return ""
        }
        return dict[index]
    }

    // Returns the object that the token names, or nil when the token is not
    // the number of an object the PDF has: a page of a file that was changed
    // can name an object that is not in it.
    private final func objectAt(_ objects: [PDFobj], _ token: String) -> PDFobj? {
        guard let number = Int(token) else {
            return nil
        }
        return getObject(number: number, from: objects)
    }

    // Returns the index to insert at, which is the end of the dictionary when
    // the index is past it.
    private final func indexAt(_ dict: [String], _ index: Int) -> Int {
        return (index < 0) ? 0 : min(index, dict.count)
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
    public final func getContentObject(_ objects: [PDFobj]) -> PDFobj? {
        var numbers = getObjectNumbers("/Contents")
        if numbers.count == 1 {
            guard let object = getObject(number: numbers[0], from: objects) else {
                return nil
            }
            if object.stream != nil {
                return object
            }
            // "/Contents 39 0 R" where the object is "[ 41 0 R 43 0 R ]"
            numbers = [Int]()
            var i = object.dict.firstIndex(of: "[") ?? -1
            while i != -1 && i + 3 < object.dict.count && object.dict[i + 3] == "R" {
                guard let number = Int(object.dict[i + 1]) else {
                    break
                }
                numbers.append(number)
                i += 3
            }
        }
        if numbers.isEmpty {
            return nil
        }
        if numbers.count == 1 {
            return getObject(number: numbers[0], from: objects)
        }
        let content = PDFobj()
        for number in numbers {
            guard let object = getObject(number: number, from: objects) else {
                continue
            }
            if object.stream != nil {
                content.data.append(contentsOf: object.data)
                content.data.append(UInt8(ascii: "\n"))     // A stream can end in the middle of a line.
            }
        }
        return content
    }

    /// Returns the resources object of this page.
    ///
    /// - Parameter objects: the objects of the PDF.
    /// - Returns: the resources object, or nil if the page has none.
    public final func getResourcesObject(_ objects: [PDFobj]) -> PDFobj? {
        var i = 0
        while i < dict.count {
            if dict[i] == "/Resources" {
                if i + 1 >= dict.count {
                    return nil
                }
                let token = dict[i + 1]
                if token == "<<" {
                    return self
                }
                guard let number = Int(token) else {
                    return nil
                }
                return getObject(number: number, from: objects)
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
            _ objects: inout [PDFobj]) throws -> Font {
        let font = try Font(coreFont)
        font.fontID = font.name.replacingOccurrences(of: "-", with: "_").uppercased()

        let obj = PDFobj()
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
        obj.number = objects.count + 1
        objects.append(obj)

        // The first /Resources of the page is the one, and the font is added
        // to it once: adding it to the page again, after a resources object
        // that names the page itself has grown the page dictionary, never
        // ended.
        var i = 0
        while i < dict.count {
            if dict[i] == "/Resources" {
                let token = tokenAt(dict, i + 1)
                if token == "<<" {                  // Direct resources object
                    addFontResource(self, objects, font.fontID!, obj.number)
                } else if let object = objectAt(objects, token) {  // Indirect
                    addFontResource(object, objects, font.fontID!, obj.number)
                }
                break
            }
            i += 1
        }

        return font
    }

    private final func addFontResource(
            _ obj: PDFobj,
            _ objects: [PDFobj],
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
            // Direct resources follow "/Resources <<" in the page dictionary; an
            // indirect resources object has no /Resources key, and its entries
            // follow its first "<<".
            if let k = obj.dict.firstIndex(of: "/Resources") {
                i = k + 2
            } else {
                i = (obj.dict.firstIndex(of: "<<") ?? -1) + 1
            }
            obj.dict.insert(contentsOf: ["/Font", "<<", ">>"], at: indexAt(obj.dict, i))
        }

        i = 0
        while i < obj.dict.count {
            if obj.dict[i] == "/Font" {
                let token = tokenAt(obj.dict, i + 1)
                if token == "<<" {
                    obj.dict.insert("/" + fontID, at: i + 2)
                    obj.dict.insert(String(number), at: i + 3)
                    obj.dict.insert("0", at: i + 4)
                    obj.dict.insert("R", at: i + 5)
                    return
                } else if let o2 = objectAt(objects, token) {
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
        if dict.contains(list[0]) {
            return
        }
        for i in 0..<dict.count {
            if dict[i] == type {
                dict.insert(contentsOf: list, at: indexAt(dict, i + 2))
                return
            }
        }
        if tokenAt(dict, 3) == "<<" {
            dict.insert(contentsOf: list, at: 4)
            return
        }
    }

    private final func addResource(
            _ type: String,
            _ obj: PDFobj,
            _ objects: [PDFobj],
            _ objNumber: Int) {
        let tag = (type == "/Font") ? "/F" : "/Im"
        let number = String(objNumber)
        var list = [tag + number, number, "0", "R"]
        for i in 0..<obj.dict.count {
            if obj.dict[i] == type {
                let token = tokenAt(obj.dict, i + 1)
                if token == "<<" {
                    insertNewObject(&obj.dict, &list, type)
                } else if let object = objectAt(objects, token) {
                    insertNewObject(&object.dict, &list, type)
                }
                return
            }
        }

        // Handle the case where the page originally does not have any font resources.
        list = [type, "<<", tag + number, number, "0", "R", ">>"]
        for i in 0..<obj.dict.count {
            if obj.dict[i] == "/Resources" {
                obj.dict.insert(contentsOf: list, at: indexAt(obj.dict, i + 2))
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
                let token = tokenAt(dict, i + 1)
                if token == "<<" {      // Direct resources object
                    addResource("/XObject", self, objects, image.objNumber!)
                } else if let object = objectAt(objects, token) {  // Indirect
                    addResource("/XObject", object, objects, image.objNumber!)
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
                let token = tokenAt(dict, i + 1)
                if token == "<<" {      // Direct resources object
                    addResource("/Font", self, objects, font.objNumber)
                } else if let object = objectAt(objects, token) {  // Indirect
                    addResource("/Font", object, objects, font.objNumber)
                }
                return
            }
        }
    }

    /// Adds a content stream to this page.
    public final func addContent(_ content: inout [UInt8], _ objects: inout [PDFobj]) {
        let obj = PDFobj()
        obj.setNumber(objects.count + 1)
        obj.setStream(&content)
        objects.append(obj)

        let objNumber = String(obj.number)
        var i = 0
        while i < dict.count {
            if dict[i] == "/Contents" {
                i += 1
                let token = tokenAt(dict, i)
                if token == "[" {
                    // Array of content objects, which can end before its "]".
                    i += 1
                    while i < dict.count {
                        if dict[i] == "]" {
                            dict.insert("R", at: i)
                            dict.insert("0", at: i)
                            dict.insert(objNumber, at: i)
                            return
                        }
                        i += 3  // Skip the number, the 0 and the R
                    }
                    return
                } else {
                    // Single content object
                    guard let obj2 = objectAt(objects, token) else {
                        return
                    }
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
                    if tokenAt(dict, i + 1) != "0" || tokenAt(dict, i + 2) != "R" {
                        return  // Not a whole "n 0 R" to put in an array.
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
        obj.setNumber(objects.count + 1)
        obj.setStream(&content)
        objects.append(obj)

        let objNumber = String(obj.number)
        var i = 0
        while i < dict.count {
            if dict[i] == "/Contents" {
                i += 1
                let token = tokenAt(dict, i)
                if token == "[" {
                    // Array of content object streams
                    i += 1
                    dict.insert("R", at: indexAt(dict, i))
                    dict.insert("0", at: indexAt(dict, i))
                    dict.insert(objNumber, at: indexAt(dict, i))
                    return
                } else {
                    // Single content object
                    guard let obj2 = objectAt(objects, token) else {
                        return
                    }
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
                    if tokenAt(dict, i + 1) != "0" || tokenAt(dict, i + 2) != "R" {
                        return  // Not a whole "n 0 R" to put in an array.
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
    @discardableResult
    public final func setGraphicsState(_ gs: GraphicsState, _ objects: inout [PDFobj]) -> PDFobj {
        var obj: PDFobj?
        var index = -1
        var i = 0
        while i < dict.count {
            if dict[i] == "/Resources" {
                let token = tokenAt(dict, i + 1)
                if token == "<<" {
                    obj = self
                    index = i + 2
                } else if let object = objectAt(objects, token) {
                    obj = object
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
        guard var resources = obj, index != -1 else {
            return self
        }
        // The graphics states go in the /ExtGState dictionary of the resources,
        // which can be an object of its own. Resources without one get it.
        var k = index
        while k < resources.dict.count && resources.dict[k] != "/ExtGState" {
            k += 1
        }
        if k == resources.dict.count {
            resources.dict.insert(contentsOf: ["/ExtGState", "<<", ">>"], at: index)
            index += 2
        } else if tokenAt(resources.dict, k + 1) == "<<" {
            index = k + 2
        } else {                                        // "/ExtGState 12 0 R"
            guard let object = objectAt(objects, tokenAt(resources.dict, k + 1)) else {
                return self
            }
            resources = object
            index = (resources.dict.firstIndex(of: "<<") ?? -1) + 1
        }
        gsNumber = getMaxGSNumber(resources)
        let name = "/GS" + String(gsNumber + 1)
        resources.dict.insert(contentsOf: [
                name, "<<",
                "/CA", String(decoding: FastFloat.toByteArray(gs.getAlphaStroking()), as: UTF8.self),
                "/ca", String(decoding: FastFloat.toByteArray(gs.getAlphaNonStroking()), as: UTF8.self),
                ">>"], at: index)
        var content = Array("q\n\(name) gs\n".utf8)
        addPrefixContent(&content, &objects)
        return self
    }
}
