/**
 * main.swift
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
import Foundation
import PDFjet

/**
 * Example_07.swift
 * This example draws in fonts read from their files. PDFjet works with any
 * TrueType (.ttf) or OpenType (.otf) font file: pass the path of the file to
 * the Font constructor. The fonts PDFjet carries are .otf files; the third
 * font here is a .ttf on purpose, IBM's TrueType edition of IBM Plex Sans in
 * examples/fonts, to show a .ttf at work. A font is embedded as a subset of
 * the glyphs the document draws, unless it is set to stay whole.
 */
public class Example_07 {
    public init() throws {
        let pdf = PDF(OutputStream(toFileAtPath: "Example_07.pdf", append: false)!)
        pdf.setCompliance(Compliance.PDF_UA_1)
        pdf.setTitle("Fonts from .otf and .ttf Files")

        let f1 = try Font(pdf, IBMPlexSans.Regular)
        let f2 = try Font(pdf, IBMPlexSans.SemiBold)

        let page = Page(pdf, Letter.PORTRAIT)

        var text = TextLine(f2, "Fonts from .otf and .ttf Files")
        text.setStructureType(StructElem.H1)
        text.setFontSize(22.0)
        text.setLocation(50.0, 80.0)
        text.drawOn(page)

        var textBlock = TextBlock(f1,
                "PDFjet works with any TrueType (.ttf) or OpenType (.otf) font file: pass "
                + "the path of the file to the Font constructor. The fonts that come with "
                + "PDFjet, the font constants such as IBMPlexSans and IBMPlexSansJP, are "
                + ".otf files. The paragraph below is drawn four times; the third is from a "
                + ".ttf file on purpose: IBM's TrueType edition of IBM Plex Sans, which the "
                + "examples have in examples/fonts, the same design as the second. A font is "
                + "embedded with only the glyphs the document draws, which keeps the file small.")
        textBlock.setFontSize(12.0)
        textBlock.setLineSpacing(1.5)
        textBlock.setLocation(50.0, 95.0)
        textBlock.setWidth(512.0)
        var xy = textBlock.drawOn(page)

        let files = [
            IBMPlexSansJP.Regular,
            IBMPlexSans.Regular,
            "examples/fonts/IBMPlexSans/IBMPlexSans-Regular.ttf",
            IBMPlexSerif.Regular,
        ]
        let kinds = [
            "OpenType with CFF outlines, keyed by CIDs, from the .otf file, embedded as a subset",
            "OpenType with CFF outlines, from the .otf file, embedded as a subset",
            "TrueType, IBM Plex Sans again, from IBM's .ttf file, embedded as a subset",
            "A font kept whole: subsetting turned off",
        ]
        let sample = "The quick brown fox jumps over the lazy dog. "
                + "Ξεσκεπάζω την ψυχοφθόρα βδελυγμία. "
                + "Съешь же ещё этих мягких французских булок, да выпей чаю."

        var y: Float = xy[1] + 30.0
        for i in 0..<files.count {
            text = TextLine(f2, files[i])
            text.setFontSize(11.0)
            text.setLocation(50.0, y)
            text.drawOn(page)

            text = TextLine(f1, kinds[i])
            text.setFontSize(10.0)
            text.setTextColor(Color.dimgray)
            text.setLocation(50.0, y + 15.0)
            text.drawOn(page)

            let font = try Font(pdf, files[i])
            font.setSubset(i != 3)
            textBlock = TextBlock(font, sample)
            textBlock.setFontSize(13.0)
            textBlock.setLineSpacing(1.4)
            textBlock.setLocation(50.0, y + 28.0)
            textBlock.setWidth(512.0)
            xy = textBlock.drawOn(page)
            y = xy[1] + 30.0
        }

        try pdf.complete()
    }
}   // End of Example_07.swift

let time0 = Int64(Date().timeIntervalSince1970 * 1000)
_ = try Example_07()
let time1 = Int64(Date().timeIntervalSince1970 * 1000)
print("Example_07 => \(String(format: "%4lld", time1 - time0)) ms")
