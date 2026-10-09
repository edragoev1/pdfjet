// xmlparser.go
//
// Copyright (c) 2026 PDFjet Software
// Licensed under the MIT License. See LICENSE file in the project root.

// Package xmlparser reads XML into elements: their names, their namespaces,
// their attributes in the order they are written, and their text. It is the
// one parser of the library and of PDFjet Pro: the library reads SVG images
// with it, in the four ports alike, and PDFjet Pro the XML of electronic
// invoices, the Cross Industry Invoice of ZUGFeRD and Factur-X. It reads what
// such a document is made of and no more, and what it leaves out is what
// makes XML dangerous to read:
//   - A document type declaration, a DOCTYPE, is refused by Parse and
//     ParseReader, as an invoice has none, and skipped by
//     ParseSkippingDoctype and ParseReaderSkippingDoctype, as the SVG files
//     of the drawing programs have one. Its entities are not read either
//     way: an entity that names a file or a URL reads what it should not,
//     and the ones that stand for each other grow a short document into
//     gigabytes.
//   - The entities are the five of XML, &lt;, &gt;, &amp;, &quot; and &apos;,
//     and the numeric ones, such as &#160; and &#xA0;. Any other is an error,
//     the ones a DOCTYPE declares among them.
//   - The elements nest MaxDepth levels at most, and the reading does not
//     recurse, so no document overflows the stack.
//   - The document is 20 MB at most, and has MaxElements elements at most.
//   - The namespaces are looked up in the elements that declare them, from
//     the innermost out, and not copied into each element, so that no number
//     of declarations makes the reading slower than the length of the
//     document times its depth.
//   - Nothing is fetched, and no schema is read.
//
// What is read is XML, and what is not is an error: a character that XML
// does not have, such as U+0000 or U+FFFF, written or as an entity; an
// attribute written twice, or not after a space; and a prefix that no element
// declares. A line break is a line feed, as XML reads a carriage return and
// the line feed after it, and the tabs and line breaks in the value of an
// attribute are spaces, as XML normalizes them.
//
// The document is UTF-8, or UTF-16 when it starts with a byte order mark;
// another encoding in the declaration is an error.
//
// The document is read as characters, as the Java port reads it. The bytes
// that are not valid UTF-8 are the character U+FFFD, one for each part of a
// sequence that is as much of a character as it is valid for, which is what
// the Unicode Standard recommends and the decoders of the JDK, of .NET and of
// Swift do. A code unit of UTF-16 that is a surrogate without the other one
// of its pair is U+FFFD as well.
package xmlparser

import (
	"errors"
	"fmt"
	"io"
	"strconv"
	"strings"
	"unicode/utf16"
	"unicode/utf8"
)

// MaxDepth is how deep the elements of a document may nest.
const MaxDepth = 256

// MaxElements is how many elements a document may have, a million: an invoice
// of the 20 MB has fewer than half as many, and an SVG image has its points in
// the attributes of its paths, not in elements. It keeps a document of tiny
// elements from taking hundreds of megabytes (the review of 9 October 2026).
const MaxElements = 1000000

// MaxSize is how many bytes a document may be, 20 MB: an invoice of some
// fifteen thousand lines, or an SVG image of millions of points.
const MaxSize = 20 << 20

// The namespace of the prefix xml, which every document has without
// declaring it.
const namespaceXML = "http://www.w3.org/XML/1998/namespace"

type parser struct {
	xml    []rune
	index  int
	line   int
	column int
	// The elements that are open, the innermost last, and the namespaces
	// each of them declares, nil where it declares none. They are read one
	// after another and not by recursion, so they are a stack of this parser
	// and not of the calls.
	open   []*XMLNode
	scopes []map[string]string
	// True to skip a DOCTYPE, which is refused otherwise, and to read a
	// prefix that no element declares as no namespace, as SVG files copied
	// from web pages have xlink:href without xmlns:xlink.
	skipDoctype bool
	// The names read, each kept once, as a document repeats a few names
	// millions of times.
	names map[string]string
	// The elements read, MaxElements at most
	elements int
}

// Parse reads the document and returns its root element. It returns an error
// if the document is not the XML this reads, has a DOCTYPE, or is more than
// 20 MB.
func Parse(bytes []byte) (*XMLNode, error) {
	return parse(bytes, false)
}

