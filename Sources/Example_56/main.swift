/**
 * main.swift
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
import Foundation
import PDFjet

/**
 * Example_56.swift
 *
 * The shapes PDFjet draws, each in a cell of its own under its name: lines
 * with dashes and caps, rectangles with square and rounded corners, the
 * markers of charts, an ellipse, arcs, a path of Bézier curves, and a closed
 * path, filled. The second page draws text at every angle around a point.
 *
 * Each shape is a Drawable: it is given its location, its size, its colors
 * and the width of its stroke, and drawOn draws it as vector graphics, which
 * stay sharp at any zoom. The shapes carry no text, so they are artifacts of
 * the PDF/UA document, and the name under each shape says what it is.
 */
public class Example_56 {
    private static let NAVY: Int32 = 0x1d3557
    private static let TEAL: Int32 = 0x2a9d8f
    private static let RED: Int32 = 0xe63946
    private static let PALE: Int32 = 0xe8f1f2

    // The cells of the grid: four across, 128 points wide, and 160 tall.
    private static let LEFT: Float = 50.0
    private static let TOP: Float = 150.0
    private static let CELL_WIDTH: Float = 128.0
    private static let CELL_HEIGHT: Float = 160.0

    private let label: Font

    public init() throws {
        let pdf = PDF(OutputStream(toFileAtPath: "Example_56.pdf", append: false)!)
        pdf.setCompliance(Compliance.PDF_UA_1)
        pdf.setTitle("Shapes")

        let regular = try Font(pdf, IBMPlexSans.Regular)
        let semiBold = try Font(pdf, IBMPlexSans.SemiBold)
        label = try Font(pdf, IBMPlexSans.Regular)
        label.setSize(9.0)

        let navy = Example_56.NAVY
        let teal = Example_56.TEAL
        let red = Example_56.RED
        let pale = Example_56.PALE
        let left = Example_56.LEFT

        var page = Page(pdf, Letter.PORTRAIT)

        let title = TextLine(semiBold, "Shapes")
        title.setStructureType(StructElem.H1)
        title.setFontSize(22.0)
        title.setLocation(left, 70.0)
        title.drawOn(page)

        let about = TextBlock(regular,
                "PDFjet draws shapes as vector graphics, which stay sharp at any zoom. Each "
                + "shape is a Drawable: give it a location, a size, its colors and the width "
                + "of its stroke, and drawOn draws it on the page.")
        about.setFontSize(11.0)
        about.setLineSpacing(1.4)
        about.setLocation(left, 85.0)
        about.setWidth(512.0)
        about.drawOn(page)

        // Lines: solid, dashed and dotted with round caps
        var c = cell(page, 0, 0, "Line: solid, dashed, and dotted with round caps")
        Line(c[0] + 10.0, c[1] + 30.0, c[0] + 110.0, c[1] + 30.0)
                .setStrokeWidth(3.0).setStrokeColor(navy).drawOn(page)
        Line(c[0] + 10.0, c[1] + 60.0, c[0] + 110.0, c[1] + 60.0)
                .setStrokeWidth(3.0).setStrokeColor(teal).setStrokeDashPattern("[8 4] 0").drawOn(page)
        Line(c[0] + 10.0, c[1] + 90.0, c[0] + 110.0, c[1] + 90.0)
                .setStrokeWidth(5.0).setStrokeColor(red).setStrokeDashPattern("[0 10] 0")
                .setLineCapStyle(CapStyle.ROUND).drawOn(page)

        // A rectangle, filled and outlined
        c = cell(page, 1, 0, "Rect: filled, with a border")
        Rect(c[0] + 10.0, c[1] + 20.0, 100.0, 80.0)
                .setFillColor(pale).setBorderColor(navy).setBorderWidth(2.0).drawOn(page)

        // A rectangle with rounded corners
        c = cell(page, 2, 0, "Rect: setCornerRadius")
        Rect(c[0] + 10.0, c[1] + 20.0, 100.0, 80.0)
                .setFillColor(teal).setBorderColor(navy).setBorderWidth(2.0)
                .setCornerRadius(16.0).drawOn(page)

        // The markers of a chart, each a Point with a shape
        c = cell(page, 3, 0, "Point: the markers of charts")
        let shapes: [Shape] = [
            Shape.CIRCLE, Shape.DIAMOND, Shape.BOX, Shape.STAR,
            Shape.UP_ARROW, Shape.DOWN_ARROW, Shape.PLUS, Shape.X_MARK,
        ]
        for i in 0..<shapes.count {
            let point = Point(c[0] + 20.0 + Float(i % 4) * 27.0, c[1] + 35.0 + Float(i / 4) * 45.0)
            point.setShape(shapes[i])
            point.setRadius(9.0)
            point.setFillColor(i % 2 == 0 ? teal : red)
            point.setStrokeColor(navy)
            point.drawOn(page)
        }

        // An ellipse, turned by 30 degrees
        c = cell(page, 0, 1, "Ellipse: setRotation")
        Ellipse()
                .setLocation(c[0] + 60.0, c[1] + 60.0)
                .setRadiusX(50.0)
                .setRadiusY(25.0)
                .setFillColor(pale)
                .setStrokeWidth(2.0)
                .setStrokeColor(navy)
                .setRotation(30.0)
                .drawOn(page)

        // An arc of three quarters of a circle
        c = cell(page, 1, 1, "Arc: setStartAngle and setSweep")
        Arc()
                .setLocation(c[0] + 60.0, c[1] + 60.0)
                .setRadius(40.0)
                .setStartAngle(0.0)
                .setSweep(270.0)
                .setStrokeWidth(6.0)
                .setStrokeColor(teal)
                .drawOn(page)

        // A wave of Bézier curves: a point, two control points, a point
        c = cell(page, 2, 1, "Path: Bézier curves")
        let wave = Path()
        wave.add(Point(0.0, 30.0))
        wave.add(Point(20.0, 0.0, Point.CONTROL_POINT_C))
        wave.add(Point(30.0, 0.0, Point.CONTROL_POINT_C))
        wave.add(Point(50.0, 30.0))
        wave.add(Point(70.0, 60.0, Point.CONTROL_POINT_C))
        wave.add(Point(80.0, 60.0, Point.CONTROL_POINT_C))
        wave.add(Point(100.0, 30.0))
        wave.setStrokeColor(red)
        wave.setStrokeWidth(3.0)
        wave.setLocation(c[0] + 10.0, c[1] + 30.0)
        wave.drawOn(page)

        // A closed path of lines, filled: a six-pointed star
        c = cell(page, 3, 1, "Path: closed and filled")
        let star = Path()
        for i in 0..<12 {
            let angle = Double.pi / 6.0 * Double(i) - Double.pi / 2.0
            let r: Float = (i % 2 == 0) ? 50.0 : 25.0
            star.add(Point(50.0 + r * Float(cos(angle)), 50.0 + r * Float(sin(angle))))
        }
        star.setClosed(true)
        star.setFillShape(true)
        star.setStrokeColor(navy)
        star.setLocation(c[0] + 10.0, c[1] + 10.0)
        star.drawOn(page)

        // The second page: text at every angle around a point
        page = Page(pdf, Letter.PORTRAIT)
        let heading = TextLine(semiBold, "Text at every angle")
        heading.setStructureType(StructElem.H2)
        heading.setFontSize(18.0)
        heading.setLocation(left, 70.0)
        heading.drawOn(page)

        let rotation = TextBlock(regular,
                "setTextRotation turns a TextLine about the point of its location, here every "
                + "15 degrees about the middle of the page, underlined with setUnderline.")
        rotation.setFontSize(11.0)
        rotation.setLineSpacing(1.4)
        rotation.setLocation(left, 85.0)
        rotation.setWidth(512.0)
        rotation.drawOn(page)

        let cx: Float = page.getWidth() / 2.0
        let cy: Float = 380.0
        let text = TextLine(regular)
        text.setFontSize(10.0)
        text.setUnderline(true)
        text.setLocation(cx, cy)
        var i = 0
        while i < 360 {
            text.setTextRotation(-i)
            // The spaces keep the start of the text clear of the circle
            text.setText("                        Hello, World: \(i) degrees")
            text.drawOn(page)
            i += 15
        }
        let hub = Point(cx, cy)
        hub.setShape(Shape.CIRCLE)
        hub.setFillColor(navy)
        hub.setRadius(46.0)
        hub.drawOn(page)
        hub.setFillColor(Color.white)
        hub.setRadius(32.0)
        hub.drawOn(page)

        try pdf.complete()
    }

    // Draws the name of the cell of the grid under it, and returns the top
    // left corner of its drawing area.
    private func cell(_ page: Page, _ column: Int, _ row: Int, _ name: String) -> [Float] {
        let x: Float = Example_56.LEFT + Float(column) * Example_56.CELL_WIDTH
        let y: Float = Example_56.TOP + Float(row) * Example_56.CELL_HEIGHT
        let caption = TextBlock(label, name)
        caption.setTextColor(Color.gray)
        caption.setLocation(x, y + 125.0)
        caption.setWidth(Example_56.CELL_WIDTH - 12.0)
        caption.drawOn(page)
        return [x, y]
    }
}   // End of Example_56.swift

let time0 = Int64(Date().timeIntervalSince1970 * 1000)
_ = try Example_56()
let time1 = Int64(Date().timeIntervalSince1970 * 1000)
print("Example_56 => \(String(format: "%4lld", time1 - time0)) ms")
