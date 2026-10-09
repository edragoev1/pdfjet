// xmlparser_test.go
//
// Copyright (c) 2026 PDFjet Software
// Licensed under the MIT License. See LICENSE file in the project root.

package xmlparser

import (
	"bytes"
	"strconv"
	"strings"
	"testing"
	"time"
	"unicode/utf16"
)

// The XML reader of SVG images, as in the Java XMLParserTest: what it reads,
// of an SVG and of the Cross Industry Invoice of PDFjet Pro, where it came
// from, and what it refuses.

// An invoice as the Cross Industry Invoice of ZUGFeRD and Factur-X is written:
// the prefixes of the namespaces, the codes in attributes, and a line of goods.
const testInvoice = `<?xml version="1.0" encoding="UTF-8"?>
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
`

func testParse(t *testing.T, xml string) *XMLNode {
	t.Helper()
	node, err := Parse([]byte(xml))
	if err != nil {
		t.Fatal(err)
	}
	return node
}

// testParseSkipping reads the document as an SVG is read, its DOCTYPE skipped.
func testParseSkipping(t *testing.T, xml string) *XMLNode {
	t.Helper()
	node, err := ParseSkippingDoctype([]byte(xml))
	if err != nil {
		t.Fatal(err)
	}
	return node
}

// testMessageSkipping returns the message of the error the document is
// refused with when its DOCTYPE is skipped.
func testMessageSkipping(t *testing.T, xml string) string {
	t.Helper()
	node, err := ParseSkippingDoctype([]byte(xml))
	if err == nil {
		t.Fatalf("%q was read as the element %s, but is not the XML this reads", xml, node.Name())
	}
	return err.Error()
}

// testMessage returns the message of the error the document is refused with.
func testMessage(t *testing.T, xml string) string {
	t.Helper()
	node, err := Parse([]byte(xml))
	if err == nil {
		t.Fatalf("%q was read as the element %s, but is not the XML this reads", xml, node.Name())
	}
	return err.Error()
}

func testEqualText(t *testing.T, expected, actual string) {
	t.Helper()
	if expected != actual {
		t.Errorf("expected %q, but was %q", expected, actual)
	}
}

// testEqualValue checks the text of the first element at the path.
func testEqualValue(t *testing.T, node *XMLNode, path, expected string) {
	t.Helper()
	value, found := node.Value(path)
	if !found {
		t.Errorf("there is no element at %q", path)
		return
	}
	testEqualText(t, expected, value)
}

func testEqualAttribute(t *testing.T, node *XMLNode, attributeName, expected string) {
	t.Helper()
	value, found := node.Attribute(attributeName)
	if !found {
		t.Errorf("there is no attribute %q", attributeName)
		return
	}
	testEqualText(t, expected, value)
}

func testContains(t *testing.T, message, expected string) {
	t.Helper()
	if !strings.Contains(message, expected) {
		t.Errorf("expected a message with %q, but was %q", expected, message)
	}
}

func testStartsWith(t *testing.T, message, expected string) {
	t.Helper()
	if !strings.HasPrefix(message, expected) {
		t.Errorf("expected a message that starts with %q, but was %q", expected, message)
	}
}

func testCount(t *testing.T, expected, actual int, what string) {
	t.Helper()
	if expected != actual {
		t.Errorf("expected %d %s, but were %d", expected, what, actual)
	}
}

// testUTF16 returns the characters in UTF-16, without a byte order mark.
func testUTF16(characters string, littleEndian bool) []byte {
	out := make([]byte, 0, 2*len(characters))
	for _, unit := range utf16.Encode([]rune(characters)) {
		if littleEndian {
			out = append(out, byte(unit), byte(unit>>8))
		} else {
			out = append(out, byte(unit>>8), byte(unit))
		}
	}
	return out
}

func TestXMLParserReadsTheValuesOfAnInvoiceByTheirPath(t *testing.T) {
	invoice := testParse(t, testInvoice)
	testEqualText(t, "rsm:CrossIndustryInvoice", invoice.Name())
	testEqualText(t, "CrossIndustryInvoice", invoice.LocalName())
	testEqualText(t, "urn:un:unece:uncefact:data:standard:CrossIndustryInvoice:100", invoice.Namespace())
	testEqualValue(t, invoice, "ExchangedDocument/ID", "RE-2026-0042")
	testEqualValue(t, invoice, "ExchangedDocument/TypeCode", "380")
	testEqualValue(t, invoice, "ExchangedDocument/IssueDateTime/DateTimeString", "20260922")
	testEqualValue(t, invoice,
		"SupplyChainTradeTransaction/ApplicableHeaderTradeSettlement/InvoiceCurrencyCode", "EUR")
	testEqualValue(t, invoice, "SupplyChainTradeTransaction/ApplicableHeaderTradeSettlement"+
		"/SpecifiedTradeSettlementHeaderMonetarySummation/GrandTotalAmount", "708.05")
	if value, found := invoice.Value("ExchangedDocument/NotThere"); found {
		t.Errorf("expected no value at ExchangedDocument/NotThere, but was %q", value)
	}
}

