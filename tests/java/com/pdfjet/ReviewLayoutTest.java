/*
 * ReviewLayoutTest.java
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
package com.pdfjet;

import static org.junit.jupiter.api.Assertions.assertArrayEquals;
import static org.junit.jupiter.api.Assertions.assertEquals;
import static org.junit.jupiter.api.Assertions.assertFalse;
import static org.junit.jupiter.api.Assertions.assertNotNull;
import static org.junit.jupiter.api.Assertions.assertNotSame;
import static org.junit.jupiter.api.Assertions.assertNull;
import static org.junit.jupiter.api.Assertions.assertSame;
import static org.junit.jupiter.api.Assertions.assertThrows;
import static org.junit.jupiter.api.Assertions.assertTrue;

import java.io.ByteArrayInputStream;
import java.io.ByteArrayOutputStream;
import java.io.File;
import java.io.FileOutputStream;
import java.io.IOException;
import java.io.OutputStream;
import java.nio.charset.StandardCharsets;
import java.util.ArrayList;
import java.util.LinkedHashMap;
import java.util.List;
import java.util.Map;
import java.util.regex.Matcher;
import java.util.regex.Pattern;
import org.junit.jupiter.api.Test;
import org.junit.jupiter.api.io.TempDir;

/**
 * The tests of the text, table and Markdown layout, from a review of them.
 */
class ReviewLayoutTest {
    @TempDir
    File tempDir;

    private static final String[] HEADER = {"Name", "City", "Total"};

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

    // The first count rows like those of BigTableTest.
    private static List<String[]> bigTableRows(int count) {
        List<String[]> rows = new ArrayList<String[]>();
        for (int i = 0; i < count; i++) {
            rows.add(new String[] {"n" + i, "City, " + i, i + ".5"});
        }
        return rows;
    }

    private static int count(String str, String text) {
        int count = 0;
        for (int i = str.indexOf(text); i != -1; i = str.indexOf(text, i + text.length())) {
            count++;
        }
        return count;
    }

    // The lowest baseline of the text the page draws, in the coordinates of
    // the PDF, which grow upwards from the bottom of the page.
    private static float lowestBaseline(Page page) {
        float lowest = page.height;
        Matcher m = Pattern.compile("[-0-9.]+ ([-0-9.]+) Td\n").matcher(TestSupport.content(page));
        while (m.find()) {
            lowest = Math.min(lowest, Float.parseFloat(m.group(1)));
        }
        return lowest;
    }

    private File write(String name, String text) throws IOException {
        File file = new File(tempDir, name);
        OutputStream out = new FileOutputStream(file);
        try {
            out.write(text.getBytes(StandardCharsets.UTF_8));
        } finally {
            out.close();
        }
        return file;
    }

    private static String repeat(String text, int count) {
        StringBuilder buf = new StringBuilder();
        for (int i = 0; i < count; i++) {
            buf.append(text);
        }
        return buf.toString();
    }

    private static PDF taggedPDF(ByteArrayOutputStream bos) throws Exception {
        PDF pdf = new PDF(bos, Compliance.PDF_UA_1);
        pdf.setTitle("Title");
        return pdf;
    }

    @Test
    void textFrameBreaksALongWordInLinearTime() throws Exception {
        // The rest of the word was measured whole for every row it was broken
        // into: 100,000 characters took 75 seconds.
        PDF pdf = TestSupport.newPDF();
        Font font = TestSupport.helvetica(pdf);
        List<String> text = new ArrayList<String>();
        text.add(repeat("a", 200000));
        TextFrame frame = new TextFrame(font, text);
        frame.setLocation(50f, 50f);
        frame.setWidth(400f);
        long start = System.nanoTime();
        frame.drawOn(new Page(pdf, Letter.PORTRAIT, Page.DETACHED));
        long took = (System.nanoTime() - start) / 1000000L;
        assertTrue(took < 3000L, "took " + took + " ms");
        assertFalse(frame.hasMoreText(), "the word is not all drawn");
    }

    @Test
    void markdownQuotesNestedDeeplyKeepTheTextWide() throws Exception {
        // Quotes nested so deeply that the text was narrower than nothing drew
        // one character on each row.
        PDF pdf = TestSupport.newPDF();
        Font font = TestSupport.helvetica(pdf);
        List<Page> pages = new ArrayList<Page>();
        long start = System.nanoTime();
        new Markdown(font, font, font, font, font).drawOn(pdf, repeat(">", 20000), pages, Letter.PORTRAIT);
        long took = (System.nanoTime() - start) / 1000000L;
        assertTrue(took < 3000L, "took " + took + " ms");
        assertTrue(pages.size() <= 40, pages.size() + " pages for 20,000 characters");
    }

