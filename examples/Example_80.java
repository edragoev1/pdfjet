package examples;

import java.io.*;
import java.util.*;
import com.pdfjet.*;

/**
 *  Example_80.java
 */
public class Example_80 {
    public Example_80() throws Exception {
        PDF pdf = new PDF(
                new BufferedOutputStream(
                        new FileOutputStream("Example_80.pdf")));
        Font f1 = new Font(pdf, CoreFont.HELVETICA_BOLD);
        Font f2 = new Font(pdf,
                getClass().getResourceAsStream("../fonts/OpenSans/OpenSans-Regular.ttf.stream"),
                Font.STREAM);

        String fileName = "linux-logo.png";
        EmbeddedFile file1 = new EmbeddedFile(
                pdf,
                fileName,
                getClass().getResourceAsStream("../images/" + fileName),
                false);     // Don't compress images.

        fileName = "Example_02.java";
        EmbeddedFile file2 = new EmbeddedFile(
                pdf,
                fileName,
                getClass().getResourceAsStream(fileName),
                true);      // Compress text files.

        Page page = new Page(pdf, Letter.PORTRAIT);
        f1.setSize(10f);

        FileAttachment attachment = new FileAttachment(pdf, file1);
        attachment.setLocation(0f, 0f);
        attachment.setIconPushPin();
        // attachment.setIconSize(24f);
        attachment.setTitle("Attached File: " + file1.getFileName());
        attachment.setDescription(
                "Right mouse click or double click on the icon to save the attached file.");
        float[] xy = attachment.drawOn(page);

        attachment = new FileAttachment(pdf, file2);
        attachment.setLocation(0f, xy[1]);
        attachment.setIconPaperclip();
        // attachment.setIconSize(24f);
        attachment.setTitle("Attached File: " + file2.getFileName());
        attachment.setDescription(
                "Right mouse click or double click on the icon to save the attached file.");
        xy = attachment.drawOn(page);

        CheckBox checkBox = new CheckBox(f1, "Hello");
        checkBox.setLocation(0f, xy[1]);
        checkBox.setCheckmark(Color.blue);
        checkBox.check(Mark.CHECK);
        xy = checkBox.drawOn(page);

        checkBox = new CheckBox(f1, "Hello");
        checkBox.setLocation(0.0, xy[1]);
        checkBox.setCheckmark(Color.blue);
        checkBox.check(Mark.X);
        xy = checkBox.drawOn(page);

        Box box = new Box();
        box.setLocation(0f, xy[1]);
        box.setSize(20f, 20f);
        box.setURIAction("https://pdfjet.com/net");
        xy = box.drawOn(page);

        RadioButton radioButton = new RadioButton(f1, "Yes");
        radioButton.setLocation(0f, xy[1]);
        radioButton.setURIAction("http://pdfjet.com");
        radioButton.select(true);
        xy = radioButton.drawOn(page);

        QRCode qr = new QRCode(
                "https://kazuhikoarase.github.io",
                ErrorCorrectLevel.L);   // Low
        qr.setModuleLength(3f);
        qr.setLocation(0f, xy[1]);
        xy = qr.drawOn(page);

        Map<String, Integer> colors = new HashMap<String, Integer>();
        colors.put("brown", Color.brown);
        colors.put("fox", Color.maroon);
        colors.put("lazy", Color.darkolivegreen);
        colors.put("jumps", Color.darkviolet);
        colors.put("dog", Color.chocolate);
        colors.put("sight", Color.blue);

        StringBuilder buf = new StringBuilder();
        buf.append("The quick brown fox jumps over the lazy dog. What a sight!\n\n");

        TextBox textBox = new TextBox(f1, buf.toString());
        textBox.setLocation(0f, xy[1]);
        textBox.setBgColor(Color.whitesmoke);
        textBox.setTextColors(colors);
        xy = textBox.drawOn(page);

        buf = new StringBuilder();
        buf.append("Donec a urna ac ipsum fringilla ultricies non vel diam. ");
        buf.append("Morbi vitae lacus ac elit luctus dignissim. ");
        buf.append("Quisque rutrum egestas facilisis. Curabitur tempus, tortor ac fringilla fringilla, ");
        buf.append("libero elit gravida sem, vel aliquam leo nibh sed libero. ");
        buf.append("Proin pretium, augue quis eleifend hendrerit, leo libero auctor magna, ");
        buf.append("vitae porttitor lorem urna eget urna. ");
        buf.append("Lorem ipsum dolor sit amet, consectetur adipiscing elit.");

        BarCode code = new BarCode(BarCode.CODE128, "Hello, World!");
        code.setLocation(0f, xy[1]);
        code.setModuleLength(0.75f);
        code.setFont(f1);
        xy = code.drawOn(page);

        buf = new StringBuilder();
        buf.append("Using another font ...\n\nThis is a test.");
        textBox = new TextBox(f2, buf.toString());
        textBox.setLocation(0f, xy[1]);
        xy = textBox.drawOn(page);

        code = new BarCode(BarCode.CODE128, "G86513JVW0C");
        code.setLocation(0f, xy[1]);
        code.setModuleLength(0.75f);
        code.setDirection(BarCode.TOP_TO_BOTTOM);
        code.setFont(f1);
        xy = code.drawOn(page);

        buf = new StringBuilder();
        BufferedReader reader =
                new BufferedReader(new FileReader("examples/Example_12.java"));
        String line = null;
        while ((line = reader.readLine()) != null) {
            buf.append(line);
            // Both CR and LF are required by the scanner!
            buf.append((char) 13);
            buf.append((char) 10);
        }
        reader.close();

        BarCode2D code2D = new BarCode2D(buf.toString());
        code2D.setModuleWidth(0.5f);
        code2D.setLocation(0f, xy[1]);
        code2D.drawOn(page);

        pdf.complete();
    }

    public static void main(String[] args) throws Exception {
        long time0 = System.currentTimeMillis();
        new Example_80();
        long time1 = System.currentTimeMillis();
        TextUtils.printDuration("Example_80", time0, time1);
    }
}   // End of Example_80.java
