import Foundation
import PDFjet

// The text document of section 3 of pdfjet-benchmarks.html, written by PDFjet for
// Swift: pages of 60 lines of 10 point Latin, Greek and Cyrillic text in
// IBM Plex Sans, one drawing call per line.
//
// Usage: PortBench bench|cold <pages>
//        PortBench sample <pages> <file>
//
// Run from the root of the repository, as the font path is relative to it.

let fontPath = "fonts/IBMPlexSans/IBMPlexSans-Regular.otf.stream"
let lines = 60
let samples = [
    "The quick brown fox jumps over the lazy dog",
    "Ξεσκεπάζω την ψυχοφθόρα βδελυγμία",
    "Съешь же ещё этих мягких французских булок",
]

func document(_ pages: Int) throws -> Data {
    let stream = OutputStream(toMemory: ())
    let pdf = PDF(stream)
    let font = try Font(pdf, fontPath)
    for p in 0..<pages {
        let page = Page(pdf, Letter.PORTRAIT)
        for l in 0..<lines {
            page.drawString(font, nil, 10.0,
                    samples[l % 3] + " " + String(p) + "." + String(l),
                    50.0, 50.0 + Float(l) * 12.0)
        }
    }
    try pdf.complete()
    return stream.property(forKey: .dataWrittenToMemoryStreamKey) as! Data
}

func now() -> Int64 {
    return Int64(Date().timeIntervalSince1970 * 1000)
}

let start = now()
let mode = CommandLine.arguments[1]
let pages = Int(CommandLine.arguments[2])!
if mode == "cold" {
    let pdf = try document(pages)
    print("swift cold \(pages) pages: \(now() - start) ms, \(pdf.count) bytes")
} else if mode == "sample" {
    try document(pages).write(to: URL(fileURLWithPath: CommandLine.arguments[3]))
} else {
    for _ in 0..<2 {
        _ = try document(pages)
    }
    var ms = [Int64]()
    var size = 0
    for _ in 0..<7 {
        let t = now()
        size = try document(pages).count
        ms.append(now() - t)
    }
    ms.sort()
    print("swift \(pages) pages: median \(ms[ms.count / 2]) ms (min \(ms[0]), max \(ms[ms.count - 1]), \(ms.count) runs), \(size) bytes")
}
