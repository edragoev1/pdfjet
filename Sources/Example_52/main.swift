import Foundation
import PDFjet

/**
 *  Example_52.swift
 */
public class Example_52 {
    public init() throws {
        let stream = OutputStream(toFileAtPath: "Example_52.pdf", append: false)
        let pdf = PDF(stream!)

        let f1 = Font(pdf, CoreFont.HELVETICA)
        let f2 = Font(pdf, CoreFont.HELVETICA_OBLIQUE)

        let page = Page(pdf, Letter.PORTRAIT)

        var paragraphs = [Paragraph]()
        let p1 = Paragraph()
        let tl1 = TextLine(f1,
"The Swiss Confederation was founded in 1291 as a defensive alliance among three cantons. In succeeding years, other localities joined the original three. The Swiss Confederation secured its independence from the Holy Roman Empire in 1499. Switzerland's sovereignty and neutrality have long been honored by the major European powers, and the country was not involved in either of the two World Wars. The political and economic integration of Europe over the past half century, as well as Switzerland's role in many UN and international organizations, has strengthened Switzerland's ties with its neighbors. However, the country did not officially become a UN member until 2002.")
        p1.add(tl1)

        let p2 = Paragraph()
        let tl2 = TextLine(f2,
"Even so, unemployment has remained at less than half the EU average.")
        p2.add(tl2)

        paragraphs.append(p1)
        paragraphs.append(p2)

        let text = Text(paragraphs)
        text.setLocation(50.0, 50.0)
        text.setWidth(500.0)
        text.drawOn(page)

        pdf.complete()
    }
}   // End of Example_52.swift

let time0 = Int64(Date().timeIntervalSince1970 * 1000)
_ = try Example_52()
let time1 = Int64(Date().timeIntervalSince1970 * 1000)
TextUtils.printDuration("Example_52", time0, time1)
