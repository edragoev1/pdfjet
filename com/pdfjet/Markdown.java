/*
 * Markdown.java
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
package com.pdfjet;

import java.io.File;
import java.util.*;

/**
 * Draws a Markdown text on as many pages as it needs: headings of # and of
 * underlines, paragraphs with the inline markup of Markup, bullet and
 * numbered lists that nest, block quotes, fenced and indented code, thematic
 * breaks, the tables and the task lists of GitHub's Markdown, - [ ] and
 * - [x], drawn as boxes, and images alone in their paragraph. In a PDF/UA
 * document the headings are H1 to H6, with no level skipped, the lists are
 * lists, the boxes of the tasks their labels, read as ☐ and ☑, the quotes
 * BlockQuotes, the code Code, the tables tables and the images figures, with
 * their text as the alternate description.
 * <p>
 * It is not all of Markdown: HTML is drawn as the text it is, a line break
 * is a space, links in a table cell are its text, and an image is drawn only
 * when it is alone in its paragraph, from the directory that
 * setImageDirectory names; with none, its text is drawn instead, so that a
 * text from anyone reads no file. Please see Example_19.
 * <p>
 * To build the pages from paragraphs and images of your own, use TextFlow.
 */
public class Markdown {
    // The heading sizes, as factors of the size of the text.
    private static final float[] HEADING_SIZES = {2.0f, 1.6f, 1.3f, 1.15f, 1.0f, 0.9f};
    private static final int CODE_BACKGROUND = 0xF3F5F8;
    private static final int RULE_COLOR = 0xC8CCD2;
    private static final int QUOTE_BAR_COLOR = 0xB0B6BF;
    private static final int TABLE_HEADER_BACKGROUND = 0xEEF0F3;
    private static final int TASK_BOX_COLOR = 0x8C959F;
    private static final String CHECK_MARK = "\u2713";
    // The least width, in sizes of the text, that quotes and lists nested in
    // each other leave their text: deeper ones are not indented further, so
    // that the text is never narrower than nothing.
    private static final float MIN_WIDTH = 10f;

    private final Font regular;
    private final Font bold;
    private final Font italic;
    private final Font boldItalic;
    private final Font code;
    private final Markup markup;
    private Font headingFont;
    private int headingColor = Color.transparent;  // Leaves the headings in the color of the text
    private boolean fullWidthTables = false;        // Tables as wide as the text, or as their text
    private int checkColor = 0x1A7F37;              // Of the box of a task that is done, and of a check mark in a table
    private float marginLeft = 72f;
    private float marginTop = 72f;
    private float marginRight = 72f;
    private float marginBottom = 72f;
    private String imageDirectory;

    // The page being drawn, where the next block goes, and the bottom of the
    // text on a page.
    private PDF pdf;
    private List<Page> pages;
    private PageSize pageSize;
    private Page page;
    private float y;
    private float bottom;
    private boolean atTop;              // Nothing is drawn on the page yet
    private int headingLevel;           // Of the last heading, as it is tagged
    // The structure elements that the blocks drawn now are the kids of: a
    // list, its item and the item's body, or a quote. They are open, so that
    // what the next pages draw goes on adding to them.
    private final Deque<StructElement> containers = new ArrayDeque<StructElement>();
    // The quote bars that are drawn on the page, as [x, the top of the bar].
    private final List<float[]> quoteBars = new ArrayList<float[]>();

    /**
     * Creates the Markdown of text in the fonts, each at its size: the size
     * of the regular font is the size of the text, which the headings are
     * drawn larger than.
     *
     * @param regular the font of the text.
     * @param bold the font of **bold** text, and of the headings unless setHeadingFont sets another.
     * @param italic the font of *italic* text.
     * @param boldItalic the font of ***bold italic*** text.
     * @param code the font of code, usually a monospaced font.
     */
    public Markdown(Font regular, Font bold, Font italic, Font boldItalic, Font code) {
        this.regular = regular;
        this.bold = bold;
        this.italic = italic;
        this.boldItalic = boldItalic;
        this.code = code;
        this.markup = new Markup(regular, bold, italic, boldItalic, code);
        this.headingFont = bold;
    }

