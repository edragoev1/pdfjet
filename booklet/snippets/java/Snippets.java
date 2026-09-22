/*
 * Snippets.java
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
import java.io.*;
import java.time.DayOfWeek;
import java.util.*;

import com.pdfjet.*;
import com.pdfjet.barcodes.*;
import com.pdfjet.datamatrix.*;
import com.pdfjet.encryption.*;
import com.pdfjet.fonts.*;
import com.pdfjet.pdf417.*;
import com.pdfjet.qrcode.*;

/**
 * The code of the PDFjet booklet: each method after an "@snippet" comment is
 * one snippet, and the booklet shows its body. A snippet that takes a PDF and
 * a font draws in a PDF of its own, named after it, with IBM Plex Sans at 12
 * points; one that takes nothing writes its own files. Run it in a folder
 * with the fonts, images and data folders of the repository, as
 * booklet/check-snippets.sh does.
 */
public class Snippets {
    interface Snippet {
        void run(PDF pdf, Font font) throws Exception;
    }

    // --- Documents and pages ------------------------------------------------

    // @snippet document-info
    static void documentInfo(PDF pdf, Font font) throws Exception {
        pdf.setTitle("Annual Report 2026");
        pdf.setAuthor("PDFjet Software");
        pdf.setSubject("The year in numbers");
        pdf.setKeywords("report, 2026");
        pdf.setLanguage("en-US");
        Page page = new Page(pdf, Letter.PORTRAIT);
        new TextLine(font, "Annual Report 2026").setLocation(50f, 50f).drawOn(page);
    }

    // @snippet page-sizes
    static void pageSizes(PDF pdf, Font font) throws Exception {
        Page page = new Page(pdf, A4.LANDSCAPE);
        String size = (int) page.getWidth() + " x " + (int) page.getHeight() + " points";
        new TextLine(font, size).setLocation(50f, 50f).drawOn(page);
        page = new Page(pdf, new PageSize(400f, 300f));    // Any width and height
        new TextLine(font, "400 x 300 points").setLocation(50f, 50f).drawOn(page);
    }

    // @snippet page-numbers
    static void pageNumbers(PDF pdf, Font font) throws Exception {
        List<Page> pages = new ArrayList<Page>();
        for (int i = 0; i < 3; i++) {
            Page page = new Page(pdf, Letter.PORTRAIT, Page.DETACHED);
            new TextLine(font, "Chapter " + (i + 1)).setLocation(50f, 50f).drawOn(page);
            pages.add(page);
        }
        for (int i = 0; i < pages.size(); i++) {
            TextLine footer = new TextLine(font, "Page " + (i + 1) + " of " + pages.size());
            pages.get(i).addFooter(footer, 30f);    // The baseline 30 points from the bottom
        }
        pdf.addPages(pages);
    }

    // @snippet watermark
    static void watermark(PDF pdf, Font font) throws Exception {
        Font bold = new Font(pdf, IBMPlexSans.Bold).setSize(120f);
        Page page = new Page(pdf, Letter.PORTRAIT);
        page.addWatermark(bold, "DRAFT");
        new TextLine(font, "The text is drawn over the watermark.").setLocation(50f, 50f).drawOn(page);
    }

    // @snippet accessible-document
    static void accessibleDocument() throws Exception {
        PDF pdf = new PDF(new BufferedOutputStream(new FileOutputStream("accessible.pdf")),
                Compliance.PDF_UA_1);
        pdf.setTitle("An accessible document");
        Font font = new Font(pdf, IBMPlexSans.Regular);
        Page page = new Page(pdf, Letter.PORTRAIT);
        new TextLine(font, "A heading").setFontSize(18f).setStructureType(StructElem.H1)
                .setLocation(50f, 50f).drawOn(page);
        new TextBlock(font, "A paragraph, which a screen reader reads after the heading.")
                .setLocation(50f, 70f).setWidth(400f).drawOn(page);
        Image image = new Image(pdf, "images/linux-logo.png");
        image.setAltDescription("Tux, the penguin of Linux");
        image.setLocation(50f, 110f).drawOn(page);
        pdf.complete();
    }

    // @snippet archival-document
    static void archivalDocument() throws Exception {
        PDF pdf = new PDF(new BufferedOutputStream(new FileOutputStream("archival.pdf")),
                Compliance.PDF_A_2B);
        pdf.setTitle("An archival document");
        Font font = new Font(pdf, IBMPlexSans.Regular);
        Page page = new Page(pdf, Letter.PORTRAIT);
        new TextLine(font, "This document will look the same in fifty years.")
                .setLocation(50f, 50f).drawOn(page);
        pdf.complete();
    }

    // --- Fonts --------------------------------------------------------------

    // @snippet fonts
    static void fonts(PDF pdf, Font font) throws Exception {
        Font serif = new Font(pdf, SourceSerif4.Regular).setSize(14f);
        Font bold = new Font(pdf, IBMPlexSans.Bold);
        Font mono = new Font(pdf, JetBrainsMono.Regular).setSize(10f);
        Page page = new Page(pdf, Letter.PORTRAIT);
        new TextLine(serif, "Source Serif 4 at 14 points").setLocation(50f, 50f).drawOn(page);
        new TextLine(bold, "IBM Plex Sans Bold at 12 points").setLocation(50f, 75f).drawOn(page);
        new TextLine(mono, "JetBrains Mono at 10 points").setLocation(50f, 100f).drawOn(page);
        new TextLine(bold, "The same font at 24 points").setFontSize(24f)
                .setLocation(50f, 135f).drawOn(page);
    }