    @Test
    void aRunningSumIsAddedUpInLinearTime() throws Exception {
        // The sums were added up again from the first row for every page, with
        // the numbers read again: 40,000 rows took 33 seconds.
        PDF pdf = TestSupport.newPDF();
        Font font = TestSupport.helvetica(pdf);
        int count = 40000;
        List<List<Cell>> data = rows(font, count, 2);
        for (int r = 1; r < count; r++) {
            data.get(r).get(1).setText("1,234.50");
        }
        Table table = new Table().setTableData(data, 1);
        table.setNumberOfFooterRows(1);
        table.setRunningSum(count - 1, 1, 2);
        table.setLocation(20f, 20f);
        List<Page> pages = new ArrayList<Page>();
        long start = System.nanoTime();
        table.drawOn(pdf, pages, Letter.PORTRAIT);
        long took = (System.nanoTime() - start) / 1000000L;
        assertTrue(took < 5000L, "took " + took + " ms");
        // The footer row is the last row, which has no number of its own.
        assertTrue(TestSupport.content(pages.get(pages.size() - 1)).contains(TestSupport.hex("49,377,531.00")),
                "the running sum of the last page is not the total");
    }

    @Test
    void aSumReadsANumberAsRightAlignNumbersDoes() {
        // A number with a line break or a control character after it is a
        // number to isNumber, which trims them.
        for (String text : new String[] {"100\n", "100\u0001", " 100 ", "(100)"}) {
            long want = text.contains("(") ? -100L : 100L;
            assertEquals(Long.valueOf(want), Table.numberOf(text, 0), text);
        }
    }

    @Test
    void aRowSpanCountsTheRowsOfTheTable() throws Exception {
        // The span of a cell over a row that wraps to four lines was written
        // as the five rows of the drawing, and not as the two rows of the table.
        ByteArrayOutputStream bos = new ByteArrayOutputStream();
        PDF pdf = taggedPDF(bos);
        Font font = TestSupport.helvetica(pdf);
        List<List<Cell>> data = new ArrayList<List<Cell>>();
        List<Cell> row = new ArrayList<Cell>();
        row.add(new Cell(font, "H1"));
        row.add(new Cell(font, "H2"));
        data.add(row);
        row = new ArrayList<Cell>();
        row.add(new Cell(font, "span").setRowSpan(2));
        row.add(new Cell(font, "a note long enough to wrap to four lines"));
        data.add(row);
        row = new ArrayList<Cell>();
        row.add(new Cell(font, ""));
        row.add(new Cell(font, "x"));
        data.add(row);
        row = new ArrayList<Cell>();
        row.add(new Cell(font, "y"));
        row.add(new Cell(font, "z"));
        data.add(row);
        Table table = new Table().setTableData(data, 1);
        table.setColumnWidth(0, 60f).setColumnWidth(1, 60f);
        table.setLocation(20f, 20f);
        table.drawOn(new Page(pdf, Letter.PORTRAIT));
        pdf.complete();
        String raw = TestSupport.latin1(bos.toByteArray());
        List<String> spans = new ArrayList<String>();
        Matcher m = Pattern.compile("/RowSpan \\d+").matcher(raw);
        while (m.find()) {
            spans.add(m.group());
        }
        assertEquals("[/RowSpan 2]", spans.toString());
        assertEquals(4, count(raw, "/S /TR\n"));
    }

