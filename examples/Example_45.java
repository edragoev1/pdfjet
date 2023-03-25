package examples;

import java.io.*;
import java.util.*;

import com.pdfjet.*;


/**
 *  Example_45.java
 *
 */
public class Example_45 {

    public Example_45() throws Exception {

        PDF pdf = new PDF(
                new BufferedOutputStream(
                        new FileOutputStream("Example_45.pdf")),
                        Compliance.PDF_UA);
        pdf.setLanguage("en-US");

        FileInputStream stream = new FileInputStream("fonts/Droid/DroidSerif-Regular.ttf.stream");
        Font f1 = new Font(pdf, stream, Font.STREAM);
        stream.close();
        f1.setSize(14f);

        stream = new FileInputStream("fonts/Droid/DroidSerif-Italic.ttf.stream");
        Font f2 = new Font(pdf, stream, Font.STREAM);
        stream.close();
        f2.setSize(14f);
        // f2.setItalic(true);

        Page page = new Page(pdf, Letter.PORTRAIT);

        TextLine textLine = new TextLine(f1);
        textLine.setLocation(70f, 70f);
        textLine.setText("Hasta la vista!");
        textLine.setLanguage("es-MX");
        textLine.setStrikeout(true);
        textLine.setUnderline(true);
        textLine.setURIAction("http://pdfjet.com");
        textLine.drawOn(page);

        textLine = new TextLine(f1);
        textLine.setLocation(70f, 90f);
        textLine.setText("416-335-7718");
        textLine.setURIAction("http://pdfjet.com");
        textLine.drawOn(page);

        textLine = new TextLine(f1);
        textLine.setLocation(70f, 120f);
        textLine.setText("2014-11-25");
        textLine.drawOn(page);

        List<Paragraph> paragraphs = new ArrayList<Paragraph>();

        Paragraph paragraph = new Paragraph()
                .add(new TextLine(f1,
"The centres also offer free one-on-one consultations with business advisors who can review your business plan and make recommendations to improve it. The small business centres offer practical resources, from step-by-step info on setting up your business to sample business plans to a range of business-related articles and books in our resource libraries."))
                .add(new TextLine(f2,
"This text is blue color and is written using italic font.")
                        .setColor(Color.blue));

        paragraphs.add(paragraph);

        Text text = new Text(paragraphs);
        text.setLocation(70f, 150f);
        text.setWidth(500f);
        text.drawOn(page);

        String[] linesOfText = new String[] {
"The Fibonacci sequence is named after Fibonacci.",
"His 1202 book Liber Abaci introduced the sequence to Western European mathematics,",
"although the sequence had been described earlier in Indian mathematics.",
"By modern convention, the sequence begins either with F0 = 0 or with F1 = 1.",
"The Liber Abaci began the sequence with F1 = 1, without an initial 0.",
"",
"Fibonacci numbers are closely related to Lucas numbers in that they are a complementary pair",
"of Lucas sequences. They are intimately connected with the golden ratio;",
"for example, the closest rational approximations to the ratio are 2/1, 3/2, 5/3, 8/5, ... .",
"Applications include computer algorithms such as the Fibonacci search technique and the",
"Fibonacci heap data structure, and graphs called Fibonacci cubes used for interconnecting",
"parallel and distributed systems. They also appear in biological settings, such as branching",
"in trees, phyllotaxis (the arrangement of leaves on a stem), the fruit sprouts of a pineapple,",
"the flowering of an artichoke, an uncurling fern and the arrangement of a pine cone."};

        PlainText plainText = new PlainText(f2, linesOfText);
        plainText.setLocation(70f, 370f);
        plainText.setWidth(520f);
        plainText.setFontSize(11f);
        float[] xy = plainText.drawOn(page);

        Box box = new Box();
        box.setLocation(xy[0], xy[1]);
        box.setSize(20f, 20f);
        box.drawOn(page);

        page = new Page(pdf, Letter.PORTRAIT);

        float w = 530f;
        float h = 13f;

        List<Field> fields = new ArrayList<Field>();
        fields.add(new Field(   0f, new String[] {"Company", "Smart Widget Designs"}));
        fields.add(new Field(   0f, new String[] {"Street Number", "120"}));
        fields.add(new Field(  w/8, new String[] {"Street Name", "Oak"}));
        fields.add(new Field(5*w/8, new String[] {"Street Type", "Street"}));
        fields.add(new Field(6*w/8, new String[] {"Direction", "West"}));
        fields.add(new Field(7*w/8, new String[] {"Suite/Floor/Apt.", "8W"})
                .setAltDescription("Suite/Floor/Apartment")
                .setActualText("Suite/Floor/Apartment"));
        fields.add(new Field(   0f, new String[] {"City/Town", "Toronto"}));
        fields.add(new Field(  w/2, new String[] {"Province", "Ontario"}));
        fields.add(new Field(7*w/8, new String[] {"Postal Code", "M5M 2N2"}));
        fields.add(new Field(   0f, new String[] {"Telephone Number", "(416) 331-2245"}));
        fields.add(new Field(  w/4, new String[] {"Fax (if applicable)", "(416) 124-9879"}));
        fields.add(new Field(  w/2, new String[] {"Email","jsmith12345@gmail.ca"}));
        fields.add(new Field(   0f, new String[] {
                "Other Information","We don't work on weekends.", "Please send us an Email."}));

        new Form(fields)
                .setLabelFont(f1)
                .setLabelFontSize(7f)
                .setValueFont(f2)
                .setValueFontSize(9f)
                .setLocation(50f, 50f)
                .setRowLength(w)
                .setRowHeight(h)
                .drawOn(page);

        Map<String, Integer> colors = new HashMap<String, Integer>();
        colors.put("Field", Color.blue);
        colors.put("ArrayList", Color.red);
        colors.put("List", Color.red);
        colors.put("Smart", Color.green);
        colors.put("Widget", Color.green);
        colors.put("Designs", Color.green);
        page.drawString(f1, f2, "        List<Field> colors = new ArrayList<Field>(\"Smart Widget Designs\");", 50f, 280f, colors);

        Image image = new Image(
                pdf, new FileInputStream("images/FormCode.png"), ImageType.PNG);
        image.setLocation(50f, 300f);
        image.scaleBy(0.50f);
        image.drawOn(page);

        pdf.complete();
    }


    public static void main(String[] args) throws Exception {
        long t0 = System.currentTimeMillis();
        new Example_45();
        long t1 = System.currentTimeMillis();
        System.out.println("Example_45 => " + (t1 - t0));
    }

}   // End of Example_45.java