    // @snippet font-files
    static void fontFiles(PDF pdf, Font font) throws Exception {
        Font otf = new Font(pdf, "fonts/IBMPlexSans/IBMPlexSans-Regular.otf");
        Font ttf = new Font(pdf, "fonts/NotoSans/NotoSans-Regular.ttf");
        Page page = new Page(pdf, Letter.PORTRAIT);
        new TextLine(otf, "An OpenType font").setLocation(50f, 50f).drawOn(page);
        new TextLine(ttf, "A TrueType font").setLocation(50f, 75f).drawOn(page);
    }

    // @snippet core-fonts
    static void coreFonts(PDF pdf, Font font) throws Exception {
        Font helvetica = new Font(pdf, CoreFont.HELVETICA_BOLD).setSize(24f);
        Page page = new Page(pdf, Letter.PORTRAIT);
        new TextLine(helvetica, "WAVE AWAY").setLocation(50f, 50f).drawOn(page);
        helvetica.setKernPairs(true);
        new TextLine(helvetica, "WAVE AWAY").setLocation(50f, 85f).drawOn(page);
    }

    // @snippet cjk-fonts
    static void cjkFonts(PDF pdf, Font font) throws Exception {
        Font chinese = new Font(pdf, CJKFont.ST_HEITI_SC_LIGHT).setSize(24f);
        Font japanese = new Font(pdf, CJKFont.KOZ_MIN_PRO_VI_REGULAR).setSize(24f);
        Font korean = new Font(pdf, CJKFont.ADOBE_MYUNGJO_STD_MEDIUM).setSize(24f);
        Page page = new Page(pdf, Letter.PORTRAIT);
        new TextLine(chinese, "新年快乐!").setLocation(50f, 50f).drawOn(page);
        new TextLine(japanese, "明けましておめでとう!").setLocation(50f, 90f).drawOn(page);
        new TextLine(korean, "새해 복 많이 받으세요!").setLocation(50f, 130f).drawOn(page);
    }

    // @snippet fallback-font
    static void fallbackFont(PDF pdf, Font font) throws Exception {
        Font japanese = new Font(pdf, IBMPlexSansJP.Regular);
        Page page = new Page(pdf, Letter.PORTRAIT);
        new TextLine(font, "Japanese is 日本語 in Japanese.")
                .setFallbackFont(japanese).setLocation(50f, 50f).drawOn(page);
    }

    // @snippet font-metrics
    static void fontMetrics(PDF pdf, Font font) throws Exception {
        Page page = new Page(pdf, Letter.PORTRAIT);
        String text = "Right aligned at x = 500";
        float width = font.stringWidth(text);
        new TextLine(font, text).setLocation(500f - width, 50f).drawOn(page);
        // The next line goes one line height of the font below.
        float y = 50f + font.getBodyHeight();
        new TextLine(font, "The next line").setLocation(500f - font.stringWidth("The next line"), y)
                .drawOn(page);
    }

    // --- Text ---------------------------------------------------------------

    // @snippet text-line
    static void textLine(PDF pdf, Font font) throws Exception {
        Page page = new Page(pdf, Letter.PORTRAIT);
        TextLine text = new TextLine(font, "Hello, World!");
        text.setFontSize(20f);
        text.setTextColor(Color.navy);
        text.setUnderline(true);
        text.setLocation(50f, 50f);
        float[] xy = text.drawOn(page);
        // drawOn returns where the text ends: the next line starts after it.
        new TextLine(font, "Turned by 15 degrees").setTextRotation(15)
                .setLocation(xy[0] + 20f, 50f).drawOn(page);
    }

    // @snippet text-block
    static void textBlock(PDF pdf, Font font) throws Exception {
        Page page = new Page(pdf, Letter.PORTRAIT);
        TextBlock block = new TextBlock(font,
                "A text block wraps its text at its width. Its lines break between words, "
                + "and an empty line starts a new paragraph.\n\nLike this one.");
        block.setLocation(50f, 50f);
        block.setWidth(250f);
        block.setLineSpacing(1.4f);
        block.setPadding(10f);
        block.setBorderColor(Color.blue);
        float[] xy = block.drawOn(page);
        new TextLine(font, "Below the block").setLocation(50f, xy[1] + 20f).drawOn(page);
    }

    // @snippet highlighted-words
    static void highlightedWords(PDF pdf, Font font) throws Exception {
        Map<String, Integer> colors = new HashMap<String, Integer>();
        colors.put("PDFjet", Color.blue);
        colors.put("fast", Color.red);
        Page page = new Page(pdf, Letter.PORTRAIT);
        new TextBlock(font, "PDFjet is fast, and PDFjet is small.")
                .setHighlightColors(colors).setLocation(50f, 50f).setWidth(400f).drawOn(page);
    }