    @Test
    void aRowThatDoesNotFitUnderAHeadingGoesToTheNextPage() throws Exception {
        // The first row of the first page was drawn where it was, past the
        // bottom of the page, when it did not fit under what was above the table.
        PDF pdf = TestSupport.newPDF();
        Font font = TestSupport.helvetica(pdf);
        Page first = new Page(pdf, Letter.PORTRAIT, Page.DETACHED);
        List<List<Cell>> data = rows(font, 5, 1);
        data.get(1).get(0).setText("one two three four five six seven eight nine ten eleven twelve");
        Table table = new Table().setTableData(data, 1);
        table.setColumnWidth(0, 60f);
        table.setLocation(20f, 20f);
        table.setBottomMargin(20f);
        table.setFirstPageTopMargin(720f);
        List<Page> pages = new ArrayList<Page>();
        pages.add(first);
        table.drawOn(pdf, first, pages, Letter.PORTRAIT);
        assertFalse(TestSupport.content(first).contains(TestSupport.hex("one")), "the row is drawn on the first page");
        assertEquals(2, pages.size());
        assertTrue(TestSupport.content(pages.get(1)).contains(TestSupport.hex("twelve")));
        for (Page page : pages) {
            assertTrue(lowestBaseline(page) >= 20f, "text under the bottom margin");
        }
    }

    @Test
    void aRowSpanTallerThanAPageIsCutBetweenItsRows() throws Exception {
        // A cell that spans rows taller than a page drew them all on one page,
        // past its bottom.
        PDF pdf = TestSupport.newPDF();
        Font font = TestSupport.helvetica(pdf);
        List<List<Cell>> data = rows(font, 80, 2);
        data.get(3).get(0).setRowSpan(70);
        Table table = new Table().setTableData(data, 1);
        table.setLocation(20f, 20f);
        table.setBottomMargin(20f);
        List<Page> pages = new ArrayList<Page>();
        table.drawOn(pdf, pages, Letter.PORTRAIT);
        assertTrue(pages.size() >= 2, pages.size() + " pages");
        int drawn = 0;
        int spanning = 0;
        for (Page page : pages) {
            assertTrue(lowestBaseline(page) >= 20f, "text under the bottom margin");
            String content = TestSupport.content(page);
            for (int r = 1; r < 80; r++) {
                drawn += count(content, "<" + TestSupport.hex("r" + r + "c1") + ">");
            }
            spanning += count(content, "<" + TestSupport.hex("r3c0") + ">");
        }
        assertEquals(79, drawn);
        // The spanning cell draws its text once, and its box on each page.
        assertEquals(1, spanning);
    }

    @Test
    void aTableWithNoRowsLeftEndsWhereItStarts() throws Exception {
        PDF pdf = TestSupport.newPDF();
        Font font = TestSupport.helvetica(pdf);
        Table table = new Table().setTableData(rows(font, 3, 2), 1);
        table.setLocation(20f, 30f);
        List<Page> pages = new ArrayList<Page>();
        table.drawOn(pdf, pages, Letter.PORTRAIT);
        float[] xy = table.drawOn(pdf, pages, Letter.PORTRAIT);
        assertEquals(1, pages.size());
        TestSupport.assertXY(20f + table.getWidth(), 30f, xy);
    }

    @Test
    void aColumnSpanIsWithinTheRow() throws Exception {
        // A column span of 0 hung the drawing, and one past the end of the row
        // ran past the cells of the row.
        for (int colspan : new int[] {0, -3, 5}) {
            PDF pdf = TestSupport.newPDF();
            Font font = TestSupport.helvetica(pdf);
            List<List<Cell>> data = rows(font, 3, 3);
            for (List<Cell> row : data) {
                for (Cell cell : row) {
                    cell.setBorders(true);
                }
            }
            data.get(1).get(1).setColSpan(colspan);
            Table table = new Table().setTableData(data, 1);
            table.setLocation(20f, 20f);
            table.drawOn(new Page(pdf, Letter.PORTRAIT));
            if (colspan < 1) {
                assertEquals(1, data.get(1).get(1).getColSpan());
            }
        }
    }

    @Test
    void aListItemGoesOnInTheNextFrameWithoutItsLabel() throws Exception {
        // The label of an item was drawn again, as a new item, in every frame
        // the item went on into.
        ByteArrayOutputStream bos = new ByteArrayOutputStream();
        PDF pdf = taggedPDF(bos);
        Font font = TestSupport.helvetica(pdf);
        Paragraph item = new Paragraph().add(new TextLine(font, repeat("word ", 400)));
        item.setListLabel(new TextLine(font, "LABEL"), 15f);
        List<Paragraph> paragraphs = new ArrayList<Paragraph>();
        paragraphs.add(item);
        TextFrame frame = new TextFrame(paragraphs);
        frame.setLocation(50f, 50f);
        frame.setWidth(200f).setHeight(200f);
        List<Page> pages = new ArrayList<Page>();
        frame.drawOn(pdf, pages, Letter.PORTRAIT);
        assertTrue(pages.size() >= 2, pages.size() + " pages");
        int labels = 0;
        for (Page page : pages) {
            labels += count(TestSupport.content(page), TestSupport.hex("LABEL"));
        }
        assertEquals(1, labels);
        pdf.addPages(pages);
        pdf.complete();
        String raw = TestSupport.latin1(bos.toByteArray());
        assertEquals(1, count(raw, "/S /Lbl\n"));
        assertEquals(pages.size(), count(raw, "/S /LBody\n"));
    }

