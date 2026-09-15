import java.io.*;
import java.lang.management.ManagementFactory;
import java.nio.charset.StandardCharsets;
import java.util.*;

/**
 * Example_43 against iText Core and Apache PDFBox: the same CSV file, 9
 * columns with widths measured from every field with the header font, shaded
 * alternate rows, a line above each row, vertical lines at the end of each
 * page, the header on every page, and "Page i of N" footers.
 *
 * Configurations:
 *   jet        Example_43 as it is, with BigTable
 *   it-canvas  iText drawing the same table with PdfCanvas
 *   it-layout  iText's own Table in large-table mode
 *   box        PDFBox drawing the same table with PDPageContentStream
 *
 * iText and PDFBox draw through one driver that follows BigTable step by step,
 * with the geometry that PDFjet's metrics of IBM Plex Sans give.
 *
 * Usage: BigTableBench bench|cold <config> <csv>
 *        BigTableBench sample <config> <csv> <pdf>
 *
 * The repository root is read from the pdfjet.root system property, and the
 * directory of the TrueType IBM Plex Sans fonts for PDFBox from plex.ttf.
 */
public class BigTableBench {
    static final String ROOT = System.getProperty("pdfjet.root", ".") + "/";
    static final String TTF = System.getProperty("plex.ttf", ROOT + "benchmarks/build/fonts") + "/";
    static final String OTF_SEMIBOLD = ROOT + "fonts/IBMPlexSans/IBMPlexSans-SemiBold.otf";
    static final String OTF_REGULAR = ROOT + "fonts/IBMPlexSans/IBMPlexSans-Regular.otf";
    static final String TTF_SEMIBOLD = TTF + "IBMPlexSans-SemiBold.ttf";
    static final String TTF_REGULAR = TTF + "IBMPlexSans-Regular.ttf";
    static final String TITLE = "Electric Vehicle Population Data";

    // The geometry of BigTable in Example_43, from PDFjet's metrics of IBM Plex
    // Sans SemiBold at 10 points (A1, D1) and Regular at 9 points (A2, D2).
    static final float A1 = 10.25f, D1 = 2.75f, A2 = 9.225f, D2 = 2.475f;
    static final float W = 792f, H = 612f, BOTTOM = 20f, PADDING = 2f;
    static final int COLUMNS = 9;
    static final float FILL = 0xF0 / 255f, PEN = 0xB0 / 255f;

    // Counts the bytes written, so that the output costs no memory or disk.
    static final class Sink extends OutputStream {
        long n;
        @Override public void write(int b) { n++; }
        @Override public void write(byte[] b, int off, int len) { n += len; }
    }

    static BufferedReader open(String file) throws IOException {
        BufferedReader r = new BufferedReader(new InputStreamReader(new FileInputStream(file), StandardCharsets.UTF_8));
        r.mark(1);
        if (r.read() != '﻿') {
            r.reset();
        }
        return r;
    }

    // Splits a line at the commas, as BigTable does.
    static String[] split(String line) {
        List<String> fields = new ArrayList<>();
        int start = 0;
        int end;
        while ((end = line.indexOf(',', start)) != -1) {
            fields.add(line.substring(start, end));
            start = end + 1;
        }
        fields.add(line.substring(start));
        return fields.toArray(new String[0]);
    }

    static boolean isNumber(String text) {
        String t = text.replace(".", "").replace(",", "").replace("'", "").trim();
        if (t.startsWith("+") || t.startsWith("-")) {
            t = t.substring(1);
        }
        if (t.isEmpty()) {
            return false;
        }
        for (int i = 0; i < t.length(); i++) {
            if (t.charAt(i) < '0' || t.charAt(i) > '9') {
                return false;
            }
        }
        return true;
    }

    // What a library draws with. y grows downward from the top of the page.
    interface Surface {
        float width(boolean header, String text) throws Exception;
        void newPage() throws Exception;
        void fill(float x1, float y1, float x2, float y2) throws Exception;
        void line(float x1, float y1, float x2, float y2) throws Exception;
        void text(boolean header, String text, float x, float y) throws Exception;
    }

    static final class Columns {
        String[] header;
        final float[] widths = new float[COLUMNS];
        final boolean[] right = new boolean[COLUMNS];
        final float[] v = new float[COLUMNS + 1];
    }