    // @snippet paragraphs
    static void paragraphs(PDF pdf, Font font) throws Exception {
        Font bold = new Font(pdf, IBMPlexSans.Bold);
        Font italic = new Font(pdf, IBMPlexSans.Italic);
        List<Paragraph> paragraphs = new ArrayList<Paragraph>();
        paragraphs.add(new Paragraph()
                .add(new TextLine(font, "A paragraph mixes"))
                .add(new TextLine(bold, "fonts,"))
                .add(new TextLine(italic, "styles").setTextColor(Color.darkred))
                .add(new TextLine(font, "and colors, and wraps them as one text.")));
        paragraphs.add(new Paragraph(new TextLine(font, "Centered.")).setTextAlignment(Alignment.CENTER));
        TextFrame frame = new TextFrame(paragraphs);
        frame.setLocation(50f, 50f);
        frame.setWidth(250f);
        frame.drawOn(new Page(pdf, Letter.PORTRAIT));
    }

    // @snippet lists
    static void lists(PDF pdf, Font font) throws Exception {
        String[] items = {"Create the PDF", "Draw on its pages", "Complete it"};
        List<Paragraph> paragraphs = new ArrayList<Paragraph>();
        for (int i = 0; i < items.length; i++) {
            paragraphs.add(new Paragraph(new TextLine(font, items[i]))
                    .setListLabel(new TextLine(font, (i + 1) + "."), 20f));
        }
        new TextFrame(paragraphs).setLocation(50f, 50f).setWidth(300f)
                .drawOn(new Page(pdf, Letter.PORTRAIT));
    }

    // @snippet markup
    static void markup(PDF pdf, Font font) throws Exception {
        Font bold = new Font(pdf, IBMPlexSans.Bold);
        Font italic = new Font(pdf, IBMPlexSans.Italic);
        Font boldItalic = new Font(pdf, IBMPlexSans.BoldItalic);
        Font code = new Font(pdf, JetBrainsMono.Regular).setSize(11f);
        Markup markup = new Markup(font, bold, italic, boldItalic, code);
        List<Paragraph> paragraphs = markup.paragraphs(
                "Write **bold**, *italic* and `code`, and link to [PDFjet](https://pdfjet.com).\n\n"
                + "An empty line starts a new paragraph.");
        new TextFrame(paragraphs).setLocation(50f, 50f).setWidth(300f)
                .drawOn(new Page(pdf, Letter.PORTRAIT));
    }

    // @snippet markdown
    static void markdown(PDF pdf, Font font) throws Exception {
        Font bold = new Font(pdf, IBMPlexSans.Bold);
        Font italic = new Font(pdf, IBMPlexSans.Italic);
        Font boldItalic = new Font(pdf, IBMPlexSans.BoldItalic);
        Font code = new Font(pdf, JetBrainsMono.Regular).setSize(10f);
        Markdown markdown = new Markdown(font, bold, italic, boldItalic, code);
        List<Page> pages = new ArrayList<Page>();
        markdown.drawOn(pdf, "# A heading\n"
                + "\n"
                + "A paragraph with **bold** text and a [link](https://pdfjet.com).\n"
                + "\n"
                + "- an item\n"
                + "- another item\n", pages, Letter.PORTRAIT);
        pdf.addPages(pages);
    }

    // @snippet text-column
    static void textColumn(PDF pdf, Font font) throws Exception {
        TextColumn column = new TextColumn();
        column.addParagraph(new Paragraph(new TextLine(font,
                "A text column draws its paragraphs one under the other.")));
        column.addParagraph(new Paragraph(new TextLine(font,
                "It justifies them, and puts space between them.")));
        column.setLocation(50f, 50f);
        column.setWidth(200f);
        column.setTextAlignment(Alignment.JUSTIFY);
        column.setParagraphSpacing(1.5f);
        column.drawOn(new Page(pdf, Letter.PORTRAIT));
    }

    // @snippet text-frame-pages
    static void textFramePages(PDF pdf, Font font) throws Exception {
        List<String> text = Arrays.asList(Content.ofTextFile("data/dostoevsky.txt").split("\n\n"));
        TextFrame frame = new TextFrame(font, text);
        frame.setLocation(72f, 72f);
        frame.setWidth(468f);
        List<Page> pages = new ArrayList<Page>();
        frame.drawOn(pdf, pages, Letter.PORTRAIT);  // As many pages as the text needs
        pdf.addPages(pages);
    }

    // @snippet text-frame-columns
    static void textFrameColumns(PDF pdf, Font font) throws Exception {
        List<String> text = Arrays.asList(Content.ofTextFile("data/dostoevsky.txt").split("\n\n"));
        TextFrame frame = new TextFrame(font, text);
        while (frame.hasMoreText()) {
            Page page = new Page(pdf, Letter.LANDSCAPE);
            for (float x = 50f; x < 700f && frame.hasMoreText(); x += 250f) {
                frame.setLocation(x, 50f).setWidth(230f).setHeight(500f);
                frame.drawOn(page);    // Draws what fits and keeps the rest
            }
        }
    }

    // @snippet right-to-left
    static void rightToLeft(PDF pdf, Font font) throws Exception {
        Font hebrew = new Font(pdf, IBMPlexSansHebrew.Regular);
        Font arabic = new Font(pdf, IBMPlexSansArabic.Regular);
        Page page = new Page(pdf, Letter.PORTRAIT);
        new TextBlock(hebrew, "שלום עולם! זהו טקסט בעברית.")
                .setRightToLeft(true).setLanguage("he")
                .setLocation(50f, 50f).setWidth(300f).drawOn(page);
        String text = Bidi.reorderVisually("مرحبا بالعالم!");
        new TextLine(arabic, text).setLanguage("ar")
                .setLocation(350f - arabic.stringWidth(text), 100f).drawOn(page);
    }

