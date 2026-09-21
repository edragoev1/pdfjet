/**
 * main.swift
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
import Foundation
import PDFjet

/**
 * Example_15.swift
 * This example draws chemical formulas with CompositeTextLine: the digits of
 * a formula are subscripts, the charge of an ion and the mass number of an
 * isotope are superscripts.
 */
public class Example_15 {
    // The sections of the page: the formulas as CompositeTextLine.addFormula
    // reads them, and what each one is called.
    private let compounds = [
        ("H2O", "Water"),
        ("CO2", "Carbon dioxide"),
        ("NaCl", "Sodium chloride"),
        ("H2SO4", "Sulfuric acid"),
        ("NaHCO3", "Sodium bicarbonate"),
        ("Fe2O3", "Iron(III) oxide"),
        ("Ca(OH)2", "Calcium hydroxide"),
        ("CuSO4\u{00B7}5H2O", "Copper(II) sulfate"),
        ("(NH4)3PO4", "Ammonium phosphate"),
        ("KAl(SO4)2", "Potassium alum"),
    ]

    private let organic = [
        ("C6H12O6", "Glucose"),
        ("CH3COOH", "Acetic acid"),
        ("C8H10N4O2", "Caffeine"),
        ("C9H8O4", "Aspirin"),
        ("C2H5OH", "Ethanol"),
        ("CH3(CH2)14COOH", "Palmitic acid"),
    ]

    private let ions = [
        ("Na^+", "Sodium"),
        ("Ca^2+", "Calcium"),
        ("NH4^+", "Ammonium"),
        ("OH^-", "Hydroxide"),
        ("SO4^2-", "Sulfate"),
        ("PO4^3-", "Phosphate"),
    ]

    private let isotopes = [
        ("^3H", "Tritium"),
        ("^14C", "Carbon-14"),
        ("^235U", "Uranium-235"),
    ]

    private let reactions = [
        ("2H2 + O2 \u{2192} 2H2O", "Hydrogen burns"),
        ("N2 + 3H2 \u{2192} 2NH3", "Ammonia, the Haber process"),
        ("CaCO3 \u{2192} CaO + CO2", "Limestone is calcined"),
        ("CH4 + 2O2 \u{2192} CO2 + 2H2O", "Methane burns"),
    ]

    public init() throws {
        let pdf = PDF(OutputStream(toFileAtPath: "Example_15.pdf", append: false)!)
        pdf.setCompliance(Compliance.PDF_UA_1)
        pdf.setTitle("Chemical Formulas")

        let f1 = try Font(pdf, IBMPlexSans.Regular)
        let f2 = try Font(pdf, IBMPlexSans.SemiBold)

        let page = Page(pdf, Letter.PORTRAIT)

        let title = TextLine(f2, "Chemical Formulas")
        title.setFontSize(18.0)
        title.setStructureType(StructElem.H1)
        title.setLocation(60.0, 70.0)
        title.drawOn(page)

        let intro = TextBlock(f1,
                "A CompositeTextLine draws one line of text out of parts that sit on "
                + "the baseline, above it or below it. AddFormula reads a formula and "
                + "places its parts: the digits that follow an element or a bracket are "
                + "subscripts, and what follows a circumflex is a superscript, the "
                + "charge of an ion or the mass number of an isotope. Each formula "
                + "below is one line and, in this tagged document, one structure "
                + "element, so a screen reader reads the formula and not its parts.")
        intro.setFontSize(11.0)
        intro.setLineSpacing(1.5)
        intro.setLocation(60.0, 90.0)
        intro.setWidth(492.0)
        let xy = intro.drawOn(page)

        var y = xy[1] + 26.0
        y = drawSection(page, f1, f2, "Compounds", compounds, 2, 266.0, 150.0, y)
        y = drawSection(page, f1, f2, "Organic compounds", organic, 2, 266.0, 150.0, y)
        y = drawSection(page, f1, f2, "Ions", ions, 3, 170.0, 60.0, y)
        y = drawSection(page, f1, f2, "Isotopes", isotopes, 3, 170.0, 60.0, y)
        _ = drawSection(page, f1, f2, "Reactions", reactions, 1, 0.0, 210.0, y)

        try pdf.complete()
    }

    // Draws the heading of a section and its formulas in the number of
    // columns, and returns the y where the next section begins.
    private func drawSection(
            _ page: Page, _ f1: Font, _ f2: Font, _ title: String,
            _ items: [(String, String)], _ columns: Int,
            _ columnWidth: Float, _ nameOffset: Float, _ y: Float) -> Float {
        let heading = TextLine(f2, title)
        heading.setFontSize(13.0)
        heading.setStructureType(StructElem.H2)
        heading.setLocation(60.0, y)
        heading.drawOn(page)

        let top = y + 22.0
        for (i, item) in items.enumerated() {
            let x = 70.0 + Float(i % columns)*columnWidth
            drawFormula(page, f1, item.0, item.1, x, top + Float(i / columns)*24.0, nameOffset)
        }
        let rows = (items.count + columns - 1) / columns
        return top + Float(rows)*24.0 + 10.0
    }

    // Draws the formula at the location and its name the offset to the right of it.
    private func drawFormula(
            _ page: Page, _ font: Font, _ formula: String, _ name: String,
            _ x: Float, _ y: Float, _ nameOffset: Float) {
        let composite = CompositeTextLine(x, y)
        composite.setFontSize(14.0)
        composite.addFormula(font, formula)
        composite.drawOn(page)

        let text = TextLine(font, name)
        text.setFontSize(10.0)
        text.setTextColor(Color.gray)
        text.setLocation(x + nameOffset, y)
        text.drawOn(page)
    }
}   // End of Example_15.swift

let time0 = Int64(Date().timeIntervalSince1970 * 1000)
_ = try Example_15()
let time1 = Int64(Date().timeIntervalSince1970 * 1000)
print("Example_15 => \(String(format: "%4lld", time1 - time0)) ms")
