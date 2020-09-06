import Foundation
import PDFjet


/**
 *  Example_33.swift
 *
 */
public class Example_33 {

    public init() throws {

        if let stream = OutputStream(toFileAtPath: "Example_33.pdf", append: false) {

            let pdf = PDF(stream)
            let page = Page(pdf, Letter.PORTRAIT)

            let image = try Image(
                    pdf,
                    InputStream(fileAtPath: "images/photoshop.jpg")!,
                    ImageType.JPG)
            image.setLocation(10.0, 10.0)
            image.scaleBy(0.25)
            image.drawOn(page)

            pdf.complete()
        }
    }

}   // End of Example_33.swift

let time0 = Int64(Date().timeIntervalSince1970 * 1000)
_ = try Example_33()
let time1 = Int64(Date().timeIntervalSince1970 * 1000)
print("Example_33 => \(time1 - time0)")
