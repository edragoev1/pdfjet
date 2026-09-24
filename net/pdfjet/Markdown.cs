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

namespace PDFjet.NET {
/// <summary>
/// Draws a Markdown text on as many pages as it needs: headings of # and of
/// underlines, paragraphs with the inline markup of Markup, bullet and
/// numbered lists that nest, block quotes, fenced and indented code, thematic
/// breaks, the tables of GitHub's Markdown, and images alone in their
/// paragraph. In a PDF/UA document the headings are H1 to H6, with no level
/// skipped, the lists are lists, the quotes BlockQuotes, the code Code, the
/// tables tables and the images figures, with their text as the alternate
/// description.
/// <para>
/// It is not all of Markdown: HTML is drawn as the text it is, a line break
/// is a space, links in a table cell are its text, and an image is drawn only
/// when it is alone in its paragraph, from the directory that
/// SetImageDirectory names; with none, its text is drawn instead, so that a
/// text from anyone reads no file. Please see Example_54.
/// </para>
/// </summary>
public class Markdown {
    // The heading sizes, as factors of the size of the text.
    private static readonly float[] HEADING_SIZES = {2.0f, 1.6f, 1.3f, 1.15f, 1.0f, 0.9f};
    private const int CODE_BACKGROUND = 0xF3F5F8;
    private const int RULE_COLOR = 0xC8CCD2;
    private const int QUOTE_BAR_COLOR = 0xB0B6BF;
    private const int TABLE_HEADER_BACKGROUND = 0xEEF0F3;