    // The first pass over the file, as in BigTable.setTableData.
    static Columns measure(String file, Surface s) throws Exception {
        Columns c = new Columns();
        int row = 0;
        try (BufferedReader r = open(file)) {
            String line;
            while ((line = r.readLine()) != null) {
                String[] f = split(line);
                if (f.length < COLUMNS) {
                    continue;
                }
                if (row == 0) {
                    c.header = f;
                }
                if (row == 1) {
                    for (int i = 0; i < COLUMNS; i++) {
                        c.right[i] = isNumber(f[i]);
                    }
                }
                for (int i = 0; i < COLUMNS; i++) {
                    c.widths[i] = Math.max(c.widths[i], s.width(true, f[i]) + 2 * PADDING);
                }
                row++;
            }
        }
        for (int i = 0; i < COLUMNS; i++) {
            c.v[i + 1] = c.v[i] + c.widths[i];
        }
        return c;
    }

    // The second pass, as in BigTable.complete. Returns the number of pages.
    static int draw(String file, Surface s) throws Exception {
        Columns c = measure(file, s);
        int pages = 0;
        boolean started = false;
        boolean startNewPage = true;
        boolean[] highlight = {true};
        float yText = 0f;
        try (BufferedReader r = open(file)) {
            String line;
            while ((line = r.readLine()) != null) {
                String[] f = split(line);
                if (f.length < COLUMNS) {
                    continue;
                }
                if (!started || startNewPage) {
                    s.newPage();
                    pages++;
                    yText = A1;
                    highlight[0] = true;
                    row(s, c, c.header, true, yText, highlight);
                    yText += D1 + A2;
                    startNewPage = false;
                    if (!started) {     // The first line is the header.
                        started = true;
                        continue;
                    }
                }
                row(s, c, f, false, yText, highlight);
                yText += D2 + A2;
                if (yText > H - BOTTOM) {
                    verticals(s, c, yText);
                    startNewPage = true;
                }
            }
        }
        verticals(s, c, yText);
        return pages;
    }

    static void row(Surface s, Columns c, String[] f, boolean header, float yText, boolean[] highlight)
            throws Exception {
        float a = header ? A1 : A2;
        float d = header ? D1 : D2;
        if (highlight[0]) {
            s.fill(c.v[0], yText - a, c.v[COLUMNS], yText + d);
            highlight[0] = false;
        } else {
            highlight[0] = true;
        }
        s.line(c.v[0], yText - a, c.v[COLUMNS], yText - a);
        for (int i = 0; i < COLUMNS; i++) {
            float x = c.v[i] + PADDING;
            if (c.right[i]) {
                x = c.v[i + 1] - PADDING - s.width(header, f[i]);
            }
            s.text(header, f[i], x, yText);
        }
    }

    static void verticals(Surface s, Columns c, float yText) throws Exception {
        for (int i = 0; i <= COLUMNS; i++) {
            s.line(c.v[i], 0f, c.v[i], yText - A2);
        }
        s.line(c.v[0], yText - A2, c.v[COLUMNS], yText - A2);
    }

    // Example_43, with the fonts and the data file given as paths from the root.
    static int jet(String file, OutputStream os) throws Exception {
        com.pdfjet.PDF pdf = new com.pdfjet.PDF(os);
        pdf.setTitle(TITLE);
        com.pdfjet.Font f1 = new com.pdfjet.Font(pdf, ROOT + com.pdfjet.fonts.IBMPlexSans.SemiBold);
        f1.setSize(10f);
        com.pdfjet.Font f2 = new com.pdfjet.Font(pdf, ROOT + com.pdfjet.fonts.IBMPlexSans.Regular);
        f2.setSize(9f);
        com.pdfjet.BigTable table = new com.pdfjet.BigTable(pdf, f1, f2, com.pdfjet.Letter.LANDSCAPE);
        table.setNumberOfColumns(9);
        table.setTableData(file, ",");
        table.setLocation(0f, 0f);
        table.setBottomMargin(20f);
        table.complete();
        List<com.pdfjet.Page> pages = table.getPages();
        for (int i = 0; i < pages.size(); i++) {
            com.pdfjet.Page page = pages.get(i);
            page.addFooter(new com.pdfjet.TextLine(f1, "Page " + (i + 1) + " of " + pages.size()));
            pdf.addPage(page);
        }
        pdf.complete();
        return pages.size();
    }

