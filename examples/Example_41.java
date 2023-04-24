package examples;

import java.io.*;
import java.util.*;
import com.pdfjet.*;

/**
 *  Example_41.java
 */
public class Example_41 {
    public Example_41() throws Exception {
        PDF pdf = new PDF(
                new BufferedOutputStream(
                        new FileOutputStream("Example_41.pdf")));

        Font f1 = new Font(pdf, CoreFont.HELVETICA);
        Font f2 = new Font(pdf, CoreFont.HELVETICA_BOLD);
        Font f3 = new Font(pdf, CoreFont.HELVETICA_OBLIQUE);

        f1.setSize(10f);
        f2.setSize(10f);
        f3.setSize(10f);

        Page page = new Page(pdf, Letter.PORTRAIT);

        List<Paragraph> paragraphs = new ArrayList<Paragraph>();
        Paragraph paragraph = new Paragraph()
                .add(new TextLine(f1,
"The small business centres offer practical resources, from step-by-step info on setting up your business to sample business plans to a range of business-related articles and books in our resource libraries.")
                        .setUnderline(true))
                .add(new TextLine(f2, "This text is bold!").setColor(Color.blue));
        paragraphs.add(paragraph);

        paragraph = new Paragraph()
                .add(new TextLine(f1,
"The centres also offer free one-on-one consultations with business advisors who can review your business plan and make recommendations to improve it.")
                        .setUnderline(true))
                .add(new TextLine(f3, "This text is using italic font.").setColor(Color.green));
        paragraphs.add(paragraph);

        Text text = new Text(paragraphs);
        text.setLocation(70f, 50f);
        text.setWidth(500f);
        text.setBorder(true);
        text.drawOn(page);

        int paragraphNumber = 1;
        for (Paragraph p : paragraphs) {
            if (p.startsWith("**")) {
                paragraphNumber = 1;
            } else {
                new TextLine(f2, String.valueOf(paragraphNumber) + ".")
                        .setLocation(p.xText - 15f, p.yText)
                        .drawOn(page);
                paragraphNumber++;
            }
        }

        Map<String, Integer> colorMap = new HashMap<String, Integer>();
        colorMap.put("Physics", Color.red);
        colorMap.put("physics", Color.red);
        colorMap.put("Experimentation", Color.orange);
        colorMap.put("that", Color.blue);
        paragraphs = Text.paragraphsFromFile(f1, "data/physics.txt");
        float f2size = f2.getSize();
        for (Paragraph p : paragraphs) {
            if (p.startsWith("**")) {
                f2.setSize(24.0);
                p.getTextLines().get(0).setFont(f2);
                p.getTextLines().get(0).setColor(Color.navy);
            } else {
                p.setColor(Color.gray);
                p.setColorMap(colorMap);
            }
        }
        f2.setSize(f2size);

        text = new Text(paragraphs);
        text.setLocation(70f, 150f);
        text.setWidth(500f);
        text.drawOn(page);

        paragraphNumber = 1;
        for (Paragraph p : paragraphs) {
            if (p.startsWith("**")) {
                paragraphNumber = 1;
            } else {
                new TextLine(f2, String.valueOf(paragraphNumber) + ".")
                        .setLocation(p.xText - 15f, p.yText)
                        .drawOn(page);
                new Line(p.x1 - 3f, p.y1, p.x1 - 3f, p.y2)
                        .setColor(Color.navy)
                        .setWidth(1f).drawOn(page);
                paragraphNumber++;
            }
        }

        pdf.complete();
    }

    public static void main(String[] args) throws Exception {
        long time0 = System.currentTimeMillis();
        new Example_41();
        long time1 = System.currentTimeMillis();
        TextUtils.printDuration("Example_41", time0, time1);
    }
}   // End of Example_41.java
