using System;
using System.IO;
using System.Collections.Generic;
using System.Diagnostics;

using PDFjet.NET;

/**
 * Example_42.java
 */
public class Example_42 {
    public Example_42() {
        PDF pdf = new PDF(new BufferedStream(
                new FileStream("Example_42.pdf", FileMode.Create)));

        Font f1 = new Font(pdf, CoreFont.HELVETICA_BOLD);
        Font f2 = new Font(pdf, CoreFont.HELVETICA);

        Page page = new Page(pdf, Letter.PORTRAIT);

        float w = 500f;

        List<Field> fields = new List<Field>();
        fields.Add(new Field(   0f, "Company", "Smart Widgets Construction Inc."));
        fields.Add(new Field(   0f, "Street Number", "120"));
        fields.Add(new Field(  w/8, "Street Name", "Oak"));
        fields.Add(new Field(4*w/8, "Street Type", "Street"));
        fields.Add(new Field(5*w/8, "Direction", "West"));
        fields.Add(new Field(6*w/8, "Suite/Floor/Apartment", "8W"));
        fields.Add(new Field(   0f, "City/Town", "Toronto"));
        fields.Add(new Field(4*w/8, "Province", "Ontario"));
        fields.Add(new Field(7*w/8, "Postal Code", "M5M 2N2"));
        fields.Add(new Field(   0f, "Telephone Number", "(416) 331-2245"));
        fields.Add(new Field(2*w/8, "Fax (if applicable)", "(416) 124-9879"));
        fields.Add(new Field(4*w/8, "Email","jsmith12345@gmail.ca"));
        fields.Add(new Field(   0f, "Other Information", "We don't work on weekends."));
        fields.Add(new Field(   0f, "", "Please send us an Email."));

        float[] xy = (new Form(fields)
                .SetLabelFont(f1)
                .SetLabelFontSize(8f)
                .SetValueFont(f2)
                .SetValueFontSize(10f)
                .SetLocation(50f, 50f)
                .SetFormWidth(w)
                .SetLineWidth(0.2f)
                .DrawOn(page));

	    Rect rect = new Rect(xy[0], xy[1], 10f, 10f);
	    rect.SetBorderWidth(0.2f);
	    rect.SetBorderColor(Color.blue);
	    rect.DrawOn(page);

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
