/*
 * CellTest.java
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
package com.pdfjet;

import static org.junit.jupiter.api.Assertions.assertEquals;
import static org.junit.jupiter.api.Assertions.assertFalse;
import static org.junit.jupiter.api.Assertions.assertNull;
import static org.junit.jupiter.api.Assertions.assertSame;
import static org.junit.jupiter.api.Assertions.assertTrue;

import org.junit.jupiter.api.Test;

class CellTest {
    @Test
    void aCellWithoutTextHasNoHeight() throws Exception {
        assertEquals(0f, new Cell(TestSupport.helvetica(TestSupport.newPDF())).getHeight(100f), 0f);
    }

    @Test
    void emptyTextIsOneLineTallWithThePaddings() throws Exception {
        Cell cell = new Cell(TestSupport.helvetica(TestSupport.newPDF()), "");
        assertEquals(2f, cell.getTopPadding(), 0f);
        assertEquals(2f, cell.getBottomPadding(), 0f);
        assertEquals(17.872f, cell.getHeight(100f), TestSupport.DELTA);
    }

    @Test
    void theCellFontSizeSetsTheHeight() throws Exception {
        Cell cell = new Cell(TestSupport.helvetica(TestSupport.newPDF()), "x");
        cell.setFontSize(24f);
        assertEquals(31.744f, cell.getHeight(100f), TestSupport.DELTA);
    }

    @Test
    void settingATextBlockClearsTheText() throws Exception {
        Font font = TestSupport.helvetica(TestSupport.newPDF());
        Cell withBlock = new Cell(font, "text");
        withBlock.setTextBlock(new TextBlock(font, "block"));
        assertNull(withBlock.getText());
    }

    @Test
    void aNewCellHasTopAndLeftBordersAndSpansOneColumn() throws Exception {
        // A table adds the right border of its last column and the bottom border of its last row.
        Cell cell = new Cell(TestSupport.helvetica(TestSupport.newPDF()), "x");
        assertTrue(cell.getBorder(Border.TOP));
        assertTrue(cell.getBorder(Border.LEFT));
        assertFalse(cell.getBorder(Border.RIGHT));
        assertFalse(cell.getBorder(Border.BOTTOM));
        assertEquals(1, cell.getColSpan());
    }

    @Test
    void setBorderChangesOneBorderAndKeepsTheColumnSpan() throws Exception {
        Cell cell = new Cell(TestSupport.helvetica(TestSupport.newPDF()), "x");
        cell.setColSpan(3);
        cell.setBorder(Border.TOP, false).setBorder(Border.BOTTOM, true);
        assertFalse(cell.getBorder(Border.TOP));
        assertTrue(cell.getBorder(Border.LEFT));
        assertTrue(cell.getBorder(Border.BOTTOM));
        assertEquals(3, cell.getColSpan());
        cell.setBorders(false);
        assertFalse(cell.getBorder(Border.LEFT) || cell.getBorder(Border.BOTTOM));
        assertEquals(3, cell.getColSpan());
    }

    @Test
    void transparentLeavesTheTextColorUnchanged() throws Exception {
        Cell cell = new Cell(TestSupport.helvetica(TestSupport.newPDF()), "x");
        cell.setTextColor(Color.blue).setTextColor(Color.transparent);
        TestSupport.assertRGB(0f, 0f, 1f, cell.getTextColor());
    }

    @Test
    void setFontChangesTheFallbackFontUnlessAnotherWasSet() throws Exception {
        PDF pdf = TestSupport.newPDF();
        Font helvetica = TestSupport.helvetica(pdf);
        Font courier = new Font(pdf, CoreFont.COURIER);
        Cell cell = new Cell(helvetica, "x").setFont(courier);
        assertSame(courier, cell.getFallbackFont());
        cell.setFallbackFont(helvetica).setFont(new Font(pdf, CoreFont.TIMES_ROMAN));
        assertSame(helvetica, cell.getFallbackFont());
    }
}
