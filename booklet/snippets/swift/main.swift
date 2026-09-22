/**
 * main.swift
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
import Foundation
import PDFjet

// The code of the PDFjet booklet: each function after an "@snippet" comment is
// one snippet, and the booklet shows its body. A snippet that takes a PDF and
// a font draws in a PDF of its own, named after it, with IBM Plex Sans at 12
// points; one that takes nothing writes its own files. Run it in a folder
// with the fonts, images and data folders of the repository, as
// booklet/check-snippets.sh does.

// --- Documents and pages ------------------------------------------------

// @snippet document-info
func documentInfo(_ pdf: PDF, _ font: Font) throws {
    pdf.setTitle("Annual Report 2026")
    pdf.setAuthor("PDFjet Software")
    pdf.setSubject("The year in numbers")
    pdf.setKeywords("report, 2026")
    pdf.setLanguage("en-US")
    let page = Page(pdf, Letter.PORTRAIT)
    TextLine(font, "Annual Report 2026").setLocation(50, 50).drawOn(page)
}

// @snippet page-sizes
func pageSizes(_ pdf: PDF, _ font: Font) throws {
    var page = Page(pdf, A4.LANDSCAPE)
    let size = "\(Int(page.getWidth())) x \(Int(page.getHeight())) points"
    TextLine(font, size).setLocation(50, 50).drawOn(page)
    page = Page(pdf, PageSize(400, 300))    // Any width and height
    TextLine(font, "400 x 300 points").setLocation(50, 50).drawOn(page)
}

// @snippet page-numbers
func pageNumbers(_ pdf: PDF, _ font: Font) throws {
    var pages = [Page]()
    for i in 0..<3 {
        let page = Page(pdf, Letter.PORTRAIT, Page.DETACHED)
        TextLine(font, "Chapter \(i + 1)").setLocation(50, 50).drawOn(page)
        pages.append(page)
    }
    for i in 0..<pages.count {
        let footer = TextLine(font, "Page \(i + 1) of \(pages.count)")
        pages[i].addFooter(footer, 30)    // The baseline 30 points from the bottom
    }
    pdf.addPages(pages)
}

// @snippet watermark
func watermark(_ pdf: PDF, _ font: Font) throws {
    let bold = try Font(pdf, IBMPlexSans.Bold).setSize(120)
    let page = Page(pdf, Letter.PORTRAIT)
    page.addWatermark(bold, "DRAFT")
    TextLine(font, "The text is drawn over the watermark.").setLocation(50, 50).drawOn(page)
}

// @snippet accessible-document
func accessibleDocument() throws {
    let pdf = PDF(OutputStream(toFileAtPath: "accessible.pdf", append: false)!,
            Compliance.PDF_UA_1)
    pdf.setTitle("An accessible document")
    let font = try Font(pdf, IBMPlexSans.Regular)
    let page = Page(pdf, Letter.PORTRAIT)
    TextLine(font, "A heading").setFontSize(18).setStructureType(StructElem.H1)
            .setLocation(50, 50).drawOn(page)
    TextBlock(font, "A paragraph, which a screen reader reads after the heading.")
            .setLocation(50, 70).setWidth(400).drawOn(page)
    let image = try Image(pdf, "images/linux-logo.png")
    image.setAltDescription("Tux, the penguin of Linux")
    image.setLocation(50, 110).drawOn(page)
    try pdf.complete()
}

// @snippet archival-document
func archivalDocument() throws {
    let pdf = PDF(OutputStream(toFileAtPath: "archival.pdf", append: false)!,
            Compliance.PDF_A_2B)
    pdf.setTitle("An archival document")
    let font = try Font(pdf, IBMPlexSans.Regular)
    let page = Page(pdf, Letter.PORTRAIT)
    TextLine(font, "This document will look the same in fifty years.")
            .setLocation(50, 50).drawOn(page)
    try pdf.complete()
}

// --- Fonts --------------------------------------------------------------

// @snippet fonts
func fonts(_ pdf: PDF, _ font: Font) throws {
    let serif = try Font(pdf, SourceSerif4.Regular).setSize(14)
    let bold = try Font(pdf, IBMPlexSans.Bold)
    let mono = try Font(pdf, JetBrainsMono.Regular).setSize(10)
    let page = Page(pdf, Letter.PORTRAIT)
    TextLine(serif, "Source Serif 4 at 14 points").setLocation(50, 50).drawOn(page)
    TextLine(bold, "IBM Plex Sans Bold at 12 points").setLocation(50, 75).drawOn(page)
    TextLine(mono, "JetBrains Mono at 10 points").setLocation(50, 100).drawOn(page)
    TextLine(bold, "The same font at 24 points").setFontSize(24)
            .setLocation(50, 135).drawOn(page)
}

// @snippet font-files
func fontFiles(_ pdf: PDF, _ font: Font) throws {
    let otf = try Font(pdf, "fonts/IBMPlexSans/IBMPlexSans-Regular.otf")
    let ttf = try Font(pdf, "fonts/NotoSans/NotoSans-Regular.ttf")
    let page = Page(pdf, Letter.PORTRAIT)
    TextLine(otf, "An OpenType font").setLocation(50, 50).drawOn(page)
    TextLine(ttf, "A TrueType font").setLocation(50, 75).drawOn(page)
}

// @snippet core-fonts
func coreFonts(_ pdf: PDF, _ font: Font) throws {
    let helvetica = try Font(pdf, CoreFont.HELVETICA_BOLD).setSize(24)
    let page = Page(pdf, Letter.PORTRAIT)
    TextLine(helvetica, "WAVE AWAY").setLocation(50, 50).drawOn(page)
    helvetica.setKernPairs(true)
    TextLine(helvetica, "WAVE AWAY").setLocation(50, 85).drawOn(page)
}

// @snippet cjk-fonts
func cjkFonts(_ pdf: PDF, _ font: Font) throws {
    let chinese = Font(pdf, CJKFont.ST_HEITI_SC_LIGHT).setSize(24)
    let japanese = Font(pdf, CJKFont.KOZ_MIN_PRO_VI_REGULAR).setSize(24)
    let korean = Font(pdf, CJKFont.ADOBE_MYUNGJO_STD_MEDIUM).setSize(24)
    let page = Page(pdf, Letter.PORTRAIT)
    TextLine(chinese, "新年快乐!").setLocation(50, 50).drawOn(page)
    TextLine(japanese, "明けましておめでとう!").setLocation(50, 90).drawOn(page)
    TextLine(korean, "새해 복 많이 받으세요!").setLocation(50, 130).drawOn(page)
}

// @snippet fallback-font
func fallbackFont(_ pdf: PDF, _ font: Font) throws {
    let japanese = try Font(pdf, IBMPlexSansJP.Regular)
    let page = Page(pdf, Letter.PORTRAIT)
    TextLine(font, "Japanese is 日本語 in Japanese.")
            .setFallbackFont(japanese).setLocation(50, 50).drawOn(page)
}

// @snippet font-metrics
func fontMetrics(_ pdf: PDF, _ font: Font) throws {
    let page = Page(pdf, Letter.PORTRAIT)
    let text = "Right aligned at x = 500"
    let width = font.stringWidth(text)
    TextLine(font, text).setLocation(500 - width, 50).drawOn(page)
    // The next line goes one line height of the font below.
    let y = 50 + font.getBodyHeight()
    TextLine(font, "The next line").setLocation(500 - font.stringWidth("The next line"), y)
            .drawOn(page)
}

// --- Text ---------------------------------------------------------------

// @snippet text-line
func textLine(_ pdf: PDF, _ font: Font) throws {
    let page = Page(pdf, Letter.PORTRAIT)
    let text = TextLine(font, "Hello, World!")
    text.setFontSize(20)
    text.setTextColor(Color.navy)
    text.setUnderline(true)
    text.setLocation(50, 50)
    let xy = text.drawOn(page)
    // drawOn returns where the text ends: the next line starts after it.
    TextLine(font, "Turned by 15 degrees").setTextRotation(15)
            .setLocation(xy[0] + 20, 50).drawOn(page)
}

// @snippet text-block
func textBlock(_ pdf: PDF, _ font: Font) throws {
    let page = Page(pdf, Letter.PORTRAIT)
    let block = TextBlock(font,
            "A text block wraps its text at its width. Its lines break between words, "
            + "and an empty line starts a new paragraph.\n\nLike this one.")
    block.setLocation(50, 50)
    block.setWidth(250)
    block.setLineSpacing(1.4)
    block.setPadding(10)
    block.setBorderColor(Color.blue)
    let xy = block.drawOn(page)
    TextLine(font, "Below the block").setLocation(50, xy[1] + 20).drawOn(page)
}

// @snippet highlighted-words
func highlightedWords(_ pdf: PDF, _ font: Font) throws {
    var colors = [String: Int32]()
    colors["PDFjet"] = Color.blue
    colors["fast"] = Color.red
    let page = Page(pdf, Letter.PORTRAIT)
    TextBlock(font, "PDFjet is fast, and PDFjet is small.")
            .setHighlightColors(colors).setLocation(50, 50).setWidth(400).drawOn(page)
}

// @snippet paragraphs
func paragraphs(_ pdf: PDF, _ font: Font) throws {
    let bold = try Font(pdf, IBMPlexSans.Bold)
    let italic = try Font(pdf, IBMPlexSans.Italic)
    var paragraphs = [Paragraph]()
    paragraphs.append(Paragraph()
            .add(TextLine(font, "A paragraph mixes"))
            .add(TextLine(bold, "fonts,"))
            .add(TextLine(italic, "styles").setTextColor(Color.darkred))
            .add(TextLine(font, "and colors, and wraps them as one text.")))
    paragraphs.append(Paragraph(TextLine(font, "Centered.")).setTextAlignment(Alignment.CENTER))
    let frame = TextFrame(paragraphs)
    frame.setLocation(50, 50)
    frame.setWidth(250)
    frame.drawOn(Page(pdf, Letter.PORTRAIT))
}

// @snippet lists
func lists(_ pdf: PDF, _ font: Font) throws {
    let items = ["Create the PDF", "Draw on its pages", "Complete it"]
    var paragraphs = [Paragraph]()
    for i in 0..<items.count {
        paragraphs.append(Paragraph(TextLine(font, items[i]))
                .setListLabel(TextLine(font, "\(i + 1)."), 20))
    }
    TextFrame(paragraphs).setLocation(50, 50).setWidth(300)
            .drawOn(Page(pdf, Letter.PORTRAIT))
}

// @snippet markup
func markup(_ pdf: PDF, _ font: Font) throws {
    let bold = try Font(pdf, IBMPlexSans.Bold)
    let italic = try Font(pdf, IBMPlexSans.Italic)
    let boldItalic = try Font(pdf, IBMPlexSans.BoldItalic)
    let code = try Font(pdf, JetBrainsMono.Regular).setSize(11)
    let markup = Markup(font, bold, italic, boldItalic, code)
    let paragraphs = markup.paragraphs(
            "Write **bold**, *italic* and `code`, and link to [PDFjet](https://pdfjet.com).\n\n"
            + "An empty line starts a new paragraph.")
    TextFrame(paragraphs).setLocation(50, 50).setWidth(300)
            .drawOn(Page(pdf, Letter.PORTRAIT))
}

// @snippet markdown
func markdown(_ pdf: PDF, _ font: Font) throws {
    let bold = try Font(pdf, IBMPlexSans.Bold)
    let italic = try Font(pdf, IBMPlexSans.Italic)
    let boldItalic = try Font(pdf, IBMPlexSans.BoldItalic)
    let code = try Font(pdf, JetBrainsMono.Regular).setSize(10)
    let markdown = Markdown(font, bold, italic, boldItalic, code)
    var pages = [Page]()
    try markdown.drawOn(pdf, "# A heading\n"
            + "\n"
            + "A paragraph with **bold** text and a [link](https://pdfjet.com).\n"
            + "\n"
            + "- an item\n"
            + "- another item\n", &pages, Letter.PORTRAIT)
    pdf.addPages(pages)
}

// @snippet text-column
func textColumn(_ pdf: PDF, _ font: Font) throws {
    let column = TextColumn()
    column.addParagraph(Paragraph(TextLine(font,
            "A text column draws its paragraphs one under the other.")))
    column.addParagraph(Paragraph(TextLine(font,
            "It justifies them, and puts space between them.")))
    column.setLocation(50, 50)
    column.setWidth(200)
    column.setTextAlignment(Alignment.JUSTIFY)
    column.setParagraphSpacing(1.5)
    column.drawOn(Page(pdf, Letter.PORTRAIT))
}

// @snippet text-frame-pages
func textFramePages(_ pdf: PDF, _ font: Font) throws {
    let text = try Content.ofTextFile("data/dostoevsky.txt").components(separatedBy: "\n\n")
    let frame = TextFrame(font, text)
    frame.setLocation(72, 72)
    frame.setWidth(468)
    var pages = [Page]()
    frame.drawOn(pdf, &pages, Letter.PORTRAIT)  // As many pages as the text needs
    pdf.addPages(pages)
}

// @snippet text-frame-columns
func textFrameColumns(_ pdf: PDF, _ font: Font) throws {
    let text = try Content.ofTextFile("data/dostoevsky.txt").components(separatedBy: "\n\n")
    let frame = TextFrame(font, text)
    while frame.hasMoreText() {
        let page = Page(pdf, Letter.LANDSCAPE)
        var x: Float = 50
        while x < 700 && frame.hasMoreText() {
            frame.setLocation(x, 50).setWidth(230).setHeight(500)
            frame.drawOn(page)    // Draws what fits and keeps the rest
            x += 250
        }
    }
}

// @snippet right-to-left
func rightToLeft(_ pdf: PDF, _ font: Font) throws {
    let hebrew = try Font(pdf, IBMPlexSansHebrew.Regular)
    let arabic = try Font(pdf, IBMPlexSansArabic.Regular)
    let page = Page(pdf, Letter.PORTRAIT)
    TextBlock(hebrew, "שלום עולם! זהו טקסט בעברית.")
            .setRightToLeft(true).setLanguage("he")
            .setLocation(50, 50).setWidth(300).drawOn(page)
    let text = Bidi.reorderVisually("مرحبا بالعالم!")
    TextLine(arabic, text).setLanguage("ar")
            .setLocation(350 - arabic.stringWidth(text), 100).drawOn(page)
}

// @snippet formulas
func formulas(_ pdf: PDF, _ font: Font) throws {
    let page = Page(pdf, Letter.PORTRAIT)
    CompositeTextLine(50, 50).setFontSize(16)
            .addFormula(font, "C6H12O6").drawOn(page)
    CompositeTextLine(50, 80).setFontSize(16)
            .addFormula(font, "SO4^2-").drawOn(page)
}

// --- Tables -------------------------------------------------------------

// @snippet table
func table(_ pdf: PDF, _ font: Font) throws {
    let bold = try Font(pdf, IBMPlexSans.SemiBold)
    let data = [
        ["Planet", "Moons", "Day, hours"],
        ["Earth", "1", "24"],
        ["Mars", "2", "24.6"],
        ["Jupiter", "95", "9.9"],
    ]
    var rows = [[Cell]]()
    for i in 0..<data.count {
        var row = [Cell]()
        for text in data[i] {
            row.append(Cell(i == 0 ? bold : font, text))
        }
        rows.append(row)
    }
    let table = Table()
    table.setTableData(rows, 1)    // One header row
    table.setLocation(50, 50)
    table.autoAdjustColumnWidths()
    table.rightAlignNumbers()
    table.drawOn(Page(pdf, Letter.PORTRAIT))
}

// @snippet table-pages
func tablePages(_ pdf: PDF, _ font: Font) throws {
    let bold = try Font(pdf, IBMPlexSans.SemiBold)
    var rows = [[Cell]]()
    rows.append([Cell(bold, "Number"), Cell(bold, "Square")])
    for i in 1...200 {
        rows.append([Cell(font, String(i)), Cell(font, String(i * i))])
    }
    let table = Table()
    table.setTableData(rows, 1)    // The header row is drawn on every page
    table.setLocation(50, 50)
    table.setBottomMargin(50)
    table.setColumnWidth(0, 100).setColumnWidth(1, 100)
    var pages = [Page]()
    table.drawOn(pdf, &pages, Letter.PORTRAIT)
    pdf.addPages(pages)
}

// @snippet cells
func cells(_ pdf: PDF, _ font: Font) throws {
    let title = Cell(font, "Spans two columns")
    title.setColSpan(2)
    title.setTextAlignment(Alignment.CENTER)
    title.setBackgroundColor(Color.lightblue)
    let left = Cell(font, "Left").setTextColor(Color.darkred)
    let right = Cell(font, "Right").setTextAlignment(Alignment.RIGHT)
    right.setBorder(Border.LEFT, false)
    var rows = [[Cell]]()
    rows.append([title, Cell(font)])    // The cell the span covers
    rows.append([left, right])
    let table = Table()
    table.setTableData(rows)
    table.setColumnWidth(0, 120).setColumnWidth(1, 120)
    table.setLocation(50, 50).drawOn(Page(pdf, Letter.PORTRAIT))
}

// @snippet cell-content
func cellContent(_ pdf: PDF, _ font: Font) throws {
    let image = try Image(pdf, "images/linux-logo.png").scaleBy(0.25)
    image.setAltDescription("Tux")
    let imageCell = Cell(font).setImage(image)
    let barcodeCell = try Cell(font).setBarcode(Barcode(Barcode.CODE_128, "PDFjet"))
    let textCell = Cell(font).setTextBlock(
            TextBlock(font, "A text block wraps in its cell, which grows to fit it."))
    var rows = [[Cell]]()
    rows.append([imageCell, barcodeCell, textCell])
    let table = Table()
    table.setTableData(rows)
    table.setColumnWidth(0, 80).setColumnWidth(1, 150).setColumnWidth(2, 150)
    table.setLocation(50, 50).drawOn(Page(pdf, Letter.PORTRAIT))
}

// @snippet table-style
func tableStyle(_ pdf: PDF, _ font: Font) throws {
    let bold = try Font(pdf, IBMPlexSans.SemiBold)
    var rows = [[Cell]]()
    rows.append([Cell(font, "Item"), Cell(font, "Price")])
    for i in 1...6 {
        rows.append([Cell(font, "Item \(i)"), Cell(font, "\(i).99")])
    }
    let table = Table()
    table.setTableData(rows, 1)
    table.setHeaderRowStyle(bold, Color.white, Color.navy)
    table.setAlternateRowColor(Color.aliceblue)
    table.setColumnWidthsInPercent(70, 30).setWidth(300)
    table.rightAlignNumbers()
    table.setLocation(50, 50).drawOn(Page(pdf, Letter.PORTRAIT))
}

// @snippet table-sums
func tableSums(_ pdf: PDF, _ font: Font) throws {
    let bold = try Font(pdf, IBMPlexSans.SemiBold)
    var rows = [[Cell]]()
    rows.append([Cell(bold, "Day"), Cell(bold, "Sales")])
    for i in 1...100 {
        rows.append([Cell(font, "Day \(i)"), Cell(font, String(10 * i))])
    }
    rows.append([Cell(bold, "Page total"), Cell(bold)])
    rows.append([Cell(bold, "Carried forward"), Cell(bold)])
    let table = Table()
    table.setTableData(rows, 1)
    table.setNumberOfFooterRows(2)    // The last two rows end every page
    table.setPageSum(rows.count - 2, 1, 0)       // Row, column, decimals
    table.setRunningSum(rows.count - 1, 1, 0)
    table.rightAlignNumbers()
    table.setColumnWidth(0, 150).setColumnWidth(1, 100)
    table.setLocation(50, 50).setBottomMargin(50)
    var pages = [Page]()
    table.drawOn(pdf, &pages, Letter.PORTRAIT)
    pdf.addPages(pages)
}

// @snippet big-table
func bigTable(_ pdf: PDF, _ font: Font) throws {
    let bold = try Font(pdf, IBMPlexSans.SemiBold).setSize(10)
    let body = try Font(pdf, IBMPlexSans.Regular).setSize(9)
    let table = BigTable(pdf, bold, body, Letter.LANDSCAPE)
    table.setNumberOfColumns(9)    // Set in this order
    try table.setTableData("data/Electric_Vehicle_Population_10_Pages.csv", ",")
    table.setLocation(0, 0)
    table.setBottomMargin(20)
    try table.complete()
}

// --- Graphics -----------------------------------------------------------

// @snippet shapes
func shapes(_ pdf: PDF, _ font: Font) throws {
    let page = Page(pdf, Letter.PORTRAIT)
    Line(50, 50, 250, 50).setStrokeWidth(2).setStrokeColor(Color.blue).drawOn(page)
    Rect(50, 70, 100, 60).setFillColor(Color.lightblue).setCornerRadius(8)
            .drawOn(page)
    Point(210, 100).setShape(Shape.STAR).setRadius(25).setFillColor(Color.gold)
            .drawOn(page)
    Ellipse().setLocation(320, 100).setRadiusX(50).setRadiusY(30)
            .setFillColor(Color.lightgreen).drawOn(page)
    Arc().setLocation(450, 100).setRadiusX(30).setRadiusY(30)
            .setStartAngle(0).setSweep(270).setStrokeWidth(4).drawOn(page)
}

// @snippet paths
func paths(_ pdf: PDF, _ font: Font) throws {
    let triangle = Path()
    triangle.add(Point(100, 50))
    triangle.add(Point(150, 130))
    triangle.add(Point(50, 130))
    triangle.setClosed(true)
    triangle.setFillShape(true)
    triangle.setStrokeColor(Color.darkorange)
    let curve = Path()
    curve.add(Point(200, 130))
    curve.add(Point(250, 20, Point.CONTROL_POINT_C))   // Bezier control points
    curve.add(Point(300, 200, Point.CONTROL_POINT_C))
    curve.add(Point(350, 90))
    curve.setStrokeWidth(3)
    let page = Page(pdf, Letter.PORTRAIT)
    triangle.drawOn(page)
    curve.drawOn(page)
}

// @snippet page-graphics
func pageGraphics(_ pdf: PDF, _ font: Font) throws {
    let page = Page(pdf, Letter.PORTRAIT)
    page.setPenColor(Color.navy)
    page.setPenWidth(2)
    page.setStrokeDashPattern("[6 3] 0")
    page.moveTo(50, 50)
    page.lineTo(250, 50)
    page.lineTo(250, 150)
    page.strokePath()
    page.setBrushColor(Color.coral)
    page.fillRect(50, 80, 150, 70)
}

// @snippet transparency
func transparency(_ pdf: PDF, _ font: Font) throws {
    let page = Page(pdf, Letter.PORTRAIT)
    page.setBrushColor(Color.blue)
    page.fillRect(50, 50, 100, 100)
    let gs = GraphicsState()
    gs.setAlphaNonStroking(0.5)    // Fills are 50% transparent
    page.saveGraphicsState()
    page.setGraphicsState(gs)
    page.setBrushColor(Color.red)
    page.fillRect(100, 100, 100, 100)
    page.restoreGraphicsState()
}

// @snippet containers
func containers(_ pdf: PDF, _ font: Font) throws {
    let container = Container(200, 100)
    container.add(Rect(0, 0, 200, 100).setFillColor(Color.lightyellow))
    container.add(TextLine(font, "Turned together").setLocation(20, 55))
    container.setLocation(150, 100)
    container.setRotation(-30)
    _ = container.drawOn(Page(pdf, Letter.PORTRAIT))
}

// @snippet stamps
func stamps(_ pdf: PDF, _ font: Font) throws {
    let stamp = Stamp(pdf).setSize(120, 40).addFont(font)
    stamp.setStrokeColor(Color.red).setStrokeWidth(3).drawRect(0, 0, 120, 40)
    stamp.setFillColor(Color.red).drawText(TextParameters()
            .setFont(font).setFontSize(20).setLocation(18, 28).setText("PAID"))
    try stamp.complete()    // Written once, drawn any number of times
    let page = Page(pdf, Letter.PORTRAIT)
    stamp.setLocation(50, 50).drawOn(page)
    stamp.setRotation(-15).setLocation(250, 60).drawOn(page)
}

// --- Images -------------------------------------------------------------

// @snippet images
func images(_ pdf: PDF, _ font: Font) throws {
    let photo = try Image(pdf, "images/gr-map.jpg")    // JPEG, PNG or BMP
    photo.setAltDescription("A map of Greece")
    let page = Page(pdf, Letter.PORTRAIT)
    photo.setLocation(50, 50)
    photo.resizeWidth(250)
    let xy = photo.drawOn(page)
    // The image is written to the PDF once, however many times it is drawn.
    photo.setLocation(50, xy[1] + 10).resizeWidth(100)
    photo.drawOn(page)
}

// @snippet svg-images
func svgImages(_ pdf: PDF, _ font: Font) throws {
    let icon = try SVGImage(fileAtPath: "images/svg/star_FILL0_wght400_GRAD0_opsz48.svg")!
    icon.setAltDescription("A star")
    icon.setLocation(50, 50)
    icon.scaleBy(2)    // Vector graphics stay sharp at any size
    icon.drawOn(Page(pdf, Letter.PORTRAIT))
}

// --- Charts -------------------------------------------------------------

// @snippet line-chart
func lineChart(_ pdf: PDF, _ font: Font) throws {
    let small = try Font(pdf, IBMPlexSans.Regular).setSize(8)
    let chart = Chart(font, small)
    chart.setLocation(50, 50)
    chart.setSize(400, 250)
    chart.setTitle("Visitors")
    chart.setXAxisTitle("Month")
    chart.setYAxisTitle("Thousands")
    let series = chart.addSeries("2026").setDrawPath(true).setStrokeColor(Color.blue)
    let visitors: [Float] = [3, 5, 4, 7, 9, 8]
    for month in 1...visitors.count {
        series.addPoint(Float(month), visitors[month - 1])
    }
    chart.drawOn(Page(pdf, Letter.PORTRAIT))
}

// @snippet bar-chart
func barChart(_ pdf: PDF, _ font: Font) throws {
    let small = try Font(pdf, IBMPlexSans.Regular).setSize(8)
    let chart = BarChart(font, small)
    chart.setLocation(50, 50)
    chart.setSize(400, 250)
    chart.setTitle("Units sold")
    chart.setCategories(["Q1", "Q2", "Q3", "Q4"])
    chart.addSeries("2025", [45, 65, 31, 52], Color.seagreen)
    chart.addSeries("2026", [75, 20, 73, 80], Color.indianred)
    chart.drawOn(Page(pdf, Letter.PORTRAIT))
}

// @snippet donut-chart
func donutChart(_ pdf: PDF, _ font: Font) throws {
    let bold = try Font(pdf, IBMPlexSans.Bold).setSize(10)
    let chart = DonutChart(font, bold)
    chart.setLocation(100, 100)
    chart.setRadii(100, 60)    // An inner radius of 0 makes a pie chart
    chart.addSlice(Slice(50, Color.steelblue, "Java"))
    chart.addSlice(Slice(30, Color.seagreen, "C#"))
    chart.addSlice(Slice(20, Color.darkorange, "Go"))
    chart.drawOn(Page(pdf, Letter.PORTRAIT))
}

// @snippet calendar
func calendar(_ pdf: PDF, _ font: Font) throws {
    let bold = try Font(pdf, IBMPlexSans.Bold).setSize(10)
    let small = try Font(pdf, IBMPlexSans.Regular).setSize(9)
    let month = CalendarMonth(bold, small, 2026, 12)
    month.setFirstDayOfWeek(2)    // Monday
    month.setLocation(50, 50)
    month.drawOn(Page(pdf, Letter.PORTRAIT))
}

// --- Barcodes -----------------------------------------------------------

// @snippet barcodes
func barcodes(_ pdf: PDF, _ font: Font) throws {
    let page = Page(pdf, Letter.PORTRAIT)
    let code128 = try Barcode(Barcode.CODE_128, "PDFjet 9.0")
    code128.setFont(font)    // The text under the bars
    code128.setLocation(50, 50)
    let xy = code128.drawOn(page)
    let ean13 = try Barcode(Barcode.EAN_13, "051234567890")    // The check digit is added
    ean13.setFont(font)
    ean13.setModuleLength(1.0)
    ean13.setLocation(50, xy[1] + 30)
    ean13.drawOn(page)
}

// @snippet qr-code
func qrCode(_ pdf: PDF, _ font: Font) throws {
    let qr = try QRCode("https://pdfjet.com", ErrorCorrectionLevel.M)
    qr.setModuleLength(4)
    qr.setLocation(50, 50)
    qr.drawOn(Page(pdf, Letter.PORTRAIT))
}

// @snippet data-matrix
func dataMatrix(_ pdf: PDF, _ font: Font) throws {
    let code = DataMatrix("Grüße aus München!")
    code.setModuleLength(4)
    code.setLocation(50, 50)
    code.drawOn(Page(pdf, Letter.PORTRAIT))
}

// @snippet pdf417
func pdf417(_ pdf: PDF, _ font: Font) throws {
    let code = try PDF417("PDF417 holds text and binary data, and survives damage.")
    code.setModuleLength(1)
    code.setLocation(50, 50)
    code.drawOn(Page(pdf, Letter.PORTRAIT))
}

// --- Links, bookmarks, annotations and layers ---------------------------

// @snippet web-links
func webLinks(_ pdf: PDF, _ font: Font) throws {
    let page = Page(pdf, Letter.PORTRAIT)
    TextLine(font, "Visit pdfjet.com").setTextColor(Color.blue).setUnderline(true)
            .setURIAction("https://pdfjet.com").setLocation(50, 50).drawOn(page)
}

// @snippet internal-links
func internalLinks(_ pdf: PDF, _ font: Font) throws {
    let contents = Page(pdf, Letter.PORTRAIT)
    TextLine(font, "Go to chapter 1").setGoToAction("chapter1")
            .setLocation(50, 50).drawOn(contents)
    let chapter = Page(pdf, Letter.PORTRAIT)
    TextLine(font, "Chapter 1").setDestination("chapter1")
            .setLocation(50, 50).drawOn(chapter)
}

// @snippet bookmarks
func bookmarks(_ pdf: PDF, _ font: Font) throws {
    let outline = Bookmark(pdf)
    let page = Page(pdf, Letter.PORTRAIT)
    var title = Title(font, "Chapter 1", 50, 50)
    let chapter = outline.addBookmark(page, title)
    title.drawOn(page)
    title = Title(font, "Section 1.1", 70, 80)
    chapter.addBookmark(page, title)    // A bookmark under the chapter
    title.drawOn(page)
}

// @snippet attachments
func attachments(_ pdf: PDF, _ font: Font) throws {
    let file = try EmbeddedFile(pdf, "data/winter-2009.txt", true)
    let page = Page(pdf, Letter.PORTRAIT)
    let attachment = FileAttachment(file)
    attachment.setLocation(50, 50)
    attachment.setIconPaperclip()
    attachment.setTitle("winter-2009.txt")
    attachment.setContents("The data of the table, attached to the page.")
    attachment.drawOn(page)
}

// @snippet notes
func notes(_ pdf: PDF, _ font: Font) throws {
    let page = Page(pdf, Letter.PORTRAIT)
    let note = TextAnnotation()
    note.setLocation(50, 50)
    note.setSize(24, 24)
    note.setTitle("Reviewer")
    note.setContents("Please check the figures on this page.")
    _ = note.drawOn(page)
}

// @snippet layers
func layers(_ pdf: PDF, _ font: Font) throws {
    let grid = OptionalContentGroup(pdf, "Grid")
    grid.setVisible(true)
    grid.setPrintable(false)    // Shown on the screen, not printed
    for x in stride(from: Float(50), through: 250, by: 50) {
        grid.add(Line(x, 50, x, 250).setStrokeColor(Color.lightgray))
    }
    grid.drawOn(Page(pdf, Letter.PORTRAIT))
}

// --- Forms --------------------------------------------------------------

// @snippet check-boxes
func checkBoxes(_ pdf: PDF, _ font: Font) throws {
    let page = Page(pdf, Letter.PORTRAIT)
    CheckBox(font, "Java").setLocation(50, 50).check(Mark.CHECK).drawOn(page)
    CheckBox(font, "Swift").setLocation(50, 75).drawOn(page)
    let xy = RadioButton(font, "Yes").setLocation(50, 110).select(true).drawOn(page)
    RadioButton(font, "No").setLocation(xy[0] + 20, 110).drawOn(page)
}

// @snippet form-fields
func formFields(_ pdf: PDF, _ font: Font) throws {
    let bold = try Font(pdf, IBMPlexSans.SemiBold)
    var fields = [Field]()
    fields.append(Field(0, "Company", "Smart Widgets Inc."))
    fields.append(Field(0, "City", "Toronto"))    // x 0 starts a new row
    fields.append(Field(200, "Province", "Ontario"))
    Form(fields).setLabelFont(font).setLabelFontSize(8)
            .setValueFont(bold).setValueFontSize(10)
            .setLocation(50, 50).setWidth(400)
            .drawOn(Page(pdf, Letter.PORTRAIT))
}

// --- Existing PDFs and security -----------------------------------------

// @snippet merge
func merge() throws {
    let pdf = PDF(OutputStream(toFileAtPath: "merged.pdf", append: false)!)
    for file in ["data/testPDFs/PDFjetLogo.pdf", "data/testPDFs/rc65-16e.pdf"] {
        let stream = InputStream(fileAtPath: file)!
        try pdf.merge(pdf.read(from: stream))
        stream.close()
    }
    try pdf.complete()
}

// @snippet split
func split() throws {
    let stream = InputStream(fileAtPath: "data/testPDFs/rc65-16e.pdf")!
    let objects = try PDF().read(from: stream)
    stream.close()
    let firstPage = PDF(OutputStream(toFileAtPath: "page1.pdf", append: false)!)
    try firstPage.merge(objects, [1])    // The page numbers to keep, from 1
    try firstPage.complete()
}

// @snippet existing-pages
func existingPages() throws {
    let pdf = PDF(OutputStream(toFileAtPath: "stamped.pdf", append: false)!)
    var objects = try pdf.read(from: InputStream(fileAtPath: "data/testPDFs/rc65-16e.pdf")!)
    let font = try Font(&objects, InputStream(fileAtPath: IBMPlexSans.Regular)!).setSize(24)
    for pageObj in pdf.getPageObjects(from: objects) {
        let page = Page(pdf, pageObj)
        page.addResource(font, &objects)
        TextLine(font, "COPY").setTextColor(Color.red).setLocation(50, 50).drawOn(page)
        page.complete(&objects)
    }
    try pdf.addObjects(objects)
    try pdf.complete()
}

// @snippet encryption
func encryption() throws {
    let pdf = PDF(OutputStream(toFileAtPath: "encrypted.pdf", append: false)!)
    let passwords = Passwords()
    passwords.setUserPassword("hello")     // To open the document
    passwords.setOwnerPassword("world")    // To change what the user may do
    let permissions = Permissions()
    permissions.grant(UserAccess.PRINT | UserAccess.PRINT_HIGH_QUALITY)
    pdf.setEncryption(Encryption(pdf, passwords, permissions))
    let font = try Font(pdf, IBMPlexSans.Regular)
    let page = Page(pdf, Letter.PORTRAIT)
    TextLine(font, "A secret").setLocation(50, 50).drawOn(page)
    try pdf.complete()
}

// --- Running the snippets -----------------------------------------------

func run(_ name: String, _ snippet: (PDF, Font) throws -> Void) throws {
    let pdf = PDF(OutputStream(toFileAtPath: name + ".pdf", append: false)!)
    let font = try Font(pdf, IBMPlexSans.Regular).setSize(12)
    try snippet(pdf, font)
    try pdf.complete()
}

try run("document-info", documentInfo)
try run("page-sizes", pageSizes)
try run("page-numbers", pageNumbers)
try run("watermark", watermark)
try accessibleDocument()
try archivalDocument()
try run("fonts", fonts)
try run("font-files", fontFiles)
try run("core-fonts", coreFonts)
try run("cjk-fonts", cjkFonts)
try run("fallback-font", fallbackFont)
try run("font-metrics", fontMetrics)
try run("text-line", textLine)
try run("text-block", textBlock)
try run("highlighted-words", highlightedWords)
try run("paragraphs", paragraphs)
try run("lists", lists)
try run("markup", markup)
try run("markdown", markdown)
try run("text-column", textColumn)
try run("text-frame-pages", textFramePages)
try run("text-frame-columns", textFrameColumns)
try run("right-to-left", rightToLeft)
try run("formulas", formulas)
try run("table", table)
try run("table-pages", tablePages)
try run("cells", cells)
try run("cell-content", cellContent)
try run("table-style", tableStyle)
try run("table-sums", tableSums)
try run("big-table", bigTable)
try run("shapes", shapes)
try run("paths", paths)
try run("page-graphics", pageGraphics)
try run("transparency", transparency)
try run("containers", containers)
try run("stamps", stamps)
try run("images", images)
try run("svg-images", svgImages)
try run("line-chart", lineChart)
try run("bar-chart", barChart)
try run("donut-chart", donutChart)
try run("calendar", calendar)
try run("barcodes", barcodes)
try run("qr-code", qrCode)
try run("data-matrix", dataMatrix)
try run("pdf417", pdf417)
try run("web-links", webLinks)
try run("internal-links", internalLinks)
try run("bookmarks", bookmarks)
try run("attachments", attachments)
try run("notes", notes)
try run("layers", layers)
try run("check-boxes", checkBoxes)
try run("form-fields", formFields)
try merge()
try split()
try existingPages()
try encryption()
