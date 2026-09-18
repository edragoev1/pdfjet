/**
 * Font.swift
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
import Foundation

///
/// Used to create font objects.
/// The font objects must be added to the PDF before they can be used to draw text.
///
public class Font {
    var name: String = ""
    var info: String = ""
    var objNumber = 0
    // The identity of the PDF the font was added to, or nil for a font of an existing PDF.
    var pdfIdentity: UUID?
    var fontID: String?

    // The object number of the embedded font file
    var fileObjNumber = 0
    var fontDescriptorObjNumber = 0
    var cidFontDictObjNumber = 0
    var toUnicodeCMapObjNumber = 0

    // Font attributes
    var unitsPerEm = 1000
    var fontAscent: Int16 = 0
    var fontDescent: Int16 = 0
    var fontLineGap: Int16 = 0     // The space the font puts between its lines.
    var bBoxLLx: Int16 = 0
    var bBoxLLy: Int16 = 0
    var bBoxURx: Int16 = 0
    var bBoxURy: Int16 = 0
    var firstChar = 32
    var lastChar = 255
    var capHeight: Int16 = 0
    var fontUnderlinePosition: Int16 = 0
    var fontUnderlineThickness: Int16 = 0
    var advanceWidth: [UInt16] = []
    var unicodeToGID: [Int] = []
    // The offsets of the marks that attach to other marks, in font units, by
    // the glyph IDs of the two marks, or nil. A font has them from its GPOS
    // table, read from a .otf or .ttf file or from a stream that keeps them.
    var markToMarkOffsets: [Int: [Int]]?
    // The anchors of the marks, by glyph ID, for each MarkToBase and
    // MarkToLigature lookup subtable, or nil: the class and anchor of each
    // mark. A font has them from its GPOS table, read from a .otf or .ttf file
    // or from a stream that keeps them.
    var markAnchors: [[Int: [Int]]]?
    // The anchors of the letters and ligatures that the marks go on, by glyph
    // ID, for each subtable in markAnchors, or nil: 1 and an anchor, or 0 and
    // no anchor, for each class of marks.
    var baseAnchors: [[Int: [Int]]]?
    // Where the marks go, compressed, as a stream font keeps it until a mark
    // is drawn in the font, or nil.
    var markData: [UInt8]?
    var cff: Bool = false
    var compressedSize: Int?
    var uncompressedSize: Int?
    var metrics: [[Int16]]?

    // Don't change the following default values!
    var size: Float = 12.0
    var isCoreFont = false
    var isCJK = false
    var skew15 = false
    var kernPairs = false
    // True for Symbol and ZapfDingbats, whose characters are the codes of
    // their own encodings, not WinAnsi.
    private var symbolic = false

    var ascent: Float = 0.0
    var descent: Float = 0.0
    var bodyHeight: Float = 0.0
    var underlinePosition: Float = 0.0
    var underlineThickness: Float = 0.0

    ///
    /// Constructor for the 14 standard fonts.
    /// Creates a font object and adds it to the PDF.
    ///
    /// Examples:
    ///
    /// ```swift
    /// let font1 = Font(pdf, CoreFont.HELVETICA)
    /// let font2 = Font(pdf, CoreFont.TIMES_ITALIC)
    /// let font3 = Font(pdf, CoreFont.ZAPF_DINGBATS)
    /// ```
    ///
    /// - Parameter pdf: the PDF to add this font to.
    /// - Parameter coreFont: the core font. Must be one the names defined in the CoreFont class.
    /// - Throws: PDFjetError when the number is not one of the fourteen core fonts.
    ///
    public init(_ pdf: PDF, _ coreFont: Int) throws {
        self.pdfIdentity = pdf.identity
        let font = try CoreFont(coreFont)
        self.isCoreFont = true
        self.name = font.name!
        self.bBoxLLx = font.bBoxLLx!
        self.bBoxLLy = font.bBoxLLy!
        self.bBoxURx = font.bBoxURx!
        self.bBoxURy = font.bBoxURy!
        self.metrics = font.metrics
        self.symbolic = font.name == "Symbol" || font.name == "ZapfDingbats"
        self.fontUnderlinePosition = Int16(font.underlinePosition!)
        self.fontUnderlineThickness = Int16(font.underlineThickness!)
        self.fontAscent = Int16(font.bBoxURy!)
        self.fontDescent = Int16(font.bBoxLLy!)
        setSize(size)

        pdf.newObj()
        pdf.append("<<\n")
        pdf.append("/Type /Font\n")
        pdf.append("/Subtype /Type1\n")
        pdf.append("/BaseFont /")
        pdf.append(self.name)
        pdf.append("\n")
        if self.name != "Symbol" && self.name != "ZapfDingbats" {
            pdf.append("/Encoding /WinAnsiEncoding\n")
        }
        pdf.append(">>\n")
        pdf.endObj()
        self.objNumber = pdf.getObjNumber()

        pdf.fonts.append(self)
    }

    // Used by PDFobj
    init(_ coreFont: Int) throws {
        let font = try CoreFont(coreFont)
        self.isCoreFont = true
        self.name = font.name!
        self.bBoxLLx = font.bBoxLLx!
        self.bBoxLLy = font.bBoxLLy!
        self.bBoxURx = font.bBoxURx!
        self.bBoxURy = font.bBoxURy!
        self.metrics = font.metrics
        self.symbolic = font.name == "Symbol" || font.name == "ZapfDingbats"
        self.fontUnderlinePosition = Int16(font.underlinePosition!)
        self.fontUnderlineThickness = Int16(font.underlineThickness!)
        self.fontAscent = Int16(font.bBoxURy!)
        self.fontDescent = Int16(font.bBoxLLy!)
        setSize(size)
    }

    ///
    /// Constructor for CJK - Chinese, Japanese and Korean fonts.
    /// Please see Example_04.
    ///
    /// - Parameter pdf: the PDF to add this font to.
    /// - Parameter font: the Chinese, Japanese or Korean font. Please see Example_04.
    ///
    public init(_ pdf: PDF, _ font: CJKFont) {
        self.pdfIdentity = pdf.identity
        var fontName: String?
        if (font == CJKFont.ADOBE_MING_STD_LIGHT) {             // Chinese (Traditional) font
            fontName = "AdobeMingStd-Light"
        } else if (font == CJKFont.ST_HEITI_SC_LIGHT) {         // Chinese (Simplified) font
            fontName = "STHeitiSC-Light"
        } else if (font == CJKFont.KOZ_MIN_PRO_VI_REGULAR) {    // Japanese font
            fontName = "KozMinProVI-Regular"
        } else if (font == CJKFont.ADOBE_MYUNGJO_STD_MEDIUM) {  // Korean font
            fontName = "AdobeMyungjoStd-Medium"
        }
        self.name = fontName!
        self.isCJK = true
        self.firstChar = 0x0020
        self.lastChar = 0xFFEE
        self.ascent = self.size
        self.descent = self.ascent/4.0
        self.bodyHeight = self.ascent + self.descent

        // Font Descriptor
        pdf.newObj()
        pdf.append("<<\n")
        pdf.append("/Type /FontDescriptor\n")
        pdf.append("/FontName /")
        pdf.append(fontName!)
        pdf.append("\n")
        pdf.append("/Flags 4\n")
        pdf.append("/FontBBox [0 0 0 0]\n")
        pdf.append(">>\n")
        pdf.endObj()

        // CIDFont Dictionary
        pdf.newObj()
        pdf.append("<<\n")
        pdf.append("/Type /Font\n")
        pdf.append("/Subtype /CIDFontType0\n")
        pdf.append("/BaseFont /")
        pdf.append(fontName!)
        pdf.append("\n")
        pdf.append("/FontDescriptor ")
        pdf.append(pdf.getObjNumber() - 1)
        pdf.append(" 0 R\n")
        pdf.append("/CIDSystemInfo <<\n")
        pdf.append("/Registry (Adobe)\n")
        if fontName!.hasPrefix("AdobeMingStd") {
            pdf.append("/Ordering (CNS1)\n")
            pdf.append("/Supplement 4\n")
        } else if fontName!.hasPrefix("AdobeSongStd")
                || fontName!.hasPrefix("STHeitiSC") {
            pdf.append("/Ordering (GB1)\n")
            pdf.append("/Supplement 4\n")
        } else if fontName!.hasPrefix("KozMinPro") {
            pdf.append("/Ordering (Japan1)\n")
            pdf.append("/Supplement 4\n")
        } else if fontName!.hasPrefix("AdobeMyungjoStd") {
            pdf.append("/Ordering (Korea1)\n")
            pdf.append("/Supplement 1\n")
        } else {
            fatalError("Unsupported font: " + fontName!)
        }
        pdf.append(">>\n")
        pdf.append(">>\n")
        pdf.endObj()

        // Type0 Font Dictionary
        pdf.newObj()
        pdf.append("<<\n")
        pdf.append("/Type /Font\n")
        pdf.append("/Subtype /Type0\n")
        pdf.append("/BaseFont /")
        if fontName!.hasPrefix("AdobeMingStd") {
            pdf.append(fontName! + "-UniCNS-UTF16-H\n")
            pdf.append("/Encoding /UniCNS-UTF16-H\n")
        } else if fontName!.hasPrefix("AdobeSongStd")
                || fontName!.hasPrefix("STHeitiSC") {
            pdf.append(fontName! + "-UniGB-UTF16-H\n")
            pdf.append("/Encoding /UniGB-UTF16-H\n")
        } else if fontName!.hasPrefix("KozMinPro") {
            pdf.append(fontName! + "-UniJIS-UCS2-H\n")
            pdf.append("/Encoding /UniJIS-UCS2-H\n")
        } else if fontName!.hasPrefix("AdobeMyungjoStd") {
            pdf.append(fontName! + "-UniKS-UCS2-H\n")
            pdf.append("/Encoding /UniKS-UCS2-H\n")
        } else {
            fatalError("Unsupported font: " + fontName!)
        }
        pdf.append("/DescendantFonts [")
        pdf.append(pdf.getObjNumber() - 1)
        pdf.append(" 0 R]\n")
        pdf.append(">>\n")
        pdf.endObj()
        self.objNumber = pdf.getObjNumber()

        pdf.fonts.append(self)
    }

    /// Creates a font from a .ttf.stream or .otf.stream font and adds it to the objects of an existing PDF.
    public init(_ objects: inout [PDFobj], _ stream: InputStream) throws {
        try FontStream2.register(&objects, self, stream)
        setSize(size)
    }

    ///
    /// Constructor for OpenType, TrueType, .otf.stream and .ttf.stream fonts. The
    /// format is told from the first bytes of the stream.
    ///
    /// - Parameter pdf: the PDF object that requires this font.
    /// - Parameter stream: the input stream to read this font from.
    ///
    public init(_ pdf: PDF, _ stream: InputStream) throws {
        self.pdfIdentity = pdf.identity
        let bytes = try Content.getFromStream(stream)
        if Font.isOpenTypeFont(bytes) {
            try OpenTypeFont.register(pdf, self, InputStream(data: Data(bytes)))
        } else {
            try FontStream1.register(pdf, self, InputStream(data: Data(bytes)))
        }
        setSize(size)
    }

    // Returns true if the bytes start with the version of an OpenType or TrueType font.
    private static func isOpenTypeFont(_ bytes: [UInt8]) -> Bool {
        if bytes.count < 4 {
            return false
        }
        var version: UInt32 = 0
        for i in 0..<4 {
            version = (version << 8) | UInt32(bytes[i])
        }
        return version == 0x00010000 || version == 0x74727565 || version == 0x4F54544F
    }

    ///
    /// Constructor for OpenType, TrueType and .otf.stream and .ttf.stream fonts.
    ///
    /// - Parameter pdf: the pdf object.
    /// - Parameter fontPath: the font path.
    /// - Throws: an error if the font cannot be read.
    ///
    public init(_ pdf: PDF, _ fontPath: String) throws {
        self.pdfIdentity = pdf.identity
        guard FileManager.default.fileExists(atPath: fontPath),
                let inputStream = InputStream(fileAtPath: fontPath) else {
            throw PDFjetError(message: "Font file not found: " + fontPath)
        }
        if (fontPath.hasSuffix(".stream")) {
            try FontStream1.register(pdf, self, inputStream)
        } else {
            try OpenTypeFont.register(pdf, self, inputStream)
        }
        setSize(size)
    }

    ///
    /// Sets the size of this font.
    ///
    /// - Parameter fontSize: specifies the size of this font.
    /// - Returns: the font.
    ///
    @discardableResult
    public func setSize(_ fontSize: Float) -> Font {
        self.size = fontSize
        if (isCJK) {
            self.ascent = size
            self.descent = ascent/4
            self.bodyHeight = self.ascent + self.descent
            return self
        }
        self.ascent = Float(fontAscent) * size / Float(unitsPerEm)
        self.descent = -Float(fontDescent) * size / Float(unitsPerEm)
        self.bodyHeight = self.ascent + self.descent
        self.underlineThickness =
                (Float(fontUnderlineThickness) * size / Float(unitsPerEm))
        self.underlinePosition =
                -(Float(fontUnderlinePosition) * size / Float(unitsPerEm)) + Float(underlineThickness) / 2.0
        return self
    }

    ///
    /// Returns the current font size.
    ///
    /// - Returns: the current size of the font.
    ///
    public func getSize() -> Float {
        return self.size
    }

    /// Returns the name of this font.
    public func getName() -> String {
        return self.name
    }

    ///
    /// Sets the kerning for the selected font to 'true' or 'false'
    /// depending on the passed value of kernPairs parameter.
    ///
    /// The kerning is implemented only for the 14 standard fonts.
    ///
    /// - Parameter kernPairs: if 'true' the kerning for this font is enabled.
    ///
    @discardableResult
    public func setKernPairs(_ kernPairs: Bool) -> Font {
        self.kernPairs = kernPairs
        return self
    }

    /// Returns the width of the string at the current font size.
    public func stringWidth(_ str: String?) -> Float {
        return stringWidth(self.size, str)
    }

    ///
    /// Returns the width of the specified string when drawn with this font at the specified font size.
    ///
    /// - Parameter fontSize: the font size.
    /// - Parameter str: the specified string.
    ///
    /// - Returns: the width of the string.
    ///
    public func stringWidth(_ fontSize: Float, _ str: String?) -> Float {
        var width: Int = 0
        if str == nil || str == "" {
            return 0.0
        }

        if isCJK {
            // Every glyph of a CJK font is as wide as the font size.
            return Float(str!.unicodeScalars.count) * fontSize
        }

        let scalars = Array(str!.unicodeScalars)
        if self.isCoreFont {
            for (i, scalar) in scalars.enumerated() {
                let c1 = coreFontCode(scalar.value)
                width += Int(metrics![c1 - 32][1])
                if self.kernPairs && i < (scalars.count - 1) {
                    width += kerning(c1, coreFontCode(scalars[i + 1].value))
                }
            }
        } else {
            for scalar in scalars {
                width += advanceWidthOf(Int(scalar.value))
            }
        }

        return Float(width) * fontSize / Float(self.unitsPerEm)
    }

    // Returns the advance width, in font units, of the glyph that Page draws
    // for the character: none for an RLM, LRM, ZWNJ, ZWJ or byte order mark,
    // which are not drawn, and that of a space for a character the font does
    // not cover.
    private func advanceWidthOf(_ codePoint: Int) -> Int {
        if Font.isJoinerOrRLM(UInt32(codePoint)) || codePoint == 0xFEFF {
            return 0
        }
        let gid = (codePoint < firstChar || codePoint > lastChar) ? unicodeToGID[0x20] : unicodeToGID[codePoint]
        return Int((gid < advanceWidth.count) ? advanceWidth[gid] : advanceWidth[0])
    }

    // Returns true if the font has a glyph for the character. A character past
    // the end of the glyph table of the font, like an emoji, has none, and a
    // core font has the characters of its encoding.
    func hasGlyph(_ codePoint: Int) -> Bool {
        if isCoreFont {
            return codePoint == 32 || coreFontCode(UInt32(codePoint)) != 32
        }
        return codePoint < unicodeToGID.count && unicodeToGID[codePoint] != 0
    }

    // Returns the font, of the font and its fallback font, that draws the
    // character at index i of the scalars, when the character before it is
    // drawn with the active font: the font when it has a glyph for the
    // character, else the fallback font when that has one, else the font. A
    // combining mark stays with the character before it when that font has it,
    // and an RLM, LRM, ZWNJ or ZWJ goes with the character after it.
    static func fontOf(
            _ font: Font,
            _ fallbackFont: Font,
            _ active: Font,
            _ scalars: String.UnicodeScalarView,
            _ i: String.UnicodeScalarView.Index) -> Font {
        var scalar = scalars[i]
        if i > scalars.startIndex && scalar.properties.generalCategory == .nonspacingMark &&
                active.hasGlyph(Int(scalar.value)) {
            return active
        }
        let after = scalars.index(after: i)
        if Font.isJoinerOrRLM(scalar.value) && after < scalars.endIndex {
            scalar = scalars[after]
        }
        if font.hasGlyph(Int(scalar.value)) || !fallbackFont.hasGlyph(Int(scalar.value)) {
            return font
        }
        return fallbackFont
    }

    ///
    /// Returns the ascent of this font.
    ///
    /// - Returns: the ascent of the font.
    ///
    public func getAscent() -> Float {
        return self.ascent
    }

    ///
    /// Returns the descent of this font.
    ///
    /// - Returns: the descent of the font.
    ///
    public func getDescent() -> Float {
        return self.descent
    }

    /// Returns the ascent at the specified font size.
    public func getAscent(_ fontSize: Float) -> Float {
        if isCJK {
            return fontSize
        }
        return Float(fontAscent) * fontSize / Float(unitsPerEm)
    }

    /// Returns the descent at the specified font size.
    public func getDescent(_ fontSize: Float) -> Float {
        if isCJK {
            return fontSize/4
        }
        return -Float(fontDescent) * fontSize / Float(unitsPerEm)
    }

    /// Returns the line gap of this font at the specified size: the space the
    /// font puts between the descent of a line and the ascent of the next one.
    /// It is 0 for most fonts, which leave that space in their ascent and
    /// descent, and 1 em for the Japanese and Chinese IBM Plex fonts, whose
    /// ascent and descent add up to 1 em.
    public func getLineGap(_ fontSize: Float) -> Float {
        if isCJK {
            return 0.0
        }
        return Float(fontLineGap) * fontSize / Float(unitsPerEm)
    }

    ///
    /// Returns the height of the body of this font at its current size.
    ///
    /// - Returns: the height of the body of the font.
    ///
    public func getBodyHeight() -> Float {
        return self.bodyHeight
    }

    /// Returns the ascent plus the descent at the specified font size.
    public func getBodyHeight(_ fontSize: Float) -> Float {
        return getAscent(fontSize) + getDescent(fontSize)
    }

    /// Returns the underline thickness at the specified font size.
    public func getUnderlineThickness(_ fontSize: Float) -> Float {
        return Float(fontUnderlineThickness) * fontSize / Float(unitsPerEm)
    }

    /// Returns the underline position at the specified font size.
    public func getUnderlinePosition(_ fontSize: Float) -> Float {
        return -(Float(fontUnderlinePosition) * fontSize / Float(unitsPerEm))
                + getUnderlineThickness(fontSize) / 2.0
    }

    /// Returns the underline thickness at the current font size.
    public func getUnderlineThickness() -> Float {
        return self.underlineThickness
    }

    /// Returns the underline position at the current font size.
    public func getUnderlinePosition() -> Float {
        return self.underlinePosition
    }

    ///
    /// Returns the number of characters from the specified string that will fit within the specified width.
    ///
    /// - Parameter str: the specified string.
    /// - Parameter width: the specified width.
    ///
    /// - Returns: the number of characters that will fit.
    ///
    public func getFitChars(
            _ str: String,
            _ width: Float) -> Int {
        var w = width * Float(unitsPerEm) / size
        if isCJK {
            // Every glyph of a CJK font is as wide as the font size.
            return max(0, min(Int(width / size), str.unicodeScalars.count))
        }
        if isCoreFont {
            return getCoreFontFitChars(str, w)
        }
        var i = 0
        for scalar in str.unicodeScalars {
            w -= Float(advanceWidthOf(Int(scalar.value)))
            if w < 0 {
                break
            }
            i += 1
        }
        return i
    }

    private func getCoreFontFitChars(
            _ str: String,
            _ width: Float) -> Int {
        var w: Float = width
        let scalars = Array(str.unicodeScalars)
        var i: Int = 0
        for scalar in scalars {
            let c1 = coreFontCode(scalar.value)
            w -= Float(metrics![c1 - 32][1])
            if w < 0 {
                return i
            }
            if kernPairs && i < (scalars.count - 1) {
                w -= Float(kerning(c1, coreFontCode(scalars[i + 1].value)))
                if w < 0 {
                    return i
                }
            }
            i += 1
        }
        return i
    }

    /// Returns the code of the character in the encoding of this core font:
    /// WinAnsi, which puts ’ at 146, “ and ” at 147 and 148, the dashes at 150
    /// and 151 and € at 128, or, for Symbol and ZapfDingbats, the character
    /// itself. A character that is not in the encoding is drawn as a space.
    func coreFontCode(_ scalar: UInt32) -> Int {
        var cp = Int(scalar)
        if !symbolic {
            cp = Font.winAnsiCode(cp)
        }
        return (cp < 32 || cp > 255) ? 32 : cp
    }

    // The WinAnsi code of the character: the character itself below 128 and
    // from 160 to 255, the code of the character WinAnsi puts from 128 to 159,
    // and a space for any other character, the C1 controls U+0080 to U+009F too.
    private static func winAnsiCode(_ cp: Int) -> Int {
        if cp < 0x80 || (cp >= 0xA0 && cp <= 0xFF) {
            return cp
        }
        switch cp {
        case 0x20AC: return 128    // €
        case 0x201A: return 130    // ‚
        case 0x0192: return 131    // ƒ
        case 0x201E: return 132    // „
        case 0x2026: return 133    // …
        case 0x2020: return 134    // †
        case 0x2021: return 135    // ‡
        case 0x02C6: return 136    // ˆ
        case 0x2030: return 137    // ‰
        case 0x0160: return 138    // Š
        case 0x2039: return 139    // ‹
        case 0x0152: return 140    // Œ
        case 0x017D: return 142    // Ž
        case 0x2018: return 145    // ‘
        case 0x2019: return 146    // ’
        case 0x201C: return 147    // “
        case 0x201D: return 148    // ”
        case 0x2022: return 149    // •
        case 0x2013: return 150    // –
        case 0x2014: return 151    // —
        case 0x02DC: return 152    // ˜
        case 0x2122: return 153    // ™
        case 0x0161: return 154    // š
        case 0x203A: return 155    // ›
        case 0x0153: return 156    // œ
        case 0x017E: return 158    // ž
        case 0x0178: return 159    // Ÿ
        default: return 32
        }
    }

    /// Returns the kerning of a pair of characters of this core font, in 1/1000
    /// of the font size: negative when the second character moves closer to the
    /// first, and 0 when the font has no kerning for the pair.
    func kerning(_ c1: Int, _ c2: Int) -> Int {
        let row = metrics![c1 - 32]
        var j = 2
        while j < row.count {
            if Int(row[j]) == c2 {
                return Int(row[j + 1])
            }
            j += 2
        }
        return 0
    }

    ///
    /// Sets the skew15 private variable.
    /// When the variable is set to 'true' all glyphs in the font are skewed on 15 degrees.
    /// This makes a regular font look like an italic type font.
    /// Use this method when you don't have real italic font in the font family,
    /// or when you want to generate smaller PDF files.
    /// For example you could embed only the Regular and Bold fonts and synthesize the RegularItalic and BoldItalic.
    ///
    /// - Parameter skew15: the skew flag.
    ///
    @discardableResult
    public func setItalic(_ skew15: Bool) -> Font {
        self.skew15 = skew15
        return self
    }

    ///
    /// Returns the width of the string at the current font size,
    /// using the fallback font for characters this font does not have.
    ///
    public func stringWidth(_ fallbackFont: Font?, _ str: String?) -> Float {
        return stringWidth(fallbackFont, self.size, str)
    }

    ///
    /// Returns the width of a string drawn using two fonts.
    ///
    /// - Parameter fallbackFont: the fallback font.
    /// - Parameter fontSize: the font size.
    /// - Parameter str: the string.
    /// - Returns: the width.
    ///
    public func stringWidth(_ fallbackFont: Font?, _ fontSize: Float, _ str: String?) -> Float {
        return stringWidth(fallbackFont, fontSize, fontSize, str)
    }

    // Returns the width of a string drawn using two fonts, with the characters
    // in the fallback font at the fallback font size.
    func stringWidth(_ fallbackFont: Font?, _ fontSize: Float, _ fallbackFontSize: Float, _ str: String?) -> Float {
        var width: Float = 0.0
        if str == nil || self.isCJK || fallbackFont == nil || fallbackFont!.isCJK {
            return stringWidth(fontSize, str)
        }
        var activeFont = self
        var activeSize = fontSize
        // The runs of text drawn with one font are the pieces of the string
        // between the characters that switch fonts, so measuring them copies
        // nothing per character: a string that is all in the primary font is
        // one piece, the string itself.
        let scalars = str!.unicodeScalars
        var start = scalars.startIndex
        var i = scalars.startIndex
        while i < scalars.endIndex {
            let charFont = Font.fontOf(self, fallbackFont!, activeFont, scalars, i)
            if charFont !== activeFont {
                width += activeFont.stringWidth(activeSize, String(scalars[start..<i]))
                start = i
                activeFont = charFont
                activeSize = (activeFont === self) ? fontSize : fallbackFontSize
            }
            i = scalars.index(after: i)
        }
        if start == scalars.startIndex {
            return width + activeFont.stringWidth(activeSize, str!)
        }
        return width + activeFont.stringWidth(activeSize, String(scalars[start...]))
    }

    // Returns true for the right-to-left and left-to-right marks and the zero
    // width non-joiner and joiner, which are not drawn: Page gives the glyph
    // before or after them an actual text.
    static func isJoinerOrRLM(_ value: UInt32) -> Bool {
        return value == 0x200F || value == 0x200E || value == 0x200C || value == 0x200D
    }
}   // End of Font.swift
