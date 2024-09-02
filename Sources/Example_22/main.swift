import Foundation
import PDFjet

/**
 *  Example_22.swift
 */
public class Example_22 {
    public init() throws {
        let pdf = PDF(OutputStream(toFileAtPath: "Example_22.pdf", append: false)!)
        let f1 = Font(pdf, CoreFont.HELVETICA)

        var page = Page(pdf, Letter.PORTRAIT)

        var text = TextLine(f1, "Page #1 -> Go to Destination #3.")
        text.setGoToAction("dest#3")
        text.setLocation(90.0, 50.0)
        page.addDestination("dest#1", 0.0, 0.0)
        text.drawOn(page)

        page = Page(pdf, Letter.PORTRAIT)

        text = TextLine(f1, "Page #2 -> Go to Destination #3.")
        text.setGoToAction("dest#3")
        text.setLocation(90.0, 550.0)
        page.addDestination("dest#2", text.getDestinationY())
        text.drawOn(page)

        page = Page(pdf, Letter.PORTRAIT)

        text = TextLine(f1, "Page #3 -> Go to Destination #4.")
        text.setGoToAction("dest#4")
        text.setLocation(90.0, 700.0)
        page.addDestination("dest#3", text.getDestinationY())
        text.drawOn(page)

        page = Page(pdf, Letter.PORTRAIT)

        text = TextLine(f1, "Page #4 -> Go to Destination #1.")
        text.setGoToAction("dest#1")
        text.setLocation(90.0, 100.0)
        page.addDestination("dest#4", text.getDestinationY())
        text.drawOn(page)

        text = TextLine(f1, "Page #4 -> Go to Destination #2.")
        text.setGoToAction("dest#2")
        text.setLocation(90.0, 200.0)
        text.drawOn(page)

        // Create a box with invisible borders
        let box = Box(20.0, 20.0, 20.0, 20.0)
        box.setColor(Color.white)
        box.setGoToAction("dest#1")
        box.drawOn(page)

        // Create an up arrow and place it in the box
        let path = Path()
        path.add(Point(10.0,  1.0))
        path.add(Point(17.0,  9.0))
        path.add(Point(13.0,  9.0))
        path.add(Point(13.0, 19.0))
        path.add(Point( 7.0, 19.0))
        path.add(Point( 7.0,  9.0))
        path.add(Point( 3.0,  9.0))
        path.setClosePath(true)
        path.setColor(Color.blue)
        path.setFillShape(true)
        path.placeIn(box)
        path.drawOn(page)

        let image = try Image(pdf, "images/up-arrow.png")
        image.setLocation(40.0, 40.0)
        image.setGoToAction("dest#1")
        image.drawOn(page)

        pdf.complete()
    }
}   // End of Example_22.swift

let time0 = Int64(Date().timeIntervalSince1970 * 1000)
_ = try Example_22()
let time1 = Int64(Date().timeIntervalSince1970 * 1000)
TextUtils.printDuration("Example_22", time0, time1)