    private readonly Font regular;
    private readonly Font bold;
    private readonly Font italic;
    private readonly Font boldItalic;
    private readonly Font code;
    private readonly Markup markup;
    private Font headingFont;
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
    /// Sets the directory that the images are read from: an image of
    /// <c>![text](source)</c> is the file of that name in the directory, a
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
        quoteBars.Clear();
        List<MarkdownParser.Block> blocks = MarkdownParser.Parse(text);
        float width = pageSize.GetWidth() - marginLeft - marginRight;
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
            DrawQuoteBar(bar[0], bar[1], y);
        }
    }

    private void DrawQuoteBar(float x, float top, float to) {
        if (to > top) {
            new Line(x, top, x, to).SetStrokeColor(QUOTE_BAR_COLOR).SetStrokeWidth(2f).DrawOn(page);
        }
    }

    private void DrawBlocks(List<MarkdownParser.Block> blocks, float x, float width, bool tight) {
        foreach (MarkdownParser.Block block in blocks) {
            switch (block.kind) {
                case MarkdownParser.Kind.HEADING:
                    DrawHeading(block, x, width);
                    break;
                case MarkdownParser.Kind.PARAGRAPH:
                    Gap(tight ? Size() * 0.25f : Size() * 0.75f);
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
    private void DrawHeading(MarkdownParser.Block block, float x, float width) {
        float fontSize = Size() * HEADING_SIZES[block.level - 1];
        Gap(Size() * (block.level <= 2 ? 1.4f : 1.1f));
        Paragraph paragraph = markup.Paragraph(block.text);
        foreach (TextLine line in paragraph.lines) {
            if (line.font == regular) {
                line.SetFont(headingFont);
            } else if (line.font == italic) {
                line.SetFont(boldItalic);
            }
            line.SetFontSize(fontSize);
        }
        headingLevel = Math.Min(block.level, headingLevel + 1);
        StructElem[] levels = {StructElem.H1, StructElem.H2, StructElem.H3,
                StructElem.H4, StructElem.H5, StructElem.H6};
        paragraph.SetStructureType(levels[headingLevel - 1]);
        Ensure(headingFont.GetBodyHeight(fontSize) + 2f * regular.GetBodyHeight(Size()));
        DrawParagraph(paragraph, x, width);
        y += Size() * 0.25f;
    }

    // Code in the code font on a light background, a line of the source at a
    // time, and a line too long for the width goes on under itself.
    private void DrawCode(String text, float x, float width) {
        Gap(Size() * 0.75f);
        float padding = Size() * 0.5f;
        float leading = code.GetBodyHeight(code.GetSize()) * 1.2f;
        int columns = Math.Max(1, ToInt((width - 2f * padding) / code.StringWidth(code.GetSize(), "0")));
        List<String> lines = new List<String>();
        foreach (String line in text.Split('\n')) {
            int start = 0;
            while (line.Length - start > columns) {
                // A surrogate pair is not cut in two: it goes on the next
                // line, or on this one when it would be the whole line.
                int cut = start + columns;
                if (Char.IsHighSurrogate(line[cut - 1])) {
                    cut = (cut - 1 > start) ? cut - 1 : cut + 1;
                }
                lines.Add(line.Substring(start, cut - start));
                start = cut;
            }
            lines.Add(line.Substring(start));
        }
        Ensure(Math.Min(3, lines.Count) * leading + 2f * padding);
        OpenContainer(StructElem.CODE);
        int i = 0;
        while (i < lines.Count) {
            int fit = Math.Max(1, ToInt((bottom - y - 2f * padding) / leading));
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
    }

    // The float as an int, as Java's cast makes it: the nearest int for a
    // value out of range, and 0 for NaN.
    private static int ToInt(float value) {
        if (float.IsNaN(value)) {
            return 0;
        }
        if (value >= 2147483647f) {
            return Int32.MaxValue;
        }
        if (value <= -2147483648f) {
            return Int32.MinValue;
        }
        return (int) value;
    }

    // A quote, indented, with a bar on its left.
    private void DrawQuote(MarkdownParser.Block block, float x, float width) {
        Gap(Size() * 0.75f);
        Ensure(regular.GetBodyHeight(Size()));
        float indent = Size() * 1.2f;
        float[] bar = {x + 2f, y};
        quoteBars.Add(bar);
        OpenContainer(StructElem.BLOCKQUOTE);
        bool top = atTop;
        atTop = true;       // The first block of the quote starts where the bar does.
        DrawBlocks(block.children, x + indent, width - indent, false);
        atTop = top && atTop;
        CloseContainer();
        quoteBars.Remove(bar);
        DrawQuoteBar(bar[0], bar[1], y);
    }

    // A list: the label of each item, a bullet or a number, to the left of
    // the blocks of the item.
    private void DrawList(MarkdownParser.Block list, float x, float width) {
        Gap(Size() * 0.75f);
        float indent = Size() * (list.ordered ? 2f : 1.4f);
        OpenContainer(StructElem.L);
        int number = list.start;
        foreach (MarkdownParser.Block item in list.children) {
            if (item != list.children[0]) {
                Gap(list.loose ? Size() * 0.5f : Size() * 0.2f);
            }
            Ensure(regular.GetBodyHeight(Size()));
            OpenContainer(StructElem.LI);
            String label = list.ordered ? number.ToString(CultureInfo.InvariantCulture) + "." : "•";
            TextLine text = new TextLine(regular, label).SetStructureType(StructElem.LBL);
            float labelX = list.ordered ? x + indent - Size() * 0.4f - text.GetWidth() : x + Size() * 0.3f;
            text.SetLocation(labelX, y + regular.GetAscent(Size())).DrawOn(page);
            OpenContainer(StructElem.LBODY);
            bool top = atTop;
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
    // wider, and on the next pages for the rows that do not fit.
    private void DrawTable(MarkdownParser.Block block, float x, float width) {
        Gap(Size() * 0.75f);
        List<List<Cell>> rows = new List<List<Cell>>();
        for (int r = 0; r < block.rows.Count; r++) {
            List<Cell> row = new List<Cell>();
            for (int c = 0; c < block.rows[r].Count; c++) {
                Cell cell = new Cell((r == 0) ? bold : regular, PlainText(block.rows[r][c]));
                cell.SetTextAlignment(block.alignments[c]);
                row.Add(cell);
            }
            rows.Add(row);
        }
        Table table = new Table();
        table.SetTableData(rows, 1);
        table.SetHeaderRowStyle(bold, 0x000000, TABLE_HEADER_BACKGROUND);
        table.SetCellBorderColor(0xC8CCD2);
        table.AutoAdjustColumnWidths();
        if (table.GetWidth() > width) {
            table.FitToWidth(width);
        }
        float rowHeight = 2f * regular.GetBodyHeight(Size());
        Ensure(2f * rowHeight);
        table.SetLocation(x, marginTop);
        table.SetFirstPageTopMargin(y);
        table.SetBottomMargin(marginBottom);
        int before = pages.Count;
        float[] xy = table.DrawOn(pdf, page, pages, pageSize);
        if (pages.Count > before) {
            // The table drew its last rows on a page of its own.
            FinishQuoteBars();
            page = pages[pages.Count - 1];
            page.structParent = Innermost();
            foreach (float[] bar in quoteBars) {
                bar[1] = marginTop;
            }
        }
        y = xy[1];
        atTop = false;
    }

    // The text of a cell, without its inline markup.
    private String PlainText(String text) {
        StringBuilder buf = new StringBuilder();
        Paragraph paragraph = markup.Paragraph(text);
        for (int i = 0; i < paragraph.lines.Count; i++) {
            if (i > 0 && !paragraph.JoinsPrevious(i)) {
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
        String path = ImagePath(block.source);
        if (path == null) {
            Paragraph paragraph = new Paragraph(new TextLine(italic,
                    block.text.Length == 0 ? block.source : block.text));
            DrawParagraph(paragraph, x, width);
            return;
        }
        String alt = block.text.Length == 0 ? FileName(block.source) : block.text;
        float scale;
        float imageWidth;
        float imageHeight;
        IDrawable drawable;
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
        Ensure(imageHeight * scale);
        drawable.SetLocation(x, y);
        drawable.DrawOn(page);
        y += imageHeight * scale;
        atTop = false;
    }

    // The path of an image in the image directory, or null when there is no
    // directory, or the source is an absolute path, a URL, has .. in it, or
    // names no file.
    private String ImagePath(String source) {
        if (imageDirectory == null || source.StartsWith("/", StringComparison.Ordinal)
                || source.StartsWith("\\", StringComparison.Ordinal)
                || source.IndexOf(':') != -1 || source.IndexOf('\\') != -1) {
            return null;
        }
        foreach (String part in source.Split('/')) {
            if (String.Equals(part, "..", StringComparison.Ordinal)) {
                return null;
            }
        }
        String file = System.IO.Path.Combine(imageDirectory, Normalize(source));
        return (File.Exists(file) && !Directory.Exists(file)) ? file : null;
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