    // @snippet formulas
    static void formulas(PDF pdf, Font font) throws Exception {
        Page page = new Page(pdf, Letter.PORTRAIT);
        new CompositeTextLine(50f, 50f).setFontSize(16f)
                .addFormula(font, "C6H12O6").drawOn(page);
        new CompositeTextLine(50f, 80f).setFontSize(16f)
                .addFormula(font, "SO4^2-").drawOn(page);
    }

    // --- Tables -------------------------------------------------------------

    // @snippet table
    static void table(PDF pdf, Font font) throws Exception {
        Font bold = new Font(pdf, IBMPlexSans.SemiBold);
        String[][] data = {
            {"Planet", "Moons", "Day, hours"},
            {"Earth", "1", "24"},
            {"Mars", "2", "24.6"},
            {"Jupiter", "95", "9.9"},
        };
        List<List<Cell>> rows = new ArrayList<List<Cell>>();
        for (int i = 0; i < data.length; i++) {
            List<Cell> row = new ArrayList<Cell>();
            for (String text : data[i]) {
                row.add(new Cell(i == 0 ? bold : font, text));
            }
            rows.add(row);
        }
        Table table = new Table();
        table.setTableData(rows, 1);    // One header row
        table.setLocation(50f, 50f);
        table.autoAdjustColumnWidths();
        table.rightAlignNumbers();
        table.drawOn(new Page(pdf, Letter.PORTRAIT));
    }

    // @snippet table-pages
    static void tablePages(PDF pdf, Font font) throws Exception {
        Font bold = new Font(pdf, IBMPlexSans.SemiBold);
        List<List<Cell>> rows = new ArrayList<List<Cell>>();
        rows.add(Arrays.asList(new Cell(bold, "Number"), new Cell(bold, "Square")));
        for (int i = 1; i <= 200; i++) {
            rows.add(Arrays.asList(new Cell(font, String.valueOf(i)),
                    new Cell(font, String.valueOf(i * i))));
        }
        Table table = new Table();
        table.setTableData(rows, 1);    // The header row is drawn on every page
        table.setLocation(50f, 50f);
        table.setBottomMargin(50f);
        table.setColumnWidth(0, 100f).setColumnWidth(1, 100f);
        List<Page> pages = new ArrayList<Page>();
        table.drawOn(pdf, pages, Letter.PORTRAIT);
        pdf.addPages(pages);
    }

    // @snippet cells
    static void cells(PDF pdf, Font font) throws Exception {
        Cell title = new Cell(font, "Spans two columns");
        title.setColSpan(2);
        title.setTextAlignment(Alignment.CENTER);
        title.setBackgroundColor(Color.lightblue);
        Cell left = new Cell(font, "Left").setTextColor(Color.darkred);
        Cell right = new Cell(font, "Right").setTextAlignment(Alignment.RIGHT);
        right.setBorder(Border.LEFT, false);
        List<List<Cell>> rows = new ArrayList<List<Cell>>();
        rows.add(Arrays.asList(title, new Cell(font)));    // The cell the span covers
        rows.add(Arrays.asList(left, right));
        Table table = new Table();
        table.setTableData(rows);
        table.setColumnWidth(0, 120f).setColumnWidth(1, 120f);
        table.setLocation(50f, 50f).drawOn(new Page(pdf, Letter.PORTRAIT));
    }

    // @snippet cell-content
    static void cellContent(PDF pdf, Font font) throws Exception {
        Image image = new Image(pdf, "images/linux-logo.png").scaleBy(0.25f);
        image.setAltDescription("Tux");
        Cell imageCell = new Cell(font).setImage(image);
        Cell barcodeCell = new Cell(font).setBarcode(new Barcode(Barcode.CODE_128, "PDFjet"));
        Cell textCell = new Cell(font).setTextBlock(
                new TextBlock(font, "A text block wraps in its cell, which grows to fit it."));
        List<List<Cell>> rows = new ArrayList<List<Cell>>();
        rows.add(Arrays.asList(imageCell, barcodeCell, textCell));
        Table table = new Table();
        table.setTableData(rows);
        table.setColumnWidth(0, 80f).setColumnWidth(1, 150f).setColumnWidth(2, 150f);
        table.setLocation(50f, 50f).drawOn(new Page(pdf, Letter.PORTRAIT));
    }

    // @snippet table-style
    static void tableStyle(PDF pdf, Font font) throws Exception {
        Font bold = new Font(pdf, IBMPlexSans.SemiBold);
        List<List<Cell>> rows = new ArrayList<List<Cell>>();
        rows.add(Arrays.asList(new Cell(font, "Item"), new Cell(font, "Price")));
        for (int i = 1; i <= 6; i++) {
            rows.add(Arrays.asList(new Cell(font, "Item " + i), new Cell(font, i + ".99")));
        }
        Table table = new Table();
        table.setTableData(rows, 1);
        table.setHeaderRowStyle(bold, Color.white, Color.navy);
        table.setAlternateRowColor(Color.aliceblue);
        table.setColumnWidthsInPercent(70f, 30f).setWidth(300f);
        table.rightAlignNumbers();
        table.setLocation(50f, 50f).drawOn(new Page(pdf, Letter.PORTRAIT));
    }

