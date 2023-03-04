/**
 *  TextColumn.swift
 *
Copyright 2023 Innovatics Inc.
*/
import Foundation


///
/// Used to create text column objects and draw them on a page.
///
/// Please see Example_10.
///
public class TextColumn : Drawable {

    var alignment = Align.LEFT
    var rotate = 0

    private var x: Float = 0.0      // This variable is set in the beginning and only reset after the drawOn
    private var y: Float = 0.0      // This variable is set in the beginning and only reset after the drawOn
    private var w: Float = 0.0
    private var h: Float = 0.0

    private var x1: Float = 0.0
    private var y1: Float = 0.0
    private var lineHeight: Float = 0.0

    private var spaceBetweenLines: Float = 1.0
    private var spaceBetweenParagraphs: Float = 2.0

    private var paragraphs: [Paragraph]

    private var lineBetweenParagraphs = false


    ///
    /// Create a text column object.
    ///
    public init() {
        self.paragraphs = [Paragraph]()
    }


    ///
    /// Create a text column object and set the rotation angle.
    ///
    /// @param rotateByDegrees the specified rotation angle in degrees.
    ///
    public init(_ rotateByDegrees: Int) {
        if rotateByDegrees != ClockWise._0_degrees &&
                rotateByDegrees != ClockWise._90_degrees &&
                rotateByDegrees != ClockWise._270_degrees {
            // TODO:
            Swift.print("Invalid rotation angle. Please use 0, 90 or 270 degrees.")
        }
        self.rotate = rotateByDegrees
        self.paragraphs = [Paragraph]()
    }


    ///
    /// Sets the lineBetweenParagraphs private variable value.
    /// If the value is set to true - an empty line will be inserted between the current and next paragraphs.
    ///
    /// @param lineBetweenParagraphs the specified Bool value.
    ///
    public func setLineBetweenParagraphs(_ lineBetweenParagraphs: Bool) {
        self.lineBetweenParagraphs = lineBetweenParagraphs
    }


    public func setSpaceBetweenLines(_ spaceBetweenLines: Float) {
        self.spaceBetweenLines = spaceBetweenLines
    }


    public func setSpaceBetweenParagraphs(_ spaceBetweenParagraphs: Float) {
        self.spaceBetweenParagraphs = spaceBetweenParagraphs
    }


    public func setPosition(_ x: Float, _ y: Float) {
        setLocation(x, y)
    }


    ///
    /// Sets the position of this text column on the page.
    ///
    /// @param x the x coordinate of the top left corner of this text column when drawn on the page.
    /// @param y the y coordinate of the top left corner of this text column when drawn on the page.
    ///
    @discardableResult
    public func setLocation(_ x: Float, _ y: Float) -> TextColumn {
        self.x = x
        self.y = y
        self.x1 = x
        self.y1 = y
        return self
    }


    ///
    /// Sets the size of this text column.
    ///
    /// @param w the width of this text column.
    /// @param h the height of this text column.
    ///
    public func setSize(_ w: Float, _ h: Float) {
        self.w = w
        self.h = h
    }


    ///
    /// Sets the desired width of this text column.
    ///
    /// @param w the width of this text column.
    ///
    public func setWidth(_ w: Float) {
        self.w = w
    }


    ///
    /// Sets the text alignment.
    ///
    /// @param alignment the specified alignment code.
    /// Supported values: Align.LEFT, Align.RIGHT. Align.CENTER and Align.JUSTIFY
    ///
    public func setAlignment(_ alignment: UInt32) {
        self.alignment = alignment
    }


    ///
    /// Sets the spacing between the lines in this text column.
    ///
    /// @param spacing the specified spacing value.
    ///
    public func setLineSpacing(_ spacing: Float) {
        self.spaceBetweenLines = spacing
    }


    ///
    /// Adds a new paragraph to this text column.
    ///
    /// @param paragraph the new paragraph object.
    ///
    public func addParagraph(_ paragraph: Paragraph) {
        self.paragraphs.append(paragraph)
    }


    ///
    /// Removes the last paragraph added to this text column.
    ///
    public func removeLastParagraph() {
        if self.paragraphs.count >= 1 {
            self.paragraphs.removeLast()
        }
    }


