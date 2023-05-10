using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Diagnostics;
using PDFjet.NET;

/**
 *  Example_50.cs
 */
public class Example_50 {
    public Example_50(String fileNumber, String fileName) {
        PDF pdf = new PDF(new BufferedStream(
                new FileStream("Example_" + fileNumber + ".pdf", FileMode.Create)));

        List<PDFobj> objects = pdf.Read(
                new BufferedStream(new FileStream(fileName, FileMode.Open, FileAccess.Read)));

        Image image = new Image(objects,
                new BufferedStream(new FileStream(
                        "images/qrcode.png", FileMode.Open, FileAccess.Read)),
                ImageType.PNG);
        image.SetLocation(495f, 65f);
        image.ScaleBy(0.40f);

        Font f1 = new Font(objects,
                new FileStream("fonts/Droid/DroidSans.ttf.stream",
                        FileMode.Open,
                        FileAccess.Read), Font.STREAM);
        f1.SetSize(12f);

        Font f2 = new Font(objects,
                new FileStream("fonts/Droid/DroidSans-Bold.ttf.stream",
                        FileMode.Open,
                        FileAccess.Read), Font.STREAM);
        f2.SetSize(12f);

        List<PDFobj> pages = pdf.GetPageObjects(objects);
        Page page = new Page(pdf, pages[0]);
        // page.InvertYAxis();

        page.AddResource(image, objects);
        page.AddResource(f1, objects);
        page.AddResource(f2, objects);
        Font f3 = page.AddResource(CoreFont.HELVETICA, objects).SetSize(12f);

        image.DrawOn(page);

        float x = 23f;
        float y = 185f;
        float dx = 15f;
        float dy = 24f;

        page.SetBrushColor(Color.blue);

        // First Name and Initial
        page.DrawString(f2, "Иван", x, y);

        // Last Name
        page.DrawString(f3, "Jones", x + 258f, y);

        // Social Insurance Number
        page.DrawString(f1, StripSpacesAndDashes("243-590-129"), x + 437f, y, dx);

        // Last Name at Birth
        page.DrawString(f1, "Culverton", x, y += dy);

        // Mailing Address
        page.DrawString(f1, "10 Elm Street", x, y += dy);

        // City
        page.DrawString(f1, "Toronto", x, y + dy);

        // Province or Territory
        page.DrawString(f1, "Ontario", x + 365f, y += dy);

        // Postal Code
        page.DrawString(f1, StripSpacesAndDashes("L7B 2E9"), x + 482f, y, dx);

        // Home Address
        page.DrawString(f1, "10 Oak Road", x, y += dy);

        // City
        y += dy;
        page.DrawString(f1, "Toronto", x, y);

        // Previous Province or Territory
        page.DrawString(f1, "Ontario", x + 365f, y);

        // Postal Code
        page.DrawString(f1, StripSpacesAndDashes("L7B 2E9"), x + 482f, y, dx);

        // Home telephone number
        page.DrawString(f1, "905-222-3333", x, y + dy);
        // Work telephone number
        page.DrawString(f1, "416-567-9903", x + 279f, y += dy);

        // Previous province or territory
        page.DrawString(f1, "British Columbia", x + 452f, y += dy);

        // Move date from previous province or territory
        y += dy;
        page.DrawString(f1, StripSpacesAndDashes("2016-04-12"), x + 452f, y, dx);

        // Date new marital status began
        page.DrawString(f1, StripSpacesAndDashes("2014-11-02"), x + 452f, 467f, dx);

        // First name of spouse
        y = 521f;
        page.DrawString(f1, "Melanie", x, y);
        // Last name of spouse
        page.DrawString(f1, "Jones", x + 258f, y);

        // Social Insurance number of spouse
        page.DrawString(f1, StripSpacesAndDashes("192-760-427"), x + 437f, y, dx);

        // Spouse or common-law partner's address
        page.DrawString(f1, "12 Smithfield Drive", x, 554f);

        // Signature Date
        page.DrawString(f1, "2016-08-07", x + 475f, 615f);

        // Signature Date of spouse
        page.DrawString(f1, "2016-08-07", x + 475f, 651f);

        // Female Checkbox 1
        // CheckBox.XMark(page, 477.5f, 197.5f, 7f);

        // Male Checkbox 1
        CheckBox.XMark(page, 534.5f, 197.5f, 7f);

        // Married
        CheckBox.XMark(page, 34.5f, 424f, 7f);

        // Living common-law
        // CheckBox.XMark(page, 121.5f, 424f, 7f);

        // Widowed
        // CheckBox.XMark(page, 235.5f, 424f, 7f);

        // Divorced
        // CheckBox.XMark(page, 325.5f, 424f, 7f);

        // Separated
        // CheckBox.XMark(page, 415.5f, 424f, 7f);

        // Single
        // CheckBox.XMark(page, 505.5f, 424f, 7f);

        // Female Checkbox 2
        CheckBox.XMark(page, 478.5f, 536.5f, 7f);

        // Male Checkbox 2
        // CheckBox.XMark(page, 535.5f, 536.5f, 7f);
        page.Complete(objects);
        pdf.AddObjects(objects);

        pdf.Complete();
    }

    private String StripSpacesAndDashes(String str) {
        StringBuilder buf = new StringBuilder();
        for (int i = 0; i < str.Length; i++) {
            char ch = str[i];
            if (ch != ' ' && ch != '-') {
                buf.Append(ch);
            }
        }
        return buf.ToString();
    }

    public static void Main(String[] args) {
        Stopwatch sw = Stopwatch.StartNew();
        long time0 = sw.ElapsedMilliseconds;
        new Example_50("50", "data/testPDFs/rc65-16e.pdf");
        // new Example_50("50", "data/testPDFs/NoPredictor.pdf");
        // new Example_50("50", "../../eBooks/UniversityPhysicsVolume1.pdf");
        // new Example_50("50", "../../eBooks/PDF32000_2008.pdf");
        long time1 = sw.ElapsedMilliseconds;
        TextUtils.PrintDuration("Example_50", time0, time1);
    }
}   // End of Example_50.cs
