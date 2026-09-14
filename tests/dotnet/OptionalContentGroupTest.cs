/*
 * OptionalContentGroupTest.cs
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
using System.IO;
using Xunit;

namespace PDFjet.NET {
public class OptionalContentGroupTest {
    private static string Layer(bool visible) {
        MemoryStream stream = new MemoryStream();
        PDF pdf = new PDF(stream);
        Page page = new Page(pdf, Letter.PORTRAIT);
        OptionalContentGroup group = new OptionalContentGroup(pdf, "Layer").SetVisible(visible).SetPrintable(true);
        group.Add(new Rect(10f, 10f, 20f, 20f));
        TestSupport.AssertXY(30f, 30f, group.DrawOn(page));
        pdf.Complete();
        return TestSupport.Latin1(stream.ToArray());
    }

    [Fact]
    public void AHiddenLayerHasTheViewStateOff() {
        string pdf = Layer(false);
        Assert.Contains("/OCProperties", pdf);
        Assert.Contains("/View << /ViewState /OFF >>", pdf);
        Assert.Contains("/Print << /PrintState /ON >>", pdf);
        // The default configuration lists it too, for the viewers that do not apply the usage
        Assert.Contains("/OFF [", pdf);
    }

    [Fact]
    public void AVisibleLayerHasTheViewStateOn() {
        string pdf = Layer(true);
        Assert.Contains("/View << /ViewState /ON >>", pdf);
        Assert.DoesNotContain("/ViewState /OFF", pdf);
        Assert.DoesNotContain("/OFF [", pdf);
        // Export is off unless SetExportable(true) is called.
        Assert.Contains("/Export << /ExportState /OFF >>", pdf);
    }

    [Fact]
    public void AGroupWrapsItsContentInMarkedContentAndIsAPageProperty() {
        MemoryStream stream = new MemoryStream();
        PDF pdf = new PDF(stream);
        Page page1 = new Page(pdf, Letter.PORTRAIT);
        OptionalContentGroup map = new OptionalContentGroup(pdf, "Map").SetVisible(true);
        map.Add(new Rect(10f, 10f, 20f, 20f)).DrawOn(page1);
        OptionalContentGroup notes = new OptionalContentGroup(pdf, "Notes");
        notes.Add(new Line(0f, 0f, 10f, 10f)).DrawOn(page1);
        string content1 = TestSupport.Content(page1);
        Assert.Contains("/OC /OC1 BDC\n", content1);
        Assert.Contains("/OC /OC2 BDC\n", content1);
        Assert.Equal(2, content1.Split("EMC").Length - 1);
        // The same group on a second page is the same object
        Page page2 = new Page(pdf, Letter.PORTRAIT);
        map.DrawOn(page2);
        Assert.Contains("/OC /OC1 BDC\n", TestSupport.Content(page2));
        pdf.Complete();
        string file = TestSupport.Latin1(stream.ToArray());
        Assert.Equal(2, file.Split("/Type /OCG").Length - 1);
        Assert.Contains("/Properties", file);
        Assert.Contains("/OC1 ", file);
        Assert.Contains("/OC2 ", file);
        Assert.Contains("/OCGs [", file);
        Assert.Contains("/Order [", file);
    }

    [Fact]
    public void ClearRemovesTheDrawables() {
        OptionalContentGroup group = new OptionalContentGroup(TestSupport.NewPDF(), "Layer");
        group.Add(new Rect(0f, 0f, 1f, 1f)).Add(new Line(0f, 0f, 1f, 1f));
        Assert.Equal(2, group.GetComponents().Count);
        Assert.Empty(group.Clear().GetComponents());
        Assert.Equal("Layer", group.GetName());
    }

    [Fact]
    public void GetComponentsReturnsACopy() {
        OptionalContentGroup group = new OptionalContentGroup(TestSupport.NewPDF(), "Layer");
        group.Add(new Rect(0f, 0f, 1f, 1f));
        group.GetComponents().Clear();
        Assert.Single(group.GetComponents());
    }
}
}