    ///
    /// Returns dimension object containing the width and height of this component.
    /// Please see Example_29.
    ///
    /// @return dimension object containing the width and height of this component.
    ///
    public func getSize() -> Dimension {
        let xy = drawOn(nil)
        return Dimension(self.w, xy[1] - self.y)
    }


    ///
    /// Draws this text column on the specified page if the 'draw' Bool value is 'true'.
    ///
    /// @param page the page to draw this text column on.
    /// @param draw the Bool value that specified if the text column should actually be drawn on the page.
    /// @return the point with x and y coordinates of the location where to draw the next component.
    ///
    @discardableResult
    public func drawOn(_ page: Page?) -> [Float] {
        var xy: [Float] = [0.0, 0.0]
        for paragraph in paragraphs {
            self.alignment = paragraph.alignment
            xy = drawParagraphOn(page, paragraph)
        }
        // Restore the original location
        setLocation(self.x, self.y)
        return xy
    }


    private func drawParagraphOn(
            _ page: Page?,
            _ paragraph: Paragraph) -> [Float] {

        self.lineHeight = paragraph.list![0].font!.bodyHeight + spaceBetweenLines
        if rotate == ClockWise._0_degrees {
            self.y1 += paragraph.list![0].font!.ascent
        }
        else if rotate == ClockWise._90_degrees {
            self.x1 += paragraph.list![0].font!.ascent
        }
        else if rotate == ClockWise._270_degrees {
            self.x1 -= paragraph.list![0].font!.ascent
        }

        var list = [TextLine]()
        var runLength: Float = 0.0
        for line in paragraph.list! {
            let tokens = line.text!.components(separatedBy: .whitespaces)
            var text: TextLine?
            for token in tokens {
                text = TextLine(line.font!, token)
                        .setColor(line.getColor())
                        .setUnderline(line.getUnderline())
                        .setStrikeout(line.getStrikeout())
                        .setVerticalOffset(line.getVerticalOffset())
                        .setURIAction(line.getURIAction())
                        .setGoToAction(line.getGoToAction())
                        .setFallbackFont(line.getFallbackFont())
                runLength += line.font!.stringWidth(line.fallbackFont, token)
                if runLength < w {
                    list.append(text!)
                    runLength += line.font!.stringWidth(line.fallbackFont, Single.space)
                }
                else {
                    if page != nil {
                        drawLineOfText(page!, list)
                    }
                    moveToNextLine()
                    list.removeAll()
                    list.append(text!)
                    runLength = line.font!.stringWidth(line.fallbackFont, token + Single.space)
                }
            }
            if line.getTrailingSpace() == false {
                runLength -= line.font!.stringWidth(line.fallbackFont, Single.space)
                text!.setTrailingSpace(false)
            }
        }
        if page != nil {
            drawNonJustifiedLine(page!, list)
        }

        if lineBetweenParagraphs {
            return moveToNextLine()
        }

        return moveToNextParagraph(self.spaceBetweenParagraphs)
    }


    @discardableResult
    private func moveToNextLine() -> [Float] {
        if rotate == 0 {
            x1 = x
            y1 += lineHeight
        }
        else if rotate == 90 {
            x1 += lineHeight
            y1 = y
        }
        else if rotate == 270 {
            x1 -= lineHeight
            y1 = y
        }
        return [x1, y1]
    }


    private func moveToNextParagraph(_ spaceBetweenParagraphs: Float) -> [Float] {
        if rotate == ClockWise._0_degrees {
            x1 = x
            y1 += spaceBetweenParagraphs
        }
        else if rotate == ClockWise._90_degrees {
            x1 += spaceBetweenParagraphs
            y1 = y
        }
        else if rotate == ClockWise._270_degrees {
            x1 -= spaceBetweenParagraphs
            y1 = y
        }
        return [x1, y1]
    }


