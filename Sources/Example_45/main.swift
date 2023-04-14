import Foundation
import PDFjet

/**
 *  Example_45.swift
 */
public class Example_45 {
    public init() throws {
        let stream = OutputStream(toFileAtPath: "Example_45.pdf", append: false)
        let pdf = PDF(stream!)
        pdf.setLanguage("en-US")
        pdf.setTitle("Hello, World!")

        let f1 = try Font(pdf, "fonts/Droid/DroidSerif-Regular.ttf.stream")
        let f2 = try Font(pdf, "fonts/Droid/DroidSerif-Italic.ttf.stream")
        let f3 = try Font(pdf, "fonts/SourceCodePro/SourceCodePro-Regular.ttf.stream")

        f1.setSize(14.0)
        f2.setSize(14.0)
        f3.setSize(10.0)

        let page = Page(pdf, Letter.PORTRAIT)

        let w: Float = 530.0
        let h: Float = 13.0

        var fields = [Field]()
        fields.append(Field(  0.0, ["Company", "Smart Widget Designs"]))
        fields.append(Field(  0.0, ["Street Number", "120"]))
        fields.append(Field(  w/8, ["Street Name", "Oak"]))
        fields.append(Field(5*w/8, ["Street Type", "Street"]))
        fields.append(Field(6*w/8, ["Direction", "West"]))
        fields.append(Field(7*w/8, ["Suite/Floor/Apt.", "8W"])
                .setAltDescription("Suite/Floor/Apartment")
                .setActualText("Suite/Floor/Apartment"))
        fields.append(Field(  0.0, ["City/Town", "Toronto"]))
        fields.append(Field(  w/2, ["Province", "Ontario"]))
        fields.append(Field(7*w/8, ["Postal Code", "M5M 2N2"]))
        fields.append(Field(  0.0, ["Telephone Number", "(416) 331-2245"]))
        fields.append(Field(  w/4, ["Fax (if applicable)", "(416) 124-9879"]))
        fields.append(Field(  w/2, ["Email","jsmith12345@gmail.ca"]))
        fields.append(Field(  0.0, [
                "Other Information","We don't work on weekends.", "Please send us an Email."]))

        let form = Form(fields)
        form.setLabelFont(f1)
        form.setLabelFontSize(7.0)
        form.setValueFont(f2)
        form.setValueFontSize(9.0)
        form.setLocation(50.0, 50.0)
        form.setRowLength(w)
        form.setRowHeight(h)
        form.drawOn(page)

        var colors = Dictionary<String, Int32>();
        colors["new"] = Color.red
        colors["ArrayList"] = Color.blue
        colors["List"] = Color.blue
        colors["String"] = Color.blue
        colors["Field"] = Color.blue
        colors["Form"] = Color.blue
        colors["Smart"] = Color.green
        colors["Widget"] = Color.green
        colors["Designs"] = Color.green

        let x: Float32 = 50.0
        var y: Float32 = 280.0
        let dy = f3.getBodyHeight()
        let lines = try Text.readLines("data/form-code-swift.txt")
        for line in lines {
            page.drawString(f3, line, x, y, Color.black, colors)
            y += dy
        }

        pdf.complete()
    }
}   // End of Example_45.swift

let time0 = Int64(Date().timeIntervalSince1970 * 1000)
_ = try Example_45()
let time1 = Int64(Date().timeIntervalSince1970 * 1000)
print("Example_45 => \(time1 - time0)")