// ParseSkippingDoctype reads the document as Parse does, and skips a DOCTYPE,
// which the SVG files of the drawing programs have. Its entities are not
// read, and a reference to one is an error. A prefix that no element declares
// is read as no namespace, as SVG files copied from web pages have xlink:href
// without xmlns:xlink, rather than an error.
func ParseSkippingDoctype(bytes []byte) (*XMLNode, error) {
	return parse(bytes, true)
}

func parse(bytes []byte, skipDoctype bool) (*XMLNode, error) {
	if len(bytes) > MaxSize {
		return nil, ErrTooLarge
	}
	xml, bad := normalize(decode(bytes))
	p := &parser{xml: xml, line: 1, column: 1, skipDoctype: skipDoctype}
	if bad != -1 {
		p.skip(bad)
		return nil, p.error(fmt.Sprintf("The character U+%04X is not a character of XML", xml[bad]))
	}
	return p.document()
}

// ErrTooLarge is the error of a document of more than MaxSize bytes.
var ErrTooLarge = errors.New("The document is more than 20 MB.")

// ParseReader reads the document of the reader as Parse does and returns its
// root element. The reader is read to its end, or to the MaxSize bytes a
// document may be, and is not closed.
func ParseReader(in io.Reader) (*XMLNode, error) {
	bytes, err := readAll(in)
	if err != nil {
		return nil, err
	}
	return Parse(bytes)
}

// ParseReaderSkippingDoctype reads the document of the reader as
// ParseSkippingDoctype does.
func ParseReaderSkippingDoctype(in io.Reader) (*XMLNode, error) {
	bytes, err := readAll(in)
	if err != nil {
		return nil, err
	}
	return ParseSkippingDoctype(bytes)
}

func readAll(in io.Reader) ([]byte, error) {
	return io.ReadAll(io.LimitReader(in, MaxSize+1))
}

// The characters of the document: UTF-8, or the UTF-16 of a byte order mark,
// without the mark. The bytes that are not valid UTF-8 are the character
// U+FFFD, one for each maximal part of a sequence, and so is a code unit of
// UTF-16 that is a surrogate without the other one, or a byte without the one
// after it.
func decode(bytes []byte) []rune {
	if len(bytes) >= 2 && bytes[0] == 0xFE && bytes[1] == 0xFF {
		return DecodeUTF16(bytes[2:], false)
	}
	if len(bytes) >= 2 && bytes[0] == 0xFF && bytes[1] == 0xFE {
		return DecodeUTF16(bytes[2:], true)
	}
	if len(bytes) >= 3 && bytes[0] == 0xEF && bytes[1] == 0xBB && bytes[2] == 0xBF {
		return decodeUTF8(bytes[3:])
	}
	return decodeUTF8(bytes)
}

// decodeUTF8 returns the characters of the UTF-8, with one U+FFFD in the place
// of each maximal subpart of a sequence that is not valid: the bytes that
// begin a character and go on as it may go on, up to the byte that it may
// not, as the Unicode Standard recommends and the decoders of the other ports
// do. A conversion of a string to []rune makes one U+FFFD of each byte.
func decodeUTF8(bytes []byte) []rune {
	runes := make([]rune, 0, len(bytes))
	for i := 0; i < len(bytes); {
		if bytes[i] < utf8.RuneSelf {
			runes = append(runes, rune(bytes[i]))
			i++
			continue
		}
		ch, size := utf8.DecodeRune(bytes[i:])
		if ch == utf8.RuneError && size == 1 {
			size = maximalSubpart(bytes[i:])
		}
		runes = append(runes, ch)
		i += size
	}
	return runes
}

// maximalSubpart returns how many bytes of a sequence that is not valid UTF-8
// are the start of a character, at least the first: the lead byte and the
// bytes after it that are in the range the character may go on in.
func maximalSubpart(bytes []byte) int {
	lead := bytes[0]
	follow := 0
	low, high := byte(0x80), byte(0xBF)
	switch {
	case lead >= 0xC2 && lead <= 0xDF:
		follow = 1
	case lead == 0xE0:
		follow, low = 2, 0xA0
	case lead == 0xED:
		follow, high = 2, 0x9F
	case lead >= 0xE1 && lead <= 0xEF:
		follow = 2
	case lead == 0xF0:
		follow, low = 3, 0x90
	case lead == 0xF4:
		follow, high = 3, 0x8F
	case lead >= 0xF1 && lead <= 0xF3:
		follow = 3
	}
	size := 1
	for ; size <= follow && size < len(bytes); size++ {
		if bytes[size] < low || bytes[size] > high {
			break
		}
		low, high = 0x80, 0xBF
	}
	return size
}

