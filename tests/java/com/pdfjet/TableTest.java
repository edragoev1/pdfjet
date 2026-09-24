/*
 * TableTest.java
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
package com.pdfjet;

import static org.junit.jupiter.api.Assertions.assertEquals;
import static org.junit.jupiter.api.Assertions.assertFalse;
import static org.junit.jupiter.api.Assertions.assertNotEquals;
import static org.junit.jupiter.api.Assertions.assertSame;
import static org.junit.jupiter.api.Assertions.assertTrue;

import java.io.File;
import java.io.FileOutputStream;
import java.io.OutputStream;
import java.nio.charset.StandardCharsets;
import java.util.ArrayList;
import java.util.Arrays;
import java.util.List;
import java.util.Map;
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
        TestSupport.assertXY(245f, 109.36f, table.drawOn((Page) null));
        TestSupport.assertXY(245f, 109.36f, table.drawOn(new Page(pdf, Letter.PORTRAIT)));
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
        TestSupport.assertXY(95f, 341.696f, table.drawOn(pdf, pages, Letter.PORTRAIT));
        assertEquals(2, pages.size());
        String first = TestSupport.content(pages.get(0));
        String second = TestSupport.content(pages.get(1));
        assertTrue(first.contains(TestSupport.hex("row0")) && second.contains(TestSupport.hex("row0")));
        assertTrue(first.contains(TestSupport.hex("row1")) && !second.contains(TestSupport.hex("row1")));
        assertTrue(!first.contains(TestSupport.hex("row59")) && second.contains(TestSupport.hex("row59")));
    }

    @Test
    void aRowTallerThanThePageIsDrawnRatherThanAskedForForever() throws Exception {
        PDF pdf = TestSupport.newPDF();
        Font font = TestSupport.helvetica(pdf);
        StringBuilder text = new StringBuilder();
        for (int i = 0; i < 200; i++) {
            text.append("word").append(i).append(' ');
        }
        List<List<Cell>> data = new ArrayList<List<Cell>>();
        List<Cell> header = new ArrayList<Cell>();
        header.add(new Cell(font, "header"));
        data.add(header);
        List<Cell> row = new ArrayList<Cell>();
        Cell tall = new Cell(font);
        tall.setTextBlock(new TextBlock(font, text.toString())).setWidth(70f);
        row.add(tall);
        data.add(row);
        Table table = new Table().setTableData(data, 1).setLocation(50f, 50f).setBottomMargin(20f);
        assertTrue(tall.getHeight(66f) > 792f);      // taller than a Letter page
        List<Page> pages = new ArrayList<Page>();
        table.drawOn(pdf, pages, Letter.PORTRAIT);   // asked for pages forever before
        assertEquals(1, pages.size());
        assertEquals(-1, table.getRowsRendered());
        assertTrue(TestSupport.content(pages.get(0)).contains(TestSupport.hex("word0")));
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
    void theFileConstructorReadsQuotedFields() throws Exception {
        File file = new File(tempDir, "quoted.csv");
        OutputStream out = new FileOutputStream(file);
        try {
            // The commas inside the quotes are text, so the delimiter is the
            // comma between the fields and every row has three of them.
            out.write(("\"Name\",\"Note\",\"Amount\"\n"
                    + "\"Smith, John\",\"said \"\"hi\"\"\",\"1,200\"\n"
                    + "Plain,,7\n").getBytes(StandardCharsets.UTF_8));
        } finally {
            out.close();
        }
        Font font = TestSupport.helvetica(TestSupport.newPDF());
        Table table = new Table(font, font, file.getPath());
        assertEquals(3, table.getRow(0).size());
        assertEquals("Name", table.getCellAt(0, 0).getText());
        assertEquals("Smith, John", table.getCellAt(1, 0).getText());
        assertEquals("said \"hi\"", table.getCellAt(1, 1).getText());
        assertEquals("1,200", table.getCellAt(1, 2).getText());
        assertEquals(3, table.getRow(2).size());
        assertEquals("Plain", table.getCellAt(2, 0).getText());
        assertEquals("", table.getCellAt(2, 1).getText());
        assertEquals("7", table.getCellAt(2, 2).getText());
    }

    @Test
    void theFileConstructorReadsLineBreaksInQuotedFieldsAsSpaces() throws Exception {
        File file = new File(tempDir, "breaks.csv");
        OutputStream out = new FileOutputStream(file);
        try {
            out.write(("Name,Address\r\n"
                    + "\"Smith, John\",\"12 Main St\r\nApt 4\"\r\n"
                    + "Plain,\"one\n\ntwo\"\n").getBytes(StandardCharsets.UTF_8));
        } finally {
            out.close();
        }
        Font font = TestSupport.helvetica(TestSupport.newPDF());
        Table table = new Table(font, font, file.getPath());
        assertEquals("12 Main St Apt 4", table.getCellAt(1, 1).getText());
        assertEquals("Plain", table.getCellAt(2, 0).getText());
        assertEquals("one  two", table.getCellAt(2, 1).getText());
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
    // The number of times the text is in the string.
    private static int count(String str, String text) {
        return str.split(java.util.regex.Pattern.quote(text), -1).length - 1;
    }

    @Test
    void aTableInAPDFUADocumentIsTaggedAsATable() throws Exception {
        java.io.ByteArrayOutputStream bos = new java.io.ByteArrayOutputStream();
        PDF pdf = new PDF(bos, Compliance.PDF_UA_1);
        pdf.setTitle("Title");
        Font font = TestSupport.helvetica(pdf);
        List<List<Cell>> data = new ArrayList<List<Cell>>();
        data.add(new ArrayList<Cell>(java.util.Arrays.asList(new Cell(font, "Name"), new Cell(font, "Notes"))));
        data.add(new ArrayList<Cell>(java.util.Arrays.asList(new Cell(font, "a"),
                new Cell(font, "a note long enough to wrap to four lines"))));
        Cell spanned = new Cell(font, "spanned").setColSpan(2);
        data.add(new ArrayList<Cell>(java.util.Arrays.asList(spanned, new Cell(font, ""))));
        Cell underlined = new Cell(font, "b");
        underlined.setUnderline(true);
        data.add(new ArrayList<Cell>(java.util.Arrays.asList(underlined, new Cell(font, "c"))));
        new Table().setTableData(data, 1).setLocation(20f, 20f).drawOn(new Page(pdf, Letter.PORTRAIT));
        pdf.complete();
        String raw = TestSupport.latin1(bos.toByteArray());
        assertEquals(1, count(raw, "/S /Table\n"), raw);
        // The lines of the wrapped note are one row and one cell.
        assertEquals(4, count(raw, "/S /TR\n"), raw);
        assertEquals(2, count(raw, "/S /TH\n"), raw);
        assertEquals(5, count(raw, "/S /TD\n"), raw);
        assertEquals(2, count(raw, "/A <</O /Table /Scope /Column>>"), raw);
        assertEquals(1, count(raw, "/A <</O /Table /ColSpan 2>>"), raw);
        assertEquals(1, raw.split("/S /TD\n[^\n]*\n/K \\[\\d+ 0 R \\d+ 0 R \\d+ 0 R \\d+ 0 R \\]", -1).length - 1, raw);
        // The text of the cells, and not the underline, is in P elements.
        assertEquals(10, count(raw, "/S /P\n"), raw);
    }

    @Test
    void theHeaderRowsOnTheNextPagesAreArtifacts() throws Exception {
        java.io.ByteArrayOutputStream bos = new java.io.ByteArrayOutputStream();
        PDF pdf = new PDF(bos, Compliance.PDF_UA_1);
        pdf.setTitle("Title");
        Table table = new Table().setTableData(rows(TestSupport.helvetica(pdf), 60, 2), 1).setLocation(20f, 20f);
        List<Page> pages = new ArrayList<Page>();
        table.drawOn(pdf, pages, Letter.PORTRAIT);
        assertEquals(2, pages.size());
        String content = TestSupport.content(pages.get(1));
        assertTrue(content.startsWith("/Artifact BMC\n"), content);
        int end = content.indexOf("EMC\n");
        assertTrue(content.indexOf(TestSupport.hex("r0c1")) < end, content);
        assertTrue(content.indexOf("BDC") > end, content);
        for (Page page : pages) {
            pdf.addPage(page);
        }
        pdf.complete();
        String raw = TestSupport.latin1(bos.toByteArray());
        assertEquals(1, count(raw, "/S /Table\n"), raw);
        assertEquals(60, count(raw, "/S /TR\n"), raw);
        assertEquals(2, count(raw, "/S /TH\n"), raw);
        assertEquals(118, count(raw, "/S /TD\n"), raw);
    }

    @Test
    void aTableIsNotTaggedInADocumentThatIsNotPDFUA() throws Exception {
        java.io.ByteArrayOutputStream bos = new java.io.ByteArrayOutputStream();
        PDF pdf = new PDF(bos);
        Table table = new Table().setTableData(rows(TestSupport.helvetica(pdf), 60, 2), 1).setLocation(20f, 20f);
        List<Page> pages = new ArrayList<Page>();
        table.drawOn(pdf, pages, Letter.PORTRAIT);
        String content = TestSupport.content(pages.get(1));
        assertFalse(content.contains("BMC") || content.contains("BDC") || content.contains("EMC"), content);
    }

    @Test
    void aTableWithAPageLeftOutOfTheDocumentStillHasAStructureTree() throws Exception {
        java.io.ByteArrayOutputStream bos = new java.io.ByteArrayOutputStream();
        PDF pdf = new PDF(bos, Compliance.PDF_UA_1);
        pdf.setTitle("Title");
        Table table = new Table().setTableData(rows(TestSupport.helvetica(pdf), 60, 2), 1).setLocation(20f, 20f);
        List<Page> pages = new ArrayList<Page>();
        table.drawOn(pdf, pages, Letter.PORTRAIT);
        pdf.addPage(pages.get(1));  // The page with the Table element is left out.
        pdf.complete();
        String raw = TestSupport.latin1(bos.toByteArray());
        assertFalse(raw.contains("/P 0 0 R"), raw);
        assertFalse(raw.contains("/K [0 0 R") || raw.contains(" 0 0 R ]"), raw);
    }
    // The y coordinates of the horizontal rules the page draws, top to bottom.
    private static List<Float> rules(Page page) {
        java.util.TreeSet<Float> ys = new java.util.TreeSet<Float>();
        java.util.regex.Matcher m = java.util.regex.Pattern.compile(
                "([-0-9.]+) ([-0-9.]+) m\\n([-0-9.]+) ([-0-9.]+) l").matcher(TestSupport.content(page));
        while (m.find()) {
            if (Math.abs(Float.parseFloat(m.group(2)) - Float.parseFloat(m.group(4))) < 0.01f) {
                ys.add(Float.parseFloat(m.group(2)));
            }
        }
        List<Float> list = new ArrayList<Float>(ys);
        java.util.Collections.reverse(list);
        return list;
    }

    // A table of rows by columns of cells with every border, the cell at 0,0
    // spanning the rows.
    private static List<List<Cell>> spanning(Font font, int rows, int columns, int rowspan) {
        List<List<Cell>> data = new ArrayList<List<Cell>>();
        for (int r = 0; r < rows; r++) {
            List<Cell> row = new ArrayList<Cell>();
            for (int c = 0; c < columns; c++) {
                Cell cell = new Cell(font, (r == 0 && c == 0) ? "spans" : (r < rowspan && c == 0) ? "" : "r" + r + "c" + c);
                cell.setWidth(60f);
                cell.setBorder(Border.TOP, true);
                cell.setBorder(Border.BOTTOM, true);
                cell.setBorder(Border.LEFT, true);
                cell.setBorder(Border.RIGHT, true);
                row.add(cell);
            }
            data.add(row);
        }
        data.get(0).get(0).setRowSpan(rowspan);
        return data;
    }

    @Test
    void aCellThatSpansRowsIsDrawnOnceOverAllOfThem() throws Exception {
        PDF pdf = TestSupport.newPDF();
        Font font = TestSupport.helvetica(pdf);
        Page page = new Page(pdf, Letter.PORTRAIT);
        new Table().setTableData(spanning(font, 3, 2, 2)).setLocation(50f, 50f).drawOn(page);
        String content = TestSupport.content(page);
        // The spanning cell is drawn once, and the cell it covers not at all.
        assertEquals(1, count(content, TestSupport.hex("spans")), content);
        assertEquals(0, count(content, TestSupport.hex("r1c0")), content);
        assertEquals(1, count(content, TestSupport.hex("r1c1")), content);
        // Its left border runs from the top of its row to the bottom of the
        // row under it, which is two rows of the three rules the table draws.
        List<Float> ys = rules(page);
        assertEquals(4, ys.size(), ys.toString());
        float rowHeight = ys.get(0) - ys.get(1);
        java.util.regex.Matcher m = java.util.regex.Pattern.compile(
                "50 ([-0-9.]+) m\\n50 ([-0-9.]+) l").matcher(content);
        assertTrue(m.find(), content);
        assertEquals(2 * rowHeight, Float.parseFloat(m.group(1)) - Float.parseFloat(m.group(2)),
                0.01f, "the spanning cell is not two rows tall");
    }

    @Test
    void aPageBreakKeepsTheRowsOfASpanTogether() throws Exception {
        // The rows a cell spans go to the next page with it, so that a span is
        // never cut in two.
        for (int rowspan : new int[] {1, 2, 3, 4}) {
            PDF pdf = TestSupport.newPDF();
            Font font = TestSupport.helvetica(pdf);
            List<List<Cell>> data = spanning(font, 60, 2, rowspan);
            // The span sits where the first page ends.
            data.get(0).get(0).setRowSpan(1);
            data.get(48).get(0).setRowSpan(rowspan).setText("spans");
            for (int r = 49; r < 48 + rowspan; r++) {
                data.get(r).get(0).setText("");
            }
            Table table = new Table().setTableData(data).setLocation(50f, 50f);
            table.setBottomMargin(20f);
            List<Page> pages = new ArrayList<Page>();
            table.drawOn(pdf, pages, Letter.PORTRAIT);
            List<String> contents = new ArrayList<String>();
            for (Page page : pages) {
                contents.add(TestSupport.content(page));
            }
            int spanPage = -1;
            for (int i = 0; i < contents.size(); i++) {
                if (count(contents.get(i), TestSupport.hex("spans")) > 0) {
                    spanPage = i;
                }
            }
            assertTrue(spanPage >= 0, "the spanning cell was not drawn");
            for (int r = 48; r < 48 + rowspan; r++) {
                assertEquals(1, count(contents.get(spanPage), TestSupport.hex("r" + r + "c1")),
                        "row " + r + " is not on the page of the span it belongs to");
            }
        }
    }

    @Test
    void aCellThatSpansRowsSaysSoInAPDFUADocument() throws Exception {
        java.io.ByteArrayOutputStream bos = new java.io.ByteArrayOutputStream();
        PDF pdf = new PDF(bos, Compliance.PDF_UA_1);
        pdf.setTitle("Title");
        Font font = TestSupport.helvetica(pdf);
        List<List<Cell>> data = spanning(font, 3, 2, 2);
        data.get(0).get(0).setColSpan(2);
        data.get(0).get(1).setText("");
        data.get(1).get(1).setText("");     // The second row is covered whole.
        new Table().setTableData(data, 1).setLocation(50f, 50f).drawOn(new Page(pdf, Letter.PORTRAIT));
        pdf.complete();
        String raw = TestSupport.latin1(bos.toByteArray());
        assertEquals(1, count(raw, "/A <</O /Table /Scope /Column /ColSpan 2 /RowSpan 2>>"), raw);
        // A row that a span covers whole holds no cell of its own.
        assertEquals(2, count(raw, "/S /TR\n"), raw);
        assertEquals(1, count(raw, "/S /TH\n"), raw);
        assertEquals(2, count(raw, "/S /TD\n"), raw);
    }

    private static final String LONG_TEXT =
            "one two three four five six seven eight nine ten eleven twelve";

    // A one row table of a cell that wraps and a cell that does not, each with
    // every border.
    private static List<List<Cell>> wrapping(Font font) {
        List<Cell> row = new ArrayList<Cell>();
        for (String text : new String[] {LONG_TEXT, "one"}) {
            Cell cell = new Cell(font, text);
            cell.setWidth(60f);
            cell.setBorders(true);
            row.add(cell);
        }
        List<List<Cell>> data = new ArrayList<List<Cell>>();
        data.add(row);
        return data;
    }

    @Test
    void aCellWhoseTextWrapsDrawsOneBorderUnderIt() throws Exception {
        // The rows a table wraps the text of a cell into are one cell, so the
        // border under it is drawn once, under the last of its lines. Each of
        // those rows kept the borders of the cell, so a cell of seven lines
        // drew seven rules across itself.
        PDF pdf = TestSupport.newPDF();
        Font font = TestSupport.helvetica(pdf);
        Page page = new Page(pdf, Letter.PORTRAIT);
        Table table = new Table().setTableData(wrapping(font)).setLocation(50f, 50f);
        float[] xy = table.drawOn(page);
        assertNotEquals(LONG_TEXT, table.getRow(0).get(0).getText(), "the text did not wrap");
        // The table draws two rules: the one over the row and the one under it.
        List<Float> ys = rules(page);
        assertEquals(2, ys.size(), ys.toString());
        assertEquals(page.height - 50f, ys.get(0), 0.01f, "the rule over the row");
        assertEquals(page.height - xy[1], ys.get(1), 0.01f, "the rule under the row");
    }

    @Test
    void theRowsACellWrapsIntoAreOneCellOfEveryBorderButTheirOwn() throws Exception {
        // The left and the right borders are drawn down every row of the wrap,
        // which is what makes the rows one cell; only the top and the bottom
        // of the cell are drawn once.
        PDF pdf = TestSupport.newPDF();
        Font font = TestSupport.helvetica(pdf);
        Page page = new Page(pdf, Letter.PORTRAIT);
        Table table = new Table().setTableData(wrapping(font)).setLocation(50f, 50f);
        float[] xy = table.drawOn(page);
        String content = TestSupport.content(page);
        // A vertical rule of each row of the wrap, down each of the three
        // edges the two cells have between and beside them.
        java.util.regex.Matcher m = java.util.regex.Pattern.compile(
                "([-0-9.]+) ([-0-9.]+) m\\n([-0-9.]+) ([-0-9.]+) l").matcher(content);
        Map<String, Float> down = new java.util.TreeMap<String, Float>();
        while (m.find()) {
            if (m.group(1).equals(m.group(3))) {
                float height = Float.parseFloat(m.group(2)) - Float.parseFloat(m.group(4));
                Float sum = down.get(m.group(1));
                down.put(m.group(1), (sum == null ? 0f : sum) + Math.abs(height));
            }
        }
        assertEquals(new ArrayList<String>(Arrays.asList("110", "170", "50")),
                new ArrayList<String>(down.keySet()), down.toString());
        float height = xy[1] - 50f;
        // The edge between the two cells is the right border of the one and
        // the left border of the other, so it is drawn twice.
        assertEquals(height, down.get("50"), 0.01f, "the left edge of the row");
        assertEquals(2 * height, down.get("110"), 0.01f, "the edge between the cells");
        assertEquals(height, down.get("170"), 0.01f, "the right edge of the row");
    }

    // The y of every rule the page draws across the first column, in the order
    // they are drawn and with none of them left out.
    private static List<Float> rulesAcrossTheFirstColumn(Page page) {
        List<Float> ys = new ArrayList<Float>();
        java.util.regex.Matcher m = java.util.regex.Pattern.compile(
                "50 ([-0-9.]+) m\\n110 ([-0-9.]+) l").matcher(TestSupport.content(page));
        while (m.find()) {
            if (m.group(1).equals(m.group(2))) {
                ys.add(Float.parseFloat(m.group(1)));
            }
        }
        return ys;
    }

    @Test
    void aCellThatWrapsAndSpansRowsDrawsItsBorderUnderTheWholeSpan() throws Exception {
        // A cell that spans rows is drawn over all of them at once, so its
        // bottom border is drawn under the whole span and not at the end of
        // the rows its own text wraps into, which is where a cell that spans
        // no rows draws it.
        PDF pdf = TestSupport.newPDF();
        Font font = TestSupport.helvetica(pdf);
        List<List<Cell>> data = spanning(font, 3, 2, 2);
        data.get(0).get(0).setText(LONG_TEXT);
        Page page = new Page(pdf, Letter.PORTRAIT);
        Table table = new Table().setTableData(data).setLocation(50f, 50f);
        float[] xy = table.drawOn(page);
        assertNotEquals(LONG_TEXT, table.getRow(0).get(0).getText(), "the text did not wrap");
        // The rule over the span, the one under it, the one over the row below
        // it, which is the same line drawn by that row, and the one under the
        // table. The rows the text wrapped into draw none of their own.
        List<Float> ys = rulesAcrossTheFirstColumn(page);
        assertEquals(4, ys.size(), ys.toString());
        assertEquals(page.height - 50f, ys.get(0), 0.01f, "the rule over the span");
        assertEquals(ys.get(1), ys.get(2), 0.01f,
                "the span and the row under it do not meet");
        assertTrue(ys.get(1) < ys.get(0) && ys.get(1) > ys.get(3),
                "the rule under the span is not between the top and the bottom of the table");
        assertEquals(page.height - xy[1], ys.get(3), 0.01f, "the rule under the table");
    }

    @Test
    void aPageBreakKeepsTheWrappedLinesOfARowTogether() throws Exception {
        // The lines a cell's text wraps into are rows of their own, which a
        // page break moves to the next page together, with the other cells of
        // the row. The row is put at each place near the end of the first page.
        boolean moved = false;
        for (int at = 30; at < 50; at++) {
            PDF pdf = TestSupport.newPDF();
            Font font = TestSupport.helvetica(pdf);
            List<List<Cell>> data = new ArrayList<List<Cell>>();
            for (int r = 0; r < 60; r++) {
                List<Cell> row = new ArrayList<Cell>();
                for (String text : (r == at) ? new String[] {LONG_TEXT, "beside"} : new String[] {"r" + r, "x"}) {
                    row.add(new Cell(font, text).setWidth(60f));
                }
                data.add(row);
            }
            Table table = new Table().setTableData(data, 1).setLocation(50f, 50f).setBottomMargin(20f);
            List<Page> pages = new ArrayList<Page>();
            table.drawOn(pdf, pages, Letter.PORTRAIT);
            assertNotEquals(LONG_TEXT, table.getRow(at).get(0).getText(), "the text did not wrap");
            int first = -1;
            int last = -1;
            int beside = -1;
            for (int i = 0; i < pages.size(); i++) {
                String content = TestSupport.content(pages.get(i));
                if (content.contains(TestSupport.hex("one"))) {
                    first = i;
                }
                if (content.contains(TestSupport.hex("twelve"))) {
                    last = i;
                }
                if (content.contains(TestSupport.hex("beside"))) {
                    beside = i;
                }
            }
            assertTrue(first >= 0, "the wrapped text was not drawn");
            assertEquals(first, last, "row " + at + ": the page break cut the wrapped text");
            assertEquals(first, beside, "row " + at + ": the other cell of the row is not with its lines");
            moved |= (first == 1 && TestSupport.content(pages.get(0)).contains(TestSupport.hex("r" + (at - 1))));
        }
        assertTrue(moved, "no row was moved to the next page whole");
    }

    @Test
    void theWrappedLinesOfARowThatFitNoPageAreCutWhereThePageEnds() throws Exception {
        // Moved to the next page, lines taller than a page would go past its
        // end there too, so they are drawn from where the row starts and go
        // on over the next pages.
        PDF pdf = TestSupport.newPDF();
        Font font = TestSupport.helvetica(pdf);
        StringBuilder text = new StringBuilder();
        for (int i = 0; i < 200; i++) {
            text.append("word").append(i).append(' ');
        }
        List<List<Cell>> data = new ArrayList<List<Cell>>();
        for (String[] texts : new String[][] {{"header", "h"}, {"r1", "x"}, {text.toString().trim(), "beside"}}) {
            List<Cell> row = new ArrayList<Cell>();
            for (String t : texts) {
                row.add(new Cell(font, t).setWidth(60f));
            }
            data.add(row);
        }
        Table table = new Table().setTableData(data, 1).setLocation(50f, 50f).setBottomMargin(20f);
        List<Page> pages = new ArrayList<Page>();
        table.drawOn(pdf, pages, Letter.PORTRAIT);
        assertTrue(pages.size() >= 3, pages.size() + " pages");
        assertEquals(-1, table.getRowsRendered());
        String firstPage = TestSupport.content(pages.get(0));
        assertTrue(firstPage.contains(TestSupport.hex("r1")) && firstPage.contains(TestSupport.hex("word0")),
                "the row does not start on the first page, after the row above it");
        assertTrue(TestSupport.content(pages.get(pages.size() - 1)).contains(TestSupport.hex("word199")));
    }

    // A table of 60 rows of two cells, "r0" to "r59" and "x", whose row at
    // index has the texts given instead.
    private static List<List<Cell>> rowsWith(Font font, int index, String... texts) {
        List<List<Cell>> data = new ArrayList<List<Cell>>();
        for (int r = 0; r < 60; r++) {
            List<Cell> row = new ArrayList<Cell>();
            for (String text : (r == index) ? texts : new String[] {"r" + r, "x"}) {
                row.add(new Cell(font, text).setWidth(60f));
            }
            data.add(row);
        }
        return data;
    }

    // The index of the last page that draws the text, or -1.
    private static int pageOf(List<Page> pages, String text) {
        int page = -1;
        for (int i = 0; i < pages.size(); i++) {
            if (TestSupport.content(pages.get(i)).contains(TestSupport.hex(text))) {
                page = i;
            }
        }
        return page;
    }

    @Test
    void aRowKeptWithTheNextOneGoesToThePageOfTheNextOne() throws Exception {
        // A heading row, whose text wraps, is kept with the row under it: a
        // page break does not fall between them, and moves both to the next
        // page. The rows are put at each place near the end of the first page.
        boolean moved = false;
        for (int at = 30; at < 50; at++) {
            PDF pdf = TestSupport.newPDF();
            List<List<Cell>> data = rowsWith(TestSupport.helvetica(pdf), at, LONG_TEXT, "heading");
            data.get(at + 1).get(0).setText("follows");
            Table table = new Table().setTableData(data, 1).setLocation(50f, 50f).setBottomMargin(20f);
            table.keepRowWithNext(at);
            List<Page> pages = new ArrayList<Page>();
            table.drawOn(pdf, pages, Letter.PORTRAIT);
            int heading = pageOf(pages, "one");
            assertTrue(heading >= 0, "the heading was not drawn");
            assertEquals(heading, pageOf(pages, "twelve"), "row " + at + ": the page break cut the heading");
            assertEquals(heading, pageOf(pages, "follows"), "row " + at + ": the heading is not with the next row");
            moved |= (heading == 1 && pageOf(pages, "r" + (at - 1)) == 0);
        }
        assertTrue(moved, "no heading was moved to the next page with the next row");
    }

    @Test
    void rowsKeptWithTheNextOneOneAfterAnotherAreKeptTogether() throws Exception {
        for (int at = 30; at < 50; at++) {
            PDF pdf = TestSupport.newPDF();
            Table table = new Table().setTableData(rowsWith(TestSupport.helvetica(pdf), -1), 1)
                    .setLocation(50f, 50f).setBottomMargin(20f);
            table.keepRowWithNext(at).keepRowWithNext(at + 1).keepRowWithNext(at + 2);
            List<Page> pages = new ArrayList<Page>();
            table.drawOn(pdf, pages, Letter.PORTRAIT);
            int page = pageOf(pages, "r" + at);
            for (int r = at + 1; r <= at + 3; r++) {
                assertEquals(page, pageOf(pages, "r" + r), "row " + r + " is not with row " + at);
            }
        }
    }

    @Test
    void rowsKeptTogetherThatFitNoPageAreDrawnEachOnItsOwn() throws Exception {
        // Moved to the next page, rows taller than a page would go past its
        // end there too, so they are drawn from where they start as if they
        // were not kept together: the pages hold the rows they hold without
        // the marks.
        List<List<Page>> drawn = new ArrayList<List<Page>>();
        for (boolean kept : new boolean[] {false, true}) {
            PDF pdf = TestSupport.newPDF();
            Table table = new Table().setTableData(rowsWith(TestSupport.helvetica(pdf), -1), 1)
                    .setLocation(50f, 50f).setBottomMargin(20f);
            for (int r = 1; kept && r < 59; r++) {
                table.keepRowWithNext(r);
            }
            List<Page> pages = new ArrayList<Page>();
            table.drawOn(pdf, pages, Letter.PORTRAIT);
            assertEquals(-1, table.getRowsRendered());
            drawn.add(pages);
        }
        assertEquals(2, drawn.get(1).size());
        for (int r = 1; r < 60; r++) {
            assertEquals(pageOf(drawn.get(0), "r" + r), pageOf(drawn.get(1), "r" + r), "row " + r);
        }
    }

    // The y of the baseline of the text on the page, or NaN.
    private static float yOf(Page page, String text) {
        java.util.regex.Matcher m = java.util.regex.Pattern.compile(
                "[-0-9.]+ ([-0-9.]+) Td\\n(?:/F\\d+ [0-9.]+ Tf\\n)?\\[<" + TestSupport.hex(text) + ">\\] TJ")
                .matcher(TestSupport.content(page));
        return m.find() ? Float.parseFloat(m.group(1)) : Float.NaN;
    }

    // A table of 60 rows, a header row, the rows "r1" to "r58", and a footer
    // row with the texts given, drawn on as many pages as it needs.
    private static List<Page> withFooter(PDF pdf, String... footer) throws Exception {
        List<List<Cell>> data = rowsWith(TestSupport.helvetica(pdf), 59, footer);
        Table table = new Table().setTableData(data, 1).setNumberOfFooterRows(1)
                .setLocation(50f, 50f).setBottomMargin(20f);
        List<Page> pages = new ArrayList<Page>();
        table.drawOn(pdf, pages, Letter.PORTRAIT);
        assertEquals(-1, table.getRowsRendered());
        return pages;
    }

    @Test
    void theFooterRowsAreDrawnUnderTheLastRowOfEveryPage() throws Exception {
        List<Page> pages = withFooter(TestSupport.newPDF(), "total", "sum");
        assertEquals(2, pages.size());
        for (int r = 1; r < 59; r++) {
            int n = 0;
            for (Page page : pages) {
                n += count(TestSupport.content(page), "<" + TestSupport.hex("r" + r) + ">");
            }
            assertEquals(1, n, "row " + r + " is drawn " + n + " times");
        }
        float rowHeight = yOf(pages.get(0), "r1") - yOf(pages.get(0), "r2");
        for (int i = 0; i < pages.size(); i++) {
            Page page = pages.get(i);
            int last = 0;
            for (int r = 1; r < 59; r++) {
                if (!Float.isNaN(yOf(page, "r" + r))) {
                    last = r;
                }
            }
            assertEquals(rowHeight, yOf(page, "r" + last) - yOf(page, "total"), 0.02f,
                    "page " + i + ": the footer is not under row " + last);
            // The bottom of the footer, 4.7 under the baseline, is over the
            // bottom margin.
            assertTrue(yOf(page, "total") - 4.7f >= 20f - 0.01f, "page " + i + ": the footer is in the margin");
        }
    }

    @Test
    void aFooterRowThatWrapsIsDrawnWholeOnEveryPage() throws Exception {
        List<Page> pages = withFooter(TestSupport.newPDF(), LONG_TEXT, "sum");
        assertEquals(2, pages.size());
        for (Page page : pages) {
            String content = TestSupport.content(page);
            assertTrue(content.contains(TestSupport.hex("one")) && content.contains(TestSupport.hex("twelve")), content);
            assertEquals(1, count(content, TestSupport.hex("sum")), content);
        }
    }

    @Test
    void theFooterRowsBeforeTheEndOfTheTableAreArtifacts() throws Exception {
        java.io.ByteArrayOutputStream bos = new java.io.ByteArrayOutputStream();
        PDF pdf = new PDF(bos, Compliance.PDF_UA_1);
        pdf.setTitle("Title");
        List<Page> pages = withFooter(pdf, "total", "sum");
        assertEquals(2, pages.size());
        for (int i = 0; i < pages.size(); i++) {
            String content = TestSupport.content(pages.get(i));
            int at = content.indexOf(TestSupport.hex("total"));
            boolean artifact = content.lastIndexOf("/Artifact BMC", at) > content.lastIndexOf("EMC", at);
            assertEquals(i < pages.size() - 1, artifact, "page " + i);
        }
        for (Page page : pages) {
            pdf.addPage(page);
        }
        pdf.complete();
        String raw = TestSupport.latin1(bos.toByteArray());
        assertEquals(1, count(raw, "/S /Table\n"), raw);
        assertEquals(60, count(raw, "/S /TR\n"), raw);
        assertEquals(2, count(raw, "/S /TH\n"), raw);
        assertEquals(118, count(raw, "/S /TD\n"), raw);
    }

    @Test
    void measuringATableWithFooterRowsReturnsTheCornerDrawingDoes() throws Exception {
        PDF pdf = TestSupport.newPDF();
        List<List<Cell>> data = rows(TestSupport.helvetica(pdf), 5, 3);
        data.get(4).get(0).setText("total");
        Table table = new Table().setTableData(data, 1).setNumberOfFooterRows(1).setLocation(20f, 20f);
        float[] measured = table.drawOn((Page) null);
        Page page = new Page(pdf, Letter.PORTRAIT);
        TestSupport.assertXY(measured[0], measured[1], table.drawOn(page));
        TestSupport.assertXY(245f, 109.36f, measured);
        assertEquals(1, count(TestSupport.content(page), TestSupport.hex("total")));
    }

    @Test
    void theFooterSumsAreTheTotalsOfThePageAndOfThePagesUpToIt() throws Exception {
        // Rows r1 to r57 hold r + 0.25 in their second cell, but for row 10,
        // which holds no number, and the two footer rows the page total and
        // the total carried forward.
        PDF pdf = TestSupport.newPDF();
        List<List<Cell>> data = rowsWith(TestSupport.helvetica(pdf), -1);
        for (int r = 1; r < 59; r++) {
            data.get(r).get(1).setText(r == 10 ? "n/a" : r + ".25");
        }
        data.get(58).get(0).setText("page");
        data.get(59).get(0).setText("carried");
        Table table = new Table().setTableData(data, 1).setNumberOfFooterRows(2)
                .setPageSum(58, 1, 2).setRunningSum(59, 1, 2)
                .setLocation(50f, 50f).setBottomMargin(20f);
        // Until it is drawn, a cell has the sum of all the rows.
        assertEquals("1,657.00", table.getCellAt(58, 1).getText());
        List<Page> pages = new ArrayList<Page>();
        table.drawOn(pdf, pages, Letter.PORTRAIT);
        assertEquals(2, pages.size());
        long carried = 0;
        for (Page page : pages) {
            long total = 0;
            for (int r = 1; r < 58; r++) {
                if (r != 10 && !Float.isNaN(yOf(page, "r" + r))) {
                    total += 100 * r + 25;
                }
            }
            carried += total;
            String content = TestSupport.content(page);
            assertTrue(content.contains("<" + TestSupport.hex(Table.formatSum(total, 2)) + ">"),
                    "no page total " + Table.formatSum(total, 2));
            assertTrue(content.contains("<" + TestSupport.hex(Table.formatSum(carried, 2)) + ">"),
                    "no total carried " + Table.formatSum(carried, 2));
        }
        // Rows 1 to 57 but row 10: 1,643 and 56 quarters.
        assertEquals(164300 + 1400, carried);
    }

    @Test
    void aSumReadsTheNumbersAsRightAlignNumbersDoes() {
        assertEquals(Long.valueOf(123450), Table.numberOf("1,234.50", 2));
        assertEquals(Long.valueOf(-123450), Table.numberOf("(1,234.50)", 2));
        assertEquals(Long.valueOf(-5), Table.numberOf(" -5 ", 0));
        assertEquals(Long.valueOf(1500), Table.numberOf("1.5E+3", 0));
        assertEquals(Long.valueOf(15), Table.numberOf("1.5e1", 0));
        assertEquals(Long.valueOf(1234), Table.numberOf("1'234", 0));
        assertEquals(Long.valueOf(50), Table.numberOf(".5", 2));
        // Halves away from zero.
        assertEquals(Long.valueOf(13), Table.numberOf("0.125", 2));
        assertEquals(Long.valueOf(-13), Table.numberOf("-0.125", 2));
        assertEquals(Long.valueOf(0), Table.numberOf("0.004", 2));
        assertEquals(Long.valueOf(1), Table.numberOf("0.5", 0));
        assertEquals(null, Table.numberOf("1.234.567", 0));
        assertEquals(null, Table.numberOf("n/a", 0));
        assertEquals(null, Table.numberOf("", 0));
        assertEquals(null, Table.numberOf("1234567890123456789", 0));
        assertEquals(null, Table.numberOf("1E99999", 0));
        assertEquals("1,234.50", Table.formatSum(123450, 2));
        assertEquals("-1,234,567", Table.formatSum(-1234567, 0));
        assertEquals("0.05", Table.formatSum(5, 2));
        assertEquals("-0.05", Table.formatSum(-5, 2));
        assertEquals("0.00", Table.formatSum(0, 2));
        assertEquals("999", Table.formatSum(999, 0));
        assertEquals("1,000", Table.formatSum(1000, 0));
    }

    @Test
    void theTotalBroughtForwardIsTheTotalCarriedFromThePageBefore() throws Exception {
        // A header row, a header row that brings the total forward, the rows
        // r2 to r58 with r + 0.25 in their second cell, and a footer row that
        // carries the total forward.
        PDF pdf = TestSupport.newPDF();
        List<List<Cell>> data = rowsWith(TestSupport.helvetica(pdf), -1);
        for (int r = 2; r < 59; r++) {
            data.get(r).get(1).setText(r + ".25");
        }
        data.get(1).get(0).setText("brought");
        data.get(59).get(0).setText("carried");
        Table table = new Table().setTableData(data, 2).setNumberOfFooterRows(1)
                .setBroughtForwardSum(1, 1, 2).setRunningSum(59, 1, 2)
                .setLocation(50f, 50f).setBottomMargin(20f);
        List<Page> pages = new ArrayList<Page>();
        table.drawOn(pdf, pages, Letter.PORTRAIT);
        assertEquals(2, pages.size());
        assertTrue(Float.isNaN(yOf(pages.get(0), "brought")), "the first page brings a total forward");
        long carried = 0;
        for (int r = 2; r < 59; r++) {
            if (!Float.isNaN(yOf(pages.get(0), "r" + r))) {
                carried += 100 * r + 25;
            }
        }
        String next = TestSupport.content(pages.get(1));
        String text = "<" + TestSupport.hex(Table.formatSum(carried, 2)) + ">";
        assertTrue(TestSupport.content(pages.get(0)).contains(text), "the first page does not carry " + text);
        assertTrue(next.contains(text), "the second page does not bring " + text + " forward");
        // Under the header row, and over the rows of the page.
        float rowHeight = yOf(pages.get(0), "r2") - yOf(pages.get(0), "r3");
        assertEquals(rowHeight, yOf(pages.get(1), "r0") - yOf(pages.get(1), "brought"), 0.02f);
        int firstRow = 2;
        while (Float.isNaN(yOf(pages.get(1), "r" + firstRow))) {
            firstRow++;
        }
        assertEquals(rowHeight, yOf(pages.get(1), "brought") - yOf(pages.get(1), "r" + firstRow), 0.02f);
        // The rows are each drawn once, and the total is that of all of them.
        for (int r = 2; r < 59; r++) {
            int n = 0;
            for (Page page : pages) {
                n += count(TestSupport.content(page), "<" + TestSupport.hex("r" + r) + ">");
            }
            assertEquals(1, n, "row " + r);
        }
        // Rows 2 to 58: 1,710 and 57 quarters.
        assertTrue(next.contains("<" + TestSupport.hex(Table.formatSum(171000 + 1425, 2)) + ">"), "no total");
    }

    // The operator that sets the fill color, as a page writes it.
    private static String fillOperator(PDF pdf, int color) throws Exception {
        Page page = new Page(pdf, Letter.PORTRAIT);
        page.setBrushColor(color);
        return TestSupport.content(page).trim();
    }

    @Test
    void everyOtherRowOfTheBodyIsStriped() throws Exception {
        // A header row, the rows r1 to r6 of the body, of which r4 wraps, and
        // a cell of r2 with a background of its own.
        PDF pdf = TestSupport.newPDF();
        Font font = TestSupport.helvetica(pdf);
        List<List<Cell>> data = rowsWith(font, 4, LONG_TEXT, "x");
        data = new ArrayList<List<Cell>>(data.subList(0, 7));
        data.get(2).get(1).setBackgroundColor(0xFF0000);
        Table table = new Table().setTableData(data, 1).setAlternateRowColor(0x336699).setLocation(50f, 50f);
        Page page = new Page(pdf, Letter.PORTRAIT);
        table.drawOn(page);
        // r2, the lines of r4 and r6, two cells each, but the cell of r2 that
        // has a background of its own.
        int r4Lines = 0;
        for (int r = 0; ; r++) {
            try {
                List<Cell> row = table.getRow(r);
                if (r > 4 && (row.get(0).properties & Cell.CONTINUED) != 0) {
                    r4Lines++;
                }
            } catch (IndexOutOfBoundsException e) {
                break;
            }
        }
        assertTrue(r4Lines > 0, "the text did not wrap");
        String content = TestSupport.content(page);
        assertEquals(2 * (1 + 1 + r4Lines + 1) - 1, count(content, fillOperator(pdf, 0x336699)), content);
        assertEquals(1, count(content, fillOperator(pdf, 0xFF0000)));
        // The cells are as they were.
        assertEquals(null, table.getCellAt(2, 0).getBackgroundColor());
    }

    @Test
    void theHeaderAndTheFooterRowsHaveTheirStyle() throws Exception {
        PDF pdf = TestSupport.newPDF();
        Font font = TestSupport.helvetica(pdf);
        Font bold = new Font(pdf, CoreFont.HELVETICA_BOLD).setSize(9f);
        List<List<Cell>> data = rowsWith(font, -1);
        Table table = new Table().setTableData(data, 1).setNumberOfFooterRows(1)
                .setHeaderRowStyle(bold, Color.white, Color.darkblue)
                .setFooterRowStyle(null, Color.transparent, Color.lightgray);
        Cell header = table.getCellAt(0, 1);
        assertSame(bold, header.getFont());
        assertEquals(9f, header.fontSize, 0f);
        TestSupport.assertRGB(1f, 1f, 1f, header.getTextColor());
        assertEquals(Color.darkblue, Util.toPackedRGB(header.getBackgroundColor()));
        // The footer keeps its font and its text color, and the body is as it was.
        Cell footer = table.getCellAt(59, 0);
        assertSame(font, footer.getFont());
        TestSupport.assertRGB(0f, 0f, 0f, footer.getTextColor());
        assertEquals(Color.lightgray, Util.toPackedRGB(footer.getBackgroundColor()));
        assertEquals(null, table.getCellAt(1, 0).getBackgroundColor());
        assertSame(font, table.getCellAt(1, 0).getFont());
    }

    @Test
    void theColumnsShareTheWidthOfTheTable() throws Exception {
        PDF pdf = TestSupport.newPDF();
        Font font = TestSupport.helvetica(pdf);
        // Two of four columns have percentages, and the other two share what
        // is left of 100; in either order of the calls.
        Table table = new Table().setTableData(rows(font, 3, 4), 1)
                .setColumnWidthsInPercent(10f, 40f).setWidth(500f);
        assertWidths(table, 50f, 200f, 125f, 125f);
        table = new Table().setTableData(rows(font, 3, 4), 1)
                .setWidth(500f).setColumnWidthsInPercent(10f, 40f);
        assertWidths(table, 50f, 200f, 125f, 125f);
        // Percentages that do not add up to 100 are shares.
        table = new Table().setTableData(rows(font, 3, 2), 1).setColumnWidthsInPercent(30f, 90f).setWidth(400f);
        assertWidths(table, 100f, 300f);
        // fitToWidth, and setWidth with no percentages, share it by the widths.
        table = new Table().setTableData(rows(font, 3, 2), 1).setColumnWidth(0, 60f).setColumnWidth(1, 120f);
        assertWidths(table.fitToWidth(360f), 120f, 240f);
        assertWidths(table.setWidth(90f), 30f, 60f);
        assertEquals(90f, table.getWidth(), 0.001f);
    }

    private static void assertWidths(Table table, float... widths) {
        for (int i = 0; i < widths.length; i++) {
            assertEquals(widths[i], table.getColumnWidth(i), 0.001f, "column " + i);
            for (int r = 0; r < 3; r++) {
                assertEquals(widths[i], table.getCellAt(r, i).getWidth(), 0.001f, "row " + r + ", column " + i);
            }
        }
    }

    @Test
    void aColumnAsWideAsItsTextDoesNotWrapIt() throws Exception {
        // In IBM Plex Sans Bold at 11 points, the width that
        // autoAdjustColumnWidths gives the column of "a", less the padding,
        // comes out a little less than the width of "a" in floating point.
        PDF pdf = TestSupport.newPDF();
        Font font = new Font(pdf, TestSupport.open("fonts/IBMPlexSans/IBMPlexSans-Bold.otf.stream")).setSize(11f);
        List<List<Cell>> rows = new ArrayList<List<Cell>>();
        rows.add(new ArrayList<Cell>(Arrays.asList(new Cell(font, "a"), new Cell(font, "b"))));
        rows.add(new ArrayList<Cell>(Arrays.asList(new Cell(font, "1"), new Cell(font, "2"))));
        Table table = new Table().setTableData(rows, 1);
        table.autoAdjustColumnWidths();
        assertEquals(1, table.getNumVerCells(table.getRow(0), 0));
        List<Page> pages = new ArrayList<Page>();
        table.drawOn(pdf, pages, Letter.PORTRAIT);
        assertEquals(1, pages.size());
    }

    @Test
    void aWordWiderThanItsColumnHasNoEmptyLine() throws Exception {
        PDF pdf = TestSupport.newPDF();
        Font font = TestSupport.helvetica(pdf);
        Cell cell = new Cell(font, "ab");
        cell.setWidth(cell.getLeftPadding() + cell.getRightPadding() + 1f);
        List<List<Cell>> rows = new ArrayList<List<Cell>>();
        rows.add(new ArrayList<Cell>(Arrays.asList(cell)));
        Table table = new Table().setTableData(rows, 0);
        // A line for each letter, and none before them.
        assertEquals(2, table.getNumVerCells(table.getRow(0), 0));
        List<Page> pages = new ArrayList<Page>();
        table.drawOn(pdf, pages, Letter.PORTRAIT);
        assertEquals(1, pages.size());
    }

    @Test
    void aWordBrokenToFitAColumnKeepsItsCharactersWhole() throws Exception {
        // A character outside the basic plane is two chars of the text, and
        // the two are not drawn on two lines.
        PDF pdf = TestSupport.newPDF();
        Font font = new Font(pdf, TestSupport.open("fonts/IBMPlexSans/IBMPlexSans-Regular.otf.stream")).setSize(11f);
        Cell cell = new Cell(font, "a\uD83D\uDE00b");
        // A column narrower than any of the characters.
        cell.setWidth(6f);
        List<List<Cell>> rows = new ArrayList<List<Cell>>();
        rows.add(new ArrayList<Cell>(Arrays.asList(cell)));
        Table table = new Table().setTableData(rows, 0);
        table.wrapAroundCellText();
        List<String> lines = new ArrayList<String>();
        for (int i = 0; i < 3; i++) {
            lines.add(table.getRow(i).get(0).getText());
        }
        assertEquals(Arrays.asList("a", "\uD83D\uDE00", "b"), lines);
    }

    @Test
    void aLineBrokenBeforeAWordWiderThanItsColumnEndsWithoutTheSpace() throws Exception {
        // The second word does not fit the column, and its first character does
        // not fit after the first word, so the first line is the first word.
        PDF pdf = TestSupport.newPDF();
        Font font = new Font(pdf, TestSupport.open("fonts/IBMPlexSans/IBMPlexSans-Regular.otf.stream")).setSize(10f);
        Cell cell = new Cell(font, "abcd Wxyzwxyzw");
        cell.setWidth(30f);
        List<List<Cell>> rows = new ArrayList<List<Cell>>();
        rows.add(new ArrayList<Cell>(Arrays.asList(cell)));
        Table table = new Table().setTableData(rows, 0);
        table.wrapAroundCellText();
        assertEquals("abcd", table.getRow(0).get(0).getText());
    }
}
