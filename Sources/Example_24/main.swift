/**
 * main.swift
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
import Foundation
import PDFjet

/**
 * Example_24.swift
 *
 * Draws the image formats PDFjet reads: a JPEG, a PNG and a BMP at their own
 * sizes, a CMYK JPEG whose inks are stored inverted, then a PNG of each color
 * type and bit depth the format has, the two ways a PNG carries transparency,
 * and a PNG that asks to be drawn at 300 dots per inch. The images of the grid
 * are from PngSuite, the test images of the PNG format, drawn at four times
 * their 32 by 32 pixels so their samples can be seen; the samples themselves
 * are checked in PNGImageTest, against the whole of PngSuite.
 */
public class Example_24 {
    // The images of the grid: the file, what it is, the description of it,
    // and whether it carries transparency, which is drawn over a color so it
    // can be seen.
    private let pngKinds = [
        ("BASN3P08", "Palette, 8 bits", "Vertical bands of red, orange, yellow, green, cyan, blue and magenta, each shading from black at the top to white at the bottom, from a palette of 256 colors.", "no"),
        ("BASN0G08", "Grayscale, 8 bits", "Horizontal bands of gray, each shading from black at the top to white at the bottom, 8 bits a pixel.", "no"),
        ("BASN2C08", "Truecolor, 8 bits", "Horizontal bands of yellow, magenta, cyan and gray, each shading from pale at the top of the band to full color at the bottom of it, in 8 bit color.", "no"),
        ("BASN0G16", "Grayscale, 16 bits", "A ramp of 16 bit gray samples that brightens from black at the left to white near the right edge and falls away again.", "no"),
        ("BASN2C16", "Truecolor, 16 bits", "Red, green and blue of 16 bit samples mixed across the square: yellow at the top left, green at the top right, red at the bottom left and blue at the bottom right.", "no"),
        ("BASN6A08", "Truecolor with alpha", "A rainbow that shades from red at the top to blue at the bottom, which its alpha channel fades from transparent at the left to opaque at the right, over a yellow square.", "yes"),
        ("BASN4A08", "Grayscale with alpha", "A gray ramp from white at the top to black at the bottom, which its alpha channel fades from transparent at the left to opaque at the right, over a yellow square.", "yes"),
        ("TP1N3P08", "Palette with transparency", "A black cube with the word NeXT on it in colored letters, and transparent pixels around it from the tRNS chunk of its palette, over a yellow square.", "yes"),
    ]

    public init() throws {
        let pdf = PDF(OutputStream(toFileAtPath: "Example_24.pdf", append: false)!)
        pdf.setCompliance(Compliance.PDF_UA_1)
        pdf.setTitle("JPEG, PNG and BMP Images")
        let font = try Font(pdf, IBMPlexSans.Regular)

        let image1 = try Image(pdf, "images/gr-map.jpg")
        image1.setAltDescription(
                "A map of Greece with its cities, roads and airports, the Ionian Sea to the west, the Aegean Sea to the east and Crete to the south.")
        let image2 = try Image(pdf, "images/ee-map.png")
        image2.setAltDescription(
                "A map of Europe in which the member states of the European Union are shaded, and Turkey, a candidate to join when the map was made, in another shade.")
        let image3 = try Image(pdf, "images/rgb24pal.bmp")
        image3.setAltDescription(
                "The letters BMP in white over bars of red, green, blue, yellow, magenta and cyan.")
        let image4 = try Image(pdf, "images/cmyk.jpg")
        image4.setAltDescription(
                "A CMYK test chart: rows of cyan, magenta, yellow and black from 0 to 100 percent in steps of 10, and bars of red, green, blue and rich black.")

        var page = Page(pdf, Letter.PORTRAIT)
        let textLine1 = TextLine(font, "This is a JPEG image.")
        textLine1.setTextRotation(0)
        textLine1.setLocation(50.0, 50.0)
        var point = textLine1.drawOn(page)
        image1.setLocation(50.0, point[1] + 5.0).scaleBy(0.25).drawOn(page)

        page = Page(pdf, Letter.PORTRAIT)
        let textLine2 = TextLine(font, "This is a PNG image.")
        textLine2.setTextRotation(0)
        textLine2.setLocation(50.0, 50.0)
        point = textLine2.drawOn(page)
        image2.setLocation(50.0, point[1] + 5.0).scaleBy(0.75).drawOn(page)

        let textLine3 = TextLine(font, "This is a BMP image.")
        textLine3.setTextRotation(0)
        textLine3.setLocation(50.0, 620.0)
        point = textLine3.drawOn(page)
        image3.setLocation(50.0, point[1] + 5.0).scaleBy(0.75).drawOn(page)

        page = Page(pdf, Letter.PORTRAIT)
        let textLine4 = TextLine(font, "This is a CMYK JPEG image, with its inks stored inverted, as Photoshop saves them.")
        textLine4.setLocation(50.0, 50.0)
        point = textLine4.drawOn(page)
        // The image is 300 DPI, so its size is 72/300 of its pixels.
        image4.setLocation(50.0, point[1] + 5.0).scaleBy(0.425 * 300.0 / 72.0).drawOn(page)

        try drawPngKinds(pdf)

        try pdf.complete()
    }

