import java.io.*;
import java.lang.management.ManagementFactory;
import java.util.*;

/**
 * Writes the same multilingual document with PDFjet, iText Core and Apache
 * PDFBox: pages of 60 lines of 10 point Latin, Greek and Cyrillic text, one
 * drawing call per line, with an embedded font.
 *
 * Configurations: jet-plex, jet-noto, box-noto-subset, box-noto-full,
 * it-plex-subset, it-plex-full, it-noto-subset, it-noto-full, and the iText
 * configurations with -flush, which flush each page when the next one starts.
 *
 * Usage: TextBench bench|cold <config> <pages>
 *        TextBench sample <config> <pages> <file>
 *
 * The repository root is read from the pdfjet.root system property.
 */
public class TextBench {
    static final String ROOT = System.getProperty("pdfjet.root", ".") + "/";
    static final String PLEX_STREAM = ROOT + "fonts/IBMPlexSans/IBMPlexSans-Regular.otf.stream";
    static final String NOTO_STREAM = ROOT + "fonts/NotoSans/NotoSans-Regular.ttf.stream";
    static final String PLEX_OTF = ROOT + "fonts/IBMPlexSans/IBMPlexSans-Regular.otf";
    static final String NOTO_TTF = ROOT + "fonts/NotoSans/NotoSans-Regular.ttf";
    static final int LINES = 60;
    static final String[] SAMPLES = {
        "The quick brown fox jumps over the lazy dog",
        "Ξεσκεπάζω την ψυχοφθόρα βδελυγμία",
        "Съешь же ещё этих мягких французских булок",
    };

    static String line(int p, int l) {
        return SAMPLES[l % 3] + " " + p + "." + l;
    }

    static byte[] pdfjet(String fontPath, int pages) throws Exception {
        ByteArrayOutputStream bos = new ByteArrayOutputStream();
        com.pdfjet.PDF pdf = new com.pdfjet.PDF(bos);
        com.pdfjet.Font font = new com.pdfjet.Font(pdf, fontPath);
        for (int p = 0; p < pages; p++) {
            com.pdfjet.Page page = new com.pdfjet.Page(pdf, com.pdfjet.Letter.PORTRAIT);
            for (int l = 0; l < LINES; l++) {
                page.drawString(font, null, 10f, line(p, l), 50f, 50f + l * 12f);
            }
        }
        pdf.complete();
        return bos.toByteArray();
    }

    static byte[] pdfbox(boolean subset, int pages) throws Exception {
        ByteArrayOutputStream bos = new ByteArrayOutputStream();
        try (org.apache.pdfbox.pdmodel.PDDocument doc = new org.apache.pdfbox.pdmodel.PDDocument()) {
            org.apache.pdfbox.pdmodel.font.PDFont font;
            try (InputStream in = new FileInputStream(NOTO_TTF)) {
                font = org.apache.pdfbox.pdmodel.font.PDType0Font.load(doc, in, subset);
            }
            for (int p = 0; p < pages; p++) {
                org.apache.pdfbox.pdmodel.PDPage page =
                        new org.apache.pdfbox.pdmodel.PDPage(org.apache.pdfbox.pdmodel.common.PDRectangle.LETTER);
                doc.addPage(page);
                try (org.apache.pdfbox.pdmodel.PDPageContentStream cs =
                        new org.apache.pdfbox.pdmodel.PDPageContentStream(doc, page)) {
                    for (int l = 0; l < LINES; l++) {
                        cs.beginText();
                        cs.setFont(font, 10f);
                        cs.newLineAtOffset(50f, 742f - l * 12f);
                        cs.showText(line(p, l));
                        cs.endText();
                    }
                }
            }
            doc.save(bos);
        }
        return bos.toByteArray();
    }

