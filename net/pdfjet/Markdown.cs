/*
 * Markdown.cs
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
using System;
using System.IO;
using System.Text;
using System.Collections.Generic;
using System.Globalization;
using System.Runtime.CompilerServices;

namespace PDFjet.NET {
/// <summary>
/// Draws a Markdown text on as many pages as it needs: headings of # and of
/// underlines, paragraphs with the inline markup of Markup, bullet and
/// numbered lists that nest, block quotes, fenced and indented code, thematic
/// breaks, the tables and the task lists of GitHub's Markdown, - [ ] and
/// - [x], drawn as boxes, and images alone in their paragraph. In a PDF/UA
/// document the headings are H1 to H6, with no level skipped, the lists are
/// lists, the boxes of the tasks their labels, read as ☐ and ☑, the quotes
/// BlockQuotes, the code Code, the tables tables and the images figures, with
/// their text as the alternate description. In any document the headings
/// are its bookmarks, the outline a reader shows, unless it has bookmarks of
/// its own.
/// <para>
/// A line that ends in two spaces or a backslash, and a &lt;br&gt;, break the
/// line, in a table cell too, and the entities of Markup, as &amp;amp; and
/// &amp;mdash;, are the characters they name.
/// </para>
/// <para>
/// It is not all of Markdown: other HTML is drawn as the text it is, any
/// other line break is a space, links in a table cell are its text, and an image is drawn only
/// when it is alone in its paragraph, from the directory that
/// SetImageDirectory names; with none, its text is drawn instead, so that a
/// text from anyone reads no file. Please see Example_19.
///
/// To build the pages from paragraphs and images of your own, use TextFlow.
/// </para>
/// </summary>
public class Markdown {
    // The heading sizes, as factors of the size of the text.
    private static readonly float[] HEADING_SIZES = {2.0f, 1.6f, 1.3f, 1.15f, 1.0f, 0.9f};
    private const int CODE_BACKGROUND = 0xF3F5F8;
    private const int RULE_COLOR = 0xC8CCD2;
    private const int QUOTE_BAR_COLOR = 0xB0B6BF;
    private const int TABLE_HEADER_BACKGROUND = 0xEEF0F3;
    private const int TASK_BOX_COLOR = 0x8C959F;
    private const String CHECK_MARK = "\u2713";
    // The least width, in sizes of the text, that quotes and lists nested in
    // each other leave their text: deeper ones are not indented further, so
    // that the text is never narrower than nothing.
    private const float MIN_WIDTH = 10f;

    private readonly Font regular;
    private readonly Font bold;
    private readonly Font italic;
    private readonly Font boldItalic;
    private readonly Font code;
    private readonly Markup markup;
    private Font headingFont;
    private int headingColor = Color.transparent;   // Leaves the headings in the color of the text
    private bool fullWidthTables = false;           // Tables as wide as the text, or as their text
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
    private bool atTop;                 // Nothing is drawn on the page yet
    private int headingLevel;           // Of the last heading, as it is tagged
    // The structure elements that the blocks drawn now are the kids of: a
    // list, its item and the item's body, or a quote. They are open, so that
    // what the next pages draw goes on adding to them.
    private readonly Stack<StructElement> containers = new Stack<StructElement>();
    // The quote bars that are drawn on the page, as [x, the top of the bar].
    private readonly List<float[]> quoteBars = new List<float[]>();
    private bool allowSplit;            // The next table starts where it is, under its heading
    // The tables and the images of the blocks, made when they are first
    // measured and drawn as they were made: an image is in the PDF once. They
    // are keyed by the block object, as Java's IdentityHashMap keys them.
    private readonly Dictionary<MarkdownParser.Block, Table> tables =
            new Dictionary<MarkdownParser.Block, Table>(BlockIdentity.Instance);
    private readonly Dictionary<MarkdownParser.Block, IDrawable> images =
            new Dictionary<MarkdownParser.Block, IDrawable>(BlockIdentity.Instance);
    private readonly Dictionary<MarkdownParser.Block, float> imageHeights =
            new Dictionary<MarkdownParser.Block, float>(BlockIdentity.Instance);
    // The room each heading keeps, by the heading, the width and whether it
    // is in a tight list, measured once: measured for every heading again,
    // down to the end of the headings after it, a run of headings took time
    // that grew as its square (the review of 28 October 2026). A block, which
    // does not override Equals, is compared by reference.
    private readonly Dictionary<(MarkdownParser.Block block, float width, bool tight), float> keeps =
            new Dictionary<(MarkdownParser.Block block, float width, bool tight), float>();

    // Compares blocks by reference, as Java's IdentityHashMap does; .NET
    // Framework 4.8 has no ReferenceEqualityComparer.
    private sealed class BlockIdentity : IEqualityComparer<MarkdownParser.Block> {
        internal static readonly BlockIdentity Instance = new BlockIdentity();

        public bool Equals(MarkdownParser.Block a, MarkdownParser.Block b) {
            return ReferenceEquals(a, b);
        }

        public int GetHashCode(MarkdownParser.Block block) {
            return RuntimeHelpers.GetHashCode(block);
        }
    }

    /// <summary>
    /// Creates the Markdown of text in the fonts, each at its size: the size
    /// of the regular font is the size of the text, which the headings are
    /// drawn larger than.
    /// </summary>
    /// <param name="regular">the font of the text.</param>
    /// <param name="bold">the font of **bold** text, and of the headings unless SetHeadingFont sets another.</param>
    /// <param name="italic">the font of *italic* text.</param>
    /// <param name="boldItalic">the font of ***bold italic*** text.</param>
    /// <param name="code">the font of code, usually a monospaced font.</param>
    public Markdown(Font regular, Font bold, Font italic, Font boldItalic, Font code) {
        this.regular = regular;
        this.bold = bold;
        this.italic = italic;
        this.boldItalic = boldItalic;
        this.code = code;
        this.markup = new Markup(regular, bold, italic, boldItalic, code);
        this.headingFont = bold;
    }

    /// <summary>
    /// Sets the font of the headings, which is the bold font unless it is set.
    /// </summary>
    /// <param name="font">the font.</param>
    /// <returns>this Markdown.</returns>
    public Markdown SetHeadingFont(Font font) {
        this.headingFont = font;
        return this;
    }

    /// <summary>
    /// Sets the color of the headings, as a 0xRRGGBB value. They are in the
    /// color of the text unless it is set.
    /// </summary>
    /// <param name="headingColor">the color.</param>
    /// <returns>this Markdown.</returns>
    public Markdown SetHeadingColor(int headingColor) {
        this.headingColor = headingColor;
        return this;
    }

    /// <summary>
    /// Sets whether the tables are as wide as the text, their columns widened
    /// by the share of the text each holds. They are as wide as their text
    /// unless it is set, and a table wider than the text is fitted to it
    /// either way.
    /// </summary>
    /// <param name="fullWidthTables">true for tables as wide as the text.</param>
    /// <returns>this Markdown.</returns>
    public Markdown SetFullWidthTables(bool fullWidthTables) {
        this.fullWidthTables = fullWidthTables;
        return this;
    }

    /// <summary>
    /// Sets the color of the text of links, as a 0xRRGGBB value. They are
    /// blue unless it is set.
    /// </summary>
    /// <param name="linkColor">the color.</param>
    /// <returns>this Markdown.</returns>
    public Markdown SetLinkColor(int linkColor) {
        markup.SetLinkColor(linkColor);
        return this;
    }

    /// <summary>
    /// Sets the color of the box of a task that is done, - [x] in the
    /// Markdown, as a 0xRRGGBB value, which has a white check mark in it, and
    /// of the text of a table cell that starts with a check mark, U+2713, as a
    /// table of features marks what each has. It is green, 0x1A7F37, unless it
    /// is set; Color.transparent leaves it as it is.
    /// </summary>
    /// <param name="checkColor">the color.</param>
    /// <returns>this Markdown.</returns>
    public Markdown SetCheckColor(int checkColor) {
        if (checkColor == Color.transparent) {
            return this;
        }
        this.checkColor = checkColor;
        return this;
    }

    /// <summary>
    /// Sets the margins of the pages, in points. They are 72 points, an inch, unless they are set.
    /// </summary>
    /// <param name="left">the left margin.</param>
    /// <param name="top">the top margin.</param>
    /// <param name="right">the right margin.</param>
    /// <param name="bottom">the bottom margin.</param>
    /// <returns>this Markdown.</returns>
    public Markdown SetMargins(float left, float top, float right, float bottom) {
        this.marginLeft = left;
        this.marginTop = top;
        this.marginRight = right;
        this.marginBottom = bottom;
        return this;
    }

    /// <summary>
    /// Sets the directory that the images are read from: the source of an
    /// image in the Markdown is the file of that name in the directory, a
    /// JPEG, PNG, BMP or SVG file. A source that is an absolute path, a URL or has .. in
    /// it is not read, and neither is any image unless the directory is set:
    /// the image's text is drawn instead.
    /// </summary>
    /// <param name="directory">the directory.</param>
    /// <returns>this Markdown.</returns>
    public Markdown SetImageDirectory(String directory) {
        this.imageDirectory = directory;
        return this;
    }

    /// <summary>
    /// Draws the text on as many new pages as it needs. The pages are created
    /// detached and added to the list, so that a footer or a page number can be
    /// drawn on each before they are added to the PDF. A text with no blocks
    /// needs no page.
    /// </summary>
    /// <param name="pdf">the PDF document.</param>
    /// <param name="text">the Markdown text.</param>
    /// <param name="pages">the list that receives the new pages.</param>
    /// <param name="pageSize">the page size, for example Letter.PORTRAIT.</param>
    /// <returns>the x and y coordinates of the bottom right corner of the text on the last page.</returns>
    public float[] DrawOn(PDF pdf, String text, List<Page> pages, PageSize pageSize) {
        this.pdf = pdf;
        this.pages = pages;
        this.pageSize = pageSize;
        this.page = null;
        this.headingLevel = 0;
        containers.Clear();
        // The tables and images of this text, whose blocks are new each time
        tables.Clear();
        images.Clear();
        imageHeights.Clear();
        keeps.Clear();
        quoteBars.Clear();
        if (!TextFrame.Finite(marginLeft) || !TextFrame.Finite(marginTop)
                || !TextFrame.Finite(marginRight) || !TextFrame.Finite(marginBottom)) {
            // Pages were made until the memory ran out (review of 12 October
            // 2026, F4)
            pdf.Fail(new ArgumentException("The margins of the Markdown must be numbers, not NaN or infinite."));
            return new float[] {marginLeft, marginTop};
        }
        List<MarkdownParser.Block> blocks = MarkdownParser.Parse(text);
        float width = pageSize.GetWidth() - marginLeft - marginRight;
        // A table too wide for a page, or tables too large, make the text an
        // error, as a table of more columns was drawn as a paragraph of pipes
        // (review of 12 October 2026, R33)
        int cells = MarkdownParser.TableCells(blocks);
        if (cells == -1) {
            pdf.Fail(new ArgumentException("A table of the Markdown has more than "
                    + MarkdownParser.MAX_TABLE_COLUMNS + " columns, more than a page can show."));
        }
        if (cells > MarkdownParser.MAX_TABLE_CELLS) {
            pdf.Fail(new ArgumentException("The tables of the Markdown have more than "
                    + MarkdownParser.MAX_TABLE_CELLS + " cells: draw a table that large with BigTable."));
        }
        if (blocks.Count > 0) {
            NewPage();
            DrawBlocks(blocks, marginLeft, width, false);
            FinishQuoteBars();
        }
        return new float[] {marginLeft + width, (page == null) ? marginTop : y};
    }

    // --- The flow of the blocks down the pages -----------------------------

    private void NewPage() {
        if (page != null) {
            FinishQuoteBars();
        }
        page = new Page(pdf, pageSize, Page.DETACHED);
        pages.Add(page);
        y = marginTop;
        bottom = pageSize.GetHeight() - marginBottom;
        atTop = true;
        page.structParent = Innermost();
        foreach (float[] bar in quoteBars) {
            bar[1] = y;
        }
    }

    // The innermost open container, or null, as Java's Deque.peek returns it.
    private StructElement Innermost() {
        return (containers.Count > 0) ? containers.Peek() : null;
    }

    // Starts a new page unless the height fits under what the page has.
    private void Ensure(float height) {
        if (!atTop && y + height > bottom) {
            NewPage();
        }
    }

    // The space before a block, which the top of a page does not have.
    private void Gap(float space) {
        if (!atTop) {
            y += space;
        }
    }

    private float Size() {
        return regular.GetSize();
    }

    private void OpenContainer(StructElem structure) {
        StructElement element = page.AddStructElement(page.structParent, structure, null, true);
        if (element != null) {
            containers.Push(element);
            page.structParent = element;
        }
    }

    private void CloseContainer() {
        if (page.structParent != null && containers.Count > 0) {
            containers.Pop();
            page.structParent = Innermost();
        }
    }

    // Draws the quote bars of the page, from their top down to where the text is.
    private void FinishQuoteBars() {
        foreach (float[] bar in quoteBars) {
            DrawQuoteBar(page, bar[0], bar[1], y);
        }
    }

    private static void DrawQuoteBar(Page page, float x, float top, float to) {
        if (to > top) {
            new Line(x, top, x, to).SetStrokeColor(QUOTE_BAR_COLOR).SetStrokeWidth(2f).DrawOn(page);
        }
    }

    private void DrawBlocks(List<MarkdownParser.Block> blocks, float x, float width, bool tight) {
        for (int i = 0; i < blocks.Count; i++) {
            MarkdownParser.Block block = blocks[i];
            switch (block.kind) {
                case MarkdownParser.Kind.HEADING:
                    // An empty heading draws nothing, and takes no level of the
                    // tags: "#" made the next "##" an H2 with no H1 over it
                    if (Util.Trim(block.text).Length == 0) {
                        break;
                    }
                    DrawHeading(block, x, width, HeadingKeep(blocks, i, width, tight));
                    break;
                case MarkdownParser.Kind.PARAGRAPH:
                    float gap = tight ? Size() * 0.25f : Size() * 0.75f;
                    if (i + 1 < blocks.Count && LeadsIntoTable(block, blocks[i + 1])) {
                        KeepWithTable(block, blocks[i + 1], gap, x, width);
                    }
                    Gap(gap);
                    DrawParagraph(markup.Paragraph(block.text), x, width);
                    break;
                case MarkdownParser.Kind.CODE:
                    DrawCode(block.text, x, width);
                    break;
                case MarkdownParser.Kind.QUOTE:
                    DrawQuote(block, x, width);
                    break;
                case MarkdownParser.Kind.LIST:
                    DrawList(block, x, width);
                    break;
                case MarkdownParser.Kind.RULE:
                    DrawRule(x, width);
                    break;
                case MarkdownParser.Kind.TABLE:
                    DrawTable(block, x, width);
                    break;
                case MarkdownParser.Kind.IMAGE:
                    DrawImage(block, x, width);
                    break;
                default:
                    break;
            }
        }
    }

    // The room a heading keeps on its page: itself and what comes after it,
    // as far as a page keeps that together, so that the heading does not end
    // a page. When the two do not fit on one page, the heading and two lines,
    // or the first rows of a table under it, or of one after its lead-in,
    // which then starts there: the table went to the next page alone.
    private float HeadingKeep(List<MarkdownParser.Block> blocks, int i, float width, bool tight) {
        float keep = KeepHeight(blocks, i, width, tight);
        if (keep <= bottom - marginTop) {
            return keep;
        }
        float line = regular.GetBodyHeight(Size());
        float heading = HeadingHeight(blocks[i], width);
        if (i + 1 < blocks.Count && blocks[i + 1].kind == MarkdownParser.Kind.TABLE) {
            allowSplit = true;
            return heading + Size() * 0.75f + 4f * line;
        }
        if (i + 2 < blocks.Count && LeadsIntoTable(blocks[i + 1], blocks[i + 2])) {
            allowSplit = true;
            return heading + ParagraphGap(tight) + ParagraphHeight(blocks[i + 1].text, width)
                    + Size() * 0.75f + 4f * line;
        }
        return heading + 2f * line;
    }

    // The room the first block of a quote or a list item needs on its page,
    // which the block does not check, drawn where the quote's bar or the
    // item's label is: a first image, code or table was drawn into the bottom
    // margin, and a short table was split.
    private float FirstKeep(List<MarkdownParser.Block> blocks, float width, bool tight) {
        if (blocks.Count == 0) {
            return regular.GetBodyHeight(Size());
        }
        float keep = (blocks[0].kind == MarkdownParser.Kind.HEADING)
                ? HeadingKeep(blocks, 0, width, tight) : KeepHeight(blocks, 0, width, tight);
        return Math.Min(keep, bottom - marginTop);
    }

    // The height a block needs on its page where it starts: the whole of a
    // table, an image or a lead-in and its table that fit on a page; three
    // lines of code; a heading and what it keeps; or a line of text.
    private float KeepHeight(List<MarkdownParser.Block> blocks, int i, float width, bool tight) {
        MarkdownParser.Block block = blocks[i];
        float line = regular.GetBodyHeight(Size());
        float room = bottom - marginTop;
        switch (block.kind) {
            case MarkdownParser.Kind.HEADING: {
                float keep;
                if (keeps.TryGetValue((block, width, tight), out keep)) {
                    return keep;
                }
                // The headings that follow it and the block after them,
                // measured from the last back, each heading keeping what is
                // after it
                int end = i + 1;
                while (end < blocks.Count && blocks[end].kind == MarkdownParser.Kind.HEADING) {
                    end++;
                }
                keep = (end < blocks.Count) ? KeepHeight(blocks, end, width, tight) : 0f;
                for (int j = end - 1; j >= i; j--) {
                    float heading = HeadingHeight(blocks[j], width);
                    if (j + 1 < blocks.Count) {
                        MarkdownParser.Block next = blocks[j + 1];
                        float gap = (next.kind == MarkdownParser.Kind.PARAGRAPH) ? ParagraphGap(tight)
                                : (next.kind == MarkdownParser.Kind.HEADING) ? HeadingGap(next) : Size() * 0.75f;
                        keep = heading + Math.Max(gap + keep, 2f * line);
                    } else {
                        keep = heading + 2f * line;
                    }
                    keeps[(blocks[j], width, tight)] = keep;
                }
                return keep;
            }
            case MarkdownParser.Kind.PARAGRAPH:
                if (i + 1 < blocks.Count && LeadsIntoTable(block, blocks[i + 1])) {
                    float keep = ParagraphHeight(block.text, width) + Size() * 0.75f
                            + TableOf(blocks[i + 1], width).GetTotalHeight();
                    if (keep <= room) {
                        return keep;
                    }
                }
                return line;
            case MarkdownParser.Kind.TABLE: {
                float height = TableOf(block, width).GetTotalHeight();
                return (height <= room) ? height : 4f * line;
            }
            case MarkdownParser.Kind.IMAGE:
                return (ImageOf(block, width) == null) ? line : imageHeights[block];
            case MarkdownParser.Kind.CODE:
                return block.text.Length == 0 ? 0f : CodeHeight(block.text, width);
            case MarkdownParser.Kind.QUOTE:
                return block.children.Count == 0 ? line
                        : KeepHeight(block.children, 0, width - IndentWithin(Size() * 1.2f, width), false);
            case MarkdownParser.Kind.LIST:
                if (block.children.Count == 0 || block.children[0].children.Count == 0) {
                    return line;
                }
                return KeepHeight(block.children[0].children, 0,
                        width - IndentWithin(Size() * (block.ordered ? 2f : 1.4f), width), !block.loose);
            default:
                return line;
        }
    }

    // The space before a paragraph.
    private float ParagraphGap(bool tight) {
        return tight ? Size() * 0.25f : Size() * 0.75f;
    }

    // The height of a heading's text at the width, of all its lines: a
    // heading of two lines was kept on its page by its first.
    private float HeadingHeight(MarkdownParser.Block block, float width) {
        TextFrame frame = new TextFrame(new List<Paragraph> {HeadingParagraph(block)}).SetParagraphGap(0f);
        frame.SetLocation(0f, 0f).SetWidth(width).SetHeight(bottom - marginTop);
        frame.DrawOn(null);
        return frame.GetTextBottom();
    }

    // The height of a paragraph's text at the width.
    private float ParagraphHeight(String text, float width) {
        TextFrame frame = new TextFrame(new List<Paragraph> {markup.Paragraph(text)}).SetParagraphGap(0f);
        frame.SetLocation(0f, 0f).SetWidth(width).SetHeight(bottom - marginTop);
        frame.DrawOn(null);
        return frame.GetTextBottom();
    }

    // The lines of code at the width, each cut where the next character would
    // pass it, measured in the code font: by the count of characters as wide
    // as a "0", CJK and other wide characters ran past the background. The
    // widths of the characters are added up in one pass, so that a long line
    // is cut in linear time. A surrogate pair is not cut in two, a line has a
    // character at least, and a code font with no width cuts none.
    private List<String> CodeLines(String text, float width) {
        float room = width - 2f * Size() * 0.5f;
        List<String> lines = new List<String>();
        foreach (String line in text.Split('\n')) {
            int start = 0;
            float used = 0f;
            int i = 0;
            while (i < line.Length) {
                int next = i + CharCountAt(line, i);
                float advance = code.StringWidth(code.GetSize(), line.Substring(i, next - i));
                if (i > start && used + advance > room) {
                    lines.Add(line.Substring(start, i - start));
                    start = i;
                    used = 0f;
                }
                used += advance;
                i = next;
            }
            lines.Add(line.Substring(start));
        }
        return lines;
    }

    // The number of chars of the code point at the index, as Java's
    // Character.charCount(codePointAt) gives it: 2 for a surrogate pair.
    private static int CharCountAt(String text, int index) {
        return (Char.IsHighSurrogate(text[index]) && index + 1 < text.Length
                && Char.IsLowSurrogate(text[index + 1])) ? 2 : 1;
    }

    // The height code keeps together on a page: three lines at most.
    private float CodeHeight(String text, float width) {
        float leading = code.GetBodyHeight(code.GetSize()) * 1.2f;
        return Math.Min(3, CodeLines(text, width).Count) * leading + 2f * Size() * 0.5f;
    }

    // A paragraph in a text frame on the page, and on the next pages for what
    // does not fit.
    private void DrawParagraph(Paragraph paragraph, float x, float width) {
        if (paragraph.lines.Count == 0) {
            return;
        }
        Ensure(FirstLineHeight(paragraph));
        TextFrame frame = new TextFrame(new List<Paragraph> {paragraph}).SetParagraphGap(0f);
        frame.SetLocation(x, y).SetWidth(width).SetHeight(bottom - y);
        frame.DrawOn(page);
        while (frame.HasMoreText()) {
            NewPage();
            frame.SetLocation(x, y).SetHeight(bottom - y);
            frame.DrawOn(page);
        }
        y = paragraph.GetY2();
        atTop = false;
    }

    private static float FirstLineHeight(Paragraph paragraph) {
        float height = 0f;
        foreach (TextLine line in paragraph.lines) {
            height = Math.Max(height, line.font.GetBodyHeight(line.fontSize));
        }
        return height;
    }

    // A heading, larger than the text, which keeps a line of the text after
    // it on its page. Its level as it is tagged is at most one more than that
    // of the heading before it, so that no level is skipped.
    private void DrawHeading(MarkdownParser.Block block, float x, float width, float keep) {
        Paragraph paragraph = HeadingParagraph(block);
        if (paragraph.lines.Count == 0) {
            // A heading of no text, such as "# <br>", draws nothing and takes
            // no level: the heading after it was tagged a level below one
            // that was not drawn (the review of 28 October 2026)
            return;
        }
        Gap(HeadingGap(block));
        headingLevel = Math.Min(block.level, headingLevel + 1);
        StructElem[] levels = {StructElem.H1, StructElem.H2, StructElem.H3,
                StructElem.H4, StructElem.H5, StructElem.H6};
        paragraph.SetStructureType(levels[headingLevel - 1]);
        Ensure(keep);
        if (!pdf.IsTagged()) {
            // The bookmark of the heading, at its level as it would be
            // tagged: a tagged document notes its headings as it draws them
            String title = System.Text.RegularExpressions.Regex.Replace(paragraph.Text().Trim(), @"\s+", " ");
            if (title.Length > 0) {
                pdf.headings.Add(new Heading(headingLevel, title, page, y));
            }
        }
        DrawParagraph(paragraph, x, width);
        y += Size() * 0.25f;
    }

    // The space above a heading.
    private float HeadingGap(MarkdownParser.Block block) {
        return Size() * (block.level <= 2 ? 1.4f : 1.1f);
    }

    // The text of a heading in the heading font, at its level's size and in
    // the heading color.
    private Paragraph HeadingParagraph(MarkdownParser.Block block) {
        float fontSize = Size() * HEADING_SIZES[block.level - 1];
        Paragraph paragraph = markup.Paragraph(block.text);
        foreach (TextLine line in paragraph.lines) {
            if (line.font == regular) {
                line.SetFont(headingFont);
            } else if (line.font == italic) {
                line.SetFont(boldItalic);
            }
            line.SetFontSize(fontSize);
            if (String.IsNullOrEmpty(line.GetURIAction()) && String.IsNullOrEmpty(line.GetGoToAction())) {
                line.SetTextColor(headingColor);
            }
        }
        return paragraph;
    }

    // Code in the code font on a light background, a line of the source at a
    // time, and a line too long for the width goes on under itself.
    private void DrawCode(String text, float x, float width) {
        if (text.Length == 0) {
            return;     // An empty block of code has nothing to tag, and no background
        }
        Gap(Size() * 0.75f);
        float padding = Size() * 0.5f;
        float leading = code.GetBodyHeight(code.GetSize()) * 1.2f;
        List<String> lines = CodeLines(text, width);
        Ensure(CodeHeight(text, width));
        // Code is an inline element, so the block is a paragraph that holds
        // its Code: PAC warns of a Code right under the document.
        OpenContainer(StructElem.P);
        OpenContainer(StructElem.CODE);
        int i = 0;
        while (i < lines.Count) {
            int fit = lines.Count - i;     // Lines of no height all fit
            if (leading > 0f) {
                fit = Math.Max(1, Util.SaturatingInt((bottom - y - 2f * padding) / leading));
            }
            int count = Math.Min(fit, lines.Count - i);
            new Rect(x, y, width, count * leading + 2f * padding).SetFillColor(CODE_BACKGROUND).DrawOn(page);
            float baseline = y + padding + code.GetAscent(code.GetSize()) + (leading - code.GetBodyHeight(code.GetSize())) / 2f;
            for (int j = 0; j < count; j++, i++) {
                if (Util.Trim(lines[i]).Length != 0) {
                    new TextLine(code, lines[i]).SetStructureType(StructElem.SPAN)
                            .SetLocation(x + padding, baseline).DrawOn(page);
                }
                baseline += leading;
            }
            y += count * leading + 2f * padding;
            atTop = false;
            if (i < lines.Count) {
                NewPage();
            }
        }
        CloseContainer();
        CloseContainer();
    }

    // A quote, indented, with a bar on its left.
    private void DrawQuote(MarkdownParser.Block block, float x, float width) {
        Gap(Size() * 0.75f);
        float indent = IndentWithin(Size() * 1.2f, width);
        // Room for its first block, which does not check the room it has
        Ensure(FirstKeep(block.children, width - indent, false));
        float[] bar = {x + 2f, y};
        quoteBars.Add(bar);
        OpenContainer(StructElem.BLOCKQUOTE);
        bool top = atTop;
        atTop = true;       // The first block of the quote starts where the bar does.
        DrawBlocks(block.children, x + indent, width - indent, false);
        atTop = top && atTop;
        CloseContainer();
        quoteBars.Remove(bar);
        DrawQuoteBar(page, bar[0], bar[1], y);
    }

    // Returns the indent, or less, so that the blocks inside a quote or a list
    // are not narrower than MIN_WIDTH sizes of the text.
    private float IndentWithin(float indent, float width) {
        return Math.Max(0f, Math.Min(indent, width - MIN_WIDTH * Size()));
    }

    // A list: the label of each item, a bullet or a number, to the left of
    // the blocks of the item.
    private void DrawList(MarkdownParser.Block list, float x, float width) {
        Gap(Size() * 0.75f);
        float indent = IndentWithin(Size() * (list.ordered ? 2f : 1.4f), width);
        OpenContainer(StructElem.L);
        int number = list.start;
        foreach (MarkdownParser.Block item in list.children) {
            if (item != list.children[0]) {
                Gap(list.loose ? Size() * 0.5f : Size() * 0.2f);
            }
            Ensure(FirstKeep(item.children, width - indent, !list.loose));
            OpenContainer(StructElem.LI);
            if (item.task != MarkdownParser.Task.NONE) {
                DrawTaskBox(item.task == MarkdownParser.Task.DONE, x, indent);
            } else {
                String label = list.ordered ? number.ToString(CultureInfo.InvariantCulture) + "." : "•";
                TextLine text = new TextLine(regular, label).SetStructureType(StructElem.LBL);
                float labelX = list.ordered ? x + indent - Size() * 0.4f - text.GetWidth() : x + Size() * 0.3f;
                text.SetLocation(labelX, y + regular.GetAscent(Size())).DrawOn(page);
            }
            OpenContainer(StructElem.LBODY);
            atTop = true;   // The first block of the item is on the line of its label.
            DrawBlocks(item.children, x + indent, width - indent, !list.loose);
            if (atTop) {
                // An item with no text still takes the line of its label.
                y += regular.GetBodyHeight(Size());
            }
            atTop = false;
            CloseContainer();
            CloseContainer();
            number++;
        }
        CloseContainer();
    }

    // Draws the box of a task in place of the label of its item, on the line
    // of its first text: a gray box, or a box of the check color with a white
    // check mark when the task is done. A screen reader reads it as ☐ or ☑,
    // the label of the item.
    private void DrawTaskBox(bool done, float x, float indent) {
        float size = Size();
        float side = size * 0.8f;
        float left = x + (indent - size * 0.4f - side) / 2f;
        float baseline = y + regular.GetAscent(size);
        float top = (baseline - size * 0.36f) - side / 2f;
        String actualText = done ? "☑" : "☐";
        // The box is drawn with the page's paths, not a Rect and Lines, which
        // are artifacts, as artifacts may not be inside the label.
        page.AddBDC(StructElem.LBL, actualText, actualText);
        page.SaveGraphicsState();
        List<Point> box = RoundedBox(left, top, side, side * 0.2f);
        if (done) {
            page.SetBrushColor(checkColor);
            page.DrawPath(box, PathOperator.FILL);
            page.SetPenColor(Color.white);
            page.SetPenWidth(side * 0.14f);
            page.SetLineCapStyle(CapStyle.ROUND);
            page.SetLineJoinStyle(JoinStyle.ROUND);
            page.MoveTo(left + side * 0.24f, top + side * 0.52f);
            page.LineTo(left + side * 0.43f, top + side * 0.70f);
            page.LineTo(left + side * 0.77f, top + side * 0.31f);
            page.StrokePath();
        } else {
            page.SetBrushColor(Color.white);
            page.SetPenColor(TASK_BOX_COLOR);
            page.SetPenWidth(size * 0.08f);
            page.DrawPath(box, PathOperator.FILL_AND_STROKE);
        }
        page.RestoreGraphicsState();
        page.AddEMC();
    }

    // The path of a square with rounded corners, of the side and the radius,
    // its top left corner at x and y.
    private static List<Point> RoundedBox(float x, float y, float side, float r) {
        const float k = 0.55228f;
        float c = r * k;
        char cp = Point.CONTROL_POINT_C;
        return new List<Point> {
            new Point(x + r, y), new Point((x + side) - r, y),
            new Point(((x + side) - r) + c, y, cp), new Point(x + side, (y + r) - c, cp), new Point(x + side, y + r),
            new Point(x + side, (y + side) - r),
            new Point(x + side, ((y + side) - r) + c, cp), new Point(((x + side) - r) + c, y + side, cp), new Point((x + side) - r, y + side),
            new Point(x + r, y + side),
            new Point((x + r) - c, y + side, cp), new Point(x, ((y + side) - r) + c, cp), new Point(x, (y + side) - r),
            new Point(x, y + r),
            new Point(x, (y + r) - c, cp), new Point((x + r) - c, y, cp), new Point(x + r, y),
        };
    }

    private void DrawRule(float x, float width) {
        Gap(Size() * 0.75f);
        Ensure(Size());
        float middle = y + Size() * 0.5f;
        new Line(x, middle, x + width, middle).SetStrokeColor(RULE_COLOR).SetStrokeWidth(1f).DrawOn(page);
        y += Size();
        atTop = false;
    }

    // A table of the text of the cells, with the header row in bold on a
    // light background, as wide as its text or as the width when it would be
    // wider, and on the next pages for the rows that do not fit. A cell that
    // starts with a check mark is in the check color, as a table of features
    // marks what each has.
    private void DrawTable(MarkdownParser.Block block, float x, float width) {
        Gap(Size() * 0.75f);
        Table table = TableOf(block, width);
        // A table that fits on a page is not split: it starts the next page
        // when it does not fit in what is left of this one, but under a
        // heading that keeps it, with which it does not fit on a page
        float height = table.GetTotalHeight();
        if (!atTop && !allowSplit && y + height > bottom && height <= bottom - marginTop) {
            NewPage();
        }
        allowSplit = false;
        // Room for its header and a row where it does not fit, not where it
        // does: a short table went to the next page alone
        float rowHeight = 2f * regular.GetBodyHeight(Size());
        if (y + height > bottom) {
            Ensure(2f * rowHeight);
        }
        table.SetLocation(x, marginTop);
        table.SetFirstPageTopMargin(y);
        table.SetBottomMargin(marginBottom);
        int before = pages.Count;
        float[] xy = table.DrawOn(pdf, page, pages, pageSize);
        if (pages.Count > before) {
            // The table drew its last rows on a page of its own. The bars of
            // the quotes it is in go down to the bottom of its first page and
            // the length of the pages between: they stopped where it started
            foreach (float[] bar in quoteBars) {
                DrawQuoteBar(page, bar[0], bar[1], bottom);
                for (int p = before; p < pages.Count - 1; p++) {
                    DrawQuoteBar(pages[p], bar[0], marginTop, bottom);
                }
            }
            page = pages[pages.Count - 1];
            page.structParent = Innermost();
            foreach (float[] bar in quoteBars) {
                bar[1] = marginTop;
            }
        }
        y = xy[1];
        atTop = false;
    }

    // Tells if the block is a paragraph that ends in a colon, which leads into
    // the table after it.
    private static bool LeadsIntoTable(MarkdownParser.Block block, MarkdownParser.Block next) {
        // Trimmed as Java's String.trim() trims, of the characters up to U+0020
        // only: C# Trim() also removes a no-break space
        // The kinds first: a list, a quote or a rule has no text
        if (block.kind != MarkdownParser.Kind.PARAGRAPH || next.kind != MarkdownParser.Kind.TABLE) {
            return false;
        }
        String text = Util.Trim(block.text);
        return text.EndsWith(":", StringComparison.Ordinal)
                || text.EndsWith("\uFF1A", StringComparison.Ordinal);     // Or the full-width colon of CJK text
    }

    // A paragraph that ends in a colon leads into the table after it, as
    // "The size of the PDFs, KB:" does, and starts a page with it when the two
    // fit on a page together but not in what is left of this one.
    private void KeepWithTable(MarkdownParser.Block paragraph, MarkdownParser.Block table, float gap,
            float x, float width) {
        if (atTop || allowSplit) {
            return;     // Its table starts under it, after a heading that keeps them
        }
        float room = bottom - marginTop;
        TextFrame frame = new TextFrame(new List<Paragraph> {markup.Paragraph(paragraph.text)}).SetParagraphGap(0f);
        frame.SetLocation(x, y + gap).SetWidth(width).SetHeight(room);
        frame.DrawOn(null);
        float needed = (frame.GetTextBottom() - y) + Size() * 0.75f + TableOf(table, width).GetTotalHeight();
        if (y + needed > bottom && needed - gap <= room) {
            NewPage();
        }
    }

    // The table of the text of the cells, as wide as its text or as the width.
    // The table of the block, made once: it was made to be measured and made
    // again to be drawn.
    private Table TableOf(MarkdownParser.Block block, float width) {
        Table table;
        if (!tables.TryGetValue(block, out table)) {
            table = MakeTable(block, width);
            tables[block] = table;
        }
        return table;
    }

    private Table MakeTable(MarkdownParser.Block block, float width) {
        List<List<Cell>> rows = new List<List<Cell>>();
        for (int r = 0; r < block.rows.Count; r++) {
            List<Cell> row = new List<Cell>();
            for (int c = 0; c < block.rows[r].Count; c++) {
                String text = block.rows[r][c];
                String plain = PlainText(text);
                Cell cell = new Cell((r == 0) ? bold : CellFont(text), plain);
                cell.properties |= Cell.LINE_BREAKS;     // At its <br>
                cell.SetTextAlignment(block.alignments[c]);
                if (r > 0 && plain.StartsWith(CHECK_MARK, StringComparison.Ordinal)) {
                    cell.SetTextColor(checkColor);
                }
                row.Add(cell);
            }
            rows.Add(row);
        }
        Table table = new Table();
        table.SetTableData(rows, 1);
        table.SetHeaderRowStyle(bold, 0x000000, TABLE_HEADER_BACKGROUND);
        table.SetCellBorderColor(0xC8CCD2);
        table.AutoAdjustColumnWidths();
        if (fullWidthTables || table.GetWidth() > width) {
            table.FitToWidth(width);
        }
        return table;
    }

    // The font of a cell: the font of its emphasis when the whole of its text
    // is in one, as **2.1** is in bold, so that a table can mark a value; the
    // regular font when it has none, or more than one, as a cell has one font.
    private Font CellFont(String text) {
        Font font = null;
        foreach (TextLine line in markup.Paragraph(text).lines) {
            if (Util.Trim(line.text).Length == 0) {
                continue;
            }
            if (font == null) {
                font = line.font;
            } else if (font != line.font) {
                return regular;
            }
        }
        return font ?? regular;
    }

    // The text of an image, its alternate description, as it reads: its
    // entities and escapes the characters they are, and its emphasis marks
    // left out. "Fish &amp; Chips" was its alternate description as written.
    private String AltText(MarkdownParser.Block block) {
        return Util.Trim(PlainText(block.text).Replace('\n', ' '));
    }

    // The text of a cell, without its inline markup, a line break at each
    // <br>.
    private String PlainText(String text) {
        StringBuilder buf = new StringBuilder();
        Paragraph paragraph = markup.Paragraph(text);
        for (int i = 0; i < paragraph.lines.Count; i++) {
            if (i > 0 && paragraph.lines[i - 1].lineBreaks > 0) {
                for (int k = 0; k < paragraph.lines[i - 1].lineBreaks; k++) {
                    buf.Append('\n');      // An empty line in the cell for each more <br>
                }
            } else if (i > 0 && !paragraph.JoinsPrevious(i)) {
                buf.Append(' ');
            }
            buf.Append(Util.Trim(paragraph.lines[i].text));
        }
        return buf.ToString();
    }

    // An image as wide as it is, or as the width when it is wider, and as tall
    // as the page when it is taller; or its text, in italic, when it is not read.
    private void DrawImage(MarkdownParser.Block block, float x, float width) {
        Gap(Size() * 0.75f);
        IDrawable drawable = ImageOf(block, width);
        if (drawable == null) {
            String alt = AltText(block);
            Paragraph paragraph = new Paragraph(new TextLine(italic, alt.Length == 0 ? block.source : alt));
            DrawParagraph(paragraph, x, width);
            return;
        }
        float height = imageHeights[block];
        Ensure(height);
        drawable.SetLocation(x, y);
        drawable.DrawOn(page);
        y += height;
        atTop = false;
    }

    // The image of the block, scaled to the width and to a page, made when it
    // is first measured or drawn, and its height kept; null for an image
    // that is not read, whose text is drawn in its place.
    private IDrawable ImageOf(MarkdownParser.Block block, float width) {
        IDrawable cached;
        if (images.TryGetValue(block, out cached)) {
            return cached;
        }
        String path = ImagePath(block.source);
        IDrawable drawable = null;
        float height = 0f;
        if (path != null) {
            String alt = AltText(block);
            if (alt.Length == 0) {
                alt = FileName(block.source);
            }
            float scale;
            float imageWidth;
            float imageHeight;
            if (path.ToLowerInvariant().EndsWith(".svg", StringComparison.Ordinal)) {
                SVGImage image = new SVGImage(path);
                image.SetAltDescription(alt);
                imageWidth = image.GetWidth();
                imageHeight = image.GetHeight();
                scale = Math.Min(1f, Math.Min(width / imageWidth, (bottom - marginTop) / imageHeight));
                image.ScaleBy(scale);
                drawable = image;
            } else {
                Image image = new Image(pdf, path);
                image.SetAltDescription(alt);
                imageWidth = image.GetWidth();
                imageHeight = image.GetHeight();
                scale = Math.Min(1f, Math.Min(width / imageWidth, (bottom - marginTop) / imageHeight));
                image.ScaleBy(scale);
                drawable = image;
            }
            height = imageHeight * scale;
        }
        images[block] = drawable;
        imageHeights[block] = height;
        return drawable;
    }

    // The path of an image in the image directory, or null when there is no
    // directory, or the source is an absolute path, a URL, has .. in it, names
    // a directory, or names no file. The source is read the same way in every
    // port: its parts between the slashes, less the empty ones and those that
    // are ".", are the names of the directories under the image directory and
    // of the file, and a source that ends in a slash or in "." names a
    // directory. An empty image directory is the working directory.
    internal String ImagePath(String source) {
        if (imageDirectory == null || source.StartsWith("/", StringComparison.Ordinal)
                || source.StartsWith("\\", StringComparison.Ordinal)
                || source.IndexOf(':') != -1 || source.IndexOf('\\') != -1) {
            return null;
        }
        String[] parts = source.Split('/');
        String last = parts[parts.Length - 1];
        if (last.Length == 0 || String.Equals(last, ".", StringComparison.Ordinal)) {
            return null;
        }
        String file = imageDirectory;
        foreach (String part in parts) {
            if (String.Equals(part, "..", StringComparison.Ordinal)) {
                return null;
            }
            if (part.Length != 0 && !String.Equals(part, ".", StringComparison.Ordinal)) {
                file = (file.Length == 0) ? part : System.IO.Path.Combine(file, part);
            }
        }
        // Only a file that has bytes, its links followed: any that was not a
        // directory was read, and a FIFO there blocked the reading, where Go
        // reads only a regular file (the review of 28 October 2026). .NET does
        // not tell a FIFO, a socket or a device from a regular file, and they
        // have a length of 0, as an empty file, which has no image either.
        try {
            FileSystemInfo info = new FileInfo(file);
            info = info.ResolveLinkTarget(true) ?? info;
            return (info is FileInfo target && target.Exists && target.Length > 0) ? file : null;
        } catch (IOException) {
            return null;    // No file, or a loop of links
        }
    }

    // The source as Java's File reads it: with one / where it has more, and
    // without the / at its end.
    private static String Normalize(String source) {
        StringBuilder buf = new StringBuilder();
        for (int i = 0; i < source.Length; i++) {
            char ch = source[i];
            if (ch != '/' || (buf.Length > 0 && buf[buf.Length - 1] != '/')) {
                buf.Append(ch);
            }
        }
        if (buf.Length > 1 && buf[buf.Length - 1] == '/') {
            buf.Length--;
        }
        return buf.ToString();
    }

    // The name of the file, after the last /, as Java's File.getName returns it.
    private static String FileName(String source) {
        String path = Normalize(source);
        return path.Substring(path.LastIndexOf('/') + 1);
    }
}   // End of Markdown.cs
}   // End of namespace PDFjet.NET
