using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

using PDFjet.NET;


/**
 *  Example_75.cs
 *
 */
class Example_75 {

    public Example_75() {

        PDF pdf = new PDF(new BufferedStream(
                new FileStream("Example_75.pdf", FileMode.Create)));

        Font f1 = new Font(pdf,
                new FileStream(
                        "fonts/OpenSans/OpenSans-Regular.ttf.stream",
                        FileMode.Open,
                        FileAccess.Read),
                Font.STREAM);

        Font f2 = new Font(pdf,
                new FileStream(
                        "fonts/OpenSans/OpenSans-Italic.ttf.stream",
                        FileMode.Open,
                        FileAccess.Read),
                Font.STREAM);
        f2.SetSize(9f);

        StringBuilder buf = new StringBuilder();
        StreamReader reader = new StreamReader("data/calculus.txt");
        String line = null;
        while ((line = reader.ReadLine()) != null) {
            buf.Append(line);
            buf.Append("\n");
        }
        reader.Close();

        float w = 530f;
        float h = 13f;

        Page page = new Page(pdf, Letter.PORTRAIT);

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
        fields.Add(new Field(  w/2, new String[] {"Email", "jsmith12345@gmail.ca"}));
        fields.Add(new Field(   0f, new String[] {"Other Information", buf.ToString()}, true));

// TODO:
        new Form(fields)
                .SetLabelFont(f1)
                .SetLabelFontSize(7f)
                .SetValueFont(f2)
                .SetValueFontSize(9f)
                .SetLocation(50f, 60f)
                .SetRowLength(w)
                .SetRowHeight(h)
                .DrawOn(page);

        pdf.Complete();
    }


    public static void Main(String[] args) {
        new Example_75();
    }

}   // End of Example_75.cs
