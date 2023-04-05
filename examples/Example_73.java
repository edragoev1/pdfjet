package examples;

import java.io.*;
import com.pdfjet.*;

/**
 * Example_73.java
 */
public class Example_73 {
    public Example_73() throws Exception {
        PDF pdf = new PDF(new FileOutputStream("Example_73.pdf"));

        Font f1 = new Font(
                pdf,
                new FileInputStream("fonts/Droid/DroidSans.ttf.stream"),
                Font.STREAM);
        Font f2 = new Font(
                pdf,
                new FileInputStream("fonts/Droid/DroidSansFallback.ttf.stream"),
                Font.STREAM);

        f1.setSize(12f);
        f2.setSize(12f);

        Page page = new Page(pdf, Letter.PORTRAIT);

        TextLine line1 = new TextLine(f1, "Hello, Beautiful World");
        TextLine line2 = new TextLine(f1, "Hello,BeautifulWorld");

        TextBox textBox = new TextBox(f1, line1.getText());
        textBox.setMargin(0f);
        textBox.setLocation(50f, 50f);
        textBox.setWidth(line1.getWidth() + 2*textBox.getMargin());
        textBox.setBgColor(Color.lightgreen);
        // The drawOn method returns the x and y of the bottom right corner of the TextBox
        float[] xy = textBox.drawOn(page);

        Box box = new Box();
        box.setLocation(xy[0], xy[1]);
        box.setSize(20f, 20f);
        box.drawOn(page);

        textBox = new TextBox(f1, line1.getText() + "!");
        textBox.setWidth(line1.getWidth() + 2*textBox.getMargin());
        textBox.setLocation(50f, 100f);
        xy = textBox.drawOn(page);

        box = new Box();
        box.setLocation(xy[0], xy[1]);
        box.setSize(20f, 20f);
        box.drawOn(page);
        
        textBox = new TextBox(f1, line2.getText());
        textBox.setWidth(line2.getWidth() + 2*textBox.getMargin());
        textBox.setLocation(50f, 200f);
        xy = textBox.drawOn(page);

        box = new Box();
        box.setLocation(xy[0], xy[1]);
        box.setSize(20f, 20f);
        box.drawOn(page);

        textBox = new TextBox(f1, line2.getText() + "!");
        textBox.setWidth(line2.getWidth() + 2*textBox.getMargin());
        textBox.setLocation(50f, 300f);
        xy = textBox.drawOn(page);

        box = new Box();
        box.setLocation(xy[0], xy[1]);
        box.setSize(20f, 20f);
        box.drawOn(page);

        textBox = new TextBox(f1, line2.getText() + "! Left Align");
        textBox.setMargin(30f);
        textBox.setVerticalAlignment(Align.TOP);
        textBox.setBgColor(Color.lightgreen);
        textBox.setWidth(line2.getWidth() + 2*textBox.getMargin());
        textBox.setLocation(50f, 400f);
        xy = textBox.drawOn(page);

        box = new Box();
        box.setLocation(xy[0], xy[1]);
        box.setSize(20f, 20f);
        box.drawOn(page);

        textBox = new TextBox(f1, line2.getText() + "! Right Align");
        textBox.setMargin(10f);
        textBox.setTextAlignment(Align.RIGHT);
        textBox.setWidth(line2.getWidth() + 2*textBox.getMargin());
        textBox.setLocation(50f, 500f);
        xy = textBox.drawOn(page);

        box = new Box();
        box.setLocation(xy[0], xy[1]);
        box.setSize(20f, 20f);
        box.drawOn(page);

        textBox = new TextBox(f1, line2.getText() + "! Center");
        textBox.setMargin(10f);
        textBox.setTextAlignment(Align.CENTER);
        textBox.setWidth(line2.getWidth() + 2*textBox.getMargin());
        textBox.setLocation(50f, 600f);
        xy = textBox.drawOn(page);

        box = new Box();
        box.setLocation(xy[0], xy[1]);
        box.setSize(20f, 20f);
        box.drawOn(page);

        String text = "保健所によると、女性は１３日に旅行先のタイから札幌に戻り、１６日午後５～８時ごろ同店を訪れ、帰宅後に発熱などの症状 が出て、２３日に医療機関ではしかと診断された。はしかのウイルスは発症日の１日前から感染者の呼吸などから放出され、本人がいなくなっても、２時間>程度空気中に漂い、空気感染する。保健所は１６日午後５～１１時に同店を訪れた人に、発熱などの異常が出た場合、早期にマスクをして医療機関を受診す>るよう呼びかけている。（本郷由美子）";

        textBox = new TextBox(f1);
        textBox.setFallbackFont(f2);
        textBox.setText(text);
        // textBox.setMargin(10f);
        textBox.setBgColor(Color.lightblue);
        textBox.setVerticalAlignment(Align.TOP);
        // textBox.setHeight(210f);
        textBox.setHeight(151f);
        textBox.setWidth(300f);
        textBox.setLocation(250f, 50f);
        textBox.drawOn(page);

        textBox = new TextBox(f1);
        textBox.setFallbackFont(f2);
        textBox.setText(text);
        // textBox.setMargin(10f);
        textBox.setBgColor(Color.lightblue);
        textBox.setVerticalAlignment(Align.CENTER);
        // textBox.setHeight(210f);
        textBox.setHeight(151f);
        textBox.setWidth(300f);
        textBox.setLocation(250f, 300f);
        textBox.drawOn(page);

        textBox = new TextBox(f1);
        textBox.setFallbackFont(f2);
        textBox.setText(text);
        // textBox.setMargin(10f);
        textBox.setBgColor(Color.lightblue);
        textBox.setVerticalAlignment(Align.BOTTOM);
        // textBox.setHeight(210f);
        textBox.setHeight(151f);
        textBox.setWidth(300f);
        textBox.setLocation(250f, 550f);
        textBox.drawOn(page);

        pdf.complete();
    }

    public static void main(String[] args) throws Exception {
        new Example_73();
    }
}
