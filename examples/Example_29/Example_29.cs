using System;
using System.IO;
using System.Collections.Generic;
using System.Diagnostics;
using PDFjet.NET;

/**
 * Example_29.cs
 */
public class Example_29 {
    public Example_29() {
        PDF pdf = new PDF(new BufferedStream(
                new FileStream("Example_29.pdf", FileMode.Create)));

        Font font = new Font(pdf, IBMPlexSans.Regular);
        font.SetSize(15f);

        Page page = new Page(pdf, Letter.PORTRAIT);

        Paragraph paragraph1 = new Paragraph();
        paragraph1.Add(new TextLine(font, Content.OfTextFile("data/languages/english.txt")));

        Paragraph paragraph2 = new Paragraph();
        paragraph2.Add(new TextLine(font, Content.OfTextFile("data/languages/greek.txt")));

        TextColumn column = new TextColumn();
        column.SetLocation(50f, 50f);
        column.SetWidth(400f);
        column.AddParagraph(paragraph1);
        column.AddParagraph(paragraph2);
        // column.DrawOn(page);

        List<List<Cell>> tableData = new List<List<Cell>>();
        List<Cell> row = new List<Cell>();
        row.Add(new Cell(font, "Hello"));
        row.Add(new Cell(font, "World"));
        row[1].SetTextColumn(column);
        tableData.Add(row);

        Table table = new Table(font, font);
        table.SetData(tableData);
        table.SetLocation(50f, 50f);
        table.DrawOn(page);

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