    static com.itextpdf.kernel.font.PdfFont itextFont(String path) throws IOException {
        return com.itextpdf.kernel.font.PdfFontFactory.createFont(path, com.itextpdf.io.font.PdfEncodings.IDENTITY_H,
                com.itextpdf.kernel.font.PdfFontFactory.EmbeddingStrategy.FORCE_EMBEDDED);
    }

    static int itextCanvas(String file, OutputStream os) throws Exception {
        com.itextpdf.kernel.pdf.PdfDocument pdf =
                new com.itextpdf.kernel.pdf.PdfDocument(new com.itextpdf.kernel.pdf.PdfWriter(os));
        pdf.getDocumentInfo().setTitle(TITLE);
        com.itextpdf.kernel.font.PdfFont f1 = itextFont(OTF_SEMIBOLD);
        com.itextpdf.kernel.font.PdfFont f2 = itextFont(OTF_REGULAR);
        com.itextpdf.kernel.pdf.canvas.PdfCanvas[] cs = {null};
        Surface s = new Surface() {
            public float width(boolean header, String text) {
                return header ? f1.getWidth(text, 10f) : f2.getWidth(text, 9f);
            }
            public void newPage() {
                if (cs[0] != null) {
                    cs[0].release();
                }
                cs[0] = new com.itextpdf.kernel.pdf.canvas.PdfCanvas(
                        pdf.addNewPage(com.itextpdf.kernel.geom.PageSize.LETTER.rotate()));
                cs[0].setLineWidth(0f);
            }
            public void fill(float x1, float y1, float x2, float y2) {
                cs[0].setFillColorRgb(FILL, FILL, FILL).rectangle(x1, H - y2, x2 - x1, y2 - y1).fill()
                        .setFillColorRgb(0f, 0f, 0f);
            }
            public void line(float x1, float y1, float x2, float y2) {
                cs[0].setStrokeColorRgb(PEN, PEN, PEN).moveTo(x1, H - y1).lineTo(x2, H - y2).stroke();
            }
            public void text(boolean header, String text, float x, float y) {
                cs[0].beginText().setFontAndSize(header ? f1 : f2, header ? 10f : 9f)
                        .moveText(x, H - y).showText(text).endText();
            }
        };
        int pages = draw(file, s);
        cs[0].release();
        for (int i = 1; i <= pages; i++) {
            String footer = "Page " + i + " of " + pages;
            new com.itextpdf.kernel.pdf.canvas.PdfCanvas(pdf.getPage(i))
                    .beginText().setFontAndSize(f1, 10f).moveText((W - f1.getWidth(footer, 10f)) / 2, A1)
                    .showText(footer).endText().release();
        }
        pdf.close();
        return pages;
    }

    static com.itextpdf.layout.element.Cell itextCell(String text, com.itextpdf.kernel.font.PdfFont font,
            float size, float leading, boolean right, boolean shaded) {
        com.itextpdf.layout.element.Paragraph p = new com.itextpdf.layout.element.Paragraph(text)
                .setFont(font).setFontSize(size).setMargin(0f).setFixedLeading(leading);
        com.itextpdf.layout.element.Cell cell = new com.itextpdf.layout.element.Cell().add(p);
        cell.setPadding(0f).setPaddingLeft(PADDING).setPaddingRight(PADDING);
        cell.setBorder(com.itextpdf.layout.borders.Border.NO_BORDER);
        cell.setBorderTop(new com.itextpdf.layout.borders.SolidBorder(
                new com.itextpdf.kernel.colors.DeviceRgb(PEN, PEN, PEN), 0.1f));
        if (shaded) {
            cell.setBackgroundColor(new com.itextpdf.kernel.colors.DeviceRgb(FILL, FILL, FILL));
        }
        if (right) {
            cell.setTextAlignment(com.itextpdf.layout.properties.TextAlignment.RIGHT);
        }
        return cell;
    }

