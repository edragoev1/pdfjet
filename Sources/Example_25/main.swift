import Foundation
import PDFjet


/**
 *  Example_25.swift
 *
 */
public class Example_25 {
    public init() throws {
        let stream = OutputStream(toFileAtPath: "Example_25.pdf", append: false)

        let pdf = PDF(stream!)

        let f1 = Font(pdf, CoreFont.HELVETICA)
        let f2 = Font(pdf, CoreFont.HELVETICA_BOLD)
        let f3 = Font(pdf, CoreFont.HELVETICA)
        let f4 = Font(pdf, CoreFont.HELVETICA_BOLD)
        let f5 = Font(pdf, CoreFont.HELVETICA)
        let f6 = Font(pdf, CoreFont.HELVETICA_BOLD)

        let page = Page(pdf, Letter.PORTRAIT)

        let composite = CompositeTextLine(50.0, 50.0)
        composite.setFontSize(14.0)

        var text1 = TextLine(f1, "C")
        var text2 = TextLine(f2, "6")
        var text3 = TextLine(f3, "H")
        let text4 = TextLine(f4, "12")
        let text5 = TextLine(f5, "O")
        let text6 = TextLine(f6, "6")

        text1.setColor(Color.dodgerblue)
        text3.setColor(Color.dodgerblue)
        text5.setColor(Color.dodgerblue)

        text2.setTextEffect(Effect.SUBSCRIPT)
        text4.setTextEffect(Effect.SUBSCRIPT)
        text6.setTextEffect(Effect.SUBSCRIPT)

        composite.addComponent(text1)
        composite.addComponent(text2)
        composite.addComponent(text3)
        composite.addComponent(text4)
        composite.addComponent(text5)
        composite.addComponent(text6)

        let xy = composite.drawOn(page)

        let box = Box()
        box.setLocation(xy[0], xy[1])
        box.setSize(20.0, 20.0)
        box.drawOn(page)

        let composite2 = CompositeTextLine(50.0, 100.0)
        composite2.setFontSize(14.0)

        text1 = TextLine(f1, "SO")
        text2 = TextLine(f2, "4")
        text3 = TextLine(f4, "2-")  // Use bold font here

        text2.setTextEffect(Effect.SUBSCRIPT)
        text3.setTextEffect(Effect.SUPERSCRIPT)

        composite2.addComponent(text1)
        composite2.addComponent(text2)
        composite2.addComponent(text3)

        composite2.drawOn(page)
        composite2.setLocation(100.0, 150.0)
        composite2.drawOn(page)

        let yy = composite2.getMinMax()
        let line1 = Line(50.0, yy[0], 200.0, yy[0])
        let line2 = Line(50.0, yy[1], 200.0, yy[1])
        line1.drawOn(page)
        line2.drawOn(page)

        let chart = DonutChart(f1, f2, false)
        chart.setLocation(300.0, 300.0)
        chart.setR1AndR2(200.0, 100.0)
        chart.addSlice(Slice(10.0, Color.red))
        chart.addSlice(Slice(20.0, Color.green))
        chart.addSlice(Slice(30.0, Color.blue))
        chart.addSlice(Slice(40.0, Color.peachpuff))
/* For testing!
        chart.addSlice(Slice(75.0, Color.red))
        chart.addSlice(Slice(25.0, Color.blue))
*/
        chart.drawOn(page)

        pdf.complete()
    }

}   // End of Example_25.swift

let time0 = Int64(Date().timeIntervalSince1970 * 1000)
_ = try Example_25()
let time1 = Int64(Date().timeIntervalSince1970 * 1000)
TextUtils.printDuration("Example_25", time0, time1)