    static byte[] itext(String fontPath, boolean subset, boolean flush, int pages) throws Exception {
        ByteArrayOutputStream bos = new ByteArrayOutputStream();
        try (com.itextpdf.kernel.pdf.PdfDocument doc =
                new com.itextpdf.kernel.pdf.PdfDocument(new com.itextpdf.kernel.pdf.PdfWriter(bos))) {
            com.itextpdf.kernel.font.PdfFont font = com.itextpdf.kernel.font.PdfFontFactory.createFont(
                    fontPath, com.itextpdf.io.font.PdfEncodings.IDENTITY_H,
                    com.itextpdf.kernel.font.PdfFontFactory.EmbeddingStrategy.FORCE_EMBEDDED);
            font.setSubset(subset);
            com.itextpdf.kernel.pdf.PdfPage previous = null;
            for (int p = 0; p < pages; p++) {
                com.itextpdf.kernel.pdf.PdfPage page = doc.addNewPage(com.itextpdf.kernel.geom.PageSize.LETTER);
                com.itextpdf.kernel.pdf.canvas.PdfCanvas cs = new com.itextpdf.kernel.pdf.canvas.PdfCanvas(page);
                for (int l = 0; l < LINES; l++) {
                    cs.beginText();
                    cs.setFontAndSize(font, 10f);
                    cs.moveText(50f, 742f - l * 12f);
                    cs.showText(line(p, l));
                    cs.endText();
                }
                cs.release();
                if (flush && previous != null) {
                    previous.flush();
                }
                previous = page;
            }
        }
        return bos.toByteArray();
    }

    static byte[] run(String config, int pages) throws Exception {
        boolean flush = config.endsWith("-flush");
        String c = flush ? config.substring(0, config.length() - "-flush".length()) : config;
        switch (c) {
        case "jet-plex": return pdfjet(PLEX_STREAM, pages);
        case "jet-noto": return pdfjet(NOTO_STREAM, pages);
        case "box-noto-subset": return pdfbox(true, pages);
        case "box-noto-full": return pdfbox(false, pages);
        case "it-plex-subset": return itext(PLEX_OTF, true, flush, pages);
        case "it-plex-full": return itext(PLEX_OTF, false, flush, pages);
        case "it-noto-subset": return itext(NOTO_TTF, true, flush, pages);
        case "it-noto-full": return itext(NOTO_TTF, false, flush, pages);
        default: throw new IllegalArgumentException(config);
        }
    }

    public static void main(String[] args) throws Exception {
        long start = System.nanoTime();
        String mode = args[0];
        String config = args[1];
        int pages = Integer.parseInt(args[2]);
        if (mode.equals("cold")) {
            byte[] pdf = run(config, pages);
            System.out.printf("%s cold %d pages: %d ms, %d bytes%n",
                    config, pages, (System.nanoTime() - start) / 1_000_000, pdf.length);
            return;
        }
        if (mode.equals("sample")) {
            try (FileOutputStream out = new FileOutputStream(args[3])) {
                out.write(run(config, pages));
            }
            return;
        }
        // Allocations are the bytes the thread allocated per document.
        com.sun.management.ThreadMXBean mx = (com.sun.management.ThreadMXBean) ManagementFactory.getThreadMXBean();
        long t0 = System.nanoTime();
        run(config, pages);
        boolean slow = (System.nanoTime() - t0) > 5_000_000_000L;
        int warmups = slow ? 0 : 2;
        int runs = slow ? 3 : 7;
        for (int i = 0; i < warmups; i++) {
            run(config, pages);
        }
        long[] ms = new long[runs];
        long size = 0;
        long allocated = 0;
        for (int i = 0; i < runs; i++) {
            long before = mx.getCurrentThreadAllocatedBytes();
            long t = System.nanoTime();
            size = run(config, pages).length;
            ms[i] = (System.nanoTime() - t) / 1_000_000;
            allocated = mx.getCurrentThreadAllocatedBytes() - before;
        }
        Arrays.sort(ms);
        System.out.printf("%s %d pages: median %d ms (min %d, max %d, %d runs), %d bytes, %.0f MB allocated%n",
                config, pages, ms[runs / 2], ms[0], ms[runs - 1], runs, size, allocated / 1048576.0);
    }
}
