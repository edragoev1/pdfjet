using System;
using System.IO;
using System.Collections.Generic;
using System.Diagnostics;

using PDFjet.NET;


/**
 *  Example_45.cs
 *
 */
public class Example_45 {

    public Example_45() {

        PDF pdf = new PDF(new BufferedStream(
                new FileStream("Example_45.pdf", FileMode.Create)),
                Compliance.PDF_UA);

        Font f1 = new Font(pdf,
                new FileStream(
                        "fonts/Droid/DroidSerif-Regular.ttf.stream",
                        FileMode.Open,
                        FileAccess.Read),
                Font.STREAM);

        Font f2 = new Font(pdf,
                new FileStream(
                        // "fonts/Droid/DroidSerif-Regular.ttf.stream",
                        "fonts/Droid/DroidSerif-Italic.ttf.stream",
                        FileMode.Open,
                        FileAccess.Read),
                Font.STREAM);

        f1.SetSize(14f);
        f2.SetSize(14f);
        // f2.SetItalic(true);

        Page page = new Page(pdf, Letter.PORTRAIT);

        TextLine text = new TextLine(f1);
        text.SetLocation(70f, 70f);
        text.SetText("Hasta la vista!");
        text.SetLanguage("es-MX");
        text.SetStrikeout(true);
        text.SetUnderline(true);
        text.SetURIAction("http://pdfjet.com");
        text.DrawOn(page);

        text = new TextLine(f1);
        text.SetLocation(70f, 90f);
        text.SetText("416-335-7718");
        text.SetURIAction("http://pdfjet.com");
        text.DrawOn(page);

        text = new TextLine(f1);
        text.SetLocation(70f, 120f);
        text.SetText("2014-11-25");
        text.DrawOn(page);

        List<Paragraph> paragraphs = new List<Paragraph>();

        Paragraph paragraph = new Paragraph()
                .Add(new TextLine(f1,
"The centres also offer free one-on-one consultations with business advisors who can review your business plan and make recommendations to improve it. The small business centres offer practical resources, from step-by-step info on setting up your business to sample business plans to a range of business-related articles and books in our resource libraries."))
                .Add(new TextLine(f2,
"This text is blue color and is written using italic font.")
                        .SetColor(Color.blue));

        paragraphs.Add(paragraph);

        Text textArea = new Text(paragraphs);
        textArea.SetLocation(70f, 150f);
        textArea.SetWidth(500f);
        textArea.DrawOn(page);

        float[] xy = (new PlainText(f2, new String[] {
"The Fibonacci sequence is named after Fibonacci.",
"His 1202 book Liber Abaci introduced the sequence to Western European mathematics,",
"although the sequence had been described earlier in Indian mathematics.",
"By modern convention, the sequence begins either with F0 = 0 or with F1 = 1.",
"The Liber Abaci began the sequence with F1 = 1, without an initial 0.",
"",
"Fibonacci numbers are closely related to Lucas numbers in that they are a complementary pair",
"of Lucas sequences. They are intimately connected with the golden ratio;",
"for example, the closest rational approximations to the ratio are 2/1, 3/2, 5/3, 8/5, ... .",
"Applications include computer algorithms such as the Fibonacci search technique and the",
"Fibonacci heap data structure, and graphs called Fibonacci cubes used for interconnecting",
"parallel and distributed systems. They also appear in biological settings, such as branching",
"in trees, phyllotaxis (the arrangement of leaves on a stem), the fruit sprouts of a pineapple,",
"the flowering of an artichoke, an uncurling fern and the arrangement of a pine cone.",
                })
                .SetLocation(70f, 370f)
                .SetFontSize(10f)
                .DrawOn(page));

        Box box = new Box();
        box.SetLocation(xy[0], xy[1]);
        box.SetSize(20f, 20f);
        box.DrawOn(page);

        page = new Page(pdf, Letter.PORTRAIT);

        text = new TextLine(f1);
        text.SetLocation(70f, 120f);
        text.SetText("416-877-1395");
        text.DrawOn(page);

        Line line = new Line(70f, 150f, 300f, 150f);
        line.SetWidth(1f);
        line.SetColor(Color.oldgloryred);
        line.SetAltDescription("This is a red line.");
        line.SetActualText("This is a red line.");
        line.DrawOn(page);

        box = new Box();
        box.SetLineWidth(1f);
        box.SetLocation(70f, 200f);
        box.SetSize(100f, 100f);
        box.SetColor(Color.oldgloryblue);
        box.SetAltDescription("This is a blue box.");
        box.SetActualText("This is a blue box.");
        box.DrawOn(page);

        page.AddBMC("Span", "This is a test", "This is a test");
        page.DrawString(f1, "This is a test", 75f, 230f);
        page.AddEMC();

        Image image = new Image(
                pdf,
                new BufferedStream(new FileStream(
                        "images/fruit.jpg", FileMode.Open, FileAccess.Read)),
                ImageType.JPG);
        image.SetLocation(70f, 310f);
        image.ScaleBy(0.5f);
        image.SetAltDescription("This is an image of a strawberry.");
        image.SetActualText("This is an image of a strawberry.");
        image.DrawOn(page);

        float w = 530f;
        float h = 13f;

        List<Field> fields = new List<Field>();
        fields.Add(new Field(   0f, new String[] {"Company", "Smart Widget Designs"}));
        fields.Add(new Field(   0f, new String[] {"Street Number", "120"}));
        fields.Add(new Field(  w/8, new String[] {"Street Name", "Oak"}));
        fields.Add(new Field(5*w/8, new String[] {"Street Type", "Street"}));
        fields.Add(new Field(6*w/8, new String[] {"Direction", "West"}));
        fields.Add(new Field(7*w/8, new String[] {"Suite/Floor/Apt.", "8W"})
                .SetAltDescription("Suite/Floor/Apartment")
                .SetActualText("Suite/Floor/Apartment"));
        fields.Add(new Field(   0f, new String[] {"City/Town", "Toronto"}));
        fields.Add(new Field(  w/2, new String[] {"Province", "Ontario"}));
        fields.Add(new Field(7*w/8, new String[] {"Postal Code", "M5M 2N2"}));
        fields.Add(new Field(   0f, new String[] {"Telephone Number", "(416) 331-2245"}));
        fields.Add(new Field(  w/4, new String[] {"Fax (if applicable)", "(416) 124-9879"}));
        fields.Add(new Field(  w/2, new String[] {"Email","jsmith12345@gmail.ca"}));
        fields.Add(new Field(   0f, new String[] {"Other Information",
                "We don't work on weekends.", "Please send us an Email."}));

// TODO:
        new Form(fields)
                .SetLabelFont(f1)
                .SetLabelFontSize(7f)
                .SetValueFont(f2)
                .SetValueFontSize(9f)
                .SetLocation(70f, 490f)
                .SetRowLength(w)
                .SetRowHeight(h)
                .DrawOn(page);

        pdf.Complete();
    }


    public static void Main(String[] args) {
        Stopwatch sw = Stopwatch.StartNew();
        long time0 = sw.ElapsedMilliseconds;
        new Example_45();
        long time1 = sw.ElapsedMilliseconds;
        sw.Stop();
        Console.WriteLine("Example_45 => " + (time1 - time0));
    }

}   // End of Example_45.cs
