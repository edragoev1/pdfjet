/*
 * Example_48.cs
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
using System;
using System.IO;
using System.Diagnostics;
using PDFjet.NET;

/**
 * Example_48.cs
 * This example draws the outline of a short guide to the structure of a PDF
 * file, and adds a bookmark for each of its titles. The numbers of the titles
 * come from their place in the tree of bookmarks.
 */
public class Example_48 {
    public Example_48() {
        PDF pdf = new PDF(new BufferedStream(
            new FileStream("Example_48.pdf", FileMode.Create)));
        pdf.SetCompliance(Compliance.PDF_UA_1);
        pdf.SetTitle("The structure of a PDF file");

        Font f1 = new Font(pdf, IBMPlexSans.Regular);
        f1.SetSize(14f);

        Font f2 = new Font(pdf, IBMPlexSans.SemiBold);
        f2.SetSize(20f);

        Page page = new Page(pdf, Letter.PORTRAIT);

        Bookmark toc = new Bookmark(pdf);
        Title title = null;

        float x = 70f;
        float y = 80f;
        float offset = 50f;

        // A bookmark without a number.
        title = new Title(f2, "The structure of a PDF file", x, y);
        title.GetTextLine().SetStructureType(StructElem.H1);
        toc.AddBookmark(page, title);
        title.DrawOn(page);

        y += 50f;
        title = new Title(f1, "File header", x, y).SetOffset(offset);
        toc.AddBookmark(page, title).AutoNumber(title.GetPrefix());
        title.DrawOn(page);

        y += 30f;
        title = new Title(f1, "File body", x, y).SetOffset(offset);
        Bookmark body = toc.AddBookmark(page, title).AutoNumber(title.GetPrefix());
        title.DrawOn(page);

        // Bookmarks nested in "File body".
        y += 30f;
        title = new Title(f1, "Objects", x, y).SetOffset(offset);
        body.AddBookmark(page, title).AutoNumber(title.GetPrefix());
        title.DrawOn(page);

        y += 30f;
        title = new Title(f1, "Streams", x, y).SetOffset(offset);
        body.AddBookmark(page, title).AutoNumber(title.GetPrefix());
        title.DrawOn(page);

        y += 30f;
        title = new Title(f1, "Cross-reference table", x, y).SetOffset(offset);
        toc.AddBookmark(page, title).AutoNumber(title.GetPrefix());
        title.DrawOn(page);

        y += 30f;
        title = new Title(f1, "File trailer", x, y).SetOffset(offset);
        toc.AddBookmark(page, title).AutoNumber(title.GetPrefix());
        title.DrawOn(page);

        page = new Page(pdf, Letter.PORTRAIT);

        y = 80f;
        title = new Title(f1, "Incremental updates", x, y).SetOffset(offset);
        Bookmark bm = toc.AddBookmark(page, title).AutoNumber(title.GetPrefix());
        title.DrawOn(page);

        y += 30f;
        title = new Title(f1, "New and changed objects", x, y).SetOffset(offset);
        bm = bm.AddBookmark(page, title).AutoNumber(title.GetPrefix());
        title.DrawOn(page);

        // Two levels down.
        y += 30f;
        title = new Title(f1, "Changed objects keep their numbers", x, y).SetOffset(offset);
        bm.AddBookmark(page, title).AutoNumber(title.GetPrefix());
        title.DrawOn(page);

        y += 30f;
        title = new Title(f1, "Deleted objects are marked as free", x, y).SetOffset(offset);
        bm.AddBookmark(page, title).AutoNumber(title.GetPrefix());
        title.DrawOn(page);

        // Back up one level.
        y += 30f;
        bm = bm.GetParent();
        title = new Title(f1, "A new cross-reference section", x, y).SetOffset(offset);
        bm.AddBookmark(page, title).AutoNumber(title.GetPrefix());
        title.DrawOn(page);

        y += 30f;
        title = new Title(f1, "A new trailer", x, y).SetOffset(offset);
        bm.AddBookmark(page, title).AutoNumber(title.GetPrefix());
        title.DrawOn(page);

        y += 30f;
        title = new Title(f1, "Linearized files", x, y).SetOffset(offset);
        toc.AddBookmark(page, title).AutoNumber(title.GetPrefix());
        title.DrawOn(page);

        y += 50f;
        title = new Title(f2, "Summary", x, y);
        toc.AddBookmark(page, title);
        title.DrawOn(page);

        pdf.Complete();
    }

    public static void Main(String[] args) {
        Stopwatch sw = Stopwatch.StartNew();
        long time0 = sw.ElapsedMilliseconds;
        new Example_48();
        long time1 = sw.ElapsedMilliseconds;
        sw.Stop();
        Console.WriteLine($"Example_48 => {time1 - time0,4} ms");
    }
}   // End of Example_48.cs
