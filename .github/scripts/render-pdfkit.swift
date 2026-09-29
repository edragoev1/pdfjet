// Renders PDFs with Apple's PDFKit, the engine of Preview, Safari and iOS, for
// check-viewers.py. It runs on macOS only.
//
//     swiftc -O .github/scripts/render-pdfkit.swift -o render-pdfkit
//     ./render-pdfkit JOB.json
//
// JOB.json is what render-pdfjs.mjs reads, and the output is the same: every
// page of every file is drawn, as Preview draws it, with its annotations, on a
// white bitmap, and its text is extracted, with PDFPage.string. The first
// maxPages pages are written to FOLDER/NAME/page-NNN.png, and FOLDER/NAME.json
// gets {"pages": COUNT, "text": [TEXT OF EACH PAGE], "errors": [...]}. PDFKit
// reports no errors of its own: a PDF it cannot open, a password it does not
// accept and a page it cannot give or draw are the errors.

import Foundation

#if canImport(PDFKit)
import CoreGraphics
import ImageIO
import PDFKit

struct Job: Decodable {
    let dpi: Double
    let maxPages: Int
    let out: String
    let files: [File]
}

struct File: Decodable {
    let name: String
    let path: String
    let password: String?
}

struct Result: Encodable {
    var pages = 0
    var text: [String] = []
    var errors: [String] = []
}

func writePNG(_ image: CGImage, to url: URL) -> Bool {
    guard let destination = CGImageDestinationCreateWithURL(url as CFURL, "public.png" as CFString, 1, nil) else {
        return false
    }
    CGImageDestinationAddImage(destination, image, nil)
    return CGImageDestinationFinalize(destination)
}

// Draws a page at dpi on a white bitmap, or returns nil.
func draw(_ page: PDFPage, dpi: Double) -> CGImage? {
    let box = page.bounds(for: .cropBox)
    let scale = dpi / 72
    // A page turned by a quarter is drawn on its side.
    let turned = page.rotation % 180 != 0
    let width = Int(((turned ? box.height : box.width) * scale).rounded())
    let height = Int(((turned ? box.width : box.height) * scale).rounded())
    guard width > 0, height > 0, let context = CGContext(
        data: nil, width: width, height: height, bitsPerComponent: 8, bytesPerRow: 0,
        space: CGColorSpaceCreateDeviceRGB(), bitmapInfo: CGImageAlphaInfo.noneSkipLast.rawValue)
    else {
        return nil
    }
    context.setFillColor(CGColor(red: 1, green: 1, blue: 1, alpha: 1))
    context.fill(CGRect(x: 0, y: 0, width: width, height: height))
    context.scaleBy(x: scale, y: scale)
    // PDFKit places the crop box and turns the page as Preview does. No example
    // has a rotated page, or a crop box away from the origin, to show it.
    page.draw(with: .cropBox, to: context)
    return context.makeImage()
}

func render(_ file: File, dpi: Double, maxPages: Int, out: URL) throws {
    var result = Result()
    let folder = out.appendingPathComponent(file.name)
    if let doc = PDFDocument(url: URL(fileURLWithPath: file.path)) {
        if doc.isLocked && !doc.unlock(withPassword: file.password ?? "") {
            result.errors.append("cannot open: the password is not accepted")
        } else {
            result.pages = doc.pageCount
            try FileManager.default.createDirectory(at: folder, withIntermediateDirectories: true)
            for i in 0..<doc.pageCount {
                guard let page = doc.page(at: i) else {
                    result.errors.append("page \(i + 1): PDFKit gives no page")
                    result.text.append("")
                    continue
                }
                if let image = draw(page, dpi: dpi) {
                    let png = folder.appendingPathComponent(String(format: "page-%03d.png", i + 1))
                    if i < maxPages && !writePNG(image, to: png) {
                        result.errors.append("page \(i + 1): cannot write \(png.path)")
                    }
                } else {
                    result.errors.append("page \(i + 1): cannot be drawn")
                }
                result.text.append(page.string ?? "")
            }
        }
    } else {
        result.errors.append("cannot open")
    }
    try JSONEncoder().encode(result).write(to: out.appendingPathComponent(file.name + ".json"))
}

let job = try JSONDecoder().decode(Job.self, from: Data(contentsOf: URL(fileURLWithPath: CommandLine.arguments[1])))
let out = URL(fileURLWithPath: job.out)
try FileManager.default.createDirectory(at: out, withIntermediateDirectories: true)
for file in job.files {
    try autoreleasepool {
        try render(file, dpi: job.dpi, maxPages: job.maxPages, out: out)
    }
}
#else
FileHandle.standardError.write("render-pdfkit needs PDFKit, which is on macOS only.\n".data(using: .utf8)!)
exit(1)
#endif
