import com.pdfjet.*;
import com.pdfjet.fonts.*;
import java.io.*;

public class FontCold {
    public static void main(String[] args) throws Exception {
        String root = System.getProperty("pdfjet.root") + "/";
        long t0 = System.nanoTime();
        PDF pdf = new PDF(new ByteArrayOutputStream());
        long t1 = System.nanoTime();
        new Font(pdf, root + IBMPlexSans.Regular);
        long t2 = System.nanoTime();
        new Font(pdf, root + IBMPlexSans.SemiBold);
        long t3 = System.nanoTime();
        new Font(pdf, root + IBMPlexSans.Italic);
        long t4 = System.nanoTime();
        PDF pdf2 = new PDF(new ByteArrayOutputStream());
        new Font(pdf2, root + IBMPlexSans.Regular);
        long t5 = System.nanoTime();
        System.out.printf("new PDF %.1f, first font %.1f, second %.1f, third %.1f, Regular again in a new PDF %.1f ms%n",
                (t1 - t0) / 1e6, (t2 - t1) / 1e6, (t3 - t2) / 1e6, (t4 - t3) / 1e6, (t5 - t4) / 1e6);
    }
}