    // @snippet table-sums
    static void tableSums(PDF pdf, Font font) throws Exception {
        Font bold = new Font(pdf, IBMPlexSans.SemiBold);
        List<List<Cell>> rows = new ArrayList<List<Cell>>();
        rows.add(Arrays.asList(new Cell(bold, "Day"), new Cell(bold, "Sales")));
        for (int i = 1; i <= 100; i++) {
            rows.add(Arrays.asList(new Cell(font, "Day " + i), new Cell(font, String.valueOf(10 * i))));
        }
        rows.add(Arrays.asList(new Cell(bold, "Page total"), new Cell(bold)));
        rows.add(Arrays.asList(new Cell(bold, "Carried forward"), new Cell(bold)));
        Table table = new Table();
        table.setTableData(rows, 1);
        table.setNumberOfFooterRows(2);    // The last two rows end every page
        table.setPageSum(rows.size() - 2, 1, 0);       // Row, column, decimals
        table.setRunningSum(rows.size() - 1, 1, 0);
        table.rightAlignNumbers();
        table.setColumnWidth(0, 150f).setColumnWidth(1, 100f);
        table.setLocation(50f, 50f).setBottomMargin(50f);
        List<Page> pages = new ArrayList<Page>();
        table.drawOn(pdf, pages, Letter.PORTRAIT);
        pdf.addPages(pages);
    }

    // @snippet big-table
    static void bigTable(PDF pdf, Font font) throws Exception {
        Font bold = new Font(pdf, IBMPlexSans.SemiBold).setSize(10f);
        Font body = new Font(pdf, IBMPlexSans.Regular).setSize(9f);
        BigTable table = new BigTable(pdf, bold, body, Letter.LANDSCAPE);
        table.setNumberOfColumns(9);    // Set in this order
        table.setTableData("data/Electric_Vehicle_Population_10_Pages.csv", ",");
        table.setLocation(0f, 0f);
        table.setBottomMargin(20f);
        table.complete();
    }

    // --- Graphics -----------------------------------------------------------

    // @snippet shapes
    static void shapes(PDF pdf, Font font) throws Exception {
        Page page = new Page(pdf, Letter.PORTRAIT);
        new Line(50f, 50f, 250f, 50f).setStrokeWidth(2f).setStrokeColor(Color.blue).drawOn(page);
        new Rect(50f, 70f, 100f, 60f).setFillColor(Color.lightblue).setCornerRadius(8f)
                .drawOn(page);
        new Point(210f, 100f).setShape(Shape.STAR).setRadius(25f).setFillColor(Color.gold)
                .drawOn(page);
        new Ellipse().setLocation(320f, 100f).setRadiusX(50f).setRadiusY(30f)
                .setFillColor(Color.lightgreen).drawOn(page);
        new Arc().setLocation(450f, 100f).setRadiusX(30f).setRadiusY(30f)
                .setStartAngle(0f).setSweep(270f).setStrokeWidth(4f).drawOn(page);
    }

    // @snippet paths
    static void paths(PDF pdf, Font font) throws Exception {
        Path triangle = new Path();
        triangle.add(new Point(100f, 50f));
        triangle.add(new Point(150f, 130f));
        triangle.add(new Point(50f, 130f));
        triangle.setClosed(true);
        triangle.setFillShape(true);
        triangle.setStrokeColor(Color.darkorange);
        Path curve = new Path();
        curve.add(new Point(200f, 130f));
        curve.add(new Point(250f, 20f, Point.CONTROL_POINT_C));   // Bezier control points
        curve.add(new Point(300f, 200f, Point.CONTROL_POINT_C));
        curve.add(new Point(350f, 90f));
        curve.setStrokeWidth(3f);
        Page page = new Page(pdf, Letter.PORTRAIT);
        triangle.drawOn(page);
        curve.drawOn(page);
    }

    // @snippet page-graphics
    static void pageGraphics(PDF pdf, Font font) throws Exception {
        Page page = new Page(pdf, Letter.PORTRAIT);
        page.setPenColor(Color.navy);
        page.setPenWidth(2f);
        page.setStrokeDashPattern("[6 3] 0");
        page.moveTo(50f, 50f);
        page.lineTo(250f, 50f);
        page.lineTo(250f, 150f);
        page.strokePath();
        page.setBrushColor(Color.coral);
        page.fillRect(50f, 80f, 150f, 70f);
    }

    // @snippet transparency
    static void transparency(PDF pdf, Font font) throws Exception {
        Page page = new Page(pdf, Letter.PORTRAIT);
        page.setBrushColor(Color.blue);
        page.fillRect(50f, 50f, 100f, 100f);
        GraphicsState gs = new GraphicsState();
        gs.setAlphaNonStroking(0.5f);    // Fills are 50% transparent
        page.saveGraphicsState();
        page.setGraphicsState(gs);
        page.setBrushColor(Color.red);
        page.fillRect(100f, 100f, 100f, 100f);
        page.restoreGraphicsState();
    }

    // @snippet containers
    static void containers(PDF pdf, Font font) throws Exception {
        Container container = new Container(200f, 100f);
        container.add(new Rect(0f, 0f, 200f, 100f).setFillColor(Color.lightyellow));
        container.add(new TextLine(font, "Turned together").setLocation(20f, 55f));
        container.setLocation(150f, 100f);
        container.setRotation(-30);
        container.drawOn(new Page(pdf, Letter.PORTRAIT));
    }