    @Test
    void aHeadingThatGoesOnInTheNextFrameIsOneBookmark() throws Exception {
        ByteArrayOutputStream bos = new ByteArrayOutputStream();
        PDF pdf = taggedPDF(bos);
        Font font = TestSupport.helvetica(pdf);
        Paragraph heading = new Paragraph().add(new TextLine(font, repeat("heading ", 200)));
        heading.setStructureType(StructElem.H1);
        List<Paragraph> paragraphs = new ArrayList<Paragraph>();
        paragraphs.add(heading);
        TextFrame frame = new TextFrame(paragraphs);
        frame.setLocation(50f, 50f);
        frame.setWidth(200f).setHeight(100f);
        List<Page> pages = new ArrayList<Page>();
        frame.drawOn(pdf, pages, Letter.PORTRAIT);
        assertTrue(pages.size() >= 2, pages.size() + " pages");
        assertEquals(1, pdf.headings.size());
    }

    @Test
    void aTextColumnDrawsTheLabelOfAListItem() throws Exception {
        // The label that Paragraph.setListLabel sets was left out by TextColumn.
        ByteArrayOutputStream bos = new ByteArrayOutputStream();
        PDF pdf = taggedPDF(bos);
        Font font = TestSupport.helvetica(pdf);
        Page page = new Page(pdf, Letter.PORTRAIT);
        TextColumn column = new TextColumn();
        column.setLocation(50f, 50f);
        column.setWidth(300f);
        for (String text : new String[] {"first item", "second item"}) {
            Paragraph item = new Paragraph().add(new TextLine(font, text));
            item.setListLabel(new TextLine(font, "LABEL"), 15f);
            column.addParagraph(item);
        }
        column.addParagraph(new Paragraph().add(new TextLine(font, "after the list")));
        column.drawOn(page);
        String content = TestSupport.content(page);
        assertEquals(2, count(content, TestSupport.hex("LABEL")));
        float[] label = TestSupport.positionOf(content, "LABEL");
        float[] text = TestSupport.positionOf(content, "first");
        assertEquals(35f, label[0], 0.01f);
        assertEquals(text[1], label[1], 0.01f);
        pdf.complete();
        String raw = TestSupport.latin1(bos.toByteArray());
        assertEquals("1 L, 2 LI, 2 Lbl, 2 LBody, 3 P", count(raw, "/S /L\n") + " L, "
                + count(raw, "/S /LI\n") + " LI, " + count(raw, "/S /Lbl\n") + " Lbl, "
                + count(raw, "/S /LBody\n") + " LBody, " + count(raw, "/S /P\n") + " P");
    }

    @Test
    void aTextFrameInTheBottomHalfOfThePageNeedsAHeight() throws Exception {
        // The height the frame took on each page was less than nothing, so it
        // drew all of its text on the first page, past the bottom.
        PDF pdf = TestSupport.newPDF();
        Font font = TestSupport.helvetica(pdf);
        List<String> text = new ArrayList<String>();
        text.add(repeat("word ", 1000));
        final TextFrame frame = new TextFrame(font, text);
        frame.setLocation(50f, 500f);
        frame.setWidth(200f);
        final PDF document = pdf;
        final List<Page> pages = new ArrayList<Page>();
        IllegalStateException e = assertThrows(IllegalStateException.class,
                () -> frame.drawOn(document, pages, Letter.PORTRAIT));
        assertEquals("The text frame has no height and is in the bottom half of the page: "
                + "set its height, or put it higher on the page.", e.getMessage());
        assertEquals(0, pages.size());
    }

    // Reads the first record of the text, as the data file readers do.
    private static String[] firstRecord(String text) throws IOException {
        UTF8.LineReader reader = new UTF8.LineReader(
                new ByteArrayInputStream(text.getBytes(StandardCharsets.UTF_8)));
        return Util.readRecord(reader.readLine(), reader, ",");
    }

