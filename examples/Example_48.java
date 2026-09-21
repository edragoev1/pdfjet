/*
 * Example_48.java
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
package examples;

import java.io.*;
import com.pdfjet.*;
import com.pdfjet.fonts.*;

/**
 * Example_48.java
 * This example draws the outline of a short guide to the structure of a PDF
 * file, and adds a bookmark for each of its titles. The numbers of the titles
 * come from their place in the tree of bookmarks.
 */
public class Example_48 {
    public Example_48() throws Exception {
        PDF pdf = new PDF(
                new BufferedOutputStream(new FileOutputStream("Example_48.pdf")));
        pdf.setCompliance(Compliance.PDF_UA_1);
        pdf.setTitle("The structure of a PDF file");

        Font f1 = new Font(pdf, IBMPlexSans.Regular);
        f1.setSize(14f);

        Font f2 = new Font(pdf, IBMPlexSans.SemiBold);
        f2.setSize(20f);

        Page page = new Page(pdf, Letter.PORTRAIT);

        Bookmark toc = new Bookmark(pdf);
        Title title = null;

        float x = 70f;
        float y = 80f;
        float offset = 50f;

        // A bookmark without a number.
        title = new Title(f2, "The structure of a PDF file", x, y);
        title.getTextLine().setStructureType(StructElem.H1);
        toc.addBookmark(page, title);
        title.drawOn(page);

        y += 50f;
        title = new Title(f1, "File header", x, y).setOffset(offset);
        toc.addBookmark(page, title).autoNumber(title.getPrefix());
        title.drawOn(page);

        y += 30f;
        title = new Title(f1, "File body", x, y).setOffset(offset);
        Bookmark body = toc.addBookmark(page, title).autoNumber(title.getPrefix());
        title.drawOn(page);

        // Bookmarks nested in "File body".
        y += 30f;
        title = new Title(f1, "Objects", x, y).setOffset(offset);
        body.addBookmark(page, title).autoNumber(title.getPrefix());
        title.drawOn(page);

        y += 30f;
        title = new Title(f1, "Streams", x, y).setOffset(offset);
        body.addBookmark(page, title).autoNumber(title.getPrefix());
        title.drawOn(page);

        y += 30f;
        title = new Title(f1, "Cross-reference table", x, y).setOffset(offset);
        toc.addBookmark(page, title).autoNumber(title.getPrefix());
        title.drawOn(page);

        y += 30f;
        title = new Title(f1, "File trailer", x, y).setOffset(offset);
        toc.addBookmark(page, title).autoNumber(title.getPrefix());
        title.drawOn(page);

        page = new Page(pdf, Letter.PORTRAIT);

        y = 80f;
        title = new Title(f1, "Incremental updates", x, y).setOffset(offset);
        Bookmark bm = toc.addBookmark(page, title).autoNumber(title.getPrefix());
        title.drawOn(page);

        y += 30f;
        title = new Title(f1, "New and changed objects", x, y).setOffset(offset);
        bm = bm.addBookmark(page, title).autoNumber(title.getPrefix());
        title.drawOn(page);

        // Two levels down.
        y += 30f;
        title = new Title(f1, "Changed objects keep their numbers", x, y).setOffset(offset);
        bm.addBookmark(page, title).autoNumber(title.getPrefix());
        title.drawOn(page);

        y += 30f;
        title = new Title(f1, "Deleted objects are marked as free", x, y).setOffset(offset);
        bm.addBookmark(page, title).autoNumber(title.getPrefix());
        title.drawOn(page);

        // Back up one level.
        y += 30f;
        bm = bm.getParent();
        title = new Title(f1, "A new cross-reference section", x, y).setOffset(offset);
        bm.addBookmark(page, title).autoNumber(title.getPrefix());
        title.drawOn(page);

        y += 30f;
        title = new Title(f1, "A new trailer", x, y).setOffset(offset);
        bm.addBookmark(page, title).autoNumber(title.getPrefix());
        title.drawOn(page);

        y += 30f;
        title = new Title(f1, "Linearized files", x, y).setOffset(offset);
        toc.addBookmark(page, title).autoNumber(title.getPrefix());
        title.drawOn(page);

        y += 50f;
        title = new Title(f2, "Summary", x, y);
        toc.addBookmark(page, title);
        title.drawOn(page);

        pdf.complete();
    }

    public static void main(String[] args) throws Exception {
        long time0 = System.currentTimeMillis();
        new Example_48();
        long time1 = System.currentTimeMillis();
        System.out.printf("Example_48 => %4d ms%n", time1 - time0);
    }
}   // End of Example_48.java