    /**
     * Sets the font of the headings, which is the bold font unless it is set.
     *
     * @param font the font.
     * @return this Markdown.
     */
    public Markdown setHeadingFont(Font font) {
        this.headingFont = font;
        return this;
    }

    /**
     * Sets the color of the headings. They are in the color of the text
     * unless it is set.
     *
     * @param headingColor the color as a 0xRRGGBB value.
     * @return this Markdown.
     */
    public Markdown setHeadingColor(int headingColor) {
        this.headingColor = headingColor;
        return this;
    }

    /**
     * Sets whether the tables are as wide as the text, their columns widened
     * by the share of the text each holds. They are as wide as their text
     * unless it is set, and a table wider than the text is fitted to it
     * either way.
     *
     * @param fullWidthTables true for tables as wide as the text.
     * @return this Markdown.
     */
    public Markdown setFullWidthTables(boolean fullWidthTables) {
        this.fullWidthTables = fullWidthTables;
        return this;
    }

    /**
     * Sets the color of the text of links. They are blue unless it is set.
     *
     * @param linkColor the color as a 0xRRGGBB value.
     * @return this Markdown.
     */
    public Markdown setLinkColor(int linkColor) {
        markup.setLinkColor(linkColor);
        return this;
    }

    /**
     * Sets the color of the box of a task that is done, - [x] in the
     * Markdown, which has a white check mark in it, and of the text of a table
     * cell that starts with a check mark, U+2713, as a table of features
     * marks what each has. It is green, 0x1A7F37, unless it is set;
     * Color.transparent leaves it as it is.
     *
     * @param checkColor the color as a 0xRRGGBB value.
     * @return this Markdown.
     */
    public Markdown setCheckColor(int checkColor) {
        if (checkColor == Color.transparent) {
            return this;
        }
        this.checkColor = checkColor;
        return this;
    }

    /**
     * Sets the margins of the pages, in points. They are 72 points, an inch, unless they are set.
     *
     * @param left the left margin.
     * @param top the top margin.
     * @param right the right margin.
     * @param bottom the bottom margin.
     * @return this Markdown.
     */
    public Markdown setMargins(float left, float top, float right, float bottom) {
        this.marginLeft = left;
        this.marginTop = top;
        this.marginRight = right;
        this.marginBottom = bottom;
        return this;
    }

    /**
     * Sets the directory that the images are read from: the source of an
     * image in the Markdown is the file of that name in the directory, a
     * JPEG, PNG, BMP or SVG file. A source that is an absolute path, a URL or has .. in
     * it is not read, and neither is any image unless the directory is set:
     * the image's text is drawn instead.
     *
     * @param directory the directory.
     * @return this Markdown.
     */
    public Markdown setImageDirectory(String directory) {
        this.imageDirectory = directory;
        return this;
    }

    /**
     * Draws the text on as many new pages as it needs. The pages are created
     * detached and added to the list, so that a footer or a page number can be
     * drawn on each before they are added to the PDF. A text with no blocks
     * needs no page.
     *
     * @param pdf the PDF document.
     * @param text the Markdown text.
     * @param pages the list that receives the new pages.
     * @param pageSize the page size, for example Letter.PORTRAIT.
     * @return the x and y coordinates of the bottom right corner of the text on the last page.
     * @throws Exception if an input or output exception occurred.
     */
    public float[] drawOn(PDF pdf, String text, List<Page> pages, PageSize pageSize) throws Exception {
        this.pdf = pdf;
        this.pages = pages;
        this.pageSize = pageSize;
        this.page = null;
        this.headingLevel = 0;
        containers.clear();
        quoteBars.clear();
        if (!TextFrame.finite(marginLeft) || !TextFrame.finite(marginTop)
                || !TextFrame.finite(marginRight) || !TextFrame.finite(marginBottom)) {
            // Pages were made until the memory ran out (review of 12 October
            // 2026, F4)
            pdf.fail(new IllegalArgumentException("The margins of the Markdown must be numbers, not NaN or infinite."));
            return new float[] {marginLeft, marginTop};
        }
        List<MarkdownParser.Block> blocks = MarkdownParser.parse(text);
        float width = pageSize.getWidth() - marginLeft - marginRight;
        // A table too wide for a page, or tables too large, make the text an
        // error, as a table of more columns was drawn as a paragraph of pipes
        // (review of 12 October 2026, R33)
        int cells = MarkdownParser.tableCells(blocks);
        if (cells == -1) {
            pdf.fail(new IllegalArgumentException("A table of the Markdown has more than "
                    + MarkdownParser.MAX_TABLE_COLUMNS + " columns, more than a page can show."));
        }
        if (cells > MarkdownParser.MAX_TABLE_CELLS) {
            pdf.fail(new IllegalArgumentException("The tables of the Markdown have more than "
                    + MarkdownParser.MAX_TABLE_CELLS + " cells: draw a table that large with BigTable."));
        }
        if (!blocks.isEmpty()) {
            newPage();
            drawBlocks(blocks, marginLeft, width, false);
            finishQuoteBars();
        }
        return new float[] {marginLeft + width, (page == null) ? marginTop : y};
    }