    // iText's own large table: the Table is added to the Document before its
    // rows and flushed every 50 rows, the header is repeated on every page, and
    // the "Page i of N" footers are added before the document is closed, with
    // immediate flushing turned off, as iText documents it.
    static int itextLayout(String file, OutputStream os) throws Exception {
        com.itextpdf.kernel.pdf.PdfDocument pdf =
                new com.itextpdf.kernel.pdf.PdfDocument(new com.itextpdf.kernel.pdf.PdfWriter(os));
        pdf.getDocumentInfo().setTitle(TITLE);
        com.itextpdf.kernel.font.PdfFont f1 = itextFont(OTF_SEMIBOLD);
        com.itextpdf.kernel.font.PdfFont f2 = itextFont(OTF_REGULAR);
        com.itextpdf.layout.Document doc = new com.itextpdf.layout.Document(
                pdf, com.itextpdf.kernel.geom.PageSize.LETTER.rotate(), false);
        doc.setMargins(0f, 0f, BOTTOM, 0f);
        Columns c = measure(file, new Surface() {
            public float width(boolean header, String text) { return f1.getWidth(text, 10f); }
            public void newPage() {}
            public void fill(float x1, float y1, float x2, float y2) {}
            public void line(float x1, float y1, float x2, float y2) {}
            public void text(boolean header, String text, float x, float y) {}
        });
        com.itextpdf.layout.element.Table table = new com.itextpdf.layout.element.Table(c.widths, true);
        doc.add(table);
        for (int i = 0; i < COLUMNS; i++) {
            table.addHeaderCell(itextCell(c.header[i], f1, 10f, A1 + D1, c.right[i], true));
        }
        try (BufferedReader r = open(file)) {
            String line;
            boolean first = true;
            int row = 0;
            while ((line = r.readLine()) != null) {
                String[] f = split(line);
                if (f.length < COLUMNS) {
                    continue;
                }
                if (first) {
                    first = false;
                    continue;
                }
                row++;
                for (int i = 0; i < COLUMNS; i++) {
                    table.addCell(itextCell(f[i], f2, 9f, A2 + D2, c.right[i], row % 2 == 0));
                }
                if (row % 50 == 0) {
                    table.flush();
                }
            }
        }
        table.complete();
        int pages = pdf.getNumberOfPages();
        for (int i = 1; i <= pages; i++) {
            doc.showTextAligned(
                    new com.itextpdf.layout.element.Paragraph("Page " + i + " of " + pages).setFont(f1).setFontSize(10f),
                    W / 2, 0f, i, com.itextpdf.layout.properties.TextAlignment.CENTER,
                    com.itextpdf.layout.properties.VerticalAlignment.BOTTOM, 0f);
        }
        doc.close();
        return pages;
    }

