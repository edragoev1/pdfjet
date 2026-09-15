/*
 * TableTest.java
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
package com.pdfjet;

import static org.junit.jupiter.api.Assertions.assertEquals;
import static org.junit.jupiter.api.Assertions.assertFalse;
import static org.junit.jupiter.api.Assertions.assertSame;
import static org.junit.jupiter.api.Assertions.assertTrue;

import java.io.File;
import java.io.FileOutputStream;
import java.io.OutputStream;
import java.nio.charset.StandardCharsets;
import java.util.ArrayList;
import java.util.List;
import org.junit.jupiter.api.Test;
import org.junit.jupiter.api.io.TempDir;

class TableTest {
    @TempDir
    File tempDir;

    private static List<List<Cell>> rows(Font font, int count, int columns) {
        List<List<Cell>> data = new ArrayList<List<Cell>>();
        for (int r = 0; r < count; r++) {
            List<Cell> row = new ArrayList<Cell>();
            for (int c = 0; c < columns; c++) {
                row.add(new Cell(font, columns == 1 ? "row" + r : "r" + r + "c" + c));
            }
            data.add(row);
        }
        return data;
    }

    @Test
    void measuringAndDrawingReturnTheSameCorner() throws Exception {
        PDF pdf = TestSupport.newPDF();
        Font font = TestSupport.helvetica(pdf);
        Table table = new Table().setTableData(rows(font, 5, 3), 1).setLocation(20f, 20f);
        TestSupport.assertXY(20f, 109.36f, table.drawOn((Page) null));
        TestSupport.assertXY(20f, 109.36f, table.drawOn(new Page(pdf, Letter.PORTRAIT)));
    }

    @Test
    void measuringFirstStillDrawsEveryRowOnThePage() throws Exception {
        PDF pdf = TestSupport.newPDF();
        Table table = new Table().setTableData(rows(TestSupport.helvetica(pdf), 60, 1), 1).setLocation(20f, 20f);
        table.drawOn((Page) null);
        Page page = new Page(pdf, Letter.PORTRAIT);
        table.drawOn(page);
        String content = TestSupport.content(page);
        assertTrue(content.contains(TestSupport.hex("row0")));
        assertTrue(content.contains(TestSupport.hex("row1")));
        assertEquals(42, table.getRowsRendered());
    }

    @Test
    void headerRowsRepeatOnEveryPage() throws Exception {
        PDF pdf = TestSupport.newPDF();
        Table table = new Table().setTableData(rows(TestSupport.helvetica(pdf), 60, 1), 1).setLocation(20f, 20f);
        List<Page> pages = new ArrayList<Page>();
        TestSupport.assertXY(20f, 341.696f, table.drawOn(pdf, pages, Letter.PORTRAIT));
        assertEquals(2, pages.size());
        String first = TestSupport.content(pages.get(0));
        String second = TestSupport.content(pages.get(1));
        assertTrue(first.contains(TestSupport.hex("row0")) && second.contains(TestSupport.hex("row0")));
        assertTrue(first.contains(TestSupport.hex("row1")) && !second.contains(TestSupport.hex("row1")));
        assertTrue(!first.contains(TestSupport.hex("row59")) && second.contains(TestSupport.hex("row59")));
    }

    @Test
    void theFileConstructorDropsAByteOrderMarkAndPadsShortRows() throws Exception {
        File file = new File(tempDir, "table.txt");
        OutputStream out = new FileOutputStream(file);
        try {
            out.write("﻿a|b|c\n1||\n2\n".getBytes(StandardCharsets.UTF_8));
        } finally {
            out.close();
        }
        Font font = TestSupport.helvetica(TestSupport.newPDF());
        Table table = new Table(font, font, file.getPath());
        assertEquals("a", table.getCellAt(0, 0).getText());
        assertEquals("c", table.getCellAt(0, 2).getText());
        assertEquals(3, table.getRow(1).size());
        assertEquals("1", table.getCellAt(1, 0).getText());
        assertEquals("", table.getCellAt(1, 2).getText());
        assertEquals(3, table.getRow(2).size());
        assertEquals("", table.getCellAt(2, 2).getText());
        assertEquals(3, table.getColumn(0).size());
    }

    @Test
    void getCellAtGetRowAndGetColumnAgree() throws Exception {
        Table table = new Table().setTableData(rows(TestSupport.helvetica(TestSupport.newPDF()), 4, 3), 1);
        assertSame(table.getCellAt(2, 1), table.getRow(2).get(1));
        assertSame(table.getCellAt(2, 1), table.getColumn(1).get(2));
        assertEquals("r2c1", table.getCellAt(2, 1).getText());
    }

    @Test
    void rightAlignNumbersRightAlignsOnlyNumbers() throws Exception {
        Font font = TestSupport.helvetica(TestSupport.newPDF());
        List<List<Cell>> data = new ArrayList<List<Cell>>();
        for (String text : new String[] {"header", "-1.5e3", "12a", "+7", "3."}) {
            List<Cell> row = new ArrayList<Cell>();
            row.add(new Cell(font, text));
            data.add(row);
        }
        Table table = new Table().setTableData(data, 1).rightAlignNumbers();
        assertEquals(Alignment.RIGHT, table.getCellAt(1, 0).getTextAlignment());
        assertEquals(Alignment.LEFT, table.getCellAt(2, 0).getTextAlignment());
        assertEquals(Alignment.RIGHT, table.getCellAt(3, 0).getTextAlignment());
        assertEquals(Alignment.RIGHT, table.getCellAt(4, 0).getTextAlignment());
        assertFalse(table.getCellAt(0, 0).getTextAlignment() == Alignment.RIGHT);
    }

    @Test
    void anEmptyTableHasNoWidth() throws Exception {
        assertEquals(0f, new Table().getWidth(), 0f);
    }

    @Test
    void anEmptyTableDrawsNothing() throws Exception {
        PDF pdf = TestSupport.newPDF();
        Page page = new Page(pdf, Letter.PORTRAIT);
        TestSupport.assertXY(20f, 30f, new Table().setLocation(20f, 30f).drawOn(page));
        TestSupport.assertXY(20f, 30f, new Table().setLocation(20f, 30f).drawOn((Page) null));
        List<Page> pages = new ArrayList<Page>();
        TestSupport.assertXY(20f, 30f, new Table().setLocation(20f, 30f).drawOn(pdf, pages, Letter.PORTRAIT));
        assertEquals(0, pages.size());
        Table empty = new Table().setTableData(new ArrayList<List<Cell>>(), 1);
        empty.autoAdjustColumnWidths().rightAlignNumbers();
        TestSupport.assertXY(0f, 0f, empty.drawOn(page));
        assertEquals("", TestSupport.content(page));
    }

    @Test
    void moreHeaderRowsThanRowsDrawsTheRows() throws Exception {
        PDF pdf = TestSupport.newPDF();
        Font font = TestSupport.helvetica(pdf);
        float[] expected = new Table().setTableData(rows(font, 2, 1), 1).setLocation(20f, 20f).drawOn((Page) null);
        float[] xy = new Table().setTableData(rows(font, 2, 1), 5).setLocation(20f, 20f).drawOn(new Page(pdf, Letter.PORTRAIT));
        TestSupport.assertXY(expected[0], expected[1], xy);
    }
}
