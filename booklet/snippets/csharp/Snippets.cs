/*
 * Snippets.cs
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
using System;
using System.IO;
using System.Collections.Generic;
using PDFjet.NET;

/// <summary>
/// The code of the PDFjet booklet: each method after an "@snippet" comment is
/// one snippet, and the booklet shows its body. A snippet that takes a PDF and
/// a font draws in a PDF of its own, named after it, with IBM Plex Sans at 12
/// points; one that takes nothing writes its own files. Run it in a folder
/// with the fonts, images and data folders of the repository, as
/// booklet/check-snippets.sh does.
/// </summary>
public class Snippets {
    delegate void Snippet(PDF pdf, Font font);

    // --- Documents and pages ------------------------------------------------

    // @snippet document-info
    static void DocumentInfo(PDF pdf, Font font) {
        pdf.SetTitle("Annual Report 2026");
        pdf.SetAuthor("PDFjet Software");
        pdf.SetSubject("The year in numbers");
        pdf.SetKeywords("report, 2026");
        pdf.SetLanguage("en-US");
        Page page = new Page(pdf, Letter.PORTRAIT);
        new TextLine(font, "Annual Report 2026").SetLocation(50f, 50f).DrawOn(page);
    }

    // @snippet page-sizes
    static void PageSizes(PDF pdf, Font font) {
        Page page = new Page(pdf, A4.LANDSCAPE);
        String size = (int) page.GetWidth() + " x " + (int) page.GetHeight() + " points";
        new TextLine(font, size).SetLocation(50f, 50f).DrawOn(page);
        page = new Page(pdf, new PageSize(400f, 300f));    // Any width and height
        new TextLine(font, "400 x 300 points").SetLocation(50f, 50f).DrawOn(page);
    }

    // @snippet page-numbers
    static void PageNumbers(PDF pdf, Font font) {
        List<Page> pages = new List<Page>();
        for (int i = 0; i < 3; i++) {
            Page page = new Page(pdf, Letter.PORTRAIT, Page.DETACHED);
            new TextLine(font, "Chapter " + (i + 1)).SetLocation(50f, 50f).DrawOn(page);
            pages.Add(page);
        }
        for (int i = 0; i < pages.Count; i++) {
            TextLine footer = new TextLine(font, "Page " + (i + 1) + " of " + pages.Count);
            pages[i].AddFooter(footer, 30f);    // The baseline 30 points from the bottom
        }
        pdf.AddPages(pages);
    }

    // @snippet watermark
    static void Watermark(PDF pdf, Font font) {
        Font bold = new Font(pdf, IBMPlexSans.Bold).SetSize(120f);
        Page page = new Page(pdf, Letter.PORTRAIT);
        page.AddWatermark(bold, "DRAFT");
        new TextLine(font, "The text is drawn over the watermark.").SetLocation(50f, 50f).DrawOn(page);
    }

    // @snippet accessible-document
    static void AccessibleDocument() {
        PDF pdf = new PDF(new BufferedStream(new FileStream("accessible.pdf", FileMode.Create)),
                Compliance.PDF_UA_1);
        pdf.SetTitle("An accessible document");
        Font font = new Font(pdf, IBMPlexSans.Regular);
        Page page = new Page(pdf, Letter.PORTRAIT);
        new TextLine(font, "A heading").SetFontSize(18f).SetStructureType(StructElem.H1)
                .SetLocation(50f, 50f).DrawOn(page);
        new TextBlock(font, "A paragraph, which a screen reader reads after the heading.")
                .SetLocation(50f, 70f).SetWidth(400f).DrawOn(page);
        Image image = new Image(pdf, "images/linux-logo.png");
        image.SetAltDescription("Tux, the penguin of Linux");
        image.SetLocation(50f, 110f).DrawOn(page);
        pdf.Complete();
    }

    // @snippet archival-document
    static void ArchivalDocument() {
        PDF pdf = new PDF(new BufferedStream(new FileStream("archival.pdf", FileMode.Create)),
                Compliance.PDF_A_2B);
        pdf.SetTitle("An archival document");
        Font font = new Font(pdf, IBMPlexSans.Regular);
        Page page = new Page(pdf, Letter.PORTRAIT);
        new TextLine(font, "This document will look the same in fifty years.")
                .SetLocation(50f, 50f).DrawOn(page);
        pdf.Complete();
    }

    // --- Fonts --------------------------------------------------------------

    // @snippet fonts
    static void Fonts(PDF pdf, Font font) {
        Font serif = new Font(pdf, SourceSerif4.Regular).SetSize(14f);
        Font bold = new Font(pdf, IBMPlexSans.Bold);
        Font mono = new Font(pdf, JetBrainsMono.Regular).SetSize(10f);
        Page page = new Page(pdf, Letter.PORTRAIT);
        new TextLine(serif, "Source Serif 4 at 14 points").SetLocation(50f, 50f).DrawOn(page);
        new TextLine(bold, "IBM Plex Sans Bold at 12 points").SetLocation(50f, 75f).DrawOn(page);
        new TextLine(mono, "JetBrains Mono at 10 points").SetLocation(50f, 100f).DrawOn(page);
        new TextLine(bold, "The same font at 24 points").SetFontSize(24f)
                .SetLocation(50f, 135f).DrawOn(page);
    }

    // @snippet font-files
    static void FontFiles(PDF pdf, Font font) {
        Font otf = new Font(pdf, "fonts/IBMPlexSans/IBMPlexSans-Regular.otf");
        Font ttf = new Font(pdf, "fonts/NotoSans/NotoSans-Regular.ttf");
        Page page = new Page(pdf, Letter.PORTRAIT);
        new TextLine(otf, "An OpenType font").SetLocation(50f, 50f).DrawOn(page);
        new TextLine(ttf, "A TrueType font").SetLocation(50f, 75f).DrawOn(page);
    }

    // @snippet core-fonts
    static void CoreFonts(PDF pdf, Font font) {
        Font helvetica = new Font(pdf, CoreFont.HELVETICA_BOLD).SetSize(24f);
        Page page = new Page(pdf, Letter.PORTRAIT);
        new TextLine(helvetica, "WAVE AWAY").SetLocation(50f, 50f).DrawOn(page);
        helvetica.SetKernPairs(true);
        new TextLine(helvetica, "WAVE AWAY").SetLocation(50f, 85f).DrawOn(page);
    }

    // @snippet cjk-fonts
    static void CjkFonts(PDF pdf, Font font) {
        Font chinese = new Font(pdf, CJKFont.ST_HEITI_SC_LIGHT).SetSize(24f);
        Font japanese = new Font(pdf, CJKFont.KOZ_MIN_PRO_VI_REGULAR).SetSize(24f);
        Font korean = new Font(pdf, CJKFont.ADOBE_MYUNGJO_STD_MEDIUM).SetSize(24f);
        Page page = new Page(pdf, Letter.PORTRAIT);
        new TextLine(chinese, "新年快乐!").SetLocation(50f, 50f).DrawOn(page);
        new TextLine(japanese, "明けましておめでとう!").SetLocation(50f, 90f).DrawOn(page);
        new TextLine(korean, "새해 복 많이 받으세요!").SetLocation(50f, 130f).DrawOn(page);
    }

    // @snippet fallback-font
    static void FallbackFont(PDF pdf, Font font) {
        Font japanese = new Font(pdf, IBMPlexSansJP.Regular);
        Page page = new Page(pdf, Letter.PORTRAIT);
        new TextLine(font, "Japanese is 日本語 in Japanese.")
                .SetFallbackFont(japanese).SetLocation(50f, 50f).DrawOn(page);
    }

    // @snippet font-metrics
    static void FontMetrics(PDF pdf, Font font) {
        Page page = new Page(pdf, Letter.PORTRAIT);
        String text = "Right aligned at x = 500";
        float width = font.StringWidth(text);
        new TextLine(font, text).SetLocation(500f - width, 50f).DrawOn(page);
        // The next line goes one line height of the font below.
        float y = 50f + font.GetBodyHeight();
        new TextLine(font, "The next line").SetLocation(500f - font.StringWidth("The next line"), y)
                .DrawOn(page);
    }

    // --- Text ---------------------------------------------------------------

    // @snippet text-line
    static void TextLine(PDF pdf, Font font) {
        Page page = new Page(pdf, Letter.PORTRAIT);
        TextLine text = new TextLine(font, "Hello, World!");
        text.SetFontSize(20f);
        text.SetTextColor(Color.navy);
        text.SetUnderline(true);
        text.SetLocation(50f, 50f);
        float[] xy = text.DrawOn(page);
        // DrawOn returns where the text ends: the next line starts after it.
        new TextLine(font, "Turned by 15 degrees").SetTextRotation(15)
                .SetLocation(xy[0] + 20f, 50f).DrawOn(page);
    }

    // @snippet text-block
    static void TextBlock(PDF pdf, Font font) {
        Page page = new Page(pdf, Letter.PORTRAIT);
        TextBlock block = new TextBlock(font,
                "A text block wraps its text at its width. Its lines break between words, "
                + "and an empty line starts a new paragraph.\n\nLike this one.");
        block.SetLocation(50f, 50f);
        block.SetWidth(250f);
        block.SetLineSpacing(1.4f);
        block.SetPadding(10f);
        block.SetBorderColor(Color.blue);
        float[] xy = block.DrawOn(page);
        new TextLine(font, "Below the block").SetLocation(50f, xy[1] + 20f).DrawOn(page);
    }

    // @snippet highlighted-words
    static void HighlightedWords(PDF pdf, Font font) {
        Dictionary<String, int> colors = new Dictionary<String, int>();
        colors["PDFjet"] = Color.blue;
        colors["fast"] = Color.red;
        Page page = new Page(pdf, Letter.PORTRAIT);
        new TextBlock(font, "PDFjet is fast, and PDFjet is small.")
                .SetHighlightColors(colors).SetLocation(50f, 50f).SetWidth(400f).DrawOn(page);
    }

    // @snippet paragraphs
    static void Paragraphs(PDF pdf, Font font) {
        Font bold = new Font(pdf, IBMPlexSans.Bold);
        Font italic = new Font(pdf, IBMPlexSans.Italic);
        List<Paragraph> paragraphs = new List<Paragraph>();
        paragraphs.Add(new Paragraph()
                .Add(new TextLine(font, "A paragraph mixes"))
                .Add(new TextLine(bold, "fonts,"))
                .Add(new TextLine(italic, "styles").SetTextColor(Color.darkred))
                .Add(new TextLine(font, "and colors, and wraps them as one text.")));
        paragraphs.Add(new Paragraph(new TextLine(font, "Centered.")).SetTextAlignment(Alignment.CENTER));
        TextFrame frame = new TextFrame(paragraphs);
        frame.SetLocation(50f, 50f);
        frame.SetWidth(250f);
        frame.DrawOn(new Page(pdf, Letter.PORTRAIT));
    }

    // @snippet lists
    static void Lists(PDF pdf, Font font) {
        String[] items = {"Create the PDF", "Draw on its pages", "Complete it"};
        List<Paragraph> paragraphs = new List<Paragraph>();
        for (int i = 0; i < items.Length; i++) {
            paragraphs.Add(new Paragraph(new TextLine(font, items[i]))
                    .SetListLabel(new TextLine(font, (i + 1) + "."), 20f));
        }
        new TextFrame(paragraphs).SetLocation(50f, 50f).SetWidth(300f)
                .DrawOn(new Page(pdf, Letter.PORTRAIT));
    }

    // @snippet markup
    static void Markup(PDF pdf, Font font) {
        Font bold = new Font(pdf, IBMPlexSans.Bold);
        Font italic = new Font(pdf, IBMPlexSans.Italic);
        Font boldItalic = new Font(pdf, IBMPlexSans.BoldItalic);
        Font code = new Font(pdf, JetBrainsMono.Regular).SetSize(11f);
        Markup markup = new Markup(font, bold, italic, boldItalic, code);
        List<Paragraph> paragraphs = markup.Paragraphs(
                "Write **bold**, *italic* and `code`, and link to [PDFjet](https://pdfjet.com).\n\n"
                + "An empty line starts a new paragraph.");
        new TextFrame(paragraphs).SetLocation(50f, 50f).SetWidth(300f)
                .DrawOn(new Page(pdf, Letter.PORTRAIT));
    }

    // @snippet markdown
    static void Markdown(PDF pdf, Font font) {
        Font bold = new Font(pdf, IBMPlexSans.Bold);
        Font italic = new Font(pdf, IBMPlexSans.Italic);
        Font boldItalic = new Font(pdf, IBMPlexSans.BoldItalic);
        Font code = new Font(pdf, JetBrainsMono.Regular).SetSize(10f);
        Markdown markdown = new Markdown(font, bold, italic, boldItalic, code);
        List<Page> pages = new List<Page>();
        markdown.DrawOn(pdf, "# A heading\n"
                + "\n"
                + "A paragraph with **bold** text and a [link](https://pdfjet.com).\n"
                + "\n"
                + "- an item\n"
                + "- another item\n", pages, Letter.PORTRAIT);
        pdf.AddPages(pages);
    }

    // @snippet text-column
    static void TextColumn(PDF pdf, Font font) {
        TextColumn column = new TextColumn();
        column.AddParagraph(new Paragraph(new TextLine(font,
                "A text column draws its paragraphs one under the other.")));
        column.AddParagraph(new Paragraph(new TextLine(font,
                "It justifies them, and puts space between them.")));
        column.SetLocation(50f, 50f);
        column.SetWidth(200f);
        column.SetTextAlignment(Alignment.JUSTIFY);
        column.SetParagraphSpacing(1.5f);
        column.DrawOn(new Page(pdf, Letter.PORTRAIT));
    }

    // @snippet text-frame-pages
    static void TextFramePages(PDF pdf, Font font) {
        List<String> text = new List<String>(
                Content.OfTextFile("data/dostoevsky.txt").Split("\n\n"));
        TextFrame frame = new TextFrame(font, text);
        frame.SetLocation(72f, 72f);
        frame.SetWidth(468f);
        List<Page> pages = new List<Page>();
        frame.DrawOn(pdf, pages, Letter.PORTRAIT);  // As many pages as the text needs
        pdf.AddPages(pages);
    }

    // @snippet text-frame-columns
    static void TextFrameColumns(PDF pdf, Font font) {
        List<String> text = new List<String>(
                Content.OfTextFile("data/dostoevsky.txt").Split("\n\n"));
        TextFrame frame = new TextFrame(font, text);
        while (frame.HasMoreText()) {
            Page page = new Page(pdf, Letter.LANDSCAPE);
            for (float x = 50f; x < 700f && frame.HasMoreText(); x += 250f) {
                frame.SetLocation(x, 50f).SetWidth(230f).SetHeight(500f);
                frame.DrawOn(page);    // Draws what fits and keeps the rest
            }
        }
    }

    // @snippet right-to-left
    static void RightToLeft(PDF pdf, Font font) {
        Font hebrew = new Font(pdf, IBMPlexSansHebrew.Regular);
        Font arabic = new Font(pdf, IBMPlexSansArabic.Regular);
        Page page = new Page(pdf, Letter.PORTRAIT);
        new TextBlock(hebrew, "שלום עולם! זהו טקסט בעברית.")
                .SetRightToLeft(true).SetLanguage("he")
                .SetLocation(50f, 50f).SetWidth(300f).DrawOn(page);
        String text = Bidi.ReorderVisually("مرحبا بالعالم!");
        new TextLine(arabic, text).SetLanguage("ar")
                .SetLocation(350f - arabic.StringWidth(text), 100f).DrawOn(page);
    }

    // @snippet formulas
    static void Formulas(PDF pdf, Font font) {
        Page page = new Page(pdf, Letter.PORTRAIT);
        new CompositeTextLine(50f, 50f).SetFontSize(16f)
                .AddFormula(font, "C6H12O6").DrawOn(page);
        new CompositeTextLine(50f, 80f).SetFontSize(16f)
                .AddFormula(font, "SO4^2-").DrawOn(page);
    }

    // --- Tables -------------------------------------------------------------

    // @snippet table
    static void Table(PDF pdf, Font font) {
        Font bold = new Font(pdf, IBMPlexSans.SemiBold);
        String[][] data = {
            new String[] {"Planet", "Moons", "Day, hours"},
            new String[] {"Earth", "1", "24"},
            new String[] {"Mars", "2", "24.6"},
            new String[] {"Jupiter", "95", "9.9"},
        };
        List<List<Cell>> rows = new List<List<Cell>>();
        for (int i = 0; i < data.Length; i++) {
            List<Cell> row = new List<Cell>();
            foreach (String text in data[i]) {
                row.Add(new Cell(i == 0 ? bold : font, text));
            }
            rows.Add(row);
        }
        Table table = new Table();
        table.SetTableData(rows, 1);    // One header row
        table.SetLocation(50f, 50f);
        table.AutoAdjustColumnWidths();
        table.RightAlignNumbers();
        table.DrawOn(new Page(pdf, Letter.PORTRAIT));
    }

    // @snippet table-pages
    static void TablePages(PDF pdf, Font font) {
        Font bold = new Font(pdf, IBMPlexSans.SemiBold);
        List<List<Cell>> rows = new List<List<Cell>>();
        rows.Add(new List<Cell> {new Cell(bold, "Number"), new Cell(bold, "Square")});
        for (int i = 1; i <= 200; i++) {
            rows.Add(new List<Cell> {new Cell(font, i.ToString()),
                    new Cell(font, (i * i).ToString())});
        }
        Table table = new Table();
        table.SetTableData(rows, 1);    // The header row is drawn on every page
        table.SetLocation(50f, 50f);
        table.SetBottomMargin(50f);
        table.SetColumnWidth(0, 100f).SetColumnWidth(1, 100f);
        List<Page> pages = new List<Page>();
        table.DrawOn(pdf, pages, Letter.PORTRAIT);
        pdf.AddPages(pages);
    }

    // @snippet cells
    static void Cells(PDF pdf, Font font) {
        Cell title = new Cell(font, "Spans two columns");
        title.SetColSpan(2);
        title.SetTextAlignment(Alignment.CENTER);
        title.SetBackgroundColor(Color.lightblue);
        Cell left = new Cell(font, "Left").SetTextColor(Color.darkred);
        Cell right = new Cell(font, "Right").SetTextAlignment(Alignment.RIGHT);
        right.SetBorder(Border.LEFT, false);
        List<List<Cell>> rows = new List<List<Cell>>();
        rows.Add(new List<Cell> {title, new Cell(font)});    // The cell the span covers
        rows.Add(new List<Cell> {left, right});
        Table table = new Table();
        table.SetTableData(rows);
        table.SetColumnWidth(0, 120f).SetColumnWidth(1, 120f);
        table.SetLocation(50f, 50f).DrawOn(new Page(pdf, Letter.PORTRAIT));
    }

    // @snippet cell-content
    static void CellContent(PDF pdf, Font font) {
        Image image = new Image(pdf, "images/linux-logo.png").ScaleBy(0.25f);
        image.SetAltDescription("Tux");
        Cell imageCell = new Cell(font).SetImage(image);
        Cell barcodeCell = new Cell(font).SetBarcode(new Barcode(Barcode.CODE_128, "PDFjet"));
        Cell textCell = new Cell(font).SetTextBlock(
                new TextBlock(font, "A text block wraps in its cell, which grows to fit it."));
        List<List<Cell>> rows = new List<List<Cell>>();
        rows.Add(new List<Cell> {imageCell, barcodeCell, textCell});
        Table table = new Table();
        table.SetTableData(rows);
        table.SetColumnWidth(0, 80f).SetColumnWidth(1, 150f).SetColumnWidth(2, 150f);
        table.SetLocation(50f, 50f).DrawOn(new Page(pdf, Letter.PORTRAIT));
    }

    // @snippet table-style
    static void TableStyle(PDF pdf, Font font) {
        Font bold = new Font(pdf, IBMPlexSans.SemiBold);
        List<List<Cell>> rows = new List<List<Cell>>();
        rows.Add(new List<Cell> {new Cell(font, "Item"), new Cell(font, "Price")});
        for (int i = 1; i <= 6; i++) {
            rows.Add(new List<Cell> {new Cell(font, "Item " + i), new Cell(font, i + ".99")});
        }
        Table table = new Table();
        table.SetTableData(rows, 1);
        table.SetHeaderRowStyle(bold, Color.white, Color.navy);
        table.SetAlternateRowColor(Color.aliceblue);
        table.SetColumnWidthsInPercent(70f, 30f).SetWidth(300f);
        table.RightAlignNumbers();
        table.SetLocation(50f, 50f).DrawOn(new Page(pdf, Letter.PORTRAIT));
    }

    // @snippet table-sums
    static void TableSums(PDF pdf, Font font) {
        Font bold = new Font(pdf, IBMPlexSans.SemiBold);
        List<List<Cell>> rows = new List<List<Cell>>();
        rows.Add(new List<Cell> {new Cell(bold, "Day"), new Cell(bold, "Sales")});
        for (int i = 1; i <= 100; i++) {
            rows.Add(new List<Cell> {new Cell(font, "Day " + i), new Cell(font, (10 * i).ToString())});
        }
        rows.Add(new List<Cell> {new Cell(bold, "Page total"), new Cell(bold)});
        rows.Add(new List<Cell> {new Cell(bold, "Carried forward"), new Cell(bold)});
        Table table = new Table();
        table.SetTableData(rows, 1);
        table.SetNumberOfFooterRows(2);    // The last two rows end every page
        table.SetPageSum(rows.Count - 2, 1, 0);       // Row, column, decimals
        table.SetRunningSum(rows.Count - 1, 1, 0);
        table.RightAlignNumbers();
        table.SetColumnWidth(0, 150f).SetColumnWidth(1, 100f);
        table.SetLocation(50f, 50f).SetBottomMargin(50f);
        List<Page> pages = new List<Page>();
        table.DrawOn(pdf, pages, Letter.PORTRAIT);
        pdf.AddPages(pages);
    }

    // @snippet big-table
    static void BigTable(PDF pdf, Font font) {
        Font bold = new Font(pdf, IBMPlexSans.SemiBold).SetSize(10f);
        Font body = new Font(pdf, IBMPlexSans.Regular).SetSize(9f);
        BigTable table = new BigTable(pdf, bold, body, Letter.LANDSCAPE);
        table.SetNumberOfColumns(9);    // Set in this order
        table.SetTableData("data/Electric_Vehicle_Population_10_Pages.csv", ",");
        table.SetLocation(0f, 0f);
        table.SetBottomMargin(20f);
        table.Complete();
    }

    // --- Graphics -----------------------------------------------------------

    // @snippet shapes
    static void Shapes(PDF pdf, Font font) {
        Page page = new Page(pdf, Letter.PORTRAIT);
        new Line(50f, 50f, 250f, 50f).SetStrokeWidth(2f).SetStrokeColor(Color.blue).DrawOn(page);
        new Rect(50f, 70f, 100f, 60f).SetFillColor(Color.lightblue).SetCornerRadius(8f)
                .DrawOn(page);
        new Point(210f, 100f).SetShape(Shape.STAR).SetRadius(25f).SetFillColor(Color.gold)
                .DrawOn(page);
        new Ellipse().SetLocation(320f, 100f).SetRadiusX(50f).SetRadiusY(30f)
                .SetFillColor(Color.lightgreen).DrawOn(page);
        new Arc().SetLocation(450f, 100f).SetRadiusX(30f).SetRadiusY(30f)
                .SetStartAngle(0f).SetSweep(270f).SetStrokeWidth(4f).DrawOn(page);
    }

    // @snippet paths
    static void Paths(PDF pdf, Font font) {
        var triangle = new PDFjet.NET.Path();
        triangle.Add(new Point(100f, 50f));
        triangle.Add(new Point(150f, 130f));
        triangle.Add(new Point(50f, 130f));
        triangle.SetClosed(true);
        triangle.SetFillShape(true);
        triangle.SetStrokeColor(Color.darkorange);
        var curve = new PDFjet.NET.Path();
        curve.Add(new Point(200f, 130f));
        curve.Add(new Point(250f, 20f, Point.CONTROL_POINT_C));   // Bezier control points
        curve.Add(new Point(300f, 200f, Point.CONTROL_POINT_C));
        curve.Add(new Point(350f, 90f));
        curve.SetStrokeWidth(3f);
        Page page = new Page(pdf, Letter.PORTRAIT);
        triangle.DrawOn(page);
        curve.DrawOn(page);
    }

    // @snippet page-graphics
    static void PageGraphics(PDF pdf, Font font) {
        Page page = new Page(pdf, Letter.PORTRAIT);
        page.SetPenColor(Color.navy);
        page.SetPenWidth(2f);
        page.SetStrokeDashPattern("[6 3] 0");
        page.MoveTo(50f, 50f);
        page.LineTo(250f, 50f);
        page.LineTo(250f, 150f);
        page.StrokePath();
        page.SetBrushColor(Color.coral);
        page.FillRect(50f, 80f, 150f, 70f);
    }

    // @snippet transparency
    static void Transparency(PDF pdf, Font font) {
        Page page = new Page(pdf, Letter.PORTRAIT);
        page.SetBrushColor(Color.blue);
        page.FillRect(50f, 50f, 100f, 100f);
        GraphicsState gs = new GraphicsState();
        gs.SetAlphaNonStroking(0.5f);    // Fills are 50% transparent
        page.SaveGraphicsState();
        page.SetGraphicsState(gs);
        page.SetBrushColor(Color.red);
        page.FillRect(100f, 100f, 100f, 100f);
        page.RestoreGraphicsState();
    }

    // @snippet containers
    static void Containers(PDF pdf, Font font) {
        Container container = new Container(200f, 100f);
        container.Add(new Rect(0f, 0f, 200f, 100f).SetFillColor(Color.lightyellow));
        container.Add(new TextLine(font, "Turned together").SetLocation(20f, 55f));
        container.SetLocation(150f, 100f);
        container.SetRotation(-30);
        container.DrawOn(new Page(pdf, Letter.PORTRAIT));
    }

    // @snippet stamps
    static void Stamps(PDF pdf, Font font) {
        Stamp stamp = new Stamp(pdf).SetSize(120f, 40f).AddFont(font);
        stamp.SetStrokeColor(Color.red).SetStrokeWidth(3f).DrawRect(0f, 0f, 120f, 40f);
        stamp.SetFillColor(Color.red).DrawText(new TextParameters()
                .SetFont(font).SetFontSize(20f).SetLocation(18f, 28f).SetText("PAID"));
        stamp.Complete();    // Written once, drawn any number of times
        Page page = new Page(pdf, Letter.PORTRAIT);
        stamp.SetLocation(50f, 50f).DrawOn(page);
        stamp.SetRotation(-15).SetLocation(250f, 60f).DrawOn(page);
    }

    // --- Images -------------------------------------------------------------

    // @snippet images
    static void Images(PDF pdf, Font font) {
        Image photo = new Image(pdf, "images/gr-map.jpg");    // JPEG, PNG or BMP
        photo.SetAltDescription("A map of Greece");
        Page page = new Page(pdf, Letter.PORTRAIT);
        photo.SetLocation(50f, 50f);
        photo.ResizeWidth(250f);
        float[] xy = photo.DrawOn(page);
        // The image is written to the PDF once, however many times it is drawn.
        photo.SetLocation(50f, xy[1] + 10f).ResizeWidth(100f);
        photo.DrawOn(page);
    }

    // @snippet svg-images
    static void SvgImages(PDF pdf, Font font) {
        SVGImage icon = new SVGImage("images/svg/star_FILL0_wght400_GRAD0_opsz48.svg");
        icon.SetAltDescription("A star");
        icon.SetLocation(50f, 50f);
        icon.ScaleBy(2f);    // Vector graphics stay sharp at any size
        icon.DrawOn(new Page(pdf, Letter.PORTRAIT));
    }

    // --- Charts -------------------------------------------------------------

    // @snippet line-chart
    static void LineChart(PDF pdf, Font font) {
        Font small = new Font(pdf, IBMPlexSans.Regular).SetSize(8f);
        Chart chart = new Chart(font, small);
        chart.SetLocation(50f, 50f);
        chart.SetSize(400f, 250f);
        chart.SetTitle("Visitors");
        chart.SetXAxisTitle("Month");
        chart.SetYAxisTitle("Thousands");
        Series series = chart.AddSeries("2026").SetDrawPath(true).SetStrokeColor(Color.blue);
        float[] visitors = {3f, 5f, 4f, 7f, 9f, 8f};
        for (int month = 1; month <= visitors.Length; month++) {
            series.AddPoint(month, visitors[month - 1]);
        }
        chart.DrawOn(new Page(pdf, Letter.PORTRAIT));
    }

    // @snippet bar-chart
    static void BarChart(PDF pdf, Font font) {
        Font small = new Font(pdf, IBMPlexSans.Regular).SetSize(8f);
        BarChart chart = new BarChart(font, small);
        chart.SetLocation(50f, 50f);
        chart.SetSize(400f, 250f);
        chart.SetTitle("Units sold");
        chart.SetCategories(new String[] {"Q1", "Q2", "Q3", "Q4"});
        chart.AddSeries("2025", new float[] {45f, 65f, 31f, 52f}, Color.seagreen);
        chart.AddSeries("2026", new float[] {75f, 20f, 73f, 80f}, Color.indianred);
        chart.DrawOn(new Page(pdf, Letter.PORTRAIT));
    }

    // @snippet donut-chart
    static void DonutChart(PDF pdf, Font font) {
        Font bold = new Font(pdf, IBMPlexSans.Bold).SetSize(10f);
        DonutChart chart = new DonutChart(font, bold);
        chart.SetLocation(100f, 100f);
        chart.SetRadii(100f, 60f);    // An inner radius of 0 makes a pie chart
        chart.AddSlice(new Slice(50f, Color.steelblue, "Java"));
        chart.AddSlice(new Slice(30f, Color.seagreen, "C#"));
        chart.AddSlice(new Slice(20f, Color.darkorange, "Go"));
        chart.DrawOn(new Page(pdf, Letter.PORTRAIT));
    }

    // @snippet calendar
    static void Calendar(PDF pdf, Font font) {
        Font bold = new Font(pdf, IBMPlexSans.Bold).SetSize(10f);
        Font small = new Font(pdf, IBMPlexSans.Regular).SetSize(9f);
        CalendarMonth month = new CalendarMonth(bold, small, 2026, 12);
        month.SetFirstDayOfWeek(DayOfWeek.Monday);
        month.SetLocation(50f, 50f);
        month.DrawOn(new Page(pdf, Letter.PORTRAIT));
    }

    // --- Barcodes -----------------------------------------------------------

    // @snippet barcodes
    static void Barcodes(PDF pdf, Font font) {
        Page page = new Page(pdf, Letter.PORTRAIT);
        Barcode code128 = new Barcode(Barcode.CODE_128, "PDFjet 9.0");
        code128.SetFont(font);    // The text under the bars
        code128.SetLocation(50f, 50f);
        float[] xy = code128.DrawOn(page);
        Barcode ean13 = new Barcode(Barcode.EAN_13, "051234567890");    // The check digit is added
        ean13.SetFont(font);
        ean13.SetModuleLength(1f);
        ean13.SetLocation(50f, xy[1] + 30f);
        ean13.DrawOn(page);
    }

    // @snippet qr-code
    static void QrCode(PDF pdf, Font font) {
        QRCode qr = new QRCode("https://pdfjet.com", ErrorCorrectionLevel.M);
        qr.SetModuleLength(4f);
        qr.SetLocation(50f, 50f);
        qr.DrawOn(new Page(pdf, Letter.PORTRAIT));
    }

    // @snippet data-matrix
    static void DataMatrix(PDF pdf, Font font) {
        DataMatrix code = new DataMatrix("Grüße aus München!");
        code.SetModuleLength(4f);
        code.SetLocation(50f, 50f);
        code.DrawOn(new Page(pdf, Letter.PORTRAIT));
    }

    // @snippet pdf417
    static void Pdf417(PDF pdf, Font font) {
        PDF417 code = new PDF417("PDF417 holds text and binary data, and survives damage.");
        code.SetModuleLength(1f);
        code.SetLocation(50f, 50f);
        code.DrawOn(new Page(pdf, Letter.PORTRAIT));
    }

    // --- Links, bookmarks, annotations and layers ---------------------------

    // @snippet web-links
    static void WebLinks(PDF pdf, Font font) {
        Page page = new Page(pdf, Letter.PORTRAIT);
        new TextLine(font, "Visit pdfjet.com").SetTextColor(Color.blue).SetUnderline(true)
                .SetURIAction("https://pdfjet.com").SetLocation(50f, 50f).DrawOn(page);
    }

    // @snippet internal-links
    static void InternalLinks(PDF pdf, Font font) {
        Page contents = new Page(pdf, Letter.PORTRAIT);
        new TextLine(font, "Go to chapter 1").SetGoToAction("chapter1")
                .SetLocation(50f, 50f).DrawOn(contents);
        Page chapter = new Page(pdf, Letter.PORTRAIT);
        new TextLine(font, "Chapter 1").SetDestination("chapter1")
                .SetLocation(50f, 50f).DrawOn(chapter);
    }

    // @snippet bookmarks
    static void Bookmarks(PDF pdf, Font font) {
        Bookmark outline = new Bookmark(pdf);
        Page page = new Page(pdf, Letter.PORTRAIT);
        Title title = new Title(font, "Chapter 1", 50f, 50f);
        Bookmark chapter = outline.AddBookmark(page, title);
        title.DrawOn(page);
        title = new Title(font, "Section 1.1", 70f, 80f);
        chapter.AddBookmark(page, title);    // A bookmark under the chapter
        title.DrawOn(page);
    }

    // @snippet attachments
    static void Attachments(PDF pdf, Font font) {
        EmbeddedFile file = new EmbeddedFile(pdf, "data/winter-2009.txt", true);
        Page page = new Page(pdf, Letter.PORTRAIT);
        FileAttachment attachment = new FileAttachment(file);
        attachment.SetLocation(50f, 50f);
        attachment.SetIconPaperclip();
        attachment.SetTitle("winter-2009.txt");
        attachment.SetContents("The data of the table, attached to the page.");
        attachment.DrawOn(page);
    }

    // @snippet notes
    static void Notes(PDF pdf, Font font) {
        Page page = new Page(pdf, Letter.PORTRAIT);
        TextAnnotation note = new TextAnnotation();
        note.SetLocation(50f, 50f);
        note.SetSize(24f, 24f);
        note.SetTitle("Reviewer");
        note.SetContents("Please check the figures on this page.");
        note.DrawOn(page);
    }

    // @snippet layers
    static void Layers(PDF pdf, Font font) {
        OptionalContentGroup grid = new OptionalContentGroup(pdf, "Grid");
        grid.SetVisible(true);
        grid.SetPrintable(false);    // Shown on the screen, not printed
        for (float x = 50f; x <= 250f; x += 50f) {
            grid.Add(new Line(x, 50f, x, 250f).SetStrokeColor(Color.lightgray));
        }
        grid.DrawOn(new Page(pdf, Letter.PORTRAIT));
    }

    // --- Forms --------------------------------------------------------------

    // @snippet check-boxes
    static void CheckBoxes(PDF pdf, Font font) {
        Page page = new Page(pdf, Letter.PORTRAIT);
        new CheckBox(font, "Java").SetLocation(50f, 50f).Check(Mark.CHECK).DrawOn(page);
        new CheckBox(font, "Swift").SetLocation(50f, 75f).DrawOn(page);
        float[] xy = new RadioButton(font, "Yes").SetLocation(50f, 110f).Select(true).DrawOn(page);
        new RadioButton(font, "No").SetLocation(xy[0] + 20f, 110f).DrawOn(page);
    }

    // @snippet form-fields
    static void FormFields(PDF pdf, Font font) {
        Font bold = new Font(pdf, IBMPlexSans.SemiBold);
        List<Field> fields = new List<Field>();
        fields.Add(new Field(0f, "Company", "Smart Widgets Inc."));
        fields.Add(new Field(0f, "City", "Toronto"));    // x 0 starts a new row
        fields.Add(new Field(200f, "Province", "Ontario"));
        new Form(fields).SetLabelFont(font).SetLabelFontSize(8f)
                .SetValueFont(bold).SetValueFontSize(10f)
                .SetLocation(50f, 50f).SetWidth(400f)
                .DrawOn(new Page(pdf, Letter.PORTRAIT));
    }

    // --- Existing PDFs and security -----------------------------------------

    // @snippet merge
    static void Merge() {
        PDF pdf = new PDF(new BufferedStream(new FileStream("merged.pdf", FileMode.Create)));
        String[] files = {"data/testPDFs/PDFjetLogo.pdf", "data/testPDFs/rc65-16e.pdf"};
        foreach (String file in files) {
            Stream stream = new BufferedStream(
                    new FileStream(file, FileMode.Open, FileAccess.Read));
            pdf.Merge(pdf.Read(stream));
            stream.Close();
        }
        pdf.Complete();
    }

    // @snippet split
    static void Split() {
        Stream stream = new BufferedStream(
                new FileStream("data/testPDFs/rc65-16e.pdf", FileMode.Open, FileAccess.Read));
        List<PDFobj> objects = new PDF().Read(stream);
        stream.Close();
        PDF firstPage = new PDF(new BufferedStream(new FileStream("page1.pdf", FileMode.Create)));
        firstPage.Merge(objects, 1);    // The page numbers to keep, from 1
        firstPage.Complete();
    }

    // @snippet existing-pages
    static void ExistingPages() {
        PDF pdf = new PDF(new BufferedStream(new FileStream("stamped.pdf", FileMode.Create)));
        List<PDFobj> objects = pdf.Read(
                new FileStream("data/testPDFs/rc65-16e.pdf", FileMode.Open, FileAccess.Read));
        Font font = new Font(objects,
                new FileStream(IBMPlexSans.Regular, FileMode.Open, FileAccess.Read)).SetSize(24f);
        foreach (PDFobj pageObj in pdf.GetPageObjects(objects)) {
            Page page = new Page(pdf, pageObj);
            page.AddResource(font, objects);
            new TextLine(font, "COPY").SetTextColor(Color.red).SetLocation(50f, 50f).DrawOn(page);
            page.Complete(objects);
        }
        pdf.AddObjects(objects);
        pdf.Complete();
    }

    // @snippet encryption
    static void Encryption() {
        PDF pdf = new PDF(new BufferedStream(new FileStream("encrypted.pdf", FileMode.Create)));
        Passwords passwords = new Passwords();
        passwords.SetUserPassword("hello");     // To open the document
        passwords.SetOwnerPassword("world");    // To change what the user may do
        Permissions permissions = new Permissions();
        permissions.Grant(UserAccess.PRINT | UserAccess.PRINT_HIGH_QUALITY);
        pdf.SetEncryption(new Encryption(pdf, passwords, permissions));
        Font font = new Font(pdf, IBMPlexSans.Regular);
        Page page = new Page(pdf, Letter.PORTRAIT);
        new TextLine(font, "A secret").SetLocation(50f, 50f).DrawOn(page);
        pdf.Complete();
    }

    // --- Running the snippets -----------------------------------------------

    static void Run(String name, Snippet snippet) {
        PDF pdf = new PDF(new BufferedStream(new FileStream(name + ".pdf", FileMode.Create)));
        Font font = new Font(pdf, IBMPlexSans.Regular).SetSize(12f);
        snippet(pdf, font);
        pdf.Complete();
    }

    public static void Main(String[] args) {
        Run("document-info", DocumentInfo);
        Run("page-sizes", PageSizes);
        Run("page-numbers", PageNumbers);
        Run("watermark", Watermark);
        AccessibleDocument();
        ArchivalDocument();
        Run("fonts", Fonts);
        Run("font-files", FontFiles);
        Run("core-fonts", CoreFonts);
        Run("cjk-fonts", CjkFonts);
        Run("fallback-font", FallbackFont);
        Run("font-metrics", FontMetrics);
        Run("text-line", TextLine);
        Run("text-block", TextBlock);
        Run("highlighted-words", HighlightedWords);
        Run("paragraphs", Paragraphs);
        Run("lists", Lists);
        Run("markup", Markup);
        Run("markdown", Markdown);
        Run("text-column", TextColumn);
        Run("text-frame-pages", TextFramePages);
        Run("text-frame-columns", TextFrameColumns);
        Run("right-to-left", RightToLeft);
        Run("formulas", Formulas);
        Run("table", Table);
        Run("table-pages", TablePages);
        Run("cells", Cells);
        Run("cell-content", CellContent);
        Run("table-style", TableStyle);
        Run("table-sums", TableSums);
        Run("big-table", BigTable);
        Run("shapes", Shapes);
        Run("paths", Paths);
        Run("page-graphics", PageGraphics);
        Run("transparency", Transparency);
        Run("containers", Containers);
        Run("stamps", Stamps);
        Run("images", Images);
        Run("svg-images", SvgImages);
        Run("line-chart", LineChart);
        Run("bar-chart", BarChart);
        Run("donut-chart", DonutChart);
        Run("calendar", Calendar);
        Run("barcodes", Barcodes);
        Run("qr-code", QrCode);
        Run("data-matrix", DataMatrix);
        Run("pdf417", Pdf417);
        Run("web-links", WebLinks);
        Run("internal-links", InternalLinks);
        Run("bookmarks", Bookmarks);
        Run("attachments", Attachments);
        Run("notes", Notes);
        Run("layers", Layers);
        Run("check-boxes", CheckBoxes);
        Run("form-fields", FormFields);
        Merge();
        Split();
        ExistingPages();
        Encryption();
    }
}
