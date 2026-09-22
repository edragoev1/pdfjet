/**
 * Paragraph.swift
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
import Foundation

///
/// Used to create paragraph objects.
/// See the TextColumn class for more information.
///
public class Paragraph {
    var xText: Float = 0.0
    var yText: Float = 0.0
    var x1: Float = 0.0
    var y1: Float = 0.0
    var x2: Float = 0.0
    var y2: Float = 0.0
    var lines = [TextLine]()
    var alignment = Alignment.LEFT
    // True after setTextAlignment. Otherwise the alignment of the text column applies.
    var explicitAlignment = false
    // The structure element of the paragraph in a PDF/UA document, which is a
    // paragraph unless it is set to a heading; see setStructureType.
    var structureType = StructElem.P
    // The label of a paragraph that is an item of a list, and how far to the
    // left of the text it is drawn; see setListLabel.
    var listLabel: TextLine?
    var listLabelIndent: Float = 0.0
    // Whether each text line was added with addJoined, by its index. A text
    // line added to the lines another way is not joined.
    private var joined = [Bool]()

    /// Makes this paragraph an item of a list, labelled by the text line, which
    /// is drawn indent points to the left of the text of the paragraph and on the
    /// baseline of its first line. A run of paragraphs that have a label is a
    /// list: in a PDF/UA document it is an L of an LI for each paragraph, each
    /// holding the Lbl of its label and the LBody of its text, so that a reader
    /// reads the label of an item before the item, however the two are drawn.
    @discardableResult
    public func setListLabel(_ label: TextLine, _ indent: Float) -> Paragraph {
        self.listLabel = label
        self.listLabelIndent = indent
        return self
    }

    /// Sets the structure element type of this paragraph, for a PDF/UA document:
    /// StructElem.H1 to StructElem.H6 for a heading, and StructElem.P, which it
    /// is, for a paragraph. The paragraph is one element however many lines and
    /// words it is drawn in.
    @discardableResult
    public func setStructureType(_ structureType: StructElem) -> Paragraph {
        self.structureType = structureType
        return self
    }

    ///
    /// Constructor for creating paragraph objects.
    ///
    public init() {
    }

    /// Creates a paragraph with the specified text line.
    public init(_ text: TextLine) {
        lines.append(text)
        joined.append(false)
    }

    ///
    /// Adds a text line to this paragraph.
    ///
    /// - Parameter text: the text line to add to this paragraph.
    /// - Returns: this paragraph.
    ///
    @discardableResult
    public func add(_ text: TextLine) -> Paragraph {
        lines.append(text)
        setJoined(lines.count - 1, false)
        return self
    }

    ///
    /// Adds a text line that goes on from the text before it with no space
    /// between them, so that a word can change its font or its color partway
    /// through: a word in bold followed by a comma in the regular font, or a
    /// link that ends before the period after it. A row of text does not
    /// break between the two. When the text line starts with a space, or the
    /// text before it ends with one, the two are apart as they are with add.
    /// Text in Chinese, Japanese or Korean is not joined.
    ///
    /// - Parameter text: the text line to add to this paragraph.
    /// - Returns: this paragraph.
    ///
    @discardableResult
    public func addJoined(_ text: TextLine) -> Paragraph {
        lines.append(text)
        setJoined(lines.count - 1, true)
        return self
    }

    private func setJoined(_ index: Int, _ value: Bool) {
        while joined.count <= index {
            joined.append(false)
        }
        joined[index] = value
    }

    // Returns true when the text line at the index goes on from the one before
    // it with no space between them: it was added with addJoined, and there is
    // no space where the two meet.
    func joinsPrevious(_ index: Int) -> Bool {
        if index <= 0 || index >= lines.count || index >= joined.count || !joined[index] {
            return false
        }
        guard let text = lines[index].text, let before = lines[index - 1].text,
                let first = text.unicodeScalars.first, let last = before.unicodeScalars.last else {
            return false
        }
        return !String.isASCIIWhitespace(first)
                && !String.isASCIIWhitespace(last)
                && !text.isCJK() && !before.isCJK()
    }

    // Returns true when the space after the text line at the index goes at the
    // start of the next text line instead, in its font: the space between two
    // text lines is the narrower of their two spaces, so that the space after
    // a word in a monospaced font, which is wide, is the space of the text
    // after it; and when the two are as wide, it is the next one's when this
    // one has an underline, a strikeout or a link, which would go on under the
    // space. The next text line has a word, is not joined to this one, and has
    // no underline, strikeout or link, which would start at the space.
    func spaceMovesToNext(_ index: Int) -> Bool {
        if index + 1 >= lines.count || joinsPrevious(index + 1) {
            return false
        }
        let line = lines[index]
        let next = lines[index + 1]
        guard let text = line.text, let nextText = next.text else {
            return false
        }
        if nextText.trim().isEmpty
                || text.isCJK() || nextText.isCJK()
                || next.underline || next.strikeout
                || next.getURIAction() != nil || next.getGoToAction() != nil {
            return false
        }
        let nextSpace = next.font!.stringWidth(next.fallbackFont, next.fontSize, Single.space)
        let space = line.font!.stringWidth(line.fallbackFont, line.fontSize, Single.space)
        let decorated = line.underline || line.strikeout
                || line.getURIAction() != nil || line.getGoToAction() != nil
        return nextSpace < space || (decorated && nextSpace <= space)
    }

    ///
    /// Sets the alignment of the text in this paragraph. A paragraph with no
    /// alignment set takes the alignment of the text column it is drawn in.
    ///
    /// - Parameter alignment: the alignment code: Alignment.LEFT, Alignment.RIGHT, Alignment.CENTER or Alignment.JUSTIFY.
    /// - Returns: this paragraph.
    ///
    @discardableResult
    public func setTextAlignment(_ alignment: Alignment) -> Paragraph {
        self.alignment = alignment
        self.explicitAlignment = true
        return self
    }

    /// Returns the text lines of this paragraph.
    public func getTextLines() -> [TextLine] {
        return lines
    }

    /// Returns true if the first line of this paragraph starts with the specified token.
    public func startsWith(_ token: String) -> Bool {
        return lines[0].getText()!.hasPrefix(token)
    }

    /// Sets the text color of all lines in this paragraph as a 0xRRGGBB value.
    @discardableResult
    public func setTextColor(_ color: Int32) -> Paragraph {
        for line in lines {
            line.setTextColor(color)
        }
        return self
    }

    /// Sets the text color of all lines in this paragraph from an array of red, green and blue values between 0.0 and 1.0.
    @discardableResult
    public func setTextColor(_ rgbColor: [Float]) -> Paragraph {
        for line in lines {
            line.setTextColor(rgbColor)
        }
        return self
    }

    /// Sets the word highlight colors of all lines in this paragraph.
    @discardableResult
    public func setHighlightColors(_ colorMap: [String: Int32]) -> Paragraph {
        for line in lines {
            line.setHighlightColors(colorMap)
        }
        return self
    }

    /// Reads a text file and returns its paragraphs. An empty line separates the paragraphs.
    public static func paragraphsFromFile(_ f1: Font, _ filePath: String) throws -> [Paragraph] {
        var paragraphs = [Paragraph]()
        let contents = try Content.ofTextFile(filePath)
        var paragraph = Paragraph()
        var textLine = TextLine(f1)
        var sb = String()
        let scalars = Array(contents.unicodeScalars)
        var i = 0
        while i < scalars.count {
            let ch = scalars[i]
            // We need at least one character after the \n\n to begin new paragraph!
            if i < (scalars.count - 2) &&
                    ch == "\n" && scalars[i + 1] == "\n" {
                textLine.setText(sb)
                paragraph.add(textLine)
                paragraphs.append(paragraph)
                paragraph = Paragraph()
                textLine = TextLine(f1)
                sb = ""
                i += 1
            } else {
                sb.append(String(ch))
            }
            i += 1
        }
        if !sb.isEmpty {
            textLine.setText(sb)
            paragraph.add(textLine)
            paragraphs.append(paragraph)
        }
        return paragraphs
    }

    /// Returns the x coordinate where the text of this paragraph starts, once a TextFrame or TextColumn has drawn it.
    public func getTextX() -> Float {
        return xText
    }

    /// Returns the baseline y coordinate of the first line of this paragraph, once a TextFrame or TextColumn has drawn it.
    public func getTextY() -> Float {
        return yText
    }

    /// Returns the x coordinate of the top left corner of this paragraph, once a TextFrame or TextColumn has drawn it.
    public func getX1() -> Float {
        return x1
    }

    /// Returns the y coordinate of the top left corner of this paragraph, once a TextFrame or TextColumn has drawn it.
    public func getY1() -> Float {
        return y1
    }

    /// Returns the x coordinate where the last line of this paragraph ends, once a TextFrame or TextColumn has drawn it.
    public func getX2() -> Float {
        return x2
    }

    /// Returns the y coordinate of the bottom of the last line of this paragraph, once a TextFrame or TextColumn has drawn it.
    public func getY2() -> Float {
        return y2
    }
}   // End of Paragraph.swift