    // @snippet stamps
    static void stamps(PDF pdf, Font font) throws Exception {
        Stamp stamp = new Stamp(pdf).setSize(120f, 40f).addFont(font);
        stamp.setStrokeColor(Color.red).setStrokeWidth(3f).drawRect(0f, 0f, 120f, 40f);
        stamp.setFillColor(Color.red).drawText(new TextParameters()
                .setFont(font).setFontSize(20f).setLocation(18f, 28f).setText("PAID"));
        stamp.complete();    // Written once, drawn any number of times
        Page page = new Page(pdf, Letter.PORTRAIT);
        stamp.setLocation(50f, 50f).drawOn(page);
        stamp.setRotation(-15).setLocation(250f, 60f).drawOn(page);
    }

    // --- Images -------------------------------------------------------------

    // @snippet images
    static void images(PDF pdf, Font font) throws Exception {
        Image photo = new Image(pdf, "images/gr-map.jpg");    // JPEG, PNG or BMP
        photo.setAltDescription("A map of Greece");
        Page page = new Page(pdf, Letter.PORTRAIT);
        photo.setLocation(50f, 50f);
        photo.resizeWidth(250f);
        float[] xy = photo.drawOn(page);
        // The image is written to the PDF once, however many times it is drawn.
        photo.setLocation(50f, xy[1] + 10f).resizeWidth(100f);
        photo.drawOn(page);
    }

    // @snippet svg-images
    static void svgImages(PDF pdf, Font font) throws Exception {
        SVGImage icon = new SVGImage("images/svg/star_FILL0_wght400_GRAD0_opsz48.svg");
        icon.setAltDescription("A star");
        icon.setLocation(50f, 50f);
        icon.scaleBy(2f);    // Vector graphics stay sharp at any size
        icon.drawOn(new Page(pdf, Letter.PORTRAIT));
    }

    // --- Charts -------------------------------------------------------------

    // @snippet line-chart
    static void lineChart(PDF pdf, Font font) throws Exception {
        Font small = new Font(pdf, IBMPlexSans.Regular).setSize(8f);
        Chart chart = new Chart(font, small);
        chart.setLocation(50f, 50f);
        chart.setSize(400f, 250f);
        chart.setTitle("Visitors");
        chart.setXAxisTitle("Month");
        chart.setYAxisTitle("Thousands");
        Series series = chart.addSeries("2026").setDrawPath(true).setStrokeColor(Color.blue);
        float[] visitors = {3f, 5f, 4f, 7f, 9f, 8f};
        for (int month = 1; month <= visitors.length; month++) {
            series.addPoint(month, visitors[month - 1]);
        }
        chart.drawOn(new Page(pdf, Letter.PORTRAIT));
    }

    // @snippet bar-chart
    static void barChart(PDF pdf, Font font) throws Exception {
        Font small = new Font(pdf, IBMPlexSans.Regular).setSize(8f);
        BarChart chart = new BarChart(font, small);
        chart.setLocation(50f, 50f);
        chart.setSize(400f, 250f);
        chart.setTitle("Units sold");
        chart.setCategories(new String[] {"Q1", "Q2", "Q3", "Q4"});
        chart.addSeries("2025", new float[] {45f, 65f, 31f, 52f}, Color.seagreen);
        chart.addSeries("2026", new float[] {75f, 20f, 73f, 80f}, Color.indianred);
        chart.drawOn(new Page(pdf, Letter.PORTRAIT));
    }

    // @snippet donut-chart
    static void donutChart(PDF pdf, Font font) throws Exception {
        Font bold = new Font(pdf, IBMPlexSans.Bold).setSize(10f);
        DonutChart chart = new DonutChart(font, bold);
        chart.setLocation(100f, 100f);
        chart.setRadii(100f, 60f);    // An inner radius of 0 makes a pie chart
        chart.addSlice(new Slice(50f, Color.steelblue, "Java"));
        chart.addSlice(new Slice(30f, Color.seagreen, "C#"));
        chart.addSlice(new Slice(20f, Color.darkorange, "Go"));
        chart.drawOn(new Page(pdf, Letter.PORTRAIT));
    }

    // @snippet calendar
    static void calendar(PDF pdf, Font font) throws Exception {
        Font bold = new Font(pdf, IBMPlexSans.Bold).setSize(10f);
        Font small = new Font(pdf, IBMPlexSans.Regular).setSize(9f);
        CalendarMonth month = new CalendarMonth(bold, small, 2026, 12);
        month.setFirstDayOfWeek(DayOfWeek.MONDAY);
        month.setLocation(50f, 50f);
        month.drawOn(new Page(pdf, Letter.PORTRAIT));
    }

    // --- Barcodes -----------------------------------------------------------

    // @snippet barcodes
    static void barcodes(PDF pdf, Font font) throws Exception {
        Page page = new Page(pdf, Letter.PORTRAIT);
        Barcode code128 = new Barcode(Barcode.CODE_128, "PDFjet 9.0");
        code128.setFont(font);    // The text under the bars
        code128.setLocation(50f, 50f);
        float[] xy = code128.drawOn(page);
        Barcode ean13 = new Barcode(Barcode.EAN_13, "051234567890");    // The check digit is added
        ean13.setFont(font);
        ean13.setModuleLength(1f);
        ean13.setLocation(50f, xy[1] + 30f);
        ean13.drawOn(page);
    }

