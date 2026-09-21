/*
 * CompositeTextLineTest.java
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
package com.pdfjet;

import static org.junit.jupiter.api.Assertions.assertEquals;
import static org.junit.jupiter.api.Assertions.assertFalse;
import static org.junit.jupiter.api.Assertions.assertTrue;

import java.util.ArrayList;
import java.util.List;
import org.junit.jupiter.api.Test;

class CompositeTextLineTest {
    private static CompositeTextLine water(Font font, float fontSize) {
        CompositeTextLine composite = new CompositeTextLine(100f, 100f);
        if (fontSize > 0f) {
            composite.setFontSize(fontSize);
        }
        TextLine h = new TextLine(font, "H");
        TextLine two = new TextLine(font, "2");
        two.setScriptPosition(ScriptPosition.SUBSCRIPT);
        TextLine o = new TextLine(font, "O");
        composite.addComponent(h);
        composite.addComponent(two);
        composite.addComponent(o);
        return composite;
    }

    @Test
    void aSubscriptIsSmallerAndLowerWithoutSettingTheFontSize() throws Exception {
        Font font = TestSupport.helvetica(TestSupport.newPDF());
        // The font size of the components is the base when the composite text
        // line has none of its own.
        CompositeTextLine composite = water(font, 0f);
        TextLine two = composite.getTextLine(1);
        assertEquals(font.getSize() * composite.getSubscriptFactor(), two.getFontSize(),
                TestSupport.DELTA);
        assertEquals(100f + font.getSize() * composite.getSubscriptPosition(),
                two.getLocation()[1], TestSupport.DELTA);
        // The same composite text line with the font size set draws the same.
        CompositeTextLine withSize = water(font, font.getSize());
        assertEquals(two.getFontSize(), withSize.getTextLine(1).getFontSize(), TestSupport.DELTA);
        assertEquals(two.getLocation()[1], withSize.getTextLine(1).getLocation()[1],
                TestSupport.DELTA);
    }

    @Test
    void theFontSizeSetAfterTheComponentsWereAddedStillReachesThem() throws Exception {
        Font font = TestSupport.helvetica(TestSupport.newPDF());
        CompositeTextLine composite = new CompositeTextLine(100f, 100f);
        TextLine h = new TextLine(font, "H");
        TextLine two = new TextLine(font, "2");
        two.setScriptPosition(ScriptPosition.SUBSCRIPT);
        composite.addComponent(h).addComponent(two);
        composite.setFontSize(24f);
        assertEquals(24f, h.getFontSize(), TestSupport.DELTA);
        assertEquals(24f * composite.getSubscriptFactor(), two.getFontSize(), TestSupport.DELTA);
        assertEquals(100f + 24f * composite.getSubscriptPosition(), two.getLocation()[1],
                TestSupport.DELTA);
    }

    @Test
    void theHeightIsTheExtentOfWhatIsDrawn() throws Exception {
        PDF pdf = TestSupport.newPDF();
        Font big = new Font(pdf, CoreFont.HELVETICA);
        big.setSize(24f);
        Font small = new Font(pdf, CoreFont.HELVETICA);
        small.setSize(8f);
        // The components come from fonts of their own sizes, and the composite
        // text line draws them at 12 points and at the subscript size.
        CompositeTextLine composite = new CompositeTextLine(100f, 100f);
        composite.setFontSize(12f);
        TextLine h = new TextLine(big, "H");
        TextLine two = new TextLine(small, "2");
        two.setScriptPosition(ScriptPosition.SUBSCRIPT);
        composite.addComponent(h).addComponent(two);
        float[] minMax = composite.getMinMaxY();
        assertEquals(100f - big.getAscent(12f), minMax[0], TestSupport.DELTA);
        assertEquals(two.getLocation()[1] + small.getDescent(two.getFontSize()), minMax[1],
                TestSupport.DELTA);
        assertEquals(minMax[1] - minMax[0], composite.getHeight(), TestSupport.DELTA);
    }

    @Test
    void anEmptyCompositeTextLineMeasuresItsOwnLocation() throws Exception {
        CompositeTextLine composite = new CompositeTextLine(100f, 50f);
        TestSupport.assertXY(100f, 50f, composite.drawOn(null));
        assertEquals(0f, composite.getWidth(), 0f);
        assertEquals(0f, composite.getHeight(), 0f);
    }

    @Test
    void theWidthAndTheLocationSurviveASecondSetLocation() throws Exception {
        Font font = TestSupport.helvetica(TestSupport.newPDF());
        CompositeTextLine composite = water(font, 12f);
        float width = composite.getWidth();
        composite.setLocation(200f, 300f);
        composite.setLocation(200f, 300f);
        assertEquals(width, composite.getWidth(), TestSupport.DELTA);
        assertEquals(200f, composite.getTextLine(0).getLocation()[0], TestSupport.DELTA);
        assertEquals(300f, composite.getTextLine(0).getLocation()[1], TestSupport.DELTA);
    }

    @Test
    void aFormulaSubscriptsTheAtomCounts() throws Exception {
        Font font = TestSupport.helvetica(TestSupport.newPDF());
        CompositeTextLine water = new CompositeTextLine(0f, 0f);
        water.addFormula(font, "H2O");
        assertEquals(3, water.getNumberOfTextLines());
        assertEquals("H", water.getTextLine(0).getText());
        assertEquals("2", water.getTextLine(1).getText());
        assertEquals("O", water.getTextLine(2).getText());
        assertEquals(ScriptPosition.SUBSCRIPT, water.getTextLine(1).getScriptPosition());
        assertEquals(ScriptPosition.NORMAL, water.getTextLine(0).getScriptPosition());
        assertTrue(water.getTextLine(1).getFontSize() < water.getTextLine(0).getFontSize());

        CompositeTextLine glucose = new CompositeTextLine(0f, 0f);
        glucose.addFormula(font, "C6H12O6");
        assertEquals(6, glucose.getNumberOfTextLines());
        assertEquals("12", glucose.getTextLine(3).getText());
        assertEquals(ScriptPosition.SUBSCRIPT, glucose.getTextLine(3).getScriptPosition());

        // A digit that begins the formula counts the molecules, not the atoms.
        CompositeTextLine coefficient = new CompositeTextLine(0f, 0f);
        coefficient.addFormula(font, "2H2O");
        assertEquals("2H", coefficient.getTextLine(0).getText());
        assertEquals(ScriptPosition.NORMAL, coefficient.getTextLine(0).getScriptPosition());
        assertEquals(ScriptPosition.SUBSCRIPT, coefficient.getTextLine(1).getScriptPosition());

        // The digits of a group in brackets follow the bracket.
        CompositeTextLine lime = new CompositeTextLine(0f, 0f);
        lime.addFormula(font, "Ca(OH)2");
        assertEquals("Ca(OH)", lime.getTextLine(0).getText());
        assertEquals(ScriptPosition.SUBSCRIPT, lime.getTextLine(1).getScriptPosition());
    }

    @Test
    void aFormulaSuperscriptsTheChargeAfterACircumflex() throws Exception {
        Font font = TestSupport.helvetica(TestSupport.newPDF());
        CompositeTextLine calcium = new CompositeTextLine(0f, 0f);
        calcium.addFormula(font, "Ca^2+");
        assertEquals(2, calcium.getNumberOfTextLines());
        assertEquals("Ca", calcium.getTextLine(0).getText());
        assertEquals("2+", calcium.getTextLine(1).getText());
        assertEquals(ScriptPosition.SUPERSCRIPT, calcium.getTextLine(1).getScriptPosition());
        assertTrue(calcium.getTextLine(1).getLocation()[1] < calcium.getTextLine(0).getLocation()[1]);

        CompositeTextLine sulfate = new CompositeTextLine(0f, 0f);
        sulfate.addFormula(font, "SO4^2-");
        assertEquals(3, sulfate.getNumberOfTextLines());
        assertEquals(ScriptPosition.SUBSCRIPT, sulfate.getTextLine(1).getScriptPosition());
        assertEquals(ScriptPosition.SUPERSCRIPT, sulfate.getTextLine(2).getScriptPosition());

        // Nothing at all is one component of nothing.
        CompositeTextLine empty = new CompositeTextLine(0f, 0f);
        empty.addFormula(font, "");
        assertEquals(0, empty.getNumberOfTextLines());
    }

    @Test
    void aFormulaIsOneStructureElement() throws Exception {
        // The components are the runs of one formula, so a screen reader
        // reads C6H12O6 and not six elements of "C", "6", "H", "12", "O"
        // and "6".
        java.io.ByteArrayOutputStream bos = new java.io.ByteArrayOutputStream();
        PDF pdf = new PDF(bos, Compliance.PDF_UA_1);
        pdf.setTitle("Title");
        Page page = new Page(pdf, Letter.PORTRAIT);
        CompositeTextLine composite = new CompositeTextLine(50f, 100f);
        composite.setFontSize(14f);
        composite.addFormula(TestSupport.helvetica(pdf), "C6H12O6");
        composite.drawOn(page);
        assertEquals(6, TestSupport.content(page).split("BDC\n", -1).length - 1);
        pdf.complete();
        assertEquals(1, TestSupport.latin1(bos.toByteArray()).split("/S /P\n", -1).length - 1,
                "the formula is more than one element");
    }

    @Test
    void aCellDrawsALineOfTextOnItsBaselineWhateverItsAlignment() throws Exception {
        PDF pdf = TestSupport.newPDF();
        Font font = TestSupport.helvetica(pdf);
        // A TextLine is a baseline drawable too, and a cell draws it where it
        // draws its own text rather than at its top left corner.
        Cell cell = new Cell(font);
        TextLine line = new TextLine(font, "Text");
        cell.setDrawable(line);
        assertEquals(new Cell(font, "Text").getHeight(100f), cell.getHeight(100f),
                TestSupport.DELTA);
        Page page = new Page(pdf, Letter.PORTRAIT);
        cell.drawOn(page, 50f, 50f, 100f, 20f);
        // The baseline is an ascent below the top of the cell and its padding.
        assertEquals(50f + cell.getTopPadding() + font.getAscent(font.getSize()),
                line.getLocation()[1], TestSupport.DELTA);
        assertTrue(TestSupport.content(page).contains(TestSupport.hex("Text")));
        // The vertical alignments move the baseline down the cell, as they do
        // for cell text: the location of a text line is in the coordinates the
        // cell is drawn in, which grow downwards.
        float[] baselines = new float[3];
        Alignment[] valigns = {Alignment.TOP, Alignment.CENTER, Alignment.BOTTOM};
        for (int i = 0; i < valigns.length; i++) {
            Cell aligned = new Cell(font);
            TextLine text = new TextLine(font, "Text");
            aligned.setDrawable(text).setVerticalAlignment(valigns[i]);
            aligned.drawOn(new Page(pdf, Letter.PORTRAIT), 50f, 50f, 100f, 40f);
            baselines[i] = text.getLocation()[1];
        }
        assertTrue(baselines[0] < baselines[1] && baselines[1] < baselines[2]);
    }

    @Test
    void aCellTakesTheAscentOfTheLineItDraws() throws Exception {
        PDF pdf = TestSupport.newPDF();
        Font small = TestSupport.helvetica(pdf);
        small.setSize(8f);
        Font big = new Font(pdf, CoreFont.HELVETICA);
        big.setSize(24f);
        // The cell font is small and the line it draws is big, so the cell is
        // as tall as the line rather than as its own font.
        Cell cell = new Cell(small);
        cell.setDrawable(new TextLine(big, "Text"));
        assertEquals(big.getAscent(24f) + big.getDescent(24f)
                + cell.getTopPadding() + cell.getBottomPadding(),
                cell.getHeight(100f), TestSupport.DELTA);
    }

    @Test
    void aTableDoesNotWrapTheTextOfACellThatDrawsALineOfItsOwn() throws Exception {
        PDF pdf = TestSupport.newPDF();
        Font font = TestSupport.helvetica(pdf);
        List<List<Cell>> data = new ArrayList<List<Cell>>();
        List<Cell> row = new ArrayList<Cell>();
        // The cell text is long and the column narrow, but the composite is
        // what the cell draws, so the text is not wrapped into more rows.
        Cell cell = new Cell(font, "a long text that would wrap in a narrow column");
        CompositeTextLine composite = new CompositeTextLine(0f, 0f);
        composite.addFormula(font, "H2O");
        cell.setCompositeTextLine(composite);
        cell.setWidth(30f);
        row.add(cell);
        data.add(row);
        Table table = new Table().setTableData(data, 0).setLocation(20f, 20f);
        Page page = new Page(pdf, Letter.PORTRAIT);
        table.drawOn(page);
        // One row, drawn to the end, and no word of the cell text on the page.
        assertEquals(1, table.getRow(0).size());
        assertEquals(-1, table.getRowsRendered());
        String content = TestSupport.content(page);
        assertFalse(content.contains(TestSupport.hex("long")));
        assertTrue(content.contains(TestSupport.hex("H")));
    }

    @Test
    void aCellDrawsACompositeTextLineWithoutAnyText() throws Exception {
        PDF pdf = TestSupport.newPDF();
        Font font = TestSupport.helvetica(pdf);
        Page page = new Page(pdf, Letter.PORTRAIT);
        Cell cell = new Cell(font);
        CompositeTextLine composite = new CompositeTextLine(0f, 0f);
        composite.addFormula(font, "H2O");
        cell.setCompositeTextLine(composite);
        // The cell is as tall as the taller of its font and the composite.
        float fontHeight = font.getBodyHeight(font.getSize());
        float expected = Math.max(fontHeight, composite.getHeight())
                + cell.getTopPadding() + cell.getBottomPadding();
        assertEquals(expected, cell.getHeight(100f), TestSupport.DELTA);
        cell.drawOn(page, 50f, 50f, 100f, cell.getHeight(100f));
        String content = TestSupport.content(page);
        assertTrue(content.contains(TestSupport.hex("H")));
        assertTrue(content.contains(TestSupport.hex("2")));
        assertTrue(content.contains(TestSupport.hex("O")));
    }
}
