/*
 * XMLParserTest.cs
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Text;
using Xunit;

namespace PDFjet.NET {
/// <summary>
/// The XML reader of SVG images, as in the Go and Java XMLParserTest: what it
/// reads, of an SVG and of the Cross Industry Invoice of PDFjet Pro, where it
/// came from, and what it refuses.
/// </summary>
public class XMLParserTest {
    // An invoice as the Cross Industry Invoice of ZUGFeRD and Factur-X is
    // written: the prefixes of the namespaces, the codes in attributes, and a
    // line of goods.
    private const string INVOICE =
            "<?xml version=\"1.0\" encoding=\"UTF-8\"?>\n"
            + "<rsm:CrossIndustryInvoice\n"
            + "        xmlns:rsm=\"urn:un:unece:uncefact:data:standard:CrossIndustryInvoice:100\"\n"
            + "        xmlns:ram=\"urn:un:unece:uncefact:data:standard:ReusableAggregateBusinessInformationEntity:100\"\n"
            + "        xmlns:udt=\"urn:un:unece:uncefact:data:standard:UnqualifiedDataType:100\">\n"
            + "    <rsm:ExchangedDocumentContext>\n"
            + "        <ram:GuidelineSpecifiedDocumentContextParameter>\n"
            + "            <ram:ID>urn:factur-x.eu:1p0:basic</ram:ID>\n"
            + "        </ram:GuidelineSpecifiedDocumentContextParameter>\n"
            + "    </rsm:ExchangedDocumentContext>\n"
            + "    <rsm:ExchangedDocument>\n"
            + "        <ram:ID>RE-2026-0042</ram:ID>\n"
            + "        <ram:TypeCode>380</ram:TypeCode>\n"
            + "        <ram:IssueDateTime>\n"
            + "            <udt:DateTimeString format=\"102\">20260922</udt:DateTimeString>\n"
            + "        </ram:IssueDateTime>\n"
            + "        <ram:IncludedNote><ram:Content>Zahlbar ohne Abzug &amp; ohne Skonto</ram:Content></ram:IncludedNote>\n"
            + "    </rsm:ExchangedDocument>\n"
            + "    <rsm:SupplyChainTradeTransaction>\n"
            + "        <ram:IncludedSupplyChainTradeLineItem>\n"
            + "            <ram:SpecifiedTradeProduct><ram:Name>PDFjet für Java</ram:Name></ram:SpecifiedTradeProduct>\n"
            + "            <ram:SpecifiedLineTradeDelivery>\n"
            + "                <ram:BilledQuantity unitCode=\"C62\">1</ram:BilledQuantity>\n"
            + "            </ram:SpecifiedLineTradeDelivery>\n"
            + "        </ram:IncludedSupplyChainTradeLineItem>\n"
            + "        <ram:IncludedSupplyChainTradeLineItem>\n"
            + "            <ram:SpecifiedTradeProduct><ram:Name>Support</ram:Name></ram:SpecifiedTradeProduct>\n"
            + "            <ram:SpecifiedLineTradeDelivery>\n"
            + "                <ram:BilledQuantity unitCode=\"C62\">2</ram:BilledQuantity>\n"
            + "            </ram:SpecifiedLineTradeDelivery>\n"
            + "        </ram:IncludedSupplyChainTradeLineItem>\n"
            + "        <ram:ApplicableHeaderTradeSettlement>\n"
            + "            <ram:InvoiceCurrencyCode>EUR</ram:InvoiceCurrencyCode>\n"
            + "            <ram:SpecifiedTradeSettlementHeaderMonetarySummation>\n"
            + "                <ram:GrandTotalAmount>708.05</ram:GrandTotalAmount>\n"
            + "            </ram:SpecifiedTradeSettlementHeaderMonetarySummation>\n"
            + "        </ram:ApplicableHeaderTradeSettlement>\n"
            + "    </rsm:SupplyChainTradeTransaction>\n"
            + "</rsm:CrossIndustryInvoice>\n";

    private static XMLNode Parse(string xml) {
        return XMLParser.Parse(Encoding.UTF8.GetBytes(xml));
    }

    private static string Message(string xml) {
        return Assert.Throws<XMLException>(() => { Parse(xml); }).Message;
    }

    [Fact]
    public void ReadsTheValuesOfAnInvoiceByTheirPath() {
        XMLNode invoice = Parse(INVOICE);
        Assert.Equal("rsm:CrossIndustryInvoice", invoice.GetName());
        Assert.Equal("CrossIndustryInvoice", invoice.GetLocalName());
        Assert.Equal("urn:un:unece:uncefact:data:standard:CrossIndustryInvoice:100", invoice.GetNamespace());
        Assert.Equal("RE-2026-0042", invoice.GetValue("ExchangedDocument/ID"));
        Assert.Equal("380", invoice.GetValue("ExchangedDocument/TypeCode"));
        Assert.Equal("20260922", invoice.GetValue("ExchangedDocument/IssueDateTime/DateTimeString"));
        Assert.Equal("EUR", invoice.GetValue(
                "SupplyChainTradeTransaction/ApplicableHeaderTradeSettlement/InvoiceCurrencyCode"));
        Assert.Equal("708.05", invoice.GetValue("SupplyChainTradeTransaction/ApplicableHeaderTradeSettlement"
                + "/SpecifiedTradeSettlementHeaderMonetarySummation/GrandTotalAmount"));
        Assert.Null(invoice.GetValue("ExchangedDocument/NotThere"));
    }

    [Fact]
    public void ReadsTheLinesOfAnInvoiceAndTheirAttributes() {
        XMLNode invoice = Parse(INVOICE);
        List<XMLNode> lines = invoice.FindAll(
                "SupplyChainTradeTransaction/IncludedSupplyChainTradeLineItem");
        Assert.Equal(2, lines.Count);
        Assert.Equal("PDFjet für Java", lines[0].GetValue("SpecifiedTradeProduct/Name"));
        Assert.Equal("Support", lines[1].GetValue("SpecifiedTradeProduct/Name"));
        XMLNode quantity = lines[0].Find("SpecifiedLineTradeDelivery/BilledQuantity");
        Assert.Equal("1", quantity.GetText());
        Assert.Equal("C62", quantity.GetAttribute("unitCode"));
        Assert.Null(quantity.GetAttribute("currencyID"));
        // The namespace of an element is the one of its prefix.
        Assert.Equal("urn:un:unece:uncefact:data:standard:"
                + "ReusableAggregateBusinessInformationEntity:100", quantity.GetNamespace());
        // A * in the path is an element of any name.
        Assert.Equal(2, invoice.FindAll("SupplyChainTradeTransaction/*/SpecifiedTradeProduct").Count);
    }

    [Fact]
    public void ReadsTheEntitiesOfXMLAndTheNumericOnes() {
        Assert.Equal("Zahlbar ohne Abzug & ohne Skonto",
                Parse(INVOICE).GetValue("ExchangedDocument/IncludedNote/Content"));
        Assert.Equal("< > & \" '", Parse("<a>&lt; &gt; &amp; &quot; &apos;</a>").GetText());
        Assert.Equal(" €😀", Parse("<a>&#160;&#x20AC;&#x1F600;</a>").GetText());
        Assert.Equal("a&b", Parse("<a note=\"a&amp;b\"/>").GetAttribute("note"));
    }

    [Fact]
    public void ReadsCommentseCDATAAndProcessingInstructions() {
        XMLNode node = Parse("<?xml version=\"1.0\"?><!-- a note -->\n"
                + "<a><?target data?><b><![CDATA[ <not> & an element ]]></b><!--x--></a>");
        // GetText is the text as it is, and GetValue is it without the
        // spaces and line breaks at its ends.
        Assert.Equal(" <not> & an element ", node.Find("b").GetText());
        Assert.Equal("<not> & an element", node.GetValue("b"));
    }

    [Fact]
    public void ReadsTheTextOfAnElementAndNotOfTheElementsInIt() {
        XMLNode node = Parse("<a>one<b>two</b>three</a>");
        Assert.Equal("onethree", node.GetText());
        Assert.Equal("two", node.GetValue("b"));
        Assert.Single(node.GetChildren());
    }

    [Fact]
    public void ReadsTagsThatCloseThemselvesAndAttributesInBothQuotes() {
        XMLNode node = Parse("<a><b id='1' note = \"two\" /><b id='2'/></a>");
        List<XMLNode> found = node.FindAll("b");
        Assert.Equal(2, found.Count);
        Assert.Equal("1", found[0].GetAttribute("id"));
        Assert.Equal("two", found[0].GetAttribute("note"));
        Assert.Equal("", found[1].GetText());
        Assert.Empty(Parse("<a/>").GetChildren());
    }

    [Fact]
    public void ReadsTheDefaultNamespaceAndTheOnesOfTheElementsInside() {
        XMLNode node = Parse("<a xmlns=\"urn:one\"><b><c xmlns=\"urn:two\"/></b></a>");
        Assert.Equal("urn:one", node.GetNamespace());
        Assert.Equal("urn:one", node.Find("b").GetNamespace());
        Assert.Equal("urn:two", node.Find("b/c").GetNamespace());
        // The namespace of an element is not the one of the element after it.
        XMLNode other = Parse("<a xmlns:p=\"urn:one\"><p:b/><c/></a>");
        Assert.Equal("urn:one", other.Find("b").GetNamespace());
        Assert.Equal("", other.Find("c").GetNamespace());
    }

    [Fact]
    public void ReadsUTF8AndUTF16WithTheirByteOrderMarks() {
        string xml = "<a>€</a>";
        byte[] utf8 = Encoding.UTF8.GetBytes(xml);
        byte[] withMark = new byte[utf8.Length + 3];
        withMark[0] = 0xEF;
        withMark[1] = 0xBB;
        withMark[2] = 0xBF;
        Array.Copy(utf8, 0, withMark, 3, utf8.Length);
        Assert.Equal("€", XMLParser.Parse(withMark).GetText());
        Assert.Equal("€", XMLParser.Parse(Encoding.BigEndianUnicode.GetBytes("﻿" + xml)).GetText());
        Assert.Equal("€", XMLParser.Parse(Encoding.Unicode.GetBytes("﻿" + xml)).GetText());
        using (MemoryStream stream = new MemoryStream(Encoding.UTF8.GetBytes(xml))) {
            Assert.Equal("€", XMLParser.Parse(stream).GetText());
        }
    }

    [Fact]
    public void ReadsTheNumbersOfXMLAndNoOtherDigits() {
        Assert.Equal("A", Parse("<a>&#65;</a>").GetText());
        Assert.Equal("A", Parse("<a>&#x41;</a>").GetText());
        Assert.Equal("A", Parse("<a>&#X41;</a>").GetText());
        Assert.Equal("😀", Parse("<a>&#128512;</a>").GetText());
        // The digits of another script are not the digits of XML, and neither
        // is a sign or a number that no character has. A reader that takes
        // them reads a document the four ports would not agree on.
        Assert.Contains("is not a number", Message("<a>&#١٢;</a>"), StringComparison.Ordinal);
        Assert.Contains("is not a number", Message("<a>&#x４１;</a>"), StringComparison.Ordinal);
        Assert.Contains("is not a number", Message("<a>&#-1;</a>"), StringComparison.Ordinal);
        Assert.Contains("is not a number", Message("<a>&#+65;</a>"), StringComparison.Ordinal);
        Assert.Contains("is not a number", Message("<a>&#;</a>"), StringComparison.Ordinal);
        Assert.Contains("is not a number", Message("<a>&#x;</a>"), StringComparison.Ordinal);
        Assert.Contains("is not a number", Message("<a>&#99999999999;</a>"), StringComparison.Ordinal);
        Assert.Contains("not a character of XML", Message("<a>&#1114112;</a>"), StringComparison.Ordinal);
    }

    [Fact]
    public void ReadsWhatFollowsAHalfOfACharacterThatHasNoOtherHalf() {
        // UTF-16 of "<a>", a high half alone, and "</a>". The decoder of Java
        // would swallow the < after the half and refuse the document; the
        // other three ports read the element, and so does this one.
        char[] units = {'﻿', '<', 'a', '>', '\uD800', '<', '/', 'a', '>'};
        byte[] bytes = new byte[2 * units.Length];
        for (int i = 0; i < units.Length; i++) {
            bytes[2 * i] = (byte) (units[i] >> 8);
            bytes[2 * i + 1] = (byte) units[i];
        }
        Assert.Equal("�", XMLParser.Parse(bytes).GetText());
        // A low half alone, and a last byte without its pair.
        units[4] = '\uDC00';
        for (int i = 0; i < units.Length; i++) {
            bytes[2 * i] = (byte) (units[i] >> 8);
            bytes[2 * i + 1] = (byte) units[i];
        }
        Assert.Equal("�", XMLParser.Parse(bytes).GetText());
        // A last byte without its pair is a replacement character too, which
        // here stands after the document and is text the document may not
        // have, so the reader refuses it rather than leave the byte out.
        byte[] odd = new byte[bytes.Length + 1];
        Array.Copy(bytes, 0, odd, 0, bytes.Length);
        odd[bytes.Length] = (byte) 'x';
        Assert.Contains("more than the one element",
                Assert.Throws<XMLException>(() => { XMLParser.Parse(odd); }).Message,
                StringComparison.Ordinal);
        // The halves of one character are one character, in both orders of the
        // bytes, and the text that follows is read as it stands.
        Assert.Equal("😀!", XMLParser.Parse(
                Encoding.BigEndianUnicode.GetBytes("﻿<a>😀!</a>")).GetText());
        Assert.Equal("😀!", XMLParser.Parse(
                Encoding.Unicode.GetBytes("﻿<a>😀!</a>")).GetText());
    }

    [Fact]
    public void SkipsADocumentTypeDeclarationAndDoesNotReadItsEntities() {
        // The files of the drawing programs have a DOCTYPE, which
        // ParseSkippingDoctype skips, as SVGImage reads them; its entities
        // read files and URLs, and grow a short document into gigabytes, so
        // they are not read, and a reference to one is an error.
        XMLNode svg = Skipping("<?xml version=\"1.0\"?>\n"
                + "<!DOCTYPE svg PUBLIC \"-//W3C//DTD SVG 1.1//EN\" "
                + "\"http://www.w3.org/Graphics/SVG/1.1/DTD/svg11.dtd\">\n"
                + "<svg xmlns=\"http://www.w3.org/2000/svg\"><path d=\"M0 0\"/></svg>");
        Assert.Equal("svg", svg.GetName());
        Assert.Single(svg.GetChildren());
        Skipping("<!DOCTYPE a [<!-- a ] and a > in a comment --><!ENTITY x \"]>\">"
                + "<!ATTLIST a b CDATA \"x>\">]><a/>");
        Assert.StartsWith("The entity &x; is not one of XML",
                SkippingMessage("<!DOCTYPE a [<!ENTITY x SYSTEM \"file:///etc/passwd\">]><a>&x;</a>"),
                StringComparison.Ordinal);
        Assert.StartsWith("The entity &b; is not one of XML",
                SkippingMessage("<!DOCTYPE a [<!ENTITY a \"xx\"><!ENTITY b \"&a;&a;\">]><a>&b;</a>"),
                StringComparison.Ordinal);
        Assert.StartsWith("The entity &x; is not one of XML", SkippingMessage("<a>&x;</a>"), StringComparison.Ordinal);
        Assert.Contains("does not end", SkippingMessage("<!DOCTYPE a [<!ENTITY x \"y\">"), StringComparison.Ordinal);
        using (MemoryStream stream = new MemoryStream(Encoding.UTF8.GetBytes("<!DOCTYPE svg><svg/>"))) {
            Assert.Equal("svg", XMLParser.ParseSkippingDoctype(stream).GetName());
        }
    }

    [Fact]
    public void ParseRefusesADocumentTypeDeclarationAndWithItTheEntitiesThatAttackAReader() {
        // The electronic invoices of PDFjet Pro are read with Parse, which
        // refuses a DOCTYPE: an invoice has none, and with it go its entities.
        Assert.StartsWith("A document type declaration is not read",
                Message("<!DOCTYPE a [<!ENTITY x SYSTEM \"file:///etc/passwd\">]><a>&x;</a>"),
                StringComparison.Ordinal);
        Assert.StartsWith("A document type declaration is not read",
                Message("<!DOCTYPE svg><svg/>"), StringComparison.Ordinal);
        using (MemoryStream stream = new MemoryStream(Encoding.UTF8.GetBytes("<!DOCTYPE svg><svg/>"))) {
            Assert.StartsWith("A document type declaration is not read",
                    Assert.Throws<XMLException>(() => { XMLParser.Parse(stream); }).Message,
                    StringComparison.Ordinal);
        }
    }

    private static XMLNode Skipping(string xml) {
        return XMLParser.ParseSkippingDoctype(Encoding.UTF8.GetBytes(xml));
    }

    private static string SkippingMessage(string xml) {
        return Assert.Throws<XMLException>(() => { Skipping(xml); }).Message;
    }

    [Fact]
    public void RefusesWhatIsNotTheXMLOfAnInvoice() {
        Assert.Contains("is closed by the tag of", Message("<a><b></a>"), StringComparison.Ordinal);
        Assert.Contains("does not end", Message("<a><b></b>"), StringComparison.Ordinal);
        Assert.Contains("closes no element", Message("</a>"), StringComparison.Ordinal);
        Assert.Contains("more than the one element", Message("<a/><b/>"), StringComparison.Ordinal);
        Assert.StartsWith("The document starts with text", Message("text<a/>"), StringComparison.Ordinal);
        Assert.Contains("has no value", Message("<a b/>"), StringComparison.Ordinal);
        Assert.Contains("not in quotes", Message("<a b=c/>"), StringComparison.Ordinal);
        Assert.Contains("holds a <", Message("<a b=\"<\"/>"), StringComparison.Ordinal);
        Assert.Contains("The comment does not end", Message("<a><!-- x</a>"), StringComparison.Ordinal);
        Assert.Contains("The character data does not end", Message("<a><![CDATA[x</a>"), StringComparison.Ordinal);
        Assert.Contains("is not read, only UTF-8 and UTF-16",
                Message("<?xml version=\"1.0\" encoding=\"ISO-8859-1\"?><a/>"), StringComparison.Ordinal);
        Assert.Contains("not a character of XML", Message("<a>&#xD800;</a>"), StringComparison.Ordinal);
        Assert.StartsWith("The document has no element", Message("   "), StringComparison.Ordinal);
        // XML has no attributes without a space between them, which the
        // parser of the invoices read (found by the fuzz replay of SVG, 8
        // October 2026).
        Assert.Contains("are not separated by a space", Message("<a b=\"1\"c=\"2\"/>"), StringComparison.Ordinal);
        Parse("<a\tb='1'\nc=\"2\" />");
    }

    [Fact]
    public void ReadsElementsThatNestNoDeeperThanMaxDepth() {
        StringBuilder deep = new StringBuilder();
        for (int i = 0; i < XMLParser.MAX_DEPTH; i++) {
            deep.Append("<a>");
        }
        deep.Append("x");
        for (int i = 0; i < XMLParser.MAX_DEPTH; i++) {
            deep.Append("</a>");
        }
        Assert.Equal("x", DeepestText(Parse(deep.ToString())));
        StringBuilder deeper = new StringBuilder("<a>");
        deeper.Append(deep);
        deeper.Append("</a>");
        Assert.Contains("nest more than " + XMLParser.MAX_DEPTH.ToString(CultureInfo.InvariantCulture),
                Message(deeper.ToString()), StringComparison.Ordinal);
    }

    private static string DeepestText(XMLNode node) {
        XMLNode deepest = node;
        while (deepest.GetChildren().Count != 0) {
            deepest = deepest.GetChildren()[0];
        }
        return deepest.GetText();
    }

    // A document of four times as many lines is read in about four times the
    // time, not sixteen. The two times are measured in the same run, the
    // shortest of three each, so that a slow or busy computer slows both.
    [Fact]
    public void ReadsALargeInvoiceQuickly() {
        TimeSpan small = ParseTime(5000);
        TimeSpan large = ParseTime(20000);
        Assert.True(large.Ticks <= 8 * small.Ticks, "an invoice of 20000 lines was read in "
                + large.TotalMilliseconds.ToString(CultureInfo.InvariantCulture) + " ms, and one of 5000 in "
                + small.TotalMilliseconds.ToString(CultureInfo.InvariantCulture) + " ms");
    }

    // Reads an invoice of the lines three times, checks what it read, and
    // returns the shortest of the times it took.
    private static TimeSpan ParseTime(int count) {
        StringBuilder xml = new StringBuilder("<rsm:CrossIndustryInvoice xmlns:rsm=\"urn:one\" xmlns:ram=\"urn:two\">");
        for (int i = 0; i < count; i++) {
            xml.Append("<ram:IncludedSupplyChainTradeLineItem><ram:SpecifiedTradeProduct><ram:Name>Item ")
                    .Append(i.ToString(CultureInfo.InvariantCulture))
                    .Append("</ram:Name></ram:SpecifiedTradeProduct>")
                    .Append("<ram:BilledQuantity unitCode=\"C62\">")
                    .Append(i.ToString(CultureInfo.InvariantCulture))
                    .Append("</ram:BilledQuantity></ram:IncludedSupplyChainTradeLineItem>");
        }
        xml.Append("</rsm:CrossIndustryInvoice>");
        TimeSpan shortest = TimeSpan.MaxValue;
        for (int run = 0; run < 3; run++) {
            Stopwatch stopwatch = Stopwatch.StartNew();
            XMLNode invoice = Parse(xml.ToString());
            if (stopwatch.Elapsed < shortest) {
                shortest = stopwatch.Elapsed;
            }
            List<XMLNode> lines = invoice.FindAll("IncludedSupplyChainTradeLineItem");
            Assert.Equal(count, lines.Count);
            Assert.Equal("Item " + (count - 1).ToString(CultureInfo.InvariantCulture),
                    lines[count - 1].GetValue("SpecifiedTradeProduct/Name"));
        }
        return shortest;
    }

    [Fact]
    public void ReadsAnUndeclaredPrefixAsNoNamespaceWhenItSkipsADoctype() {
        // An SVG copied from a web page has xlink:href without xmlns:xlink, and
        // one of a drawing program can have sodipodi: elements without their
        // declaration; refused, they no longer drew as they did before the parser
        // (the review of 9 October 2026). An invoice is read as strictly as ever.
        string xml = "<svg xmlns=\"http://www.w3.org/2000/svg\"><use xlink:href=\"#a\"/><sodipodi:namedview/></svg>";
        Assert.Contains("is not declared", Message(xml), StringComparison.Ordinal);
        Assert.Contains("is not declared", Message("<svg><use xlink:href=\"#a\"/></svg>"), StringComparison.Ordinal);
        XMLNode svg = Skipping(xml);
        Assert.Equal("#a", svg.GetChildren()[0].GetAttribute("xlink:href"));
        Assert.Equal("#a", svg.GetChildren()[0].GetAttribute("href"));
        XMLNode view = svg.GetChildren()[1];
        Assert.Equal("namedview", view.GetLocalName());
        Assert.Equal("", view.GetNamespace());
    }

    [Fact]
    public void ADeclarationIsNotAnAttributeOfItsPrefix() {
        XMLNode node = Parse("<a xmlns:id=\"urn:x\" b=\"1\"/>");
        Assert.Null(node.GetAttribute("id"));
        Assert.Equal("1", node.GetAttribute("b"));
    }

    [Fact]
    public void RefusesAnAttributeWrittenTwiceAmongMany() {
        // Past 16 attributes they are compared by a set, before by one another.
        StringBuilder b = new StringBuilder("<a");
        for (int i = 0; i < 20; i++) {
            b.Append(" a").Append(i.ToString(CultureInfo.InvariantCulture)).Append("='1'");
        }
        foreach (string twice in new string[] {" a3='2'", " a18='2'"}) {
            Assert.Contains("is written twice", Message(b.ToString() + twice + "/>"), StringComparison.Ordinal);
        }
        Assert.Contains("is written twice", Message("<a b='1' c='2' b='3'/>"), StringComparison.Ordinal);
        XMLNode node = Parse(b.ToString() + "/>");
        Assert.Equal(20, node.GetAttributes().Count);
        Assert.Equal("1", node.GetAttribute("a19"));
    }

    [Fact]
    public void ReadsAStylesheetInstructionFirstAndAQuoteInAnInstructionOfTheDoctype() {
        // A processing instruction whose name starts with xml, first, was read as
        // the declaration, and its pseudo-attribute as an encoding; a quote in an
        // instruction of a skipped DOCTYPE as the start of a string (the review
        // of 9 October 2026).
        Assert.Equal("1", Parse("<?xml-stylesheet type=\"text/xsl\" encoding=\"latin1\" href=\"a.xsl\"?><a b=\"1\"/>")
                .GetAttribute("b"));
        Assert.Equal("1", Skipping("<!DOCTYPE svg [<?pi don't ?>]><svg b=\"1\"/>").GetAttribute("b"));
        Assert.Contains("is not read", Message("<?xml version=\"1.0\" encoding=\"latin1\"?><a/>"), StringComparison.Ordinal);
        Assert.Contains("empty prefix", Message("<a xmlns:=\"urn:x\"/>"), StringComparison.Ordinal);
    }

    [Fact]
    public void ReadsAMillionElementsAndNoMore() {
        // A document of 20 MB of tiny elements took hundreds of megabytes (the
        // review of 9 October 2026).
        Func<int, string> many = count => "<a>" + new StringBuilder().Insert(0, "<b/>", count - 1) + "</a>";
        Parse(many(XMLParser.MAX_ELEMENTS));
        Assert.Contains("more than 1000000 elements", Message(many(XMLParser.MAX_ELEMENTS + 1)),
                StringComparison.Ordinal);
    }
}
}   // End of namespace PDFjet.NET
