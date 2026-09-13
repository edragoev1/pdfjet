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
    }

    [Fact]
    public void AVisibleLayerHasTheViewStateOn() {
        string pdf = Layer(true);
        Assert.Contains("/View << /ViewState /ON >>", pdf);
        Assert.DoesNotContain("/ViewState /OFF", pdf);
        // Export is off unless SetExportable(true) is called.
        Assert.Contains("/Export << /ExportState /OFF >>", pdf);
    }

    [Fact]
    public void ClearRemovesTheDrawables() {
        OptionalContentGroup group = new OptionalContentGroup(TestSupport.NewPDF(), "Layer");
        group.Add(new Rect(0f, 0f, 1f, 1f)).Add(new Line(0f, 0f, 1f, 1f));
        Assert.Equal(2, group.GetComponents().Count);
        Assert.Empty(group.Clear().GetComponents());
        Assert.Equal("Layer", group.GetName());
    }
}
}
