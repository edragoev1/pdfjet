using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using PDFjet.NET;

/**
 *  Test_07.cs
 */
public class Test_07 {
    public Test_07() {

        PDF pdf = new PDF(
                new BufferedStream(new FileStream("Test_07.pdf", FileMode.Create)),
                Compliance.PDF_A_1B);

        Font f1 = new Font(pdf,
                new FileStream(
                        "fonts/Droid/DroidSerif-Regular.ttf",
                        FileMode.Open,
                        FileAccess.Read));

        Font f2 = new Font(pdf,
                new FileStream(
                        "fonts/Droid/DroidSerif-Italic.ttf",
                        FileMode.Open,
                        FileAccess.Read));

        f1.SetSize(15f);
        f2.SetSize(15f);

        Page page = new Page(pdf, Letter.PORTRAIT);
        
        float x_pos = 70f;
        float y_pos = 70f;
        TextLine text = new TextLine(f1);
        text.SetPosition(x_pos, y_pos);
        StringBuilder buf = new StringBuilder();
        for (int i = 0x20; i < 0x7F; i++) {
            if (i % 16 == 0) {
                text.SetText(buf.ToString());
                text.SetPosition(x_pos, y_pos += 24);
                text.DrawOn(page);
                buf = new StringBuilder();
            }
            buf.Append((char) i);
        }

        y_pos += 24;
        buf = new StringBuilder();
        for (int i = 0x390; i < 0x3EF; i++) {
            if (i % 16 == 0) {
                text.SetText(buf.ToString());
                text.SetPosition(x_pos, y_pos += 24);
                text.DrawOn(page);
                buf = new StringBuilder();
            }
            if (i == 0x3A2 || (i >= 0x3CF && i <= 0x3EF)) {
                // Replace .notdef with space to generate PDF/A compliant PDF
                buf.Append((char) 0x0020);
            }
            else {
                buf.Append((char) i);
            }
        }

        y_pos += 24;
        buf = new StringBuilder();
        for (int i = 0x410; i < 0x46F; i++) {
            if (i % 16 == 0) {
                text.SetText(buf.ToString());
                text.SetPosition(x_pos, y_pos += 24);
                text.DrawOn(page);
                buf = new StringBuilder();
            }
            buf.Append((char) i);
        }


        x_pos = 370;
        y_pos = 70;
        text = new TextLine(f2);
        text.SetPosition(x_pos, y_pos);
        buf = new StringBuilder();
        for (int i = 0x20; i < 0x7F; i++) {
            if (i % 16 == 0) {
                text.SetText(buf.ToString());
                text.SetPosition(x_pos, y_pos += 24);
                text.DrawOn(page);
                buf = new StringBuilder();
            }
            buf.Append((char) i);
        }

        y_pos += 24;
        buf = new StringBuilder();
        for (int i = 0x390; i < 0x3EF; i++) {
            if (i % 16 == 0) {
                text.SetText(buf.ToString());
                text.SetPosition(x_pos, y_pos += 24);
                text.DrawOn(page);
                buf = new StringBuilder();
            }
            if (i == 0x3A2 || (i >= 0x3CF && i <= 0x3EF)) {
                // Replace .notdef with space to generate PDF/A compliant PDF
                buf.Append((char) 0x0020);
            }
            else {
                buf.Append((char) i);
            }
        }
        
        y_pos += 24;
        buf = new StringBuilder();
        for (int i = 0x410; i < 0x46F; i++) {
            if (i % 16 == 0) {
                text.SetText(buf.ToString());
                text.SetPosition(x_pos, y_pos += 24);
                text.DrawOn(page);
                buf = new StringBuilder();
            }
            buf.Append((char) i);
        }

        pdf.Complete();
    }

    public static void Main(String[] args) {
        new Test_07();
    }
}   // End of Test_07.cs