    // --- The flow of the blocks down the pages -----------------------------

    private void newPage() throws Exception {
        if (page != null) {
            finishQuoteBars();
        }
        page = new Page(pdf, pageSize, Page.DETACHED);
        pages.add(page);
        y = marginTop;
        bottom = pageSize.getHeight() - marginBottom;
        atTop = true;
        page.structParent = containers.peek();
        for (float[] bar : quoteBars) {
            bar[1] = y;
        }
    }

    // Starts a new page unless the height fits under what the page has.
    private void ensure(float height) throws Exception {
        if (!atTop && y + height > bottom) {
            newPage();
        }
    }

    // The space before a block, which the top of a page does not have.
    private void gap(float space) {
        if (!atTop) {
            y += space;
        }
    }

    private float size() {
        return regular.getSize();
    }

    private void openContainer(StructElem structure) {
        StructElement element = page.addStructElement(page.structParent, structure, null, true);
        if (element != null) {
            containers.push(element);
            page.structParent = element;
        }
    }

    private void closeContainer() {
        if (page.structParent != null && !containers.isEmpty()) {
            containers.pop();
            page.structParent = containers.peek();
        }
    }

    // Draws the quote bars of the page, from their top down to where the text is.
    private void finishQuoteBars() throws Exception {
        for (float[] bar : quoteBars) {
            drawQuoteBar(bar[0], bar[1], y);
        }
    }

    private void drawQuoteBar(float x, float top, float to) throws Exception {
        if (to > top) {
            new Line(x, top, x, to).setStrokeColor(QUOTE_BAR_COLOR).setStrokeWidth(2f).drawOn(page);
        }
    }

    private void drawBlocks(List<MarkdownParser.Block> blocks, float x, float width, boolean tight)
            throws Exception {
        for (int i = 0; i < blocks.size(); i++) {
            MarkdownParser.Block block = blocks.get(i);
            switch (block.kind) {
                case HEADING:
                    if (i + 2 < blocks.size() && leadsIntoTable(blocks.get(i + 1), blocks.get(i + 2))) {
                        keepHeadingWithTable(block, blocks.get(i + 1), blocks.get(i + 2),
                                tight ? size() * 0.25f : size() * 0.75f, x, width);
                    }
                    drawHeading(block, x, width);
                    break;
                case PARAGRAPH:
                    float gap = tight ? size() * 0.25f : size() * 0.75f;
                    if (i + 1 < blocks.size() && leadsIntoTable(block, blocks.get(i + 1))) {
                        keepWithTable(block, blocks.get(i + 1), gap, x, width);
                    }
                    gap(gap);
                    drawParagraph(markup.paragraph(block.text), x, width);
                    break;
                case CODE:
                    drawCode(block.text, x, width);
                    break;
                case QUOTE:
                    drawQuote(block, x, width);
                    break;
                case LIST:
                    drawList(block, x, width);
                    break;
                case RULE:
                    drawRule(x, width);
                    break;
                case TABLE:
                    drawTable(block, x, width);
                    break;
                case IMAGE:
                    drawImage(block, x, width);
                    break;
                default:
                    break;
            }
        }
    }

