/**
 * main.swift
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
import Foundation
import PDFjet

/**
 * Example_45.swift
 * Using the Form and Field classes to draw a shipment request. A field at x = 0
 * starts a new row, the other fields of the row start at their own x, and a
 * field with an empty label continues the value above it on a new line.
 */
public class Example_45 {
    public init() throws {
        let pdf = PDF(OutputStream(toFileAtPath: "Example_45.pdf", append: false)!)
        pdf.setCompliance(Compliance.PDF_UA_1)
        pdf.setTitle("Shipment Request")

        let f1 = try Font(pdf, IBMPlexSans.Regular)
        let f2 = try Font(pdf, IBMPlexSans.SemiBold)

        let page = Page(pdf, Letter.PORTRAIT)

        var text = TextLine(f2, "Shipment Request")
        text.setFontSize(22.0)
        text.setLocation(56.0, 80.0)
        text.drawOn(page)

        let textBlock = TextBlock(f1,
                "A Form draws its fields in rows. A field at x = 0 starts a new row, "
                + "the other fields of the row start at their own x, and a field with "
                + "an empty label continues the value above it on a new line.")
        textBlock.setFontSize(12.0)
        textBlock.setLineSpacing(1.5)
        textBlock.setLocation(56.0, 95.0)
        textBlock.setWidth(500.0)
        var xy = textBlock.drawOn(page)

        let w: Float = 500.0    // The width of the form

        var fields = [Field]()
        fields.append(Field(  0.0, "Sender", "Maple Leaf Instruments Ltd."))
        fields.append(Field(  0.0, "Street Address", "480 King Street West"))
        fields.append(Field(6*w/8, "Suite", "1200"))
        fields.append(Field(  0.0, "City", "Toronto"))
        fields.append(Field(3*w/8, "Province", "Ontario"))
        fields.append(Field(5*w/8, "Postal Code", "M5V 1L7"))
        fields.append(Field(6*w/8, "Country", "Canada"))
        fields.append(Field(  0.0, "Recipient", "Nordic Sensor Labs AB"))
        fields.append(Field(  0.0, "Street Address", "Drottninggatan 55"))
        fields.append(Field(6*w/8, "Floor", "3"))
        fields.append(Field(  0.0, "City", "Stockholm"))
        fields.append(Field(5*w/8, "Postal Code", "111 21"))
        fields.append(Field(6*w/8, "Country", "Sweden"))
        fields.append(Field(  0.0, "Contact", "Anna Lindqvist"))
        fields.append(Field(3*w/8, "Email", "anna.lindqvist@example.com"))
        fields.append(Field(  0.0, "Contents", "Two calibrated pressure sensors"))
        fields.append(Field(5*w/8, "Weight", "3.2 kg"))
        fields.append(Field(6*w/8, "Declared Value", "CAD 1,450.00"))
        fields.append(Field(  0.0, "Instructions",
                "Keep upright and away from magnets. Deliver on a weekday"))
        fields.append(Field(  0.0, "", "between 9:00 and 17:00, to the reception on the third floor."))

        xy = Form(fields)
                .setLabelFont(f1)
                .setLabelFontSize(8.0)
                .setLabelColor(Color.gray)
                .setValueFont(f2)
                .setValueFontSize(10.0)
                .setValueColor(Color.black)
                .setLocation(56.0, xy[1] + 20.0)
                .setWidth(w)
                .setStrokeWidth(0.5)
                .drawOn(page)

        text = TextLine(f1, "The recipient signs for the package on delivery.")
        text.setFontSize(10.0)
        text.setTextColor(Color.gray)
        text.setLocation(56.0, xy[1] + 20.0)
        text.drawOn(page)

        try pdf.complete()
    }
}   // End of Example_45.swift

let time0 = Int64(Date().timeIntervalSince1970 * 1000)
_ = try Example_45()
let time1 = Int64(Date().timeIntervalSince1970 * 1000)
print("Example_45 => \(String(format: "%4lld", time1 - time0)) ms")