    // @snippet qr-code
    static void qrCode(PDF pdf, Font font) throws Exception {
        QRCode qr = new QRCode("https://pdfjet.com", ErrorCorrectionLevel.M);
        qr.setModuleLength(4f);
        qr.setLocation(50f, 50f);
        qr.drawOn(new Page(pdf, Letter.PORTRAIT));
    }

    // @snippet data-matrix
    static void dataMatrix(PDF pdf, Font font) throws Exception {
        DataMatrix code = new DataMatrix("Grüße aus München!");
        code.setModuleLength(4f);
        code.setLocation(50f, 50f);
        code.drawOn(new Page(pdf, Letter.PORTRAIT));
    }

    // @snippet pdf417
    static void pdf417(PDF pdf, Font font) throws Exception {
        PDF417 code = new PDF417("PDF417 holds text and binary data, and survives damage.");
        code.setModuleLength(1f);
        code.setLocation(50f, 50f);
        code.drawOn(new Page(pdf, Letter.PORTRAIT));
    }

    // --- Links, bookmarks, annotations and layers ---------------------------

    // @snippet web-links
    static void webLinks(PDF pdf, Font font) throws Exception {
        Page page = new Page(pdf, Letter.PORTRAIT);
        new TextLine(font, "Visit pdfjet.com").setTextColor(Color.blue).setUnderline(true)
                .setURIAction("https://pdfjet.com").setLocation(50f, 50f).drawOn(page);
    }

    // @snippet internal-links
    static void internalLinks(PDF pdf, Font font) throws Exception {
        Page contents = new Page(pdf, Letter.PORTRAIT);
        new TextLine(font, "Go to chapter 1").setGoToAction("chapter1")
                .setLocation(50f, 50f).drawOn(contents);
        Page chapter = new Page(pdf, Letter.PORTRAIT);
        new TextLine(font, "Chapter 1").setDestination("chapter1")
                .setLocation(50f, 50f).drawOn(chapter);
    }

    // @snippet bookmarks
    static void bookmarks(PDF pdf, Font font) throws Exception {
        Bookmark outline = new Bookmark(pdf);
        Page page = new Page(pdf, Letter.PORTRAIT);
        Title title = new Title(font, "Chapter 1", 50f, 50f);
        Bookmark chapter = outline.addBookmark(page, title);
        title.drawOn(page);
        title = new Title(font, "Section 1.1", 70f, 80f);
        chapter.addBookmark(page, title);    // A bookmark under the chapter
        title.drawOn(page);
    }

    // @snippet attachments
    static void attachments(PDF pdf, Font font) throws Exception {
        EmbeddedFile file = new EmbeddedFile(pdf, "data/winter-2009.txt", true);
        Page page = new Page(pdf, Letter.PORTRAIT);
        FileAttachment attachment = new FileAttachment(file);
        attachment.setLocation(50f, 50f);
        attachment.setIconPaperclip();
        attachment.setTitle("winter-2009.txt");
        attachment.setContents("The data of the table, attached to the page.");
        attachment.drawOn(page);
    }

    // @snippet notes
    static void notes(PDF pdf, Font font) throws Exception {
        Page page = new Page(pdf, Letter.PORTRAIT);
        TextAnnotation note = new TextAnnotation();
        note.setLocation(50f, 50f);
        note.setSize(24f, 24f);
        note.setTitle("Reviewer");
        note.setContents("Please check the figures on this page.");
        note.drawOn(page);
    }

    // @snippet layers
    static void layers(PDF pdf, Font font) throws Exception {
        OptionalContentGroup grid = new OptionalContentGroup(pdf, "Grid");
        grid.setVisible(true);
        grid.setPrintable(false);    // Shown on the screen, not printed
        for (float x = 50f; x <= 250f; x += 50f) {
            grid.add(new Line(x, 50f, x, 250f).setStrokeColor(Color.lightgray));
        }
        grid.drawOn(new Page(pdf, Letter.PORTRAIT));
    }

    // --- Forms --------------------------------------------------------------

    // @snippet check-boxes
    static void checkBoxes(PDF pdf, Font font) throws Exception {
        Page page = new Page(pdf, Letter.PORTRAIT);
        new CheckBox(font, "Java").setLocation(50f, 50f).check(Mark.CHECK).drawOn(page);
        new CheckBox(font, "Swift").setLocation(50f, 75f).drawOn(page);
        float[] xy = new RadioButton(font, "Yes").setLocation(50f, 110f).select(true).drawOn(page);
        new RadioButton(font, "No").setLocation(xy[0] + 20f, 110f).drawOn(page);
    }

    // @snippet form-fields
    static void formFields(PDF pdf, Font font) throws Exception {
        Font bold = new Font(pdf, IBMPlexSans.SemiBold);
        List<Field> fields = new ArrayList<Field>();
        fields.add(new Field(0f, "Company", "Smart Widgets Inc."));
        fields.add(new Field(0f, "City", "Toronto"));    // x 0 starts a new row
        fields.add(new Field(200f, "Province", "Ontario"));
        new Form(fields).setLabelFont(font).setLabelFontSize(8f)
                .setValueFont(bold).setValueFontSize(10f)
                .setLocation(50f, 50f).setWidth(400f)
                .drawOn(new Page(pdf, Letter.PORTRAIT));
    }

    // --- Existing PDFs and security -----------------------------------------

