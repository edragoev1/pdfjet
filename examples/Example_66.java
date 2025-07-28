package examples;

import com.pdfjet.*;
import java.io.*;

public class Example_66 {
    public static void main(String[] args) {
        try {
            // Create a PDF document
            PDF pdf = new PDF(new BufferedOutputStream(new FileOutputStream("Example_66.pdf")));

            // Load the Red Hat Text font
            // Font font = new Font(pdf, new FileInputStream("fonts/RedHatText/RedHatText-Regular.ttf"));
            Font font = new Font(pdf, new FileInputStream("fonts/Andika/Andika-Regular.ttf"));

            // Create a page for the PDF
            Page page = new Page(pdf, Letter.LANDSCAPE);

            // Set the font size (16pt) and draw the text on the page
            font.setSize(16f);
            TextLine text = new TextLine(font, "Hello, World! 0123456789");
            text.setPosition(30f, 30f);  // Set the position at the top of the page
            text.drawOn(page);

            // Close the PDF document
            pdf.complete();

            System.out.println("PDF created successfully!");
        } catch (Exception e) {
            e.printStackTrace();
        }
    }
}
