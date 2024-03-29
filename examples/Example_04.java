package examples;

import java.io.*;
import com.pdfjet.*;

/**
 *  Example_04.java
 *
 *  The PDF generated by this example program will only work with
 *  Adobe Reader 8 or Foxit Reader v2.0 or higher versions. It also
 *  requires the Asian Font Packs from Adobe or Foxit Software respectively.
 */
public class Example_04 {
    public Example_04() throws Exception {
        PDF pdf = new PDF(
                new BufferedOutputStream(new FileOutputStream("Example_04.pdf")));

        // Chinese (Traditional) font
        Font f1 = new Font(pdf, CJKFont.ADOBE_MING_STD_LIGHT);

        // Chinese (Simplified) font
        Font f2 = new Font(pdf, CJKFont.ST_HEITI_SC_LIGHT);

        // Japanese font
        Font f3 = new Font(pdf, CJKFont.KOZ_MIN_PRO_VI_REGULAR);

        // Korean font
        Font f4 = new Font(pdf, CJKFont.ADOBE_MYUNGJO_STD_MEDIUM);

        Page page = new Page(pdf, Letter.PORTRAIT);

        f1.setSize(14f);
        f2.setSize(14f);
        f3.setSize(14f);
        f4.setSize(14f);

        String fileName = "data/happy-new-year.txt";
        float x_pos = 100f;
        float y_pos = 100f;
        BufferedReader reader = new BufferedReader(
                new InputStreamReader(new FileInputStream(fileName), "UTF-8"));
        TextLine text = new TextLine(f1);
        String line = null;
        while ((line = reader.readLine()) != null) {
            if (line.contains("Simplified")) {
                text.setFont(f2);
            } else if (line.contains("Japanese")) {
                text.setFont(f3);
            } else if (line.contains("Korean")) {
                text.setFont(f4);
            }
            text.setText(line);
            text.setLocation(x_pos, y_pos);
            text.drawOn(page);
            y_pos += 25f;
        }
        reader.close();

        pdf.complete();
    }

    public static void main(String[] args) throws Exception {
        long time0 = System.currentTimeMillis();
        new Example_04();
        long time1 = System.currentTimeMillis();
        TextUtils.printDuration("Example_04", time0, time1);
    }
}   // End of Example_04.java
