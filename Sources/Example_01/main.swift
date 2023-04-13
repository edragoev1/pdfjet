import Foundation
import PDFjet

/**
 *  Example_01.swift
 */
public class Example_01 {
    public init() throws {
        let stream = OutputStream(toFileAtPath: "Example_01.pdf", append: false)
        let pdf = PDF(stream!)

        let f1 = try Font(pdf, "fonts/Droid/DroidSans.ttf.stream")
        let f2 = try Font(pdf, "fonts/Droid/DroidSansFallback.ttf")

        f1.setSize(12.0)
        f2.setSize(12.0)

        var page = Page(pdf, Letter.PORTRAIT)

        var textLine = TextLine(f1, "Happy New Year!")
        textLine.setLocation(70.0, 70.0)
        textLine.drawOn(page)

        textLine = TextLine(f1, "С Новым Годом!")
        textLine.setLocation(70.0, 100.0)
        textLine.drawOn(page)

        textLine = TextLine(f1, "Ευτυχισμένο το Νέο Έτος!")
        textLine.setLocation(70.0, 130.0)
        textLine.drawOn(page)

        textLine = TextLine(f1, "新年快樂！")
        textLine.setFallbackFont(f2)
        textLine.setLocation(300.0, 70.0)
        textLine.drawOn(page)

        textLine = TextLine(f1, "新年快乐！")
        textLine.setFallbackFont(f2)
        textLine.setLocation(300.0, 100.0)
        textLine.drawOn(page)

        textLine = TextLine(f1, "明けましておめでとうございます！")
        textLine.setFallbackFont(f2)
        textLine.setLocation(300.0, 130.0)
        textLine.drawOn(page)

        textLine = TextLine(f1, "새해 복 많이 받으세요!")
        textLine.setFallbackFont(f2)
        textLine.setLocation(300.0, 160.0)
        textLine.drawOn(page)

        page = Page(pdf, Letter.PORTRAIT)
        var paragraphs = [Paragraph]()

        var str = try String(contentsOfFile: "data/LCG.txt", encoding: .utf8)
        var lines = str.split(separator: "\n")
        var i = 0
        for line in lines {
            let paragraph = Paragraph()
            paragraph.add(TextLine(f1, String(line)))
            paragraphs.append(paragraph)
		    if (i == 0) {
                var textLine2 = TextLine(f1,
                        "Hello, World! This is a test to check if this line will be wrapped around properly.")
                textLine2.setColor(Color.blue)
                textLine2.setUnderline(true)
			    paragraph.add(textLine2)

                textLine2 = TextLine(f1, "This is a test!")
                textLine2.setColor(Color.oldgloryred)
                textLine2.setUnderline(true)
			    paragraph.add(textLine2)
		    }
            i += 1
        }

        var text = Text(paragraphs)
        text.setLocation(50.0, 50.0)
        text.setWidth(500.0)
        let xy = text.drawOn(page)

        let points = text.getBeginParagraphPoints()
        var n = 0
	    for point in points {
		    let textLine = TextLine(f1, String(n+1)+".")
		    textLine.setLocation(point[0]-20.0, point[1])
		    textLine.drawOn(page)
            n += 1
	    }

        let box = Box()
        box.setLocation(xy[0], xy[1])
        box.setSize(20.0, 20.0)
        box.drawOn(page)

        page = Page(pdf, Letter.PORTRAIT)

        paragraphs = [Paragraph]()

        str = try String(contentsOfFile: "data/CJK.txt", encoding: .utf8)
        lines = str.split(separator: "\n")
        for line in lines {
            if line == "" {
                continue
            }
            let paragraph = Paragraph()
            let textLine = TextLine(f2, String(line))
            textLine.setFallbackFont(f1)
            paragraph.add(textLine)
            paragraphs.append(paragraph)
        }
        text = Text(paragraphs)
        text.setLocation(50.0, 50.0)
        text.setWidth(500.0)
        text.drawOn(page)

        pdf.complete()
    }

}   // End of Example_01.swift

let time0 = Int64(Date().timeIntervalSince1970 * 1000)
_ = try Example_01()
let time1 = Int64(Date().timeIntervalSince1970 * 1000)
print("Example_01 => \(time1 - time0)")
