package examples;

import java.io.*;
import com.pdfjet.*;

/**
 *  Example_19.java
 */
public class Example_19 {
    public Example_19() throws Exception {
        PDF pdf = new PDF(
                new BufferedOutputStream(
                        new FileOutputStream("Example_19.pdf")));

        Font f1 = new Font(pdf, "fonts/OpenSans/OpenSans-Regular.ttf.stream");
        Font f2 = new Font(pdf, "fonts/Droid/DroidSansFallback.ttf.stream");

        f1.setSize(10f);
        f2.setSize(10f);

        StringBuilder buf = new StringBuilder();
        BufferedReader reader =
                new BufferedReader(new FileReader("data/calculus-short.txt"));
        String line = null;
        while ((line = reader.readLine()) != null) {
            buf.append(line);
        }
        reader.close();

        Page page = new Page(pdf, Letter.PORTRAIT);

        // Columns x coordinates
        float x1 = 50f;
        float y1 = 50f;

        float x2 = 300f;

        // Width of the second column:
        float w2 = 300f;

        Image image1 = new Image(pdf, "images/fruit.jpg");
        Image image2 = new Image(pdf, "images/ee-map.png");

        // Draw the first image
        image1.setLocation(x1, y1);
        image1.scaleBy(0.75f);
        image1.drawOn(page);

        TextBlock textBlock = new TextBlock(f1);
        textBlock.setText(buf.toString());
        textBlock.setLocation(x2, y1);
        textBlock.setWidth(w2);
        textBlock.setDrawBorder(true);
        // textBlock.setTextAlignment(Align.RIGHT);
        // textBlock.setTextAlignment(Align.CENTER);
        float[] xy = textBlock.drawOn(page);

        // Draw the second image
        image2.setLocation(x1, xy[1] + 10f);
        image2.scaleBy(1f/3f);
        image2.drawOn(page);

        textBlock = new TextBlock(f1);
        textBlock.setText("Lorem ipsum dolor sit amet, consectetur adipiscing elit. Nulla elementum interdum elit, quis vehicula urna interdum quis. Phasellus gravida ligula quam, nec blandit nulla. Sed posuere, lorem eget feugiat placerat, ipsum nulla euismod nisi, in semper mi nibh sed elit. Mauris libero est, sodales dignissim congue sed, pulvinar non ipsum. Sed risus nisi, ultrices nec eleifend at, viverra sed neque. Integer vehicula massa non arcu viverra ullamcorper. Ut id tellus id ante mattis commodo. Donec dignissim aliquam tortor, eu pharetra ipsum ullamcorper in. Vivamus ultrices imperdiet iaculis.");
        textBlock.setLocation(x2, xy[1] + 10f);
        textBlock.setWidth(w2);
        textBlock.setDrawBorder(true);
        textBlock.drawOn(page);

        textBlock = new TextBlock(f1);
        textBlock.setFallbackFont(f2);
        textBlock.setText("保健所によると、女性は１３日に旅行先のタイから札幌に戻り、１６日午後５～８時ごろ同店を訪れ、帰宅後に発熱などの症状が出て、２３日に医療機関ではしかと診断された。はしかのウイルスは発症日の１日前から感染者の呼吸などから放出され、本人がいなくなっても、２時間程度空気中に漂い、空気感染する。保健所は１６日午後５～１１時に同店を訪れた人に、発熱などの異常が出た場合、早期にマスクをして医療機関を受診するよう呼びかけている。（本郷由美子）");
        textBlock.setLocation(x1, 550f);
        textBlock.setWidth(350f);
        textBlock.setDrawBorder(true);
        textBlock.drawOn(page);

        TextBox textBox = new TextBox(f1);
        textBox.setFallbackFont(f2);
        textBox.setText("保健所によると、女性は１３日に旅行先のタイから札幌に戻り、１６日午後５～８時ごろ同店を訪れ、帰宅後に発熱などの症状が出て、２３日に医療機関ではしかと診断された。はしかのウイルスは発症日の１日前から感染者の呼吸などから放出され、本人がいなくなっても、２時間程度空気中に漂い、空気感染する。保健所は１６日午後５～１１時に同店を訪れた人に、発熱などの異常が出た場合、早期にマスクをして医療機関を受診するよう呼びかけている。（本郷由美子）");
        textBox.setLocation(x1, 680f);
        textBox.setWidth(350f);
        textBox.setBorder(Border.ALL);
        textBox.drawOn(page);

        pdf.complete();
    }

    public static void main(String[] args) throws Exception {
        long t0 = System.currentTimeMillis();
        new Example_19();
        long t1 = System.currentTimeMillis();
        System.out.println("Example_19 => " + (t1 - t0));
    }
}   // End of Example_19.java
