/*
 * ReviewFollowupTest.cs
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
using System;
using System.Collections.Generic;
using System.IO;
using Xunit;

namespace PDFjet.NET {
/// <summary>
/// What was left open by the review: CMYK colors in PDF/A, the footer of a big
/// table, and the destinations and headings in a container.
/// </summary>
public class ReviewFollowupTest {
    private const float DELTA = 0.01f;

    [Fact]
    public void APDFADocumentUsesNoCMYKColor() {
        // The output intent of PDF/A is sRGB, so its colors are not CMYK, as
        // its images are not.
        Compliance[] levels = {Compliance.PDF_A_1B, Compliance.PDF_A_2B, Compliance.PDF_A_3A_UA_1};
        foreach (Compliance level in levels) {
            foreach (bool pen in new bool[] {true, false}) {
                Page page = new Page(new PDF(new MemoryStream(), level), Letter.PORTRAIT);
                string before = TestSupport.Content(page);
                InvalidOperationException e = Assert.Throws<InvalidOperationException>(() => {
                    if (pen) {
                        page.SetPenColorCMYK(0.1f, 0.2f, 0.3f, 0.4f);
                    } else {
                        page.SetBrushColorCMYK(0.1f, 0.2f, 0.3f, 0.4f);
                    }
                });
                Assert.Equal("A document of " + level + " cannot use a CMYK color: "
                        + "its output intent is sRGB, so its colors are gray or RGB.", e.Message);
                // Nothing is written
                Assert.Equal(before, TestSupport.Content(page));
            }
        }
        // A document that is not PDF/A uses them
        foreach (Compliance level in new Compliance[] {Compliance.PDF_1_7, Compliance.PDF_UA_1}) {
            Page page = new Page(new PDF(new MemoryStream(), level), Letter.PORTRAIT);
            page.SetPenColorCMYK(0.1f, 0.2f, 0.3f, 0.4f);
            page.SetBrushColorCMYK(0.1f, 0.2f, 0.3f, 0.4f);
            string content = TestSupport.Content(page);
            Assert.Contains("0.1 0.2 0.3 0.4 K\n", content);
            Assert.Contains("0.1 0.2 0.3 0.4 k\n", content);
        }
    }

    [Fact]
    public void TheFooterOfABigTableIsAPaginationArtifact() {
        PDF pdf = new PDF(new MemoryStream(), Compliance.PDF_UA_1).SetTitle("Title");
        Font font = TestSupport.Helvetica(pdf);
        List<string[]> rows = new List<string[]>();
        for (int i = 0; i < 100; i++) {
            rows.Add(new string[] {"n" + i, "City, " + i, i + ".5"});
        }
        BigTable table = new BigTable(pdf, font, font, Letter.PORTRAIT);
        table.SetNumberOfColumns(3);
        table.SetTableData(new string[] {"Name", "City", "Total"}, rows);
        table.SetLocation(10f, 10f);
        table.Complete();
        List<Page> pages = table.GetPages();
        // The content of the last page, which is written when the PDF is
        string content = TestSupport.Content(pages[pages.Count - 1]);
        // The footer is marked once, as a footer, and not inside a plain artifact
        int footer = content.IndexOf("/Artifact <</Type /Pagination /Subtype /Footer>> BDC\n", StringComparison.Ordinal);
        Assert.True(footer >= 0 && footer < content.IndexOf(TestSupport.Hex("Page 2 of 2"), StringComparison.Ordinal), content);
        Assert.DoesNotContain("/Artifact BMC\n/Artifact <<", content);
        pdf.Complete();
    }

    // Draws the text line with a destination, as a heading, in a container at
    // 100, 200 turned by the degrees, or on the page at x, y moved by 100, 200
    // when there is no container, and returns the page.
    private static Page DrawInContainer(bool inContainer, float degrees) {
        PDF pdf = new PDF(new MemoryStream(), Compliance.PDF_UA_1).SetTitle("Title");
        Page page = new Page(pdf, Letter.PORTRAIT);
        Font font = TestSupport.Helvetica(pdf);
        TextLine line = new TextLine(font, "Heading");
        line.SetDestination("heading");
        line.SetStructureType(StructElem.H1);
        if (inContainer) {
            line.SetLocation(0f, 50f);
            Container container = new Container(100f, 100f);
            container.SetLocation(100f, 200f);
            container.SetRotation(degrees);
            container.Add(line);
            container.DrawOn(page);
        } else {
            line.SetLocation(100f, 250f);
            line.DrawOn(page);
        }
        return page;
    }

    [Fact]
    public void ADestinationAndAHeadingInAContainerAreWhereTheTextIs() {
        // Moved with the container, as if drawn where it puts the text
        Page want = DrawInContainer(false, 0f);
        Page got = DrawInContainer(true, 0f);
        // The left of the page, where the destination of a text line is, is
        // the left of the container
        TestSupport.AssertNear(want.destinations[0].xPosition + 100f, got.destinations[0].xPosition, DELTA, "x");
        TestSupport.AssertNear(want.destinations[0].yPosition, got.destinations[0].yPosition, DELTA, "y");
        TestSupport.AssertNear(want.pdf.headings[0].top, got.pdf.headings[0].top, DELTA, "top");
        TestSupport.AssertNear(250f - 12f, got.pdf.headings[0].top, DELTA, "top");

        // Turned a quarter, the text runs down the page from 50 left of the
        // center, 150 and 250, and its top 12 above the baseline is 12 right of it
        Page turned = DrawInContainer(true, 90f);
        TestSupport.AssertNear(162f, turned.destinations[0].xPosition, DELTA, "x");
        TestSupport.AssertNear(792f - 200f, turned.destinations[0].yPosition, DELTA, "y");
        TestSupport.AssertNear(200f, turned.pdf.headings[0].top, DELTA, "top");
    }
}
}