// normalize makes each line break of the document a line feed, as XML reads a
// carriage return and the line feed after it, and a carriage return on its
// own, and returns the characters and where the first of them is that is not
// a character of XML, or -1.
func normalize(xml []rune) ([]rune, int) {
	bad := -1
	written := 0
	for i := 0; i < len(xml); i++ {
		ch := xml[i]
		if ch == '\r' {
			ch = '\n'
			if i+1 < len(xml) && xml[i+1] == '\n' {
				i++
			}
		} else if bad == -1 && !isXMLCharacter(ch) {
			bad = written
		}
		xml[written] = ch
		written++
	}
	return xml[:written], bad
}

// isXMLCharacter returns whether the character is one XML has: all but the
// control characters other than the tab and the line breaks, and U+FFFE and
// U+FFFF. The surrogates are not characters either, and are never read as
// characters, since a surrogate without the other half of its pair is read
// as U+FFFD.
func isXMLCharacter(ch rune) bool {
	return ch == '\t' || ch == '\n' || ch == '\r' ||
		(ch >= 0x20 && ch != 0xFFFE && ch != 0xFFFF && (ch < 0xD800 || ch > 0xDFFF) && ch <= 0x10FFFF)
}

// DecodeUTF16 returns the characters of the bytes of UTF-16, big-endian or
// little-endian, without a byte order mark: a code unit that is a surrogate
// without the other one of its pair, or a byte without the one after it, is
// U+FFFD, as the reader of the document reads them.
func DecodeUTF16(bytes []byte, littleEndian bool) []rune {
	units := make([]uint16, 0, len(bytes)/2)
	for i := 0; i+1 < len(bytes); i += 2 {
		if littleEndian {
			units = append(units, uint16(bytes[i])|uint16(bytes[i+1])<<8)
		} else {
			units = append(units, uint16(bytes[i])<<8|uint16(bytes[i+1]))
		}
	}
	runes := utf16.Decode(units)
	if len(bytes)%2 != 0 {
		runes = append(runes, utf8.RuneError)
	}
	return runes
}

// --- The document -----------------------------------------------------------

func (p *parser) document() (*XMLNode, error) {
	if err := p.prolog(); err != nil {
		return nil, err
	}
	root, err := p.element()
	if err != nil {
		return nil, err
	}
	for p.index < len(p.xml) {
		if isWhitespace(p.peek()) {
			p.next()
		} else if p.startsWith("<!--") {
			if err := p.comment(); err != nil {
				return nil, err
			}
		} else if p.startsWith("<?") {
			if err := p.processingInstruction(); err != nil {
				return nil, err
			}
		} else {
			return nil, p.error("There is more than the one element of the document")
		}
	}
	return root, nil
}

// The declaration, the comments, the processing instructions and, when it is
// skipped, the DOCTYPE before the root element.
func (p *parser) prolog() error {
	for p.index < len(p.xml) {
		if isWhitespace(p.peek()) {
			p.next()
		} else if p.index == 0 && p.startsWith("<?xml") && p.index+5 < len(p.xml) && isWhitespace(p.xml[p.index+5]) {
			// The declaration, and not a processing instruction whose name
			// starts with xml, as xml-stylesheet (the review of 9 October 2026)
			if err := p.declaration(); err != nil {
				return err
			}
		} else if p.startsWith("<!--") {
			if err := p.comment(); err != nil {
				return err
			}
		} else if p.startsWith("<?") {
			if err := p.processingInstruction(); err != nil {
				return err
			}
		} else if p.startsWith("<!DOCTYPE") {
			if !p.skipDoctype {
				return p.error("A document type declaration is not read, as its entities are not")
			}
			if err := p.doctype(); err != nil {
				return err
			}
		} else if p.peek() == '<' {
			return nil
		} else {
			return p.error("The document starts with text")
		}
	}
	return p.error("The document has no element")
}

