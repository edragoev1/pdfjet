/**
 * main.swift
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
import Foundation
import PDFjet

/**
 * Example_79.swift
 * A product catalog of two pages, on Letter paper: the logo and the title, a
 * grid of products with a picture, a name, a description, the code and the
 * price of each, the colors the products come in, the price list, how to
 * order, and a QR code of the shop online.
 *
 * A catalog is a finished document that customers receive, not a form. The
 * products are one list, and both the grid and the price list are made from
 * it, so a price is changed in one place; the products without a picture are
 * in the price list only. The grid places three products to a row, each in a
 * column of the same width, and starts the next row under the tallest
 * product of the row, as a longer description pushes it down. The codes and
 * the prices are in Cascadia Mono, a monospace font, so that the characters
 * of the codes line up from row to row of the price list.
 *
 * The pictures here are small drawings, SVG files in
 * resources/images/catalog that SVGImage draws as vectors, sharp at any
 * size, scaled to the width of a column. A real catalog has photos: an
 * Image of a JPEG in place of SVGImage, with the same setAltDescription.
 * The logo is an SVG, as logos usually are.
 *
 * The document is PDF/UA-1: the title, the sections and the products are
 * headings of three levels, every picture and the QR code is a figure that
 * says what it shows, the price list is a table, and the running header of
 * each page and the color swatches are artifacts. The layout is that of the
 * product catalog template of PDFjet Forms. The studio and its products are
 * made up.
 */
struct ExampleError: Error, CustomStringConvertible {
    let message: String
    var description: String { message }
}

public class Example_79 {
    // A product: its code, name, description, the price in dollars, the
    // number of colors it comes in, and a picture, nil for none.
    struct Product {
        let code: String
        let name: String
        let description: String
        let price: Decimal
        let colors: Int
        let picture: String?
        let alt: String?

        init(_ code: String, _ name: String, _ description: String, _ price: String, _ colors: Int,
                _ picture: String?, _ alt: String?) {
            self.code = code
            self.name = name
            self.description = description
            self.price = Decimal(string: price)!
            self.colors = colors
            self.picture = picture
            self.alt = alt
        }
    }

    static let STUDIO = "Northwind Studio"
    static let TITLE = "Catalog 2026"
    static let ORDER_URL = "https://example.com/order"

    // The pictures and the logo are SVG files, drawn as vectors, sharp at any size
    static let IMAGES = "resources/images/catalog/"

    static let PRODUCTS = [
        Product("NB-A5-L", "Lined notebook, A5", "160 pages of lined paper, 100 g/m², "
                + "a navy linen cover.", "24.00", 4, "notebook-lined.svg", "A navy notebook with a cream label"),
        Product("NB-A5-D", "Dot grid notebook, A5", "160 pages with a 5 mm dot grid, "
                + "for notes and sketches.", "24.00", 4, "notebook-dots.svg", "An open notebook of dotted pages"),
        Product("MG-350-B", "Stoneware mug", "350 ml, thrown by hand; dishwasher safe.",
                "32.00", 4, "mug.svg", "A brown stoneware mug"),
        Product("NB-A4-S", "Sketchbook, A4", "80 sheets of 160 g/m² paper for pencil, "
                + "ink and washes.", "36.00", 2, "sketchbook.svg",
                "A sketchbook with a kraft cover and a spiral binding"),
        Product("MG-250-E", "Espresso cups, two", "90 ml each, in a teal glaze, with their "
                + "saucers.", "38.00", 3, "espresso-cups.svg", "Two small teal cups on saucers"),
        Product("BW-180-S", "Serving bowl, 18 cm", "Wide and shallow, for salads and fruit; "
                + "oven safe.", "58.00", 2, "serving-bowl.svg", "A wide sand-colored bowl with a teal rim"),
        Product("NB-A6-P", "Pocket notebook, A6, three", "", "28.00", 4, nil, nil),
        Product("GC-050", "Gift card", "", "50.00", 0, nil, nil),
    ]

    // The colors the covers and the glazes come in, as on the swatches
    static let COLOR_NAMES = ["Navy", "Teal", "Sand", "Clay"]
    static let COLORS: [Int32] = [0x1d3557, 0x0c727d, 0xf4eee4, 0x6e4a2e]

