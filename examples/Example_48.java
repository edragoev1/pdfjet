package examples;

import java.io.*;
import com.pdfjet.*;

/**
 *  Example_48.java
 */
public class Example_48 {
    public Example_48() throws Exception {
        PDF pdf = new PDF(
                new BufferedOutputStream(
                        new FileOutputStream("Example_48.pdf")),
                        Compliance.PDF_UA);

        Font f1 = new Font(pdf, "fonts/SourceSansPro/SourceSansPro-Regular.otf.stream");
        f1.setSize(14f);

        Page page = new Page(pdf, Letter.PORTRAIT);

        Bookmark toc = new Bookmark(pdf);
        Title title = null;

        float x = 70f;
        float y = 50f;
        float offset = 50f;

        y += 30f;
        title = new Title(f1, "This is a test!", x, y);
        toc.addBookmark(page, title);
        title.drawOn(page);

        y += 30f;
        title = new Title(f1, "General", x, y).setOffset(offset);
        toc.addBookmark(page, title).autoNumber(title.prefix);
        title.drawOn(page);

        y += 30f;
        title = new Title(f1, "File Header", x, y).setOffset(offset);
        toc.addBookmark(page, title).autoNumber(title.prefix);
        title.drawOn(page);

        y += 30f;
        title = new Title(f1, "File Body", x, y).setOffset(offset);
        toc.addBookmark(page, title).autoNumber(title.prefix);
        title.drawOn(page);

        y += 30f;
        title = new Title(f1, "Cross-Reference Table", x, y).setOffset(offset);
        toc.addBookmark(page, title).autoNumber(title.prefix);
        title.drawOn(page);

        page = new Page(pdf, Letter.PORTRAIT);

        y = 50f;
        title = new Title(f1, "File Trailer", x, y).setOffset(offset);
        toc.addBookmark(page, title).autoNumber(title.prefix);
        title.drawOn(page);

        y += 30f;
        title = new Title(f1, "Incremental Updates", x, y).setOffset(offset);
        Bookmark bm = toc.addBookmark(page, title).autoNumber(title.prefix);
        title.drawOn(page);

        y += 30f;
        title = new Title(f1, "Hello", x, y).setOffset(offset);
        bm = bm.addBookmark(page, title).autoNumber(title.prefix);
        title.drawOn(page);

        y += 30f;
        title = new Title(f1, "World", x, y).setOffset(offset);
        bm = bm.addBookmark(page, title).autoNumber(title.prefix);
        title.drawOn(page);

        y += 30f;
        title = new Title(f1, "Yahoo!!", x, y).setOffset(offset);
        bm.addBookmark(page, title).autoNumber(title.prefix);
        title.drawOn(page);

        y += 30f;
        title = new Title(f1, "Test Test Test ...", x, y).setOffset(offset);
        bm.addBookmark(page, title).autoNumber(title.prefix);
        title.drawOn(page);

        y += 30f;
        bm = bm.getParent();
        title = new Title(f1, "Let's see ...", x, y).setOffset(offset);
        bm.addBookmark(page, title).autoNumber(title.prefix);
        title.drawOn(page);

        y += 30f;
        title = new Title(f1, "One more item.", x, y).setOffset(offset);
        toc.addBookmark(page, title).autoNumber(title.prefix);
        title.drawOn(page);

        y += 30f;
        title = new Title(f1, "The End :)", x, y);
        toc.addBookmark(page, title);
        title.drawOn(page);

        pdf.complete();
    }

    public static void main(String[] args) throws Exception {
        long t0 = System.currentTimeMillis();
        new Example_48();
        long t1 = System.currentTimeMillis();
        System.out.println("Example_48 => " + (t1 - t0));
    }
}   // End of Example_48.java
