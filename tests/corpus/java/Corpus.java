/**
 *  Corpus.java
 *
Copyright (c) 2026 PDFjet Software
Licensed under the MIT License. See LICENSE file in the project root.
*/

import java.io.BufferedOutputStream;
import java.io.ByteArrayInputStream;
import java.io.File;
import java.io.FileOutputStream;
import java.io.OutputStream;
import java.nio.file.Files;
import java.nio.file.Paths;
import java.util.LinkedHashMap;
import java.util.List;
import java.util.Map;

import com.pdfjet.PDF;
import com.pdfjet.PDFobj;
import com.pdfjet.PageSize;

/**
 * The Java port's side of the corpus check, tests/corpus/check-corpus.py, as
 * tests/corpus/go/main.go is the Go port's. It reads one PDF, merges the whole
 * of it into a document of its own, and splits its first and its last page
 * into two more:
 *
 *     java -cp CLASSES:. Corpus IN.pdf PASSWORD OUTDIR
 *
 * It writes merged.pdf, first.pdf and last.pdf into OUTDIR, the ones it could
 * make, and prints what it read as JSON: the error, or the number of pages and
 * the size of each page.
 *
 * The reader refuses a malformed PDF with a checked exception, most often a
 * plain Exception, and the writer refuses a misuse with an
 * IllegalArgumentException or an IllegalStateException. Any other unchecked
 * exception -- a NullPointerException, an index out of bounds, a
 * NumberFormatException that nothing caught -- and any Error, like an
 * OutOfMemoryError or a StackOverflowError, is a programming error of the
 * port and is reported as a crash.
 */
public final class Corpus {

    private String error;
    private String crash;
    private int pages;
    private final StringBuilder sizes = new StringBuilder();
    private final Map<String, String> made = new LinkedHashMap<String, String>();

    // Returns what t is if it is a crash, or null if it is a refusal.
    static String crashOf(Throwable t) {
        if (t instanceof Error
                || (t instanceof RuntimeException
                        && t.getClass() != IllegalArgumentException.class
                        && t.getClass() != IllegalStateException.class)) {
            StringBuilder sb = new StringBuilder(t.toString());
            for (StackTraceElement e : t.getStackTrace()) {
                if (e.getClassName().startsWith("com.pdfjet.")) {
                    sb.append(" at ").append(e);
                    break;
                }
            }
            return sb.toString();
        }
        return null;
    }

    static String messageOf(Throwable t) {
        return (t.getMessage() != null) ? t.getMessage() : t.toString();
    }

    static List<PDFobj> read(byte[] data, String password) throws Exception {
        return new PDF().read(new ByteArrayInputStream(data), password);
    }

    // Merges the pages into dir/name, and returns "ok" or the error.
    String write(byte[] data, String password, File dir, String name, int... pageNumbers) {
        File file = new File(dir, name);
        try {
            // Each document is read again: a merge may change what it merges.
            List<PDFobj> objects = read(data, password);
            OutputStream os = new BufferedOutputStream(new FileOutputStream(file));
            try {
                PDF pdf = new PDF(os);
                if (pageNumbers.length == 0) {
                    pdf.merge(objects);
                } else {
                    pdf.merge(objects, pageNumbers);
                }
                pdf.complete();
            } finally {
                os.close();
            }
            return "ok";
        } catch (Throwable t) {
            String c = crashOf(t);
            if (c != null) {
                crash = name + ": " + c;
            }
            file.delete();
            return messageOf(t);
        }
    }

    void run(String path, String password, String outdir) {
        byte[] data;
        try {
            data = Files.readAllBytes(Paths.get(path));
        } catch (Exception e) {
            error = messageOf(e);
            return;
        }
        List<PDFobj> objects;
        try {
            objects = read(data, password);
        } catch (Throwable t) {
            crash = crashOf(t);
            error = messageOf(t);
            return;
        }
        List<PDFobj> pageObjects = new PDF().getPageObjects(objects);
        pages = pageObjects.size();
        for (PDFobj page : pageObjects) {
            PageSize size = page.getPageSize();
            if (sizes.length() > 0) {
                sizes.append(',');
            }
            sizes.append('[').append(number(size.getWidth()))
                    .append(',').append(number(size.getHeight())).append(']');
        }
        File dir = new File(outdir);
        made.put("merged", write(data, password, dir, "merged.pdf"));
        if (pages > 0) {
            made.put("first", write(data, password, dir, "first.pdf", 1));
            made.put("last", write(data, password, dir, "last.pdf", pages));
        }
    }

    static String number(float f) {
        if (Float.isNaN(f) || Float.isInfinite(f)) {
            return "null";
        }
        return Float.toString(f);
    }

    static String quote(String s) {
        StringBuilder sb = new StringBuilder("\"");
        for (int i = 0; i < s.length(); i++) {
            char c = s.charAt(i);
            if (c == '"' || c == '\\') {
                sb.append('\\').append(c);
            } else if (c < 0x20 || c > 0x7e) {
                sb.append(String.format("\\u%04x", (int) c));
            } else {
                sb.append(c);
            }
        }
        return sb.append('"').toString();
    }

    String json() {
        StringBuilder sb = new StringBuilder("{");
        if (error != null) {
            sb.append("\"error\":").append(quote(error)).append(',');
        }
        if (crash != null) {
            sb.append("\"crash\":").append(quote(crash)).append(',');
        }
        sb.append("\"pages\":").append(pages);
        sb.append(",\"sizes\":[").append(sizes).append("],\"made\":{");
        String separator = "";
        for (Map.Entry<String, String> entry : made.entrySet()) {
            sb.append(separator).append(quote(entry.getKey())).append(':').append(quote(entry.getValue()));
            separator = ",";
        }
        return sb.append("}}").toString();
    }

    public static void main(String[] args) {
        if (args.length != 3) {
            System.err.println("usage: java -cp CLASSES:. Corpus IN.pdf PASSWORD OUTDIR");
            System.exit(2);
        }
        Corpus out = new Corpus();
        try {
            out.run(args[0], args[1], args[2]);
        } catch (Throwable t) {
            out.crash = crashOf(t);
            if (out.crash == null) {
                out.crash = t.toString();
            }
        }
        System.out.println(out.json());
    }
}
