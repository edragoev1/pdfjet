import Foundation
import PDFjet

/**
 *  Example_49.swift
 */
public class Example_49 {
    public init() throws {
        let pdf = PDF(OutputStream(toFileAtPath: "Example_49.pdf", append: false)!)

        let f1 = try Font(pdf, "fonts/Droid/DroidSerif-Regular.ttf")
        let f2 = try Font(pdf, "fonts/Droid/DroidSerif-Italic.ttf")

        f1.setSize(14.0)
        f2.setSize(16.0)

        let page = Page(pdf, Letter.PORTRAIT)

        let paragraph1 = Paragraph()
                .add(TextLine(f1, "Hello"))
                .add(TextLine(f1, "W").setColor(Color.black).setTrailingSpace(false))
                .add(TextLine(f1, "o").setColor(Color.red).setTrailingSpace(false))
                .add(TextLine(f1, "r").setColor(Color.green).setTrailingSpace(false))
                .add(TextLine(f1, "l").setColor(Color.blue).setTrailingSpace(false))
                .add(TextLine(f1, "d").setColor(Color.black))
                .add(TextLine(f1, "$").setTrailingSpace(false)
                        .setVerticalOffset(f1.getAscent() - f2.getAscent()))
                .add(TextLine(f2, "29.95").setColor(Color.blue))
                .setAlignment(Align.RIGHT)

        let paragraph2 = Paragraph()
                .add(TextLine(f1, "Hello"))
                .add(TextLine(f1, "World"))
                .add(TextLine(f1, "$"))
                .add(TextLine(f2, "29.95").setColor(Color.blue))
                .setAlignment(Align.RIGHT)

        let column = TextColumn()
        column.addParagraph(paragraph1)
        column.addParagraph(paragraph2)
        column.setLocation(70.0, 150.0)
        column.setWidth(500.0)
        column.drawOn(page)

        var paragraphs = [Paragraph]()
        paragraphs.append(paragraph1)
        paragraphs.append(paragraph2)

        let text = Text(paragraphs)
        text.setLocation(70.0, 200.0)
        text.setWidth(500.0)
        text.drawOn(page)

        pdf.complete()
    }
}   // End of Example_49.swift

let time0 = Int64(Date().timeIntervalSince1970 * 1000)
_ = try Example_49()
let time1 = Int64(Date().timeIntervalSince1970 * 1000)
TextUtils.printDuration("Example_49", time0, time1)
