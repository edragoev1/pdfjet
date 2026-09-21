/*
 * BigTableTest.java
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
package com.pdfjet;

import static org.junit.jupiter.api.Assertions.assertEquals;
import static org.junit.jupiter.api.Assertions.assertThrows;
import static org.junit.jupiter.api.Assertions.assertTrue;

import java.io.File;
import java.io.FileOutputStream;
import java.io.OutputStream;
import java.nio.charset.StandardCharsets;
import java.util.ArrayList;
import java.util.Arrays;
import java.util.List;
import org.junit.jupiter.api.Test;
import org.junit.jupiter.api.io.TempDir;

class BigTableTest {
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
        // The table is one Table element over all its pages: a TR for each
        // row, and a TH for each header field the first time the header is
        // drawn or a TD for each field of a row, each holding its own text.
        java.io.ByteArrayOutputStream bos = new java.io.ByteArrayOutputStream();
        PDF pdf = new PDF(bos, Compliance.PDF_UA_1);
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
        // A field the page was too narrow for is drawn cut, but a reader is
        // read the whole of it: the cell of the structure tree keeps it.
        java.io.ByteArrayOutputStream bos = new java.io.ByteArrayOutputStream();
        PDF pdf = new PDF(bos, Compliance.PDF_UA_1);
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
}
