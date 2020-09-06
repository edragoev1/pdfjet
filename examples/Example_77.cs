using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

using PDFjet.NET;


/**
 *  Example_77.cs
 *
 */
public class Example_77 {

    public Example_77() {

        PDF pdf = new PDF(new BufferedStream(
                new FileStream("Example_77.pdf", FileMode.Create)), Compliance.PDF_A_1B);

        Page page = new Page(pdf, Letter.PORTRAIT);

        Font f1 = new Font(pdf, CoreFont.HELVETICA_BOLD);
        f1.SetSize(12f);

        Font f2 = new Font(pdf, CoreFont.HELVETICA);
        f2.SetSize(12f);

        Image image1 = new Image(
                pdf,
                new BufferedStream(new FileStream(
                        "cat1.jpg", FileMode.Open, FileAccess.Read)),
                ImageType.JPG);
        image1.ScaleBy(0.1f);

        List<List<Cell>> tableData = new List<List<Cell>>();

        List<Cell> row = new List<Cell>();
        row.Add(new Cell(f1));
        row.Add(new Cell(f2));
        tableData.Add(row);

        row = new List<Cell>();
        row.Add(new Cell(f1, "Hello, World!"));
        row.Add(new Cell(f2, "This is a test."));
        tableData.Add(row);

        tableData[0][0].SetImage(image1);

        Table table = new Table();
        table.SetData(tableData);
        table.SetLocation(50f, 50f);
        table.SetColumnWidth(0, 270f);
        table.SetColumnWidth(1, 270f);

        StringBuilder buf = new StringBuilder();
        buf.Append("Name: 20200306_050741\n");
        buf.Append("Recorded: 2018:09:28 18:28:43\n");

        TextBox textBox = new TextBox(f1, buf.ToString());
        textBox.SetWidth(400f);
        textBox.SetNoBorders();

        // tableData[0][1].SetTextBox(textBox);
        tableData[0][1].SetDrawable(textBox);

        table.DrawOn(page);

        pdf.Complete();
    }

    public static void Main(String[] args) {
        Stopwatch sw = Stopwatch.StartNew();
        long time0 = sw.ElapsedMilliseconds;
        new Example_77();
        long time1 = sw.ElapsedMilliseconds;
        sw.Stop();
        Console.WriteLine("Example_77 => " + (time1 - time0));
    }

}   // End of Example_77.cs