    // A paragraph in a text frame on the page, and on the next pages for what
    // does not fit.
    private void drawParagraph(Paragraph paragraph, float x, float width) throws Exception {
        if (paragraph.lines.isEmpty()) {
            return;
        }
        ensure(firstLineHeight(paragraph));
        TextFrame frame = new TextFrame(Arrays.asList(paragraph)).setParagraphGap(0f);
        frame.setLocation(x, y).setWidth(width).setHeight(bottom - y);
        frame.drawOn(page);
        while (frame.hasMoreText()) {
            newPage();
            frame.setLocation(x, y).setHeight(bottom - y);
            frame.drawOn(page);
        }
        y = paragraph.getY2();
        atTop = false;
    }

    private static float firstLineHeight(Paragraph paragraph) {
        float height = 0f;
        for (TextLine line : paragraph.lines) {
            height = Math.max(height, line.font.getBodyHeight(line.fontSize));
        }
        return height;
    }

    // A heading, larger than the text, which keeps a line of the text after
    // it on its page. Its level as it is tagged is at most one more than that
    // of the heading before it, so that no level is skipped.
    private void drawHeading(MarkdownParser.Block block, float x, float width) throws Exception {
        float fontSize = size() * HEADING_SIZES[block.level - 1];
        gap(headingGap(block));
        Paragraph paragraph = headingParagraph(block);
        headingLevel = Math.min(block.level, headingLevel + 1);
        StructElem[] levels = {StructElem.H1, StructElem.H2, StructElem.H3,
                StructElem.H4, StructElem.H5, StructElem.H6};
        paragraph.setStructureType(levels[headingLevel - 1]);
        ensure(headingFont.getBodyHeight(fontSize) + 2f * regular.getBodyHeight(size()));
        drawParagraph(paragraph, x, width);
        y += size() * 0.25f;
    }

    // The space above a heading.
    private float headingGap(MarkdownParser.Block block) {
        return size() * (block.level <= 2 ? 1.4f : 1.1f);
    }

    // The text of a heading in the heading font, at its level's size and in
    // the heading color.
    private Paragraph headingParagraph(MarkdownParser.Block block) {
        float fontSize = size() * HEADING_SIZES[block.level - 1];
        Paragraph paragraph = markup.paragraph(block.text);
        for (TextLine line : paragraph.lines) {
            if (line.font == regular) {
                line.setFont(headingFont);
            } else if (line.font == italic) {
                line.setFont(boldItalic);
            }
            line.setFontSize(fontSize);
            if (line.getURIAction() == null && line.getGoToAction() == null) {
                line.setTextColor(headingColor);
            }
        }
        return paragraph;
    }

    // Code in the code font on a light background, a line of the source at a
    // time, and a line too long for the width goes on under itself.
    private void drawCode(String text, float x, float width) throws Exception {
        gap(size() * 0.75f);
        float padding = size() * 0.5f;
        float leading = code.getBodyHeight(code.getSize()) * 1.2f;
        // A code font with no width, or no size, cuts no line.
        int columns = Integer.MAX_VALUE;
        float advance = code.stringWidth(code.getSize(), "0");
        if (advance > 0f) {
            columns = Math.max(1, (int) ((width - 2f * padding) / advance));
        }
        List<String> lines = new ArrayList<String>();
        for (String line : text.split("\n", -1)) {
            int start = 0;
            while (line.length() - start > columns) {
                // A surrogate pair is not cut in two: it goes on the next
                // line, or on this one when it would be the whole line.
                int cut = start + columns;
                if (Character.isHighSurrogate(line.charAt(cut - 1))) {
                    cut = (cut - 1 > start) ? cut - 1 : cut + 1;
                }
                lines.add(line.substring(start, cut));
                start = cut;
            }
            lines.add(line.substring(start));
        }
        ensure(Math.min(3, lines.size()) * leading + 2f * padding);
        // Code is an inline element, so the block is a paragraph that holds
        // its Code: PAC warns of a Code right under the document.
        openContainer(StructElem.P);
        openContainer(StructElem.CODE);
        int i = 0;
        while (i < lines.size()) {
            int fit = lines.size() - i;     // Lines of no height all fit
            if (leading > 0f) {
                fit = Math.max(1, (int) ((bottom - y - 2f * padding) / leading));
            }
            int count = Math.min(fit, lines.size() - i);
            new Rect(x, y, width, count * leading + 2f * padding).setFillColor(CODE_BACKGROUND).drawOn(page);
            float baseline = y + padding + code.getAscent(code.getSize()) + (leading - code.getBodyHeight(code.getSize())) / 2f;
            for (int j = 0; j < count; j++, i++) {
                if (!lines.get(i).trim().isEmpty()) {
                    new TextLine(code, lines.get(i)).setStructureType(StructElem.SPAN)
                            .setLocation(x + padding, baseline).drawOn(page);
                }
                baseline += leading;
            }
            y += count * leading + 2f * padding;
            atTop = false;
            if (i < lines.size()) {
                newPage();
            }
        }
        closeContainer();
        closeContainer();
    }

