import Foundation
import PDFjet


/**
 *  Example_02.swift
 *
 *  Draw the Canadian flag using a Path object that contains both lines
 *  and curve segments. Every curve segment must have exactly 2 control points.
 */
public class Example_02 {

    public init() {

        if let stream = OutputStream(toFileAtPath: "Example_02.pdf", append: false) {

            let pdf = PDF(stream)
            let page = Page(pdf, Letter.PORTRAIT)

            let flag = Box(85.0, 85.0, 64.0, 32.0)
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
            path.setFillShape(true)
            path.placeIn(flag, 19.0, 3.0)
    
            path.drawOn(page)
    
            var box = Box()
            box.setSize(16.0, 32.0)
            box.setColor(Color.red)
            box.setFillShape(true)
            box.placeIn(flag, 0.0, 0.0)
            box.drawOn(page)
            box.placeIn(flag, 48.0, 0.0)
            box.drawOn(page)
    
            path.scaleBy(15)
            path.setFillShape(false)
            let xy: [Float] = path.drawOn(page)

            box = Box()
            box.setLocation(xy[0], xy[1])
            box.setSize(20.0, 20.0)
            box.drawOn(page)

            pdf.complete()
        }
    }

}   // End of Example_02.swift

let time0 = Int64(Date().timeIntervalSince1970 * 1000)
_ = Example_02()
let time1 = Int64(Date().timeIntervalSince1970 * 1000)
print("Example_02 => \(time1 - time0)")