func TestXMLParserReadsTheLinesOfAnInvoiceAndTheirAttributes(t *testing.T) {
	invoice := testParse(t, testInvoice)
	lines := invoice.FindAll("SupplyChainTradeTransaction/IncludedSupplyChainTradeLineItem")
	testCount(t, 2, len(lines), "lines")
	testEqualValue(t, lines[0], "SpecifiedTradeProduct/Name", "PDFjet für Java")
	testEqualValue(t, lines[1], "SpecifiedTradeProduct/Name", "Support")
	quantity := lines[0].Find("SpecifiedLineTradeDelivery/BilledQuantity")
	testEqualText(t, "1", quantity.Text())
	testEqualAttribute(t, quantity, "unitCode", "C62")
	if value, found := quantity.Attribute("currencyID"); found {
		t.Errorf("expected no attribute currencyID, but was %q", value)
	}
	// The namespace of an element is the one of its prefix.
	testEqualText(t, "urn:un:unece:uncefact:data:standard:"+
		"ReusableAggregateBusinessInformationEntity:100", quantity.Namespace())
	// A * in the path is an element of any name.
	testCount(t, 2, len(invoice.FindAll("SupplyChainTradeTransaction/*/SpecifiedTradeProduct")), "elements")
}

func TestXMLParserReadsTheEntitiesOfXMLAndTheNumericOnes(t *testing.T) {
	testEqualValue(t, testParse(t, testInvoice), "ExchangedDocument/IncludedNote/Content",
		"Zahlbar ohne Abzug & ohne Skonto")
	testEqualText(t, "< > & \" '", testParse(t, "<a>&lt; &gt; &amp; &quot; &apos;</a>").Text())
	testEqualText(t, " €😀", testParse(t, "<a>&#160;&#x20AC;&#x1F600;</a>").Text())
	testEqualAttribute(t, testParse(t, `<a note="a&amp;b"/>`), "note", "a&b")
}

func TestXMLParserReadsTheNumbersOfXMLAndNoOtherDigits(t *testing.T) {
	testEqualText(t, "A", testParse(t, "<a>&#65;</a>").Text())
	testEqualText(t, "A", testParse(t, "<a>&#x41;</a>").Text())
	testEqualText(t, "A", testParse(t, "<a>&#X41;</a>").Text())
	testEqualText(t, "\U0001F600", testParse(t, "<a>&#128512;</a>").Text())
	// The digits of another script are not the digits of XML, and neither is
	// a sign or a number that no character has. A reader that takes them reads
	// a document the four ports would not agree on.
	for _, xml := range []string{"<a>&#\u0661\u0662;</a>", "<a>&#x\uFF14\uFF11;</a>",
		"<a>&#-1;</a>", "<a>&#+65;</a>", "<a>&#;</a>", "<a>&#x;</a>",
		"<a>&#99999999999;</a>"} {
		if message := testMessage(t, xml); !strings.Contains(message, "is not a number") {
			t.Errorf("%s: %s", xml, message)
		}
	}
	if message := testMessage(t, "<a>&#1114112;</a>"); !strings.Contains(
		message, "not a character of XML") {
		t.Error(message)
	}
}

func TestXMLParserReadsWhatFollowsAHalfOfACharacterThatHasNoOtherHalf(t *testing.T) {
	// "<a>", a high half alone, and "</a>": the half is the replacement
	// character and the < after it is read as it stands, in both orders of
	// the bytes. The decoder of Java swallowed the <.
	for _, half := range []uint16{0xD800, 0xDC00} {
		for _, littleEndian := range []bool{false, true} {
			units := []uint16{0xFEFF, '<', 'a', '>', half, '<', '/', 'a', '>'}
			out := make([]byte, 0, 2*len(units))
			for _, unit := range units {
				if littleEndian {
					out = append(out, byte(unit), byte(unit>>8))
				} else {
					out = append(out, byte(unit>>8), byte(unit))
				}
			}
			node, err := Parse(out)
			if err != nil {
				t.Fatal(err)
			}
			testEqualText(t, "\uFFFD", node.Text())
			// A last byte without its pair is a replacement character too,
			// which stands after the document and is text it may not have.
			if _, err := Parse(append(out, 'x')); err == nil ||
				!strings.Contains(err.Error(), "more than the one element") {
				t.Errorf("a stray byte: %v", err)
			}
		}
	}
	node, err := Parse(testUTF16("\uFEFF<a>\U0001F600!</a>", false))
	if err != nil {
		t.Fatal(err)
	}
	testEqualText(t, "\U0001F600!", node.Text())
}

