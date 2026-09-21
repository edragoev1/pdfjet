/**
 * CompositeTextLineTests.swift
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
import Foundation
import Testing
@testable import PDFjet

@Suite struct CompositeTextLineTests {
    private func water(_ font: Font, _ fontSize: Float) -> CompositeTextLine {
        let composite = CompositeTextLine(100.0, 100.0)
        if fontSize > 0.0 {
            composite.setFontSize(fontSize)
        }
        let h = TextLine(font, "H")
        let two = TextLine(font, "2")
        two.setScriptPosition(ScriptPosition.SUBSCRIPT)
        let o = TextLine(font, "O")
        composite.addComponent(h)
        composite.addComponent(two)
        composite.addComponent(o)
        return composite
    }

    @Test func aSubscriptIsSmallerAndLowerWithoutSettingTheFontSize() {
        let font = TestSupport.helvetica(TestSupport.newPDF())
        // The font size of the components is the base when the composite text
        // line has none of its own.
        let composite = water(font, 0.0)
        let two = composite.getTextLine(1)!
        #expect(abs(two.getFontSize()
                - font.getSize() * composite.getSubscriptFactor()) < TestSupport.delta)
        #expect(abs(two.getLocation()[1]
                - (100.0 + font.getSize() * composite.getSubscriptPosition())) < TestSupport.delta)
        // The same composite text line with the font size set draws the same.
        let withSize = water(font, font.getSize())
        #expect(abs(withSize.getTextLine(1)!.getFontSize() - two.getFontSize()) < TestSupport.delta)
        #expect(abs(withSize.getTextLine(1)!.getLocation()[1] - two.getLocation()[1])
                < TestSupport.delta)
    }

    @Test func theFontSizeSetAfterTheComponentsWereAddedStillReachesThem() {
        let font = TestSupport.helvetica(TestSupport.newPDF())
        let composite = CompositeTextLine(100.0, 100.0)
        let h = TextLine(font, "H")
        let two = TextLine(font, "2")
        two.setScriptPosition(ScriptPosition.SUBSCRIPT)
        composite.addComponent(h).addComponent(two)
        composite.setFontSize(24.0)
        #expect(abs(h.getFontSize() - 24.0) < TestSupport.delta)
        #expect(abs(two.getFontSize() - 24.0 * composite.getSubscriptFactor()) < TestSupport.delta)
        #expect(abs(two.getLocation()[1]
                - (100.0 + 24.0 * composite.getSubscriptPosition())) < TestSupport.delta)
    }

    @Test func theHeightIsTheExtentOfWhatIsDrawn() throws {
        let pdf = TestSupport.newPDF()
        let big = try Font(pdf, CoreFont.HELVETICA)
        big.setSize(24.0)
        let small = try Font(pdf, CoreFont.HELVETICA)
        small.setSize(8.0)
        // The components come from fonts of their own sizes, and the composite
        // text line draws them at 12 points and at the subscript size.
        let composite = CompositeTextLine(100.0, 100.0)
        composite.setFontSize(12.0)
        let h = TextLine(big, "H")
        let two = TextLine(small, "2")
        two.setScriptPosition(ScriptPosition.SUBSCRIPT)
        composite.addComponent(h).addComponent(two)
        let minMax = composite.getMinMaxY()
        #expect(abs(minMax[0] - (100.0 - big.getAscent(12.0))) < TestSupport.delta)
        #expect(abs(minMax[1]
                - (two.getLocation()[1] + small.getDescent(two.getFontSize()))) < TestSupport.delta)
        #expect(abs(composite.getHeight() - (minMax[1] - minMax[0])) < TestSupport.delta)
    }

    @Test func anEmptyCompositeTextLineMeasuresItsOwnLocation() {
        let composite = CompositeTextLine(100.0, 50.0)
        let xy = composite.drawOn(nil)
        #expect(abs(xy[0] - 100.0) < TestSupport.delta)
        #expect(abs(xy[1] - 50.0) < TestSupport.delta)
        #expect(composite.getWidth() == 0.0)
        #expect(composite.getHeight() == 0.0)
    }

    @Test func theWidthAndTheLocationSurviveASecondSetLocation() {
        let font = TestSupport.helvetica(TestSupport.newPDF())
        let composite = water(font, 12.0)
        let width = composite.getWidth()
        _ = composite.setLocation(200.0, 300.0)
        _ = composite.setLocation(200.0, 300.0)
        #expect(abs(composite.getWidth() - width) < TestSupport.delta)
        #expect(abs(composite.getTextLine(0)!.getLocation()[0] - 200.0) < TestSupport.delta)
        #expect(abs(composite.getTextLine(0)!.getLocation()[1] - 300.0) < TestSupport.delta)
    }

    @Test func aFormulaSubscriptsTheAtomCounts() {
        let font = TestSupport.helvetica(TestSupport.newPDF())
        let water = CompositeTextLine(0.0, 0.0)
        water.addFormula(font, "H2O")
        #expect(water.getNumberOfTextLines() == 3)
        #expect(water.getTextLine(0)!.getText() == "H")
        #expect(water.getTextLine(1)!.getText() == "2")
        #expect(water.getTextLine(2)!.getText() == "O")
        #expect(water.getTextLine(1)!.getScriptPosition() == ScriptPosition.SUBSCRIPT)
        #expect(water.getTextLine(0)!.getScriptPosition() == ScriptPosition.NORMAL)
        #expect(water.getTextLine(1)!.getFontSize() < water.getTextLine(0)!.getFontSize())

        let glucose = CompositeTextLine(0.0, 0.0)
        glucose.addFormula(font, "C6H12O6")
        #expect(glucose.getNumberOfTextLines() == 6)
        #expect(glucose.getTextLine(3)!.getText() == "12")
        #expect(glucose.getTextLine(3)!.getScriptPosition() == ScriptPosition.SUBSCRIPT)

        // A digit that begins the formula counts the molecules, not the atoms.
        let coefficient = CompositeTextLine(0.0, 0.0)
        coefficient.addFormula(font, "2H2O")
        #expect(coefficient.getTextLine(0)!.getText() == "2H")
        #expect(coefficient.getTextLine(0)!.getScriptPosition() == ScriptPosition.NORMAL)
        #expect(coefficient.getTextLine(1)!.getScriptPosition() == ScriptPosition.SUBSCRIPT)

        // The digits of a group in brackets follow the bracket.
        let lime = CompositeTextLine(0.0, 0.0)
        lime.addFormula(font, "Ca(OH)2")
        #expect(lime.getTextLine(0)!.getText() == "Ca(OH)")
        #expect(lime.getTextLine(1)!.getScriptPosition() == ScriptPosition.SUBSCRIPT)
    }

    @Test func aFormulaSuperscriptsTheChargeAfterACircumflex() {
        let font = TestSupport.helvetica(TestSupport.newPDF())
        let calcium = CompositeTextLine(0.0, 0.0)
        calcium.addFormula(font, "Ca^2+")
        #expect(calcium.getNumberOfTextLines() == 2)
        #expect(calcium.getTextLine(0)!.getText() == "Ca")
        #expect(calcium.getTextLine(1)!.getText() == "2+")
        #expect(calcium.getTextLine(1)!.getScriptPosition() == ScriptPosition.SUPERSCRIPT)
        #expect(calcium.getTextLine(1)!.getLocation()[1] < calcium.getTextLine(0)!.getLocation()[1])

        let sulfate = CompositeTextLine(0.0, 0.0)
        sulfate.addFormula(font, "SO4^2-")
        #expect(sulfate.getNumberOfTextLines() == 3)
        #expect(sulfate.getTextLine(1)!.getScriptPosition() == ScriptPosition.SUBSCRIPT)
        #expect(sulfate.getTextLine(2)!.getScriptPosition() == ScriptPosition.SUPERSCRIPT)

        // Nothing at all is one component of nothing.
        let empty = CompositeTextLine(0.0, 0.0)
        empty.addFormula(font, "")
        #expect(empty.getNumberOfTextLines() == 0)
    }

    @Test func aFormulaIsOneStructureElement() throws {
        // The components are the runs of one formula, so a screen reader
        // reads C6H12O6 and not six elements of "C", "6", "H", "12", "O"
        // and "6".
        let memory = MemoryPDF(Compliance.PDF_UA_1)
        memory.pdf.setTitle("Title")
        let page = Page(memory.pdf, Letter.PORTRAIT)
        let composite = CompositeTextLine(50.0, 100.0)
        composite.setFontSize(14.0)
        composite.addFormula(TestSupport.helvetica(memory.pdf), "C6H12O6")
        composite.drawOn(page)
        #expect(TestSupport.content(page).components(separatedBy: "BDC\n").count - 1 == 6)
        try memory.pdf.complete()
        let raw = TestSupport.latin1(memory.bytes)
        #expect(raw.components(separatedBy: "/S /P\n").count - 1 == 1,
                "the formula is more than one element")
    }

    @Test func aCellDrawsALineOfTextOnItsBaselineWhateverItsAlignment() {
        let pdf = TestSupport.newPDF()
        let font = TestSupport.helvetica(pdf)
        // A TextLine is a baseline drawable too, and a cell draws it where it
        // draws its own text rather than at its top left corner.
        let cell = Cell(font)
        let line = TextLine(font, "Text")
        cell.setDrawable(line)
        #expect(abs(cell.getHeight(100.0) - Cell(font, "Text").getHeight(100.0))
                < TestSupport.delta)
        let page = Page(pdf, Letter.PORTRAIT)
        cell.drawOn(page, 50.0, 50.0, 100.0, 20.0)
        // The baseline is an ascent below the top of the cell and its padding.
        #expect(abs(line.getLocation()[1]
                - (50.0 + cell.getTopPadding() + font.getAscent(font.getSize())))
                < TestSupport.delta)
        #expect(TestSupport.content(page).contains(TestSupport.hex("Text")))
        // The vertical alignments move the baseline down the cell, as they do
        // for cell text.
        var baselines = [Float]()
        for valign in [Alignment.TOP, Alignment.CENTER, Alignment.BOTTOM] {
            let aligned = Cell(font)
            let text = TextLine(font, "Text")
            aligned.setDrawable(text).setVerticalAlignment(valign)
            aligned.drawOn(Page(pdf, Letter.PORTRAIT), 50.0, 50.0, 100.0, 40.0)
            baselines.append(text.getLocation()[1])
        }
        #expect(baselines[0] < baselines[1] && baselines[1] < baselines[2])
    }

    @Test func aCellTakesTheAscentOfTheLineItDraws() throws {
        let pdf = TestSupport.newPDF()
        let small = TestSupport.helvetica(pdf)
        small.setSize(8.0)
        let big = try Font(pdf, CoreFont.HELVETICA)
        big.setSize(24.0)
        // The cell font is small and the line it draws is big, so the cell is
        // as tall as the line rather than as its own font.
        let cell = Cell(small)
        cell.setDrawable(TextLine(big, "Text"))
        #expect(abs(cell.getHeight(100.0) - (big.getAscent(24.0) + big.getDescent(24.0)
                + cell.getTopPadding() + cell.getBottomPadding())) < TestSupport.delta)
    }

    @Test func aTableDoesNotWrapTheTextOfACellThatDrawsALineOfItsOwn() {
        let pdf = TestSupport.newPDF()
        let font = TestSupport.helvetica(pdf)
        // The cell text is long and the column narrow, but the composite is
        // what the cell draws, so the text is not wrapped into more rows.
        let cell = Cell(font, "a long text that would wrap in a narrow column")
        let composite = CompositeTextLine(0.0, 0.0)
        composite.addFormula(font, "H2O")
        cell.setCompositeTextLine(composite)
        cell.setWidth(30.0)
        let table = Table().setTableData([[cell]], 0)
        table.setLocation(20.0, 20.0)
        let page = Page(pdf, Letter.PORTRAIT)
        table.drawOn(page)
        // One row, drawn to the end, and no word of the cell text on the page.
        #expect(table.getRow(0).count == 1)
        #expect(table.getRowsRendered() == -1)
        let content = TestSupport.content(page)
        #expect(!content.contains(TestSupport.hex("long")))
        #expect(content.contains(TestSupport.hex("H")))
    }

    @Test func aCellDrawsACompositeTextLineWithoutAnyText() {
        let pdf = TestSupport.newPDF()
        let font = TestSupport.helvetica(pdf)
        let page = Page(pdf, Letter.PORTRAIT)
        let cell = Cell(font)
        let composite = CompositeTextLine(0.0, 0.0)
        composite.addFormula(font, "H2O")
        cell.setCompositeTextLine(composite)
        // The cell is as tall as the taller of its font and the composite.
        var expected = font.getBodyHeight(font.getSize())
        if composite.getHeight() > expected {
            expected = composite.getHeight()
        }
        expected += cell.getTopPadding() + cell.getBottomPadding()
        #expect(abs(cell.getHeight(100.0) - expected) < TestSupport.delta)
        cell.drawOn(page, 50.0, 50.0, 100.0, cell.getHeight(100.0))
        let content = TestSupport.content(page)
        #expect(content.contains(TestSupport.hex("H")))
        #expect(content.contains(TestSupport.hex("2")))
        #expect(content.contains(TestSupport.hex("O")))
    }
}
