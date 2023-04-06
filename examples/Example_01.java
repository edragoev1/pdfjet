package examples;

import java.io.*;
import java.util.*;

import com.pdfjet.*;


/**
 *  Example_01.java
 *
 */
class Example_01 {

    public Example_01() throws Exception {

        PDF pdf = new PDF(
                new BufferedOutputStream(new FileOutputStream("Example_01.pdf")));

        Font font1 = new Font(pdf, "fonts/Droid/DroidSans.ttf.stream");
        Font font2 = new Font(pdf, "fonts/Droid/DroidSansFallback.ttf.stream");

        font1.setSize(12f);
        font2.setSize(12f);

        Page page = new Page(pdf, Letter.PORTRAIT);

        TextLine textLine = new TextLine(font1, "Happy New Year!");
        textLine.setLocation(70f, 70f);
        textLine.drawOn(page);

        textLine = new TextLine(font1, "С Новым Годом!");
        textLine.setLocation(70f, 100f);
        textLine.drawOn(page);

        textLine = new TextLine(font1, "Ευτυχισμένο το Νέο Έτος!");
        textLine.setLocation(70f, 130f);
        textLine.drawOn(page);

        textLine = new TextLine(font1, "新年快樂！");
        textLine.setFallbackFont(font2);    // Required for scripts other than Latin, Cyrillic and Greek
        textLine.setLocation(300f, 70f);
        textLine.drawOn(page);

        textLine = new TextLine(font1, "新年快乐！");
        textLine.setFallbackFont(font2);
        textLine.setLocation(300f, 100f);
        textLine.drawOn(page);

        textLine = new TextLine(font1, "明けましておめでとうございます！");
        textLine.setFallbackFont(font2);
        textLine.setLocation(300f, 130f);
        textLine.drawOn(page);

        textLine = new TextLine(font1, "새해 복 많이 받으세요!");
        textLine.setFallbackFont(font2);
        textLine.setLocation(300f, 160f);
        textLine.drawOn(page);

        page = new Page(pdf, Letter.PORTRAIT);

        List<Paragraph> paragraphs = new ArrayList<Paragraph>();
        Paragraph paragraph = null;

        BufferedReader reader = new BufferedReader(
                new InputStreamReader(new FileInputStream("data/LCG.txt"), "UTF-8"));
        String line = null;
        int i = 0;
        while ((line = reader.readLine()) != null) {
            if (line.equals("")) {
                continue;
            }
            paragraph = new Paragraph();
            textLine = new TextLine(font1, line);
            textLine.setFallbackFont(font2);
            paragraph.add(textLine);
            paragraphs.add(paragraph);
            if (i == 0) {
                textLine = new TextLine(font1,
                        "Hello, World! This is a test to check if this line will be wrapped around properly.");
                textLine.setColor(Color.blue);
                textLine.setUnderline(true);
                paragraph.add(textLine);

                textLine = new TextLine(font1, "This is a test!");
                textLine.setColor(Color.oldgloryred);
                textLine.setUnderline(true);
                paragraph.add(textLine);
            }
            i++;
        }
        reader.close();

        Text text = new Text(paragraphs);
        text.setLocation(50f, 50f);
        text.setWidth(500f);
        float[] xy = text.drawOn(page);

        List<float[]> points = text.getBeginParagraphPoints();
        int n = 0;
	    for (float[] point : points) {
		    textLine = new TextLine(font1, String.valueOf(n+1)+".");
		    textLine.setLocation(point[0]-20, point[1]);
		    textLine.drawOn(page);
            n++;
	    }

        Box box = new Box();
        box.setLocation(xy[0], xy[1]);
        box.setSize(20f, 20f);
        box.drawOn(page);

        page = new Page(pdf, Letter.PORTRAIT);

        paragraphs = new ArrayList<Paragraph>();
        reader = new BufferedReader(
                new InputStreamReader(new FileInputStream("data/CJK.txt"), "UTF-8"));
        while ((line = reader.readLine()) != null) {
            if (line.equals("")) {
                continue;
            }
            paragraph = new Paragraph();
            textLine = new TextLine(font1, line);
            textLine.setFallbackFont(font2);
            paragraph.add(textLine);
            paragraphs.add(paragraph);
        }
        reader.close();

        text = new Text(paragraphs);
        text.setLocation(50f, 50f);
        text.setWidth(500f);
        text.drawOn(page);

        pdf.complete();
    }


    public static void main(String[] args) throws Exception {
        long t0 = System.currentTimeMillis();
        new Example_01();
        long t1 = System.currentTimeMillis();
        System.out.println("Example_01 => " + (t1 - t0));
    }

}   // End of Example_01.java
