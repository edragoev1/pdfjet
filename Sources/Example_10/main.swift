import Foundation
import PDFjet

///
/// Example_10.swift
///
public class Example_10 {
    public init() throws {
        let pdf = PDF(OutputStream(toFileAtPath: "Example_10.pdf", append: false)!)
        pdf.setTitle("Using TextColumn and Paragraph classes")
        pdf.setSubject("Examples")
        pdf.setAuthor("Innovatics Inc.")

        let image1 = try Image(pdf, "images/sz-map.png")

        let f1 = Font(pdf, CoreFont.HELVETICA)
        let f2 = Font(pdf, CoreFont.HELVETICA_BOLD)
        let f3 = Font(pdf, CoreFont.HELVETICA_BOLD)
        let f4 = Font(pdf, CoreFont.HELVETICA_OBLIQUE)

        f1.setSize(10.0)
        f2.setSize(14.0)
        f3.setSize(12.0)
        f4.setSize(10.0)

        let page = Page(pdf, Letter.PORTRAIT)

        image1.setLocation(90.0, 35.0)
        image1.scaleBy(0.75)
        image1.drawOn(page)

        let rotate = ClockWise._0_degrees
        let column = TextColumn(rotate)
        column.setSpaceBetweenLines(5.0)
        column.setSpaceBetweenParagraphs(10.0)

        let p1 = Paragraph()
        p1.setAlignment(Align.CENTER)
        p1.add(TextLine(f2, "Switzerland"))

        let p2 = Paragraph()
        p2.add(TextLine(f2, "Introduction"))

        var buf = String()
        buf.append("The Swiss Confederation was founded in 1291 as a defensive ")
        buf.append("alliance among three cantons. In succeeding years, other ")
        buf.append("localities joined the original three. ")
        buf.append("The Swiss Confederation secured its independence from the ")
        buf.append("Holy Roman Empire in 1499. Switzerland's sovereignty and ")
        buf.append("neutrality have long been honored by the major European ")
        buf.append("powers, and the country was not involved in either of the ")
        buf.append("two World Wars. The political and economic integration of ")
        buf.append("Europe over the past half century, as well as Switzerland's ")
        buf.append("role in many UN and international organizations, has ")
        buf.append("strengthened Switzerland's ties with its neighbors. ")
        buf.append("However, the country did not officially become a UN member ")
        buf.append("until 2002.")

        let p3 = Paragraph()
        // p3.setAlignment(Align.LEFT)
        // p3.setAlignment(Align.RIGHT)
        p3.setAlignment(Align.JUSTIFY)
        var text = TextLine(f1, buf)
        p3.add(text)

        buf = String()
        buf.append("Switzerland remains active in many UN and international ")
        buf.append("organizations but retains a strong commitment to neutrality.")

        text = TextLine(f1, buf)
        text.setColor(Color.red)
        p3.add(text)

        let p4 = Paragraph()
        p4.add(TextLine(f3, "Economy"))

        buf = String()
        buf.append("Switzerland is a peaceful, prosperous, and stable modern ")
        buf.append("market economy with low unemployment, a highly skilled ")
        buf.append("labor force, and a per capita GDP larger than that of the ")
        buf.append("big Western European economies. The Swiss in recent years ")
        buf.append("have brought their economic practices largely into ")
        buf.append("conformity with the EU's to enhance their international ")
        buf.append("competitiveness. Switzerland remains a safehaven for ")
        buf.append("investors, because it has maintained a degree of bank secrecy ")
        buf.append("and has kept up the franc's long-term external value. ")
        buf.append("Reflecting the anemic economic conditions of Europe, GDP ")
        buf.append("growth stagnated during the 2001-03 period, improved during ")
        buf.append("2004-05 to 1.8% annually and to 2.9% in 2006.")

        let p5 = Paragraph()
        p5.setAlignment(Align.JUSTIFY)
        text = TextLine(f1, buf)
        p5.add(text)

        text = TextLine(f4,
                "Even so, unemployment has remained at less than half the EU average.")
        text.setColor(Color.blue)
        p5.add(text)

        let p6 = Paragraph()
        p6.setAlignment(Align.RIGHT)

        text = TextLine(f1, "Source: The world fact book.")
        text.setColor(Color.blue)

        text.setURIAction(
                "https://www.cia.gov/library/publications/the-world-factbook/geos/sz.html")

        p6.add(text)

        column.addParagraph(p1)
        column.addParagraph(p2)
        column.addParagraph(p3)
        column.addParagraph(p4)
        column.addParagraph(p5)
        column.addParagraph(p6)

        if rotate == ClockWise._0_degrees {
            column.setLocation(90.0, 300.0)
        } else if rotate == ClockWise._90_degrees {
            column.setLocation(90.0, 780.0)
        } else if rotate == ClockWise._270_degrees {
            column.setLocation(550.0, 310.0)
        }

        let columnWidth: Float = 470.0
        column.setSize(columnWidth, 100.0)
        let xy = column.drawOn(page)
        if rotate == ClockWise._0_degrees {
            Line(
                    xy[0],
                    xy[1],
                    xy[0] + columnWidth,
                    xy[1]).drawOn(page)
        }

        pdf.complete()
    }
}   // End of Example_10.swift

let time0 = Int64(Date().timeIntervalSince1970 * 1000)
_ = try Example_10()
let time1 = Int64(Date().timeIntervalSince1970 * 1000)
TextUtils.printDuration("Example_10", time0, time1)