    // A page of a PNG of each kind, and a page of one that asks for its size.
    private func drawPngKinds(_ pdf: PDF) throws {
        let f1 = try Font(pdf, IBMPlexSans.SemiBold)
        let f2 = try Font(pdf, IBMPlexSans.Regular)

        var page = Page(pdf, Letter.PORTRAIT)

        let title = TextLine(f1, "PNG Images")
        title.setStructureType(StructElem.H1)
        title.setFontSize(18.0)
        title.setLocation(50.0, 50.0)
        title.drawOn(page)

        let textBlock = TextBlock(f2,
                "PDFjet reads a PNG of any color type and bit depth the format has: a " +
                "palette, grayscale or truecolor image of 1, 2, 4, 8 or 16 bits a " +
                "sample. The samples go into the PDF as they are, so an image is " +
                "embedded once and drawn at any size without being resampled. The two " +
                "ways a PNG carries transparency -- the alpha channel of a truecolor " +
                "or grayscale image, and the tRNS chunk of a palette -- both become " +
                "the soft mask of the image, which is why the three below let the " +
                "yellow square behind them through. An interlaced PNG is refused with " +
                "a message that says how to convert it.")
        textBlock.setFontSize(11.0)
        textBlock.setLineSpacing(1.4)
        textBlock.setLocation(50.0, 70.0)
        textBlock.setWidth(512.0)
        let xy = textBlock.drawOn(page)

        // The grid: four across, each image over the name of what it is.
        let size: Float = 128.0         // 32 pixels drawn at four times their size
        let columnWidth: Float = 128.0
        let rowHeight: Float = 168.0
        let top = xy[1] + 24.0
        for (i, row) in pngKinds.enumerated() {
            let x = 50.0 + Float(i%4)*columnWidth
            let y = top + Float(i/4)*rowHeight

            // A transparent image is drawn over a color, which its soft mask
            // lets through where the image is not opaque.
            if row.3 == "yes" {
                page.addArtifactBMC()
                page.setBrushColor(0xFFE9A0)        // A pale yellow
                page.fillRect(x, y, size, size)
                page.addEMC()
            }

            let image = try Image(pdf, "PngSuite/" + row.0 + ".PNG")
            image.setAltDescription(row.2)
            image.scaleBy(4.0)
            image.setLocation(x, y)
            image.drawOn(page)

            let caption = TextLine(f2, row.1)
            caption.setFontSize(9.0)
            caption.setLocation(x, y + size + 14.0)
            caption.drawOn(page)
        }

        // A PNG from outside PngSuite, which carries the chunks a file written
        // by a drawing program has and asks to be drawn at 300 dots per inch.
        page = Page(pdf, Letter.PORTRAIT)
        let heading = TextLine(f1, "A PNG that asks for its own size")
        heading.setStructureType(StructElem.H2)
        heading.setFontSize(14.0)
        heading.setLocation(50.0, 50.0)
        heading.drawOn(page)

        let note = TextBlock(f2,
                "The pHYs chunk of a PNG says how large the image is meant to be. This " +
                "one is 1520 by 400 pixels at 300 dots per inch, so it is drawn 364.8 " +
                "by 96 points: a quarter of the size it would be at one point for each " +
                "pixel, and sharp for it. It carries an iCCP color profile, a bKGD " +
                "background, a tIME timestamp and two IDAT chunks, which is what its " +
                "own text says.")
        note.setFontSize(11.0)
        note.setLineSpacing(1.4)
        note.setLocation(50.0, 72.0)
        note.setWidth(512.0)
        let xy2 = note.drawOn(page)

        let chunks = try Image(pdf, "images/rgba-8bit-chunks.png")
        chunks.setAltDescription(
                "Three half transparent circles in red, green and blue that overlap, " +
                "beside the heading 8-bit RGBA PNG, 1520 by 400, and the note that the " +
                "image has anti-aliased text and half transparent circles on a " +
                "transparent background, not interlaced, with iCCP, bKGD, pHYs, tIME " +
                "and two IDAT chunks.")
        chunks.setLocation(50.0, xy2[1] + 20.0)
        chunks.drawOn(page)
    }
}   // End of Example_24.swift

let time0 = Int64(Date().timeIntervalSince1970 * 1000)
_ = try Example_24()
let time1 = Int64(Date().timeIntervalSince1970 * 1000)
print("Example_24 => \(String(format: "%4lld", time1 - time0)) ms")
