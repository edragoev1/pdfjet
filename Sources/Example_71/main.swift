import Foundation
import PDFjet

///
/// Example_71.swift
///
public class Example_71 {

    public init() throws {

        if let stream = OutputStream(toFileAtPath: "Example_71.pdf", append: false) {
            let pdf = PDF(stream)

            let f1 = try Font(
                    pdf,
                    InputStream(fileAtPath: "fonts/Droid/DroidSerif-Bold.ttf.stream")!,
		            // InputStream(fileAtPath: "fonts/Noto/NotoSans-Bold.ttf.stream")!,
                    Font.STREAM)
            f1.setSize(12.0)

            let f2 = try Font(
                    pdf,
                    InputStream(fileAtPath: "fonts/Droid/DroidSerif-Italic.ttf.stream")!,
		            // InputStream(fileAtPath: "fonts/Noto/NotoSans-Italic.ttf.stream")!,
                    Font.STREAM)
            f2.setSize(12.0)

            let page = Page(pdf, Letter.PORTRAIT)

            let calendar = CalendarMonth(f1, f2, 2018, 9)
	        calendar.setLocation(0.0, 0.0); 
            let point = calendar.drawOn(page)

	        let calendar2 = CalendarMonth(f1, f2, 2018, 10);
            calendar2.setLocation(0.0, point[1]); 
            calendar2.drawOn(page);
    
            pdf.complete()
        }
    }

}   // End of Example_71.swift

let time0 = Int64(Date().timeIntervalSince1970 * 1000)
_ = try Example_71()
let time1 = Int64(Date().timeIntervalSince1970 * 1000)
print("Example_71 => \(time1 - time0)")
