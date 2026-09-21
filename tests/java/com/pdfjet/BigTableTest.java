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

}
