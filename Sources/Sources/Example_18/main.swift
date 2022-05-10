import Foundation
import PDFjet


/**
 *  Example_18.swift
 *
 */
public class Example_18 {

    public init() {

        if let stream = OutputStream(toFileAtPath: "Example_18.pdf", append: false) {

            let pdf = PDF(stream)
            let page = Page(pdf, Letter.PORTRAIT)

            page.setPenWidth(5.0)
            page.setBrushColor(0x353638)

            let x1: Float = 300.0
            let y1: Float = 300.0
            let r1: Float = 50.0
            let r2: Float = 50.0

            var path = [Point]()

            let segment1 = Path.getCurvePoints(x1, y1, r1, r2, Segment.CLOCKWISE_00_03)
            var segment2 = Path.getCurvePoints(x1, y1, r1, r2, Segment.CLOCKWISE_03_06)
            var segment3 = Path.getCurvePoints(x1, y1, r1, r2, Segment.CLOCKWISE_06_09)

            path.append(contentsOf: segment1)

            segment2.removeFirst()
            path.append(contentsOf: segment2)

            segment3.removeFirst()
            path.append(contentsOf: segment3)

            // page.drawPath(path, Operation.FILL)
            page.drawPath(path, Operation.STROKE)

            let segment4 = Path.getCurvePoints(x1, y1, r1, r2, Segment.CLOCKWISE_09_12)
            page.setPenWidth(15.0)
            page.setPenColor(Color.red)
            page.drawPath(segment4, Operation.STROKE)

            pdf.complete()
        }
    }

}   // End of Example_18.swift

let time0 = Int64(Date().timeIntervalSince1970 * 1000)
_ = Example_18()
let time1 = Int64(Date().timeIntervalSince1970 * 1000)
print("Example_18 => \(time1 - time0)")
