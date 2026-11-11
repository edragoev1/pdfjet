/*
 * DefaultWidthTest.cs
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
using System;
using System.Collections.Generic;
using Xunit;

namespace PDFjet.NET {
public class DefaultWidthTest {
    // Every line PDFjet draws by default is 0.5 points wide (9.1.9), as Word's
    // table borders and TextBlock's and TextFrame's are: 0, the thinnest line
    // of the device, is a pixel on a screen and faint or gone in print, and the
    // PDF standard advises against it (ISO 32000, 8.4.3.2). 0 is still there
    // when set.
    [Fact]
    public void EveryLineIsHalfAPointUnlessSet() {
        PDF pdf = TestSupport.NewPDF();
        Font font = TestSupport.Helvetica(pdf);
        Assert.Equal(0.5f, new Cell(font, "x").GetBorderWidth());
        // The width a rect, a line and a path draw with, on pages of their own
        Page page = new Page(pdf, Letter.PORTRAIT);
        new Rect(0f, 0f, 10f, 10f).SetBorderColor(Color.black).DrawOn(page);
        Assert.Contains("0.5 w\n", TestSupport.Content(page));
        page = new Page(pdf, Letter.PORTRAIT);
        new Line(0f, 0f, 10f, 10f).DrawOn(page);
        Assert.Contains("0.5 w\n", TestSupport.Content(page));
        page = new Page(pdf, Letter.PORTRAIT);
        new Path().Add(new Point(0f, 0f)).Add(new Point(10f, 10f)).DrawOn(page);
        Assert.Contains("0.5 w\n", TestSupport.Content(page));
        page = new Page(pdf, Letter.PORTRAIT);
        page.SetDefaultPenWidth();
        Assert.Contains("0.5 w\n", TestSupport.Content(page));

        BigTable table = new BigTable(pdf, font, font, Letter.PORTRAIT).SetNumberOfColumns(1);
        table.SetTableData(new string[] {"Name"}, new List<string[]> {new string[] {"a"}});
        table.SetLocation(20f, 20f);
        table.Complete();
        string content = TestSupport.Content(table.GetPages()[0]);
        Assert.Contains("0.5 w\n", content);
        Assert.DoesNotContain("\n0 w\n", content);
        // Set to 0, the thinnest line, as before
        BigTable zero = new BigTable(pdf, font, font, Letter.PORTRAIT).SetNumberOfColumns(1).SetBorderWidth(0f);
        zero.SetTableData(new string[] {"Name"}, new List<string[]> {new string[] {"a"}});
        zero.SetLocation(20f, 20f);
        zero.Complete();
        Assert.Contains("0 w\n", TestSupport.Content(zero.GetPages()[0]));
    }
}
}