// <!DOCTYPE svg PUBLIC "..." "..." [ ... ]>: skipped to its end, its internal
// subset too, past the > of its declarations, of its comments and of its
// quoted strings. Its entities are not read.
func (p *parser) doctype() error {
	p.skip(len("<!DOCTYPE"))
	depth := 0
	for p.index < len(p.xml) {
		switch {
		case p.startsWith("<!--"):
			end := indexOf(p.xml, "-->", p.index+4)
			if end == -1 {
				return p.error("A comment of the document type declaration does not end")
			}
			p.skip(end + 3 - p.index)
			continue
		case p.startsWith("<?"):
			// A processing instruction, whose text can hold a quote, as
			// <?pi don't ?> (the review of 9 October 2026)
			end := indexOf(p.xml, "?>", p.index+2)
			if end == -1 {
				return p.error("A processing instruction of the document type declaration does not end")
			}
			p.skip(end + 2 - p.index)
			continue
		case p.peek() == '"' || p.peek() == '\'':
			quote := p.next()
			for p.index < len(p.xml) && p.peek() != quote {
				p.next()
			}
			if p.index >= len(p.xml) {
				return p.error("A string of the document type declaration does not end")
			}
		case p.peek() == '[':
			depth++
		case p.peek() == ']':
			depth--
		case p.peek() == '>' && depth <= 0:
			p.next()
			return nil
		}
		p.next()
	}
	return p.error("The document type declaration does not end")
}

// <?xml version="1.0" encoding="UTF-8"?>: the encoding is UTF-8 or the UTF-16
// of the byte order mark.
func (p *parser) declaration() error {
	end := indexOf(p.xml, "?>", p.index)
	if end == -1 {
		return p.error("The declaration does not end")
	}
	declaration := p.xml[p.index:end]
	encoding := indexOf(declaration, "encoding", 0)
	if encoding != -1 {
		quote := -1
		for i := encoding + len("encoding"); i < len(declaration); i++ {
			ch := declaration[i]
			if ch == '"' || ch == '\'' {
				quote = i
				break
			}
		}
		closing := -1
		if quote != -1 {
			closing = indexOf(declaration, string(declaration[quote]), quote+1)
		}
		name := ""
		if closing != -1 {
			name = string(declaration[quote+1 : closing])
		}
		if !strings.EqualFold(name, "UTF-8") && !strings.EqualFold(name, "UTF-16") &&
			!strings.EqualFold(name, "UTF-16BE") && !strings.EqualFold(name, "UTF-16LE") {
			return p.error("The encoding " + name + " is not read, only UTF-8 and UTF-16")
		}
	}
	p.skip(end + 2 - p.index)
	return nil
}

func (p *parser) comment() error {
	end := indexOf(p.xml, "-->", p.index)
	if end == -1 {
		return p.error("The comment does not end")
	}
	p.skip(end + 3 - p.index)
	return nil
}

func (p *parser) processingInstruction() error {
	end := indexOf(p.xml, "?>", p.index)
	if end == -1 {
		return p.error("The processing instruction does not end")
	}
	p.skip(end + 2 - p.index)
	return nil
}

// --- The elements -----------------------------------------------------------

// The root element and the elements in it, which are read one after another
// with a stack and not by recursion, so that a document of any depth is read
// in the stack of this call alone.
func (p *parser) element() (*XMLNode, error) {
	var root *XMLNode
	for {
		if p.index >= len(p.xml) {
			return nil, p.error("The element " + p.open[len(p.open)-1].name + " does not end")
		}
		if p.startsWith("<!--") {
			if err := p.comment(); err != nil {
				return nil, err
			}
		} else if p.startsWith("<![CDATA[") {
			if err := p.characterData(); err != nil {
				return nil, err
			}
		} else if p.startsWith("<?") {
			if err := p.processingInstruction(); err != nil {
				return nil, err
			}
		} else if p.startsWith("</") {
			if err := p.closeTag(); err != nil {
				return nil, err
			}
			if len(p.open) == 0 {
				return root, nil
			}
		} else if p.peek() == '<' {
			if len(p.open) >= MaxDepth {
				return nil, p.error("The elements nest more than " + strconv.Itoa(MaxDepth) + " deep")
			}
			if root != nil && len(p.open) == 0 {
				return nil, p.error("There is more than the one element of the document")
			}
			if p.elements == MaxElements {
				return nil, p.error("The document has more than " + strconv.Itoa(MaxElements) + " elements")
			}
			p.elements++
			node, err := p.openTag()
			if err != nil {
				return nil, err
			}
			if root == nil {
				root = node
			}
			if len(p.open) == 0 {
				return root, nil // An empty root element, <svg/>
			}
		} else {
			if err := p.text(); err != nil {
				return nil, err
			}
		}
	}
}

