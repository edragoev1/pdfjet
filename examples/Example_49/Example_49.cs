/*
 * Example_49.cs
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
using System;
using System.Collections.Generic;
using System.IO;
using System.Diagnostics;
using PDFjet.NET;

/**
 * Example_49.cs
 *
 * Merges and splits existing PDF documents. merge draws a cover page and
 * merges three documents after it, into Example_49.pdf: the pages of each
 * follow in their order and keep their content, resources, annotations and
 * links, and what belongs to a whole document, such as its bookmarks, form
 * fields and tagging, is left out. split writes one PDF for each page of a
 * document, Example_49_1.pdf to Example_49_5.pdf, and all of its pages in
 * reverse order to Example_49_reversed.pdf; a link to a page that is not in
 * the same PDF leads nowhere.
 */
public class Example_49 {
    public Example_49() {
        Merge();
        Split();
    }

    private void Merge() {
        PDF pdf = new PDF(new BufferedStream(
                new FileStream("Example_49.pdf", FileMode.Create)));

        String[] fileNames = {
            "resources/data/testPDFs/scanned-linearized.pdf",
            "resources/data/testPDFs/sample-document.pdf",
            "resources/data/testPDFs/PDFjetLogo.pdf"
        };
        List<List<PDFobj>> documents = new List<List<PDFobj>>();
        foreach (String fileName in fileNames) {
            using (BufferedStream stream = new BufferedStream(
                    new FileStream(fileName, FileMode.Open, FileAccess.Read))) {
                documents.Add(pdf.Read(stream));
            }
        }

        Font f1 = new Font(pdf, IBMPlexSans.Bold);
        f1.SetSize(24f);
        Font f2 = new Font(pdf, IBMPlexSans.Regular);
        f2.SetSize(12f);

        Page page = new Page(pdf, Letter.PORTRAIT);
        new TextLine(f1, "Merged documents").SetLocation(50f, 80f).DrawOn(page);
        float y = 130f;
        for (int i = 0; i < fileNames.Length; i++) {
            int pages = pdf.GetPageObjects(documents[i]).Count;
            String text = fileNames[i] + ", " + pages + (pages == 1 ? " page" : " pages");
            new TextLine(f2, text).SetLocation(50f, y).DrawOn(page);
            y += 20f;
        }

        foreach (List<PDFobj> objects in documents) {
            pdf.Merge(objects);
        }

        pdf.Complete();
    }

    private void Split() {
        List<PDFobj> objects;
        using (BufferedStream stream = new BufferedStream(
                new FileStream("resources/data/testPDFs/scanned-linearized.pdf", FileMode.Open, FileAccess.Read))) {
            objects = new PDF().Read(stream);
        }
        int count = new PDF().GetPageObjects(objects).Count;

        for (int i = 1; i <= count; i++) {
            PDF part = new PDF(new BufferedStream(
                    new FileStream("Example_49_" + i + ".pdf", FileMode.Create)));
            part.Merge(objects, i);
            part.Complete();
        }

        int[] reversed = new int[count];
        for (int i = 0; i < count; i++) {
            reversed[i] = count - i;
        }
        PDF pdf = new PDF(new BufferedStream(
                new FileStream("Example_49_reversed.pdf", FileMode.Create)));
        pdf.Merge(objects, reversed);
        pdf.Complete();
    }

    public static void Main(String[] args) {
        Stopwatch sw = Stopwatch.StartNew();
        long time0 = sw.ElapsedMilliseconds;
        new Example_49();
        long time1 = sw.ElapsedMilliseconds;
        Console.WriteLine($"Example_49 => {time1 - time0,4} ms");
    }
}   // End of Example_49.cs