    static let NAVY: Int32 = 0x1d3557
    static let INK: Int32 = 0x333333
    static let SAND: Int32 = 0xf4eee4
    static let CLAY: Int32 = 0x6e4a2e
    static let RULE: Int32 = 0xdde1e8

    static let LEFT: Float = 40.0
    static let WIDTH: Float = 532.0            // Letter, 612 wide, less the margins
    static let GAP: Float = 12.0
    static let PER_ROW = 3
    static let COLUMN: Float = (WIDTH - Float(PER_ROW - 1) * GAP) / Float(PER_ROW)

    private let sans: Font
    private let sansBold: Font
    private let serif: Font
    private let serifItalic: Font
    private let mono: Font

    public init() throws {
        guard let output = OutputStream(toFileAtPath: "Example_79.pdf", append: false) else {
            throw ExampleError(message: "Cannot open Example_79.pdf for writing")
        }
        let pdf = PDF(output)
        pdf.setCompliance(Compliance.PDF_UA_1)
        pdf.setTitle(Example_79.TITLE)
        pdf.setAuthor(Example_79.STUDIO)
        pdf.setLanguage("en-CA")

        sans = try Font(pdf, IBMPlexSans.Regular)
        sans.setSize(10.0)
        sansBold = try Font(pdf, IBMPlexSans.SemiBold)
        sansBold.setSize(10.0)                  // The headings set their own size
        serif = try Font(pdf, IBMPlexSerif.Regular)
        serif.setSize(11.0)
        serifItalic = try Font(pdf, IBMPlexSerif.Italic)
        serifItalic.setSize(13.0)
        mono = try Font(pdf, CascadiaMono.Regular)
        mono.setSize(10.0)

        let left = Example_79.LEFT
        let width = Example_79.WIDTH
        let gap = Example_79.GAP

        var pages = [Page]()

        // Page 1: the logo, the title and a line about the studio, then the
        // products with a picture, three to a row
        var page = Page(pdf, Letter.PORTRAIT, Page.DETACHED)
        pages.append(page)
        let logo = try Example_79.svg(Example_79.IMAGES + "northwind-logo.svg")
        logo.setAltDescription(Example_79.STUDIO)
        logo.setLocation(left, 52.0)
        logo.drawOn(page)
        let x = left + width / 4.0 + gap
        heading(page, Example_79.TITLE, StructElem.H1, 26.0, x, 50.0)
        let tagline = TextBlock(serifItalic,
                "Paper goods and ceramics, made by hand in our studio by the lake.")
        tagline.setTextColor(Example_79.INK)
        tagline.setLocation(x, 86.0)
        tagline.setWidth(left + width - x)
        var y = tagline.drawOn(page)[1] + 18.0
        Example_79.rule(page, y)

        y = heading(page, "Notebooks and mugs", StructElem.H2, 16.0, left, y + 20.0)
        y = paragraph(page, "Every notebook is sewn by hand and opens flat, on paper heavy enough for "
                + "ink. Our mugs are thrown on the wheel and glazed in small batches, so no two are "
                + "quite alike. Each item has its code and its price in Canadian dollars.", left, y, width)

        var column = 0
        var rowTop = y + 18.0
        var rowEnd = rowTop
        for product in Example_79.PRODUCTS {
            if product.picture == nil {
                continue
            }
            let end = try drawProduct(page, product, left + Float(column) * (Example_79.COLUMN + gap), rowTop)
            rowEnd = max(rowEnd, end)
            column += 1
            if column == Example_79.PER_ROW {
                column = 0
                rowTop = rowEnd + 22.0
            }
        }

        // Page 2: the colors, the price list of all the products, and how to order
        page = Page(pdf, Letter.PORTRAIT, Page.DETACHED)
        pages.append(page)
        y = heading(page, "Colors", StructElem.H2, 16.0, left, 52.0)
        y = paragraph(page, "Covers and glazes come in four colors; the price list says how many each "
                + "item has. Name the color with the code when you order.", left, y, width)
        y = drawSwatches(page, y + 14.0)

        y = heading(page, "Price list", StructElem.H2, 16.0, left, y + 30.0)
        y = drawPriceList(page, y + 4.0)

        // How to order on the left, the QR code of the shop online on the right
        let qrX = left + 0.75 * width + gap
        let top = y + 30.0
        var end = heading(page, "Ordering", StructElem.H3, 12.0, left, top)
        paragraph(page, "Prices in Canadian dollars, taxes extra. Orders over $150 ship free within "
                + "Canada. Write to orders@example.com with the codes and colors you would like, and "
                + "we send an invoice within a day.", left, end, 0.75 * width - gap)
        end = heading(page, "Order online", StructElem.H3, 12.0, qrX, top)
        let qr = try QRCode(Example_79.ORDER_URL, ErrorCorrectionLevel.M)
        qr.setModuleLength(2.5)
        qr.setAltDescription("QR code of " + Example_79.ORDER_URL)
        qr.setLocation(qrX, end + 4.0)
        qr.drawOn(page)

        // The running header of each page, now that their number is known
        for i in 0..<pages.count {
            drawHeader(pages[i], i + 1, pages.count)
        }
        pdf.addPages(pages)
        try pdf.complete()
    }