// <name attribute="value"> or <name/>: the element, added to the one it is in,
// and open until its closing tag unless it closes itself.
func (p *parser) openTag() (*XMLNode, error) {
	p.next() // The <
	name := p.name()
	if name == "" {
		return nil, p.error("A tag has no name")
	}
	var declared map[string]string
	var attributes []XMLAttribute
	var written map[string]bool // Past a few attributes, which are compared one by one
	for first := true; ; first = false {
		spaced := p.index < len(p.xml) && isWhitespace(p.peek())
		p.skipWhitespace()
		ch := p.peek()
		if ch == '>' || ch == '/' {
			break
		}
		// An attribute is after a space: XML has no name="1"other="2".
		if !spaced {
			if first {
				return nil, p.error("The name of the tag " + name + " is not followed by a space")
			}
			return nil, p.error("The attributes of " + name + " are not separated by a space")
		}
		attributeName := p.name()
		if attributeName == "" {
			return nil, p.error("The attributes of " + name + " are not a name and a value")
		}
		p.skipWhitespace()
		if p.peek() != '=' {
			return nil, p.error("The attribute " + attributeName + " of " + name + " has no value")
		}
		p.next()
		p.skipWhitespace()
		value, err := p.attributeValue()
		if err != nil {
			return nil, err
		}
		twice := false
		if written != nil {
			twice = written[attributeName]
		} else {
			for _, attribute := range attributes {
				twice = twice || attribute.Name == attributeName
			}
			if len(attributes) >= 16 {
				written = make(map[string]bool, 2*len(attributes))
				for _, attribute := range attributes {
					written[attribute.Name] = true
				}
			}
		}
		if twice {
			return nil, p.error("The attribute " + attributeName + " of " + name + " is written twice")
		}
		if written != nil {
			written[attributeName] = true
		}
		if attributeName == "xmlns" || strings.HasPrefix(attributeName, "xmlns:") {
			if declared == nil {
				declared = make(map[string]string)
			}
			prefix := ""
			if attributeName != "xmlns" {
				prefix = attributeName[len("xmlns:"):]
				if prefix == "" {
					return nil, p.error("A namespace of " + name + " is declared for an empty prefix")
				}
				// A prefix stands for a namespace, and the default
				// namespace is the only one that may be none.
				if value == "" {
					return nil, p.error("The prefix " + prefix + " of " + name +
						" is declared as no namespace")
				}
			}
			declared[prefix] = value
		}
		attributes = append(attributes, XMLAttribute{Name: attributeName, Value: value})
	}
	// The namespace of the element is the one its prefix stands for in the
	// element itself or in the nearest element it is in that declares it.
	namespace, found := p.namespaceOf(prefixOf(name), declared)
	if !found && !p.skipDoctype {
		return nil, p.error("The prefix " + prefixOf(name) + " of " + name + " is not declared")
	}
	node := newXMLNode(name, namespace)
	for _, attribute := range attributes {
		prefix := prefixOf(attribute.Name)
		if prefix != "" && prefix != "xmlns" && !p.skipDoctype {
			if _, found := p.namespaceOf(prefix, declared); !found {
				return nil, p.error("The prefix " + prefix + " of the attribute " + attribute.Name +
					" of " + name + " is not declared")
			}
		}
		node.addAttribute(attribute.Name, attribute.Value)
	}
	if len(p.open) > 0 {
		p.open[len(p.open)-1].addChild(node)
	}
	empty := p.peek() == '/'
	if empty {
		p.next()
	}
	if p.peek() != '>' {
		return nil, p.error("The tag " + name + " does not end with >")
	}
	p.next()
	if !empty {
		p.open = append(p.open, node)
		p.scopes = append(p.scopes, declared)
	}
	return node, nil
}

