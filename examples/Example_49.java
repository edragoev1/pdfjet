package examples;

import java.io.*;
import java.util.*;
import com.pdfjet.*;

/**
 * Example_49.java
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
        f2.setSize(16f);

        Page page = new Page(pdf, Letter.PORTRAIT);

        Paragraph paragraph1 = new Paragraph()
                .add(new TextLine(f1, "Hello"))
                .add(new TextLine(f1, "W").setTextColor(Color.black))
                .add(new TextLine(f1, "o").setTextColor(Color.red))
                .add(new TextLine(f1, "r").setTextColor(Color.green))
                .add(new TextLine(f1, "l").setTextColor(Color.blue))
                .add(new TextLine(f1, "d").setTextColor(Color.black))
                .add(new TextLine(f1, "$").setVerticalOffset(1f))
                .add(new TextLine(f2, "29.95").setTextColor(Color.blue))
                .setAlignment(Align.RIGHT);

        Paragraph paragraph2 = new Paragraph()
                .add(new TextLine(f1, "Hello"))
                .add(new TextLine(f1, "World"))
                .add(new TextLine(f1, "$"))
                .add(new TextLine(f2, "29.95").setTextColor(Color.blue))
                .setAlignment(Align.RIGHT);

        TextColumn column = new TextColumn();
        column.addParagraph(paragraph1);
        column.addParagraph(paragraph2);
        column.setLocation(70f, 150f);
        column.setWidth(500f);
        column.drawOn(page);

        List<Paragraph> paragraphs = new ArrayList<Paragraph>();
        paragraphs.add(paragraph1);
        paragraphs.add(paragraph2);

        Text text = new Text(paragraphs);
        text.setLocation(70f, 200f);
        text.setWidth(500f);
        text.drawOn(page);

        TextLine textLine = new TextLine(f1, "Hello, World!");
        textLine.setLocation(100f, 300f);
        textLine.setTextDirection(30);
        textLine.setVerticalOffset(50f);
        textLine.setUnderline(true);
        textLine.drawOn(page);

        pdf.complete();
    }

    public static void main(String[] args) throws Exception {
        long time0 = System.currentTimeMillis();
        new Example_49();
        long time1 = System.currentTimeMillis();
        TextUtils.printDuration("Example_49", time0, time1);
    }
}   // End of Example_49.java
