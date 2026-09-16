import java.io.BufferedReader;
import java.io.FileInputStream;
import java.io.FileOutputStream;
import java.io.InputStreamReader;
import java.io.OutputStream;
import java.nio.charset.StandardCharsets;
import java.util.ArrayList;
import java.util.Arrays;
import java.util.List;

import com.pdfjet.Alignment;
import com.pdfjet.Cell;
import com.pdfjet.Font;
import com.pdfjet.Letter;
import com.pdfjet.PDF;
import com.pdfjet.Page;
import com.pdfjet.Table;

/**
 * Table at the scale the TODO asks about: the 9 columns of the Electric
 * Vehicle Population CSV built as Cell objects and drawn with Table on as
 * many Letter pages as they need.
 *
 * The widths are narrow enough that the last two columns wrap, so the
 * benchmark measures the wrapping as well as the drawing; the rows of the
 * file are repeated until the table has the number of rows asked for.
 *
 * Usage: TableBench bench|cold <rows>
 *        TableBench sample <rows> <file>
 *
 * Run from the root of the repository, as the paths are relative to it.
 */
public class TableBench {
    static final String CSV = "data/Electric_Vehicle_Population_10_Pages.csv";
    static final String SEMIBOLD = "fonts/IBMPlexSans/IBMPlexSans-SemiBold.otf.stream";
    static final String REGULAR = "fonts/IBMPlexSans/IBMPlexSans-Regular.otf.stream";
    static final int COLUMNS = 9;
    static final float[] WIDTHS = {68f, 58f, 58f, 26f, 44f, 34f, 52f, 60f, 152f};

    // Counts the bytes written, so that the output costs no memory or disk.
    static final class Sink extends OutputStream {
        long n;
        @Override public void write(int b) { n++; }
        @Override public void write(byte[] b, int off, int len) { n += len; }
    }

    // The first COLUMNS fields of every line of the CSV, the header first.
    static List<String[]> readFields() throws Exception {
        List<String[]> lines = new ArrayList<String[]>();
        BufferedReader reader = new BufferedReader(
                new InputStreamReader(new FileInputStream(CSV), StandardCharsets.UTF_8));
        String line;
        while ((line = reader.readLine()) != null) {
            String[] fields = line.split(",", -1);
            if (fields.length < COLUMNS) {
                continue;
            }
            lines.add(Arrays.copyOf(fields, COLUMNS));
        }
        reader.close();
        return lines;
    }

    static List<List<Cell>> tableData(List<String[]> fields, int rows, Font f1, Font f2) {
        List<List<Cell>> data = new ArrayList<List<Cell>>();
        for (int r = 0; r <= rows; r++) {
            // Row 0 is the header; the data rows repeat the file from line 1.
            String[] values = (r == 0) ? fields.get(0) : fields.get(1 + (r - 1) % (fields.size() - 1));
            List<Cell> row = new ArrayList<Cell>();
            for (int c = 0; c < COLUMNS; c++) {
                Cell cell = new Cell((r == 0) ? f1 : f2, values[c]);
                cell.setWidth(WIDTHS[c]);
                cell.setTextAlignment(c == 4 ? Alignment.RIGHT : Alignment.LEFT);
                row.add(cell);
            }
            data.add(row);
        }
        return data;
    }

    static int document(List<String[]> fields, int rows, OutputStream out) throws Exception {
        PDF pdf = new PDF(out);
        Font f1 = new Font(pdf, SEMIBOLD);
        f1.setSize(8f);
        Font f2 = new Font(pdf, REGULAR);
        f2.setSize(8f);

        Table table = new Table();
        table.setTableData(tableData(fields, rows, f1, f2), 1);
        table.setLocation(20f, 20f);
        table.setBottomMargin(20f);

        List<Page> pages = new ArrayList<Page>();
        table.drawOn(pdf, pages, Letter.PORTRAIT);
        for (Page page : pages) {
            pdf.addPage(page);
        }
        pdf.complete();
        return pages.size();
    }

    static long run(List<String[]> fields, int rows, long[] pages) throws Exception {
        Sink sink = new Sink();
        pages[0] = document(fields, rows, sink);
        return sink.n;
    }

    public static void main(String[] args) throws Exception {
        long start = System.nanoTime();
        String mode = args[0];
        int rows = Integer.parseInt(args[1]);
        List<String[]> fields = readFields();
        long[] pages = new long[1];
        if (mode.equals("cold")) {
            long bytes = run(fields, rows, pages);
            System.out.printf("java cold %d rows: %d ms, %d pages, %d bytes%n",
                    rows, (System.nanoTime() - start) / 1000000, pages[0], bytes);
            return;
        }
        if (mode.equals("sample")) {
            FileOutputStream out = new FileOutputStream(args[2]);
            document(fields, rows, out);
            out.close();
            return;
        }
        for (int i = 0; i < 2; i++) {
            run(fields, rows, pages);
        }
        long[] ms = new long[7];
        long bytes = 0;
        for (int i = 0; i < ms.length; i++) {
            long t = System.nanoTime();
            bytes = run(fields, rows, pages);
            ms[i] = (System.nanoTime() - t) / 1000000;
        }
        Arrays.sort(ms);
        System.out.printf("java %d rows: median %d ms (min %d, max %d, %d runs), %d pages, %d bytes%n",
                rows, ms[ms.length / 2], ms[0], ms[ms.length - 1], ms.length, pages[0], bytes);
    }
}