    @Test
    void aRecordOfManyLinesIsReadInLinearTime() throws Exception {
        // Every line closes a quoted field and opens the next, and the record
        // was split again at each line.
        StringBuilder text = new StringBuilder("\"a");
        String xs = repeat("x", 1000);
        for (int i = 0; i < 5000; i++) {
            text.append('\n').append(xs).append("\",\"");
        }
        text.append("\nend\"");
        long start = System.nanoTime();
        String[] fields = firstRecord(text.toString());
        long took = (System.nanoTime() - start) / 1000000L;
        assertTrue(took < 3000L, "took " + took + " ms");
        assertEquals(5001, fields.length);
        assertEquals("a " + xs, fields[0]);
        assertEquals(" end", fields[5000]);
        // A quote after the delimiter opens a field, and one inside a field
        // that does not start with one is text.
        assertArrayEquals(new String[] {"a b", "c\"d", "e f"}, firstRecord("\"a\nb\",c\"d,\"e\nf\""));
    }

    @Test
    void aBigTableThrowsTheErrorOfAQuoteThatIsNotClosed() throws Exception {
        final File file = write("open.csv", "A,B\n1,\"2\n3,4\n");
        PDF pdf = TestSupport.newPDF();
        Font font = TestSupport.helvetica(pdf);
        final BigTable table = new BigTable(pdf, font, font, Letter.PORTRAIT).setNumberOfColumns(2);
        IllegalArgumentException e = assertThrows(IllegalArgumentException.class,
                () -> table.setTableData(file.getPath(), ","));
        assertEquals("A quoted field is not closed by the end of the data file: 1,\"2\n3,4", e.getMessage());
    }

    @Test
    void theLinesOfADataFileEndAtACarriageReturnToo() throws Exception {
        // A carriage return alone ended a line in Java and C#, and not in Go
        // and Swift.
        File file = write("cr.csv", "A,B\r11,22\r\n33,44\n55,66\r");
        PDF pdf = TestSupport.newPDF();
        Font font = TestSupport.helvetica(pdf);
        Table table = new Table(font, font, file.getPath());
        // The fourth row is the last.
        assertSame(table.getCellAt(-1, 0), table.getCellAt(3, 0));
        assertEquals("55 66", table.getCellAt(3, 0).getText() + " " + table.getCellAt(3, 1).getText());
        BigTable bigTable = new BigTable(pdf, font, font, Letter.PORTRAIT).setNumberOfColumns(2)
                .setTableData(file.getPath(), ",");
        bigTable.setLocation(10f, 10f);
        bigTable.complete();
        String content = TestSupport.content(bigTable.getPages().get(0));
        for (String text : new String[] {"11", "22", "33", "44", "55", "66"}) {
            assertTrue(content.contains("<" + TestSupport.hex(text) + ">"), text);
        }
    }

    @Test
    void aBigTableThatDoesNotFitOnItsFirstPageStartsOnTheNext() throws Exception {
        // The header and the first row were drawn wherever the first page had
        // them start, even under its bottom margin.
        PDF pdf = TestSupport.newPDF();
        Font font = TestSupport.helvetica(pdf);
        Page first = new Page(pdf, Letter.PORTRAIT);
        BigTable table = new BigTable(pdf, font, font, Letter.PORTRAIT).setNumberOfColumns(3)
                .setTableData(HEADER, bigTableRows(5));
        table.setFirstPage(first, first.height - 15f);
        table.setLocation(10f, 10f);
        table.complete();
        List<Page> pages = table.getPages();
        assertEquals(1, pages.size());
        assertNotSame(first, pages.get(0));
        assertFalse(TestSupport.content(first).contains(TestSupport.hex(HEADER[0])));
        assertTrue(TestSupport.content(pages.get(0)).contains(TestSupport.hex("Page 1 of 1")));
    }

    @Test
    void aBigTableCountsItsFirstPageAtItsOwnHeight() throws Exception {
        // The pages were counted as if the first were of the size of the next.
        PDF pdf = TestSupport.newPDF();
        Font font = TestSupport.helvetica(pdf);
        Page first = new Page(pdf, Letter.LANDSCAPE);
        BigTable table = new BigTable(pdf, font, font, Letter.PORTRAIT).setNumberOfColumns(3)
                .setTableData(HEADER, bigTableRows(200));
        table.setFirstPage(first, 100f);
        table.setLocation(10f, 10f);
        table.complete();
        List<Page> pages = table.getPages();
        String want = "Page " + pages.size() + " of " + pages.size();
        assertTrue(TestSupport.content(pages.get(pages.size() - 1)).contains(TestSupport.hex(want)), want);
    }

