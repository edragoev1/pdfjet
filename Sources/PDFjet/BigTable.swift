import Foundation

/**
 * Use this class if you have a lot of data.
 */
public class BigTable {
    private var pdf: PDF
    private var page: Page?
    private var pageSize: [Float]
    private var f1: Font
    private var f2: Font
    private var x1: Float?
    private var y1: Float?
    private var yText: Float?
    private var pages: [Page]
    private var align: [Int]?
    private var vertLines: [Float]
    private var headerRow: [String]?
    private var bottomMargin: Float = 15.0
    private var spacing: Float = 0.0
    private var padding: Float = 2.0
    private var language: String = "en-US"
    private var highlightRow: Bool = true
    private var highlightColor: Int32 = 0xF0F0F0
    private var penColor: Int32 = 0xB0B0B0

    public init(_ pdf: PDF, _ f1: Font, _ f2: Font, _ pageSize: [Float]) {
        self.pdf = pdf
        self.pageSize = pageSize
        self.f1 = f1
        self.f2 = f2
        self.pages = [Page]()
        self.vertLines = [Float]()
    }

    public func setLocation(_ x1: Float, _ y1: Float) {
        self.x1 = x1
        self.y1 = y1
    }

    public func setTextAlignment(_ align: [Int]) {
        self.align = align
    }

    public func setColumnSpacing(_ spacing: Float) {
        self.spacing = spacing
    }

    public func setBottomMargin(_ bottomMargin: Float) {
        self.bottomMargin = bottomMargin
    }

    public func setLanguage(_ language: String) {
        self.language = language
    }

    public func getPages() -> [Page] {
        return pages
    }

    public func setColumnWidths(_ widths: [Float]) {
        vertLines.removeAll()
        vertLines.append(x1!)
        var sumOfWidths = x1!
        for width in widths {
            sumOfWidths += width + spacing
            vertLines.append(sumOfWidths)
        }
    }

    public func drawRow(_ row: [String], _ markerColor: Int32) {
        if headerRow == nil {
            headerRow = row
            newPage(row, Color.black)
        } else {
            drawOn(row, markerColor)
        }
    }

    private func newPage(_ row: [String], _ color: Int32) {
        var original: [Float]
        if page != nil {
            page!.addArtifactBMC()
            original = page!.getPenColor()
            page!.setPenColor(penColor)
            page!.drawLine(Float(vertLines[0]), yText! - f1.ascent, Float(vertLines[headerRow!.count]), yText! - f1.ascent)
            // Draw the vertical lines
            var i = 0
            while i <= headerRow!.count {
                page!.drawLine(vertLines[i], y1!, vertLines[i], yText! - f1.ascent)
                i += 1
            }
            page!.setPenColor(original)
            page!.addEMC()
        }

        page = Page(pdf, pageSize, Page.DETACHED)
        pages.append(page!)
        page!.setPenWidth(0.0)
        yText = y1! + f1.ascent

        // Highlight row and draw horizontal line
        page!.addArtifactBMC()
        drawHighlight(page!, highlightColor, f1)
        highlightRow = false
        original = page!.getPenColor()
        page!.setPenColor(penColor)
        page!.drawLine(Float(vertLines[0]), yText! - f1.ascent, Float(vertLines[headerRow!.count]), yText! - f1.ascent)
        page!.setPenColor(original)
        page!.addEMC()

        let rowText = getRowText(headerRow!)
        page!.addBMC(StructElem.P, language, rowText, rowText)
        page!.setTextFont(f1)
        page!.setBrushColor(color)
        var xText: Float?
        var xText2: Float?
        var i = 0
        while i < headerRow!.count {
            let text = headerRow![i]
            xText = Float(vertLines[i])
            xText2 = Float(vertLines[i + 1])
            page!.beginText()
            if align == nil || align![i] == 0 { // Align Left
                page!.setTextLocation((xText! + padding), yText!)
            } else if align![i] == 1 {          // Align Right
                page!.setTextLocation((xText2! - padding) - f1.stringWidth(text), yText!)
            }
            page!.drawText(text)
            page!.endText()
            i += 1
        }
        page!.addEMC()
        yText! += f1.descent + f2.ascent
    }

