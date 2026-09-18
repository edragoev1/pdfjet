/*
 * Example_29.cs
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
using System;
using System.IO;
using System.Collections.Generic;
using System.Diagnostics;
using PDFjet.NET;

/**
 * Example_29.cs
 * This example draws a table whose cells hold text columns: each paragraph of
 * English and Greek text wraps inside its cell, and the cell grows to fit it.
 */
public class Example_29 {
    public Example_29() {
        PDF pdf = new PDF(new BufferedStream(
                new FileStream("Example_29.pdf", FileMode.Create)));
        pdf.SetCompliance(Compliance.PDF_UA_1);
        pdf.SetTitle("Text Columns in Table Cells");

        Font f1 = new Font(pdf, IBMPlexSans.Regular);
        f1.SetSize(10f);

        Font f2 = new Font(pdf, IBMPlexSans.SemiBold);
        f2.SetSize(10f);

        Page page = new Page(pdf, Letter.PORTRAIT);

        TextLine text = new TextLine(f2, "Text Columns in Table Cells");
        text.SetFontSize(22f);
        text.SetLocation(50f, 70f);
        text.DrawOn(page);

        String[] languages = {"English", "Greek"};
        String[] files = {"data/languages/english.txt", "data/languages/greek.txt"};

        List<List<Cell>> tableData = new List<List<Cell>>();

        List<Cell> row = new List<Cell>();
        row.Add(new Cell(f2, "Language"));
        row.Add(new Cell(f2, "Text"));
        tableData.Add(row);

        for (int i = 0; i < languages.Length; i++) {
            // Each line of the file after the first two is a paragraph.
            List<String> lines = Content.LinesOfTextFile(files[i]);
            TextColumn column = new TextColumn();
            column.SetWidth(400f);
            for (int j = 2; j < lines.Count; j++) {
                Paragraph paragraph = new Paragraph();
                paragraph.Add(new TextLine(f1, lines[j]));
                column.AddParagraph(paragraph);
            }

            row = new List<Cell>();
            row.Add(new Cell(f1, languages[i]));
            row.Add(new Cell(f1, ""));
            row[1].SetTextColumn(column);
            tableData.Add(row);
        }

        Table table = new Table();
        table.SetTableData(tableData, 1);
        table.SetColumnWidth(0, 90f);
        table.SetColumnWidth(1, 420f);
        table.SetLocation(50f, 100f);
        table.DrawOn(page);

        pdf.Complete();
    }

    public static void Main(String[] args) {
        Stopwatch sw = Stopwatch.StartNew();
        long time0 = sw.ElapsedMilliseconds;
        new Example_29();
        long time1 = sw.ElapsedMilliseconds;
        sw.Stop();
        Console.WriteLine($"Example_29 => {time1 - time0,4} ms");
    }
}   // End of Example_29.cs
