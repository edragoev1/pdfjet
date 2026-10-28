/*
 * TextFlow.java
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
package com.pdfjet;

import java.util.*;

/**
 * Headings, paragraphs and images flowed onto as many pages as they need, one
 * below the other between the margins: a report, a manual, a long letter.
 * Paragraphs break across pages between their lines, as a TextFrame breaks
 * them; an image goes whole, to the top of the next page when it does not fit
 * below the text, with its caption; a heading goes to the next page with what
 * follows it rather than end a page. The pages are created detached and added
 * to a list, so that a footer or a page number can be drawn on each before they
 * are added to the PDF.
 * <p>
 * Use a TextFrame to place text by hand, in frames on the same page or on
 * others, the text flowing from one to the next; use TextFlow to pour the text
 * and the images in, page after page. The order they are added in is the
 * reading order of a PDF/UA document: the paragraphs are tagged as a TextFrame
 * tags them, the images as figures with their alternate descriptions.
 * Use Markdown instead for a text written in Markdown. Please see Example_22.
 */
public class TextFlow {
    private final PDF pdf;
    private final PageSize pageSize;
    private float top = 72f;
    private float right = 72f;
    private float bottom = 72f;
    private float left = 72f;
    private float firstPageTop = 0f;
    private final List<Item> items = new ArrayList<Item>();

    // One thing added to the flow: paragraphs, an image or a space.
    private static final class Item {
        List<Paragraph> paragraphs;
        float gap;
        boolean hasGap;
        Image image;
        Alignment alignment = Alignment.LEFT;
        float space;
    }

    /**
     * Creates a flow of text and images onto pages of the size, with margins
     * of 72 points, an inch, on every side.
     *
     * @param pdf the PDF document.
     * @param pageSize the page size, for example Letter.PORTRAIT.
     */
    public TextFlow(PDF pdf, PageSize pageSize) {
        this.pdf = pdf;
        this.pageSize = pageSize;
    }

    /**
     * Sets the margins of every side of the pages, in points. The text and the
     * images are as wide as the page less the left and right margins.
     *
     * @param margin the margin of every side.
     * @return this flow.
     */
    public TextFlow setMargins(float margin) {
        return setMargins(margin, margin, margin, margin);
    }

    /**
     * Sets the margins of the pages, in points: top, right, bottom and left,
     * the order CSS gives them.
     *
     * @param top the top margin.
     * @param right the right margin.
     * @param bottom the bottom margin.
     * @param left the left margin.
     * @return this flow.
     */
    public TextFlow setMargins(float top, float right, float bottom, float left) {
        this.top = top;
        this.right = right;
        this.bottom = bottom;
        this.left = left;
        return this;
    }

    /**
     * Sets where the flow starts on the first page that drawOn(first, pages)
     * is given, below what is drawn there already, such as a heading or a
     * letterhead. Without it the flow starts at the top margin.
     *
     * @param y the y coordinate the flow starts at on the first page.
     * @return this flow.
     */
    public TextFlow setFirstPageTop(float y) {
        this.firstPageTop = y;
        return this;
    }

    /**
     * Adds a paragraph: a heading, when its structure type is StructElem.H1 to
     * StructElem.H6, or a paragraph of text, a caption, a line.
     *
     * @param paragraph the paragraph to add.
     * @return this flow.
     */
    public TextFlow add(Paragraph paragraph) {
        Item item = new Item();
        item.paragraphs = Collections.singletonList(paragraph);
        items.add(item);
        return this;
    }

    /**
     * Adds paragraphs with gap points between them, which flow across pages as
     * the text of a TextFrame does.
     *
     * @param paragraphs the paragraphs to add.
     * @param gap the space between two paragraphs, in points.
     * @return this flow.
     */
    public TextFlow add(List<Paragraph> paragraphs, float gap) {
        Item item = new Item();
        item.paragraphs = paragraphs;
        item.gap = gap;
        item.hasGap = true;
        items.add(item);
        return this;
    }

    /**
     * Adds an image at the left margin. An image wider than the text is made as
     * wide as the text, and one taller than the space between the top and the
     * bottom margins as tall as that space, keeping its shape; it is never cut.
     * A paragraph of StructElem.CAPTION added right after it, after a space or
     * not, is its caption, and goes to the next page with it when the two do
     * not fit.
     *
     * @param image the image to add.
     * @return this flow.
     */
    public TextFlow add(Image image) {
        return add(image, Alignment.LEFT);
    }

