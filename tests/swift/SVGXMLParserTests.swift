/**
 * SVGXMLParserTests.swift
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
import Foundation
import Testing
@testable import PDFjet

/// The reader of the XML of SVG images, as the Java XMLParserTest reads it:
/// what it reads, of an SVG and of the Cross Industry Invoice of PDFjet Pro,
/// where it came from, and what it refuses.
@Suite struct SVGXMLParserTests {
    // An invoice as the Cross Industry Invoice of ZUGFeRD and Factur-X is
    // written: the prefixes of the namespaces, the codes in attributes, and a
    // line of goods.
    private static let INVOICE = #"""
        <?xml version="1.0" encoding="UTF-8"?>
        <rsm:CrossIndustryInvoice
                xmlns:rsm="urn:un:unece:uncefact:data:standard:CrossIndustryInvoice:100"
                xmlns:ram="urn:un:unece:uncefact:data:standard:ReusableAggregateBusinessInformationEntity:100"
                xmlns:udt="urn:un:unece:uncefact:data:standard:UnqualifiedDataType:100">
            <rsm:ExchangedDocumentContext>
                <ram:GuidelineSpecifiedDocumentContextParameter>
                    <ram:ID>urn:factur-x.eu:1p0:basic</ram:ID>
                </ram:GuidelineSpecifiedDocumentContextParameter>
            </rsm:ExchangedDocumentContext>
            <rsm:ExchangedDocument>
                <ram:ID>RE-2026-0042</ram:ID>
                <ram:TypeCode>380</ram:TypeCode>
                <ram:IssueDateTime>
                    <udt:DateTimeString format="102">20260922</udt:DateTimeString>
                </ram:IssueDateTime>
                <ram:IncludedNote><ram:Content>Zahlbar ohne Abzug &amp; ohne Skonto</ram:Content></ram:IncludedNote>
            </rsm:ExchangedDocument>
            <rsm:SupplyChainTradeTransaction>
                <ram:IncludedSupplyChainTradeLineItem>
                    <ram:SpecifiedTradeProduct><ram:Name>PDFjet für Java</ram:Name></ram:SpecifiedTradeProduct>
                    <ram:SpecifiedLineTradeDelivery>
                        <ram:BilledQuantity unitCode="C62">1</ram:BilledQuantity>
                    </ram:SpecifiedLineTradeDelivery>
                </ram:IncludedSupplyChainTradeLineItem>
                <ram:IncludedSupplyChainTradeLineItem>
                    <ram:SpecifiedTradeProduct><ram:Name>Support</ram:Name></ram:SpecifiedTradeProduct>
                    <ram:SpecifiedLineTradeDelivery>
                        <ram:BilledQuantity unitCode="C62">2</ram:BilledQuantity>
                    </ram:SpecifiedLineTradeDelivery>
                </ram:IncludedSupplyChainTradeLineItem>
                <ram:ApplicableHeaderTradeSettlement>
                    <ram:InvoiceCurrencyCode>EUR</ram:InvoiceCurrencyCode>
                    <ram:SpecifiedTradeSettlementHeaderMonetarySummation>
                        <ram:GrandTotalAmount>708.05</ram:GrandTotalAmount>
                    </ram:SpecifiedTradeSettlementHeaderMonetarySummation>
                </ram:ApplicableHeaderTradeSettlement>
            </rsm:SupplyChainTradeTransaction>
        </rsm:CrossIndustryInvoice>

        """#

    private func parse(_ xml: String) throws -> SVGXMLNode {
        return try SVGXMLParser.parse(Array(xml.utf8))
    }

    private func message(_ xml: String) -> String {
        let error = #expect(throws: SVGXMLError.self, "\(xml)") {
            try parse(xml)
        }
        return error?.message ?? ""
    }

    @Test func readsTheValuesOfAnInvoiceByTheirPath() throws {
        let invoice = try parse(SVGXMLParserTests.INVOICE)
        #expect(invoice.getName() == "rsm:CrossIndustryInvoice")
        #expect(invoice.getLocalName() == "CrossIndustryInvoice")
        #expect(invoice.getNamespace() == "urn:un:unece:uncefact:data:standard:CrossIndustryInvoice:100")
        #expect(invoice.getValue("ExchangedDocument/ID") == "RE-2026-0042")
        #expect(invoice.getValue("ExchangedDocument/TypeCode") == "380")
        #expect(invoice.getValue("ExchangedDocument/IssueDateTime/DateTimeString") == "20260922")
        #expect(invoice.getValue(
                "SupplyChainTradeTransaction/ApplicableHeaderTradeSettlement/InvoiceCurrencyCode") == "EUR")
        #expect(invoice.getValue("SupplyChainTradeTransaction/ApplicableHeaderTradeSettlement"
                + "/SpecifiedTradeSettlementHeaderMonetarySummation/GrandTotalAmount") == "708.05")
        #expect(invoice.getValue("ExchangedDocument/NotThere") == nil)
    }

    @Test func readsTheLinesOfAnInvoiceAndTheirAttributes() throws {
        let invoice = try parse(SVGXMLParserTests.INVOICE)
        let lines = invoice.findAll("SupplyChainTradeTransaction/IncludedSupplyChainTradeLineItem")
        #expect(lines.count == 2)
        #expect(lines[0].getValue("SpecifiedTradeProduct/Name") == "PDFjet für Java")
        #expect(lines[1].getValue("SpecifiedTradeProduct/Name") == "Support")
        let quantity = try #require(lines[0].find("SpecifiedLineTradeDelivery/BilledQuantity"))
        #expect(quantity.getText() == "1")
        #expect(quantity.getAttribute("unitCode") == "C62")
        #expect(quantity.getAttribute("currencyID") == nil)
        // The namespace of an element is the one of its prefix.
        #expect(quantity.getNamespace() == "urn:un:unece:uncefact:data:standard:"
                + "ReusableAggregateBusinessInformationEntity:100")
        // A * in the path is an element of any name.
        #expect(invoice.findAll("SupplyChainTradeTransaction/*/SpecifiedTradeProduct").count == 2)
    }

    @Test func readsTheEntitiesOfXMLAndTheNumericOnes() throws {
        #expect(try parse(SVGXMLParserTests.INVOICE).getValue("ExchangedDocument/IncludedNote/Content")
                == "Zahlbar ohne Abzug & ohne Skonto")
        #expect(try parse("<a>&lt; &gt; &amp; &quot; &apos;</a>").getText() == #"< > & " '"#)
        #expect(try parse("<a>&#160;&#x20AC;&#x1F600;</a>").getText() == "\u{00A0}€😀")
        #expect(try parse(#"<a note="a&amp;b"/>"#).getAttribute("note") == "a&b")
    }

    @Test func readsCommentseCDATAAndProcessingInstructions() throws {
        let node = try parse(#"<?xml version="1.0"?><!-- a note -->"# + "\n"
                + "<a><?target data?><b><![CDATA[ <not> & an element ]]></b><!--x--></a>")
        // getText is the text as it is, and getValue is it without the
        // spaces and line breaks at its ends.
        #expect(try #require(node.find("b")).getText() == " <not> & an element ")
        #expect(node.getValue("b") == "<not> & an element")
    }

    @Test func readsTheTextOfAnElementAndNotOfTheElementsInIt() throws {
        let node = try parse("<a>one<b>two</b>three</a>")
        #expect(node.getText() == "onethree")
        #expect(node.getValue("b") == "two")
        #expect(node.getChildren().count == 1)
    }

    @Test func readsTagsThatCloseThemselvesAndAttributesInBothQuotes() throws {
        let node = try parse(#"<a><b id='1' note = "two" /><b id='2'/></a>"#)
        let found = node.findAll("b")
        #expect(found.count == 2)
        #expect(found[0].getAttribute("id") == "1")
        #expect(found[0].getAttribute("note") == "two")
        #expect(found[1].getText() == "")
        #expect(try parse("<a/>").getChildren().count == 0)
    }

    @Test func readsTheDefaultNamespaceAndTheOnesOfTheElementsInside() throws {
        let node = try parse(#"<a xmlns="urn:one"><b><c xmlns="urn:two"/></b></a>"#)
        #expect(node.getNamespace() == "urn:one")
        #expect(try #require(node.find("b")).getNamespace() == "urn:one")
        #expect(try #require(node.find("b/c")).getNamespace() == "urn:two")
        // The namespace of an element is not the one of the element after it.
        let other = try parse(#"<a xmlns:p="urn:one"><p:b/><c/></a>"#)
        #expect(try #require(other.find("b")).getNamespace() == "urn:one")
        #expect(try #require(other.find("c")).getNamespace() == "")
    }

    @Test func readsUTF8AndUTF16WithTheirByteOrderMarks() throws {
        let xml = "<a>€</a>"
        let utf8 = Array(xml.utf8)
        let withMark: [UInt8] = [0xEF, 0xBB, 0xBF] + utf8
        #expect(try SVGXMLParser.parse(withMark).getText() == "€")
        #expect(try SVGXMLParser.parse(bytes("\u{FEFF}" + xml, .utf16BigEndian)).getText() == "€")
        #expect(try SVGXMLParser.parse(bytes("\u{FEFF}" + xml, .utf16LittleEndian)).getText() == "€")
        #expect(try SVGXMLParser.parse(InputStream(data: Data(utf8))).getText() == "€")
    }

    private func bytes(_ text: String, _ encoding: String.Encoding) -> [UInt8] {
        return [UInt8](text.data(using: encoding) ?? Data())
    }

    @Test func readsTheNumbersOfXMLAndNoOtherDigits() throws {
        #expect(try parse("<a>&#65;</a>").getText() == "A")
        #expect(try parse("<a>&#x41;</a>").getText() == "A")
        #expect(try parse("<a>&#X41;</a>").getText() == "A")
        #expect(try parse("<a>&#128512;</a>").getText() == "😀")
        // The digits of another script are not the digits of XML, and neither
        // is a sign or a number that no character has. A reader that takes
        // them reads a document the four ports would not agree on.
        #expect(message("<a>&#١٢;</a>").contains("is not a number"))
        #expect(message("<a>&#x４１;</a>").contains("is not a number"))
        #expect(message("<a>&#-1;</a>").contains("is not a number"))
        #expect(message("<a>&#+65;</a>").contains("is not a number"))
        #expect(message("<a>&#;</a>").contains("is not a number"))
        #expect(message("<a>&#x;</a>").contains("is not a number"))
        #expect(message("<a>&#99999999999;</a>").contains("is not a number"))
        #expect(message("<a>&#1114112;</a>").contains("not a character of XML"))
    }

    @Test func readsWhatFollowsAHalfOfACharacterThatHasNoOtherHalf() throws {
        // UTF-16 of "<a>", a high half alone, and "</a>". The decoder of Java
        // would swallow the < after the half and refuse the document; the
        // other three ports read the element, and so does this one.
        var units: [UInt16] = [0xFEFF, 0x3C, 0x61, 0x3E, 0xD800, 0x3C, 0x2F, 0x61, 0x3E]
        #expect(try SVGXMLParser.parse(bigEndian(units)).getText() == "\u{FFFD}")
        // A low half alone, and a last byte without its pair.
        units[4] = 0xDC00
        #expect(try SVGXMLParser.parse(bigEndian(units)).getText() == "\u{FFFD}")
        // A last byte without its pair is a replacement character too, which
        // here stands after the document and is text the document may not
        // have, so the reader refuses it rather than leave the byte out.
        let odd = bigEndian(units) + [0x78]     // x
        let error = #expect(throws: SVGXMLError.self) {
            try SVGXMLParser.parse(odd)
        }
        let problem = error?.message ?? ""
        #expect(problem.contains("more than the one element"))
        // The halves of one character are one character, in both orders of the
        // bytes, and the text that follows is read as it stands.
        #expect(try SVGXMLParser.parse(
                bytes("\u{FEFF}<a>😀!</a>", .utf16BigEndian)).getText() == "😀!")
        #expect(try SVGXMLParser.parse(
                bytes("\u{FEFF}<a>😀!</a>", .utf16LittleEndian)).getText() == "😀!")
    }

    private func bigEndian(_ units: [UInt16]) -> [UInt8] {
        var bytes = [UInt8]()
        for unit in units {
            bytes.append(UInt8(unit >> 8))
            bytes.append(UInt8(unit & 0xFF))
        }
        return bytes
    }

    @Test func skipsADocumentTypeDeclarationAndDoesNotReadItsEntities() throws {
        // The files of the drawing programs have a DOCTYPE, which is skipped;
        // its entities read files and URLs, and grow a short document into
        // gigabytes, so they are not read, and a reference to one is an error.
        let svg = try parse("""
            <?xml version="1.0"?>
            <!DOCTYPE svg PUBLIC "-//W3C//DTD SVG 1.1//EN" "http://www.w3.org/Graphics/SVG/1.1/DTD/svg11.dtd">
            <svg xmlns="http://www.w3.org/2000/svg"><path d="M0 0"/></svg>
            """)
        #expect(svg.getName() == "svg")
        #expect(svg.getChildren().count == 1)
        _ = try parse(#"<!DOCTYPE a [<!-- a ] and a > in a comment --><!ENTITY x "]>"><!ATTLIST a b CDATA "x>">]><a/>"#)
        #expect(message(#"<!DOCTYPE a [<!ENTITY x SYSTEM "file:///etc/passwd">]><a>&x;</a>"#)
                .hasPrefix("The entity &x; is not one of XML"))
        #expect(message(#"<!DOCTYPE a [<!ENTITY a "xx"><!ENTITY b "&a;&a;">]><a>&b;</a>"#)
                .hasPrefix("The entity &b; is not one of XML"))
        #expect(message("<a>&x;</a>").hasPrefix("The entity &x; is not one of XML"))
        #expect(message(#"<!DOCTYPE a [<!ENTITY x "y">"#).contains("does not end"))
    }

    @Test func refusesWhatIsNotTheXMLOfAnInvoice() {
        #expect(message("<a><b></a>").contains("is closed by the tag of"))
        #expect(message("<a><b></b>").contains("does not end"))
        #expect(message("</a>").contains("closes no element"))
        #expect(message("<a/><b/>").contains("more than the one element"))
        #expect(message("text<a/>").hasPrefix("The document starts with text"))
        #expect(message("<a b/>").contains("has no value"))
        #expect(message("<a b=c/>").contains("not in quotes"))
        #expect(message(#"<a b="<"/>"#).contains("holds a <"))
        #expect(message("<a><!-- x</a>").contains("The comment does not end"))
        #expect(message("<a><![CDATA[x</a>").contains("The character data does not end"))
        #expect(message(#"<?xml version="1.0" encoding="ISO-8859-1"?><a/>"#)
                .contains("is not read, only UTF-8 and UTF-16"))
        #expect(message("<a>&#xD800;</a>").contains("not a character of XML"))
        #expect(message("   ").hasPrefix("The document has no element"))
        // XML has no attributes without a space between them, which the
        // parser of the invoices read (found by the fuzz replay of SVG, 8
        // October 2026).
        #expect(message(#"<a b="1"c="2"/>"#).contains("are not separated by a space"))
        #expect((try? parse("<a\tb='1'\nc=\"2\" />")) != nil)
    }

    @Test func readsElementsThatNestNoDeeperThanMaxDepth() throws {
        var deep = ""
        for _ in 0..<SVGXMLParser.MAX_DEPTH {
            deep += "<a>"
        }
        deep += "x"
        for _ in 0..<SVGXMLParser.MAX_DEPTH {
            deep += "</a>"
        }
        #expect(try deepestText(parse(deep)) == "x")
        let deeper = "<a>" + deep + "</a>"
        #expect(message(deeper).contains("nest more than \(SVGXMLParser.MAX_DEPTH)"))
    }

    private func deepestText(_ node: SVGXMLNode) -> String {
        var deepest = node
        while !deepest.getChildren().isEmpty {
            deepest = deepest.getChildren()[0]
        }
        return deepest.getText()
    }

    // A document of four times as many lines is read in about four times the
    // time, not sixteen. The two times are measured in the same run, the
    // shortest of three each, so that a slow or busy computer, or a debug
    // build, slows both.
    @Test func readsALargeInvoiceQuickly() throws {
        let small = try parseTime(5000)
        let large = try parseTime(20000)
        #expect(large <= small * 8, "an invoice of 20000 lines was read in \(large), and one of 5000 in \(small)")
    }

    // Reads an invoice of the lines three times, checks what it read, and
    // returns the shortest of the times it took.
    private func parseTime(_ count: Int) throws -> Duration {
        var xml = #"<rsm:CrossIndustryInvoice xmlns:rsm="urn:one" xmlns:ram="urn:two">"#
        for i in 0..<count {
            xml += "<ram:IncludedSupplyChainTradeLineItem><ram:SpecifiedTradeProduct><ram:Name>Item "
                    + "\(i)" + "</ram:Name></ram:SpecifiedTradeProduct>"
                    + #"<ram:BilledQuantity unitCode="C62">"# + "\(i)"
                    + "</ram:BilledQuantity></ram:IncludedSupplyChainTradeLineItem>"
        }
        xml += "</rsm:CrossIndustryInvoice>"
        var shortest: Duration?
        for _ in 0..<3 {
            var invoice: SVGXMLNode?
            let elapsed = try ContinuousClock().measure {
                invoice = try parse(xml)
            }
            shortest = min(shortest ?? elapsed, elapsed)
            let lines = try #require(invoice).findAll("IncludedSupplyChainTradeLineItem")
            try #require(lines.count == count)
            #expect(lines[count - 1].getValue("SpecifiedTradeProduct/Name") == "Item \(count - 1)")
        }
        return try #require(shortest)
    }
}