func TestXMLParserReadsCommentsCDATAAndProcessingInstructions(t *testing.T) {
	node := testParse(t, "<?xml version=\"1.0\"?><!-- a note -->\n"+
		"<a><?target data?><b><![CDATA[ <not> & an element ]]></b><!--x--></a>")
	// Text is the text as it is, and Value is it without the spaces and line
	// breaks at its ends.
	testEqualText(t, " <not> & an element ", node.Find("b").Text())
	testEqualValue(t, node, "b", "<not> & an element")
}

func TestXMLParserReadsTheTextOfAnElementAndNotOfTheElementsInIt(t *testing.T) {
	node := testParse(t, "<a>one<b>two</b>three</a>")
	testEqualText(t, "onethree", node.Text())
	testEqualValue(t, node, "b", "two")
	testCount(t, 1, len(node.Children()), "elements")
}

func TestXMLParserReadsTagsThatCloseThemselvesAndAttributesInBothQuotes(t *testing.T) {
	node := testParse(t, `<a><b id='1' note = "two" /><b id='2'/></a>`)
	found := node.FindAll("b")
	testCount(t, 2, len(found), "elements")
	testEqualAttribute(t, found[0], "id", "1")
	testEqualAttribute(t, found[0], "note", "two")
	testEqualText(t, "", found[1].Text())
	testCount(t, 0, len(testParse(t, "<a/>").Children()), "elements")
}

func TestXMLParserReadsTheDefaultNamespaceAndTheOnesOfTheElementsInside(t *testing.T) {
	node := testParse(t, `<a xmlns="urn:one"><b><c xmlns="urn:two"/></b></a>`)
	testEqualText(t, "urn:one", node.Namespace())
	testEqualText(t, "urn:one", node.Find("b").Namespace())
	testEqualText(t, "urn:two", node.Find("b/c").Namespace())
	// The namespace of an element is not the one of the element after it.
	other := testParse(t, `<a xmlns:p="urn:one"><p:b/><c/></a>`)
	testEqualText(t, "urn:one", other.Find("b").Namespace())
	testEqualText(t, "", other.Find("c").Namespace())
}

func TestXMLParserReadsUTF8AndUTF16WithTheirByteOrderMarks(t *testing.T) {
	xml := "<a>€</a>"
	withMark := append([]byte{0xEF, 0xBB, 0xBF}, []byte(xml)...)
	node, err := Parse(withMark)
	if err != nil {
		t.Fatal(err)
	}
	testEqualText(t, "€", node.Text())
	for _, littleEndian := range []bool{false, true} {
		node, err := Parse(testUTF16("\uFEFF"+xml, littleEndian))
		if err != nil {
			t.Fatal(err)
		}
		testEqualText(t, "€", node.Text())
	}
	node, err = ParseReader(bytes.NewReader([]byte(xml)))
	if err != nil {
		t.Fatal(err)
	}
	testEqualText(t, "€", node.Text())
}

func TestXMLParserRefusesADocumentTypeDeclarationAndWithItTheEntitiesThatAttackAReader(t *testing.T) {
	// The entities of a DOCTYPE read files and URLs, and grow a short document
	// into gigabytes; without it neither is possible. An invoice has none.
	testStartsWith(t, testMessage(t, `<!DOCTYPE a [<!ENTITY x SYSTEM "file:///etc/passwd">]><a>&x;</a>`),
		"A document type declaration is not read")
	testStartsWith(t, testMessage(t, `<!DOCTYPE a [<!ENTITY a "xx"><!ENTITY b "&a;&a;">]><a>&b;</a>`),
		"A document type declaration is not read")
	testStartsWith(t, testMessage(t, "<a>&x;</a>"), "The entity &x; is not one of XML")
}

func TestXMLParserSkipsADocumentTypeDeclarationAndDoesNotReadItsEntities(t *testing.T) {
	// The files of the drawing programs have a DOCTYPE, which an SVG image is
	// read past; its entities are not read, and a reference to one is an
	// error.
	svg := testParseSkipping(t, `<?xml version="1.0"?>
<!DOCTYPE svg PUBLIC "-//W3C//DTD SVG 1.1//EN" "http://www.w3.org/Graphics/SVG/1.1/DTD/svg11.dtd">
<svg xmlns="http://www.w3.org/2000/svg"><path d="M0 0"/></svg>`)
	testEqualText(t, "svg", svg.Name())
	testCount(t, 1, len(svg.Children()), "elements")
	testParseSkipping(t, `<!DOCTYPE a [<!-- a ] and a > in a comment --><!ENTITY x "]>"><!ATTLIST a b CDATA "x>">]><a/>`)
	testStartsWith(t, testMessageSkipping(t, `<!DOCTYPE a [<!ENTITY x SYSTEM "file:///etc/passwd">]><a>&x;</a>`),
		"The entity &x; is not one of XML")
	testStartsWith(t, testMessageSkipping(t, `<!DOCTYPE a [<!ENTITY a "xx"><!ENTITY b "&a;&a;">]><a>&b;</a>`),
		"The entity &b; is not one of XML")
	testContains(t, testMessageSkipping(t, `<!DOCTYPE a [<!ENTITY x "y">`), "does not end")
}

