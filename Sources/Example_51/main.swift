import Foundation
import PDFjet

/**
 * Example_51.swift
 *
 * Splits an existing PDF document into one PDF for each of its pages,
 * Example_51_1.pdf to Example_51_5.pdf, and writes all of its pages in reverse
 * order to Example_51.pdf. The objects that read returns are merged into every
 * PDF. The pages keep their content, resources, annotations and links; a link
 * to a page that is not in the same PDF leads nowhere.
 */
public class Example_51 {
    public init() throws {
        let objects = try PDF().read(from: InputStream(fileAtPath: "data/testPDFs/wirth.pdf")!)
        let count = PDF().getPageObjects(from: objects).count

        for i in 1...count {
            let part = PDF(OutputStream(toFileAtPath: "Example_51_\(i).pdf", append: false)!)
            try part.merge(objects, [i])
            try part.complete()
        }

        let pdf = PDF(OutputStream(toFileAtPath: "Example_51.pdf", append: false)!)
        try pdf.merge(objects, Array((1...count).reversed()))
        try pdf.complete()
    }
}   // End of Example_51.swift

let time0 = Int64(Date().timeIntervalSince1970 * 1000)
_ = try Example_51()
let time1 = Int64(Date().timeIntervalSince1970 * 1000)
print("Example_51 => \(time1 - time0) ms")