    static int pdfbox(String file, OutputStream os) throws Exception {
        try (org.apache.pdfbox.pdmodel.PDDocument doc = new org.apache.pdfbox.pdmodel.PDDocument()) {
            doc.getDocumentInformation().setTitle(TITLE);
            org.apache.pdfbox.pdmodel.font.PDType0Font f1 =
                    org.apache.pdfbox.pdmodel.font.PDType0Font.load(doc, new File(TTF_SEMIBOLD));
            org.apache.pdfbox.pdmodel.font.PDType0Font f2 =
                    org.apache.pdfbox.pdmodel.font.PDType0Font.load(doc, new File(TTF_REGULAR));
            org.apache.pdfbox.pdmodel.PDPageContentStream[] cs = {null};
            Surface s = new Surface() {
                public float width(boolean header, String text) throws IOException {
                    return header ? f1.getStringWidth(text) / 1000f * 10f : f2.getStringWidth(text) / 1000f * 9f;
                }
                public void newPage() throws IOException {
                    if (cs[0] != null) {
                        cs[0].close();
                    }
                    org.apache.pdfbox.pdmodel.PDPage page = new org.apache.pdfbox.pdmodel.PDPage(
                            new org.apache.pdfbox.pdmodel.common.PDRectangle(W, H));
                    doc.addPage(page);
                    cs[0] = new org.apache.pdfbox.pdmodel.PDPageContentStream(doc, page);
                    cs[0].setLineWidth(0f);
                }
                public void fill(float x1, float y1, float x2, float y2) throws IOException {
                    cs[0].setNonStrokingColor(FILL, FILL, FILL);
                    cs[0].addRect(x1, H - y2, x2 - x1, y2 - y1);
                    cs[0].fill();
                    cs[0].setNonStrokingColor(0f, 0f, 0f);
                }
                public void line(float x1, float y1, float x2, float y2) throws IOException {
                    cs[0].setStrokingColor(PEN, PEN, PEN);
                    cs[0].moveTo(x1, H - y1);
                    cs[0].lineTo(x2, H - y2);
                    cs[0].stroke();
                }
                public void text(boolean header, String text, float x, float y) throws IOException {
                    cs[0].beginText();
                    cs[0].setFont(header ? f1 : f2, header ? 10f : 9f);
                    cs[0].newLineAtOffset(x, H - y);
                    cs[0].showText(text);
                    cs[0].endText();
                }
            };
            int pages = draw(file, s);
            cs[0].close();
            for (int i = 0; i < pages; i++) {
                String footer = "Page " + (i + 1) + " of " + pages;
                try (org.apache.pdfbox.pdmodel.PDPageContentStream fc = new org.apache.pdfbox.pdmodel.PDPageContentStream(
                        doc, doc.getPage(i), org.apache.pdfbox.pdmodel.PDPageContentStream.AppendMode.APPEND, true, true)) {
                    fc.beginText();
                    fc.setFont(f1, 10f);
                    fc.newLineAtOffset((W - f1.getStringWidth(footer) / 1000f * 10f) / 2, A1);
                    fc.showText(footer);
                    fc.endText();
                }
            }
            doc.save(os);
            return pages;
        }
    }

    // Every configuration writes through the same 8 MB buffer as Example_43.
    static int run(String config, String csv, OutputStream sink) throws Exception {
        BufferedOutputStream os = new BufferedOutputStream(sink, 8 * 1024 * 1024);
        int pages;
        switch (config) {
        case "jet": pages = jet(csv, os); break;
        case "it-canvas": pages = itextCanvas(csv, os); break;
        case "it-layout": pages = itextLayout(csv, os); break;
        case "box": pages = pdfbox(csv, os); break;
        default: throw new IllegalArgumentException(config);
        }
        os.flush();
        return pages;
    }

    public static void main(String[] args) throws Exception {
        long start = System.nanoTime();
        String mode = args[0];
        String config = args[1];
        String csv = args[2];
        if (mode.equals("sample")) {
            try (FileOutputStream out = new FileOutputStream(args[3])) {
                int pages = run(config, csv, out);
                System.out.printf("%s sample: %d pages, %d ms%n", config, pages, (System.nanoTime() - start) / 1_000_000);
            }
            return;
        }
        if (mode.equals("cold")) {
            Sink sink = new Sink();
            int pages = run(config, csv, sink);
            System.out.printf("%s cold: %d ms, %d pages, %d bytes%n",
                    config, (System.nanoTime() - start) / 1_000_000, pages, sink.n);
            return;
        }
        com.sun.management.ThreadMXBean mx = (com.sun.management.ThreadMXBean) ManagementFactory.getThreadMXBean();
        long t0 = System.nanoTime();
        run(config, csv, new Sink());
        boolean slow = (System.nanoTime() - t0) > 5_000_000_000L;
        int warmups = slow ? 0 : 2;
        int runs = slow ? 3 : 7;
        for (int i = 0; i < warmups; i++) {
            run(config, csv, new Sink());
        }
        long[] ms = new long[runs];
        long size = 0;
        long allocated = 0;
        int pages = 0;
        for (int i = 0; i < runs; i++) {
            Sink sink = new Sink();
            long before = mx.getCurrentThreadAllocatedBytes();
            long t = System.nanoTime();
            pages = run(config, csv, sink);
            ms[i] = (System.nanoTime() - t) / 1_000_000;
            allocated = mx.getCurrentThreadAllocatedBytes() - before;
            size = sink.n;
        }
        Arrays.sort(ms);
        System.out.printf("%s: median %d ms (min %d, max %d, %d runs), %d pages, %d bytes, %.0f MB allocated%n",
                config, ms[runs / 2], ms[0], ms[runs - 1], runs, pages, size, allocated / 1048576.0);
    }
}
