/**
 * DrawableTests.swift
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
import Foundation
import Testing
@testable import PDFjet

/// Every Drawable measures itself with drawOn(nil): it draws nothing, returns
/// the corner that drawing returns, and does not change what drawing draws.
@Suite struct DrawableTests {
    typealias Maker = (PDF, Font) throws -> Drawable

    static func drawables() -> [(String, Maker)] {
        return [
            ("TextBlock", { _, font in TextBlock(font, "Hello world, this is a text block that wraps.").setWidth(100) }),
            ("TextColumn", { _, font in
                TextColumn().setWidth(120).addParagraph(Paragraph(TextLine(font, "Hello column text that wraps around here")))
            }),
            ("TextFrame", { _, font in TextFrame(font, ["Hello frame text"]).setWidth(120).setHeight(200) }),
            ("TextLine", { _, font in TextLine(font, "Hello line") }),
            ("CompositeTextLine", { _, font in CompositeTextLine(0, 0).addComponent(TextLine(font, "Composite")) }),
            ("Title", { _, font in Title(font, "Title", 0, 0) }),
            ("Image", { pdf, _ in try Image(pdf, TestSupport.open("images/up-arrow.png")) }),
            ("SVGImage", { _, _ in
                try SVGImage(stream: TestSupport.open("images/svg/arrow_forward_FILL0_wght400_GRAD0_opsz48.svg"))
            }),
            ("Barcode", { _, _ in try Barcode(Barcode.CODE_128, "Hello") }),
            ("QRCode", { _, _ in try QRCode("Hello", ErrorCorrectionLevel.M) }),
            ("PDF417", { _, _ in try PDF417("Hello") }),
            ("DataMatrix", { _, _ in DataMatrix("Hello") }),
            ("Chart", { _, font in
                let chart = Chart(font, font)
                chart.setSize(200, 150)
                chart.addSeries("s").addPoint(0, 0).addPoint(1, 2)
                return chart
            }),
            ("BarChart", { _, font in BarChart(font, font).setSize(200, 150).addSeries("s", [1, 2, 3]) }),
            ("DonutChart", { _, font in
                DonutChart(font, font).setRadii(50, 20).addSlice(Slice(1, Color.red, "a")).addSlice(Slice(2, Color.green, "b"))
            }),
            ("Table", { _, font in
                var data = [[Cell]]()
                for r in 0..<3 {
                    var row = [Cell]()
                    for c in 0..<2 {
                        row.append(Cell(font, "r\(r)c\(c)"))
                    }
                    data.append(row)
                }
                return Table().setTableData(data, 1)
            }),
            ("Rect", { _, _ in Rect().setSize(50, 30).setBorderColor(Color.black) }),
            ("Container", { _, _ in Container(80, 40).add(Rect().setSize(80, 40).setBorderColor(Color.black)) }),
            ("CalendarMonth", { _, font in CalendarMonth(font, font, 2026, 9) }),
            ("Form", { _, font in Form([Field(0, "Name", "John")]).setLabelFont(font).setValueFont(font) }),
            ("CheckBox", { _, font in CheckBox(font, "Check") }),
            ("RadioButton", { _, font in RadioButton(font, "Radio") }),
            ("Line", { _, _ in Line(0, 0, 50, 20) }),
            ("Arc", { _, _ in Arc().setRadius(20).setSweep(90) }),
            ("Point", { _, _ in Point(0, 0).setRadius(5) }),
            ("Path", { _, _ in Path().add(Point(0, 0)).add(Point(30, 20)) }),
            ("Stamp", { pdf, _ in
                let stamp = Stamp(pdf).setSize(50, 30)
                try stamp.complete()
                return stamp
            }),
            ("SquareAnnotation", { _, _ in SquareAnnotation().setSize(30, 20) }),
            ("FileAttachment", { pdf, _ in
                FileAttachment(try EmbeddedFile(pdf, "hello.txt", InputStream(data: Data("Hello".utf8)), false))
            }),
        ]
    }

    private func draw(_ make: Maker, measureFirst: Bool) throws -> ([Float], String) {
        let pdf = TestSupport.newPDF()
        let drawable = try make(pdf, TestSupport.helvetica(pdf))
        drawable.setLocation(40, 60)
        if measureFirst {
            drawable.drawOn(nil)
        }
        let page = Page(pdf, Letter.PORTRAIT)
        let xy = drawable.drawOn(page)
        return (xy, TestSupport.content(page))
    }

    @Test func drawOnNilMeasuresWithoutDrawing() throws {
        var failures = [String]()
        for (name, make) in DrawableTests.drawables() {
            let pdf = TestSupport.newPDF()
            let drawable = try make(pdf, TestSupport.helvetica(pdf))
            drawable.setLocation(40, 60)
            let measured = drawable.drawOn(nil)
            let (drawn, plain) = try draw(make, measureFirst: false)
            let (drawnAfterMeasuring, afterMeasuring) = try draw(make, measureFirst: true)
            if abs(measured[0] - drawn[0]) > TestSupport.delta || abs(measured[1] - drawn[1]) > TestSupport.delta {
                failures.append("\(name): measured \(measured), drawn \(drawn)")
            }
            if plain != afterMeasuring || drawn != drawnAfterMeasuring {
                failures.append("\(name): measuring first changes what is drawn")
            }
        }
        #expect(failures.isEmpty, "\(failures.joined(separator: "\n"))")
    }

    @Test func tablesAndTextColumnsReturnTheirRightEdge() throws {
        for (name, make) in DrawableTests.drawables() where name == "Table" || name == "TextColumn" {
            let pdf = TestSupport.newPDF()
            let drawable = try make(pdf, TestSupport.helvetica(pdf))
            let want: Float = (drawable as? Table).map { 40 + $0.getWidth() } ?? 160
            drawable.setLocation(40, 60)
            let xy = drawable.drawOn(Page(pdf, Letter.PORTRAIT))
            #expect(abs(xy[0] - want) < TestSupport.delta, "\(name)")
        }
    }
    @Test func aContainerDrawsItsAnnotationsInTheSamePlaceEveryTime() throws {
        // The container moved the corners of an annotation by its own
        // location on every drawing, so the annotation of the second page
        // ended up that far from what the container drew there.
        let memory = MemoryPDF()
        let container = Container(200, 100)
        _ = container.setLocation(50, 60)
        let square = SquareAnnotation()
        _ = square.setLocation(10, 10)
        _ = square.setSize(80, 40)
        _ = container.add(square)
        for _ in 0..<3 {
            _ = container.drawOn(Page(memory.pdf, Letter.PORTRAIT))
        }
        try memory.pdf.complete()
        let raw = TestSupport.latin1(memory.bytes)
        let regex = try NSRegularExpression(pattern: "/Rect \\[([-0-9. ]+)\\]")
        var rects = [String]()
        let range = NSRange(raw.startIndex..., in: raw)
        for match in regex.matches(in: raw, range: range) {
            rects.append(String(raw[Range(match.range(at: 1), in: raw)!])
                    .trimmingCharacters(in: .whitespaces))
        }
        #expect(rects.count == 3)
        #expect(rects[0] == rects[1], "the second drawing moved the annotation")
        #expect(rects[0] == rects[2], "the third drawing moved the annotation")
    }
    @Test func everyDrawableDrawsTheSameThingEveryTime() throws {
        // A drawable is drawn where it is put, however often it is drawn: a
        // header, a watermark or a logo goes on every page of a document. The
        // two that hold their place in a text are named below.
        var failures = [String]()
        for (name, make) in DrawableTests.drawables() {
            let pdf = TestSupport.newPDF()
            let drawable = try make(pdf, TestSupport.helvetica(pdf))
            _ = drawable.setLocation(40, 60)
            let page1 = Page(pdf, Letter.PORTRAIT)
            _ = drawable.drawOn(page1)
            let first = TestSupport.latin1(page1.getContent())
            let page2 = Page(pdf, Letter.PORTRAIT)
            _ = drawable.drawOn(page2)
            let same = first == TestSupport.latin1(page2.getContent())
            // A TextFrame and a Table draw what is left of their text and
            // their rows, which is what makes them flow from page to page.
            let flows = name == "TextFrame" || name == "Table"
            if same == flows {
                failures.append(name + (same ? " draws the same thing twice" : " draws something else"))
            }
        }
        #expect(failures.isEmpty, "\(failures)")
    }
}
