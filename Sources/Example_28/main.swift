/**
 * main.swift
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
import Foundation
import PDFjet

/**
 * Example_28.swift
 * This example reads fonts from OpenType and TrueType files. Any .otf or
 * .ttf file on the computer is a font for PDFjet. A TrueType font is
 * embedded as a subset of the glyphs the document draws, unless it is set
 * to stay whole; a font with CFF outlines is embedded whole.
 */
public class Example_28 {
    public init() throws {
        let pdf = PDF(OutputStream(toFileAtPath: "Example_28.pdf", append: false)!)
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
                "PDFjet reads OpenType and TrueType fonts as they are: pass the path "
                + "of any .otf or .ttf file on the computer to the Font constructor. "
                + "The IBMPlexSans and NotoSans constants are the paths of the fonts "
                + "that come with PDFjet. A TrueType font is embedded with only the "
                + "glyphs the document draws, which keeps the file small; a font with "
                + "CFF outlines is embedded whole. The paragraph below is drawn four times.")
        textBlock.setFontSize(12.0)
        textBlock.setLineSpacing(1.5)
        textBlock.setLocation(50.0, 95.0)
        textBlock.setWidth(512.0)
        var xy = textBlock.drawOn(page)

        let files = [
            "fonts/IBMPlexSans/IBMPlexSans-Regular.otf",
            IBMPlexSans.Regular,
            SourceSerif4.Regular,
            NotoSans.Regular,
        ]
        let kinds = [
            "OpenType with CFF outlines, from the .otf file, embedded whole",
            "TrueType, from the .ttf file, embedded as a subset",
            "Another TrueType font, embedded as a subset",
            "A TrueType font kept whole: subsetting turned off",
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
}   // End of Example_28.swift

let time0 = Int64(Date().timeIntervalSince1970 * 1000)
_ = try Example_28()
let time1 = Int64(Date().timeIntervalSince1970 * 1000)
print("Example_28 => \(String(format: "%4lld", time1 - time0)) ms")
