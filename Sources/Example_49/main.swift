/**
 * main.swift
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
import Foundation
import PDFjet

/**
 * Example_49.swift
 * This example draws a menu with paragraphs that mix fonts, sizes and colors,
 * currency signs raised with a vertical offset, and a rotated, underlined label.
 */
public class Example_49 {
    public init() throws {
        let pdf = PDF(OutputStream(toFileAtPath: "Example_49.pdf", append: false)!)
        pdf.setCompliance(Compliance.PDF_UA_1)
        pdf.setTitle("Paragraphs with mixed text styles")

        let f1 = try Font(pdf, SourceSerif4.Regular)
        f1.setSize(14.0)

        let f2 = try Font(pdf, SourceSerif4.Italic)
        f2.setSize(14.0)

        let f3 = try Font(pdf, SourceSerif4.SemiBold)
        f3.setSize(14.0)

        let f4 = try Font(pdf, SourceSerif4.SemiBold)
        f4.setSize(9.0)

        let page = Page(pdf, Letter.PORTRAIT)

        let title = TextLine(f3, "Café Menu")
        title.setStructureType(StructElem.H1)
        title.setFontSize(28.0)
        title.setLocation(70.0, 100.0)
        title.drawOn(page)

        // Each paragraph mixes a name, an italic description and a price
        // with a small dollar sign raised by a vertical offset.
        let names = ["Espresso", "Cappuccino", "Hot chocolate"]
        let notes = ["rich and intense", "with steamed milk foam", "made with dark cocoa"]
        let prices = ["3.25", "4.50", "3.95"]

        let column = TextColumn()
        for i in 0..<names.count {
            let paragraph = Paragraph()
                    .add(TextLine(f3, names[i]))
                    .add(TextLine(f2, notes[i]).setTextColor(Color.gray))
                    .add(TextLine(f4, "$").setVerticalOffset(-4.0))
                    .add(TextLine(f1, prices[i]).setTextColor(Color.darkred))
            column.addParagraph(paragraph)
        }

        // A paragraph that colors some of its words, aligned to the right.
        column.addParagraph(Paragraph()
                .add(TextLine(f2, "Freshly"))
                .add(TextLine(f3, "roasted").setTextColor(Color.saddlebrown))
                .add(TextLine(f2, "every"))
                .add(TextLine(f3, "morning").setTextColor(Color.darkorange))
                .setTextAlignment(Alignment.RIGHT))

        column.setLocation(70.0, 140.0)
        column.setWidth(470.0)
        column.setParagraphSpacing(1.8)
        let xy = column.drawOn(page)

        // A TextFrame wraps the words of its paragraphs to its width.
        var paragraphs = [Paragraph]()
        paragraphs.append(Paragraph()
                .add(TextLine(f1, "Our beans come from small farms in"))
                .add(TextLine(f3, "Colombia,"))
                .add(TextLine(f3, "Ethiopia"))
                .add(TextLine(f1, "and"))
                .add(TextLine(f3, "Guatemala,"))
                .add(TextLine(f1, "and we roast them in small batches."))
                .add(TextLine(f2, "Ask us about the beans of the week.").setTextColor(Color.darkred)))
        paragraphs.append(Paragraph()
                .add(TextLine(f2, "Prices include tax.").setTextColor(Color.gray)))

        let frame = TextFrame(paragraphs)
        frame.setLocation(70.0, xy[1] + 30.0)
        frame.setWidth(470.0)
        frame.drawOn(page)

        let label = TextLine(f3, "Today's special!")
        label.setFontSize(18.0)
        label.setTextColor(Color.red)
        label.setLocation(400.0, 90.0)
        label.setTextRotation(15)
        label.setUnderline(true)
        label.drawOn(page)

        try pdf.complete()
    }
}   // End of Example_49.swift


let time0 = Int64(Date().timeIntervalSince1970 * 1000)
_ = try Example_49()
let time1 = Int64(Date().timeIntervalSince1970 * 1000)
print("Example_49 => \(String(format: "%4lld", time1 - time0)) ms")
