import Foundation
import PDFjet

// Table at the scale the TODO asks about: the 9 columns of the Electric
// Vehicle Population CSV built as Cell objects and drawn with Table on as
// many Letter pages as they need.
//
// The widths are narrow enough that some columns wrap, so the benchmark
// measures the wrapping as well as the drawing; the rows of the file are
// repeated until the table has the number of rows asked for.
//
// Usage: TableBench bench|cold <rows>
//        TableBench sample <rows> <file>
//
// Run from the root of the repository, as the paths are relative to it.

let csvFile = "data/Electric_Vehicle_Population_10_Pages.csv"
let semibold = "fonts/IBMPlexSans/IBMPlexSans-SemiBold.otf.stream"
let regular = "fonts/IBMPlexSans/IBMPlexSans-Regular.otf.stream"
let columns = 9
let widths: [Float] = [68.0, 58.0, 58.0, 26.0, 44.0, 34.0, 52.0, 60.0, 152.0]

// The first columns fields of every line of the CSV, the header first.
func readFields() throws -> [[String]] {
    let text = try String(contentsOfFile: csvFile, encoding: .utf8)
    var lines = [[String]]()
    for line in text.replacingOccurrences(of: "\r\n", with: "\n").components(separatedBy: "\n") {
        let fields = line.components(separatedBy: ",")
        if fields.count < columns {
            continue
        }
        lines.append(Array(fields[0..<columns]))
    }
    return lines
}

func tableData(_ fields: [[String]], _ rows: Int, _ f1: Font, _ f2: Font) -> [[Cell]] {
    var data = [[Cell]]()
    for r in 0...rows {
        // Row 0 is the header; the data rows repeat the file from line 1.
        let values = (r == 0) ? fields[0] : fields[1 + (r - 1) % (fields.count - 1)]
        var row = [Cell]()
        for c in 0..<columns {
            let cell = Cell((r == 0) ? f1 : f2, values[c])
            cell.setWidth(widths[c])
            cell.setTextAlignment(c == 4 ? Alignment.RIGHT : Alignment.LEFT)
            row.append(cell)
        }
        data.append(row)
    }
    return data
}

func document(_ fields: [[String]], _ rows: Int, _ stream: OutputStream) throws -> Int {
    let pdf = PDF(stream)
    let f1 = try Font(pdf, semibold)
    f1.setSize(8.0)
    let f2 = try Font(pdf, regular)
    f2.setSize(8.0)

    let table = Table()
    _ = table.setTableData(tableData(fields, rows, f1, f2), 1)
    _ = table.setLocation(20.0, 20.0)
    _ = table.setBottomMargin(20.0)

    var pages = [Page]()
    _ = table.drawOn(pdf, &pages, Letter.PORTRAIT)
    for page in pages {
        pdf.addPage(page)
    }
    try pdf.complete()
    return pages.count
}

func run(_ fields: [[String]], _ rows: Int) throws -> (Int, Int) {
    let stream = OutputStream(toMemory: ())
    let pages = try document(fields, rows, stream)
    let data = stream.property(forKey: .dataWrittenToMemoryStreamKey) as! Data
    return (pages, data.count)
}

func now() -> Int64 {
    return Int64(Date().timeIntervalSince1970 * 1000)
}

let start = now()
let mode = CommandLine.arguments[1]
let rows = Int(CommandLine.arguments[2])!
let fields = try readFields()
if mode == "cold" {
    let (pages, bytes) = try run(fields, rows)
    print("swift cold \(rows) rows: \(now() - start) ms, \(pages) pages, \(bytes) bytes")
} else if mode == "sample" {
    let stream = OutputStream(toMemory: ())
    _ = try document(fields, rows, stream)
    let data = stream.property(forKey: .dataWrittenToMemoryStreamKey) as! Data
    try data.write(to: URL(fileURLWithPath: CommandLine.arguments[3]))
} else {
    for _ in 0..<2 {
        _ = try run(fields, rows)
    }
    var ms = [Int64]()
    var pages = 0
    var size = 0
    for _ in 0..<7 {
        let t = now()
        (pages, size) = try run(fields, rows)
        ms.append(now() - t)
    }
    ms.sort()
    print("swift \(rows) rows: median \(ms[ms.count / 2]) ms (min \(ms[0]), max \(ms[ms.count - 1]), \(ms.count) runs), \(pages) pages, \(size) bytes")
}
