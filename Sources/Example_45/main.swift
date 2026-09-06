import Foundation
import PDFjet

/**
 * Example_45.swift
 */
public class Example_45 {
    public init() {
        let pdf = PDF(OutputStream(toFileAtPath: "Example_45.pdf", append: false)!)

        let f1 = Font(pdf, CoreFont.HELVETICA_BOLD)
        let f2 = Font(pdf, CoreFont.HELVETICA)

        let page = Page(pdf, Letter.PORTRAIT)

        let w: Float = 500.0

        var fields = [Field]()
        fields.append(Field(  0.0, "Company", "Smart Widgets Construction Inc."))
        fields.append(Field(  0.0, "Street Number", "120"))
        fields.append(Field(  w/8, "Street Name", "Oak"))
        fields.append(Field(5*w/8, "Street Type", "Street"))
        fields.append(Field(6*w/8, "Direction", "West"))
        fields.append(Field(7*w/8, "Suite/Floor/Apt.", "8W"))
        fields.append(Field(  0.0, "City/Town", "Toronto"))
        fields.append(Field(  w/2, "Province", "Ontario"))
        fields.append(Field(7*w/8, "Postal Code", "M5M 2N2"))
        fields.append(Field(  0.0, "Telephone Number", "(416) 331-2245"))
        fields.append(Field(  w/4, "Fax (if applicable)", "(416) 124-9879"))
        fields.append(Field(  w/2, "Email","jsmith12345@gmail.ca"))
        fields.append(Field(  0.0, "Other Information", "Hello, World!"))
        fields.append(Field(  0.0, "", "This is a test."))

        Form(fields)
                .setLabelFont(f1)
                .setLabelFontSize(8.0)
                .setValueFont(f2)
                .setValueFontSize(10.0)
                .setLocation(50.0, 50.0)
                .setFormWidth(w)
                .drawOn(page)

        pdf.complete()
    }
}   // End of Example_45.swift

let time0 = Int64(Date().timeIntervalSince1970 * 1000)
_ = Example_45()
let time1 = Int64(Date().timeIntervalSince1970 * 1000)
TextUtils.printDuration("Example_45", time0, time1)
