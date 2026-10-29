import java.io.*;
import java.lang.management.ManagementFactory;
import java.nio.charset.StandardCharsets;
import java.nio.file.*;
import java.util.*;

import com.pdfjet.*;
import com.pdfjet.fonts.*;

/**
 * The documents people make most, timed by phase: a one-page invoice, a report
 * of about 8 pages (prose and a table of 230 rows) and a Markdown text of about
 * 10 pages, the first half of PDFjet's README, all in IBM Plex. Each document is made in three
 * phases: the fonts read from their files, the pages drawn, and complete(),
 * which subsets and embeds the fonts, compresses the pages and writes them.
 *
 * Usage: CommonBench bench|cold <document>
 *   bench: 20 warm-up documents, then the median of 15 of each phase
 *   cold:  the first document of a fresh JVM, each phase
 *
 * The repository is read from the pdfjet.root system property.
 */
public class CommonBench {
    static final String ROOT = System.getProperty("pdfjet.root", ".") + "/";

    static final class Sink extends OutputStream {
        long n;
        @Override public void write(int b) { n++; }
        @Override public void write(byte[] b, int off, int len) { n += len; }
    }

    // The nanoseconds of each phase: fonts, drawing, complete.
    static long[] invoice(OutputStream os) throws Exception {
        long t0 = System.nanoTime();
        PDF pdf = new PDF(os);
        pdf.setTitle("Invoice RE-2026-0042");
        Font regular = new Font(pdf, ROOT + IBMPlexSans.Regular).setSize(10f);
        Font bold = new Font(pdf, ROOT + IBMPlexSans.SemiBold).setSize(10f);
        Font title = new Font(pdf, ROOT + IBMPlexSans.SemiBold).setSize(20f);
        long t1 = System.nanoTime();

        Page page = new Page(pdf, Letter.PORTRAIT);
        new TextLine(title, "Invoice RE-2026-0042").setLocation(50f, 70f).drawOn(page);
        String[] from = {"PDFjet Software", "100 King Street West", "Toronto, ON M5X 1A9", "support@pdfjet.com"};
        String[] to = {"Muster GmbH", "Hauptstrasse 12", "20095 Hamburg", "Germany"};
        for (int i = 0; i < from.length; i++) {
            new TextLine(i == 0 ? bold : regular, from[i]).setLocation(50f, 110f + 14f * i).drawOn(page);
            new TextLine(i == 0 ? bold : regular, to[i]).setLocation(350f, 110f + 14f * i).drawOn(page);
        }
        List<List<Cell>> rows = new ArrayList<>();
        rows.add(Arrays.asList(new Cell(bold, "Item"), new Cell(bold, "Description"),
                new Cell(bold, "Quantity"), new Cell(bold, "Price"), new Cell(bold, "Amount")));
        double total = 0;
        for (int i = 1; i <= 12; i++) {
            double price = 19.5 * i + 0.99;
            int quantity = i % 4 + 1;
            total += price * quantity;
            rows.add(Arrays.asList(new Cell(regular, String.valueOf(i)),
                    new Cell(regular, "Consulting, part " + i + " of the project, on site"),
                    new Cell(regular, String.valueOf(quantity)),
                    new Cell(regular, String.format(Locale.ROOT, "%.2f", price)),
                    new Cell(regular, String.format(Locale.ROOT, "%.2f", price * quantity))));
        }
        Table table = new Table();
        table.setTableData(rows, 1);
        table.setCellPadding(5f, 4f);
        table.setWidth(512f);
        table.setColumnWidthsInPercent(8f, 50f, 12f, 15f, 15f);
        table.setLocation(50f, 190f);
        float[] xy = table.drawOn(page);
        new TextLine(bold, String.format(Locale.ROOT, "Total: %.2f EUR", total))
                .setLocation(400f, xy[1] + 30f).drawOn(page);
        long t2 = System.nanoTime();

        pdf.complete();
        long t3 = System.nanoTime();
        return new long[] {t1 - t0, t2 - t1, t3 - t2};
    }

