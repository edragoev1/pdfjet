import Foundation
import PDFjet


/**
 *  Example_45.swift
 *
 */
public class Example_45 {

    public init() throws {

        let stream = OutputStream(toFileAtPath: "Example_45.pdf", append: false)
        let pdf = PDF(stream!)
        pdf.setLanguage("en-US")
        pdf.setTitle("Hello, World!")

        let f1 = try Font(
                pdf,
                InputStream(fileAtPath: "fonts/Droid/DroidSerif-Regular.ttf.stream")!,
                Font.STREAM)
        f1.setSize(14.0)

        let f2 = try Font(
                pdf,
                InputStream(fileAtPath: "fonts/Droid/DroidSerif-Italic.ttf.stream")!,
                Font.STREAM)
        f2.setSize(14.0)

        var page = Page(pdf, Letter.PORTRAIT)

        var text = TextLine(f1)
        text.setLocation(70.0, 70.0)
        text.setText("Hasta la vista!")
        text.setLanguage("es-MX")
        text.setStrikeout(true)
        text.setUnderline(true)
        text.setURIAction("http://pdfjet.com")
        text.drawOn(page)

        text = TextLine(f1)
        text.setLocation(70.0, 90.0)
        text.setText("416-335-7718")
        text.setURIAction("http://pdfjet.com")
        text.drawOn(page)

        text = TextLine(f1)
        text.setLocation(70.0, 120.0)
        text.setText("2014-11-25")
        text.drawOn(page)

        var paragraphs = [Paragraph]()

        let paragraph = Paragraph()
                .add(TextLine(f1,
"The centres also offer free one-on-one consultations with business advisors who can review your business plan and make recommendations to improve it. The small business centres offer practical resources, from step-by-step info on setting up your business to sample business plans to a range of business-related articles and books in our resource libraries."))
                .add(TextLine(f2,
"This text is blue color and is written using italic font.")
                        .setColor(Color.blue))

        paragraphs.append(paragraph)

        let textArea = Text(paragraphs)
        textArea.setLocation(70.0, 150.0)
        textArea.setWidth(500.0)
        textArea.drawOn(page)

        let xy = PlainText(f2, [
"The Fibonacci sequence is named after Fibonacci.",
"His 1202 book Liber Abaci introduced the sequence to Western European mathematics,",
"although the sequence had been described earlier in Indian mathematics.",
"By modern convention, the sequence begins either with F0 = 0 or with F1 = 1.",
"The Liber Abaci began the sequence with F1 = 1, without an initial 0.",
"",
"Fibonacci numbers are closely related to Lucas numbers in that they are a complementary pair",
"of Lucas sequences. They are intimately connected with the golden ratio;",
"for example, the closest rational approximations to the ratio are 2/1, 3/2, 5/3, 8/5, ... .",
"Applications include computer algorithms such as the Fibonacci search technique and the",
"Fibonacci heap data structure, and graphs called Fibonacci cubes used for interconnecting",
"parallel and distributed systems. They also appear in biological settings, such as branching",
"in trees, phyllotaxis (the arrangement of leaves on a stem), the fruit sprouts of a pineapple,",
"the flowering of an artichoke, an uncurling fern and the arrangement of a pine cone.",
                ])
                .setLocation(70.0, 370.0)
                .setFontSize(10.0)
                .drawOn(page)

        var box = Box()
        box.setLocation(xy[0], xy[1])
        box.setSize(20.0, 20.0)
        box.drawOn(page)

        page = Page(pdf, Letter.PORTRAIT)

        text = TextLine(f1)
        text.setLocation(70.0, 120.0)
        text.setText("416-877-1395")
        text.drawOn(page)

        let line = Line(70.0, 150.0, 300.0, 150.0)
        line.setWidth(1.0)
        line.setColor(Color.oldgloryred)
        line.setAltDescription("This is a red line.")
        line.setActualText("This is a red line.")
        line.drawOn(page)

        box = Box()
        box.setLineWidth(1.0)
        box.setLocation(70.0, 200.0)
        box.setSize(100.0, 100.0)
        box.setColor(Color.oldgloryblue)
        box.setAltDescription("This is a blue box.")
        box.setActualText("This is a blue box.")
        box.drawOn(page)

        page.addBMC("Span", "This is a test", "This is a test")
        page.drawString(f1, "This is a test", 75.0, 230.0)
        page.addEMC()

        let image = try Image(
                pdf,
                InputStream(fileAtPath: "images/fruit.jpg")!,
                ImageType.JPG)
        image.setLocation(70.0, 310.0)
        image.scaleBy(0.5)
        image.setAltDescription("This is an image of a strawberry.")
        image.setActualText("This is an image of a strawberry.")
        image.drawOn(page)

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
        form.setLocation(70.0, 490.0)
        form.setRowLength(w)
        form.setRowHeight(h)
        form.drawOn(page)

        pdf.complete()
    }

}   // End of Example_45.swift

let time0 = Int64(Date().timeIntervalSince1970 * 1000)
_ = try Example_45()
let time1 = Int64(Date().timeIntervalSince1970 * 1000)
print("Example_45 => \(time1 - time0)")
