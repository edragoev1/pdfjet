using System;
using System.IO;
using System.Collections.Generic;
using System.Diagnostics;

using PDFjet.NET;


/**
 *  Example_42.java
 *
 */
public class Example_42 {

    public Example_42() {

        PDF pdf = new PDF(new BufferedStream(
                new FileStream("Example_42.pdf", FileMode.Create)));

        Font f1 = new Font(pdf, CoreFont.HELVETICA_BOLD);
        Font f2 = new Font(pdf, CoreFont.HELVETICA);

        Page page = new Page(pdf, Letter.PORTRAIT);

        float w = 500f;
        float h = 13f;

        List<Field> fields = new List<Field>();
        fields.Add(new Field(   0f, new String[] {"Company", "Smart Widgets Construction Inc."}));
        fields.Add(new Field(   0f, new String[] {"Street Number", "120"}));
        fields.Add(new Field(  w/8, new String[] {"Street Name", "Oak"}));
        fields.Add(new Field(5*w/8, new String[] {"Street Type", "Street"}));
        fields.Add(new Field(6*w/8, new String[] {"Direction", "West"}));
        fields.Add(new Field(7*w/8, new String[] {"Suite/Floor/Apt.", "8W"}));
        fields.Add(new Field(   0f, new String[] {"City/Town", "Toronto"}));
        fields.Add(new Field(  w/2, new String[] {"Province", "Ontario"}));
        fields.Add(new Field(7*w/8, new String[] {"Postal Code", "M5M 2N2"}));
        fields.Add(new Field(   0f, new String[] {"Telephone Number", "(416) 331-2245"}));
        fields.Add(new Field(  w/4, new String[] {"Fax (if applicable)", "(416) 124-9879"}));
        fields.Add(new Field(  w/2, new String[] {"Email","jsmith12345@gmail.ca"}));
        fields.Add(new Field(   0f, new String[] {"Other Information","", ""}));

/*
        float[] xy = (new Form(fields)
                .SetLabelFont(f1)
                .SetLabelFontSize(8f)
                .SetValueFont(f2)
                .SetValueFontSize(10f)
                .SetLocation(70f, 90f)
                .SetRowLength(w)
                .SetRowHeight(h)
                .DrawOn(page));
*/
        new Form(fields)
                .SetLabelFont(f1)
                .SetLabelFontSize(8f)
                .SetValueFont(f2)
                .SetValueFontSize(10f)
                .SetLocation(70f, 90f)
                .SetRowLength(w)
                .SetRowHeight(h)
                .DrawOn(page);
/*
Console.WriteLine(xy[0]);
Console.WriteLine(xy[1]);
        Box box = new Box();
        box.SetLocation(xy[0], xy[1]);
        box.SetSize(20f, 20f);
        box.DrawOn(page);
*/
        pdf.Complete();
    }


    public static void Main(String[] args) {
        Stopwatch sw = Stopwatch.StartNew();
        long time0 = sw.ElapsedMilliseconds;
        new Example_42();
        long time1 = sw.ElapsedMilliseconds;
        sw.Stop();
        TextUtils.PrintDuration("Example_42", time0, time1);
    }

}   // End of Example_42.cs
