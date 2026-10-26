/*
 * BigTableTest.java
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
package com.pdfjet;

import static org.junit.jupiter.api.Assertions.assertEquals;
import static org.junit.jupiter.api.Assertions.assertFalse;
import static org.junit.jupiter.api.Assertions.assertThrows;
import static org.junit.jupiter.api.Assertions.assertTrue;

import java.io.File;
import java.io.FileOutputStream;
import java.io.OutputStream;
import java.nio.charset.StandardCharsets;
import java.util.ArrayList;
import java.util.Arrays;
import java.util.List;
import org.junit.jupiter.api.AfterEach;
import org.junit.jupiter.api.Test;
import org.junit.jupiter.api.io.TempDir;

class BigTableTest {
    @AfterEach
    void forbidCoreFonts() {
        TestSupport.allowCoreFonts(false);
    }

    @TempDir
    File tempDir;

    private static final String[] HEADER = {"Name", "City", "Total"};

    // A hundred rows, with a delimiter inside a quoted field, and a short row
    // in the middle, which is skipped.
    private static List<String[]> rows() {
        List<String[]> rows = new ArrayList<String[]>();
        for (int i = 0; i < 100; i++) {
            if (i == 90) {
                rows.add(new String[] {"short"});
            }
            rows.add(new String[] {"n" + i, "City, " + i, i + ".5"});
        }
        return rows;
    }

    private static List<Page> draw(BigTable table) throws Exception {
        table.setLocation(10f, 10f);
        table.complete();
        return table.getPages();
    }

    // The rows as a delimited file, with the fields that hold a comma quoted.
    private static String csv() {
        StringBuilder csv = new StringBuilder("Name,City,Total\n");
        for (String[] row : rows()) {
            csv.append(row.length == 1 ? row[0] : row[0] + ",\"" + row[1] + "\"," + row[2]).append('\n');
        }
        return csv.toString();
    }

    @Test
    void rowsFromMemoryDrawWhatTheSameFileDraws() throws Exception {
        File file = new File(tempDir, "rows.csv");
        OutputStream out = new FileOutputStream(file);
        try {
            out.write(csv().getBytes(StandardCharsets.UTF_8));
        } finally {
            out.close();
        }

        PDF pdf1 = TestSupport.newPDF();
        Font font1 = TestSupport.helvetica(pdf1);
        BigTable fromFile = new BigTable(pdf1, font1, font1, Letter.PORTRAIT)
                .setNumberOfColumns(3).setTableData(file.getPath(), ",");
        List<Page> filePages = draw(fromFile);

        PDF pdf2 = TestSupport.newPDF();
        Font font2 = TestSupport.helvetica(pdf2);
        BigTable fromMemory = new BigTable(pdf2, font2, font2, Letter.PORTRAIT)
                .setNumberOfColumns(3).setTableData(HEADER, rows());
        List<Page> memoryPages = draw(fromMemory);

        // A page releases its content when it is written, so the last page is
        // compared, which also shows the column widths of the first pass.
        assertEquals(2, filePages.size());
        assertEquals(2, memoryPages.size());
        String last = TestSupport.content(memoryPages.get(1));
        assertEquals(TestSupport.content(filePages.get(1)), last);
        assertTrue(last.contains(TestSupport.hex("Name")) && last.contains(TestSupport.hex("n99")));
        assertTrue(!last.contains(TestSupport.hex("short")));
    }

    @Test
    void chosenColumnsAreDrawnInTheirOrder() throws Exception {
        File file = new File(tempDir, "rows.csv");
        OutputStream out = new FileOutputStream(file);
        try {
            out.write(csv().getBytes(StandardCharsets.UTF_8));
        } finally {
            out.close();
        }
        List<String[]> rows = rows();
        rows.add(95, new String[] {"n95b", "x"});   // No third field, so it is skipped

        PDF pdf1 = TestSupport.newPDF();
        Font font1 = TestSupport.helvetica(pdf1);
        List<Page> filePages = draw(new BigTable(pdf1, font1, font1, Letter.PORTRAIT)
                .setColumns(2, 0).setTableData(file.getPath(), ","));
        PDF pdf2 = TestSupport.newPDF();
        Font font2 = TestSupport.helvetica(pdf2);
        List<Page> memoryPages = draw(new BigTable(pdf2, font2, font2, Letter.PORTRAIT)
                .setColumns(2, 0).setTableData(HEADER, rows));

        assertEquals(2, memoryPages.size());
        String last = TestSupport.content(memoryPages.get(1));
        assertEquals(TestSupport.content(filePages.get(1)), last);
        assertTrue(last.indexOf(TestSupport.hex("Total")) < last.indexOf(TestSupport.hex("Name")));
        assertTrue(last.contains(TestSupport.hex("n99")));
        assertTrue(!last.contains(TestSupport.hex("City")) && !last.contains(TestSupport.hex("n95b")));
    }

    @Test
    void aNegativeColumnIndexIsRefused() throws Exception {
        PDF pdf = TestSupport.newPDF();
        Font font = TestSupport.helvetica(pdf);
        BigTable table = new BigTable(pdf, font, font, Letter.PORTRAIT);
        IllegalArgumentException e = assertThrows(IllegalArgumentException.class, () -> table.setColumns(1, -1));
        assertEquals("A column index cannot be negative.", e.getMessage());
    }

    // A table of the first five rows on one page, drawn after the change.
    private static String drawSmall(PDF pdf, java.util.function.Consumer<BigTable> change) throws Exception {
        Font font = TestSupport.helvetica(pdf);
        BigTable table = new BigTable(pdf, font, font, Letter.PORTRAIT)
                .setNumberOfColumns(3).setTableData(HEADER, rows().subList(0, 5));
        change.accept(table);
        List<Page> pages = draw(table);
        assertEquals(1, pages.size());
        return TestSupport.content(pages.get(0));
    }

    @Test
    void theVerticalLinesCanBeLeftOut() throws Exception {
        // The lines between the columns and at the sides, one for each of the
        // three columns and one more, are left out, and the lines between the
        // rows are drawn as before.
        String all = drawSmall(TestSupport.newPDF(), table -> {});
        String rows = drawSmall(TestSupport.newPDF(), table -> table.setVerticalLines(false));
        assertEquals(count(all, "\nS\n") - 4, count(rows, "\nS\n"));
        assertTrue(count(rows, "\nS\n") > 0, "the lines between the rows are drawn");
        assertEquals(all, drawSmall(TestSupport.newPDF(), table -> table.setVerticalLines(true)));
    }

    @Test
    void theShadingAndTheBorderColorsCanBeChangedOrLeftOut() throws Exception {
        String defaults = drawSmall(TestSupport.newPDF(), table -> {});
        assertTrue(defaults.contains("0.94 0.94 0.94 rg\n") && defaults.contains("0.69 0.69 0.69 RG\n"));
        assertTrue(defaults.contains(" re\nf\n"), defaults);    // A shaded row is one rectangle

        String colored = drawSmall(TestSupport.newPDF(),
                table -> table.setShadingColor(0xFF0000).setBorderColor(new float[] {0f, 0f, 1f}));
        assertTrue(colored.contains("1 0 0 rg\n") && colored.contains("0 0 1 RG\n"));
        assertTrue(!colored.contains("0.94 0.94 0.94 rg") && !colored.contains("0.69 0.69 0.69 RG"));

        String plain = drawSmall(TestSupport.newPDF(),
                table -> table.setShadingColor(Color.transparent).setBorderColor((float[]) null));
        assertTrue(!plain.contains("\nf\n") && !plain.contains("\nS\n"));
        assertTrue(plain.contains(TestSupport.hex("n4")));
    }

    @Test
    void thePaddingCanBeSetAfterTheData() throws Exception {
        String content = drawSmall(TestSupport.newPDF(), table -> table.setPadding(10f));
        assertTrue(content.contains("BT\n20 "), content);    // The location is 10, 10
        PDF pdf = TestSupport.newPDF();
        Font font = TestSupport.helvetica(pdf);
        IllegalArgumentException e = assertThrows(IllegalArgumentException.class,
                () -> new BigTable(pdf, font, font, Letter.PORTRAIT).setPadding(-1f));
        assertEquals("The padding cannot be negative.", e.getMessage());
    }

    @Test
    void theFooterCanHaveItsOwnTextAndFontOrBeLeftOut() throws Exception {
        final PDF pdf = TestSupport.newPDF();
        final Font big = TestSupport.helvetica(pdf).setSize(20f);
        String custom = drawSmall(pdf, table -> table.setFooter("{page}/{pages}", big));
        assertTrue(custom.contains(TestSupport.hex("1/1")) && custom.contains(" 20 Tf\n"));

        String none = drawSmall(TestSupport.newPDF(), table -> table.setFooter(null, null));
        String defaults = drawSmall(TestSupport.newPDF(), table -> {});
        assertTrue(defaults.startsWith(none) && defaults.length() > none.length());
    }

    @Test
    void theVerticalPaddingMakesTallerRowsOnMorePages() throws Exception {
        // The rows are taller by the padding above and below their text, so
        // the table takes more pages, and the footer counts them all.
        int[] counts = new int[2];
        float[] paddings = {0f, 3f};
        for (int i = 0; i < 2; i++) {
            PDF pdf = TestSupport.newPDF();
            Font font = TestSupport.helvetica(pdf);
            BigTable table = new BigTable(pdf, font, font, Letter.PORTRAIT)
                    .setNumberOfColumns(3).setTableData(HEADER, rows())
                    .setVerticalPadding(paddings[i]).setFooter("{page}/{pages}", null);
            List<Page> pages = draw(table);
            counts[i] = pages.size();
            String last = TestSupport.content(pages.get(pages.size() - 1));
            assertTrue(last.contains(TestSupport.hex(counts[i] + "/" + counts[i])), "the footer counts the pages");
            assertTrue(last.contains(TestSupport.hex("n99")));
        }
        assertTrue(counts[1] > counts[0], counts[0] + " pages, then " + counts[1]);
        // 0 is the table as it was.
        assertEquals(drawSmall(TestSupport.newPDF(), table -> {}),
                drawSmall(TestSupport.newPDF(), table -> table.setVerticalPadding(0f)));
        PDF pdf = TestSupport.newPDF();
        Font font = TestSupport.helvetica(pdf);
        for (float bad : new float[] {-1f, Float.NaN, Float.POSITIVE_INFINITY}) {
            IllegalArgumentException e = assertThrows(IllegalArgumentException.class,
                    () -> new BigTable(pdf, font, font, Letter.PORTRAIT).setVerticalPadding(bad));
            assertEquals("The vertical padding must be a number, 0 or more.", e.getMessage());
        }
    }

    @Test
    void theHeaderCanHaveAColorOfItsOwn() throws Exception {
        // With no shading, the header alone is filled, in its color.
        String header = drawSmall(TestSupport.newPDF(),
                table -> table.setShadingColor(Color.transparent).setHeaderColor(0xFF0000));
        assertTrue(header.contains("1 0 0 rg\n"), header);
        assertEquals(1, count(header, "\nf\n"), "one filled rectangle, the header's");
        // With the shading, the rows under it are shaded as before.
        String shaded = drawSmall(TestSupport.newPDF(), table -> table.setHeaderColor(0xFF0000));
        String defaults = drawSmall(TestSupport.newPDF(), table -> {});
        assertEquals(count(defaults, "\nf\n"), count(shaded, "\nf\n"));
        assertTrue(shaded.contains("1 0 0 rg\n") && shaded.contains("0.94 0.94 0.94 rg\n"));
        // Color.transparent is the table as it was.
        assertEquals(defaults, drawSmall(TestSupport.newPDF(), table -> table.setHeaderColor(Color.transparent)));
    }

    @Test
    void theFooterCanBeSmallerThanItsFont() throws Exception {
        String small = drawSmall(TestSupport.newPDF(), table -> table.setFooterFontSize(7f));
        String defaults = drawSmall(TestSupport.newPDF(), table -> {});
        assertTrue(small.contains(" 7 Tf\n") && !defaults.contains(" 7 Tf\n"), small);
        PDF pdf = TestSupport.newPDF();
        Font font = TestSupport.helvetica(pdf);
        for (float bad : new float[] {0f, -8f, Float.NaN, Float.POSITIVE_INFINITY}) {
            IllegalArgumentException e = assertThrows(IllegalArgumentException.class,
                    () -> new BigTable(pdf, font, font, Letter.PORTRAIT).setFooterFontSize(bad));
            assertEquals("The size of the footer must be a number more than 0.", e.getMessage());
        }
    }

    @Test
    void theFooterCanSitAtTheBottomMargin() throws Exception {
        // The baseline of the footer is the offset above the bottom of the
        // page, y = 30 in the coordinates of the PDF.
        String raised = drawSmall(TestSupport.newPDF(), table -> table.setFooterOffset(30f));
        String defaults = drawSmall(TestSupport.newPDF(), table -> {});
        assertTrue(raised.contains(" 30 Td\n") && !defaults.contains(" 30 Td\n"), raised);
        PDF pdf = TestSupport.newPDF();
        Font font = TestSupport.helvetica(pdf);
        for (float bad : new float[] {-1f, Float.NaN, Float.POSITIVE_INFINITY}) {
            IllegalArgumentException e = assertThrows(IllegalArgumentException.class,
                    () -> new BigTable(pdf, font, font, Letter.PORTRAIT).setFooterOffset(bad));
            assertEquals("The offset of the footer must be a number, 0 or more.", e.getMessage());
        }
    }

    @Test
    void lineBreaksInFieldsAreDrawnAsSpaces() throws Exception {
        File file = new File(tempDir, "breaks.csv");
        OutputStream out = new FileOutputStream(file);
        try {
            out.write(("Name,City,Total\n\"n\n0\",\"City\r\n0\",1\nn1,City 1,2\n").getBytes(StandardCharsets.UTF_8));
        } finally {
            out.close();
        }
        PDF pdf1 = TestSupport.newPDF();
        Font font1 = TestSupport.helvetica(pdf1);
        List<Page> fromFile = draw(new BigTable(pdf1, font1, font1, Letter.PORTRAIT)
                .setNumberOfColumns(3).setTableData(file.getPath(), ","));

        PDF pdf2 = TestSupport.newPDF();
        Font font2 = TestSupport.helvetica(pdf2);
        List<String[]> rows = Arrays.asList(new String[] {"n\r0", "City\r\n0", "1"}, new String[] {"n1", "City 1", "2"});
        List<Page> fromMemory = draw(new BigTable(pdf2, font2, font2, Letter.PORTRAIT)
                .setNumberOfColumns(3).setTableData(HEADER, rows));

        PDF pdf3 = TestSupport.newPDF();
        Font font3 = TestSupport.helvetica(pdf3);
        List<String[]> spaces = Arrays.asList(new String[] {"n 0", "City 0", "1"}, new String[] {"n1", "City 1", "2"});
        List<Page> withSpaces = draw(new BigTable(pdf3, font3, font3, Letter.PORTRAIT)
                .setNumberOfColumns(3).setTableData(HEADER, spaces));

        assertEquals(1, fromFile.size());
        assertEquals(TestSupport.content(withSpaces.get(0)), TestSupport.content(fromFile.get(0)));
        assertEquals(TestSupport.content(withSpaces.get(0)), TestSupport.content(fromMemory.get(0)));
    }

    @Test
    void theRowsAreReadTwiceAndClosed() throws Exception {
        final List<String[]> rows = rows();
        final int[] opened = {0};
        final int[] closed = {0};
        Iterable<String[]> source = () -> {
            opened[0]++;
            return new java.util.Iterator<String[]>() {
                private int index;

                @Override
                public boolean hasNext() {
                    return index < rows.size();
                }

                @Override
                public String[] next() {
                    return rows.get(index++);
                }
            };
        };
        PDF pdf = TestSupport.newPDF();
        Font font = TestSupport.helvetica(pdf);
        draw(new BigTable(pdf, font, font, Letter.PORTRAIT).setNumberOfColumns(3).setTableData(HEADER, source));
        assertEquals(2, opened[0]);

        class ClosingIterator implements java.util.Iterator<String[]>, java.io.Closeable {
            private int index;

            @Override
            public boolean hasNext() {
                return index < rows.size();
            }

            @Override
            public String[] next() {
                return rows.get(index++);
            }

            @Override
            public void close() {
                closed[0]++;
            }
        }
        pdf = TestSupport.newPDF();
        font = TestSupport.helvetica(pdf);
        draw(new BigTable(pdf, font, font, Letter.PORTRAIT)
                .setNumberOfColumns(3).setTableData(HEADER, () -> new ClosingIterator()));
        assertEquals(2, closed[0]);
    }

    @Test
    void aHeaderWithFewerFieldsThanColumnsIsRefused() throws Exception {
        PDF pdf = TestSupport.newPDF();
        Font font = TestSupport.helvetica(pdf);
        BigTable table = new BigTable(pdf, font, font, Letter.PORTRAIT).setNumberOfColumns(4);
        IllegalArgumentException e = assertThrows(IllegalArgumentException.class,
                () -> table.setTableData(HEADER, rows()));
        assertEquals("The header does not have a field for every column.", e.getMessage());
    }
    // The number of times the text is in the string.
    private static int count(String str, String text) {
        return str.split(java.util.regex.Pattern.quote(text), -1).length - 1;
    }

    @Test
    void isTaggedAsATableInAPDFUADocument() throws Exception {
        TestSupport.allowCoreFonts(true); // What the document tags, in a core font
        // The table is one Table element over all its pages: a TR for each
        // row, and a TH for each header field the first time the header is
        // drawn or a TD for each field of a row, each holding its own text.
        java.io.ByteArrayOutputStream bos = new java.io.ByteArrayOutputStream();
        PDF pdf = new PDF(bos, Compliance.PDF_UA_1).setTitle("Test");
        pdf.setTitle("Title");
        Font font = TestSupport.helvetica(pdf);
        BigTable table = new BigTable(pdf, font, font, Letter.PORTRAIT);
        table.setNumberOfColumns(3);
        table.setTableData(HEADER, rows());
        List<Page> pages = draw(table);
        assertEquals(2, pages.size());
        // The header that repeats on the second page is an artifact, and the
        // shading and the lines of every page are artifacts too.
        String content = TestSupport.latin1(pages.get(1).getContent());
        assertTrue(content.indexOf(TestSupport.hex("Name")) < content.indexOf("BDC\n"), content);
        assertTrue(content.indexOf("/Artifact BMC\n") < content.indexOf(TestSupport.hex("Name")), content);
        pdf.complete();
        String raw = TestSupport.latin1(bos.toByteArray());
        assertEquals(1, count(raw, "/S /Table\n"), raw);
        // The 100 rows of the data, and the header row of the first page.
        assertEquals(101, count(raw, "/S /TR\n"), raw);
        assertEquals(3, count(raw, "/S /TH\n"), raw);
        assertEquals(300, count(raw, "/S /TD\n"), raw);
        // A cell holds the text itself and has no paragraph under it.
        assertEquals(0, count(raw, "/S /P\n"), raw);
        assertEquals(3, count(raw, "/A <</O /Table /Scope /Column>>"), raw);
    }

    @Test
    void isNotTaggedInADocumentThatIsNotPDFUA() throws Exception {
        java.io.ByteArrayOutputStream bos = new java.io.ByteArrayOutputStream();
        PDF pdf = new PDF(bos);
        Font font = TestSupport.helvetica(pdf);
        BigTable table = new BigTable(pdf, font, font, Letter.PORTRAIT);
        table.setNumberOfColumns(3);
        table.setTableData(HEADER, rows());
        draw(table);
        pdf.complete();
        String raw = TestSupport.latin1(bos.toByteArray());
        for (String text : new String[] {"/S /Table\n", "/S /TR\n", "BDC\n", "/Artifact BMC\n"}) {
            assertEquals(0, count(raw, text), text);
        }
    }


    // The x coordinates of the text the page draws, in the order it draws it.
    private static List<float[]> textPositions(Page page) {
        List<float[]> list = new ArrayList<float[]>();
        java.util.regex.Matcher m = java.util.regex.Pattern.compile(
                "([-0-9.]+) ([-0-9.]+) Td\\n(?:/F\\d+ [0-9.]+ Tf\\n)?\\[<([0-9A-Fa-f]*)>\\] TJ")
                .matcher(TestSupport.latin1(page.getContent()));
        while (m.find()) {
            list.add(new float[] {Float.parseFloat(m.group(1)), m.group(3).length() / 2f});
        }
        return list;
    }

    @Test
    void theAlignmentOfAColumnTheTableDoesNotHaveIsRefused() throws Exception {
        // The alignment of a column was written into the array of them
        // whatever the column was, and there is no array before setTableData.
        PDF pdf = TestSupport.newPDF();
        Font font = TestSupport.helvetica(pdf);
        BigTable table = new BigTable(pdf, font, font, Letter.PORTRAIT).setNumberOfColumns(2);
        assertEquals("The table has no column 0: set the alignment of a column after setTableData.",
                assertThrows(IllegalArgumentException.class,
                        () -> table.setTextAlignment(0, Alignment.RIGHT)).getMessage());
        table.setTableData(new String[] {"A", "B"}, rows());
        table.setTextAlignment(1, Alignment.RIGHT);
        assertEquals("The table has no column 2: set the alignment of a column after setTableData.",
                assertThrows(IllegalArgumentException.class,
                        () -> table.setTextAlignment(2, Alignment.RIGHT)).getMessage());
    }

    @Test
    void aColumnIsAsWideAsTheFontOfEachRowDrawsIt() throws Exception {
        // The columns were measured with the header font whatever font a row
        // was drawn with, so a body font wider than the header font ran over
        // the column on its right.
        PDF pdf = TestSupport.newPDF();
        Font f1 = new Font(pdf, CoreFont.HELVETICA_BOLD).setSize(8f);
        Font f2 = new Font(pdf, CoreFont.HELVETICA).setSize(14f);
        BigTable table = new BigTable(pdf, f1, f2, Letter.PORTRAIT);
        table.setNumberOfColumns(2);
        List<String[]> rows = new ArrayList<String[]>();
        rows.add(new String[] {"wwww", "xx"});
        table.setTableData(new String[] {"A", "B"}, rows);
        table.setLocation(50f, 50f);
        table.setFooter(null, null);
        table.complete();

        List<float[]> positions = textPositions(table.getPages().get(0));
        assertEquals(4, positions.size(), positions.toString());
        // The header "A" ends before "B" starts, and so does the row under it.
        assertTrue(positions.get(0)[0] + f1.stringWidth("A") <= positions.get(1)[0],
                "the header runs into the next column");
        assertTrue(positions.get(2)[0] + f2.stringWidth("wwww") <= positions.get(3)[0],
                "the row runs into the next column: " + positions.get(2)[0] + " + "
                        + f2.stringWidth("wwww") + " > " + positions.get(3)[0]);
    }


    // A table of one column of the given text, drawn on a letter page in a
    // font wide enough that the text may not fit it.
    // The columns take the percentages of the table's width that
    // setColumnWidthsInPercent gives them, the page less the margin on each
    // side, and a field wider than its column is cut to fit it; percentages that
    // are not one for each column, or a negative one, are refused.
    @Test
    void theColumnsCanBeGivenWidthsInPercent() throws Exception {
        PDF pdf = TestSupport.newPDF();
        Font font = new Font(pdf, CoreFont.HELVETICA);
        BigTable table = new BigTable(pdf, font, font, Letter.PORTRAIT);
        table.setNumberOfColumns(3);
        table.setColumnWidthsInPercent(20f, 30f, 50f);
        List<String[]> rows = new ArrayList<String[]>();
        rows.add(new String[] {"A field far wider than a fifth of the width of the table", "b", "c"});
        table.setTableData(new String[] {"A", "B", "C"}, rows);
        table.setLocation(36f, 20f);
        table.setFooter(null, null);
        table.complete();
        Page page = table.getPages().get(0);
        // The vertical lines, 540 points of the table at 20, 30 and 50 percent.
        List<Float> lines = new ArrayList<Float>();
        java.util.regex.Matcher m = java.util.regex.Pattern.compile("([-0-9.]+) [-0-9.]+ m")
                .matcher(TestSupport.content(page));
        while (m.find()) {
            lines.add(Float.parseFloat(m.group(1)));
        }
        for (float x : new float[] {36f, 144f, 306f, 576f}) {
            assertTrue(lines.contains(x), "no line at x=" + x + ": " + lines);
        }
        List<String> drawn = drawnText(page);
        assertTrue(drawn.size() == 6 && drawn.get(3).endsWith(" ...") && drawn.get(4).equals("b"), drawn.toString());
        assertTrue(font.stringWidth(drawn.get(3)) <= 108f - 4f, "the cut field is " + font.stringWidth(drawn.get(3)));

        for (float[] percents : new float[][] {{50f, 50f}, {20f, 30f, -50f}}) {
            BigTable refused = new BigTable(TestSupport.newPDF(), font, font, Letter.PORTRAIT);
            refused.setNumberOfColumns(3);
            assertThrows(IllegalArgumentException.class, () -> refused.setColumnWidthsInPercent(percents));
        }
    }

    private static BigTable tooWide(PDF pdf, Font font, String text) throws Exception {
        BigTable table = new BigTable(pdf, font, font, Letter.PORTRAIT);
        table.setNumberOfColumns(1);
        List<String[]> rows = new ArrayList<String[]>();
        rows.add(new String[] {text});
        table.setTableData(new String[] {"Header"}, rows);
        table.setLocation(0f, 20f);
        table.setFooter(null, null);
        table.complete();
        return table;
    }

    // The strings the page draws, in the order they are drawn.
    private static List<String> drawnText(Page page) {
        List<String> list = new ArrayList<String>();
        java.util.regex.Matcher m = java.util.regex.Pattern.compile("\\[<([0-9A-Fa-f]*)>\\] TJ")
                .matcher(TestSupport.latin1(page.getContent()));
        while (m.find()) {
            // A core font draws a character as the one byte of its code.
            StringBuilder text = new StringBuilder();
            for (int i = 0; i + 1 < m.group(1).length(); i += 2) {
                text.append((char) Integer.parseInt(m.group(1).substring(i, i + 2), 16));
            }
            list.add(text.toString());
        }
        return list;
    }

    @Test
    void aTableTooWideForItsPageIsCutBackToItAndSaysSo() throws Exception {
        // The columns of a table are as wide as their widest field, which can
        // come to more than the page holds; the last of them were drawn off
        // the right edge, where they are lost. They are cut back to the page
        // now, and a field that was cut ends in " ..." to say it was.
        PDF pdf = TestSupport.newPDF();
        Font font = new Font(pdf, CoreFont.HELVETICA).setSize(24f);
        String text = "A string that is far too long to fit across the width of a letter page";
        BigTable table = tooWide(pdf, font, text);
        Page page = table.getPages().get(0);
        List<String> drawn = drawnText(page);
        assertEquals(2, drawn.size(), drawn.toString());
        assertTrue(drawn.get(1).endsWith(" ..."), drawn.get(1));
        assertTrue(text.startsWith(drawn.get(1).substring(0, drawn.get(1).length() - 4)),
                "the text drawn is not the start of the text: " + drawn.get(1));
        // What is drawn fits the page, which the whole of the text does not.
        assertTrue(font.stringWidth(text) > Letter.PORTRAIT.getWidth(), "the text fits the page");
        assertTrue(font.stringWidth(drawn.get(1)) <= Letter.PORTRAIT.getWidth(),
                "the text drawn does not fit the page: " + font.stringWidth(drawn.get(1)));
        // And the vertical lines of the table are on the page with it.
        java.util.regex.Matcher m = java.util.regex.Pattern.compile("([-0-9.]+) [-0-9.]+ m")
                .matcher(TestSupport.content(page));
        while (m.find()) {
            assertTrue(Float.parseFloat(m.group(1)) <= Letter.PORTRAIT.getWidth(),
                    "a line of the table is off the page at x=" + m.group(1));
        }
    }

    @Test
    void aTableThatFitsItsPageIsNotCut() throws Exception {
        // Nothing is measured or cut when the columns fit, and no field of a
        // table that fits ends in the mark of one that was cut.
        PDF pdf = TestSupport.newPDF();
        Font font = new Font(pdf, CoreFont.HELVETICA).setSize(24f);
        String text = "Short enough";
        BigTable table = tooWide(pdf, font, text);
        List<String> drawn = drawnText(table.getPages().get(0));
        assertEquals(Arrays.asList("Header", text), drawn);
    }

    @Test
    void theCellOfACutFieldKeepsTheWholeOfItsText() throws Exception {
        TestSupport.allowCoreFonts(true); // What the document tags, in a core font
        // A field the page was too narrow for is drawn cut, but a reader is
        // read the whole of it: the cell of the structure tree keeps it.
        java.io.ByteArrayOutputStream bos = new java.io.ByteArrayOutputStream();
        PDF pdf = new PDF(bos, Compliance.PDF_UA_1).setTitle("Test");
        pdf.setTitle("Title");
        Font font = new Font(pdf, CoreFont.HELVETICA).setSize(24f);
        String text = "A string that is far too long to fit across the width of a letter page";
        BigTable table = tooWide(pdf, font, text);   // Its pages are added as they are drawn.
        // The content of a page is written out and let go by complete().
        List<String> drawn = drawnText(table.getPages().get(0));
        pdf.complete();
        String raw = TestSupport.latin1(bos.toByteArray());
        // An Alt is written as the UTF-16 of the text, after a byte order mark.
        StringBuilder alt = new StringBuilder();
        for (byte b : ("\uFEFF" + text).getBytes(StandardCharsets.UTF_16BE)) {
            alt.append(String.format("%02x", b & 0xFF));
        }
        assertTrue(raw.contains("/Alt <" + alt + ">"),
                "the cell does not keep the whole of the text");
        assertTrue(drawn.get(1).endsWith(" ...") && !drawn.get(1).equals(text), drawn.get(1));
    }

    @Test
    void canStartOnAPageWithAHeadingAboveIt() throws Exception {
        // The first page is the one given, with the table under what is on it;
        // the next pages are new, and the footer counts them all.
        java.io.ByteArrayOutputStream bos = new java.io.ByteArrayOutputStream();
        PDF pdf = new PDF(bos);
        Font font = TestSupport.helvetica(pdf);
        Page first = new Page(pdf, Letter.PORTRAIT);
        new TextLine(font, "Heading").setLocation(10f, 20f).drawOn(first);
        BigTable table = new BigTable(pdf, font, font, Letter.PORTRAIT)
                .setNumberOfColumns(3).setTableData(HEADER, rows()).setFirstPage(first, 500f);
        List<Page> pages = draw(table);
        assertTrue(pages.size() >= 2 && pages.get(0) == first, pages.size() + " pages");
        String last = "Page " + pages.size() + " of " + pages.size();
        assertTrue(TestSupport.content(pages.get(pages.size() - 1)).contains(TestSupport.hex(last)),
                "the last page's footer is not " + last);
        // The page given is a page of the PDF once, not a new one as well.
        pdf.complete();
        String raw = TestSupport.latin1(bos.toByteArray());
        assertTrue(raw.contains("/Count " + pages.size() + "\n"),
                "the PDF does not have the " + pages.size() + " pages of the table");
        // Without a first page, the rows fit on fewer pages.
        PDF other = TestSupport.newPDF();
        Font helvetica = TestSupport.helvetica(other);
        List<Page> plain = draw(new BigTable(other, helvetica, helvetica, Letter.PORTRAIT)
                .setNumberOfColumns(3).setTableData(HEADER, rows()));
        assertTrue(plain.size() < pages.size(),
                pages.size() + " pages from a y of 500 on the first page, and " + plain.size() + " from the top");
    }

    // A table of three columns, which fill gives a field wider than its
    // column in the first and the third, and a fourth field in each row.
    private static BigTable edgeTable(PDF pdf) throws Exception {
        Font font = new Font(pdf, CoreFont.HELVETICA);
        BigTable table = new BigTable(pdf, font, font, Letter.PORTRAIT);
        table.setNumberOfColumns(3);
        table.setLocation(36f, 20f);
        table.setFooter(null, null);
        return table;
    }

    private static BigTable fill(BigTable table) {
        String wide = "A field far wider than a fifth of the width of the table";
        List<String[]> rows = new ArrayList<String[]>();
        rows.add(new String[] {wide, "b", wide, "d"});
        return table.setTableData(new String[] {"A", "B", "C", "D"}, rows);
    }

    // The edge cases of setColumnWidthsInPercent: the columns set again after
    // the percentages, fewer threw an ArrayIndexOutOfBoundsException (review
    // of 12 October 2026, C4); a column too narrow for " ..." drew it over the
    // next column; the largest float as a share made the widths infinite; a
    // margin of half the page or more made widths of less than nothing (T5).
    @Test
    void theEdgeCasesOfColumnWidthsInPercent() throws Exception {
        for (int columns : new int[] {2, 4}) {
            final BigTable table = edgeTable(TestSupport.newPDF());
            table.setColumnWidthsInPercent(20f, 30f, 50f);
            table.setNumberOfColumns(columns);
            Exception e = assertThrows(IllegalArgumentException.class, () -> fill(table).complete());
            assertEquals("The table has " + columns + " columns, and 3 percentages: give one for each column.",
                    e.getMessage());
        }
        BigTable narrow = edgeTable(TestSupport.newPDF());
        narrow.setColumnWidthsInPercent(1f, 98f, 1f);
        fill(narrow).complete();
        for (String text : drawnText(narrow.getPages().get(0))) {
            assertFalse(text.equals(" ..."), "a column too narrow for it draws " + text);
        }
        BigTable huge = edgeTable(TestSupport.newPDF());
        huge.setColumnWidthsInPercent(Float.MAX_VALUE, 1f, 1f);
        fill(huge).complete();
        assertTrue(TestSupport.content(huge.getPages().get(0)).contains("576 "));
        final BigTable margin = fill(edgeTable(TestSupport.newPDF()));
        margin.setColumnWidthsInPercent(20f, 30f, 50f);
        Exception e = assertThrows(IllegalArgumentException.class, () -> margin.setLocation(400f, 20f));
        assertEquals("The margin of the table, its x, is half the width of the page or more: the columns have no room.", e.getMessage());
    }
}
