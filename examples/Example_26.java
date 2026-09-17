/*
 * Example_26.java
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
package examples;

import java.io.*;
import com.pdfjet.*;
import com.pdfjet.fonts.*;

/**
 * Example_26.java
 * This example draws a survey with check boxes and radio buttons.
 */
public class Example_26 {
    public Example_26() throws Exception {
        PDF pdf = new PDF(
                new BufferedOutputStream(new FileOutputStream("Example_26.pdf")));

        Font f1 = new Font(pdf, IBMPlexSans.Regular);
        f1.setSize(11f);

        Font f2 = new Font(pdf, IBMPlexSans.SemiBold);
        f2.setSize(12f);

        Page page = new Page(pdf, Letter.PORTRAIT);

        float x = 70f;
        float y = 90f;

        TextLine text = new TextLine(f2, "Customer Survey");
        text.setFontSize(22f);
        text.setLocation(x, y);
        text.drawOn(page);

        // Check boxes, one below the other.
        y += 50f;
        new TextLine(f2, "Which PDFjet ports do you use?").setLocation(x, y).drawOn(page);

        y += 15f;
        new CheckBox(f1, "Java")
                .setLocation(x, y)
                .setCheckmarkColor(Color.blue)
                .check(Mark.CHECK)
                .drawOn(page);

        y += 25f;
        new CheckBox(f1, "C#")
                .setLocation(x, y)
                .setCheckmarkColor(Color.blue)
                .check(Mark.CHECK)
                .drawOn(page);

        y += 25f;
        new CheckBox(f1, "Swift")
                .setLocation(x, y)
                .drawOn(page);

        y += 25f;
        new CheckBox(f1, "Go")
                .setLocation(x, y)
                .drawOn(page);

        // Radio buttons in a row. Each one starts where the one before it ends.
        y += 50f;
        new TextLine(f2, "How did you hear about PDFjet?").setLocation(x, y).drawOn(page);

        y += 15f;
        float[] xy = new RadioButton(f1, "Web search")
                .setLocation(x, y)
                .select(true)
                .drawOn(page);

        xy = new RadioButton(f1, "A colleague")
                .setLocation(xy[0] + 20f, y)
                .drawOn(page);

        new RadioButton(f1, "Other")
                .setLocation(xy[0] + 20f, y)
                .drawOn(page);

        y += 50f;
        new TextLine(f2, "Would you recommend PDFjet?").setLocation(x, y).drawOn(page);

        y += 15f;
        xy = new RadioButton(f1, "Yes")
                .setLocation(x, y)
                .select(true)
                .drawOn(page);

        new RadioButton(f1, "No")
                .setLocation(xy[0] + 20f, y)
                .drawOn(page);

        // A check box marked with an X, and one with a link.
        y += 50f;
        new TextLine(f2, "Stay in touch").setLocation(x, y).drawOn(page);

        y += 15f;
        new CheckBox(f1, "Send me news about new releases")
                .setLocation(x, y)
                .setCheckmarkColor(Color.red)
                .check(Mark.X)
                .drawOn(page);

        y += 25f;
        xy = new CheckBox(f1, "Visit https://pdfjet.com")
                .setLocation(x, y)
                .setURIAction("https://pdfjet.com")
                .drawOn(page);

        // A border around the survey.
        Rect rect = new Rect(50f, 50f, 512f, xy[1] + 25f - 50f);
        rect.setBorderColor(Color.lightgray);
        rect.drawOn(page);

        pdf.complete();
    }

    public static void main(String[] args) throws Exception {
        long time0 = System.currentTimeMillis();
        new Example_26();
        long time1 = System.currentTimeMillis();
        System.out.printf("Example_26 => %4d ms%n", time1 - time0);
    }
}   // End of Example_26.java
