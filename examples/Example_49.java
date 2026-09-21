/*
 * Example_49.java
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
package examples;

import java.io.*;
import java.util.*;
import com.pdfjet.*;
import com.pdfjet.fonts.*;

/**
 * Example_49.java
 * This example draws a menu with paragraphs that mix fonts, sizes and colors,
 * currency signs raised with a vertical offset, and a rotated, underlined label.
 */
public class Example_49 {
    public Example_49() throws Exception {
        PDF pdf = new PDF(
                new BufferedOutputStream(new FileOutputStream("Example_49.pdf")));
        pdf.setCompliance(Compliance.PDF_UA_1);
        pdf.setTitle("Paragraphs with mixed text styles");

        Font f1 = new Font(pdf, SourceSerif4.Regular);
        f1.setSize(14f);

        Font f2 = new Font(pdf, SourceSerif4.Italic);
        f2.setSize(14f);

        Font f3 = new Font(pdf, SourceSerif4.SemiBold);
        f3.setSize(14f);

        Font f4 = new Font(pdf, SourceSerif4.SemiBold);
        f4.setSize(9f);

        Page page = new Page(pdf, Letter.PORTRAIT);

        TextLine title = new TextLine(f3, "Café Menu");
        title.setStructureType(StructElem.H1);
        title.setFontSize(28f);
        title.setLocation(70f, 100f);
        title.drawOn(page);

        // Each paragraph mixes a name, an italic description and a price
        // with a small dollar sign raised by a vertical offset.
        String[] names = {"Espresso", "Cappuccino", "Hot chocolate"};
        String[] notes = {"rich and intense", "with steamed milk foam", "made with dark cocoa"};
        String[] prices = {"3.25", "4.50", "3.95"};

        TextColumn column = new TextColumn();
        for (int i = 0; i < names.length; i++) {
            Paragraph paragraph = new Paragraph()
                    .add(new TextLine(f3, names[i]))
                    .add(new TextLine(f2, notes[i]).setTextColor(Color.gray))
                    .add(new TextLine(f4, "$").setVerticalOffset(-4f))
                    .add(new TextLine(f1, prices[i]).setTextColor(Color.darkred));
            column.addParagraph(paragraph);
        }

        // A paragraph that colors some of its words, aligned to the right.
        column.addParagraph(new Paragraph()
                .add(new TextLine(f2, "Freshly"))
                .add(new TextLine(f3, "roasted").setTextColor(Color.saddlebrown))
                .add(new TextLine(f2, "every"))
                .add(new TextLine(f3, "morning").setTextColor(Color.darkorange))
                .setTextAlignment(Alignment.RIGHT));

        column.setLocation(70f, 140f);
        column.setWidth(470f);
        column.setParagraphSpacing(1.8f);
        float[] xy = column.drawOn(page);

        // A TextFrame wraps the words of its paragraphs to its width.
        List<Paragraph> paragraphs = new ArrayList<Paragraph>();
        paragraphs.add(new Paragraph()
                .add(new TextLine(f1, "Our beans come from small farms in"))
                .add(new TextLine(f3, "Colombia,"))
                .add(new TextLine(f3, "Ethiopia"))
                .add(new TextLine(f1, "and"))
                .add(new TextLine(f3, "Guatemala,"))
                .add(new TextLine(f1, "and we roast them in small batches."))
                .add(new TextLine(f2, "Ask us about the beans of the week.").setTextColor(Color.darkred)));
        paragraphs.add(new Paragraph()
                .add(new TextLine(f2, "Prices include tax.").setTextColor(Color.gray)));

        TextFrame frame = new TextFrame(paragraphs);
        frame.setLocation(70f, xy[1] + 30f);
        frame.setWidth(470f);
        frame.drawOn(page);

        TextLine label = new TextLine(f3, "Today's special!");
        label.setFontSize(18f);
        label.setTextColor(Color.red);
        label.setLocation(400f, 90f);
        label.setTextRotation(15);
        label.setUnderline(true);
        label.drawOn(page);

        pdf.complete();
    }

    public static void main(String[] args) throws Exception {
        long time0 = System.currentTimeMillis();
        new Example_49();
        long time1 = System.currentTimeMillis();
        System.out.printf("Example_49 => %4d ms%n", time1 - time0);
    }
}   // End of Example_49.java
