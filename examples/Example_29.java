/*
 * Example_29.java
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
package examples;

import java.io.*;
import java.util.*;
import com.pdfjet.*;
import com.pdfjet.fonts.*;

/**
 * Example_29.java
 * This example draws a table whose cells hold text columns: each paragraph of
 * English and Greek text wraps inside its cell, and the cell grows to fit it.
 */
public class Example_29 {
    public Example_29() throws Exception {
        PDF pdf = new PDF(
                new BufferedOutputStream(new FileOutputStream("Example_29.pdf")));

        Font f1 = new Font(pdf, IBMPlexSans.Regular);
        f1.setSize(10f);

        Font f2 = new Font(pdf, IBMPlexSans.SemiBold);
        f2.setSize(10f);

        Page page = new Page(pdf, Letter.PORTRAIT);

        TextLine text = new TextLine(f2, "Text Columns in Table Cells");
        text.setFontSize(22f);
        text.setLocation(50f, 70f);
        text.drawOn(page);

        String[] languages = {"English", "Greek"};
        String[] files = {"data/languages/english.txt", "data/languages/greek.txt"};

        List<List<Cell>> tableData = new ArrayList<List<Cell>>();

        List<Cell> row = new ArrayList<Cell>();
        row.add(new Cell(f2, "Language"));
        row.add(new Cell(f2, "Text"));
        tableData.add(row);

        for (int i = 0; i < languages.length; i++) {
            // Each line of the file after the first two is a paragraph.
            List<String> lines = Content.linesOfTextFile(files[i]);
            TextColumn column = new TextColumn();
            column.setWidth(400f);
            for (int j = 2; j < lines.size(); j++) {
                Paragraph paragraph = new Paragraph();
                paragraph.add(new TextLine(f1, lines.get(j)));
                column.addParagraph(paragraph);
            }

            row = new ArrayList<Cell>();
            row.add(new Cell(f1, languages[i]));
            row.add(new Cell(f1, ""));
            row.get(1).setTextColumn(column);
            tableData.add(row);
        }

        Table table = new Table();
        table.setTableData(tableData, 1);
        table.setColumnWidth(0, 90f);
        table.setColumnWidth(1, 420f);
        table.setLocation(50f, 100f);
        table.drawOn(page);

        pdf.complete();
    }

    public static void main(String[] args) throws Exception {
        long time0 = System.currentTimeMillis();
        new Example_29();
        long time1 = System.currentTimeMillis();
        System.out.printf("Example_29 => %4d ms%n", time1 - time0);
    }
}   // End of Example_29.java
