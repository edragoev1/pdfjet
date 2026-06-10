using System;
using System.IO;
using System.Diagnostics;
using System.Collections;
using System.Collections.Generic;
using PDFjet.NET;

/**
 * Example_29.cs
 */
public class Example_29 {
    public Example_29() {
        PDF pdf = new PDF(new BufferedStream(
                new FileStream("Example_29.pdf", FileMode.Create)));

        Font font = new Font(pdf, CoreFont.HELVETICA);

        Page page = new Page(pdf, Letter.LANDSCAPE);

        // A paragraph has no size. It is just a list of text lines.
        Paragraph paragraph1 = new Paragraph();
        paragraph1.Add(new TextLine(font, "Lorem ipsum dolor sit amet, consectetur adipiscing elit."));
        paragraph1.Add(new TextLine(font, "Nulla elementum interdum elit, quis vehicula urna interdum quis."));
        paragraph1.Add(new TextLine(font, "Phasellus gravida ligula quam, nec blandit nulla. Sed posuere, lorem eget feugiat placerat, ipsum nulla euismod nisi, in semper mi nibh sed elit."));
        paragraph1.Add(new TextLine(font, "Mauris libero est, sodales dignissim congue sed, pulvinar non ipsum."));
        paragraph1.Add(new TextLine(font, "Sed risus nisi, ultrices nec eleifend at, viverra sed neque."));

        Paragraph paragraph2 = new Paragraph();
        paragraph2.Add(new TextLine(font, "Integer vehicula massa non arcu viverra ullamcorper."));
        paragraph2.Add(new TextLine(font, "Ut id tellus id ante mattis commodo."));
        paragraph2.Add(new TextLine(font, "Donec dignissim aliquam tortor, eu pharetra ipsum ullamcorper in."));
        paragraph2.Add(new TextLine(font, "Vivamus ultrices imperdiet iaculis."));

        List<TextLine> lines = paragraph1.GetTextLines();
        float r = 0.1f, g = 0.2f, b = 0f;
        float fontSize = 18f;
        int i = 0;
        foreach (var line in lines) {
            line.SetTextColor(r, g, b);
            r = (r + 0.1f) - MathF.Truncate(r + 0.1f);
            g = (g + 0.3f) - MathF.Truncate(g + 0.3f);
            b = (b + 0.2f) - MathF.Truncate(b + 0.2f);
            if (i > 0) {
                line.SetFontSize(fontSize);
            }
            line.SetUnderline(i % 2 == 0);
            line.SetStrikeout(i % 2 == 0);
            if (i == 1) {
                line.SetTextEffect(Effect.SUBSCRIPT);
                line.SetFontSize(line.GetFontSize()*0.6f);
            } else if (i == 4) {
                line.SetTextEffect(Effect.SUPERSCRIPT);
                line.SetFontSize(line.GetFontSize()*0.6f);
            }
            i += 1;
        }

        TextColumn column = new TextColumn();
        column.SetLocation(50f, 50f);
        column.SetWidth(420f);
        column.SetHeight(400f);
        column.AddParagraph(paragraph1);
        column.AddParagraph(paragraph2);
        column.SetFixedHeight(400f);

        Table table1 = new Table(font,font);
        List<List<Cell>> tableData = new List<List<Cell>>();
        List<Cell> row = new List<Cell>();
        Cell cell = new Cell(font, "");
        cell.SetTextColumn(column);
        cell.SetStrokeColor(Color.red);
        cell.SetLineWidth(2f);
        row.Add(cell);
        tableData.Add(row);

        float[] point2 = column.DrawOn(null);

        TextBlock tb = new TextBlock(font,
                "Peter Blood, bachelor of medicine and several other things besides, smoked a pipe and tended the geraniums boxed on the sill of his window above Water Lane in the town of Bridgewater.");
        tb.SetFontSize(16f);
        Cell cell2 = new Cell(font, "");
        cell2.SetTextBlock(tb);
        cell2.SetWidth(200);
        row.Add(cell2);
        table1.SetData(tableData);
        table1.SetLocation(50f, 50f);
        float[] xy = table1.DrawOn(page);

        Box box = new Box();
        box.SetLocation(xy[0], xy[1] + 2f);
        box.SetSize(25f, 25f);
        box.SetLineWidth(2f);
        box.SetColor(Color.darkblue);
        box.DrawOn(page);

        pdf.Complete();
    }

    public static void Main(String[] args) {
        Stopwatch sw = Stopwatch.StartNew();
        long time0 = sw.ElapsedMilliseconds;
        new Example_29();
        long time1 = sw.ElapsedMilliseconds;
        sw.Stop();
        TextUtils.PrintDuration("Example_29", time0, time1);
    }
}   // End of Example_29.cs
