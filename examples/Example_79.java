package examples;

import java.io.*;
import com.pdfjet.*;

/**
 *  Example_79.java
 */
public class Example_79 {
    public Example_79() throws Exception {
        PDF pdf = new PDF(new FileOutputStream("Example_79.pdf"));
        Font f1 = new Font(
                pdf,
                new FileInputStream("fonts/Droid/DroidSans.ttf.stream"),
                Font.STREAM);
        Font f2 = new Font(pdf, CoreFont.HELVETICA);

        f1.setSize(72f);
        f2.setSize(24f);

        Page page = new Page(pdf, Letter.PORTRAIT);

        StringBuilder buf = new StringBuilder();
        buf.append("Heya, World! This is a test to show the functionality of a TextBox.");

        float x1 = 90f;
        float y1 = 50f;

        Point p1 = new Point(x1, y1);
        p1.setRadius(5f);
        p1.setFillShape(true);
        p1.drawOn(page);

        TextLine textline = new TextLine(f2, "(x1, y1)");
        textline.setLocation(x1, y1 - 15f);
        textline.drawOn(page);

        TextBox textBox = new TextBox(f1, buf.toString());
        textBox.setLocation(x1, y1);
        textBox.setWidth(500f);
        textBox.setHeight(230f);
        textBox.setMargin(0f);
        textBox.setSpacing(0f);
        textBox.setBgColor(Color.lightgreen);
        float[] xy = textBox.drawOn(page);

        float x2 = x1 + textBox.getWidth();
        float y2 = y1 + textBox.getHeight();

        f2.setSize(18f);

        // Text on the left
        TextLine ascent_text = new TextLine(f2, "Ascent");
        ascent_text.setLocation(x1 - 85f, y1 + 40f); //(y1 + f1.getAscent()) / 2);
        ascent_text.drawOn(page);

        TextLine descent_text = new TextLine(f2, "Descent");
        descent_text.setLocation(x1 - 85f, y1 + f1.getAscent() + 15f);
        descent_text.drawOn(page);

        // Lines beside the text
        Line arrow_line1 = new Line(x1 - 10f, y1, x1 - 10f, y1 + f1.getAscent());
        arrow_line1.setColor(Color.blue);
        arrow_line1.setWidth(3f);
        arrow_line1.drawOn(page);

        Line arrow_line2 = new Line(x1 - 10f, y1 + f1.getAscent(),
                            x1 - 10f, y1 + f1.getAscent() + f1.getDescent());
        arrow_line2.setColor(Color.red);
        arrow_line2.setWidth(3f);
        arrow_line2.drawOn(page);


        // Lines for first line of text
        Line text_line1 = new Line(x1, y1 + f1.getAscent(), x2, y1 + f1.getAscent());
        text_line1.drawOn(page);

        Line descent_line1 = new Line(x1, y1 + (f1.getAscent() + f1.getDescent()),
                                x2, y1 + (f1.getAscent() + f1.getDescent()));
        descent_line1.drawOn(page);


        // Lines for second line of text
        float curr_y = y1 + f1.getBodyHeight();

        Line text_line2 = new Line(x1, curr_y + f1.getAscent(), x2, curr_y + f1.getAscent());
        text_line2.drawOn(page);

        Line descent_line2 = new Line(x1, curr_y + f1.getAscent() + f1.getDescent(),
                                x2, curr_y + f1.getAscent() + f1.getDescent());
        descent_line2.drawOn(page);


        Point p2 = new Point(x2, y2);
        p2.setRadius(5f);
        p2.setFillShape(true);
        p2.drawOn(page);

        f2.setSize(24f);
        TextLine textline2 = new TextLine(f2, "(x2, y2)");
        textline2.setLocation(x2 - 80f, y2 + 30f);
        textline2.drawOn(page);

        Box box = new Box();
        box.setLocation(xy[0], xy[1]);
        box.setSize(20f, 20f);
        box.drawOn(page);

        pdf.complete();
    }


    public void drawTextAndLines(
            String text, Page page, Font font, float x, float y) throws Exception {
        TextLine textline = new TextLine(font, text);
        textline.setLocation(x, y);
        textline.drawOn(page);

        Line ascenderLine = new Line(x, y - font.getAscent(), x + 100f, y - font.getAscent());
        ascenderLine.setWidth(2f);
        ascenderLine.drawOn(page);

        Line line = new Line(x, y, x + 100f, y);
        line.setWidth(2f);
        line.drawOn(page);

        Line descenderLine = new Line(x, y + font.getDescent(), x + 100f, y + font.getDescent());
        descenderLine.setWidth(2f);
        descenderLine.drawOn(page);
    }


    public static void main(String[] args) throws Exception {
        long t0 = System.currentTimeMillis();
        new Example_79();
        long t1 = System.currentTimeMillis();
        System.out.println("Example_79 => " + (t1 - t0));
    }

}   // End of Example_79.java