    // Draws a product in its column, its top at y: the picture, the name, a
    // heading under the section, the description, and the code and the
    // price. Returns where it ended.
    private func drawProduct(_ page: Page, _ product: Product, _ x: Float, _ y: Float) throws -> Float {
        // The drawings are 300 wide, scaled to the width of the column
        let picture = try Example_79.svg(Example_79.IMAGES + product.picture!)
        picture.scaleBy(Example_79.COLUMN / picture.getWidth())
        picture.setAltDescription(product.alt!)
        picture.setLocation(x, y)
        var y = picture.drawOn(page)[1]
        y = heading(page, product.name, StructElem.H3, 12.0, x, y + 14.0)
        y = paragraph(page, product.description, x, y, Example_79.COLUMN)
        // The code padded to ten characters, so the prices line up from
        // product to product in the monospace font
        let text = Example_79.padded(product.code, 10) + " $" + Example_79.money(product.price)
        let price = TextLine(mono, text)
        price.setTextColor(Example_79.INK)
        price.setLocation(x, y + 18.0)
        price.drawOn(page)
        return y + 18.0
    }

    // Draws a swatch of each color with its name under it, in four columns,
    // its top at y, and returns where the names end. The swatches are
    // decoration; the names say the colors.
    private func drawSwatches(_ page: Page, _ y: Float) -> Float {
        let width = (Example_79.WIDTH - 3 * Example_79.GAP) / 4.0
        for i in 0..<Example_79.COLORS.count {
            let x = Example_79.LEFT + Float(i) * (width + Example_79.GAP)
            let swatch = Rect(x, y, width, 40.0)
            swatch.setCornerRadius(6.0)
            swatch.setFillColor(Example_79.COLORS[i])
            // The sand swatch is near the color of the paper, so it has a border
            swatch.setBorderColor(Example_79.COLORS[i] == Example_79.SAND ? Example_79.CLAY : Example_79.COLORS[i])
            swatch.drawOn(page)
            let name = TextLine(sans, Example_79.COLOR_NAMES[i])
            name.setLocation(x + (width - name.getWidth()) / 2.0, y + 58.0)
            name.drawOn(page)
        }
        return y + 58.0
    }

    // Draws the price list of all the products, its top at y, and returns
    // where it ended: the header in white on navy, the rows on sand, no
    // borders, the codes and the prices in the monospace font.
    private func drawPriceList(_ page: Page, _ y: Float) -> Float {
        var rows = [[Cell]]()
        rows.append(Example_79.row(sansBold, "Code", "Item", "Colors", "Price"))
        for product in Example_79.PRODUCTS {
            rows.append(Example_79.row(mono, product.code, product.name,
                    (product.colors == 0) ? "" : String(product.colors),
                    "$" + Example_79.money(product.price)))
        }
        for i in 1..<rows.count {
            for cell in rows[i] {
                cell.setBackgroundColor(Example_79.SAND)
            }
        }

        let table = Table()
        table.setTableData(rows, 1)
        table.setHeaderRowStyle(sansBold, Color.white, Example_79.NAVY)
        table.setCellBorders(false)
        table.setCellPadding(4.0, 3.0)
        table.setWidth(Example_79.WIDTH)
        table.setColumnWidthsInPercent(20.0, 50.0, 15.0, 15.0)
        table.setTextAlignmentInColumn(2, Alignment.CENTER)
        table.setTextAlignmentInColumn(3, Alignment.RIGHT)
        table.setLocation(Example_79.LEFT, y)
        return table.drawOn(page)[1]
    }

