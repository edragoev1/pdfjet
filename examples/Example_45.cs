using System;
using System.IO;
using System.Collections.Generic;
using System.Diagnostics;
using PDFjet.NET;

/**
 *  Example_45.cs
 */
public class Example_45 {
    public Example_45() {
        PDF pdf = new PDF(new BufferedStream(
                new FileStream("Example_45.pdf", FileMode.Create)),
                Compliance.PDF_UA);

        Font f1 = new Font(pdf, "fonts/Droid/DroidSerif-Regular.ttf.stream");
        Font f2 = new Font(pdf, "fonts/Droid/DroidSerif-Italic.ttf.stream");
        Font f3 = new Font(pdf, "fonts/SourceCodePro/SourceCodePro-Regular.ttf.stream");

        f1.SetSize(14f);
        f2.SetSize(14f);
        f3.SetSize(10f);

        Page page = new Page(pdf, Letter.PORTRAIT);

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
        fields.Add(new Field(   0f, new String[] {
                "Other Information","We don't work on weekends.", "Please send us an Email."}));

        new Form(fields)
                .SetLabelFont(f1)
                .SetLabelFontSize(7f)
                .SetValueFont(f2)
                .SetValueFontSize(9f)
                .SetLocation(50f, 50f)
                .SetRowLength(w)
                .SetRowHeight(h)
                .DrawOn(page);

        Dictionary<String, int> colors = new Dictionary<String, int>();
        colors["new"] = Color.red;
        colors["ArrayList"] = Color.blue;
        colors["List"] = Color.blue;
        colors["String"] = Color.blue;
        colors["Field"] = Color.blue;
        colors["Form"] = Color.blue;
        colors["Smart"] = Color.green;
        colors["Widget"] = Color.green;
        colors["Designs"] = Color.green;

        float x = 50;
        float y = 280;
        float dy = f3.GetBodyHeight();
        List<String> lines = Text.ReadLines("data/form-code-csharp.txt");
        foreach (String line in lines) {
            page.DrawString(f3, line, x, y, Color.gray, colors);
            y += dy;
        }

        pdf.Complete();
    }

    public static void Main(String[] args) {
        Stopwatch sw = Stopwatch.StartNew();
        long time0 = sw.ElapsedMilliseconds;
        new Example_45();
        long time1 = sw.ElapsedMilliseconds;
        sw.Stop();
        TextUtils.PrintDuration("Example_45", time0, time1);
    }

}   // End of Example_45.cs
