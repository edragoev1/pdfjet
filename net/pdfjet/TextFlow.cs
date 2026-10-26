/*
 * TextFlow.cs
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
using System;
using System.Collections.Generic;

namespace PDFjet.NET {
/// <summary>
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
/// </summary>
public class TextFlow {
    private readonly PDF pdf;
    private readonly PageSize pageSize;
    private float top = 72f;
    private float right = 72f;
    private float bottom = 72f;
    private float left = 72f;
    private float firstPageTop = 0f;
    private readonly List<Item> items = new List<Item>();

    // One thing added to the flow: paragraphs, an image or a space.
    private sealed class Item {
        internal List<Paragraph> paragraphs;
        internal float gap;
        internal bool hasGap;
        internal Image image;
        internal Alignment alignment = Alignment.LEFT;
        internal float space;
    }

    /// <summary>
    /// Creates a flow of text and images onto pages of the size, with margins
    /// of 72 points, an inch, on every side.
    /// </summary>
    /// <param name="pdf">the PDF document.</param>
    /// <param name="pageSize">the page size, for example Letter.PORTRAIT.</param>
    public TextFlow(PDF pdf, PageSize pageSize) {
        this.pdf = pdf;
        this.pageSize = pageSize;
    }

    /// <summary>
    /// Sets the margins of every side of the pages, in points. The text and the
    /// images are as wide as the page less the left and right margins.
    /// </summary>
    /// <param name="margin">the margin of every side.</param>
    /// <returns>this flow.</returns>
    public TextFlow SetMargins(float margin) {
        return SetMargins(margin, margin, margin, margin);
    }

    /// <summary>
    /// Sets the margins of the pages, in points: top, right, bottom and left,
    /// the order CSS gives them.
    /// </summary>
    /// <param name="top">the top margin.</param>
    /// <param name="right">the right margin.</param>
    /// <param name="bottom">the bottom margin.</param>
    /// <param name="left">the left margin.</param>
    /// <returns>this flow.</returns>
    public TextFlow SetMargins(float top, float right, float bottom, float left) {
        this.top = top;
        this.right = right;
        this.bottom = bottom;
        this.left = left;
        return this;
    }

    /// <summary>
    /// Sets where the flow starts on the first page that DrawOn(first, pages)
    /// is given, below what is drawn there already, such as a heading or a
    /// letterhead. Without it the flow starts at the top margin.
    /// </summary>
    /// <param name="y">the y coordinate the flow starts at on the first page.</param>
    /// <returns>this flow.</returns>
    public TextFlow SetFirstPageTop(float y) {
        this.firstPageTop = y;
        return this;
    }

    /// <summary>
    /// Adds a paragraph: a heading, when its structure type is StructElem.H1 to
    /// StructElem.H6, or a paragraph of text, a caption, a line.
    /// </summary>
    /// <param name="paragraph">the paragraph to add.</param>
    /// <returns>this flow.</returns>
    public TextFlow Add(Paragraph paragraph) {
        Item item = new Item();
        item.paragraphs = new List<Paragraph> {paragraph};
        items.Add(item);
        return this;
    }

    /// <summary>
    /// Adds paragraphs with gap points between them, which flow across pages as
    /// the text of a TextFrame does.
    /// </summary>
    /// <param name="paragraphs">the paragraphs to add.</param>
    /// <param name="gap">the space between two paragraphs, in points.</param>
    /// <returns>this flow.</returns>
    public TextFlow Add(List<Paragraph> paragraphs, float gap) {
        Item item = new Item();
        item.paragraphs = paragraphs;
        item.gap = gap;
        item.hasGap = true;
        items.Add(item);
        return this;
    }

    /// <summary>
    /// Adds an image at the left margin. An image wider than the text is made as
    /// wide as the text, and one taller than the space between the top and the
    /// bottom margins as tall as that space, keeping its shape; it is never cut.
    /// A paragraph of StructElem.CAPTION added right after it, after a space or
    /// not, is its caption, and goes to the next page with it when the two do
    /// not fit.
    /// </summary>
    /// <param name="image">the image to add.</param>
    /// <returns>this flow.</returns>
    public TextFlow Add(Image image) {
        return Add(image, Alignment.LEFT);
    }

    /// <summary>
    /// Adds an image as Add(image) does, at the left margin, in the center of
    /// the text, or at the right margin: Alignment.LEFT, Alignment.CENTER or
    /// Alignment.RIGHT.
    /// </summary>
    /// <param name="image">the image to add.</param>
    /// <param name="alignment">where the image goes across the text.</param>
    /// <returns>this flow.</returns>
    public TextFlow Add(Image image, Alignment alignment) {
        Item item = new Item();
        item.image = image;
        item.alignment = alignment;
        items.Add(item);
        return this;
    }

    /// <summary>
    /// Adds points of space below what was added before. A space at the top of
    /// a page is left out, as the gap between two paragraphs is.
    /// </summary>
    /// <param name="points">the space, in points.</param>
    /// <returns>this flow.</returns>
    public TextFlow AddSpace(float points) {
        if (points > 0f) {
            Item item = new Item();
            item.space = points;
            items.Add(item);
        }
        return this;
    }

    /// <summary>
    /// Draws what was added on as many new pages as it needs, created detached
    /// and added to the list; add them to the PDF afterwards.
    /// </summary>
    /// <param name="pages">the list that receives the new pages.</param>
    /// <returns>the x and y coordinates of the bottom right corner of what was
    /// drawn on the last page, where something drawn after it can go.</returns>
    public float[] DrawOn(List<Page> pages) {
        return DrawOn(null, pages);
    }

    /// <summary>
    /// Draws what was added as DrawOn(pages) does, starting on first, a page
    /// with other content above the flow, which SetFirstPageTop puts the flow
    /// under. The next pages are new and are added to the list; first is not.
    /// </summary>
    /// <param name="first">the page the flow starts on.</param>
    /// <param name="pages">the list that receives the new pages.</param>
    /// <returns>the x and y coordinates of the bottom right corner of what was
    /// drawn on the last page, where something drawn after it can go.</returns>
    public float[] DrawOn(Page first, List<Page> pages) {
        float width = pageSize.GetWidth() - left - right;
        float height = pageSize.GetHeight() - top - bottom;
        if (!TextFrame.Finite(top) || !TextFrame.Finite(right) || !TextFrame.Finite(bottom)
                || !TextFrame.Finite(left) || !TextFrame.Finite(firstPageTop) || width <= 0f || height <= 0f) {
            pdf.Fail(new ArgumentException(
                    "The margins of the flow must be numbers that leave room on the page for the text."));
        }
        Page page = first;
        float y = top;
        bool atTop = true;      // Nothing drawn yet on this page: a space here is left out
        if (first != null && firstPageTop > top) {
            y = firstPageTop;
            atTop = false;
        }
        float limit = pageSize.GetHeight() - bottom;
        for (int i = 0; i < items.Count; i++) {
            Item item = items[i];
            if (item.space > 0f) {
                if (page != null && !atTop) {
                    y += item.space;
                    if (y >= limit) {
                        page = NewPage(pages);  // The space ends the page, and is not carried over
                        y = top;
                        atTop = true;
                    }
                }
            } else if (item.image != null) {
                Image image = item.image;
                FitImage(image, width, height);
                float needed = image.GetHeight();
                float withCaption = needed + CaptionHeight(i + 1, width);
                if (withCaption <= height) {
                    needed = withCaption;   // The image and its caption go together
                }
                if (page == null || (!atTop && y + needed > limit)) {
                    page = NewPage(pages);
                    y = top;
                }
                float x = left;
                if (item.alignment == Alignment.CENTER) {
                    x += (width - image.GetWidth()) / 2f;
                } else if (item.alignment == Alignment.RIGHT) {
                    x += width - image.GetWidth();
                }
                image.SetLocation(x, y);
                image.DrawOn(page);
                y += image.GetHeight();
                atTop = false;
            } else {
                float needed = FirstLineHeight(item.paragraphs);
                if (needed == 0f) {
                    continue;   // No text to draw
                }
                if (IsHeading(item.paragraphs[item.paragraphs.Count - 1])) {
                    // The heading, and the first line or the image after it
                    float withNext = TextHeight(item, width) + NextHeight(i + 1, width, height);
                    if (withNext <= height) {
                        needed = withNext;
                    }
                }
                if (page == null || (!atTop && y + needed > limit)) {
                    page = NewPage(pages);
                    y = top;
                }
                TextFrame frame = Frame(item, width);
                frame.SetLocation(left, y).SetHeight(limit - y).DrawOn(page);
                while (frame.HasMoreText()) {
                    page = NewPage(pages);
                    y = top;
                    frame.SetLocation(left, y).SetHeight(limit - y).DrawOn(page);
                }
                y = frame.GetTextBottom();
                atTop = false;
            }
        }
        return new float[] {left + width, y};
    }

    private Page NewPage(List<Page> pages) {
        Page page = new Page(pdf, pageSize, Page.DETACHED);
        pages.Add(page);
        return page;
    }

    // A text frame of the paragraphs of the item, as wide as the text.
    private static TextFrame Frame(Item item, float width) {
        TextFrame frame = new TextFrame(item.paragraphs).SetWidth(width);
        if (item.hasGap) {
            frame.SetParagraphGap(item.gap);
        }
        return frame;
    }

    // The height of the paragraphs of the item, all of them drawn in one frame.
    private static float TextHeight(Item item, float width) {
        TextFrame frame = Frame(item, width);
        frame.SetLocation(0f, 0f);
        frame.DrawOn(null);     // With no page the text is measured and kept
        return frame.GetTextBottom();
    }

    // The height that has to fit below a heading so that it does not end a
    // page: the spaces after it, and the first line of the paragraphs or the
    // image that comes next.
    private float NextHeight(int index, float width, float height) {
        float total = 0f;
        for (int i = index; i < items.Count; i++) {
            Item item = items[i];
            if (item.space > 0f) {
                total += item.space;
            } else if (item.image != null) {
                float w = item.image.GetWidth();
                float h = item.image.GetHeight();
                if (w > width) {
                    h = h * (width / w);
                }
                return total + Math.Min(h, height);
            } else {
                float line = FirstLineHeight(item.paragraphs);
                if (line > 0f) {
                    return total + line;
                }
            }
        }
        return total;
    }

    // The height of the caption of an image, a paragraph of StructElem.CAPTION
    // added right after it, and of the spaces before it; 0 when the image has
    // no caption.
    private float CaptionHeight(int index, float width) {
        float total = 0f;
        for (int i = index; i < items.Count; i++) {
            Item item = items[i];
            if (item.space > 0f) {
                total += item.space;
            } else if (item.image != null) {
                return 0f;
            } else {
                if (item.paragraphs.Count == 0 || item.paragraphs[0].structureType != StructElem.CAPTION) {
                    return 0f;
                }
                return total + TextHeight(item, width);
            }
        }
        return 0f;
    }

    // Makes the image as wide as the text at most, and as tall as the space
    // between the margins at most, keeping its shape.
    private static void FitImage(Image image, float width, float height) {
        if (image.GetWidth() > width) {
            image.ResizeWidth(width);
        }
        if (image.GetHeight() > height) {
            image.ResizeHeight(height);
        }
    }

    // The height of the first line of the paragraphs, its ascent and descent,
    // which a TextFrame places at its top; 0 when they have no text.
    private static float FirstLineHeight(List<Paragraph> paragraphs) {
        foreach (Paragraph paragraph in paragraphs) {
            if (paragraph.lines != null && paragraph.lines.Count > 0) {
                TextLine line = paragraph.lines[0];
                return line.font.GetAscent(line.fontSize) + line.font.GetDescent(line.fontSize);
            }
        }
        return 0f;
    }

    // Whether the paragraph is a heading, StructElem.H1 to H6.
    private static bool IsHeading(Paragraph paragraph) {
        switch (paragraph.structureType) {
        case StructElem.H1: case StructElem.H2: case StructElem.H3:
        case StructElem.H4: case StructElem.H5: case StructElem.H6:
            return true;
        default:
            return false;
        }
    }
}   // End of TextFlow.cs
}   // End of namespace PDFjet.NET