    private func drawOn(_ row: [String], _ markerColor: Int32) {
        if (row.count > headerRow!.count) {
            // Prevent crashes when some data rows have extra fields!
            // The application should check for this and handle it the right way.
            return;
        }

        // Highlight row and draw horizontal line
        page!.addArtifactBMC()
        if highlightRow {
            drawHighlight(page!, highlightColor, f2)
            highlightRow = false
        } else {
            highlightRow = true
        }
        let original = page!.getPenColor()
        page!.setPenColor(penColor)
        page!.drawLine(Float(vertLines[0]), yText! - f2.ascent, Float(vertLines[headerRow!.count]), yText! - f2.ascent)
        page!.setPenColor(original)
        page!.addEMC()

        let rowText = getRowText(row)
        page!.addBMC(StructElem.P, language, rowText, rowText)
        page!.setPenWidth(0.0)
        page!.setTextFont(f2)
        page!.setBrushColor(Color.black)
        var xText: Float
        var xText2: Float?
        var i = 0
        while i < row.count {
            let text = row[i]
            xText = Float(vertLines[i])
            xText2 = Float(vertLines[i + 1])
            page!.beginText()
            if align == nil || align![i] == 0 { // Align Left
                page!.setTextLocation((xText + padding), yText!)
            } else if align![i] == 1 {          // Align Right
                page!.setTextLocation((xText2! - padding) - f2.stringWidth(text), yText!)
            }
            page!.drawText(text)
            page!.endText()
            i += 1
        }
        page!.addEMC()
        if markerColor != Color.black {
            page!.addArtifactBMC()
            let originalColor = page!.getPenColor()
            page!.setPenColor(markerColor)
            page!.setPenWidth(3.0)
            page!.drawLine(vertLines[0] - 2.0, yText! - f2.ascent, vertLines[0] - 2.0, yText! + f2.descent)
            page!.drawLine(xText2! + 2.0, yText! - f2.ascent, xText2! + 2.0, yText! + f2.descent)
            page!.setPenColor(originalColor)
            page!.setPenWidth(0.0)
            page!.addEMC()
        }
        yText! += f2.descent + f2.ascent
        if (yText! + f2.descent > (page!.height - bottomMargin)) {
            newPage(row, Color.black)
        }
    }

    public func complete() {
	    page!.addArtifactBMC()
        let original = page!.getPenColor()
        page!.setPenColor(penColor)
        page!.drawLine(Float(vertLines[0]), yText! - f2.ascent, Float(vertLines[headerRow!.count]), yText! - f2.ascent)
        // Draw the vertical lines
        var i = 0
        while i <= headerRow!.count {
            page!.drawLine(vertLines[i], y1!, vertLines[i], yText! - f1.ascent)
            i += 1
        }
        page!.setPenColor(original)
        page!.addEMC()
    }

    private func drawHighlight(_ page: Page, _ color: Int32, _ font: Font) {
        let original = page.getBrushColor()
        page.setBrushColor(color)
        page.moveTo(Float(vertLines[0]), yText! - font.ascent)
        page.lineTo(Float(vertLines[headerRow!.count]), yText! - font.ascent)
        page.lineTo(Float(vertLines[headerRow!.count]), yText! + font.descent)
        page.lineTo(Float(vertLines[0]), yText! + font.descent)
        page.fillPath()
        page.setBrushColor(original)
    }

    private func getRowText(_ row: [String]) -> String {
        var buf = String()
        for field in row {
            buf.append(field)
            buf.append(" ")
        }
        return buf
    }

    public func getColumnWidths(_ fileName: String) throws -> [Float] {
        let text = try String(contentsOfFile: fileName, encoding: String.Encoding.utf8)
        let lines = text.components(separatedBy: .newlines)
        var widths = [Float]()
        align = [Int]()
        var rowNumber = 0
        for line in lines {
            if line != "" {
                let fields: [String] = line.components(separatedBy: ",")
                var i = 0
                while i < fields.count {
                    let field = fields[i]
                    let width = f1.stringWidth(nil, field)
                    if rowNumber == 0 {         // Header Row
                        widths.append(width)
                    } else {
                        if i < widths.count && width > widths[i] {
                            widths[i] = width
                        }
                    }
                    i += 1
                }
                if rowNumber == 1 {             // First Data Row
                    for field in fields {
                        align!.append(getAlignment(field))
                    }
                }
                rowNumber += 1
            }
        }
        return widths
    }

    func getAlignment(_ str: String) -> Int {
        var buf = String(str)
        if (str.hasPrefix("(") && str.hasSuffix(")")) {
            let index1 = str.index(str.startIndex, offsetBy: 1)
            let index2 = str.index(str.endIndex, offsetBy: -1)
            buf = String(str[index1..<index2])
        }
        for scalar in buf.unicodeScalars {
            if (scalar != "." && scalar != "," && scalar != "'") {
                buf.append(String(scalar))
            }
        }
        let value = Double(buf)
        if value != nil {
            return 1    // Align Right
        }
        return 0        // Align Left
    }
}