    // The height a quote or a list item makes room for on its page before it
    // starts: a line of the text, or a heading and the two lines it keeps with
    // it, when the first of its blocks is one. The first block of a quote or
    // an item skips the check of a page's room, and a heading was drawn into
    // the bottom margin (review of 12 October 2026, F16).
    private float firstBlockHeight(List<MarkdownParser.Block> blocks) {
        while (blocks != null && !blocks.isEmpty()) {
            MarkdownParser.Block first = blocks.get(0);
            if (first.kind == MarkdownParser.Kind.HEADING) {
                float fontSize = size() * HEADING_SIZES[first.level - 1];
                return headingFont.getBodyHeight(fontSize) + 2f * regular.getBodyHeight(size());
            } else if (first.kind == MarkdownParser.Kind.QUOTE) {
                blocks = first.children;
            } else if (first.kind == MarkdownParser.Kind.LIST && !first.children.isEmpty()) {
                blocks = first.children.get(0).children;
            } else {
                blocks = null;
            }
        }
        return regular.getBodyHeight(size());
    }

    // A quote, indented, with a bar on its left.
    private void drawQuote(MarkdownParser.Block block, float x, float width) throws Exception {
        gap(size() * 0.75f);
        ensure(firstBlockHeight(block.children));
        float indent = indentWithin(size() * 1.2f, width);
        float[] bar = {x + 2f, y};
        quoteBars.add(bar);
        openContainer(StructElem.BLOCKQUOTE);
        boolean top = atTop;
        atTop = true;       // The first block of the quote starts where the bar does.
        drawBlocks(block.children, x + indent, width - indent, false);
        atTop = top && atTop;
        closeContainer();
        quoteBars.remove(bar);
        drawQuoteBar(bar[0], bar[1], y);
    }

    // The indent, or less, so that the blocks inside a quote or a list are not
    // narrower than MIN_WIDTH sizes of the text.
    private float indentWithin(float indent, float width) {
        return Math.max(0f, Math.min(indent, width - MIN_WIDTH * size()));
    }

    // A list: the label of each item, a bullet or a number, to the left of
    // the blocks of the item.
    private void drawList(MarkdownParser.Block list, float x, float width) throws Exception {
        gap(size() * 0.75f);
        float indent = indentWithin(size() * (list.ordered ? 2f : 1.4f), width);
        openContainer(StructElem.L);
        int number = list.start;
        for (MarkdownParser.Block item : list.children) {
            if (item != list.children.get(0)) {
                gap(list.loose ? size() * 0.5f : size() * 0.2f);
            }
            ensure(firstBlockHeight(item.children));
            openContainer(StructElem.LI);
            if (item.task != MarkdownParser.Task.NONE) {
                drawTaskBox(item.task == MarkdownParser.Task.DONE, x, indent);
            } else {
                String label = list.ordered ? number + "." : "•";
                TextLine text = new TextLine(regular, label).setStructureType(StructElem.LBL);
                float labelX = list.ordered ? x + indent - size() * 0.4f - text.getWidth() : x + size() * 0.3f;
                text.setLocation(labelX, y + regular.getAscent(size())).drawOn(page);
            }
            openContainer(StructElem.LBODY);
            atTop = true;   // The first block of the item is on the line of its label.
            drawBlocks(item.children, x + indent, width - indent, !list.loose);
            if (atTop) {
                // An item with no text still takes the line of its label.
                y += regular.getBodyHeight(size());
            }
            atTop = false;
            closeContainer();
            closeContainer();
            number++;
        }
        closeContainer();
    }

