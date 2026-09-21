/**
 * main.swift
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
import Foundation
import PDFjet

/**
 * Example_03.swift
 */
public class Example_03 {
    public init() throws {
        let pdf = PDF(OutputStream(toFileAtPath: "Example_03.pdf", append: false)!)
        pdf.setCompliance(Compliance.PDF_UA_1)
        pdf.setTitle("Paragraphs in a Text Frame")

        let f1 = try Font(pdf, IBMPlexSans.Regular)
        f1.setSize(10.0)

        let f2 = try Font(pdf, IBMPlexSans.Bold)
        f2.setSize(10.0)

        let f3 = try Font(pdf, IBMPlexSans.Italic)
        f3.setSize(10.0)

        let page = Page(pdf, Letter.PORTRAIT)

        var paragraphs = [Paragraph]()
        var paragraph = Paragraph()
                .add(TextLine(f1,
"The small business centres offer practical resources, from step-by-step info on setting up your business to sample business plans to a range of business-related articles and books in our resource libraries.")
                        .setUnderline(true))
                .add(TextLine(f2, "This text is bold!").setTextColor(Color.blue))
        paragraphs.append(paragraph)

        paragraph = Paragraph()
                .add(TextLine(f1,
"The centres also offer free one-on-one consultations with business advisors who can review your business plan and make recommendations to improve it.")
                        .setUnderline(true))
                .add(TextLine(f3, "This text is using italic font.").setTextColor(Color.green))
        paragraphs.append(paragraph)

        // The two paragraphs are a numbered list. The number of a paragraph
        // is its label, which the text frame draws to the left of it and
        // reads before it.
        var paragraphNumber: Int = 1
        for p in paragraphs {
            p.setListLabel(TextLine(f2, String(paragraphNumber) + "."), 15.0)
            paragraphNumber += 1
        }

        var text = TextFrame(paragraphs)
        text.setLocation(70.0, 50.0)
        text.setWidth(500.0)
        text.setBorders(true)
        text.setBorderColor(Color.blue)
        text.drawOn(page)


        var colorMap = [String: Int32]()
        colorMap["Physics"] = Color.red
        colorMap["physics"] = Color.red
        colorMap["Experimentation"] =  Color.orange
        colorMap["science"] = Color.blue
        paragraphs = try Paragraph.paragraphsFromFile(f1, "data/physics.txt")
        // The paragraphs under each heading are a numbered list.
        paragraphNumber = 1
        for p in paragraphs {
            if (p.startsWith("**")) {
                p.setStructureType(StructElem.H1)
                p.getTextLines()[0].setFont(f2).setFontSize(24.0)
                p.getTextLines()[0].setTextColor(Color.navy)
                paragraphNumber = 1
            } else {
                p.setTextColor(Color.gray)
                p.setHighlightColors(colorMap)
                p.setListLabel(TextLine(f2, String(paragraphNumber) + "."), 15.0)
                paragraphNumber += 1
            }
        }

        text = TextFrame(paragraphs)
        text.setLocation(70.0, 150.0)
        text.setWidth(500.0)
        text.setBorders(true)
        text.setBorderColor(Color.blue)
        text.drawOn(page)

        // The rule beside each paragraph reaches from its top to its bottom,
        // which the frame knows only after it has drawn it.
        for p in paragraphs where !p.startsWith("**") {
            Line(p.getX1() - 3.0, p.getY1(), p.getX1() - 3.0, p.getY2())
                    .setStrokeColor(Color.navy)
                    .setStrokeWidth(1.0).drawOn(page)
        }

        try pdf.complete()
    }
}   // End of Example_03.swift

let time0 = Int64(Date().timeIntervalSince1970 * 1000)
_ = try Example_03()
let time1 = Int64(Date().timeIntervalSince1970 * 1000)
print("Example_03 => \(String(format: "%4lld", time1 - time0)) ms")
