using System;
using System.IO;
using System.Diagnostics;
using System.Collections.Generic;
using PDFjet.NET;

public class Example_01 {
    // Constructor to generate the PDF document
    public Example_01() {
        // Create a file stream to save the PDF
        FileStream fs = new FileStream("Example_01.pdf", FileMode.Create);

        // Initialize PDF object with the file stream
        PDF pdf = new PDF(new BufferedStream(fs));
        pdf.SetCompliance(Compliance.PDF_UA_1);

        // Load font for the PDF (IBMPlexSans Regular)
        Font font = new Font(pdf, "fonts/IBMPlexSans/IBMPlexSans-Regular.otf.stream");
        // Font font = new Font(pdf, IBMPlexSans.Regular);
        font.SetSize(12f);

        // Create a new page with Portrait orientation
        Page page = new Page(pdf, Letter.PORTRAIT);

        Dictionary<string, int> map = new Dictionary<string, int>();
        map["Everyone"] = Color.darkred;
        map["Pay"] = Color.darkgreen;
        map["Freedom"] = Color.blue;

        // Add English text from a file
        TextBlock textBlock = new TextBlock(font,
                Content.OfTextFile("data/languages/english.txt"));
        textBlock.SetLocation(50f, 50f);
        textBlock.SetWidth(430f);
        textBlock.SetFontSize(12f);
        textBlock.SetTextPadding(10f);
        textBlock.SetBorderColor(Color.black);
        textBlock.SetKeywordHighlightColors(map);
        float[] xy = textBlock.DrawOn(page);  // Draw the text and get coordinates

        // Draw a blue rectangle around the English text block
        Rect rect = new Rect(xy[0], xy[1], 30f, 30f);
        rect.SetBorderColor(Color.blue);
        rect.DrawOn(page);

        // Add Greek text from a file
        textBlock = new TextBlock(font,
                Content.OfTextFile("data/languages/greek.txt"));
        textBlock.SetLocation(50f, xy[1] + 30f);
        textBlock.SetWidth(430f);
        textBlock.SetFontSize(12f);
        textBlock.SetBorderColor(Color.transparent);  // No border for Greek text
        xy = textBlock.DrawOn(page);  // Draw the Greek text and update coordinates

        // Add Bulgarian text from a file with a blue border and rounded corners
        textBlock = new TextBlock(font, Content.OfTextFile("data/languages/bulgarian.txt"));
        textBlock.SetLocation(50f, xy[1] + 30f);
        textBlock.SetWidth(473f);
        textBlock.SetTextPadding(10f);
        textBlock.SetBorderColor(Color.blue);
        textBlock.SetBorderCornerRadius(10f);
        textBlock.SetUnderline(true);
        textBlock.DrawOn(page);  // Draw the Bulgarian text

        // Complete the PDF document creation
        pdf.Complete();
    }

    // Main method to run the example and measure execution time
    public static void Main(String[] args) {
        // Start a stopwatch to measure the execution time
        Stopwatch sw = Stopwatch.StartNew();
        long time0 = sw.ElapsedMilliseconds;

        // Create an instance of Example_01 to generate the PDF
        new Example_01();

        // Stop the stopwatch and get the elapsed time
        long time1 = sw.ElapsedMilliseconds;
        sw.Stop();

        // Print the execution duration
        TextUtils.PrintDuration("Example_01", time0, time1);
    }
}
