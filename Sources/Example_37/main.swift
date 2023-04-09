import Foundation
import PDFjet

/**
 *  Example_37.swift
 */
public class Example_37 {
    public init() throws {
        var pdf = PDF(OutputStream(toFileAtPath: "Example_37.pdf", append: false)!)

        var objects = try pdf.read(from: InputStream(fileAtPath: "data/testPDFs/wirth.pdf")!)
        // try pdf.read(&objects, from: InputStream(fileAtPath: "data/testPDFs/Smalltalk-and-OO.pdf")!)
        // try pdf.read(&objects, from: InputStream(fileAtPath: "data/testPDFs/InsideSmalltalk1.pdf")!)
        // try pdf.read(&objects, from: InputStream(fileAtPath: "data/testPDFs/InsideSmalltalk2.pdf")!)
        // try pdf.read(&objects, from: InputStream(fileAtPath: "data/testPDFs/Greenbook.pdf")!)
        // try pdf.read(&objects, from: InputStream(fileAtPath: "data/testPDFs/Bluebook.pdf")!)
        // try pdf.read(&objects, from: InputStream(fileAtPath: "data/testPDFs/Orangebook.pdf")!)

        let f1 = try Font(
                &objects,
                InputStream(fileAtPath: "fonts/OpenSans/OpenSans-Regular.ttf.stream")!,
                Font.STREAM)
        f1.setSize(72.0)

        let line = TextLine(f1, "This is a test!")
        line.setLocation(50.0, 350.0)
        line.setColor(Color.peru)

        let pages = pdf.getPageObjects(from: &objects)
        for i in 0..<pages.count {
            var pageObj = pages[i]
            let gs = GraphicsState()
            gs.setAlphaStroking(0.75)           // Stroking alpha
            gs.setAlphaNonStroking(0.75)        // Nonstroking alpha
            pageObj.setGraphicsState(gs, &objects)

            let page = Page(&pdf, &pageObj)
            page.addResource(f1, &objects)
            page.setBrushColor(Color.blue)
            page.drawString(f1, "Hello, World!", 50.0, 200.0)

            line.drawOn(page)

            page.complete(&objects) // The graphics stack is unwinded automatically
        }
        pdf.addObjects(&objects)

/*
        var images = [Image]()
        for obj in objects {
            if obj.getValue("/Subtype") == "/Image" {
                let w = Float(obj.getValue("/Width"))!
                let h = Float(obj.getValue("/Height"))!
                if w > 500.0 && h > 500.0 {
                    images.append(try Image(pdf, obj))
                }
            }
        }

        let f1 = Font(pdf, CoreFont.HELVETICA)
        f1.setSize(72.0)

        var page: Page?
        for image in images {
            page = Page(pdf, A4.PORTRAIT)

            let gs = GraphicsState()
            gs.set_CA(0.75)     // Stroking alpha
            gs.set_ca(0.75)     // Nonstroking alpha
            page!.setGraphicsState(gs)

            image.resizeToFit(page!, keepAspectRatio: true)

            // image.flipUpsideDown(true)
            // image.setLocation(0.0, -image.getHeight()!)

            // image.setRotate(ClockWise._180_degrees)
            // image.setLocation(0.0, 0.0)

            image.drawOn(page!)

            let text = TextLine(f1, "Hello, World!")
            text.setColor(Color.blue)
            text.setLocation(150.0, 150.0)
            text.drawOn(page!)

            page!.setGraphicsState(GraphicsState())
        }
*/
        pdf.complete()
    }
}   // End of Example_37.swift

let time0 = Int64(Date().timeIntervalSince1970 * 1000)
_ = try Example_37()
let time1 = Int64(Date().timeIntervalSince1970 * 1000)
print("Example_37 => \(time1 - time0)")
