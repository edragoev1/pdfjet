import Foundation
import PDFjet

///
/// Example_28.swift shows how to use the NotoSansSymbols font.
///
public class Example_28 {
    public init() throws {
        let stream = OutputStream(toFileAtPath: "Example_28.pdf", append: false)!
        let pdf = PDF(stream)

        let f1 = try Font(pdf, "fonts/NotoSansSymbols/NotoSansSymbols-Regular.ttf.stream")
        f1.setSize(28.0)

        let page = Page(pdf, Letter.LANDSCAPE)

        let x = Float(35.0)
        var y = Float(55.0)
        let dy = Float(35.0)

        drawLineOfText(page, f1, x, y, 0x0041, 0x005A);
        y += dy;

        drawLineOfText(page, f1, x, y, 0x0061, 0x007A);
        y += dy;

        drawLineOfText(page, f1, x, y, 0x24B6, 0x24CF);
        y += dy;

        drawLineOfText(page, f1, x, y, 0x24D0, 0x24E9);
        y += dy;

        drawLineOfText(page, f1, x, y, 0x24F5, 0x24FE);
        y += dy;

        drawLineOfText(page, f1, x, y, 0x2624, 0x262F);
        y += dy;

        drawLineOfText(page, f1, x, y, 0x2638, 0x2653);
        y += dy;

        drawLineOfText(page, f1, x, y, 0x2669, 0x267E);
        y += dy;

        drawLineOfText(page, f1, x, y, 0x2690, 0x26A9);
        y += dy;

        drawLineOfText(page, f1, x, y, 0x26AD, 0x26BC);
        y += dy;

        drawLineOfText(page, f1, x, y, 0x26E2, 0x26FE);
        y += dy;

        pdf.complete()
    }

    private func drawLineOfText(
            _ page: Page, _ f1: Font, _ x: Float, _ y: Float, _ c1: Int, _ c2: Int) {
        var buf = String()
        var i = c1
        while i <= c2 {
            buf.append(Character(UnicodeScalar(i)!))
            i += 1
        }
        let text = TextLine(f1)
        text.setText(buf)
        text.setLocation(x, y)
        text.drawOn(page)
    }
}   // End of Example_28.swift

let time0 = Int64(Date().timeIntervalSince1970 * 1000)
_ = try Example_28()
let time1 = Int64(Date().timeIntervalSince1970 * 1000)
TextUtils.printDuration("Example_28", time0, time1)