    // The box of a task in place of the label of its item, on the line of its
    // first text: a gray box, or a box of the check color with a white check
    // mark when the task is done. A screen reader reads it as ☐ or ☑, the
    // label of the item.
    private void drawTaskBox(boolean done, float x, float indent) throws Exception {
        float size = size();
        float side = size * 0.8f;
        float left = x + (indent - size * 0.4f - side) / 2f;
        float baseline = y + regular.getAscent(size);
        float top = (baseline - size * 0.36f) - side / 2f;
        String actualText = done ? "☑" : "☐";
        // The box is drawn with the page's paths, not a Rect and Lines, which
        // are artifacts, as artifacts may not be inside the label.
        page.addBDC(StructElem.LBL, actualText, actualText);
        page.saveGraphicsState();
        List<Point> box = roundedBox(left, top, side, side * 0.2f);
        if (done) {
            page.setBrushColor(checkColor);
            page.drawPath(box, PathOperator.FILL);
            page.setPenColor(Color.white);
            page.setPenWidth(side * 0.14f);
            page.setLineCapStyle(CapStyle.ROUND);
            page.setLineJoinStyle(JoinStyle.ROUND);
            page.moveTo(left + side * 0.24f, top + side * 0.52f);
            page.lineTo(left + side * 0.43f, top + side * 0.70f);
            page.lineTo(left + side * 0.77f, top + side * 0.31f);
            page.strokePath();
        } else {
            page.setBrushColor(Color.white);
            page.setPenColor(TASK_BOX_COLOR);
            page.setPenWidth(size * 0.08f);
            page.drawPath(box, PathOperator.FILL_AND_STROKE);
        }
        page.restoreGraphicsState();
        page.addEMC();
    }

    // The path of a square with rounded corners, of the side and the radius,
    // its top left corner at x and y.
    private static List<Point> roundedBox(float x, float y, float side, float r) {
        final float k = 0.55228f;
        float c = r * k;
        char cp = Point.CONTROL_POINT_C;
        return Arrays.asList(
                new Point(x + r, y), new Point((x + side) - r, y),
                new Point(((x + side) - r) + c, y, cp), new Point(x + side, (y + r) - c, cp), new Point(x + side, y + r),
                new Point(x + side, (y + side) - r),
                new Point(x + side, ((y + side) - r) + c, cp), new Point(((x + side) - r) + c, y + side, cp), new Point((x + side) - r, y + side),
                new Point(x + r, y + side),
                new Point((x + r) - c, y + side, cp), new Point(x, ((y + side) - r) + c, cp), new Point(x, (y + side) - r),
                new Point(x, y + r),
                new Point(x, (y + r) - c, cp), new Point((x + r) - c, y, cp), new Point(x + r, y));
    }

    private void drawRule(float x, float width) throws Exception {
        gap(size() * 0.75f);
        ensure(size());
        float middle = y + size() * 0.5f;
        new Line(x, middle, x + width, middle).setStrokeColor(RULE_COLOR).setStrokeWidth(1f).drawOn(page);
        y += size();
        atTop = false;
    }

    // Tells if the block is a paragraph that ends in a colon, which leads into
    // the table after it.
    private static boolean leadsIntoTable(MarkdownParser.Block block, MarkdownParser.Block next) {
        return block.kind == MarkdownParser.Kind.PARAGRAPH && next.kind == MarkdownParser.Kind.TABLE
                && block.text.trim().endsWith(":");
    }

