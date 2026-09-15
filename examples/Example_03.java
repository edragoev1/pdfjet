package examples;

import java.io.*;
import java.util.*;
import com.pdfjet.*;
import com.pdfjet.fonts.*;

/**
 * Example_03.java
 */
public class Example_03 {
    public Example_03() throws Exception {
        PDF pdf = new PDF(
                new BufferedOutputStream(
                        new FileOutputStream("Example_03.pdf")));

        Font f1 = new Font(pdf, IBMPlexSans.Regular);
        f1.setSize(10f);

        Font f2 = new Font(pdf, IBMPlexSans.Bold);
        f2.setSize(10f);

        Font f3 = new Font(pdf, IBMPlexSans.Italic);
        f3.setSize(10f);

        Page page = new Page(pdf, Letter.PORTRAIT);

        List<Paragraph> paragraphs = new ArrayList<Paragraph>();
        Paragraph paragraph = new Paragraph()
                .add(new TextLine(f1,
"The small business centres offer practical resources, from step-by-step info on setting up your business to sample business plans to a range of business-related articles and books in our resource libraries.")
                        .setUnderline(true))
                .add(new TextLine(f2, "This text is bold!").setTextColor(Color.blue));
        paragraphs.add(paragraph);

        paragraph = new Paragraph()
                .add(new TextLine(f1,
"The centres also offer free one-on-one consultations with business advisors who can review your business plan and make recommendations to improve it.")
                        .setUnderline(true))
                .add(new TextLine(f3, "This text is using italic font.").setTextColor(Color.green));
        paragraphs.add(paragraph);

        TextFrame text = new TextFrame(paragraphs);
        text.setLocation(70f, 50f);
        text.setWidth(500f);
        text.setBorders(true);
        text.setBorderColor(Color.blue);
        text.drawOn(page);

        int paragraphNumber = 1;
        for (Paragraph p : paragraphs) {
            if (p.startsWith("**")) {
                paragraphNumber = 1;
            } else {
                new TextLine(f2, String.valueOf(paragraphNumber) + ".")
                        .setLocation(p.getTextX() - 15f, p.getTextY())
                        .drawOn(page);
                paragraphNumber++;
            }
        }

        Map<String, Integer> colorMap = new HashMap<String, Integer>();
        colorMap.put("Physics", Color.red);
        colorMap.put("physics", Color.red);
        colorMap.put("Experimentation", Color.orange);
        colorMap.put("science", Color.blue);
        paragraphs = Paragraph.paragraphsFromFile(f1, "data/physics.txt");
        for (Paragraph p : paragraphs) {
            if (p.startsWith("**")) {
                p.getTextLines().get(0).setFont(f2).setFontSize(24f);
                p.getTextLines().get(0).setTextColor(Color.navy);
            } else {
                p.setTextColor(Color.gray);
                p.setHighlightColors(colorMap);
            }
        }

        text = new TextFrame(paragraphs);
        text.setLocation(70f, 150f);
        text.setWidth(500f);
        text.setBorders(true);
        text.setBorderColor(Color.blue);
        text.drawOn(page);

        paragraphNumber = 1;
        for (Paragraph p : paragraphs) {
            if (p.startsWith("**")) {
                paragraphNumber = 1;
            } else {
                new TextLine(f2, String.valueOf(paragraphNumber) + ".")
                        .setLocation(p.getTextX() - 15f, p.getTextY())
                        .drawOn(page);
                new Line(p.getX1() - 3f, p.getY1(), p.getX1() - 3f, p.getY2())
                        .setStrokeColor(Color.navy)
                        .setStrokeWidth(1f).drawOn(page);
                paragraphNumber++;
            }
        }

        pdf.complete();
    }

    public static void main(String[] args) throws Exception {
        long time0 = System.currentTimeMillis();
        new Example_03();
        long time1 = System.currentTimeMillis();
        System.out.printf("Example_03 => %4d ms%n", time1 - time0);
    }
}   // End of Example_03.java
