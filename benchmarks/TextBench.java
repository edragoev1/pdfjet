import java.io.*;
import java.lang.management.ManagementFactory;
import java.util.*;

/**
 * Writes a multilingual document with PDFjet: pages of 60 lines of 10 point
 * Latin, Greek and Cyrillic text, one drawing call per line, with an embedded
 * font.
 *
 * Configurations: jet-plex (IBM Plex Sans) and jet-noto (Noto Sans).
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

    static byte[] run(String config, int pages) throws Exception {
        switch (config) {
        case "jet-plex": return pdfjet(PLEX_STREAM, pages);
        case "jet-noto": return pdfjet(NOTO_STREAM, pages);
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
