import Foundation
import PDFjet

/**
 *  Example_20.swift
 */
public class Example_20 {
    public init() throws {
        let pdf = PDF(OutputStream(toFileAtPath: "Example_20.pdf", append: false)!)
        var objects = try pdf.read(
                from: InputStream(fileAtPath: "data/testPDFs/PDFjetLogo.pdf")!)
        pdf.addResourceObjects(&objects)

        let f1 = try Font(
                pdf,
                InputStream(fileAtPath: "fonts/OpenSans/OpenSans-Regular.ttf.stream")!,
                Font.STREAM).setSize(18.0)

        let pages = pdf.getPageObjects(from: objects)
        let contents = pages[0].getContentsObject(&objects)!
        var page = Page(pdf, Letter.PORTRAIT)

        let height: Float = 105.0   // The logo height in points.
        let x: Float = 50.0
        let y: Float = 50.0
        let xScale: Float = 0.5
        let yScale: Float = 0.5

        page.drawContents(
                contents.getData(),
                height,
                x,
                y,
                xScale,
                yScale)

        page.setPenColor(Color.darkblue)
        page.setPenWidth(0.0)
        page.drawRect(0.0, 0.0, 50.0, 50.0)

        let path = Path()

        path.add(Point(13.0,  0.0))
        path.add(Point(15.5,  4.5))

        path.add(Point(18.0,  3.5))
        path.add(Point(15.5, 13.5, Point.CONTROL_POINT))
        path.add(Point(15.5, 13.5, Point.CONTROL_POINT))
        path.add(Point(20.5,  7.5))

        path.add(Point(21.0,  9.5))
        path.add(Point(25.0,  9.0))
        path.add(Point(24.0, 13.0))
        path.add(Point(25.5, 14.0))
        path.add(Point(19.0, 19.0))
        path.add(Point(20.0, 21.5))
        path.add(Point(13.5, 20.5))
        path.add(Point(13.5, 27.0))
        path.add(Point(12.5, 27.0))
        path.add(Point(12.5, 20.5))
        path.add(Point( 6.0, 21.5))
        path.add(Point( 7.0, 19.0))
        path.add(Point( 0.5, 14.0))
        path.add(Point( 2.0, 13.0))
        path.add(Point( 1.0,  9.0))
        path.add(Point( 5.0,  9.5))

        path.add(Point( 5.5,  7.5))
        path.add(Point(10.5, 13.5, Point.CONTROL_POINT))
        path.add(Point(10.5, 13.5, Point.CONTROL_POINT))
        path.add(Point( 8.0,  3.5))

        path.add(Point(10.5,  4.5))
        path.setClosePath(true)
        path.setColor(Color.red)
        // path.setFillShape(true)
        path.setLocation(100.0, 100.0)
        path.scaleBy(10.0)

        path.drawOn(page)

        page = Page(pdf, Letter.PORTRAIT)

        let line = TextLine(f1, "Hello, World!")
        line.setLocation(50.0, 50.0)
        line.drawOn(page)

        let qr = QRCode(
                "https://kazuhikoarase.github.io",
                ErrorCorrectLevel.L)    // Low
        qr.setModuleLength(3.0)
        qr.setLocation(50.0, 200.0)
        qr.drawOn(page)

        pdf.complete()
    }
}   // End of Example_20.swift

let time0 = Int64(Date().timeIntervalSince1970 * 1000)
_ = try Example_20()
let time1 = Int64(Date().timeIntervalSince1970 * 1000)
print("Example_20 => \(time1 - time0)")
