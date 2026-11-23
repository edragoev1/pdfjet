/*
 * StampPathTest.cs
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
using System.Collections.Generic;
using System.IO;
using Xunit;

namespace PDFjet.NET {
// A stamp's path is written as a page's is: a curve's operator, c, v or y,
// and a newline after it and after the path's operator. Stamp had no
// Append(char), so C# converted the char to a float and wrote 99 for c and
// 10 for the newline: every DrawPath of a stamp ended "S10", and its curves
// were not curves (the cleanup scan of 23 November 2026).
public class StampPathTest {
    [Fact]
    public void AStampsPathHasItsOperatorsAndNewlines() {
        MemoryStream stream = new MemoryStream();
        PDF pdf = new PDF(stream);
        Page page = new Page(pdf, Letter.PORTRAIT);
        Stamp stamp = new Stamp(pdf).SetSize(200f, 200f);
        stamp.DrawPath(new List<Point> {
            new Point(15f, 15f),
            new Point(25f, 25f, Point.CONTROL_POINT_C),
            new Point(35f, 35f, Point.CONTROL_POINT_C),
            new Point(45f, 45f) }, PathOperator.STROKE);
        stamp.DrawPath(new List<Point> { new Point(15f, 15f), new Point(45f, 15f) }, PathOperator.FILL);
        stamp.Complete();
        stamp.SetLocation(50f, 50f);
        stamp.DrawOn(page);
        pdf.Complete();
        string text = TestSupport.Latin1(stream.ToArray());
        Assert.Contains(" c\nS\n", text);
        Assert.Contains(" l\nf\n", text);
        Assert.DoesNotContain("S10", text);
        Assert.DoesNotContain("f10", text);
    }
}
}
