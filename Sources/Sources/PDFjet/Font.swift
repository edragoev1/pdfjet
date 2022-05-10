/**
 *  Font.swift
 *
Copyright 2020 Innovatics Inc.

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


///
/// Used to create font objects.
/// The font objects must added to the PDF before they can be used to draw text.
///
public class Font {

    // Chinese (Traditional) font
    public static let AdobeMingStd_Light = "AdobeMingStd-Light"

    // Chinese (Simplified) font
    public static let STHeitiSC_Light = "STHeitiSC-Light"

    // Japanese font
    public static let KozMinProVI_Regular = "KozMinProVI-Regular"

    // Korean font
    public static let AdobeMyungjoStd_Medium = "AdobeMyungjoStd-Medium"

    public static var STREAM: Bool = true

    var name: String = ""
    var info: String = ""
    var objNumber = 0
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
    var bBoxLLx: Int16 = 0
    var bBoxLLy: Int16 = 0
    var bBoxURx: Int16 = 0
    var bBoxURy: Int16 = 0
    var firstChar = 32
    var lastChar = 255
    var capHeight: Int16 = 0
    var fontUnderlinePosition: Int16 = 0
    var fontUnderlineThickness: Int16 = 0
    var advanceWidth: [UInt16]?
    var glyphWidth: [Int]?
    var unicodeToGID: [Int]?
    var cff: Bool?
    var compressedSize: Int?
    var uncompressedSize: Int?
    var metrics: [[Int16]]?

    // Don't change the following default values!
    var size: Float = 12.0
    var isCoreFont = false
    var isCJK = false
    var skew15 = false
    var kernPairs = false

    var ascent: Float = 0.0
    var descent: Float = 0.0
    var bodyHeight: Float = 0.0
    var underlinePosition: Float = 0.0
    var underlineThickness: Float = 0.0


    ///
    /// Constructor for the 14 standard fonts.
    /// Creates a font object and adds it to the PDF.
    ///
    /// <pre>
    /// Examples:
    ///     Font font1 = Font(pdf, CoreFont.HELVETICA)
    ///     Font font2 = Font(pdf, CoreFont.TIMES_ITALIC)
    ///     Font font3 = Font(pdf, CoreFont.ZAPF_DINGBATS)
    ///     ...
    /// </pre>
    ///
    /// @param pdf the PDF to add this font to.
    /// @param coreFont the core font. Must be one the names defined in the CoreFont class.
    ///
    public init(_ pdf: PDF, _ coreFont: CoreFont) {
        let font = StandardFont.getInstance(coreFont)
        self.isCoreFont = true
        self.name = font.name!
        self.bBoxLLx = font.bBoxLLx!
        self.bBoxLLy = font.bBoxLLy!
        self.bBoxURx = font.bBoxURx!
        self.bBoxURy = font.bBoxURy!
        self.metrics = font.metrics
        self.fontUnderlinePosition = Int16(font.underlinePosition!)
        self.fontUnderlineThickness = Int16(font.underlineThickness!)
        self.fontAscent = Int16(font.bBoxURy!)
        self.fontDescent = Int16(font.bBoxLLy!)
        setSize(size)

        pdf.newobj()
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
        pdf.endobj()
        self.objNumber = pdf.getObjNumber()

        pdf.fonts.append(self)
    }


    // Used by PDFobj
    init(_ coreFont: CoreFont) {
        let font = StandardFont.getInstance(coreFont)
        self.isCoreFont = true
        self.name = font.name!
        self.bBoxLLx = font.bBoxLLx!
        self.bBoxLLy = font.bBoxLLy!
        self.bBoxURx = font.bBoxURx!
        self.bBoxURy = font.bBoxURy!
        self.metrics = font.metrics
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
    /// @param pdf the PDF to add this font to.
    /// @param fontName the font name. Please see Example_04.
    ///
    public init(_ pdf: PDF, _ fontName: String) {
        self.name = fontName
        self.isCJK = true
        self.firstChar = 0x0020
        self.lastChar = 0xFFEE
        self.ascent = self.size
        self.descent = self.ascent/4.0
        self.bodyHeight = self.ascent + self.descent

        // Font Descriptor
        pdf.newobj()
        pdf.append("<<\n")
        pdf.append("/Type /FontDescriptor\n")
        pdf.append("/FontName /")
        pdf.append(fontName)
        pdf.append("\n")
        pdf.append("/Flags 4\n")
        pdf.append("/FontBBox [0 0 0 0]\n")
        pdf.append(">>\n")
        pdf.endobj()

        // CIDFont Dictionary
        pdf.newobj()
        pdf.append("<<\n")
        pdf.append("/Type /Font\n")
        pdf.append("/Subtype /CIDFontType0\n")
        pdf.append("/BaseFont /")
        pdf.append(fontName)
        pdf.append("\n")
        pdf.append("/FontDescriptor ")
        pdf.append(pdf.getObjNumber() - 1)
        pdf.append(" 0 R\n")
        pdf.append("/CIDSystemInfo <<\n")
        pdf.append("/Registry (Adobe)\n")
        if fontName.hasPrefix("AdobeMingStd") {
            pdf.append("/Ordering (CNS1)\n")
            pdf.append("/Supplement 4\n")
        } else if fontName.hasPrefix("AdobeSongStd")
                || fontName.hasPrefix("STHeitiSC") {
            pdf.append("/Ordering (GB1)\n")
            pdf.append("/Supplement 4\n")
        } else if fontName.hasPrefix("KozMinPro") {
            pdf.append("/Ordering (Japan1)\n")
            pdf.append("/Supplement 4\n")
        } else if fontName.hasPrefix("AdobeMyungjoStd") {
            pdf.append("/Ordering (Korea1)\n")
            pdf.append("/Supplement 1\n")
        } else {
            // TODO:
            print("Unsupported font: " + fontName)
        }
        pdf.append(">>\n")
        pdf.append(">>\n")
        pdf.endobj()

        // Type0 Font Dictionary
        pdf.newobj()
        pdf.append("<<\n")
        pdf.append("/Type /Font\n")
        pdf.append("/Subtype /Type0\n")
        pdf.append("/BaseFont /")
        if fontName.hasPrefix("AdobeMingStd") {
            pdf.append(fontName + "-UniCNS-UTF16-H\n")
            pdf.append("/Encoding /UniCNS-UTF16-H\n")
        } else if fontName.hasPrefix("AdobeSongStd")
                || fontName.hasPrefix("STHeitiSC") {
            pdf.append(fontName + "-UniGB-UTF16-H\n")
            pdf.append("/Encoding /UniGB-UTF16-H\n")
        } else if fontName.hasPrefix("KozMinPro") {
            pdf.append(fontName + "-UniJIS-UCS2-H\n")
            pdf.append("/Encoding /UniJIS-UCS2-H\n")
        } else if fontName.hasPrefix("AdobeMyungjoStd") {
            pdf.append(fontName + "-UniKS-UCS2-H\n")
            pdf.append("/Encoding /UniKS-UCS2-H\n")
        } else {
            // TODO:
            print("Unsupported font: " + fontName)
        }
        pdf.append("/DescendantFonts [")
        pdf.append(pdf.getObjNumber() - 1)
        pdf.append(" 0 R]\n")
        pdf.append(">>\n")
        pdf.endobj()
        self.objNumber = pdf.getObjNumber()

        pdf.fonts.append(self)
    }


    // Constructor for .ttf.stream fonts:
    public init(_ pdf: PDF, _ stream: InputStream, _ flag: Bool) throws {
        try FontStream1.register(pdf, self, stream)
        setSize(size)
    }


    // Constructor for .ttf.stream fonts:
    public init(_ objects: inout [PDFobj], _ stream: InputStream, _ flag: Bool) throws {
        try FontStream2.register(&objects, self, stream)
        setSize(size)
    }


    ///
    /// Constructor for OpenType and TrueType fonts.
    ///
    /// @param pdf the PDF object that requires this font.
    /// @param stream the input stream to read this font from.
    ///
    public init(_ pdf: PDF, _ stream: InputStream) throws {
        OpenTypeFont.register(pdf, self, stream)
        setSize(size)
    }


    ///
    /// Sets the size of this font.
    ///
    /// @param fontSize specifies the size of this font.
    /// @return the font.
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
    /// @return the current size of the font.
    ///
    public func getSize() -> Float {
        return self.size
    }


    ///
    /// Sets the kerning for the selected font to 'true' or 'false'
    /// depending on the passed value of kernPairs parameter.
    ///
    /// The kerning is implemented only for the 14 standard fonts.
    ///
    /// @param kernPairs if 'true' the kerning for this font is enabled.
    ///
    @discardableResult
    public func setKernPairs(_ kernPairs: Bool) -> Font {
        self.kernPairs = kernPairs
        return self
    }


    ///
    /// Returns the width of the specified string when drawn on the page with this font using the current font size.
    ///
    /// @param str the specified string.
    ///
    /// @return the width of the string when draw on the page with this font using the current selected size.
    ///
    public func stringWidth(_ str: String?) -> Float {
        if str == nil {
            return 0.0
        }

        if isCJK {
            return Float(str!.count) * self.ascent
        }

        let scalars = Array(str!.unicodeScalars)
        var width = 0
        var i = 0
        while i < scalars.count {
            var c1 = Int(scalars[i].value)
            if self.isCoreFont {
                if c1 < self.firstChar || c1 > self.lastChar {
                    c1 = 0x20
                }
                c1 -= 32
                width += Int(metrics![c1][1])
                if self.kernPairs && i < (scalars.count - 1) {
                    var c2 = scalars[i + 1].value
                    if c2 < self.firstChar || c2 > self.lastChar {
                        c2 = 32
                    }

                    var j: Int = 2
                    while j < metrics![c1].count {
                        if metrics![c1][j] == c2 {
                            width += Int(metrics![c1][j + 1])
                            break
                        }
                        j += 2
                    }
                }
            }
            else {
                if c1 < firstChar || c1 > lastChar {
                    width += Int(advanceWidth![0])
                }
                else {
                    width += glyphWidth![c1]
                }
            }
            i += 1
        }

        return Float(width) * self.size / Float(self.unitsPerEm)
    }


    ///
    /// Returns the ascent of this font.
    ///
    /// @return the ascent of the font.
    ///
    public func getAscent() -> Float {
        return self.ascent
    }


    ///
    /// Returns the descent of this font.
    ///
    /// @return the descent of the font.
    ///
    public func getDescent() -> Float {
        return self.descent
    }


    ///
    /// Returns the height of this font.
    ///
    /// @return the height of the font.
    ///
    public func getHeight() -> Float {
        return self.ascent + self.descent
    }


    ///
    /// Returns the height of the body of the font.
    ///
    /// @return float the height of the body of the font.
    ///
    public func getBodyHeight() -> Float {
        return self.bodyHeight
    }


    ///
    /// Returns the number of characters from the specified string that will fit within the specified width.
    ///
    /// @param str the specified string.
    /// @param width the specified width.
    ///
    /// @return the number of characters that will fit.
    ///
    public func getFitChars(
            _ str: String,
            _ width: Float) -> Int {

        var w = width * Float(unitsPerEm) / size

        if isCJK {
            return Int(w / Float(self.ascent))
        }

        if isCoreFont {
            return getCoreFontFitChars(str, w)
        }

        var i = 0
        for scalar in str.unicodeScalars {
            let c1 = Int(scalar.value)
            if c1 < firstChar || c1 > lastChar {
                w -= Float(advanceWidth![0])
            }
            else {
                w -= Float(glyphWidth![c1])
            }
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
            var c1 = Int(scalar.value)
            if c1 < firstChar || c1 > lastChar {
                c1 = 32
            }

            c1 -= 32
            w -= Float(metrics![c1][1])
            if w < 0 {
                return i
            }

            if kernPairs && i < (scalars.count - 1) {
                var c2 = scalars[i + 1].value
                if c2 < firstChar || c2 > lastChar {
                    c2 = 32
                }

                var j: Int = 2
                while j < metrics![c1].count {
                    if metrics![c1][j] == c2 {
                        w -= Float(metrics![c1][j + 1])
                        if w < 0 {
                            return i
                        }
                        break
                    }
                    j += 2
                }
            }
            i += 1
        }

        return i
    }


    ///
    /// Sets the skew15 private variable.
    /// When the variable is set to 'true' all glyphs in the font are skewed on 15 degrees.
    /// This makes a regular font look like an italic type font.
    /// Use this method when you don't have real italic font in the font family,
    /// or when you want to generate smaller PDF files.
    /// For example you could embed only the Regular and Bold fonts and synthesize the RegularItalic and BoldItalic.
    ///
    /// @param skew15 the skew flag.
    ///
    public func setItalic(_ skew15: Bool) {
        self.skew15 = skew15
    }


    ///
    /// Returns the width of a string drawn using two fonts.
    ///
    /// @param fallbackFont the fallback font.
    /// @param str the string.
    /// @return the width.
    ///
    public func stringWidth(_ fallbackFont: Font?, _ str: String?) -> Float {
        var width: Float = 0.0

        if self.isCoreFont || self.isCJK || fallbackFont == nil || fallbackFont!.isCoreFont || fallbackFont!.isCJK {
            return stringWidth(str)
        }

        var activeFont = self
        var buf = String()
        for scalar in str!.unicodeScalars {
            if activeFont.unicodeToGID![Int(scalar.value)] == 0 {
                width += activeFont.stringWidth(buf)
                buf = ""
                // Switch the active font
                if activeFont === self {
                    activeFont = fallbackFont!
                }
                else {
                    activeFont = self
                }
            }
            buf.append(String(scalar))
        }
        width += activeFont.stringWidth(buf)

        return width
    }

}   // End of Font.swift
