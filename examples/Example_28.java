package examples;

import java.io.*;
import com.pdfjet.*;

/**
 *  Example_28.java
 *  Example that shows how to use fallback font and the NotoSans symbols font.
 */
public class Example_28 {
    public Example_28() throws Exception {
        PDF pdf = new PDF(
                new BufferedOutputStream(new FileOutputStream("Example_28.pdf")));

        Font f1 = new Font(pdf, "fonts/NotoSansTC/NotoSansTC-Regular.ttf.stream");
        f1.setSize(11f);

        Font f2 = new Font(pdf, "fonts/NotoSansSC/NotoSansSC-Regular.ttf.stream");
        f2.setSize(11f);

        Font f3 = new Font(pdf, "fonts/NotoSansSymbols/NotoSansSymbols-Regular.ttf.stream");
        f3.setSize(11f);

        Page page = new Page(pdf, Letter.LANDSCAPE);

        BufferedReader reader = new BufferedReader(
                new InputStreamReader(
                        new FileInputStream("data/report.csv"), "UTF-8"));

        float y = 40f;
        String str = null;
        while ((str = reader.readLine()) != null) {
            new TextLine(f1, str)
                    .setFallbackFont(f2)
                    .setLocation(50f, y += 20f)
                    .drawOn(page);
        }
        reader.close();

        float x = 50f;
        y = 210f;
        float dy = 22f;

        TextLine text = new TextLine(f3);
        StringBuilder buf = new StringBuilder();
        int count = 0;
        for (int i = 0x2200; i <= 0x22FF; i++) {
            // Draw the Math Symbols
            if (count % 80 == 0) {
                text.setText(buf.toString());
                text.setLocation(x, y += dy);
                text.drawOn(page);
                buf.setLength(0);
            }
            buf.append((char) i);
            count++;
        }
        text.setText(buf.toString());
        text.setLocation(x, y += dy);
        text.drawOn(page);
        buf.setLength(0);

        count = 0;
        for (int i = 0x25A0; i <= 0x25FF; i++) {
            // Draw the Geometric Shapes
            if (count % 80 == 0) {
                text.setText(buf.toString());
                text.setLocation(x, y += dy);
                text.drawOn(page);
                buf.setLength(0);
            }
            buf.append((char) i);
            count++;
        }
        text.setText(buf.toString());
        text.setLocation(x, y += dy);
        text.drawOn(page);
        buf.setLength(0);

        count = 0;
        for (int i = 0x2701; i <= 0x27ff; i++) {
            // Draw the Dingbats
            if (count % 80 == 0) {
                text.setText(buf.toString());
                text.setLocation(x, y += dy);
                text.drawOn(page);
                buf.setLength(0);
            }
            buf.append((char) i);
            count++;
        }
        text.setText(buf.toString());
        text.setLocation(x, y += dy);
        text.drawOn(page);
        buf.setLength(0);

        count = 0;
        for (int i = 0x2800; i <= 0x28FF; i++) {
            // Draw the Braille Patterns
            if (count % 80 == 0) {
                text.setText(buf.toString());
                text.setLocation(x, y += dy);
                text.drawOn(page);
                buf.setLength(0);
            }
            buf.append((char) i);
            count++;
        }
        text.setText(buf.toString());
        text.setLocation(x, y);
        text.drawOn(page);

        pdf.complete();
    }

    public static void main(String[] args) throws Exception {
        long time0 = System.currentTimeMillis();
        new Example_28();
        long time1 = System.currentTimeMillis();
        TextUtils.printDuration("Example_28", time0, time1);
    }
}   // End of Example_28.java