    // A heading over a paragraph that leads into a table, as "Lines of code"
    // over "Lines of code of each program:" and its table, starts a page with
    // them when the three fit on a page together but not in what is left of
    // this one: the paragraph and the table went to the next page, and the
    // heading ended this one.
    private void keepHeadingWithTable(MarkdownParser.Block heading, MarkdownParser.Block paragraph,
            MarkdownParser.Block table, float gap, float x, float width) throws Exception {
        if (atTop) {
            return;
        }
        float room = bottom - marginTop;
        float headingGap = headingGap(heading);
        TextFrame title = new TextFrame(Arrays.asList(headingParagraph(heading))).setParagraphGap(0f);
        title.setLocation(x, y + headingGap).setWidth(width).setHeight(room).drawOn(null);
        float below = title.getTextBottom() + size() * 0.25f + gap;
        TextFrame text = new TextFrame(Arrays.asList(markup.paragraph(paragraph.text))).setParagraphGap(0f);
        text.setLocation(x, below).setWidth(width).setHeight(room).drawOn(null);
        float needed = (text.getTextBottom() - y) + size() * 0.75f + tableOf(table, width).getTotalHeight();
        if (y + needed > bottom && needed - headingGap <= room) {
            newPage();
        }
    }

    // A paragraph that ends in a colon leads into the table after it, as
    // "The size of the PDFs, KB:" does, and starts a page with it when the two
    // fit on a page together but not in what is left of this one.
    private void keepWithTable(MarkdownParser.Block paragraph, MarkdownParser.Block table, float gap,
            float x, float width) throws Exception {
        if (atTop) {
            return;
        }
        float room = bottom - marginTop;
        TextFrame frame = new TextFrame(Arrays.asList(markup.paragraph(paragraph.text))).setParagraphGap(0f);
        frame.setLocation(x, y + gap).setWidth(width).setHeight(room).drawOn(null);
        float needed = (frame.getTextBottom() - y) + size() * 0.75f + tableOf(table, width).getTotalHeight();
        if (y + needed > bottom && needed - gap <= room) {
            newPage();
        }
    }

    // A table of the text of the cells, with the header row in bold on a
    // light background, as wide as its text or as the width when it would be
    // wider, and on the next pages for the rows that do not fit. A cell that
    // starts with a check mark is in the check color, as a table of features
    // marks what each has.
    private void drawTable(MarkdownParser.Block block, float x, float width) throws Exception {
        gap(size() * 0.75f);
        Table table = tableOf(block, width);
        // A table that fits on a page is not split: it starts the next page
        // when it does not fit in what is left of this one.
        float height = table.getTotalHeight();
        if (!atTop && y + height > bottom && height <= bottom - marginTop) {
            newPage();
        }
        float rowHeight = 2f * regular.getBodyHeight(size());
        ensure(2f * rowHeight);
        table.setLocation(x, marginTop);
        table.setFirstPageTopMargin(y);
        table.setBottomMargin(marginBottom);
        int before = pages.size();
        float[] xy = table.drawOn(pdf, page, pages, pageSize);
        if (pages.size() > before) {
            // The table drew its last rows on a page of its own.
            finishQuoteBars();
            page = pages.get(pages.size() - 1);
            page.structParent = containers.peek();
            for (float[] bar : quoteBars) {
                bar[1] = marginTop;
            }
        }
        y = xy[1];
        atTop = false;
    }

    // The table of the text of the cells, as wide as its text or as the width.
    private Table tableOf(MarkdownParser.Block block, float width) {
        List<List<Cell>> rows = new ArrayList<List<Cell>>();
        for (int r = 0; r < block.rows.size(); r++) {
            List<Cell> row = new ArrayList<Cell>();
            for (int c = 0; c < block.rows.get(r).size(); c++) {
                String text = block.rows.get(r).get(c);
                String plain = plainText(text);
                Cell cell = new Cell((r == 0) ? bold : cellFont(text), plain);
                cell.setTextAlignment(block.alignments.get(c));
                if (r > 0 && plain.startsWith(CHECK_MARK)) {
                    cell.setTextColor(checkColor);
                }
                row.add(cell);
            }
            rows.add(row);
        }
        Table table = new Table();
        table.setTableData(rows, 1);
        table.setHeaderRowStyle(bold, 0x000000, TABLE_HEADER_BACKGROUND);
        table.setCellBorderColor(0xC8CCD2);
        table.autoAdjustColumnWidths();
        if (fullWidthTables || table.getWidth() > width) {
            table.fitToWidth(width);
        }
        return table;
    }