    // The studio and the catalog on the left and the page number on the
    // right, above the content, in small gray type. It repeats on every
    // page, so it is an artifact.
    private func drawHeader(_ page: Page, _ number: Int, _ count: Int) {
        let left = TextLine(sans, Example_79.STUDIO + " · " + Example_79.TITLE)
        left.setFontSize(9.0)
        left.setTextColor(Color.dimgray)
        left.setLocation(Example_79.LEFT, 26.0)
        let right = TextLine(sans, "Page " + String(number) + " of " + String(count))
        right.setFontSize(9.0)
        right.setTextColor(Color.dimgray)
        right.setLocation(Example_79.LEFT + Example_79.WIDTH - right.getWidth(), 26.0)
        page.addArtifactBMC()
        left.drawOn(page)
        right.drawOn(page)
        page.addEMC()
    }

    // Draws a heading in navy, its top at y, and returns where the text
    // under it starts.
    @discardableResult
    private func heading(
            _ page: Page, _ text: String, _ level: StructElem, _ size: Float, _ x: Float, _ y: Float) -> Float {
        let heading = TextLine(sansBold, text)
        heading.setStructureType(level)
        heading.setFontSize(size)
        heading.setTextColor(Example_79.NAVY)
        heading.setLocation(x, y + heading.getAscent())
        heading.drawOn(page)
        return y + heading.getAscent() + 10.0
    }

    // Draws a paragraph in the serif font, its top at y, of the width, and
    // returns where it ended.
    @discardableResult
    private func paragraph(_ page: Page, _ text: String, _ x: Float, _ y: Float, _ width: Float) -> Float {
        let block = TextBlock(serif, text)
        block.setTextColor(Example_79.INK)
        block.setLineSpacing(1.15)
        block.setLocation(x, y)
        block.setWidth(width)
        return block.drawOn(page)[1]
    }

    // A thin line across the page, decoration only, so an artifact that a
    // screen reader skips.
    private static func rule(_ page: Page, _ y: Float) {
        let line = Line(LEFT, y, LEFT + WIDTH, y)
        line.setStrokeColor(RULE)
        page.addArtifactBMC()
        line.drawOn(page)
        page.addEMC()
    }

    // A row of the table, a cell in the font for each text.
    private static func row(_ font: Font, _ texts: String...) -> [Cell] {
        var row = [Cell]()
        for text in texts {
            row.append(Cell(font, text))
        }
        return row
    }

    // The SVG file at the path, or an error that names it.
    private static func svg(_ path: String) throws -> SVGImage {
        guard let image = try SVGImage(fileAtPath: path) else {
            throw ExampleError(message: "Cannot open SVG file: " + path)
        }
        return image
    }

    // The text with spaces after it to the length, as %-10s does in Java's
    // String.format; a longer text is kept whole.
    private static func padded(_ text: String, _ length: Int) -> String {
        return text + String(repeating: " ", count: max(0, length - text.count))
    }

    // The amount rounded to the cent, half up, as 0.125 to 0.13.
    private static func rounded(_ amount: Decimal) -> Decimal {
        var value = amount
        var result = Decimal()
        NSDecimalRound(&result, &value, 2, .plain)
        return result
    }

    // The amount with two decimals and commas between the thousands, as
    // 1,200.00. It is put together here, digit by digit, so that it is the
    // same whatever the locale of the computer, where one set to French
    // writes 1 200,00.
    private static func money(_ amount: Decimal) -> String {
        let cents = NSDecimalNumber(decimal: rounded(amount) * 100).int64Value
        let digits = Array(String(abs(cents) / 100))
        var text = cents < 0 ? "-" : ""
        for i in 0..<digits.count {
            if i > 0 && (digits.count - i) % 3 == 0 {
                text += ","
            }
            text.append(digits[i])
        }
        let rest = abs(cents) % 100
        return text + (rest < 10 ? ".0" : ".") + String(rest)
    }
}   // End of Example_79.swift

let time0 = Int64(Date().timeIntervalSince1970 * 1000)
_ = try Example_79()
let time1 = Int64(Date().timeIntervalSince1970 * 1000)
print("Example_79 => \(String(format: "%4lld", time1 - time0)) ms")
