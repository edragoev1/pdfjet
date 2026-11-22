/**
 * HBShape.swift
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
#if os(macOS) || os(Linux)
import Foundation

///
/// A Shaper for TextBlock and TextLine that runs hb-shape, the program of HarfBuzz, the
/// text engine of Chrome and Android: the same shaping on every server, with
/// nothing linked into the program, for Swift on Linux, which has no text
/// engine of its own, and on a Mac for output the same as a Linux server's
/// (Core Text is the Mac's own, CoreTextShaper). Lines break at spaces and, in
/// Thai, Lao, Khmer and Burmese, between words, by ICU's dictionaries of the
/// world fonts. hb-shape is installed, HarfBuzz 15 or newer: on Ubuntu and
/// Debian `apt install libharfbuzz-bin`, on Fedora and RHEL
/// `dnf install harfbuzz-utils`, on macOS and Linux `brew install harfbuzz`.
/// It talks with hb-shape through files, not pipes: the text of each font in
/// a temporary file, the glyphs back in another.
///
///     TextBlock(hindi, text).setFallbackFont(latin).setShaper(HBShape())
///
public final class HBShape: Shaper, Sendable {
    private let program: String

    /// An HBShape that runs hb-shape of the PATH, or of the path given.
    public init(_ program: String = "hb-shape") {
        self.program = program
    }

    // Runs hb-shape with the arguments, its output and its errors to files,
    // and waits for it to end: what it wrote to its output
    private func run(_ arguments: [String]) throws -> String {
        let dir = FileManager.default.temporaryDirectory
        let out = dir.appendingPathComponent("pdfjet-hbshape-\(UUID().uuidString).out")
        let err = dir.appendingPathComponent("pdfjet-hbshape-\(UUID().uuidString).err")
        _ = FileManager.default.createFile(atPath: out.path, contents: nil)
        _ = FileManager.default.createFile(atPath: err.path, contents: nil)
        defer {
            try? FileManager.default.removeItem(at: out)
            try? FileManager.default.removeItem(at: err)
        }
        let process = Process()
        if program.contains("/") {
            process.executableURL = URL(fileURLWithPath: program)
            process.arguments = arguments
        } else {    // Looked for on the PATH
            process.executableURL = URL(fileURLWithPath: "/usr/bin/env")
            process.arguments = [program] + arguments
        }
        let outHandle = try FileHandle(forWritingTo: out)
        let errHandle = try FileHandle(forWritingTo: err)
        process.standardOutput = outHandle
        process.standardError = errHandle
        do {
            try process.run()
        } catch {
            throw PDFjetError(message: "HBShape could not run hb-shape, the program of HarfBuzz: "
                    + "install it (Ubuntu: apt install libharfbuzz-bin; macOS: brew install harfbuzz), "
                    + "or give its path to HBShape. \(error)")
        }
        process.waitUntilExit()
        if process.terminationStatus != 0 {
            let message = (try? String(contentsOf: err, encoding: .utf8))?
                    .trimmingCharacters(in: .whitespacesAndNewlines) ?? ""
            if process.terminationStatus == 127 {   // env found no such program
                throw PDFjetError(message: "HBShape runs HarfBuzz's program hb-shape, which is not "
                        + "installed: install it (Ubuntu: apt install libharfbuzz-bin; macOS: brew install harfbuzz), "
                        + "its path given to HBShape, or give TextBlock a Shaper of your own with setShaper.")
            }
            throw PDFjetError(message: "hb-shape: exit \(process.terminationStatus)"
                    + (message.isEmpty ? "" : ": " + message))
        }
        return (try? String(contentsOf: out, encoding: .utf8)) ?? ""
    }

    /// What hb-shape --version prints first, such as "hb-shape (HarfBuzz) 15.2.0".
    public func getVersion() throws -> String {
        let text = try run(["--version"])
        return (text.split(separator: "\n").first.map(String.init) ?? text)
                .trimmingCharacters(in: .whitespacesAndNewlines)
    }

    // A glyph shaped, in points from the start of the text
    private struct Glyph {
        var id: Int
        var font: Int
        var cluster: Int    // The index in the text, in code points, of its cluster's first character
        var x: Float
        var y: Float
        var advance: Float
    }

    // hb-shape's glyph, in font units
    private struct HBGlyph: Decodable {
        let g: Int
        let cl: Int
        let ax: Int
        let dx: Int?
        let dy: Int?
    }

    /// Shapes the text with hb-shape, each run of one font in that font, and
    /// breaks it into lines no wider than the width.
    public func shape(_ request: ShapingRequest) throws -> [ShapedLine] {
        let text = request.text
        if text.contains("\n") || text.contains("\r") {
            throw PDFjetError(message: "HBShape shapes a paragraph: a text without line feeds")
        }
        if request.fontFiles.isEmpty {
            throw PDFjetError(message: "HBShape needs a font file")
        }
        let fonts = try request.fontFiles.map { try OTF($0) }
        let cps = DictionaryBreaks.codePoints(text)
        // The index in the text, in UTF-16 code units, of each code point, and of the end
        var offsets = [Int](repeating: 0, count: cps.count + 1)
        for i in 0..<cps.count {
            offsets[i + 1] = offsets[i] + (cps[i] > 0xFFFF ? 2 : 1)
        }

        // Each character in the first font that has it, else in the first; a
        // mark in the font of the character it is on
        var fontOf = [Int](repeating: 0, count: cps.count)
        for i in 0..<cps.count {
            let c = cps[i]
            let category = Unicode.Scalar(UInt32(c))?.properties.generalCategory
            if i > 0, let category, [.nonspacingMark, .spacingMark, .enclosingMark].contains(category) {
                fontOf[i] = fontOf[i - 1]
                continue
            }
            for (k, otf) in fonts.enumerated() {
                let first = otf.firstChar ?? 0
                let last = otf.lastChar ?? 0x10FFFF
                if category == .control || (c >= first && c <= last && otf.unicodeToGID[c] != 0) {
                    fontOf[i] = k
                    break
                }
            }
        }
        // The runs of one font
        var runs = [(start: Int, end: Int)]()
        var start = 0
        while start < cps.count {
            var end = start + 1
            while end < cps.count && fontOf[end] == fontOf[start] {
                end += 1
            }
            runs.append((start, end))
            start = end
        }

        // Each font's runs shaped in one run of hb-shape, a run a line
        let dir = FileManager.default.temporaryDirectory.appendingPathComponent("pdfjet-hbshape-\(UUID().uuidString)")
        // The user's alone: other users of the machine do not read the text
        // being shaped (ultra review, 22 November 2026)
        try FileManager.default.createDirectory(at: dir, withIntermediateDirectories: true,
                attributes: [.posixPermissions: 0o700])
        defer {
            try? FileManager.default.removeItem(at: dir)
        }
        var shapedRuns = [Int: [HBGlyph]]()     // By the start of the run
        for k in 0..<fonts.count {
            let list = runs.filter { fontOf[$0.start] == k }
            if list.isEmpty {
                continue
            }
            var input = String.UnicodeScalarView()
            for run in list {
                for i in run.start..<run.end {
                    if let scalar = Unicode.Scalar(UInt32(cps[i])) {
                        input.append(scalar)
                    }
                }
                input.append("\n")
            }
            let font = dir.appendingPathComponent("font\(k)")
            let inFile = dir.appendingPathComponent("in\(k).txt")
            try Data(request.fontFiles[k]).write(to: font)
            try Data(String(input).utf8).write(to: inFile)
            // Left to right, as TextBlock draws, not guessed: an Arabic or
            // Hebrew run would come back right to left, its clusters falling
            // (ultra review, 22 November 2026)
            var arguments = ["--font-file=" + font.path, "--text-file=" + inFile.path,
                    "--no-glyph-names", "--output-format=json", "--direction=ltr"]
            if let language = request.language, HBShape.isLanguageTag(language) {
                arguments.append("--language=" + language)
            }
            let output = try run(arguments)
            let lines = output.split(separator: "\n").map { $0.replacingOccurrences(of: "\r", with: "") }
                    .filter { !$0.isEmpty }
            if lines.count != list.count {
                throw PDFjetError(message: "hb-shape gave \(lines.count) lines for \(list.count) runs of text")
            }
            for (i, line) in lines.enumerated() {
                shapedRuns[list[i].start] = try JSONDecoder().decode([HBGlyph].self, from: Data(line.utf8))
            }
        }

        // The glyphs of the runs in order, in points
        var glyphs = [Glyph]()
        var pen: Float = 0
        for run in runs {
            let k = fontOf[run.start]
            let scale = request.fontSize / Float(fonts[k].unitsPerEm ?? 1000)
            for g in shapedRuns[run.start] ?? [] {
                if run.start + g.cl >= run.end {
                    throw PDFjetError(message: "hb-shape gave a glyph of a character past its run")
                }
                glyphs.append(Glyph(id: g.g, font: k, cluster: run.start + g.cl,
                        x: pen + Float(g.dx ?? 0) * scale, y: -Float(g.dy ?? 0) * scale,
                        advance: Float(g.ax) * scale))
                pen += Float(g.ax) * scale
            }
        }
        return HBShape.lines(glyphs, cps, offsets, offsets[cps.count], request.width)
    }

    /// Whether the language is a BCP 47 tag of its letters, digits and
    /// hyphens, as hb-shape is given it: else none is given.
    static func isLanguageTag(_ language: String) -> Bool {
        if language.isEmpty || language.count > 35 {
            return false
        }
        return language.unicodeScalars.allSatisfy { c in
            (c >= "a" && c <= "z") || (c >= "A" && c <= "Z") || (c >= "0" && c <= "9") || c == "-"
        }
    }

    // The lines no wider than the width: broken after a space, or between two
    // words of Thai, Lao, Khmer or Burmese, the last place that fits; a word
    // wider than a line alone on its line
    private static func lines(_ glyphs: [Glyph], _ cps: [Int], _ offsets: [Int], _ end: Int,
            _ width: Float) -> [ShapedLine] {
        let wordEnd = Set(DictionaryBreaks.wordEnds(cps))
        var lines = [ShapedLine]()
        var first = 0
        while first < glyphs.count {
            let lineStart = glyphs[first].x
            var last = glyphs.count
            var lastBreak = -1
            for i in first..<glyphs.count {
                let g = glyphs[i]
                if i > first && g.cluster > 0 && glyphs[i - 1].cluster != g.cluster
                        && (cps[g.cluster - 1] == 0x20 || (wordEnd.contains(g.cluster) && cps[g.cluster] != 0x20)) {
                    lastBreak = i
                }
                if cps[g.cluster] != 0x20 && g.x + g.advance - lineStart > width && lastBreak > first {
                    last = lastBreak
                    break
                }
            }
            let lineEnd = (last < glyphs.count) ? offsets[glyphs[last].cluster] : end
            // The width of the line without the spaces at its end
            var visible = last
            while visible > first && cps[glyphs[visible - 1].cluster] == 0x20 {
                visible -= 1
            }
            var lineWidth: Float = 0
            if visible > first {
                lineWidth = glyphs[visible - 1].x + glyphs[visible - 1].advance - lineStart
            }
            var fontIndexes = [Int]()
            var ids = [Int]()
            var positions = [Float]()
            for g in glyphs[first..<last] {
                fontIndexes.append(g.font)
                ids.append(g.id)
                positions.append(g.x - lineStart)
                positions.append(g.y)
            }
            lines.append(ShapedLine(offsets[glyphs[first].cluster], lineEnd, fontIndexes, ids, positions, lineWidth))
            first = last
        }
        return lines
    }
}
#endif