    /**
     * Adds an image as add(image) does, at the left margin, in the center of
     * the text, or at the right margin: Alignment.LEFT, Alignment.CENTER or
     * Alignment.RIGHT.
     *
     * @param image the image to add.
     * @param alignment where the image goes across the text.
     * @return this flow.
     */
    public TextFlow add(Image image, Alignment alignment) {
        Item item = new Item();
        item.image = image;
        item.alignment = alignment;
        items.add(item);
        return this;
    }

    /**
     * Adds points of space below what was added before. A space at the top of
     * a page is left out, as the gap between two paragraphs is.
     *
     * @param points the space, in points.
     * @return this flow.
     */
    public TextFlow addSpace(float points) {
        if (points > 0f) {
            Item item = new Item();
            item.space = points;
            items.add(item);
        }
        return this;
    }

    /**
     * Draws what was added on as many new pages as it needs, created detached
     * and added to the list; add them to the PDF afterwards.
     *
     * @param pages the list that receives the new pages.
     * @return the x and y coordinates of the bottom right corner of what was
     *     drawn on the last page, where something drawn after it can go.
     * @throws Exception if an input or output exception occurred.
     */
    public float[] drawOn(List<Page> pages) throws Exception {
        return drawOn(null, pages);
    }

    /**
     * Draws what was added as drawOn(pages) does, starting on first, a page
     * with other content above the flow, which setFirstPageTop puts the flow
     * under. The next pages are new and are added to the list; first is not.
     *
     * @param first the page the flow starts on.
     * @param pages the list that receives the new pages.
     * @return the x and y coordinates of the bottom right corner of what was
     *     drawn on the last page, where something drawn after it can go.
     * @throws Exception if an input or output exception occurred.
     */
    public float[] drawOn(Page first, List<Page> pages) throws Exception {
        float width = pageSize.getWidth() - left - right;
        float height = pageSize.getHeight() - top - bottom;
        if (!TextFrame.finite(top) || !TextFrame.finite(right) || !TextFrame.finite(bottom) || !TextFrame.finite(left)
                || !TextFrame.finite(firstPageTop) || width <= 0f || height <= 0f) {
            pdf.fail(new IllegalArgumentException(
                    "The margins of the flow must be numbers that leave room on the page for the text."));
        }
        Page page = first;
        float y = top;
        boolean atTop = true;   // Nothing drawn yet on this page: a space here is left out
        if (first != null && firstPageTop > top) {
            y = firstPageTop;
            atTop = false;
        }
        float limit = pageSize.getHeight() - bottom;
        for (int i = 0; i < items.size(); i++) {
            Item item = items.get(i);
            if (item.space > 0f) {
                if (page != null && !atTop) {
                    y += item.space;
                    if (y >= limit) {
                        page = newPage(pages);  // The space ends the page, and is not carried over
                        y = top;
                        atTop = true;
                    }
                }
            } else if (item.image != null) {
                Image image = item.image;
                fitImage(image, width, height);
                float needed = image.getHeight();
                float withCaption = needed + captionHeight(i + 1, width);
                if (withCaption <= height) {
                    needed = withCaption;   // The image and its caption go together
                }
                if (page == null || (!atTop && y + needed > limit)) {
                    page = newPage(pages);
                    y = top;
                }
                float x = left;
                if (item.alignment == Alignment.CENTER) {
                    x += (width - image.getWidth()) / 2f;
                } else if (item.alignment == Alignment.RIGHT) {
                    x += width - image.getWidth();
                }
                image.setLocation(x, y);
                image.drawOn(page);
                y += image.getHeight();
                atTop = false;
            } else {
                float needed = firstLineHeight(item.paragraphs);
                if (needed == 0f) {
                    continue;   // No text to draw
                }
                if (isHeading(item.paragraphs.get(item.paragraphs.size() - 1))) {
                    // The heading and what it keeps after it, when the two fit
                    // on a page, or else the heading and the first line or the
                    // image after it
                    float textHeight = textHeight(item, width);
                    float withNext = textHeight + nextHeight(i + 1, width, height, true);
                    if (withNext > height) {
                        withNext = textHeight + nextHeight(i + 1, width, height, false);
                    }
                    if (withNext <= height) {
                        needed = withNext;
                    }
                }
                if (page == null || (!atTop && y + needed > limit)) {
                    page = newPage(pages);
                    y = top;
                }
                TextFrame frame = frame(item, width);
                frame.setLocation(left, y).setHeight(limit - y).drawOn(page);
                while (frame.hasMoreText()) {
                    page = newPage(pages);
                    y = top;
                    frame.setLocation(left, y).setHeight(limit - y).drawOn(page);
                }
                y = frame.getTextBottom();
                atTop = false;
            }
        }
        return new float[] {left + width, y};
    }

