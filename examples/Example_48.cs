using System;
using System.IO;
using System.Diagnostics;
using PDFjet.NET;

/**
 *  Example_48.cs
 *
 */
public class Example_48 {
    public Example_48() {
        PDF pdf = new PDF(new BufferedStream(
                new FileStream("Example_48.pdf", FileMode.Create)),
                Compliance.PDF_UA);

        Font f1 = new Font(pdf, new FileStream(
                "fonts/OpenSans/OpenSans-Regular.ttf.stream",
                FileMode.Open,
                FileAccess.Read), Font.STREAM);

        Page page = new Page(pdf, Letter.PORTRAIT);

        Bookmark toc = new Bookmark(pdf);
        Title title = null;

        float x = 70f;
        float y = 50f;
        float offset = 50f;

        y += 30f;
        title = new Title(f1, "This is a test!", x, y);
        toc.AddBookmark(page, title);
        title.DrawOn(page);

        y += 30f;
        title = new Title(f1, "General", x, y).SetOffset(offset);
        toc.AddBookmark(page, title).AutoNumber(title.prefix);
        title.DrawOn(page);

        y += 30f;
        title = new Title(f1, "File Header", x, y).SetOffset(offset);
        toc.AddBookmark(page, title).AutoNumber(title.prefix);
        title.DrawOn(page);

        y += 30f;
        title = new Title(f1, "File Body", x, y).SetOffset(offset);
        toc.AddBookmark(page, title).AutoNumber(title.prefix);
        title.DrawOn(page);

        y += 30f;
        title = new Title(f1, "Cross-Reference Table", x, y).SetOffset(offset);
        toc.AddBookmark(page, title).AutoNumber(title.prefix);
        title.DrawOn(page);

        page = new Page(pdf, Letter.PORTRAIT);

        y = 50f;
        title = new Title(f1, "File Trailer", x, y).SetOffset(offset);
        toc.AddBookmark(page, title).AutoNumber(title.prefix);
        title.DrawOn(page);

        y += 30f;
        title = new Title(f1, "Incremental Updates", x, y).SetOffset(offset);
        Bookmark bm = toc.AddBookmark(page, title).AutoNumber(title.prefix);
        title.DrawOn(page);

        y += 30f;
        title = new Title(f1, "Hello", x, y).SetOffset(offset);
        bm = bm.AddBookmark(page, title).AutoNumber(title.prefix);
        title.DrawOn(page);

        y += 30f;
        title = new Title(f1, "World", x, y).SetOffset(offset);
        bm = bm.AddBookmark(page, title).AutoNumber(title.prefix);
        title.DrawOn(page);

        y += 30f;
        title = new Title(f1, "Yahoo!!", x, y).SetOffset(offset);
        bm.AddBookmark(page, title).AutoNumber(title.prefix);
        title.DrawOn(page);

        y += 30f;
        title = new Title(f1, "Test Test Test ...", x, y).SetOffset(offset);
        bm.AddBookmark(page, title).AutoNumber(title.prefix);
        title.DrawOn(page);

        y += 30f;
        bm = bm.GetParent();
        title = new Title(f1, "Let's see ...", x, y).SetOffset(offset);
        bm.AddBookmark(page, title).AutoNumber(title.prefix);
        title.DrawOn(page);

        y += 30f;
        title = new Title(f1, "One more item.", x, y).SetOffset(offset);
        toc.AddBookmark(page, title).AutoNumber(title.prefix);
        title.DrawOn(page);

        y += 30f;
        title = new Title(f1, "The End :)", x, y);
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
        TextUtils.PrintDuration("Example_48", time0, time1);
    }
}   // End of Example_48.cs
