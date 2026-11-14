/**
 * LinkActionTest.cs
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using Xunit;

namespace PDFjet.NET {
// The links to one address share one action, which holds the address: each
// link carried it, and twenty links to an address of 20,000 characters added
// about 800 KB. Each link keeps its own description.
public class LinkActionTest {
    [Fact]
    public void LinksToOneAddressShareOneAction() {
        string address = "https://pdfjet.com/" + new string('a', 300);
        MemoryStream stream = new MemoryStream();
        PDF pdf = new PDF(stream);
        Font font = TestSupport.PlexSans(pdf);
        Page page = new Page(pdf, Letter.PORTRAIT);
        new TextLine(font, "One").SetURIAction(address).SetLocation(50f, 100f).DrawOn(page);
        new TextLine(font, "Two").SetURIAction(address).SetLocation(50f, 120f).DrawOn(page);
        new TextLine(font, "Site").SetURIAction("https://pdfjet.com").SetLocation(50f, 140f).DrawOn(page);
        pdf.Complete();
        string raw = TestSupport.Latin1(stream.ToArray());
        Assert.Equal(3, raw.Split("/Subtype /Link").Length - 1);
        Assert.Equal(2, raw.Split("/S /URI\n").Length - 1);
        HashSet<string> actions = new HashSet<string>();
        MatchCollection links = Regex.Matches(raw, @"/A (\d+) 0 R\n");
        foreach (Match link in links) {
            actions.Add(link.Groups[1].Value);
        }
        Assert.Equal(3, links.Count);
        Assert.Equal(2, actions.Count);
    }
}
}
