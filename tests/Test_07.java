import java.io.*;
import com.pdfjet.*;

/**
 *  Test_07.java
 */
public class Test_07 {
    public Test_07() throws Exception {
        PDF pdf = new PDF(
                new FileOutputStream("Test_07.pdf"), Compliance.PDF_A_1B);

        Font f1 = new Font(pdf,
                new FileInputStream("fonts/Droid/DroidSerif-Regular.ttf"));
        Font f2 = new Font(pdf,
                new FileInputStream("fonts/Droid/DroidSerif-Italic.ttf"));

        f1.setSize(15f);
        f2.setSize(15f);

        Page page = new Page(pdf, Letter.PORTRAIT);
        
        float x_pos = 70f;
        float y_pos = 70f;
        TextLine text = new TextLine(f1);
        text.setPosition(x_pos, y_pos);
        StringBuilder buf = new StringBuilder();

        for (int i = 0x20; i < 0x7F; i++) {
            if (i % 16 == 0) {
                text.setText(buf.toString());
                text.setPosition(x_pos, y_pos += 24);
                text.drawOn(page);
                buf = new StringBuilder();
            }
            buf.append((char) i);
        }

        text.setText(buf.toString());
        text.setPosition(x_pos, y_pos += 24);
        text.drawOn(page);

        y_pos += 24;
        buf = new StringBuilder();
        for (int i = 0x390; i < 0x3EF; i++) {
            if (i % 16 == 0) {
                text.setText(buf.toString());
                text.setPosition(x_pos, y_pos += 24);
                text.drawOn(page);
                buf = new StringBuilder();
            }
            if (i == 0x3A2 || (i >= 0x3CF && i <= 0x3EF)) {
                // Replace .notdef with space to generate PDF/A compliant PDF
                buf.append((char) 0x0020);
            }
            else {
                buf.append((char) i);
            }
        }

        y_pos += 24;
        buf = new StringBuilder();
        for (int i = 0x410; i <= 0x46F; i++) {
            if (i % 16 == 0) {
                text.setText(buf.toString());
                text.setPosition(x_pos, y_pos += 24);
                text.drawOn(page);
                buf = new StringBuilder();
            }
            buf.append((char) i);
        }


        x_pos = 370;
        y_pos = 70;
        text = new TextLine(f2);
        text.setPosition(x_pos, y_pos);
        buf = new StringBuilder();
        for (int i = 0x20; i < 0x7F; i++) {
            if (i % 16 == 0) {
                text.setText(buf.toString());
                text.setPosition(x_pos, y_pos += 24);
                text.drawOn(page);
                buf = new StringBuilder();
            }
            buf.append((char) i);
        }
        text.setText(buf.toString());
        text.setPosition(x_pos, y_pos += 24);
        text.drawOn(page);

        y_pos += 24;
        buf = new StringBuilder();
        for (int i = 0x390; i < 0x3EF; i++) {
            if (i % 16 == 0) {
                text.setText(buf.toString());
                text.setPosition(x_pos, y_pos += 24);
                text.drawOn(page);
                buf = new StringBuilder();
            }
            if (i == 0x3A2 || (i >= 0x3CF && i <= 0x3EF)) {
                // Replace .notdef with space to generate PDF/A compliant PDF
                buf.append((char) 0x0020);
            }
            else {
                buf.append((char) i);
            }
        }
        
        y_pos += 24;
        buf = new StringBuilder();
        for (int i = 0x410; i < 0x46F; i++) {
            if (i % 16 == 0) {
                text.setText(buf.toString());
                text.setPosition(x_pos, y_pos += 24);
                text.drawOn(page);
                buf = new StringBuilder();
            }
            buf.append((char) i);
        }

        pdf.complete();
    }

    public static void main(String[] args) throws Exception {
        new Test_07();
    }
}   // End of Test_07.java
