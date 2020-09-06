import Foundation
import PDFjet


/**
 *  Example_93.swift
 *
 */
public class Example_93 {

    public init() throws {
        let stream = OutputStream(toFileAtPath: "Example_93.pdf", append: false)

        let pdf = PDF(stream!)

        let image1 = try Image(
                pdf,
                InputStream(fileAtPath: "images/ee-map.png")!,
                ImageType.PNG)
/*
        let image1 = try Image(
                pdf,
                InputStream(fileAtPath: "images/ee-map.png.stream")!,
                ImageType.PNG_STREAM)
*/
        let page = Page(pdf, A4.PORTRAIT)

        image1.scaleBy(0.75)
        let xy = image1.drawOn(page)

        Box().setLocation(xy[0], xy[1]).setSize(20.0, 20.0).drawOn(page)

        pdf.complete()
    }

}   // End of Example_93.swift

let time0 = Int64(Date().timeIntervalSince1970 * 1000)
_ = try Example_93()
let time1 = Int64(Date().timeIntervalSince1970 * 1000)
print("Example_93 => \(time1 - time0)")
