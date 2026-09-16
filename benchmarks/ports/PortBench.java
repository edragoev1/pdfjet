import java.io.ByteArrayOutputStream;
import java.io.FileOutputStream;
import java.util.Arrays;

import com.pdfjet.Font;
import com.pdfjet.Letter;
import com.pdfjet.PDF;
import com.pdfjet.Page;

/**
 * The text document of section 5 of jet-vs-box.html, written by PDFjet for
 * Java: pages of 60 lines of 10 point Latin, Greek and Cyrillic text in
 * IBM Plex Sans, one drawing call per line.
 *
 * Usage: PortBench bench|cold <pages>
 *        PortBench sample <pages> <file>
 *
 * Run from the root of the repository, as the font path is relative to it.
 */
public class PortBench {
    static final String FONT = "fonts/IBMPlexSans/IBMPlexSans-Regular.otf.stream";
    static final int LINES = 60;
    static final String[] SAMPLES = {
        "The quick brown fox jumps over the lazy dog",
        "Ξεσκεπάζω την ψυχοφθόρα βδελυγμία",
        "Съешь же ещё этих мягких французских булок",
    };

    static byte[] document(int pages) throws Exception {
        ByteArrayOutputStream bos = new ByteArrayOutputStream();
        PDF pdf = new PDF(bos);
        Font font = new Font(pdf, FONT);
        for (int p = 0; p < pages; p++) {
            Page page = new Page(pdf, Letter.PORTRAIT);
            for (int l = 0; l < LINES; l++) {
                page.drawString(font, null, 10f,
                        SAMPLES[l % 3] + " " + p + "." + l, 50f, 50f + l * 12f);
            }
        }
        pdf.complete();
        return bos.toByteArray();
    }

    public static void main(String[] args) throws Exception {
        long start = System.nanoTime();
        String mode = args[0];
        int pages = Integer.parseInt(args[1]);
        if (mode.equals("cold")) {
            byte[] pdf = document(pages);
            System.out.printf("java cold %d pages: %d ms, %d bytes%n",
                    pages, (System.nanoTime() - start) / 1000000, pdf.length);
            return;
        }
        if (mode.equals("sample")) {
            try (FileOutputStream out = new FileOutputStream(args[2])) {
                out.write(document(pages));
            }
            return;
        }
        for (int i = 0; i < 2; i++) {
            document(pages);
        }
        long[] ms = new long[7];
        int size = 0;
        for (int i = 0; i < ms.length; i++) {
            long t = System.nanoTime();
            size = document(pages).length;
            ms[i] = (System.nanoTime() - t) / 1000000;
        }
        Arrays.sort(ms);
        System.out.printf("java %d pages: median %d ms (min %d, max %d, %d runs), %d bytes%n",
                pages, ms[ms.length / 2], ms[0], ms[ms.length - 1], ms.length, size);
    }
}
