/**
 * TextColumn.swift
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
import Foundation

///
/// A column of paragraphs, each a list of TextLine objects that can differ in
/// font, size and color, aligned left, right, center or justified, with a line
/// spacing, a paragraph spacing and an optional line between the paragraphs.
/// It draws all of its paragraphs where it is placed, top down; to rotate a
/// column, add it to a Container and rotate that.
///
/// Use a TextColumn for an article or a page of mixed text: bold or colored
/// words in a paragraph, justified paragraphs, CJK paragraphs. Use a TextBlock
/// for one run of text in one font, and a TextFrame when the text must
/// continue from one frame to the next, across columns or pages. Please see
/// Example_10, Example_29, Example_44 and Example_49.
///
public class TextColumn : Drawable {
    var alignment = Alignment.LEFT

    private var x: Float = 0.0      // This variable is set in the beginning and only reset after the drawOn
    var y: Float = 0.0              // This variable is set in the beginning and only reset after the drawOn
    private var w: Float = 0.0
    private var h: Float = 0.0
    private var x1: Float = 0.0
    private var y1: Float = 0.0
    private var lineSpacing: Float = 1.0
    private var paragraphSpacing: Float = 1.0
    private var paragraphs: [Paragraph]
    private var lineBetweenParagraphs = false

    ///
    /// Create a text column object.
    ///
    public init() {
        self.paragraphs = [Paragraph]()
    }

    ///
    /// Sets the lineBetweenParagraphs private variable value.
    /// If the value is set to true - an empty line will be inserted between the current and next paragraphs.
    ///
    /// - Parameter lineBetweenParagraphs: the specified Bool value.
    ///
    @discardableResult
    public func setLineBetweenParagraphs(_ lineBetweenParagraphs: Bool) -> TextColumn {
        self.lineBetweenParagraphs = lineBetweenParagraphs
        return self
    }

    /// Sets the spacing between the lines in this text column.
    @discardableResult
    public func setLineSpacing(_ lineSpacing: Float) -> TextColumn {
        self.lineSpacing = lineSpacing
        return self
    }

    /// Sets the space between paragraphs.
    @discardableResult
    public func setParagraphSpacing(_ paragraphSpacing: Float) -> TextColumn {
        self.paragraphSpacing = paragraphSpacing
        return self
    }

    ///
    /// Sets the position of this text column on the page.
    ///
    /// - Parameter x: the x coordinate of the top left corner of this text column when drawn on the page.
    /// - Parameter y: the y coordinate of the top left corner of this text column when drawn on the page.
    ///
    @discardableResult
    public func setLocation(_ x: Float, _ y: Float) -> Self {
        self.x = x
        self.y = y
        self.x1 = x
        self.y1 = y
        return self
    }

    ///
    /// Sets the desired width of this text column.
    ///
    /// - Parameter w: the width of this text column.
    ///
    @discardableResult
    public func setWidth(_ w: Float) -> TextColumn {
        self.w = w
        return self
    }

    ///
    /// Sets the height of this text column.
    ///
    /// - Parameter h: the height of this text column.
    ///
    @discardableResult
    public func setHeight(_ h: Float) -> TextColumn {
        self.h = h
        return self
    }

    ///
    /// Returns the width of this text column.
    ///
    public func getWidth() -> Float {
        return self.w
    }

    ///
    /// Returns the height of this text column.
    ///
    public func getHeight() -> Float {
        return self.h
    }

    ///
    /// Sets the text alignment.
    ///
    /// - Parameter alignment: the specified alignment code.
    /// Supported values: Alignment.LEFT, Alignment.RIGHT, Alignment.CENTER and Alignment.JUSTIFY
    ///
    @discardableResult
    public func setTextAlignment(_ alignment: Alignment) -> TextColumn {
        self.alignment = alignment
        return self
    }

    ///
    /// Adds a new paragraph to this text column.
    ///
    /// - Parameter paragraph: the new paragraph object.
    ///
    @discardableResult
    public func addParagraph(_ paragraph: Paragraph) -> TextColumn {
        self.paragraphs.append(paragraph)
        return self
    }

    ///
    /// Removes the last paragraph added to this text column.
    ///
    @discardableResult
    public func removeLastParagraph() -> TextColumn {
        if self.paragraphs.count >= 1 {
            self.paragraphs.removeLast()
        }
        return self
    }

    ///
    /// Returns dimension object containing the width and height of this component.
    /// Please see Example_29.
    ///
    /// - Returns: dimension object containing the width and height of this component.
    ///
    public func getSize() -> Dimension {
        let xy = drawOn(nil)
        return Dimension(self.w, xy[1] - self.y)
    }

    ///
    /// Draws this text column on the specified page.
    /// If the page is nil, nothing is drawn and only the location of the next component is computed.
    ///
    /// - Parameter page: the page to draw this text column on.
    /// - Returns: the x and y coordinates of the bottom right corner of this text column.
    ///
    @discardableResult
    public func drawOn(_ page: Page?) -> [Float] {
        var xy: [Float] = [x, y]
        for (i, paragraph) in paragraphs.enumerated() {
            xy = drawParagraphOn(page, paragraph, i == (paragraphs.count - 1))
        }
        // Restore the original location
        setLocation(self.x, self.y)
        // A column with a height reaches at least that far down from its location
        if y + h > xy[1] {
            xy[1] = y + h
        }
        return [x + w, xy[1]]
    }

    private func drawParagraphOn(
            _ page: Page?, _ paragraph: Paragraph, _ lastParagraph: Bool) -> [Float] {
        let alignment = paragraph.explicitAlignment ? paragraph.alignment : self.alignment
        var list = [TextLine]()
        var lineHeight: Float = 0.0
        var maxAscent: Float = 0.0
        var maxDescent: Float = 0.0
        for line in paragraph.lines {
            if (line.getHeight() * self.lineSpacing) > lineHeight {
                lineHeight = line.getHeight() * lineSpacing
            }
            if line.font!.getAscent(line.fontSize) > maxAscent {
                maxAscent = line.font!.getAscent(line.fontSize)
            }
            if line.font!.getDescent(line.fontSize) > maxDescent {
                maxDescent = line.font!.getDescent(line.fontSize)
            }
        }
        self.y1 += maxAscent

        var runLength: Float = 0.0
        for line in paragraph.lines {
            for token in (line.text ?? "").splitOnWhitespace() {
                let textLine = line.copyWithText(token + Single.space)
                // The token is measured without the space that follows it: a
                // line is as wide as the text it shows. A token wider than the
                // column goes on a line of its own rather than after an empty
                // one, which would leave the line above it blank.
                if list.isEmpty || (runLength + TextColumn.width(textLine, token)) <= self.w {
                    list.append(textLine)
                    runLength += textLine.getWidth()
                } else {
                    drawLineOfText(page, list, alignment)
                    moveToNextLine(lineHeight)
                    list.removeAll()
                    list.append(textLine)
                    runLength = textLine.getWidth()
                }
            }
        }
        // The last line of a paragraph is not justified.
        drawNonJustifiedLine(page, list, alignment)

        // The paragraph reaches down to the descent of its last line. The
        // spacing and the blank line go between the paragraphs, not after the
        // last one.
        if lastParagraph {
            return moveToNextParagraph(maxDescent)
        }
        if lineBetweenParagraphs {
            moveToNextLine(lineHeight)
        }

        return moveToNextParagraph(lineHeight * self.paragraphSpacing)
    }

    // The last token of a line drawn on the page ends the line: its underline
    // and its strikeout stop at its text, not after the space that follows it.
    private static func markLastToken(_ list: [TextLine]) {
        if let last = list.last {
            last.isLastToken = true
        }
    }

    // The width of the text the line shows: every token with the space after
    // it, and the last token without it.
    private static func visibleWidth(_ list: [TextLine]) -> Float {
        var runLength: Float = 0.0
        for (i, textLine) in list.enumerated() {
            if i == (list.count - 1) {
                runLength += width(textLine, trimTrailingSpaces(textLine.text))
            } else {
                runLength += textLine.getWidth()
            }
        }
        return runLength
    }

    private static func width(_ textLine: TextLine, _ text: String?) -> Float {
        return textLine.font!.stringWidth(textLine.fallbackFont, textLine.fontSize, text)
    }

    private static func trimTrailingSpaces(_ text: String?) -> String {
        var trimmed = text ?? ""
        while trimmed.hasSuffix(" ") {
            trimmed.removeLast()
        }
        return trimmed
    }

    @discardableResult
    private func moveToNextLine(_ lineHeight: Float) -> [Float] {
        x1 = x
        y1 += lineHeight
        return [x1, y1]
    }

    private func moveToNextParagraph(_ paragraphSpacing: Float) -> [Float] {
        x1 = x
        y1 += paragraphSpacing
        return [x1, y1]
    }

    private func drawLineOfText(_ page: Page?, _ list: [TextLine], _ alignment: Alignment) {
        if alignment == Alignment.JUSTIFY {
            TextColumn.markLastToken(list)
            // The spaces are widened so that the text of the line reaches both
            // edges. A line of one token has no space to widen.
            let dx = (list.count > 1) ?
                    (w - TextColumn.visibleWidth(list)) / Float(list.count - 1) : 0.0
            // Each token draws its own link annotation when the line has a URI or GoTo action.
            for textLine in list {
                textLine.setLocation(x1, y1 + textLine.getVerticalOffset())
                textLine.drawOn(page)
                x1 += textLine.getWidth() + dx
            }
        } else {
            drawNonJustifiedLine(page, list, alignment)
        }
    }

    private func drawNonJustifiedLine(_ page: Page?, _ list: [TextLine], _ alignment: Alignment) {
        TextColumn.markLastToken(list)
        let runLength = TextColumn.visibleWidth(list)

        if alignment == Alignment.CENTER {
            x1 = x + ((w - runLength) / 2)
        } else if alignment == Alignment.RIGHT {
            x1 = x + (w - runLength)
        }

        // Each token draws its own link annotation when the line has a URI or GoTo action.
        for textLine in list {
            textLine.setLocation(x1, y1 + textLine.getVerticalOffset())
            textLine.drawOn(page)
            x1 += textLine.getWidth()
        }
    }

    ///
    /// Adds a paragraph of Chinese, Japanese or Korean text to this text column,
    /// wrapped at any character to the width of the column.
    ///
    /// - Parameter font: the font used by this paragraph.
    /// - Parameter text: the text.
    ///
    @discardableResult
    public func addCJKParagraph(_ font: Font, _ text: String) -> TextColumn {
        var paragraph: Paragraph
        var buf = String()
        for scalar in text.unicodeScalars {
            if font.stringWidth(buf + String(scalar)) > w {
                paragraph = Paragraph()
                paragraph.add(TextLine(font, buf))
                addParagraph(paragraph)
                buf = ""
            }
            buf.append(String(scalar))
        }
        paragraph = Paragraph()
        paragraph.add(TextLine(font, buf))
        return addParagraph(paragraph)
    }
}   // End of TextColumn.swift
