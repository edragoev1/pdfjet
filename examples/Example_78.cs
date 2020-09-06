using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Diagnostics;

using PDFjet.NET;


/**
 *  Example_78.cs
 */
public class Example_78 {

    public Example_78() {

        PDF pdf = new PDF(new BufferedStream(
                new FileStream("Example_78.pdf", FileMode.Create)));

        Font f1 = new Font(pdf, CoreFont.HELVETICA_BOLD);

        Font f2 = new Font(pdf, new FileStream(
                "fonts/OpenSans/OpenSans-Regular.ttf.stream",
                FileMode.Open,
                FileAccess.Read),
                Font.STREAM);

        String fileName = "linux-logo.png";
        EmbeddedFile file1 = new EmbeddedFile(
                pdf,
                fileName,
                new FileStream("images/" + fileName, FileMode.Open, FileAccess.Read),
                false);     // Don't compress images.

        fileName = "Example_02.cs";
        EmbeddedFile file2 = new EmbeddedFile(
                pdf,
                fileName,
                new FileStream(fileName, FileMode.Open, FileAccess.Read),
                true);      // Compress text files.

        Page page = new Page(pdf, Letter.PORTRAIT);

        f1.SetSize(10f);

        FileAttachment attachment = new FileAttachment(pdf, file1);
        attachment.SetLocation(0f, 0f);
        attachment.SetIconPushPin();
        attachment.SetTitle("Attached File: " + file1.GetFileName());
        attachment.SetDescription(
                "Right mouse click or double click on the icon to save the attached file.");
        float[] point = attachment.DrawOn(page);

        attachment = new FileAttachment(pdf, file2);
        attachment.SetLocation(0f, point[1]);
        attachment.SetIconPaperclip();
        attachment.SetTitle("Attached File: " + file2.GetFileName());
        attachment.SetDescription(
                "Right mouse click or double click on the icon to save the attached file.");
        point = attachment.DrawOn(page);


        CheckBox checkbox = new CheckBox(f1, "Hello");
        checkbox.SetLocation(0f, point[1]);
        checkbox.SetCheckmark(Color.blue);
        checkbox.Check(Mark.CHECK);
        point = checkbox.DrawOn(page);

        checkbox = new CheckBox(f1, "Hello");
        checkbox.SetLocation(0.0, point[1]);
        checkbox.SetCheckmark(Color.blue);
        checkbox.Check(Mark.X);
        point = checkbox.DrawOn(page);

        Box box = new Box();
        box.SetLocation(0f, point[1]);
        box.SetSize(20f, 20f);
        point = box.DrawOn(page);

        RadioButton radiobutton = new RadioButton(f1, "Yes");
        radiobutton.SetLocation(0f, point[1]);
        radiobutton.SetURIAction("http://pdfjet.com");
        radiobutton.Select(true);
        point = radiobutton.DrawOn(page);

        QRCode qr = new QRCode(
                "https://kazuhikoarase.github.io",
                ErrorCorrectLevel.L);   // Low
        qr.SetModuleLength(3f);
        qr.SetLocation(0f, point[1]);
        point = qr.DrawOn(page);

        Dictionary<String, Int32> colors = new Dictionary<String, Int32>();
        colors["brown"] = Color.brown;
        colors["fox"] = Color.maroon;
        colors["lazy"] = Color.darkolivegreen;
        colors["jumps"] = Color.darkviolet;
        colors["dog"] = Color.chocolate;
        colors["sight"] = Color.blue;

        StringBuilder buf = new StringBuilder();
        buf.Append("The quick brown fox jumps over the lazy dog. What a sight!\n\n");

        TextBox textbox = new TextBox(f1, buf.ToString());
        textbox.SetLocation(0f, point[1]);
        // textbox.SetWidth(f1.StringWidth(buf.ToString()));
        textbox.SetBgColor(Color.whitesmoke);
        textbox.SetTextColors(colors);
        point = textbox.DrawOn(page);


        buf = new StringBuilder();
        buf.Append("Calculus, originally called infinitesimal calculus or \"the calculus of infinitesimals\", ");
        buf.Append("is the mathematical study of continuous change, in the same way that geometry is the ");
        buf.Append("study of shape and algebra is the study of generalizations of arithmetic operations. ");
        buf.Append("It has two major branches, differential calculus and integral calculus; ");
        buf.Append("the former concerns instantaneous rates of change, and the slopes of curves, ");
        buf.Append("while integral calculus concerns accumulation of quantities, and areas under or between ");
        buf.Append("curves. These two branches are related to each other by the fundamental theorem of calculus, ");
        buf.Append("and they make use of the fundamental notions of convergence of infinite sequences and ");
        buf.Append("infinite series to a well-defined limit.");

        TextBlock textBlock = new TextBlock(f1);
        textBlock.SetText(buf.ToString());
        textBlock.SetLocation(0f, point[1]);
        // textBlock.SetWidth(50f);
        point = textBlock.DrawOn(page);

        BarCode code = new BarCode(BarCode.CODE128, "Hello, World!");
        code.SetLocation(0f, point[1]);
        code.SetModuleLength(0.75f);
        code.SetFont(f1);
        point = code.DrawOn(page);

        buf = new StringBuilder();
        buf.Append("Using another font ...\n\nThis is a test.");
        textbox = new TextBox(f2, buf.ToString());
        textbox.SetLocation(0f, point[1]);
        point = textbox.DrawOn(page);

        code = new BarCode(BarCode.CODE128, "G86513JVW0C");
        code.SetLocation(0f, point[1]);
        code.SetModuleLength(0.75f);
        code.SetDirection(BarCode.TOP_TO_BOTTOM);
        code.SetFont(f1);
        point = code.DrawOn(page);

        buf = new StringBuilder();
        StreamReader reader = new StreamReader(
                new FileStream("Example_12.cs", FileMode.Open));
        String line = null;
        while ((line = reader.ReadLine()) != null) {
            buf.Append(line);
            // Both CR and LF are required by the scanner!
            buf.Append((char) 13);
            buf.Append((char) 10);
        }

        BarCode2D code2D = new BarCode2D(buf.ToString());
        code2D.SetModuleWidth(0.5f);
        code2D.SetLocation(0f, point[1]);
        point = code2D.DrawOn(page);

        pdf.Complete();
    }


    public static void Main(String[] args) {
        Stopwatch sw = Stopwatch.StartNew();
        long time0 = sw.ElapsedMilliseconds;
        new Example_78();
        long time1 = sw.ElapsedMilliseconds;
        sw.Stop();
        Console.WriteLine("Example_78 => " + (time1 - time0));
    }

}   // End of Example_78.java
