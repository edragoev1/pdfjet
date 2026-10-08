/*
 * XMLParserTest.java
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
package com.pdfjet;

import static org.junit.jupiter.api.Assertions.assertEquals;
import static org.junit.jupiter.api.Assertions.assertNull;
import static org.junit.jupiter.api.Assertions.assertThrows;
import static org.junit.jupiter.api.Assertions.assertTrue;

import java.io.ByteArrayInputStream;
import java.nio.charset.StandardCharsets;
import java.util.List;
import org.junit.jupiter.api.Test;

// The XML reader of SVG images: what it reads, of an SVG and of the Cross
// Industry Invoice of PDFjet Pro, where it came from, and what it refuses.
class XMLParserTest {
    // An invoice as the Cross Industry Invoice of ZUGFeRD and Factur-X is
    // written: the prefixes of the namespaces, the codes in attributes, and a
    // line of goods.
    private static final String INVOICE =
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

    private static XMLNode parse(String xml) throws Exception {
        return XMLParser.parse(xml.getBytes(StandardCharsets.UTF_8));
    }

    private static String message(String xml) {
        return assertThrows(XMLException.class, () -> parse(xml)).getMessage();
    }

    @Test
    void readsTheValuesOfAnInvoiceByTheirPath() throws Exception {
        XMLNode invoice = parse(INVOICE);
        assertEquals("rsm:CrossIndustryInvoice", invoice.getName());
        assertEquals("CrossIndustryInvoice", invoice.getLocalName());
        assertEquals("urn:un:unece:uncefact:data:standard:CrossIndustryInvoice:100", invoice.getNamespace());
        assertEquals("RE-2026-0042", invoice.getValue("ExchangedDocument/ID"));
        assertEquals("380", invoice.getValue("ExchangedDocument/TypeCode"));
        assertEquals("20260922", invoice.getValue("ExchangedDocument/IssueDateTime/DateTimeString"));
        assertEquals("EUR", invoice.getValue(
                "SupplyChainTradeTransaction/ApplicableHeaderTradeSettlement/InvoiceCurrencyCode"));
        assertEquals("708.05", invoice.getValue("SupplyChainTradeTransaction/ApplicableHeaderTradeSettlement"
                + "/SpecifiedTradeSettlementHeaderMonetarySummation/GrandTotalAmount"));
        assertNull(invoice.getValue("ExchangedDocument/NotThere"));
    }

    @Test
    void readsTheLinesOfAnInvoiceAndTheirAttributes() throws Exception {
        XMLNode invoice = parse(INVOICE);
        List<XMLNode> lines = invoice.findAll(
                "SupplyChainTradeTransaction/IncludedSupplyChainTradeLineItem");
        assertEquals(2, lines.size());
        assertEquals("PDFjet für Java", lines.get(0).getValue("SpecifiedTradeProduct/Name"));
        assertEquals("Support", lines.get(1).getValue("SpecifiedTradeProduct/Name"));
        XMLNode quantity = lines.get(0).find("SpecifiedLineTradeDelivery/BilledQuantity");
        assertEquals("1", quantity.getText());
        assertEquals("C62", quantity.getAttribute("unitCode"));
        assertNull(quantity.getAttribute("currencyID"));
        // The namespace of an element is the one of its prefix.
        assertEquals("urn:un:unece:uncefact:data:standard:"
                + "ReusableAggregateBusinessInformationEntity:100", quantity.getNamespace());
        // A * in the path is an element of any name.
        assertEquals(2, invoice.findAll("SupplyChainTradeTransaction/*/SpecifiedTradeProduct").size());
    }

    @Test
    void readsTheEntitiesOfXMLAndTheNumericOnes() throws Exception {
        assertEquals("Zahlbar ohne Abzug & ohne Skonto",
                parse(INVOICE).getValue("ExchangedDocument/IncludedNote/Content"));
        assertEquals("< > & \" '", parse("<a>&lt; &gt; &amp; &quot; &apos;</a>").getText());
        assertEquals(" €😀", parse("<a>&#160;&#x20AC;&#x1F600;</a>").getText());
        assertEquals("a&b", parse("<a note=\"a&amp;b\"/>").getAttribute("note"));
    }

    @Test
    void readsCommentseCDATAAndProcessingInstructions() throws Exception {
        XMLNode node = parse("<?xml version=\"1.0\"?><!-- a note -->\n"
                + "<a><?target data?><b><![CDATA[ <not> & an element ]]></b><!--x--></a>");
        // getText is the text as it is, and getValue is it without the
        // spaces and line breaks at its ends.
        assertEquals(" <not> & an element ", node.find("b").getText());
        assertEquals("<not> & an element", node.getValue("b"));
    }

    @Test
    void readsTheTextOfAnElementAndNotOfTheElementsInIt() throws Exception {
        XMLNode node = parse("<a>one<b>two</b>three</a>");
        assertEquals("onethree", node.getText());
        assertEquals("two", node.getValue("b"));
        assertEquals(1, node.getChildren().size());
    }

    @Test
    void readsTagsThatCloseThemselvesAndAttributesInBothQuotes() throws Exception {
        XMLNode node = parse("<a><b id='1' note = \"two\" /><b id='2'/></a>");
        List<XMLNode> found = node.findAll("b");
        assertEquals(2, found.size());
        assertEquals("1", found.get(0).getAttribute("id"));
        assertEquals("two", found.get(0).getAttribute("note"));
        assertEquals("", found.get(1).getText());
        assertEquals(0, parse("<a/>").getChildren().size());
    }

    @Test
    void readsTheDefaultNamespaceAndTheOnesOfTheElementsInside() throws Exception {
        XMLNode node = parse("<a xmlns=\"urn:one\"><b><c xmlns=\"urn:two\"/></b></a>");
        assertEquals("urn:one", node.getNamespace());
        assertEquals("urn:one", node.find("b").getNamespace());
        assertEquals("urn:two", node.find("b/c").getNamespace());
        // The namespace of an element is not the one of the element after it.
        XMLNode other = parse("<a xmlns:p=\"urn:one\"><p:b/><c/></a>");
        assertEquals("urn:one", other.find("b").getNamespace());
        assertEquals("", other.find("c").getNamespace());
    }

    @Test
    void readsUTF8AndUTF16WithTheirByteOrderMarks() throws Exception {
        String xml = "<a>€</a>";
        byte[] utf8 = xml.getBytes(StandardCharsets.UTF_8);
        byte[] withMark = new byte[utf8.length + 3];
        withMark[0] = (byte) 0xEF;
        withMark[1] = (byte) 0xBB;
        withMark[2] = (byte) 0xBF;
        System.arraycopy(utf8, 0, withMark, 3, utf8.length);
        assertEquals("€", XMLParser.parse(withMark).getText());
        assertEquals("€", XMLParser.parse(("﻿" + xml).getBytes(StandardCharsets.UTF_16BE)).getText());
        assertEquals("€", XMLParser.parse(("﻿" + xml).getBytes(StandardCharsets.UTF_16LE)).getText());
        assertEquals("€", XMLParser.parse(
                new ByteArrayInputStream(xml.getBytes(StandardCharsets.UTF_8))).getText());
    }

    @Test
    void readsTheNumbersOfXMLAndNoOtherDigits() throws Exception {
        assertEquals("A", parse("<a>&#65;</a>").getText());
        assertEquals("A", parse("<a>&#x41;</a>").getText());
        assertEquals("A", parse("<a>&#X41;</a>").getText());
        assertEquals("😀", parse("<a>&#128512;</a>").getText());
        // The digits of another script are not the digits of XML, and neither
        // is a sign or a number that no character has. A reader that takes
        // them reads a document the four ports would not agree on.
        assertTrue(message("<a>&#١٢;</a>").contains("is not a number"));
        assertTrue(message("<a>&#x４１;</a>").contains("is not a number"));
        assertTrue(message("<a>&#-1;</a>").contains("is not a number"));
        assertTrue(message("<a>&#+65;</a>").contains("is not a number"));
        assertTrue(message("<a>&#;</a>").contains("is not a number"));
        assertTrue(message("<a>&#x;</a>").contains("is not a number"));
        assertTrue(message("<a>&#99999999999;</a>").contains("is not a number"));
        assertTrue(message("<a>&#1114112;</a>").contains("not a character of XML"));
    }

    @Test
    void readsWhatFollowsAHalfOfACharacterThatHasNoOtherHalf() throws Exception {
        // UTF-16 of "<a>", a high half alone, and "</a>". The decoder of Java
        // would swallow the < after the half and refuse the document; the
        // other three ports read the element, and so does this one.
        char[] units = {'﻿', '<', 'a', '>', '\uD800', '<', '/', 'a', '>'};
        byte[] bytes = new byte[2 * units.length];
        for (int i = 0; i < units.length; i++) {
            bytes[2 * i] = (byte) (units[i] >> 8);
            bytes[2 * i + 1] = (byte) units[i];
        }
        assertEquals("�", XMLParser.parse(bytes).getText());
        // A low half alone, and a last byte without its pair.
        units[4] = '\uDC00';
        for (int i = 0; i < units.length; i++) {
            bytes[2 * i] = (byte) (units[i] >> 8);
            bytes[2 * i + 1] = (byte) units[i];
        }
        assertEquals("�", XMLParser.parse(bytes).getText());
        // A last byte without its pair is a replacement character too, which
        // here stands after the document and is text the document may not
        // have, so the reader refuses it rather than leave the byte out.
        byte[] odd = new byte[bytes.length + 1];
        System.arraycopy(bytes, 0, odd, 0, bytes.length);
        odd[bytes.length] = 'x';
        assertTrue(assertThrows(XMLException.class, () -> XMLParser.parse(odd))
                .getMessage().contains("more than the one element"));
        // The halves of one character are one character, in both orders of the
        // bytes, and the text that follows is read as it stands.
        assertEquals("😀!", XMLParser.parse(
                ("﻿<a>😀!</a>").getBytes(StandardCharsets.UTF_16BE)).getText());
        assertEquals("😀!", XMLParser.parse(
                ("﻿<a>😀!</a>").getBytes(StandardCharsets.UTF_16LE)).getText());
    }

    @Test
    void skipsADocumentTypeDeclarationAndDoesNotReadItsEntities() throws Exception {
        // The files of the drawing programs have a DOCTYPE, which is skipped;
        // its entities read files and URLs, and grow a short document into
        // gigabytes, so they are not read, and a reference to one is an error.
        XMLNode svg = parse("<?xml version=\"1.0\"?>\n"
                + "<!DOCTYPE svg PUBLIC \"-//W3C//DTD SVG 1.1//EN\" "
                + "\"http://www.w3.org/Graphics/SVG/1.1/DTD/svg11.dtd\">\n"
                + "<svg xmlns=\"http://www.w3.org/2000/svg\"><path d=\"M0 0\"/></svg>");
        assertEquals("svg", svg.getName());
        assertEquals(1, svg.getChildren().size());
        parse("<!DOCTYPE a [<!-- a ] and a > in a comment --><!ENTITY x \"]>\">"
                + "<!ATTLIST a b CDATA \"x>\">]><a/>");
        assertTrue(message("<!DOCTYPE a [<!ENTITY x SYSTEM \"file:///etc/passwd\">]><a>&x;</a>")
                .startsWith("The entity &x; is not one of XML"));
        assertTrue(message("<!DOCTYPE a [<!ENTITY a \"xx\"><!ENTITY b \"&a;&a;\">]><a>&b;</a>")
                .startsWith("The entity &b; is not one of XML"));
        assertTrue(message("<a>&x;</a>").startsWith("The entity &x; is not one of XML"));
        assertTrue(message("<!DOCTYPE a [<!ENTITY x \"y\">").contains("does not end"));
    }

    @Test
    void refusesWhatIsNotTheXMLOfAnInvoice() throws Exception {
        assertTrue(message("<a><b></a>").contains("is closed by the tag of"));
        assertTrue(message("<a><b></b>").contains("does not end"));
        assertTrue(message("</a>").contains("closes no element"));
        assertTrue(message("<a/><b/>").contains("more than the one element"));
        assertTrue(message("text<a/>").startsWith("The document starts with text"));
        assertTrue(message("<a b/>").contains("has no value"));
        assertTrue(message("<a b=c/>").contains("not in quotes"));
        assertTrue(message("<a b=\"<\"/>").contains("holds a <"));
        assertTrue(message("<a><!-- x</a>").contains("The comment does not end"));
        assertTrue(message("<a><![CDATA[x</a>").contains("The character data does not end"));
        assertTrue(message("<?xml version=\"1.0\" encoding=\"ISO-8859-1\"?><a/>")
                .contains("is not read, only UTF-8 and UTF-16"));
        assertTrue(message("<a>&#xD800;</a>").contains("not a character of XML"));
        assertTrue(message("   ").startsWith("The document has no element"));
        // XML has no attributes without a space between them, which the
        // parser of the invoices read (found by the fuzz replay of SVG, 8
        // October 2026).
        assertTrue(message("<a b=\"1\"c=\"2\"/>").contains("are not separated by a space"));
        parse("<a\tb='1'\nc=\"2\" />");
    }

    @Test
    void readsElementsThatNestNoDeeperThanMaxDepth() throws Exception {
        StringBuilder deep = new StringBuilder();
        for (int i = 0; i < XMLParser.MAX_DEPTH; i++) {
            deep.append("<a>");
        }
        deep.append("x");
        for (int i = 0; i < XMLParser.MAX_DEPTH; i++) {
            deep.append("</a>");
        }
        assertEquals("x", deepestText(parse(deep.toString())));
        StringBuilder deeper = new StringBuilder("<a>");
        deeper.append(deep);
        deeper.append("</a>");
        assertTrue(message(deeper.toString()).contains("nest more than " + XMLParser.MAX_DEPTH));
    }

    private static String deepestText(XMLNode node) {
        XMLNode deepest = node;
        while (!deepest.getChildren().isEmpty()) {
            deepest = deepest.getChildren().get(0);
        }
        return deepest.getText();
    }

    // A document of four times as many lines is read in about four times the
    // time, not sixteen. The two times are measured in the same run, the
    // shortest of three each, so that a slow or busy computer slows both.
    @Test
    void readsALargeInvoiceQuickly() throws Exception {
        long small = parseTime(5000);
        long large = parseTime(20000);
        assertTrue(large <= 8 * small, "an invoice of 20000 lines was read in " + large / 1000000
                + " ms, and one of 5000 in " + small / 1000000 + " ms");
    }

    // Reads an invoice of the lines three times, checks what it read, and
    // returns the shortest of the times it took, in nanoseconds.
    private static long parseTime(int count) throws Exception {
        StringBuilder xml = new StringBuilder("<rsm:CrossIndustryInvoice xmlns:rsm=\"urn:one\" xmlns:ram=\"urn:two\">");
        for (int i = 0; i < count; i++) {
            xml.append("<ram:IncludedSupplyChainTradeLineItem><ram:SpecifiedTradeProduct><ram:Name>Item ")
                    .append(i).append("</ram:Name></ram:SpecifiedTradeProduct>")
                    .append("<ram:BilledQuantity unitCode=\"C62\">").append(i)
                    .append("</ram:BilledQuantity></ram:IncludedSupplyChainTradeLineItem>");
        }
        xml.append("</rsm:CrossIndustryInvoice>");
        long shortest = Long.MAX_VALUE;
        for (int run = 0; run < 3; run++) {
            long time0 = System.nanoTime();
            XMLNode invoice = parse(xml.toString());
            shortest = Math.min(shortest, System.nanoTime() - time0);
            List<XMLNode> lines = invoice.findAll("IncludedSupplyChainTradeLineItem");
            assertEquals(count, lines.size());
            assertEquals("Item " + (count - 1), lines.get(count - 1).getValue("SpecifiedTradeProduct/Name"));
        }
        return shortest;
    }
}
