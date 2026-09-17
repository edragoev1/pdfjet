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

import com.pdfjet.barcodes.Barcode;
import java.util.ArrayList;
import java.util.Arrays;
import java.util.List;
import org.junit.jupiter.api.Test;

class CellTest {
    // A drawable that is 30 wide and 20 high, and remembers where it was
    // placed and how many times it was drawn.
    private static final class Box implements Drawable {
        float x;
        float y;
        int draws;

        @Override
        public float[] drawOn(Page page) {
            if (page != null) {
                draws++;
            }
            return new float[] {x + 30f, y + 20f};
        }

        @Override
        public Drawable setLocation(float x, float y) {
            this.x = x;
            this.y = y;
            return this;
        }
    }

    @Test
    void theLastContentSetterWins() throws Exception {
        Font font = TestSupport.helvetica(TestSupport.newPDF());
        Cell cell = new Cell(font, "text");
        Barcode barcode = new Barcode(Barcode.CODE_128, "x");
        cell.setTextBlock(new TextBlock(font, "block")).setBarcode(barcode);
        assertNull(cell.getTextBlock());
        assertSame(barcode, cell.getBarcode());
        assertSame(barcode, cell.getDrawable());
        Box box = new Box();
        cell.setDrawable(box);
        assertNull(cell.getBarcode());
        assertNull(cell.getImage());
        assertNull(cell.getTextColumn());
        assertSame(box, cell.getDrawable());
        assertNull(cell.getText());
    }

    @Test
    void anyDrawableIsMeasuredAndAlignedInTheCell() throws Exception {
        PDF pdf = TestSupport.newPDF();
        Box box = new Box();
        Cell cell = new Cell(TestSupport.helvetica(pdf)).setDrawable(box);
        assertEquals(24f, cell.getHeight(100f), TestSupport.DELTA);   // 20 and the paddings of 2
        Page page = new Page(pdf, Letter.PORTRAIT);
        cell.drawOn(page, 10f, 50f, 100f, 24f);
        TestSupport.assertXY(12f, 52f, new float[] {box.x, box.y});
        cell.setTextAlignment(Alignment.CENTER).drawOn(page, 10f, 50f, 100f, 24f);
        TestSupport.assertXY(45f, 52f, new float[] {box.x, box.y});
        cell.setTextAlignment(Alignment.RIGHT).drawOn(page, 10f, 50f, 100f, 24f);
        TestSupport.assertXY(78f, 52f, new float[] {box.x, box.y});
        assertEquals(3, box.draws);
    }

    @Test
    void textSetAfterTheDrawableIsDrawnAndMeasuredInstead() throws Exception {
        PDF pdf = TestSupport.newPDF();
        Box box = new Box();
        Cell cell = new Cell(TestSupport.helvetica(pdf)).setDrawable(box);
        cell.setText("x");
        assertEquals(17.872f, cell.getHeight(100f), TestSupport.DELTA);
        cell.drawOn(new Page(pdf, Letter.PORTRAIT), 10f, 50f, 100f, 24f);
        assertEquals(0, box.draws);
        assertSame(box, cell.getDrawable());
    }

    @Test
    void aTableFitsItsColumnsToAnyDrawable() throws Exception {
        Font font = TestSupport.helvetica(TestSupport.newPDF());
        List<List<Cell>> data = new ArrayList<List<Cell>>();
        data.add(new ArrayList<Cell>(Arrays.asList(new Cell(font, "a"))));
        data.add(new ArrayList<Cell>(Arrays.asList(new Cell(font).setDrawable(new Box()))));
        Table table = new Table().setTableData(data, 1).autoAdjustColumnWidths();
        assertEquals(34f, table.getColumnWidth(0), TestSupport.DELTA);
    }

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
    void colorsAreKeptToTheNearestOf256Steps() throws Exception {
        Cell cell = new Cell(TestSupport.helvetica(TestSupport.newPDF()), "x");
        TestSupport.assertRGB(0f, 0f, 0f, cell.getTextColor());         // black by default
        assertNull(cell.getBackgroundColor());
        assertNull(cell.getBorderColor());
        cell.setTextColor(new float[] {0.5f, 0.25f, 1f});
        TestSupport.assertRGB(128 / 255f, 64 / 255f, 1f, cell.getTextColor());
        cell.setBackgroundColor(new float[] {-1f, 2f, 0.1f});           // kept between 0 and 1
        TestSupport.assertRGB(0f, 1f, 26 / 255f, cell.getBackgroundColor());
        cell.setBorderColor(0x336699);
        TestSupport.assertRGB(0x33 / 255f, 0x66 / 255f, 0x99 / 255f, cell.getBorderColor());
        cell.setBackgroundColor(Color.transparent);
        assertNull(cell.getBackgroundColor());
        cell.setBorderColor((float[]) null);
        assertNull(cell.getBorderColor());
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
