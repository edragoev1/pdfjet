import Foundation
import PDFjet

/**
 *  Example_41.swift
 */
public class Example_41 {
    public init() throws {
        if let stream = OutputStream(toFileAtPath: "Example_41.pdf", append: false) {
            let pdf = PDF(stream)

            let f1 = Font(pdf, CoreFont.HELVETICA)
            let f2 = Font(pdf, CoreFont.HELVETICA_BOLD)
            let f3 = Font(pdf, CoreFont.HELVETICA_OBLIQUE)

            f1.setSize(10.0)
            f2.setSize(10.0)
            f3.setSize(10.0)

            let page = Page(pdf, Letter.PORTRAIT)

            // var paragraphs = [Paragraph]()

            // var paragraph = Paragraph()
            //         .add(TextLine(f1, "The small business centres offer practical resources, from step-by-step info on setting up your business to sample business plans to a range of business-related articles and books in our resource libraries.")
            //         .setUnderline(true))
            //         .add(TextLine(f2, "This text is bold!")
            //         .setColor(Color.blue))
            // paragraphs.append(paragraph)

            // paragraph = Paragraph()
            //         .add(TextLine(f1, "The centres also offer free one-on-one consultations with business advisors who can review your business plan and make recommendations to improve it.")
            //         .setUnderline(true))
            //         .add(TextLine(f3, "This text is using italic font.")
            //         .setColor(Color.green))
            // paragraphs.append(paragraph)

            var paragraphs = try Text.paragraphsFromFile(f1, "data/physics.txt")
            var colorMap = [String: Int32]()
            colorMap["Physics"] = Color.red
            colorMap["physics"] = Color.red
            colorMap["Experimentation"] =  Color.orange
            paragraphs = try Text.paragraphsFromFile(f1, "data/physics.txt")
            let f2size = f2.getSize()
            for p in paragraphs {
                if (p.startsWith("**")) {
                    f2.setSize(24.0)
                    p.getTextLines()[0].setFont(f2)
                    p.getTextLines()[0].setColor(Color.navy);
                } else {
                    p.setColor(Color.gray)
                    p.setColorMap(colorMap)
                }
            }
            f2.setSize(f2size);

            let text = Text(paragraphs)
            text.setLocation(70.0, 90.0)
            text.setWidth(500.0)
            // text.setBorder(true)
            // text.setSpaceBetweenTextLines(0.0)
            text.drawOn(page)

            var paragraphNumber: Int = 1
            for p in paragraphs {
                if p.startsWith("**") {
                    paragraphNumber = 1
                } else {
                    TextLine(f2, String(paragraphNumber) + ".")
                            .setLocation(p.xText! - 15.0, p.yText!)
                            .drawOn(page)
                    paragraphNumber += 1
                }
            }

            pdf.complete()
        }
    }
}   // End of Example_41.swift

let time0 = Int64(Date().timeIntervalSince1970 * 1000)
_ = try Example_41()
let time1 = Int64(Date().timeIntervalSince1970 * 1000)
print("Example_41 => \(time1 - time0)")