    // The font of a cell: the font of its emphasis when the whole of its text
    // is in one, as **2.1** is in bold, so that a table can mark a value; the
    // regular font when it has none, or more than one, as a cell has one font.
    private Font cellFont(String text) {
        Font font = null;
        for (TextLine line : markup.paragraph(text).lines) {
            if (line.text.trim().isEmpty()) {
                continue;
            }
            if (font == null) {
                font = line.font;
            } else if (font != line.font) {
                return regular;
            }
        }
        return (font != null) ? font : regular;
    }

    // The text of a cell, without its inline markup.
    private String plainText(String text) {
        StringBuilder buf = new StringBuilder();
        Paragraph paragraph = markup.paragraph(text);
        for (int i = 0; i < paragraph.lines.size(); i++) {
            if (i > 0 && !paragraph.joinsPrevious(i)) {
                buf.append(' ');
            }
            buf.append(paragraph.lines.get(i).text.trim());
        }
        return buf.toString();
    }

    // An image as wide as it is, or as the width when it is wider, and as tall
    // as the page when it is taller; or its text, in italic, when it is not read.
    private void drawImage(MarkdownParser.Block block, float x, float width) throws Exception {
        gap(size() * 0.75f);
        String path = imagePath(block.source);
        if (path == null) {
            Paragraph paragraph = new Paragraph(new TextLine(italic,
                    block.text.isEmpty() ? block.source : block.text));
            drawParagraph(paragraph, x, width);
            return;
        }
        String alt = block.text.isEmpty() ? new File(block.source).getName() : block.text;
        float scale;
        float imageWidth;
        float imageHeight;
        Drawable drawable;
        if (path.toLowerCase(Locale.ROOT).endsWith(".svg")) {
            SVGImage image = new SVGImage(path);
            image.setAltDescription(alt);
            imageWidth = image.getWidth();
            imageHeight = image.getHeight();
            scale = Math.min(1f, Math.min(width / imageWidth, (bottom - marginTop) / imageHeight));
            image.scaleBy(scale);
            drawable = image;
        } else {
            Image image = new Image(pdf, path);
            image.setAltDescription(alt);
            imageWidth = image.getWidth();
            imageHeight = image.getHeight();
            scale = Math.min(1f, Math.min(width / imageWidth, (bottom - marginTop) / imageHeight));
            image.scaleBy(scale);
            drawable = image;
        }
        ensure(imageHeight * scale);
        drawable.setLocation(x, y);
        drawable.drawOn(page);
        y += imageHeight * scale;
        atTop = false;
    }

    // The path of an image in the image directory, or null when there is no
    // directory, or the source is an absolute path, a URL, has .. in it, names
    // a directory, or names no file. The source is read the same way in every
    // port: its parts between the slashes, less the empty ones and those that
    // are ".", are the names of the directories under the image directory and
    // of the file, and a source that ends in a slash or in "." names a
    // directory. An empty image directory is the working directory.
    String imagePath(String source) {
        if (imageDirectory == null || source.startsWith("/") || source.startsWith("\\")
                || source.indexOf(':') != -1 || source.indexOf('\\') != -1) {
            return null;
        }
        String[] parts = source.split("/", -1);
        String last = parts[parts.length - 1];
        if (last.isEmpty() || last.equals(".")) {
            return null;
        }
        StringBuilder path = new StringBuilder(imageDirectory);
        for (String part : parts) {
            if (part.equals("..")) {
                return null;
            }
            if (!part.isEmpty() && !part.equals(".")) {
                if (path.length() > 0) {
                    path.append(File.separatorChar);
                }
                path.append(part);
            }
        }
        File file = new File(path.toString());
        return file.isFile() ? file.getPath() : null;
    }
}   // End of Markdown.java
