package examples;

import java.io.*;
import com.pdfjet.*;

/**
 * Example_28.java
 * Example that shows how to use the NotoSansSymbols font.
 */
public class Example_28 {
    public Example_28() throws Exception {
        PDF pdf = new PDF(
                new BufferedOutputStream(new FileOutputStream("Example_28.pdf")));

        Font f1 = new Font(pdf, "fonts/NotoSansSymbols/NotoSansSymbols-Regular.ttf.stream");
        f1.setSize(24f);

        Page page = new Page(pdf, Letter.LANDSCAPE);

        float x = 50f;
        float y = 50f;
        float dy = 25f;

        TextLine text = new TextLine(f1);
        StringBuilder buf = new StringBuilder();
        for (int i = 0x0041; i <= 0x0080; i++) {
            if (buf.length() < 26) {
                buf.append((char) i);
            } else {
                text.setText(buf.toString());
                text.setLocation(x, y += dy);
                text.drawOn(page);
                buf.setLength(0);
                buf.append((char) i);
            }
        }
        text.setText(buf.toString());
        text.setLocation(x, y += dy);
        text.drawOn(page);
        buf.setLength(0);

        for (int i = 0x24B6; i <= 0x24E9; i++) {
            if (buf.length() < 26) {
                buf.append((char) i);
            } else {
                text.setText(buf.toString());
                text.setLocation(x, y += dy);
                text.drawOn(page);
                buf.setLength(0);
                buf.append((char) i);
            }
        }
        text.setText(buf.toString());
        text.setLocation(x, y += dy);
        text.drawOn(page);
        buf.setLength(0);

        pdf.complete();
    }

    public static void main(String[] args) throws Exception {
        long time0 = System.currentTimeMillis();
        new Example_28();
        long time1 = System.currentTimeMillis();
        TextUtils.printDuration("Example_28", time0, time1);
    }
}   // End of Example_28.java
