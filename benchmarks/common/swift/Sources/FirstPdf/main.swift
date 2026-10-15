// The first PDF of a process, by phase, as in the Java, C# and Go probes.
import Foundation
import PDFjet

func now() -> UInt64 { DispatchTime.now().uptimeNanoseconds }
func ms(_ a: UInt64, _ b: UInt64) -> String { String(format: "%.1f", Double(b - a) / 1e6) }

let root = CommandLine.arguments[1] + "/"
if CommandLine.arguments.count > 2 {
    let a = now()
    if CommandLine.arguments[2] == "date" {
        let f = DateFormatter()
        f.locale = Locale(identifier: "en_US_POSIX")
        f.calendar = Calendar(identifier: .gregorian)
        f.dateFormat = "yyyy-MM-dd'T'HH:mm:ss"
        f.timeZone = TimeZone(identifier: "UTC")
        _ = f.string(from: Date())
    } else {
        _ = UUID()
        var g = SystemRandomNumberGenerator()
        _ = (0..<16).map { _ in UInt8.random(in: 0...255, using: &g) }
    }
    print("\(CommandLine.arguments[2]) first use \(ms(a, now())) ms")
    exit(0)
}
let t0 = now()
let pdf = PDF(OutputStream(toMemory: ()))
let t1 = now()
let regular = try Font(pdf, root + "fonts/IBMPlexSans/IBMPlexSans-Regular.ttf")
let t2 = now()
let bold = try Font(pdf, root + "fonts/IBMPlexSans/IBMPlexSans-SemiBold.ttf")
let t3 = now()
let page = Page(pdf, Letter.PORTRAIT)
for i in 0..<40 {
    let line = TextLine(i % 5 == 0 ? bold : regular, "Line \(i) of the invoice, with some words on it")
    line.setLocation(50.0, Float(60 + 15 * i))
    line.drawOn(page)
}
let t4 = now()
try pdf.complete()
let t5 = now()
print("new PDF \(ms(t0, t1)), first font \(ms(t1, t2)), second \(ms(t2, t3)), page \(ms(t3, t4)), complete \(ms(t4, t5)), total \(ms(t0, t5)) ms")
