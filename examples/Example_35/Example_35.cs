using System;
using System.IO;
using System.Collections.Generic;
using System.Diagnostics;
using PDFjet.NET;

/**
 * Example_35.cs
 */
public class Example_35 {
    public Example_35() {
        PDF pdf = new PDF(new BufferedStream(
                new FileStream("Example_35.pdf", FileMode.Create)));

        Page page = new Page(pdf, Letter.PORTRAIT);

        Font f1 = new Font(pdf, IBMPlexSans.Regular);
        f1.SetSize(14f);

        Font f2 = new Font(pdf, IBMPlexSans.Bold);
        f2.SetSize(14f);

        // Base container
        Container container = new Container(400f, 400f);
        container.SetLocation(100f, 100f);

        // Add a rectangle to container
        Rect rect = new Rect(0f, 0f, 400f, 400f);
        rect.SetFillColor(Color.gray);
        container.Add(rect);

        var stamp = new Stamp(pdf).WithSize(400f, 400f).WithFont(f1).WithFont(f2);

        // Draw path ...
        stamp.SetFillColor(Color.lightblue)
            .SetStrokeColor(Color.red)
            .SetStrokeWidth(4f)
            .MoveTo(0f, 0f)
            .LineTo(400f, 0f)
            .LineTo(400f, 400f)
            .LineTo(0f, 400f)
            .FillPath()
            .ClosePath();

        // Draw Rectangle
        stamp.SetStrokeColor(Color.blue)
            .SetStrokeWidth(1f)
            .DrawRect(10f, 10f, 380f, 380f);

        // Fill Rectangle
        stamp.SetFillColor(Color.green).FillRect(10f, 10f, 20f, 20f);

        // Draw some text
        var parameters = new TextParameters()
            .SetFont(f1)
            .SetFontSize(14f)
            .SetTextLocation(25f, 25f)
            .SetText("Hello, World!");
        stamp.DrawText(parameters);

        // Change some parameters and draw the text again
        parameters.SetFont(f2).SetTextLocation(25f, 50f);
        stamp.SetFillColor(Color.darkgreen).DrawText(parameters);

        stamp.Complete();   // The stamp is complete!

        stamp.SetLocation(50f, 50f).DrawOn(page);

        // Rotate the stamp counter clockwise and draw it again
        stamp.Rotate(15).DrawOn(page);

        // Rotate the stamp clockwise and draw it again
        stamp.Rotate(-15).DrawOn(page);

        // Add a text line to container
        TextLine title = new TextLine(f1, "Container");
        title.SetLocation(150f, 20f);
        container.Add(title);
/*
        // Nested container #1
        Container nested1 = new Container(200f, 200f);
        nested1.SetLocation(0f, 0f);
        nested1.SetRotationCounterClockwise(30);
        nested1.SetScaleFactor(0.8f);

        Rect innerRect = new Rect(0f, 0f, 200f, 200f);
        innerRect.SetFillColor(Color.blue);
        nested1.Add(innerRect);

        TextLine innerText = new TextLine(f1, "Nested 1");
        innerText.SetLocation(50f, 100f);
        nested1.Add(innerText);

        container.Add(nested1);

        // Nested container #2
        Container nested2 = new Container(100f, 100f);
        nested2.SetLocation(250f, 250f);
        nested2.SetRotationCounterClockwise(45);

        Rect smallRect = new Rect(0f, 0f, 100f, 100f);
        smallRect.SetFillColor(Color.red);
        nested2.Add(smallRect);

        TextLine smallText = new TextLine(f1, "Nested 2");
        smallText.SetLocation(10f, 50f);
        nested2.Add(smallText);

        container.Add(nested2);

        container.SetRotationClockwise(45);
*/

        // Draw the entire hierarchy on the page
        container.DrawOn(page);

/*
        var container5 = new Container(200f, 20f);
        var rect5 = new Rect(0f, 0f, 200f, 20f);
        container5.Add(rect5);

        var rect6 = new Rect(0f, 0f, 10f, 10f);
        rect6.SetFillColor(Color.blue);
        container5.Add(rect6);

        var rect7 = new Rect(190f, 10f, 10f, 10f);
        rect7.SetBorderColor(Color.red);
        rect7.SetBorderWidth(2f);
        container5.Add(rect7);

        container5.SetLocation(50f, 600f);
        container5.DrawOn(page);

        container5.SetRotation(-90);
        container5.DrawOn(page);
*/
        pdf.Complete();
    }

    public static void Main(String[] args) {
        Stopwatch sw = Stopwatch.StartNew();
        long time0 = sw.ElapsedMilliseconds;
        new Example_35();
        long time1 = sw.ElapsedMilliseconds;
        sw.Stop();
        TextUtils.PrintDuration("Example_35", time0, time1);
    }
}   // End of Example_35.cs