    @Test
    void aBigTableWithoutDataDrawsNothing() throws Exception {
        PDF pdf = TestSupport.newPDF();
        Font font = TestSupport.helvetica(pdf);
        BigTable table = new BigTable(pdf, font, font, Letter.PORTRAIT).setNumberOfColumns(2);
        table.complete();
        assertEquals(0, table.getPages().size());
    }

    @Test
    void aBigTableCutsTheTextOfAColumnBetweenCodePoints() throws Exception {
        // A column cut back to fit the page cut its text in UTF-16 units.
        PDF pdf = TestSupport.newPDF();
        Font font = TestSupport.helvetica(pdf);
        String wide = repeat("😀", 400);
        List<String[]> rows = new ArrayList<String[]>();
        rows.add(new String[] {"a", wide});
        BigTable table = new BigTable(pdf, font, font, Letter.PORTRAIT).setNumberOfColumns(2)
                .setTableData(new String[] {"A", "B"}, rows);
        table.setLocation(10f, 10f);
        table.complete();
        assertEquals(1, table.getPages().size());
    }

    @Test
    void aTextBlockWithALeadingOfZeroDrawsItsLines() throws Exception {
        // The lines that fit were the height divided by a leading of 0.
        PDF pdf = TestSupport.newPDF();
        Font font = TestSupport.helvetica(pdf);
        Page page = new Page(pdf, Letter.PORTRAIT);
        TextBlock block = new TextBlock(font, "one two three four five six seven eight nine ten");
        block.setWidth(40f).setHeight(30f).setLineSpacing(0f);
        block.setLocation(10f, 10f);
        block.drawOn(page);
        assertTrue(TestSupport.content(page).contains(TestSupport.hex("ten")), "the last line is not drawn");
        assertEquals(Alignment.LEFT, new TextBlock(font, "x").getTextAlignment());
    }

    @Test
    void markdownCodeInAFontOfSizeZeroIsDrawn() throws Exception {
        PDF pdf = TestSupport.newPDF();
        Font font = TestSupport.helvetica(pdf);
        Font code = new Font(pdf, CoreFont.COURIER).setSize(0f);
        List<Page> pages = new ArrayList<Page>();
        new Markdown(font, font, font, font, code).drawOn(pdf, "```\none\ntwo\n```", pages, Letter.PORTRAIT);
        assertEquals(1, pages.size());
    }

    @Test
    void markdownReadsTheSourceOfAnImageTheSameWayInEveryPort() throws Exception {
        PDF pdf = TestSupport.newPDF();
        Font font = TestSupport.helvetica(pdf);
        String images = TestSupport.file("images").getPath();
        Markdown markdown = new Markdown(font, font, font, font, font).setImageDirectory(images);
        Map<String, Boolean> sources = new LinkedHashMap<String, Boolean>();
        sources.put("linux-logo.png", true);
        sources.put("./linux-logo.png", true);
        sources.put(".//linux-logo.png", true);
        sources.put("linux-logo.png/", false);
        sources.put("linux-logo.png/.", false);
        sources.put("../images/linux-logo.png", false);
        sources.put("x/../linux-logo.png", false);
        sources.put("", false);
        sources.put(".", false);
        sources.put("́/../linux-logo.png", false);
        sources.put("/́linux-logo.png", false);
        sources.put("c:linux-logo.png", false);
        sources.put("linux-logo.png\\..\\x.png", false);
        sources.put("%2E%2E/images/linux-logo.p", false);
        for (Map.Entry<String, Boolean> source : sources.entrySet()) {
            assertEquals(source.getValue(), markdown.imagePath(source.getKey()) != null, source.getKey());
        }
        // An empty directory is the working directory.
        Markdown here = new Markdown(font, font, font, font, font).setImageDirectory("");
        if (new File("images/linux-logo.png").isFile()) {
            assertNotNull(here.imagePath("images/linux-logo.png"));
        }
        assertNull(here.imagePath("linux-logo.png/"));
    }
}
