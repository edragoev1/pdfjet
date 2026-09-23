/**
 * main.swift
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
import Foundation
import PDFjet

/**
 * Example_05.swift
 *
 * Kerning with a core font: what it is, and the same words in two text blocks,
 * one above the other, drawn in Helvetica-Bold without kerning and with it.
 *
 * The fonts are core fonts, of the fourteen fonts every PDF viewer has, so the
 * document carries no font program. It is small, it is written fast, and the
 * widths and the kerning pairs of the fonts are built into PDFjet, which is
 * what setKernPairs applies. The disadvantages: the viewer draws the text with
 * its own version of the font, so the look differs a little between viewers;
 * only the WinAnsi characters can be drawn, so no Cyrillic, Greek or CJK text;
 * and a document with a font that is not embedded cannot claim PDF/A or PDF/UA
 * compliance. For those, use an embedded font like IBM Plex Sans, as the other
 * examples do.
 */
public class Example_05 {
    // Words with the pairs of letters kerning closes up: WA, AV, AW, AY, Yo,
    // To, Vo, VA and the like.
    private static let SAMPLE = "WAVE AWAY: Your Tokyo voyage, VAT paid."

    private static let BACKGROUND: Int32 = 0xf1f4f8

    public init() throws {
        let pdf = PDF(OutputStream(toFileAtPath: "Example_05.pdf", append: false)!)
        pdf.setTitle("Kerning")

        let regular = try Font(pdf, CoreFont.HELVETICA)
        regular.setSize(11.0)
        let bold = try Font(pdf, CoreFont.HELVETICA_BOLD)
        bold.setSize(24.0)

        // The same font twice: the one without kerning, which is the default,
        // and the one with it.
        let plain = try Font(pdf, CoreFont.HELVETICA_BOLD)
        plain.setSize(30.0)
        let kerned = try Font(pdf, CoreFont.HELVETICA_BOLD)
        kerned.setSize(30.0)
        kerned.setKernPairs(true)

        let page = Page(pdf, Letter.PORTRAIT)

        let title = TextLine(bold, "Kerning")
        title.setLocation(50.0, 70.0)
        title.drawOn(page)

        let about = TextBlock(regular,
                "Kerning moves particular pairs of letters closer together, so that the space "
                + "between the letters of a word looks even. Every letter of a font has a width, "
                + "the box it is drawn in, and some pairs of letters leave a gap between their "
                + "boxes that the eye reads as a space: a capital A beside a V or a W, a capital T, "
                + "V or Y over a small o, an L before a T. A font lists these pairs and how far to "
                + "move each of them.\n\n"
                + "The fourteen core fonts every PDF viewer has come with their lists, which are "
                + "built into PDFjet. font.setKernPairs(true) turns kerning on for a font: PDFjet "
                + "moves the letters of each pair as it draws them, with the TJ operator, and "
                + "measures the text the same way, so that a TextBlock breaks its lines where the "
                + "kerned words end. The two blocks below are the same words in the same font, "
                + "without kerning and with it.")
        about.setLineSpacing(1.4)
        about.setLocation(50.0, 90.0)
        about.setWidth(512.0)
        let xy = about.drawOn(page)

        var y: Float = xy[1] + 30.0
        y = Example_05.drawSample(page, regular, plain, "Without kerning: font.setKernPairs(false), the default", y)
        y = Example_05.drawSample(page, regular, kerned, "With kerning: font.setKernPairs(true)", y + 25.0)

        // How much kerning takes off the width of the words, as PDFjet
        // measures them, to the nearest point. Rounds half up, as Java does.
        let narrower = Int((plain.stringWidth(Example_05.SAMPLE)
                - kerned.stringWidth(Example_05.SAMPLE) + 0.5).rounded(.down))
        let note = TextLine(regular,
                "Kerning makes these words \(narrower) points narrower at 30 points.")
        note.setTextColor(Color.gray)
        note.setLocation(50.0, y + 30.0)
        note.drawOn(page)

        try pdf.complete()
    }

    // Draws the label, and under it the sample words in the font, in a text
    // block with a light background, and returns the bottom of the block.
    private static func drawSample(
            _ page: Page, _ labelFont: Font, _ font: Font, _ label: String, _ y: Float) -> Float {
        let caption = TextLine(labelFont, label)
        caption.setTextColor(Color.gray)
        caption.setLocation(50.0, y)
        caption.drawOn(page)

        let block = TextBlock(font, SAMPLE)
        block.setBackgroundColor(BACKGROUND)
        block.setPadding(10.0)
        block.setLineSpacing(1.2)
        block.setLocation(50.0, y + 8.0)
        block.setWidth(512.0)
        return block.drawOn(page)[1]
    }
}   // End of Example_05.swift

let time0 = Int64(Date().timeIntervalSince1970 * 1000)
_ = try Example_05()
let time1 = Int64(Date().timeIntervalSince1970 * 1000)
print("Example_05 => \(String(format: "%4lld", time1 - time0)) ms")
