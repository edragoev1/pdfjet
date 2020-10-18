using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using System.Text;

using PDFjet.NET;


/**
 *  Example_70.cs
 *
 */
public class Example_70 {

    public Example_70() {

        PDF pdf = new PDF(new BufferedStream(
                new FileStream("Example_70.pdf", FileMode.Create)));

        Font f1 = new Font(pdf,
                new FileStream(
                        "fonts/Roboto/Roboto-Light.ttf.stream",
                        FileMode.Open,
                        FileAccess.Read), Font.STREAM);
        f1.SetSize(18f);

        Font f2 = new Font(pdf,
                new FileStream(
                        "fonts/Noto/NotoSansSymbols-Regular.ttf.stream",
                        FileMode.Open,
                        FileAccess.Read), Font.STREAM);
        f2.SetSize(18f);

        Page page = new Page(pdf, Letter.LANDSCAPE);

        TextLine text = new TextLine(f1, "BLA ☎ BLABLA ☎");
        text.SetFallbackFont(f2);
        text.SetLocation(70f, 70f);
        text.DrawOn(page);

        pdf.Complete();
    }


    public static void Main(String[] args) {
        new Example_70();
    }

}   // End of Example_70.cs