    private Page newPage(List<Page> pages) throws Exception {
        Page page = new Page(pdf, pageSize, Page.DETACHED);
        pages.add(page);
        return page;
    }

    // A text frame of the paragraphs of the item, as wide as the text.
    private static TextFrame frame(Item item, float width) {
        TextFrame frame = new TextFrame(item.paragraphs).setWidth(width);
        if (item.hasGap) {
            frame.setParagraphGap(item.gap);
        }
        return frame;
    }

    // The height of the paragraphs of the item, all of them drawn in one frame.
    private static float textHeight(Item item, float width) throws Exception {
        TextFrame frame = frame(item, width);
        frame.setLocation(0f, 0f);
        frame.drawOn(null);     // With no page the text is measured and kept
        return frame.getTextBottom();
    }

    // The height that has to fit below a heading so that it does not end a
    // page: the spaces after it, and the first line of the paragraphs or the
    // image that comes next. With whole, what the heading keeps after it: a
    // heading that comes next counts whole, with what it keeps in turn, and an
    // image with its caption when the two fit on a page. A heading kept only
    // the first line of a heading after it, which then went to the next page
    // with its paragraph, or an image without its caption, which went with
    // it, and was left at the bottom of a page (the review of 28 October
    // 2026). The sum stops once it is more than the height of a page, so that
    // a run of headings is measured in linear time.
    private float nextHeight(int index, float width, float height, boolean whole) throws Exception {
        float total = 0f;
        for (int i = index; i < items.size() && total <= height; i++) {
            Item item = items.get(i);
            if (item.space > 0f) {
                total += item.space;
            } else if (item.image != null) {
                float w = item.image.getWidth();
                float h = item.image.getHeight();
                if (w > width) {
                    h = h * (width / w);
                }
                h = Math.min(h, height);
                if (whole) {
                    float withCaption = h + captionHeight(i + 1, width);
                    if (withCaption <= height) {
                        h = withCaption;
                    }
                }
                return total + h;
            } else {
                float line = firstLineHeight(item.paragraphs);
                if (line > 0f && whole && item.paragraphs.size() == 1 && isHeading(item.paragraphs.get(0))) {
                    total += textHeight(item, width);
                } else if (line > 0f) {
                    return total + line;
                }
            }
        }
        return total;
    }

    // The height of the caption of an image, a paragraph of StructElem.CAPTION
    // added right after it, and of the spaces before it; 0 when the image has
    // no caption.
    private float captionHeight(int index, float width) throws Exception {
        float total = 0f;
        for (Item item : items.subList(index, items.size())) {
            if (item.space > 0f) {
                total += item.space;
            } else if (item.image != null) {
                return 0f;
            } else {
                if (item.paragraphs.isEmpty() || item.paragraphs.get(0).structureType != StructElem.CAPTION) {
                    return 0f;
                }
                return total + textHeight(item, width);
            }
        }
        return 0f;
    }

    // Makes the image as wide as the text at most, and as tall as the space
    // between the margins at most, keeping its shape.
    private static void fitImage(Image image, float width, float height) {
        if (image.getWidth() > width) {
            image.resizeWidth(width);
        }
        if (image.getHeight() > height) {
            image.resizeHeight(height);
        }
    }

    // The height of the first line of the paragraphs, its ascent and descent,
    // which a TextFrame places at its top; 0 when they have no text.
    private static float firstLineHeight(List<Paragraph> paragraphs) {
        for (Paragraph paragraph : paragraphs) {
            if (paragraph.lines != null && !paragraph.lines.isEmpty()) {
                TextLine line = paragraph.lines.get(0);
                return line.font.getAscent(line.fontSize) + line.font.getDescent(line.fontSize);
            }
        }
        return 0f;
    }

    // Whether the paragraph is a heading, StructElem.H1 to H6.
    private static boolean isHeading(Paragraph paragraph) {
        switch (paragraph.structureType) {
        case H1: case H2: case H3: case H4: case H5: case H6:
            return true;
        default:
            return false;
        }
    }
}
