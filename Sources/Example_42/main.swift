import Foundation
import PDFjet


/**
 *  Example_42.swift
 *
 */
public class Example_42 {

    public init() {

        if let stream = OutputStream(toFileAtPath: "Example_42.pdf", append: false) {

            let pdf = PDF(stream)

            let f1 = Font(pdf, CoreFont.HELVETICA_BOLD)
            let f2 = Font(pdf, CoreFont.HELVETICA)

            let page = Page(pdf, Letter.PORTRAIT)

            let w: Float = 500.0
            let h: Float = 13.0

            var fields = [Field]()
            fields.append(Field(   0.0, ["Company", "Smart Widgets Construction Inc."]))
            fields.append(Field(   0.0, ["Street Number", "120"]))
            fields.append(Field(   w/8, ["Street Name", "Oak"]))
            fields.append(Field( 5*w/8, ["Street Type", "Street"]))
            fields.append(Field( 6*w/8, ["Direction", "West"]))
            fields.append(Field( 7*w/8, ["Suite/Floor/Apt.", "8W"]))
            fields.append(Field(   0.0, ["City/Town", "Toronto"]))
            fields.append(Field(   w/2, ["Province", "Ontario"]))
            fields.append(Field( 7*w/8, ["Postal Code", "M5M 2N2"]))
            fields.append(Field(   0.0, ["Telephone Number", "(416) 331-2245"]))
            fields.append(Field(   w/4, ["Fax (if applicable)", "(416) 124-9879"]))
            fields.append(Field(   w/2, ["Email","jsmith12345@gmail.ca"]))
            fields.append(Field(   0.0, ["Other Information", "", ""]))

            Form(fields)
                    .setLabelFont(f1)
                    .setLabelFontSize(8.0)
                    .setValueFont(f2)
                    .setValueFontSize(10.0)
                    .setLocation(70.0, 90.0)
                    .setRowLength(w)
                    .setRowHeight(h)
                    .drawOn(page)
/*
            var box = Box()
            box.setLocation(xy[0], xy[1])
            box.setSize(20.0, 20.0)
            box.drawOn(page)
*/
            pdf.complete()
        }
    }

}   // End of Example_42.swift

let time0 = Int64(Date().timeIntervalSince1970 * 1000)
_ = Example_42()
let time1 = Int64(Date().timeIntervalSince1970 * 1000)
print("Example_42 => \(time1 - time0)")