    // @snippet merge
    static void merge() throws Exception {
        PDF pdf = new PDF(new BufferedOutputStream(new FileOutputStream("merged.pdf")));
        for (String file : new String[] {"data/testPDFs/PDFjetLogo.pdf", "data/testPDFs/rc65-16e.pdf"}) {
            InputStream stream = new BufferedInputStream(new FileInputStream(file));
            pdf.merge(pdf.read(stream));
            stream.close();
        }
        pdf.complete();
    }

    // @snippet split
    static void split() throws Exception {
        InputStream stream = new BufferedInputStream(new FileInputStream("data/testPDFs/rc65-16e.pdf"));
        List<PDFobj> objects = new PDF().read(stream);
        stream.close();
        PDF firstPage = new PDF(new BufferedOutputStream(new FileOutputStream("page1.pdf")));
        firstPage.merge(objects, 1);    // The page numbers to keep, from 1
        firstPage.complete();
    }

    // @snippet existing-pages
    static void existingPages() throws Exception {
        PDF pdf = new PDF(new BufferedOutputStream(new FileOutputStream("stamped.pdf")));
        List<PDFobj> objects = pdf.read(new FileInputStream("data/testPDFs/rc65-16e.pdf"));
        Font font = new Font(objects, new FileInputStream(IBMPlexSans.Regular)).setSize(24f);
        for (PDFobj pageObj : pdf.getPageObjects(objects)) {
            Page page = new Page(pdf, pageObj);
            page.addResource(font, objects);
            new TextLine(font, "COPY").setTextColor(Color.red).setLocation(50f, 50f).drawOn(page);
            page.complete(objects);
        }
        pdf.addObjects(objects);
        pdf.complete();
    }

    // @snippet encryption
    static void encryption() throws Exception {
        PDF pdf = new PDF(new BufferedOutputStream(new FileOutputStream("encrypted.pdf")));
        Passwords passwords = new Passwords();
        passwords.setUserPassword("hello");     // To open the document
        passwords.setOwnerPassword("world");    // To change what the user may do
        Permissions permissions = new Permissions();
        permissions.grant(UserAccess.PRINT, UserAccess.PRINT_HIGH_QUALITY);
        pdf.setEncryption(new Encryption(pdf, passwords, permissions));
        Font font = new Font(pdf, IBMPlexSans.Regular);
        Page page = new Page(pdf, Letter.PORTRAIT);
        new TextLine(font, "A secret").setLocation(50f, 50f).drawOn(page);
        pdf.complete();
    }

    // --- Running the snippets -----------------------------------------------

    static void run(String name, Snippet snippet) throws Exception {
        PDF pdf = new PDF(new BufferedOutputStream(new FileOutputStream(name + ".pdf")));
        Font font = new Font(pdf, IBMPlexSans.Regular).setSize(12f);
        snippet.run(pdf, font);
        pdf.complete();
    }

    public static void main(String[] args) throws Exception {
        run("document-info", Snippets::documentInfo);
        run("page-sizes", Snippets::pageSizes);
        run("page-numbers", Snippets::pageNumbers);
        run("watermark", Snippets::watermark);
        accessibleDocument();
        archivalDocument();
        run("fonts", Snippets::fonts);
        run("font-files", Snippets::fontFiles);
        run("core-fonts", Snippets::coreFonts);
        run("cjk-fonts", Snippets::cjkFonts);
        run("fallback-font", Snippets::fallbackFont);
        run("font-metrics", Snippets::fontMetrics);
        run("text-line", Snippets::textLine);
        run("text-block", Snippets::textBlock);
        run("highlighted-words", Snippets::highlightedWords);
        run("paragraphs", Snippets::paragraphs);
        run("lists", Snippets::lists);
        run("markup", Snippets::markup);
        run("markdown", Snippets::markdown);
        run("text-column", Snippets::textColumn);
        run("text-frame-pages", Snippets::textFramePages);
        run("text-frame-columns", Snippets::textFrameColumns);
        run("right-to-left", Snippets::rightToLeft);
        run("formulas", Snippets::formulas);
        run("table", Snippets::table);
        run("table-pages", Snippets::tablePages);
        run("cells", Snippets::cells);
        run("cell-content", Snippets::cellContent);
        run("table-style", Snippets::tableStyle);
        run("table-sums", Snippets::tableSums);
        run("big-table", Snippets::bigTable);
        run("shapes", Snippets::shapes);
        run("paths", Snippets::paths);
        run("page-graphics", Snippets::pageGraphics);
        run("transparency", Snippets::transparency);
        run("containers", Snippets::containers);
        run("stamps", Snippets::stamps);
        run("images", Snippets::images);
        run("svg-images", Snippets::svgImages);
        run("line-chart", Snippets::lineChart);
        run("bar-chart", Snippets::barChart);
        run("donut-chart", Snippets::donutChart);
        run("calendar", Snippets::calendar);
        run("barcodes", Snippets::barcodes);
        run("qr-code", Snippets::qrCode);
        run("data-matrix", Snippets::dataMatrix);
        run("pdf417", Snippets::pdf417);
        run("web-links", Snippets::webLinks);
        run("internal-links", Snippets::internalLinks);
        run("bookmarks", Snippets::bookmarks);
        run("attachments", Snippets::attachments);
        run("notes", Snippets::notes);
        run("layers", Snippets::layers);
        run("check-boxes", Snippets::checkBoxes);
        run("form-fields", Snippets::formFields);
        merge();
        split();
        existingPages();
        encryption();
    }
}
