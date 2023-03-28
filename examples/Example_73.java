package examples;

import java.io.*;
import java.util.*;
import com.pdfjet.*;

/**
 * Example_73.java
 */
public class Example_73 {
    public Example_73() throws Exception {
        PDF pdf = new PDF(new FileOutputStream("Example_73.pdf"));
        Font f1 = new Font(pdf, CoreFont.HELVETICA);
        f1.setSize(14f);

        Page page = new Page(pdf, Letter.PORTRAIT);

        TextBox textBox = new TextBox(f1, readContentOfFile("data/latin.txt"));
        textBox.setLocation(50f, 50f);
        textBox.setWidth(430f);
        textBox.setBgColor(Color.whitesmoke);
        float[] xy = textBox.drawOn(page);  // drawOn method returns the x and y of the bottom right corner of the TextBox

        Box box = new Box();
        box.setLocation(xy[0], xy[1]);
        box.setSize(20f, 20f);
        box.drawOn(page);

        pdf.complete();
    }

    public String readContentOfFile(String filePath) throws IOException {
        StringBuilder buffer = new StringBuilder();
        FileInputStream stream = new FileInputStream(filePath);
        int ch;
        while ((ch = stream.read()) != -1) {
            buffer.append((char) ch);
        }
        stream.close();
        return buffer.toString();
    }

    public static void main(String[] args) throws Exception {
        new Example_73();
    }
}
