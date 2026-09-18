/**
 * TextFrame.swift
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
import Foundation

///
/// Paragraphs of text lines, wrapped at the width of the frame, with an optional
/// border. A frame with a height draws as much of the text as fits and keeps the
/// rest for the next frame, so text flows from frame to frame. A frame without a
/// height draws all of the text. Drawing consumes the text: a second drawOn
/// draws what is left, so build a new frame to draw the same text again.
///
/// Use a TextFrame for text that continues from one frame to the next: the
/// columns of an article, or the pages of a long text. Use a TextColumn to
/// draw paragraphs in one place, with justified text or a rotation, and a
/// TextBlock for one run of text in one font. Please see Example_03 and
/// Example_47.
///
public class TextFrame : Drawable {
    private var paragraphs: [Paragraph]
    private var x: Float = 0.0
    private var y: Float = 0.0
    private var w: Float = 0.0
    private var h: Float = 0.0
    private var paragraphGap: Float = 0.0
    private var hasParagraphGap = false
    private var border = false
    private var borderColor: [Float] = [0.0, 0.0, 0.0]
    private var borderWidth: Float = 0.5
    private var borderPattern = "[] 0"

    // The text that is not drawn yet starts at this paragraph, at this text line
    // of the paragraph and at this token of the text line. The tokens are nil
    // when the text line has not been started.
    private var paragraphIndex = 0
    private var lineIndex = 0
    private var tokens: [String]?
    private var tokenIndex = 0

    // The row of text being drawn: where the text goes next, whether the row can
    // take more text, whether the frame has a row yet, and where the next row goes.
    private var xText: Float = 0.0
    private var yText: Float = 0.0
    private var rowOpen = false
    private var rowPlaced = false
    private var nextBaseline: Float = 0.0
    private var startsParagraph = false     // The next row starts a paragraph

    // The text of the row being drawn, drawn when the row is complete, so that it
    // can be aligned: each part is a text line with some of its text.
    private var row = [RowPart]()

    private struct RowPart {
        let textLine: TextLine
        let text: String
        let x: Float
        let paragraph: Paragraph
        let startsParagraph: Bool   // The first text of the paragraph
        let endsTextLine: Bool      // The last text of the text line
    }

    /// Creates a text frame from paragraphs of text lines. An empty line separates
    /// the paragraphs unless setParagraphGap sets another gap.
    public init(_ paragraphs: [Paragraph]) {
        self.paragraphs = paragraphs
    }

    /// Creates a text frame from strings, one paragraph each, in the font at its
    /// size. An empty line separates the paragraphs.
    public init(_ f1: Font, _ inputList: [String]) {
        self.paragraphs = inputList.map { Paragraph(TextLine(f1, $0)) }
    }

    /// Sets the location of the top left corner of this text frame.
    @discardableResult
    public func setLocation(_ x: Float, _ y: Float) -> Self {
        self.x = x
        self.y = y
        return self
    }

    /// Sets the width at which the lines wrap.
    @discardableResult
    public func setWidth(_ w: Float) -> TextFrame {
        self.w = w
        return self
    }

    /// Sets the height of this text frame. With a height of 0, the default, the
    /// frame draws all of its text.
    @discardableResult
    public func setHeight(_ h: Float) -> TextFrame {
        self.h = h
        return self
    }

    /// Returns the width of this text frame.
    public func getWidth() -> Float {
        return self.w
    }

    /// Returns the height of this text frame.
    public func getHeight() -> Float {
        return self.h
    }

    /// Sets the space between paragraphs, in points, 0 or more: from the bottom of
    /// the text of a paragraph to the top of the text of the next, so paragraphs
    /// never overlap. The default is one empty line in the size of the next
    /// paragraph, so a heading is not followed by an empty line of its own size.
    /// A negative gap is taken as 0.
    @discardableResult
    public func setParagraphGap(_ paragraphGap: Float) -> TextFrame {
        self.paragraphGap = max(0.0, paragraphGap)
        self.hasParagraphGap = true
        return self
    }

    /// Sets whether a border is drawn around this text frame.
    @discardableResult
    public func setBorders(_ border: Bool) -> TextFrame {
        self.border = border
        return self
    }

    /// Sets the border color as a 0xRRGGBB value and draws a border around this text frame. Color.transparent removes the border.
    @discardableResult
    public func setBorderColor(_ color: Int32) -> TextFrame {
        if color == Color.transparent {
            self.border = false
            return self
        }
        let r = Float(((color >> 16) & 0xff))/255.0
        let g = Float(((color >>  8) & 0xff))/255.0
        let b = Float(((color)       & 0xff))/255.0
        return setBorderColor([r, g, b])
    }

    /// Sets the border color from an array of red, green and blue values and draws a border around this text frame.
    @discardableResult
    public func setBorderColor(_ borderColor: [Float]) -> TextFrame {
        self.borderColor = borderColor
        self.border = true
        return self
    }

    /// Sets the border width.
    @discardableResult
    public func setBorderWidth(_ borderWidth: Float) -> TextFrame {
        self.borderWidth = borderWidth
        return self
    }

    /// Sets the dash pattern of the border, for example "[3] 0".
    @discardableResult
    public func setBorderDashPattern(_ borderPattern: String) -> TextFrame {
        self.borderPattern = borderPattern
        return self
    }

    /// Returns true if some of the text has not been drawn yet.
    public func hasMoreText() -> Bool {
        return paragraphIndex < paragraphs.count
    }

    ///
    /// Draws the text on the page: all of it when this frame has no height, or as
    /// much as fits in the height, keeping the rest for the next frame. The first
    /// line of a frame is drawn even when it does not fit, so the text always
    /// flows, and a word wider than the frame is broken. With no page nothing is
    /// drawn, the paragraphs get their coordinates and the text is kept.
    ///
    /// - Parameter page: the page to draw on.
    /// - Returns: the x and y coordinates of the bottom right corner of this frame,
    ///   or of the text when the frame has no height.
    ///
    @discardableResult
    public func drawOn(_ page: Page?) -> [Float] {
        let startParagraph = paragraphIndex
        let startLine = lineIndex
        let startTokens = tokens
        let startToken = tokenIndex

        var bottom = drawParagraphs(page)
        if h > 0.0 {
            bottom = y + h
        }
        if border {
            let rect = Rect(x, y, w, bottom - y)
            rect.setBorderColor(borderColor)
            rect.setBorderWidth(borderWidth)
            rect.setBorderDashPattern(borderPattern)
            rect.drawOn(page)
        }

        if page == nil {
            paragraphIndex = startParagraph
            lineIndex = startLine
            tokens = startTokens
            tokenIndex = startToken
        }
        return [x + w, bottom]
    }

    // Draws the text that is left, as much of it as fits in the height of the
    // frame, and returns the bottom of the text drawn.
    private func drawParagraphs(_ page: Page?) -> Float {
        xText = x
        rowOpen = false
        rowPlaced = false
        startsParagraph = false
        row.removeAll()
        var bottom = y
        while paragraphIndex < paragraphs.count {
            let paragraph = paragraphs[paragraphIndex]
            while lineIndex < paragraph.lines.count {
                let textLine = paragraph.lines[lineIndex]
                if !rowOpen && !openRow(textLine) {
                    drawRow(page, false)
                    return bottom
                }
                if tokens == nil {
                    if lineIndex == 0 {
                        paragraph.x1 = x
                        paragraph.y1 = yText - textLine.font!.getAscent(textLine.fontSize)
                        paragraph.xText = xText
                        paragraph.yText = yText
                    }
                    tokens = tokenize(textLine)
                    tokenIndex = 0
                }
                if !drawTokens(page, paragraph, textLine) {
                    drawRow(page, false)
                    return bottom
                }
                paragraph.x2 = xText
                paragraph.y2 = yText + textLine.font!.getDescent(textLine.fontSize)
                bottom = paragraph.y2
                tokens = nil
                tokenIndex = 0
                lineIndex += 1
            }
            drawRow(page, true)
            xText = x
            rowOpen = false
            if let lastLine = paragraph.lines.last {
                // The next paragraph starts below the descent of this one, after the gap.
                nextBaseline = yText + lastLine.font!.getDescent(lastLine.fontSize)
                startsParagraph = true
            }
            paragraphIndex += 1
            lineIndex = 0
        }
        return bottom
    }

    // Starts a row of text for the text line, below the previous row or at the
    // top of the frame. Returns false when the row does not fit in the height of
    // the frame. The first row of a frame always fits, so the text keeps flowing.
    private func openRow(_ textLine: TextLine) -> Bool {
        var baseline = y + textLine.font!.getAscent(textLine.fontSize)
        if rowPlaced {
            baseline = nextBaseline
            if startsParagraph {
                // The gap, one empty line of this text by default, then its ascent.
                let gap = hasParagraphGap ? paragraphGap : textLine.getHeight()
                baseline += gap + textLine.font!.getAscent(textLine.fontSize)
            }
        }
        if h > 0.0 && rowPlaced &&
                (baseline + textLine.font!.getDescent(textLine.fontSize)) > (y + h) {
            return false
        }
        xText = x
        yText = baseline
        rowOpen = true
        rowPlaced = true
        startsParagraph = false
        return true
    }

    // Draws the tokens of the text line that are left, wrapping them at the width
    // of the frame. Returns false when a row does not fit in the height of the
    // frame; the tokens that are left stay for the next frame.
    private func drawTokens(_ page: Page?, _ paragraph: Paragraph, _ textLine: TextLine) -> Bool {
        let font = textLine.font!
        let fallbackFont = textLine.fallbackFont
        let fontSize = textLine.fontSize
        var buf = String()
        var runLength: Float = 0.0
        while tokenIndex < tokens!.count {
            if !rowOpen && !openRow(textLine) {
                return false
            }
            let token = tokens![tokenIndex]
            // The token is measured without the space that follows it, as in
            // TextColumn: a row is as wide as the text it shows.
            if (runLength + TextFrame.width(textLine, token)) <= ((x + w) - xText) {
                buf.append(token)
                buf.append(Single.space)
                runLength += TextFrame.width(textLine, token + Single.space)
                tokenIndex += 1
                continue
            }
            if buf.isEmpty && xText == x {
                // The token does not fit in an empty row, so the row takes as much of it as fits.
                let head = headThatFits(textLine, token)
                buf.append(head)
                let headCount = head.unicodeScalars.count
                if headCount == token.unicodeScalars.count {
                    tokenIndex += 1
                } else {
                    tokens![tokenIndex] = String(String.UnicodeScalarView(token.unicodeScalars.dropFirst(headCount)))
                }
            }
            addToRow(paragraph, textLine, buf, false)
            drawRow(page, false)
            buf = ""
            runLength = 0.0
            xText = x
            rowOpen = false
            nextBaseline = yText + textLine.getHeight()
        }
        addToRow(paragraph, textLine, buf, true)
        xText += font.stringWidth(fallbackFont, fontSize, buf)
        return true
    }

    // Returns the longest start of the token that fits in the width of the frame,
    // and at least the first character of the token.
    private func headThatFits(_ textLine: TextLine, _ token: String) -> String {
        let scalars = Array(token.unicodeScalars)
        var end = 1
        while end < scalars.count {
            let next = String(String.UnicodeScalarView(scalars[0...end]))
            if textLine.font!.stringWidth(textLine.fallbackFont, textLine.fontSize, next) > w {
                break
            }
            end += 1
        }
        return String(String.UnicodeScalarView(scalars[0..<end]))
    }

    // Adds the string to the row, at the current text position.
    private func addToRow(_ paragraph: Paragraph, _ textLine: TextLine, _ str: String, _ endsTextLine: Bool) {
        let first = row.isEmpty && lineIndex == 0 && xText == paragraph.xText
                && yText == paragraph.yText
        row.append(RowPart(textLine: textLine, text: str, x: xText, paragraph: paragraph,
                startsParagraph: first, endsTextLine: endsTextLine))
    }

    // Draws the parts of the row, with every setting of their text lines, including
    // the vertical offset and the link, as TextColumn does. A paragraph aligned to
    // the right or to the center moves the row, and a justified one widens the
    // spaces of every row but its last.
    private func drawRow(_ page: Page?, _ lastRowOfParagraph: Bool) {
        if row.isEmpty {
            return
        }
        let paragraph = row[0].paragraph
        let alignment = paragraph.explicitAlignment ? paragraph.alignment : Alignment.LEFT
        let last = row[row.count - 1]
        let rowWidth = last.x + TextFrame.width(last.textLine, TextFrame.trimTrailingSpaces(last.text)) - x

        if alignment == Alignment.JUSTIFY && !lastRowOfParagraph {
            drawJustifiedRow(page, rowWidth)
        } else {
            var shift: Float = 0.0
            if alignment == Alignment.RIGHT {
                shift = w - rowWidth
            } else if alignment == Alignment.CENTER {
                shift = (w - rowWidth) / 2.0
            }
            for part in row {
                part.textLine.copyWithText(part.text).setLocation(part.x + shift, yText).drawOn(page)
                if part.startsParagraph {
                    part.paragraph.xText += shift
                }
                if part.endsTextLine {
                    part.paragraph.x2 = part.x + TextFrame.width(part.textLine, part.text) + shift
                }
            }
        }
        row.removeAll()
    }

    // Draws the words of the row one by one, with the width left in the row shared
    // out among the spaces between them.
    private func drawJustifiedRow(_ page: Page?, _ rowWidth: Float) {
        let texts = row.map { Array($0.text.unicodeScalars) }
        var spaces = 0
        for i in 0..<texts.count {
            for j in 0..<texts[i].count where texts[i][j] == " " && hasWordAfter(texts, i, j + 1) {
                spaces += 1
            }
        }
        let dx: Float = (spaces > 0) ? (w - rowWidth) / Float(spaces) : 0.0
        var xWord = x
        for i in 0..<row.count {
            let part = row[i]
            let text = texts[i]
            var start = 0
            while start < text.count {
                var end = start
                while end < text.count && text[end] != " " {
                    end += 1
                }
                if end > start {
                    let word = String(String.UnicodeScalarView(text[start..<end]))
                    part.textLine.copyWithText(word).setLocation(xWord, yText).drawOn(page)
                    xWord += TextFrame.width(part.textLine, word)
                }
                if end < text.count {
                    xWord += TextFrame.width(part.textLine, Single.space)
                    if hasWordAfter(texts, i, end + 1) {
                        xWord += dx
                    }
                }
                start = end + 1
            }
            if part.endsTextLine {
                part.paragraph.x2 = xWord
            }
        }
    }

    // Returns true when a word follows this position of the row.
    private func hasWordAfter(_ texts: [[Unicode.Scalar]], _ partIndex: Int, _ charIndex: Int) -> Bool {
        for i in partIndex..<texts.count {
            var j = (i == partIndex) ? charIndex : 0
            while j < texts[i].count {
                if texts[i][j] != " " {
                    return true
                }
                j += 1
            }
        }
        return false
    }

    private static func trimTrailingSpaces(_ text: String) -> String {
        var scalars = Array(text.unicodeScalars)
        while let last = scalars.last, last == " " {
            scalars.removeLast()
        }
        return String(String.UnicodeScalarView(scalars))
    }

    private static func width(_ textLine: TextLine, _ text: String) -> Float {
        return textLine.font!.stringWidth(textLine.fallbackFont, textLine.fontSize, text)
    }

    // Splits the text of the text line into words, or, for CJK text, which has no
    // spaces between its words, into runs of characters that fit in the width.
    private func tokenize(_ textLine: TextLine) -> [String] {
        if !textLine.text!.isCJK() {
            return textLine.text!.splitOnWhitespace()
        }
        var list = [String]()
        var buf = String()
        for scalar in textLine.text!.unicodeScalars {
            if textLine.font!.stringWidth(textLine.fallbackFont, textLine.fontSize, buf + String(scalar)) <= w {
                buf.unicodeScalars.append(scalar)
            } else {
                if !buf.isEmpty {
                    list.append(buf)
                }
                buf = String(scalar)
            }
        }
        if !buf.isEmpty {
            list.append(buf)
        }
        return list
    }
}   // End of TextFrame.swift
