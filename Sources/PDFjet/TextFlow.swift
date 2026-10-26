/**
 * TextFlow.swift
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
import Foundation

///
/// Headings, paragraphs and images flowed onto as many pages as they need, one
/// below the other between the margins: a report, a manual, a long letter.
/// Paragraphs break across pages between their lines, as a TextFrame breaks
/// them; an image goes whole, to the top of the next page when it does not fit
/// below the text, with its caption; a heading goes to the next page with what
/// follows it rather than end a page. The pages are created detached and added
/// to a list, so that a footer or a page number can be drawn on each before
/// they are added to the PDF.
///
/// Use a TextFrame to place text by hand, in frames on the same page or on
/// others, the text flowing from one to the next; use TextFlow to pour the text
/// and the images in, page after page. The order they are added in is the
/// reading order of a PDF/UA document: the paragraphs are tagged as a TextFrame
/// tags them, the images as figures with their alternate descriptions.
/// Use Markdown instead for a text written in Markdown. Please see Example_22.
///
public class TextFlow {
    private let pdf: PDF
    private let pageSize: PageSize
    private var top: Float = 72.0
    private var right: Float = 72.0
    private var bottom: Float = 72.0
    private var left: Float = 72.0
    private var firstPageTop: Float = 0.0
    private var items = [Item]()

    // One thing added to the flow: paragraphs, an image or a space.
    private struct Item {
        var paragraphs = [Paragraph]()
        var gap: Float = 0.0
        var hasGap = false
        var image: Image? = nil
        var alignment = Alignment.LEFT
        var space: Float = 0.0
    }

    ///
    /// Creates a flow of text and images onto pages of the size, with margins
    /// of 72 points, an inch, on every side.
    ///
    /// - Parameter pdf: the PDF document.
    /// - Parameter pageSize: the page size, for example Letter.PORTRAIT.
    ///
    public init(_ pdf: PDF, _ pageSize: PageSize) {
        self.pdf = pdf
        self.pageSize = pageSize
    }

    ///
    /// Sets the margins of every side of the pages, in points. The text and the
    /// images are as wide as the page less the left and right margins.
    ///
    /// - Parameter margin: the margin of every side.
    /// - Returns: this flow.
    ///
    @discardableResult
    public func setMargins(_ margin: Float) -> TextFlow {
        return setMargins(margin, margin, margin, margin)
    }

    ///
    /// Sets the margins of the pages, in points: top, right, bottom and left,
    /// the order CSS gives them.
    ///
    /// - Parameter top: the top margin.
    /// - Parameter right: the right margin.
    /// - Parameter bottom: the bottom margin.
    /// - Parameter left: the left margin.
    /// - Returns: this flow.
    ///
    @discardableResult
    public func setMargins(_ top: Float, _ right: Float, _ bottom: Float, _ left: Float) -> TextFlow {
        self.top = top
        self.right = right
        self.bottom = bottom
        self.left = left
        return self
    }

    ///
    /// Sets where the flow starts on the first page that drawOn(first, pages)
    /// is given, below what is drawn there already, such as a heading or a
    /// letterhead. Without it the flow starts at the top margin.
    ///
    /// - Parameter y: the y coordinate the flow starts at on the first page.
    /// - Returns: this flow.
    ///
    @discardableResult
    public func setFirstPageTop(_ y: Float) -> TextFlow {
        self.firstPageTop = y
        return self
    }

    ///
    /// Adds a paragraph: a heading, when its structure type is StructElem.H1 to
    /// StructElem.H6, or a paragraph of text, a caption, a line.
    ///
    /// - Parameter paragraph: the paragraph to add.
    /// - Returns: this flow.
    ///
    @discardableResult
    public func add(_ paragraph: Paragraph) -> TextFlow {
        items.append(Item(paragraphs: [paragraph]))
        return self
    }

    ///
    /// Adds paragraphs with gap points between them, which flow across pages as
    /// the text of a TextFrame does.
    ///
    /// - Parameter paragraphs: the paragraphs to add.
    /// - Parameter gap: the space between two paragraphs, in points.
    /// - Returns: this flow.
    ///
    @discardableResult
    public func add(_ paragraphs: [Paragraph], _ gap: Float) -> TextFlow {
        items.append(Item(paragraphs: paragraphs, gap: gap, hasGap: true))
        return self
    }

    ///
    /// Adds an image at the left margin, or in the center of the text, or at
    /// the right margin: Alignment.LEFT, Alignment.CENTER or Alignment.RIGHT.
    /// An image wider than the text is made as wide as the text, and one taller
    /// than the space between the top and the bottom margins as tall as that
    /// space, keeping its shape; it is never cut. A paragraph of
    /// StructElem.CAPTION added right after it, after a space or not, is its
    /// caption, and goes to the next page with it when the two do not fit.
    ///
    /// - Parameter image: the image to add.
    /// - Parameter alignment: where the image goes across the text.
    /// - Returns: this flow.
    ///
    @discardableResult
    public func add(_ image: Image, _ alignment: Alignment = Alignment.LEFT) -> TextFlow {
        items.append(Item(image: image, alignment: alignment))
        return self
    }

    ///
    /// Adds points of space below what was added before. A space at the top of
    /// a page is left out, as the gap between two paragraphs is.
    ///
    /// - Parameter points: the space, in points.
    /// - Returns: this flow.
    ///
    @discardableResult
    public func addSpace(_ points: Float) -> TextFlow {
        if points > 0.0 {
            items.append(Item(space: points))
        }
        return self
    }

    ///
    /// Draws what was added on as many new pages as it needs, created detached
    /// and added to the list; add them to the PDF afterwards.
    ///
    /// - Parameter pages: the list that receives the new pages.
    /// - Returns: the x and y coordinates of the bottom right corner of what was
    ///   drawn on the last page, where something drawn after it can go.
    ///
    @discardableResult
    public func drawOn(_ pages: inout [Page]) -> [Float] {
        return drawOn(nil, &pages)
    }

    ///
    /// Draws what was added as drawOn(pages) does, starting on first, a page
    /// with other content above the flow, which setFirstPageTop puts the flow
    /// under. The next pages are new and are added to the list; first is not.
    ///
    /// - Parameter first: the page the flow starts on.
    /// - Parameter pages: the list that receives the new pages.
    /// - Returns: the x and y coordinates of the bottom right corner of what was
    ///   drawn on the last page, where something drawn after it can go.
    ///
    @discardableResult
    public func drawOn(_ first: Page?, _ pages: inout [Page]) -> [Float] {
        let width = pageSize.getWidth() - left - right
        let height = pageSize.getHeight() - top - bottom
        if !top.isFinite || !right.isFinite || !bottom.isFinite || !left.isFinite
                || !firstPageTop.isFinite || width <= 0.0 || height <= 0.0 {
            pdf.fail("The margins of the flow must be numbers that leave room on the page for the text.")
            return [left, top]
        }
        var page = first
        var y = top
        var atTop = true        // Nothing drawn yet on this page: a space here is left out
        if first != nil && firstPageTop > top {
            y = firstPageTop
            atTop = false
        }
        let limit = pageSize.getHeight() - bottom
        for (i, item) in items.enumerated() {
            if item.space > 0.0 {
                if page != nil && !atTop {
                    y += item.space
                    if y >= limit {
                        page = newPage(&pages)  // The space ends the page, and is not carried over
                        y = top
                        atTop = true
                    }
                }
            } else if let image = item.image {
                fitImage(image, width, height)
                var needed = image.getHeight()
                let withCaption = needed + captionHeight(i + 1, width)
                if withCaption <= height {
                    needed = withCaption    // The image and its caption go together
                }
                if page == nil || (!atTop && y + needed > limit) {
                    page = newPage(&pages)
                    y = top
                }
                var x = left
                if item.alignment == Alignment.CENTER {
                    x += (width - image.getWidth()) / 2.0
                } else if item.alignment == Alignment.RIGHT {
                    x += width - image.getWidth()
                }
                image.setLocation(x, y)
                image.drawOn(page)
                y += image.getHeight()
                atTop = false
            } else {
                var needed = firstLineHeight(item.paragraphs)
                if needed == 0.0 {
                    continue    // No text to draw
                }
                if let last = item.paragraphs.last, isHeading(last) {
                    // The heading, and the first line or the image after it
                    let withNext = textHeight(item, width) + nextHeight(i + 1, width, height)
                    if withNext <= height {
                        needed = withNext
                    }
                }
                if page == nil || (!atTop && y + needed > limit) {
                    page = newPage(&pages)
                    y = top
                }
                let frame = self.frame(item, width)
                frame.setLocation(left, y).setHeight(limit - y).drawOn(page)
                while frame.hasMoreText() {
                    page = newPage(&pages)
                    y = top
                    frame.setLocation(left, y).setHeight(limit - y).drawOn(page)
                }
                y = frame.getTextBottom()
                atTop = false
            }
        }
        return [left + width, y]
    }

    private func newPage(_ pages: inout [Page]) -> Page {
        let page = Page(pdf, pageSize, false)
        pages.append(page)
        return page
    }

    // A text frame of the paragraphs of the item, as wide as the text.
    private func frame(_ item: Item, _ width: Float) -> TextFrame {
        let frame = TextFrame(item.paragraphs).setWidth(width)
        if item.hasGap {
            frame.setParagraphGap(item.gap)
        }
        return frame
    }

    // The height of the paragraphs of the item, all of them drawn in one frame.
    private func textHeight(_ item: Item, _ width: Float) -> Float {
        let frame = self.frame(item, width)
        frame.setLocation(0.0, 0.0)
        frame.drawOn(nil)       // With no page the text is measured and kept
        return frame.getTextBottom()
    }

    // The height that has to fit below a heading so that it does not end a
    // page: the spaces after it, and the first line of the paragraphs or the
    // image that comes next.
    private func nextHeight(_ index: Int, _ width: Float, _ height: Float) -> Float {
        var total: Float = 0.0
        for item in items[index...] {
            if item.space > 0.0 {
                total += item.space
            } else if let image = item.image {
                let w = image.getWidth()
                var h = image.getHeight()
                if w > width {
                    h = h * (width / w)
                }
                return total + min(h, height)
            } else {
                let line = firstLineHeight(item.paragraphs)
                if line > 0.0 {
                    return total + line
                }
            }
        }
        return total
    }

    // The height of the caption of an image, a paragraph of StructElem.CAPTION
    // added right after it, and of the spaces before it; 0 when the image has
    // no caption.
    private func captionHeight(_ index: Int, _ width: Float) -> Float {
        var total: Float = 0.0
        for item in items[index...] {
            if item.space > 0.0 {
                total += item.space
            } else if item.image != nil {
                return 0.0
            } else {
                guard let first = item.paragraphs.first, first.structureType == StructElem.CAPTION else {
                    return 0.0
                }
                return total + textHeight(item, width)
            }
        }
        return 0.0
    }

    // Makes the image as wide as the text at most, and as tall as the space
    // between the margins at most, keeping its shape.
    private func fitImage(_ image: Image, _ width: Float, _ height: Float) {
        if image.getWidth() > width {
            image.resizeWidth(width)
        }
        if image.getHeight() > height {
            image.resizeHeight(height)
        }
    }

    // The height of the first line of the paragraphs, its ascent and descent,
    // which a TextFrame places at its top; 0 when they have no text.
    private func firstLineHeight(_ paragraphs: [Paragraph]) -> Float {
        for paragraph in paragraphs {
            if let line = paragraph.lines.first, let font = line.font {
                return font.getAscent(line.fontSize) + font.getDescent(line.fontSize)
            }
        }
        return 0.0
    }

    // Whether the paragraph is a heading, StructElem.H1 to H6.
    private func isHeading(_ paragraph: Paragraph) -> Bool {
        switch paragraph.structureType {
        case .H1, .H2, .H3, .H4, .H5, .H6:
            return true
        default:
            return false
        }
    }
}   // End of TextFlow.swift