// prefixOf returns the prefix of the name, the part before its colon, or an
// empty string when it has none.
func prefixOf(name string) string {
	if colon := strings.IndexByte(name, ':'); colon != -1 {
		return name[:colon]
	}
	return ""
}

// namespaceOf returns the namespace the prefix stands for, in the namespaces
// the element declares and then in the ones of the elements it is in, from the
// innermost out, and whether it stands for one. No prefix is the default
// namespace, which is none where no element declares one, and xml is the
// namespace of XML itself.
func (p *parser) namespaceOf(prefix string, declared map[string]string) (string, bool) {
	if namespace, found := declared[prefix]; found {
		return namespace, true
	}
	for i := len(p.scopes) - 1; i >= 0; i-- {
		if namespace, found := p.scopes[i][prefix]; found {
			return namespace, true
		}
	}
	switch prefix {
	case "":
		return "", true
	case "xml":
		return namespaceXML, true
	}
	return "", false
}

// </name>, which ends the element that is open.
func (p *parser) closeTag() error {
	p.skip(2) // The </
	name := p.name()
	p.skipWhitespace()
	if p.peek() != '>' {
		return p.error("The closing tag of " + name + " does not end with >")
	}
	p.next()
	if len(p.open) == 0 {
		return p.error("The closing tag of " + name + " closes no element")
	}
	node := p.open[len(p.open)-1]
	p.open = p.open[:len(p.open)-1]
	p.scopes = p.scopes[:len(p.scopes)-1]
	if node.name != name {
		return p.error("The element " + node.name + " is closed by the tag of " + name)
	}
	return nil
}

// <![CDATA[ the text as it is ]]>
func (p *parser) characterData() error {
	end := indexOf(p.xml, "]]>", p.index)
	if end == -1 {
		return p.error("The character data does not end")
	}
	if len(p.open) == 0 {
		return p.error("There is text outside the element of the document")
	}
	p.open[len(p.open)-1].addText(string(p.xml[p.index+len("<![CDATA[") : end]))
	p.skip(end + 3 - p.index)
	return nil
}

// The text up to the next <, with its entities read.
func (p *parser) text() error {
	var buf strings.Builder
	for p.index < len(p.xml) && p.peek() != '<' {
		ch := p.next()
		if ch == '&' {
			characters, err := p.entity()
			if err != nil {
				return err
			}
			buf.WriteString(characters)
		} else {
			buf.WriteRune(ch)
		}
	}
	if len(p.open) == 0 {
		if Trim(buf.String()) == "" {
			return nil
		}
		return p.error("There is text outside the element of the document")
	}
	p.open[len(p.open)-1].addText(buf.String())
	return nil
}

// The &lt;, &gt;, &amp;, &quot; and &apos; of XML, and the numeric ones, such
// as &#160; and &#xA0;. The & is read. The digits are the ones of ASCII, as
// XML writes them; the Java port takes the digits of any script as well,
// because its Integer.parseInt does.
func (p *parser) entity() (string, error) {
	end := indexOf(p.xml, ";", p.index)
	if end == -1 || utf16Len(p.xml[p.index:end]) > 16 {
		return "", p.error("An & that is not an entity, such as &amp;")
	}
	name := string(p.xml[p.index:end])
	p.skip(end + 1 - p.index)
	switch name {
	case "lt":
		return "<", nil
	case "gt":
		return ">", nil
	case "amp":
		return "&", nil
	case "quot":
		return "\"", nil
	case "apos":
		return "'", nil
	}
	if strings.HasPrefix(name, "#") {
		digits, base := name[1:], 10
		if len(digits) > 0 && (digits[0] == 'x' || digits[0] == 'X') {
			digits, base = digits[1:], 16
		}
		// Only the digits of XML: a sign is not one of them, and neither are
		// the digits of another script, which ParseInt does not take but
		// Integer.parseInt in the Java port did.
		code, err := strconv.ParseInt(digits, base, 32)
		if err != nil || !isNumber(digits, base == 16) {
			return "", p.error("The character &" + name + "; is not a number")
		}
		if !isXMLCharacter(rune(code)) {
			return "", p.error("The character &" + name + "; is not a character of XML")
		}
		return string(rune(code)), nil
	}
	return "", p.error("The entity &" + name + "; is not one of XML, and this reads no others")
}