    static long[] report(OutputStream os) throws Exception {
        long t0 = System.nanoTime();
        PDF pdf = new PDF(os);
        pdf.setTitle("World Communications");
        Font regular = new Font(pdf, ROOT + IBMPlexSans.Regular).setSize(11f);
        Font bold = new Font(pdf, ROOT + IBMPlexSans.SemiBold).setSize(11f);
        Font italic = new Font(pdf, ROOT + IBMPlexSans.Italic).setSize(11f);
        Font small = new Font(pdf, ROOT + IBMPlexSans.Regular).setSize(8f);
        Font smallBold = new Font(pdf, ROOT + IBMPlexSans.SemiBold).setSize(8f);
        long t1 = System.nanoTime();

        String prose = new String(Files.readAllBytes(Paths.get(ROOT + "resources/data/the-idiot.txt")),
                StandardCharsets.UTF_8).substring(0, 7000);
        Markup markup = new Markup(regular, bold, italic, italic, regular);
        List<Paragraph> paragraphs = new ArrayList<>();
        paragraphs.add(new Paragraph(new TextLine(bold, "World Communications").setFontSize(20f)));
        paragraphs.addAll(markup.paragraphs(prose.replace("\n\n", "\u0000").replace('\n', ' ').replace('\u0000', '\n')
                .replace("\n", "\n\n")));
        List<Page> pages = new ArrayList<>();
        TextFrame frame = new TextFrame(paragraphs);
        frame.setLocation(50f, 50f);
        frame.setWidth(512f);
        frame.drawOn(pdf, pages, Letter.PORTRAIT);

        List<String> lines = Files.readAllLines(Paths.get(ROOT + "resources/data/world-communications.txt"), StandardCharsets.UTF_8);
        List<List<Cell>> rows = new ArrayList<>();
        for (int r = 0; r < lines.size(); r++) {
            String[] fields = lines.get(r).split("\\|", -1);
            List<Cell> row = new ArrayList<>();
            for (int c = 0; c < 8 && c < fields.length; c++) {
                row.add(new Cell(r == 0 ? smallBold : small, fields[c]));
            }
            while (row.size() < 8) {
                row.add(new Cell(small, ""));
            }
            rows.add(row);
        }
        Table table = new Table();
        table.setTableData(rows, 1);
        table.setCellPadding(3f, 2f);
        table.setWidth(552f);
        table.setColumnWidthsInPercent(16f, 12f, 12f, 12f, 12f, 12f, 12f, 12f);
        table.setLocation(30f, 40f);
        table.setBottomMargin(40f);
        table.drawOn(pdf, pages, Letter.PORTRAIT);
        for (int i = 0; i < pages.size(); i++) {
            pages.get(i).addFooter(new TextLine(small, "Page " + (i + 1) + " of " + pages.size()));
        }
        pdf.addPages(pages);
        long t2 = System.nanoTime();

        pdf.complete();
        long t3 = System.nanoTime();
        return new long[] {t1 - t0, t2 - t1, t3 - t2};
    }

    static long[] markdown(OutputStream os) throws Exception {
        long t0 = System.nanoTime();
        PDF pdf = new PDF(os);
        pdf.setTitle("PDFjet");
        Font regular = new Font(pdf, ROOT + IBMPlexSans.Regular).setSize(11f);
        Font bold = new Font(pdf, ROOT + IBMPlexSans.Bold).setSize(11f);
        Font italic = new Font(pdf, ROOT + IBMPlexSans.Italic).setSize(11f);
        Font boldItalic = new Font(pdf, ROOT + IBMPlexSans.BoldItalic).setSize(11f);
        Font code = new Font(pdf, ROOT + CascadiaMono.Regular).setSize(9.5f);
        long t1 = System.nanoTime();

        String text = new String(Files.readAllBytes(Paths.get(ROOT + "README.md")), StandardCharsets.UTF_8);
        // Its first half, up to the heading after 22,000 characters: about 10 pages
        text = text.substring(0, text.indexOf("\n## ", 22000));
        Markdown markdown = new Markdown(regular, bold, italic, boldItalic, code);
        List<Page> pages = new ArrayList<>();
        markdown.drawOn(pdf, text, pages, Letter.PORTRAIT);
        pdf.addPages(pages);
        long t2 = System.nanoTime();

        pdf.complete();
        long t3 = System.nanoTime();
        return new long[] {t1 - t0, t2 - t1, t3 - t2};
    }

    static long[] make(String doc, OutputStream os) throws Exception {
        BufferedOutputStream out = new BufferedOutputStream(os, 65536);
        long[] t;
        switch (doc) {
        case "invoice": t = invoice(out); break;
        case "report": t = report(out); break;
        case "markdown": t = markdown(out); break;
        default: throw new IllegalArgumentException(doc);
        }
        out.flush();
        return t;
    }

    static double ms(long ns) { return ns / 1e6; }

    public static void main(String[] args) throws Exception {
        String mode = args[0];
        String doc = args[1];
        if (mode.equals("sample")) {
            try (FileOutputStream out = new FileOutputStream(args[2])) {
                make(doc, out);
            }
            return;
        }
        if (mode.equals("cold")) {
            Sink sink = new Sink();
            long[] t = make(doc, sink);
            System.out.printf(Locale.ROOT, "%s cold: fonts %.1f ms, drawing %.1f ms, complete %.1f ms, total %.1f ms, %d bytes%n",
                    doc, ms(t[0]), ms(t[1]), ms(t[2]), ms(t[0] + t[1] + t[2]), sink.n);
            return;
        }
        com.sun.management.ThreadMXBean mx = (com.sun.management.ThreadMXBean) ManagementFactory.getThreadMXBean();
        for (int i = 0; i < 20; i++) {
            make(doc, new Sink());
        }
        int runs = 15;
        long[][] t = new long[3][runs];
        long[] total = new long[runs];
        long allocated = 0;
        long size = 0;
        for (int i = 0; i < runs; i++) {
            Sink sink = new Sink();
            long before = mx.getCurrentThreadAllocatedBytes();
            long[] p = make(doc, sink);
            allocated = mx.getCurrentThreadAllocatedBytes() - before;
            size = sink.n;
            for (int k = 0; k < 3; k++) {
                t[k][i] = p[k];
            }
            total[i] = p[0] + p[1] + p[2];
        }
        for (long[] a : t) {
            Arrays.sort(a);
        }
        Arrays.sort(total);
        System.out.printf(Locale.ROOT, "%s warm: fonts %.1f ms, drawing %.1f ms, complete %.1f ms, total %.1f ms, %d bytes, %.1f MB allocated%n",
                doc, ms(t[0][runs / 2]), ms(t[1][runs / 2]), ms(t[2][runs / 2]), ms(total[runs / 2]), size, allocated / 1048576.0);
    }
}