    @discardableResult
    private func drawLineOfText(_ page: Page, _ list: [TextLine]) -> [Float] {
        if alignment == Align.JUSTIFY {
            var sumOfWordWidths: Float = 0.0
            for textLine in list {
                sumOfWordWidths +=
                        textLine.font!.stringWidth(textLine.fallbackFont, textLine.text)
            }

            let dx = (w - sumOfWordWidths) / Float(list.count - 1)
            for textLine in list {
                textLine.setLocation(x1, y1 + textLine.getVerticalOffset())
                if textLine.getGoToAction() != nil {
                    page.addAnnotation(Annotation(
                            nil,                        // The URI
                            textLine.getGoToAction(),   // The destination name
                            x,
                            y - textLine.font!.ascent,
                            x + textLine.font!.stringWidth(textLine.fallbackFont, textLine.text),
                            y + textLine.font!.descent,
                            nil,
                            nil,
                            nil))
                }

                if rotate == 0 {
                    textLine.setTextDirection(0).drawOn(page)
                    x1 += textLine.font!.stringWidth(textLine.fallbackFont, textLine.text) + dx
                }
                else if rotate == 90 {
                    textLine.setTextDirection(90).drawOn(page)
                    y1 -= textLine.font!.stringWidth(textLine.fallbackFont, textLine.text) + dx
                }
                else if rotate == 270 {
                    textLine.setTextDirection(270).drawOn(page)
                    y1 += textLine.font!.stringWidth(textLine.fallbackFont, textLine.text) + dx
                }
            }
        }
        else {
            return drawNonJustifiedLine(page, list)
        }

        return [x1, y1]
    }


    @discardableResult
    private func drawNonJustifiedLine(_ page: Page, _ list: [TextLine]) -> [Float] {

        var runLength: Float = 0.0
        for i in 0..<list.count {
            let textLine = list[i]
            if i < (list.count - 1) {
                if textLine.getTrailingSpace() {
                    textLine.setText(textLine.getText()! + Single.space)
                }
            }
            runLength += textLine.font!.stringWidth(textLine.fallbackFont, textLine.text)
        }

        if alignment == Align.CENTER {
            if rotate == ClockWise._0_degrees {
                x1 = x + ((w - runLength) / 2)
            }
            else if rotate == ClockWise._90_degrees {
                y1 = y - ((w - runLength) / 2)
            }
            else if rotate == ClockWise._270_degrees {
                y1 = y + ((w - runLength) / 2)
            }
        }
        else if alignment == Align.RIGHT {
            if rotate == ClockWise._0_degrees {
                x1 = x + (w - runLength)
            }
            else if rotate == ClockWise._90_degrees {
                y1 = y - (w - runLength)
            }
            else if rotate == ClockWise._270_degrees {
                y1 = y + (w - runLength)
            }
        }

        for textLine in list {
            textLine.setLocation(x1, y1 + textLine.getVerticalOffset())
            if textLine.getGoToAction() != nil {
                page.addAnnotation(Annotation(
                        nil,                        // The URI
                        textLine.getGoToAction(),   // The destination name
                        x,
                        y - textLine.font!.ascent,
                        x + textLine.font!.stringWidth(textLine.fallbackFont, textLine.text!),
                        y + textLine.font!.descent,
                        nil,
                        nil,
                        nil))
            }
            if rotate == ClockWise._0_degrees {
                textLine.setTextDirection(0).drawOn(page)
                x1 += textLine.font!.stringWidth(textLine.fallbackFont, textLine.text)
            }
            else if rotate == ClockWise._90_degrees {
                textLine.setTextDirection(90).drawOn(page)
                y1 -= textLine.font!.stringWidth(textLine.fallbackFont, textLine.text)
            }
            else if rotate == ClockWise._270_degrees {
                textLine.setTextDirection(270).drawOn(page)
                y1 += textLine.font!.stringWidth(textLine.fallbackFont, textLine.text)
            }
        }

        return [x1, y1]
    }


    ///
    /// Adds a new paragraph with Chinese text to this text column.
    ///
    /// @param font the font used by this paragraph.
    /// @param chinese the Chinese text.
    ///
    public func addChineseParagraph(_ font: Font, _ chinese: String) {
        var paragraph: Paragraph
        var buf = String()
        for scalar in chinese.unicodeScalars {
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
        addParagraph(paragraph)
    }


    ///
    /// Adds a new paragraph with Japanese text to this text column.
    ///
    /// @param font the font used by this paragraph.
    /// @param japanese the Japanese text.
    ///
    public func addJapaneseParagraph(_ font: Font, _ japanese: String) {
        addChineseParagraph(font, japanese)
    }

}   // End of TextColumn.swift