// A number of a numeric entity: the digits 0 to 9, and a to f as well when the
// entity is hexadecimal, in ASCII and in no other script.
func isNumber(digits string, hex bool) bool {
	if digits == "" {
		return false
	}
	for i := 0; i < len(digits); i++ {
		ch := digits[i]
		if ch >= '0' && ch <= '9' {
			continue
		}
		if hex && ((ch >= 'a' && ch <= 'f') || (ch >= 'A' && ch <= 'F')) {
			continue
		}
		return false
	}
	return true
}

// --- The characters ---------------------------------------------------------

// A name of an element or of an attribute: what stands before the whitespace,
// the =, the / or the > after it.
func (p *parser) name() string {
	start := p.index
	for p.index < len(p.xml) {
		ch := p.peek()
		if isWhitespace(ch) || ch == '=' || ch == '/' || ch == '>' || ch == '<' {
			break
		}
		p.next()
	}
	name := string(p.xml[start:p.index])
	if kept, ok := p.names[name]; ok {
		return kept
	}
	if p.names == nil {
		p.names = make(map[string]string)
	}
	if len(p.names) < 4096 {
		p.names[name] = name
	}
	return name
}

// "value" or 'value', with its entities read.
func (p *parser) attributeValue() (string, error) {
	quote := p.peek()
	if quote != '"' && quote != '\'' {
		return "", p.error("The value of an attribute is not in quotes")
	}
	p.next()
	var buf strings.Builder
	for {
		if p.index >= len(p.xml) {
			return "", p.error("The value of an attribute does not end")
		}
		ch := p.next()
		if ch == quote {
			return buf.String(), nil
		} else if ch == '<' {
			return "", p.error("The value of an attribute holds a <")
		} else if ch == '&' {
			characters, err := p.entity()
			if err != nil {
				return "", err
			}
			buf.WriteString(characters)
		} else if ch == '\t' || ch == '\n' {
			// XML makes each tab and line break of a value a space, and
			// keeps the ones written as entities, such as &#10;.
			buf.WriteByte(' ')
		} else {
			buf.WriteRune(ch)
		}
	}
}

func (p *parser) startsWith(characters string) bool {
	return startsWith(p.xml, p.index, characters)
}

func (p *parser) peek() rune {
	if p.index < len(p.xml) {
		return p.xml[p.index]
	}
	return 0
}

func (p *parser) next() rune {
	ch := p.xml[p.index]
	p.index++
	if ch == '\n' {
		p.line++
		p.column = 1
	} else if ch > 0xFFFF {
		// The columns are counted in the code units of UTF-16, as the Java
		// port counts them, in which a character above U+FFFF is a pair.
		p.column += 2
	} else {
		p.column++
	}
	return ch
}

func (p *parser) skip(count int) {
	for i := 0; i < count && p.index < len(p.xml); i++ {
		p.next()
	}
}

func (p *parser) skipWhitespace() {
	for p.index < len(p.xml) && isWhitespace(p.peek()) {
		p.next()
	}
}

func isWhitespace(ch rune) bool {
	return ch == ' ' || ch == '\t' || ch == '\n' || ch == '\r'
}

// startsWith returns whether the characters stand at the index.
func startsWith(runes []rune, index int, characters string) bool {
	for _, ch := range characters {
		if index >= len(runes) || runes[index] != ch {
			return false
		}
		index++
	}
	return true
}

// indexOf returns where the characters stand at or after the index, or -1.
func indexOf(runes []rune, characters string, from int) int {
	for i := from; i < len(runes); i++ {
		if startsWith(runes, i, characters) {
			return i
		}
	}
	return -1
}

// utf16Len returns how many code units of UTF-16 the characters are: the Java
// port reads the document as such code units, and counts in them the length of
// what stands between an & and its ;.
func utf16Len(runes []rune) int {
	count := 0
	for _, ch := range runes {
		if ch > 0xFFFF {
			count += 2
		} else {
			count++
		}
	}
	return count
}

func (p *parser) error(message string) error {
	return fmt.Errorf("%s, at line %d column %d", message, p.line, p.column)
}
