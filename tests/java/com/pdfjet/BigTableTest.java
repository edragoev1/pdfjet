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

    @Test
    void rowsFromMemoryDrawWhatTheSameFileDraws() throws Exception {
        StringBuilder csv = new StringBuilder("Name,City,Total\n");
        for (String[] row : rows()) {
            csv.append(row.length == 1 ? row[0] : row[0] + ",\"" + row[1] + "\"," + row[2]).append('\n');
        }
        File file = new File(tempDir, "rows.csv");
        OutputStream out = new FileOutputStream(file);
        try {
            out.write(csv.toString().getBytes(StandardCharsets.UTF_8));
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
                .setNumberOfColumns(3).setTableData(HEADER, ClosingIterator::new));
        assertEquals(2, closed[0]);
    }

    @Test
    void aHeaderWithFewerFieldsThanColumnsIsRefused() throws Exception {
        PDF pdf = TestSupport.newPDF();
        Font font = TestSupport.helvetica(pdf);
        BigTable table = new BigTable(pdf, font, font, Letter.PORTRAIT).setNumberOfColumns(4);
        IllegalArgumentException e = assertThrows(IllegalArgumentException.class,
                () -> table.setTableData(HEADER, rows()));
        assertEquals("The header has fewer fields than the table has columns.", e.getMessage());
    }
}