func TestXMLParserRefusesWhatIsNotTheXMLOfAnInvoice(t *testing.T) {
	testContains(t, testMessage(t, "<a><b></a>"), "is closed by the tag of")
	testContains(t, testMessage(t, "<a><b></b>"), "does not end")
	testContains(t, testMessage(t, "</a>"), "closes no element")
	testContains(t, testMessage(t, "<a/><b/>"), "more than the one element")
	testStartsWith(t, testMessage(t, "text<a/>"), "The document starts with text")
	testContains(t, testMessage(t, "<a b/>"), "has no value")
	testContains(t, testMessage(t, "<a b=c/>"), "not in quotes")
	testContains(t, testMessage(t, `<a b="<"/>`), "holds a <")
	testContains(t, testMessage(t, "<a><!-- x</a>"), "The comment does not end")
	testContains(t, testMessage(t, "<a><![CDATA[x</a>"), "The character data does not end")
	testContains(t, testMessage(t, `<?xml version="1.0" encoding="ISO-8859-1"?><a/>`),
		"is not read, only UTF-8 and UTF-16")
	testContains(t, testMessage(t, "<a>&#xD800;</a>"), "not a character of XML")
	testStartsWith(t, testMessage(t, "   "), "The document has no element")
	// XML has no attributes without a space between them, which the parser
	// of the invoices read (found by the fuzz replay of SVG, 8 October 2026).
	testContains(t, testMessage(t, `<a b="1"c="2"/>`), "are not separated by a space")
	testParse(t, "<a\tb='1'\nc=\"2\" />")
}

func TestXMLParserReadsElementsThatNestNoDeeperThanMaxDepth(t *testing.T) {
	var deep strings.Builder
	for i := 0; i < MaxDepth; i++ {
		deep.WriteString("<a>")
	}
	deep.WriteString("x")
	for i := 0; i < MaxDepth; i++ {
		deep.WriteString("</a>")
	}
	testEqualText(t, "x", testDeepestText(testParse(t, deep.String())))
	deeper := "<a>" + deep.String() + "</a>"
	testContains(t, testMessage(t, deeper), "nest more than "+strconv.Itoa(MaxDepth))
}

func testDeepestText(node *XMLNode) string {
	deepest := node
	for len(deepest.Children()) != 0 {
		deepest = deepest.Children()[0]
	}
	return deepest.Text()
}

// A document of four times as many lines is read in about four times the
// time, not sixteen. The two times are measured in the same run, the shortest
// of three each, so that a slow or busy computer slows both.
func TestXMLParserReadsALargeInvoiceQuickly(t *testing.T) {
	small := testParseTime(t, 5000)
	large := testParseTime(t, 20000)
	if large > 8*small {
		t.Errorf("an invoice of 20000 lines was read in %v, and one of 5000 in %v", large, small)
	}
}

// testParseTime reads an invoice of the lines three times, checks what it
// read, and returns the shortest of the times it took.
func testParseTime(t *testing.T, count int) time.Duration {
	var xml strings.Builder
	xml.WriteString(`<rsm:CrossIndustryInvoice xmlns:rsm="urn:one" xmlns:ram="urn:two">`)
	for i := 0; i < count; i++ {
		number := strconv.Itoa(i)
		xml.WriteString("<ram:IncludedSupplyChainTradeLineItem><ram:SpecifiedTradeProduct><ram:Name>Item ")
		xml.WriteString(number)
		xml.WriteString("</ram:Name></ram:SpecifiedTradeProduct>")
		xml.WriteString(`<ram:BilledQuantity unitCode="C62">`)
		xml.WriteString(number)
		xml.WriteString("</ram:BilledQuantity></ram:IncludedSupplyChainTradeLineItem>")
	}
	xml.WriteString("</rsm:CrossIndustryInvoice>")
	var shortest time.Duration
	for run := 0; run < 3; run++ {
		time0 := time.Now()
		invoice := testParse(t, xml.String())
		elapsed := time.Since(time0)
		if run == 0 || elapsed < shortest {
			shortest = elapsed
		}
		lines := invoice.FindAll("IncludedSupplyChainTradeLineItem")
		testCount(t, count, len(lines), "lines")
		testEqualValue(t, lines[count-1], "SpecifiedTradeProduct/Name", "Item "+strconv.Itoa(count-1))
	}
	return shortest
}
