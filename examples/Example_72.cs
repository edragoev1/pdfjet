using System;
using System.IO;

using PDFjet.NET;


/**
 *  Example_72.cs
 *
 */
public class Example_72 {

    public Example_72() {

        PDF pdf = new PDF(new BufferedStream(
                new FileStream("Example_72.pdf", FileMode.Create)),
                Compliance.PDF_UA);

        Font f1 = new Font(pdf, new FileStream(
                "fonts/Droid/DroidSans.ttf.stream",
                FileMode.Open,
                FileAccess.Read), Font.STREAM);

        Font f2 = new Font(pdf, new FileStream(
                "fonts/Droid/DroidSansFallback.ttf.stream",
                FileMode.Open,
                FileAccess.Read), Font.STREAM);

        pdf.SetTitle("Hello, World!");
        pdf.SetAuthor("你好，世界！");

        Page page = new Page(pdf, Letter.PORTRAIT);

        Bookmark toc = new Bookmark(pdf);

        float x = 70f;
        float y = 50f;
        float offset = 20f;

        y += 30f;
        Title title = new Title(f1, "This is a test!", x, y);
        toc.AddBookmark(page, title);
        title.DrawOn(page);

        y += 30f;
        title = new Title(f1, "你好，世界！", x, y);
        title.textLine.SetFallbackFont(f2);
        title.SetOffset(offset);
        toc.AddBookmark(page, title).AutoNumber(title.prefix);
        title.DrawOn(page);

        y += 30f;
        title = new Title(f1, "File Header", x, y);
        title.SetOffset(offset);
        toc.AddBookmark(page, title).AutoNumber(title.prefix);
        title.DrawOn(page);
/*
        y += 30f;
        pref = new TextLine(f1);
        pref.SetLocation(x, y);
        text = new TextLine(f1, "File Body");
        text.SetLocation(x + offset, y);
        toc.AddBookmark(page, text.GetDestinationY(), text.GetText()).AutoNumber(pref);
        pref.DrawOn(page);
        text.DrawOn(page);

        y += 30f;
        pref = new TextLine(f1);
        pref.SetLocation(x, y);
        text = new TextLine(f1, "Cross-Reference Table");
        text.SetLocation(x + offset, y);
        toc.AddBookmark(page, text.GetDestinationY(), text.GetText()).AutoNumber(pref);
        pref.DrawOn(page);
        text.DrawOn(page);

        page = new Page(pdf, Letter.PORTRAIT);

        y = 50f;
        pref = new TextLine(f1);
        pref.SetLocation(x, y);
        text = new TextLine(f1, "File Trailer");
        text.SetLocation(x + offset, y);
        toc.AddBookmark(page, text.GetDestinationY(), text.GetText()).AutoNumber(pref);
        pref.DrawOn(page);
        text.DrawOn(page);

        y += 30f;
        pref = new TextLine(f1);
        pref.SetLocation(x, y);
        text = new TextLine(f1, "Incremental Updates");
        text.SetLocation(x + offset, y);
        Bookmark bm = toc.AddBookmark(page, text.GetDestinationY(), text.GetText()).AutoNumber(pref);
        pref.DrawOn(page);
        text.DrawOn(page);

        y += 30f;
        pref = new TextLine(f1);
        pref.SetLocation(x, y);
        text = new TextLine(f1, "Hello");
        text.SetLocation(x + offset, y);
        bm = bm.AddBookmark(page, text.GetDestinationY(), text.GetText()).AutoNumber(pref);
        pref.DrawOn(page);
        text.DrawOn(page);

        y += 30f;
        pref = new TextLine(f1);
        pref.SetLocation(x, y);
        text = new TextLine(f1, "World");
        text.SetLocation(x + offset, y);
        bm = bm.AddBookmark(page, text.GetDestinationY(), text.GetText()).AutoNumber(pref);
        pref.DrawOn(page);
        text.DrawOn(page);

        y += 30f;
        pref = new TextLine(f1);
        pref.SetLocation(x, y);
        text = new TextLine(f1, "Yahoo!!");
        text.SetLocation(x + offset, y);
        bm.AddBookmark(page, text.GetDestinationY(), text.GetText()).AutoNumber(pref);
        pref.DrawOn(page);
        text.DrawOn(page);

        y += 30f;
        pref = new TextLine(f1);
        pref.SetLocation(x, y);
        text = new TextLine(f1, "Test Test Test ...");
        text.SetLocation(x + offset, y);
        bm.AddBookmark(page, text.GetDestinationY(), text.GetText()).AutoNumber(pref);
        pref.DrawOn(page);
        text.DrawOn(page);

        bm = bm.GetParent();
        y += 30f;
        pref = new TextLine(f1);
        pref.SetLocation(x, y);
        text = new TextLine(f1, "Let's see ...");
        text.SetLocation(x + offset, y);
        bm.AddBookmark(page, text.GetDestinationY(), text.GetText()).AutoNumber(pref);
        pref.DrawOn(page);
        text.DrawOn(page);

        y += 30f;
        pref = new TextLine(f1);
        pref.SetLocation(x, y);
        text = new TextLine(f1, "One more item.");
        text.SetLocation(x + offset, y);
        toc.AddBookmark(page, text.GetDestinationY(), text.GetText()).AutoNumber(pref);
        pref.DrawOn(page);
        text.DrawOn(page);

        y += 30f;
        pref = new TextLine(f1);
        pref.SetLocation(x, y);
        text = new TextLine(f1, "The End :)");
        text.SetLocation(x, y);
        toc.AddBookmark(page, text.GetDestinationY(), text.GetText());
        pref.DrawOn(page);
        text.DrawOn(page);
*/
        pdf.Complete();
    }


    public static void Main(String[] args) {
        new Example_72();
    }

}   // End of Example_72.cs
