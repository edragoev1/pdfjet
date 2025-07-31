import Foundation

public class BigTable {
    private let pdf: PDF
    private let f1: Font
    private let f2: Font
    private var pageSize: [Float]
    private var y: Float = 0.0
    private var yText: Float = 0.0
    private var pages: [Page] = []
    private var page: Page?
    private var widths: [Float] = []
    private var headerFields: [String] = []
    private var alignment: [Int] = []
    private var vertLines: [Float] = []
    private var bottomMargin: Float = 20.0
    private var padding: Float = 2.0
    private var language: String = "en-US"
    private var highlightRow: Bool = true
    private var highlightColor: Int32 = 0xF0F0F0
    private var penColor: Int32 = 0xB0B0B0
    private var fileName: String = ""
    private var delimiter: String = ""
    private var numberOfColumns: Int = 0
    private var startNewPage: Bool = true

    public init(_ pdf: PDF, _ f1: Font, _ f2: Font, _ pageSize: [Float]) {
        self.pdf = pdf
        self.f1 = f1
        self.f2 = f2
        self.pageSize = pageSize
        self.pages = []
    }

    public func setLocation(_ x: Float, _ y: Float) {
        for i in 0...self.numberOfColumns {
            self.vertLines[i] += x
        }
        self.y = y
    }

    public func setNumberOfColumns(_ numberOfColumns: Int) {
        self.numberOfColumns = numberOfColumns
    }

    public func setTextAlignment(_ column: Int, _ alignment: Int) {
        self.alignment[column] = alignment
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

    private func drawTextAndLine(fields: [String], font: Font) throws {
        if page == nil {
            page = Page(pdf, pageSize, Page.DETACHED)
            pages.append(page!)
            page!.setPenWidth(0.0)
            self.yText = self.y + f1.ascent
            self.highlightRow = true
            drawFieldsAndLine(fields: headerFields, font: f1)
            self.yText += f1.descent + f2.ascent
            startNewPage = false
            return
        }
        if startNewPage {
            page = Page(pdf, pageSize, Page.DETACHED)
            pages.append(page!)
            page!.setPenWidth(0.0)
            self.yText = self.y + f1.ascent
            self.highlightRow = true
            drawFieldsAndLine(fields: headerFields, font: f1)
            self.yText += f1.descent + f2.ascent
            startNewPage = false
        }

        drawFieldsAndLine(fields: fields, font: f2)
        self.yText += f2.ascent + f2.descent
        if self.yText > (page!.height - self.bottomMargin) {
            drawTheVerticalLines()
            startNewPage = true
        }
    }

    private func drawFieldsAndLine(fields: [String], font: Font) {
        if fields.count < numberOfColumns {
            return
        }
        page!.addArtifactBMC()
        if self.highlightRow {
            highlightRow(page: page!, font: font, color: highlightColor)
            self.highlightRow = false
        } else {
            self.highlightRow = true
        }

        let original = page!.getPenColor()
        page!.setPenColor(penColor)
        page!.moveTo(vertLines[0], self.yText - font.ascent)
        page!.lineTo(vertLines[numberOfColumns], self.yText - font.ascent)
        page!.strokePath()
        page!.setPenColor(original)
        page!.addEMC()

        let rowText = getRowText(row: fields)
        page!.addBMC(StructElem.P, language, rowText, rowText)
        page!.setPenWidth(0.0)
        page!.setTextFont(font)
        page!.setBrushColor(Color.black)
        for i in 0..<numberOfColumns {
            let text = fields[i]
            let xText1 = vertLines[i] + self.padding
            let xText2 = vertLines[i + 1] - self.padding
            page!.beginText()
            if alignment[i] == Alignment.LEFT {
                page!.setTextLocation(xText1, self.yText)
            } else if alignment[i] == Alignment.RIGHT {
                page!.setTextLocation(xText2 - font.stringWidth(text), self.yText)
            }
            page!.drawText(text)
            page!.endText()
        }
        page!.addEMC()
    }

    private func highlightRow(page: Page, font: Font, color: Int32) {
        let original = page.getBrushColor()
        page.setBrushColor(color)
        page.moveTo(vertLines[0], self.yText - font.ascent)
        page.lineTo(vertLines[numberOfColumns], self.yText - font.ascent)
        page.lineTo(vertLines[numberOfColumns], self.yText + font.descent)
        page.lineTo(vertLines[0], self.yText + font.descent)
        page.fillPath()
        page.setBrushColor(original)
    }

    private func drawTheVerticalLines() {
        page!.addArtifactBMC()
        let original = page!.getPenColor()
        page!.setPenColor(penColor)
        for i in 0...numberOfColumns {
            page!.drawLine(
                vertLines[i],
                self.y,
                vertLines[i],
                self.yText - f2.ascent)
        }
        page!.moveTo(vertLines[0], self.yText - f2.ascent)
        page!.lineTo(vertLines[numberOfColumns], self.yText - f2.ascent)
        page!.strokePath()
        page!.setPenColor(original)
        page!.addEMC()
    }

    private func getRowText(row: [String]) -> String {
        var buf = ""
        for field in row {
            buf += field + " "
        }
        return buf
    }

    private func getAlignment(str: String) -> Int {
        var buf = ""
        if str.hasPrefix("(") && str.hasSuffix(")") {
            buf = String(str.dropFirst().dropLast())
        } else {
            buf = str
        }
        
        var cleaned = ""
        for ch in buf {
            if ch != "." && ch != "," && ch != "'" {
                cleaned.append(ch)
            }
        }
        
        if Double(cleaned) != nil {
            return Alignment.RIGHT
        }
        return Alignment.LEFT
    }

    public func setTableData(_ fileName: String, _ delimiter: String) throws {
        self.fileName = fileName
        self.delimiter = delimiter
        self.vertLines = [Float](repeating: 0.0, count: numberOfColumns + 1)
        self.headerFields = [String](repeating: "", count: numberOfColumns)
        self.widths = [Float](repeating: 0.0, count: numberOfColumns)
        self.alignment = [Int](repeating: 0, count: numberOfColumns)

        var rowNumber = 0
        let reader = try String(contentsOfFile: fileName, encoding: String.Encoding.utf8)
        let lines = reader.components(separatedBy: .newlines)

        // let text = try String(contentsOfFile: fileName, encoding: String.Encoding.utf8)
        // let lines = text.components(separatedBy: .newlines)

        for line in lines {
            let fields = line.components(separatedBy: delimiter)
            if fields.count < numberOfColumns {
                continue;
            }
            if rowNumber == 0 {
                for i in 0..<numberOfColumns {
                    headerFields[i] = fields[i]
                }
            }
            if rowNumber == 1 {
                for i in 0..<numberOfColumns {
                    alignment[i] = getAlignment(str: fields[i])
                }
            }
            for i in 0..<numberOfColumns {
                let field = fields[i]
                let width = f1.stringWidth(field) + 2 * self.padding
                if width > widths[i] {
                    widths[i] = width
                }
            }
            rowNumber += 1
        }

        vertLines[0] = 0.0
        var vertLineX: Float = 0.0
        for i in 0..<widths.count {
            vertLineX += widths[i]
            vertLines[i + 1] = vertLineX
        }
    }

    public func complete() throws {
        let reader = try String(contentsOfFile: fileName)
        let lines = reader.components(separatedBy: .newlines)
        
        for line in lines {
            let fields = line.components(separatedBy: delimiter)
            try drawTextAndLine(fields: fields, font: f2)
        }
        drawTheVerticalLines()
    }
}
