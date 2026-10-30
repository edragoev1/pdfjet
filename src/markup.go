// markup.go
//
// Copyright (c) 2026 PDFjet Software
// Licensed under the MIT License. See LICENSE file in the project root.

package pdfjet

import (
	"strings"

	"github.com/edragoev1/pdfjet/v9/src/color"
)

// Markup makes paragraphs of text with the inline markup of Markdown:
// **bold**, *italic*, ***bold italic***, `code` and [links](https://pdfjet.com),
// in the fonts given for each, and as GitHub's Markdown has them, __bold__ and
// _italic_, ~~strikethrough~~ of two ~, one being text here, as in ~5, and
// links that are their address,
// <https://pdfjet.com> or https://pdfjet.com alone. An _ emphasizes only at
// the edge of a word, so that snake_case is text; an address alone ends
// before the punctuation at its end, as the period of a sentence, and before a
// ) that closes no ( of its own. A backslash before a punctuation character, as
// in \*, makes it plain text, and so is a mark that has no match, such as the
// * of 2 * 3. Emphasis can be inside a link, and neither inside code. A word
// keeps its punctuation next to it in another style, as in **bold**, with no
// space between them: the paragraph joins its text lines with
// Paragraph.AddJoined.
//
// A line that ends in two spaces or a backslash, and a <br>, are hard line
// breaks, which TextFrame draws as the end of a row, and the spaces before
// the line break are not in the text; two spaces and a line break at the end
// of the text are not, and are dropped, and a backslash at the end of the text
// is a backslash, as in CommonMark. Any other line break is a space. The
// spaces and tabs at the start of the line after a line break, hard or not,
// are not in the text, as in CommonMark. The entities &amp;, &lt;, &gt;, &quot;, &apos;, &nbsp;, &copy;,
// &reg;, &trade;, &mdash;, &ndash;, &hellip;, the quotes and a few more, and
// the numbers &#8212; and &#x2014;, are the characters they name.
//
// Headings, lists, block quotes and the rest of Markdown's blocks are not
// read: Paragraphs splits a text into paragraphs at its empty lines. The markup is read in time linear in the length of the
// text, so it can come from anyone. Please see Example_18.
//
// The text is read as characters, as Java reads it, with the whitespace of
// Java's Character.isWhitespace. A byte of the text that is not valid UTF-8
// is read as the character U+FFFD, one for each such byte, as a conversion of
// the string to []rune reads it.
type Markup struct {
	regular       *Font
	bold          *Font
	italic        *Font
	boldItalic    *Font
	code          *Font
	linkColor     int32
	linkUnderline bool
}

const (
	markupBold   = 1
	markupItalic = 2
	markupCode   = 4
	markupStrike = 8
	markupBreak  = 16 // A hard line break
)

// markupEntities are the named entities that are read, as GitHub's Markdown
// reads them: the markup's own characters, the spaces and the punctuation of
// typeset text.
var markupEntities = map[string]rune{
	"amp": '&', "lt": '<', "gt": '>', "quot": '"', "apos": '\'',
	"nbsp": 0xA0, "copy": 0xA9, "reg": 0xAE, "trade": 0x2122,
	"mdash": 0x2014, "ndash": 0x2013, "hellip": 0x2026,
	"lsquo": 0x2018, "rsquo": 0x2019, "ldquo": 0x201C, "rdquo": 0x201D,
	"laquo": 0xAB, "raquo": 0xBB, "bull": 0x2022, "middot": 0xB7,
	"deg": 0xB0, "times": 0xD7, "divide": 0xF7, "plusmn": 0xB1,
	"sect": 0xA7, "para": 0xB6, "euro": 0x20AC, "pound": 0xA3,
	"yen": 0xA5, "cent": 0xA2, "shy": 0xAD,
}

// NewMarkup creates the markup of paragraphs in the fonts, each at its size.
//   - regular: the font of the text.
//   - bold: the font of **bold** text.
//   - italic: the font of *italic* text.
//   - boldItalic: the font of ***bold italic*** text.
//   - code: the font of `code`, usually a monospaced font.
func NewMarkup(regular, bold, italic, boldItalic, code *Font) *Markup {
	return &Markup{
		regular:       regular,
		bold:          bold,
		italic:        italic,
		boldItalic:    boldItalic,
		code:          code,
		linkColor:     color.Blue,
		linkUnderline: true,
	}
}

// SetLinkColor sets the color of the text of links, as a 0xRRGGBB value. The
// default is color.Blue.
// color.Transparent leaves it unchanged.
func (markup *Markup) SetLinkColor(linkColor int32) *Markup {
	if linkColor == color.Transparent {
		return markup
	}
	markup.linkColor = linkColor
	return markup
}

// SetLinkUnderline sets whether the text of links is underlined, as it is by
// default.
func (markup *Markup) SetLinkUnderline(underline bool) *Markup {
	markup.linkUnderline = underline
	return markup
}

// Paragraphs returns the paragraphs of the text, which empty lines separate:
// none for a text with no words.
func (markup *Markup) Paragraphs(text string) []*Paragraph {
	paragraphs := make([]*Paragraph, 0)
	var buf strings.Builder
	for _, line := range strings.Split(text, "\n") {
		line = strings.TrimSuffix(line, "\r")
		if trimSpace(line) == "" {
			paragraphs = markup.addParagraph(paragraphs, &buf)
		} else {
			buf.WriteString(line)
			buf.WriteByte('\n')
		}
	}
	return markup.addParagraph(paragraphs, &buf)
}

func (markup *Markup) addParagraph(paragraphs []*Paragraph, buf *strings.Builder) []*Paragraph {
	if buf.Len() > 0 {
		// Without the line break after the last line, which made a backslash
		// at the end of the paragraph a hard line break, and dropped it, and
		// without the whitespace at its end, as CommonMark strips the final
		// spaces of a paragraph: "0  \n" is "0"
		text := buf.String()
		paragraph := markup.Paragraph(strings.TrimRightFunc(text[:len(text)-1], isJavaWhitespace))
		if len(paragraph.lines) > 0 {
			paragraphs = append(paragraphs, paragraph)
		}
		buf.Reset()
	}
	return paragraphs
}

// Paragraph returns the text as one paragraph: a text line for each run of
// text in one style, each joined to the text before it.
func (markup *Markup) Paragraph(text string) *Paragraph {
	parser := newMarkupParser([]rune(text))
	if end := parser.trailingBreak(); end < len(parser.text) {
		// A hard line break at the end of the text is no break, as in
		// CommonMark, and the whitespace at the end is dropped: "0  \n" is
		// the text line "0"
		parser = newMarkupParser(parser.text[:end])
	}
	nodes := make([]*markupNode, 0)
	nodes = parser.parse(0, len(parser.text), "", nodes)
	paragraph := NewParagraph()
	// The runs of one style and link, in order.
	var buf strings.Builder
	flags := 0
	link := ""
	joined := false
	for _, node := range nodes {
		if node.flags&markupBreak != 0 {
			// A hard line break: the row ends after the text before it, and
			// the text after it is not joined to it
			if buf.Len() > 0 {
				markup.addRun(paragraph, buf.String(), flags, link, joined)
				buf.Reset()
			}
			if len(paragraph.lines) > 0 {
				paragraph.lines[len(paragraph.lines)-1].lineBreaks++
			}
			joined = false
			continue
		}
		if buf.Len() > 0 && (node.flags != flags || node.link != link) {
			joined = markup.addRun(paragraph, buf.String(), flags, link, joined)
			buf.Reset()
		}
		flags = node.flags
		link = node.link
		buf.WriteString(node.text)
	}
	if buf.Len() > 0 {
		markup.addRun(paragraph, buf.String(), flags, link, joined)
	}
	return paragraph
}

// addRun adds a run of text in one style. A run of spaces only is not added:
// it puts a space before the next run, as between two words. Returns whether
// the next run is joined to this one.
func (markup *Markup) addRun(paragraph *Paragraph, text string, flags int, link string, joined bool) bool {
	if trimSpace(text) == "" {
		return false
	}
	font := markup.regular
	switch {
	case flags&markupCode != 0:
		font = markup.code
	case flags&markupBold != 0 && flags&markupItalic != 0:
		font = markup.boldItalic
	case flags&markupBold != 0:
		font = markup.bold
	case flags&markupItalic != 0:
		font = markup.italic
	}
	textLine := NewTextLine(font, text)
	if flags&markupStrike != 0 {
		textLine.SetStrikeout(true)
	}
	if link != "" {
		textLine.SetURIAction(link)
		textLine.SetTextColor(markup.linkColor)
		// The underline in the color of the link: it was black under the
		// blue (review of 15 October 2026)
		textLine.SetDecorationColor(markup.linkColor)
		textLine.SetUnderline(markup.linkUnderline)
	}
	if joined {
		paragraph.AddJoined(textLine)
	} else {
		paragraph.Add(textLine)
	}
	return true
}

// markupNode is text in one style, or a run of *, of _ or of ~~ that may open
// or close emphasis or a strikethrough, which is text too as far as it does
// not. The link is "" for text that is not in a link: the URL of a link is
// never empty.
type markupNode struct {
	text  string
	flags int
	link  string
	// Whether the node is a run whose emphasis is not matched yet, which is
	// when Java's text is null.
	run bool
	// Of a run: its character, *, _ or ~, where it is among the nodes, how
	// many of its characters are not matched yet, whether they can open and
	// close emphasis, and the emphasis the run opens and closes, counted in
	// levels of bold, italic and strikethrough.
	char     rune
	index    int
	count    int
	canOpen  bool
	canClose bool
	opens    *[3]int
	closes   *[3]int
}

type markupParser struct {
	text []rune
	// Where each ] that closes a [ is, by the index of the [.
	closingBracket []int
	// The index of the next ) and of the next space at or after each index.
	nextParenthesis []int
	nextSpace       []int
	// The runs of backticks, by their length: where each starts, in order,
	// and the first one of each length that can still close a code span.
	backtickRuns   map[int][]int
	backtickCursor map[int]int
}

func newMarkupParser(text []rune) *markupParser {
	parser := &markupParser{
		text:           text,
		backtickRuns:   make(map[int][]int),
		backtickCursor: make(map[int]int),
	}
	n := len(text)
	parser.closingBracket = make([]int, n)
	for i := range parser.closingBracket {
		parser.closingBracket[i] = -1
	}
	open := make([]int, 0)
	for i := 0; i < n; i++ {
		ch := text[i]
		if ch == '\\' && i+1 < n {
			i++
		} else if ch == '[' {
			open = append(open, i)
		} else if ch == ']' && len(open) > 0 {
			parser.closingBracket[open[len(open)-1]] = i
			open = open[:len(open)-1]
		}
	}
	parser.nextParenthesis = make([]int, n+1)
	parser.nextSpace = make([]int, n+1)
	parser.nextParenthesis[n] = n
	parser.nextSpace[n] = n
	for i := n - 1; i >= 0; i-- {
		ch := text[i]
		if ch == ')' {
			parser.nextParenthesis[i] = i
		} else {
			parser.nextParenthesis[i] = parser.nextParenthesis[i+1]
		}
		if isJavaWhitespace(ch) {
			parser.nextSpace[i] = i
		} else {
			parser.nextSpace[i] = parser.nextSpace[i+1]
		}
	}
	for i := 0; i < n; {
		if text[i] == '\\' && i+1 < n && text[i+1] != '`' {
			i += 2
		} else if text[i] == '`' {
			// A backtick after a backslash is text where it would open
			// code, as parse reads it, but closes code, in which a
			// backslash is text, as in GitHub's Markdown: `foo\` was
			// not closed
			start := i
			for i < n && text[i] == '`' {
				i++
			}
			length := i - start
			if _, ok := parser.backtickRuns[length]; !ok {
				parser.backtickCursor[length] = 0
			}
			parser.backtickRuns[length] = append(parser.backtickRuns[length], start)
		} else {
			i++
		}
	}
	return parser
}

// parse reads the text from start to end into the nodes, all in the link when
// it is not "", with the emphasis of the runs of * matched, and returns them.
func (parser *markupParser) parse(start, end int, link string, nodes []*markupNode) []*markupNode {
	text := parser.text
	first := len(nodes)
	buf := make([]rune, 0)
	i := start
	for i < end {
		ch := text[i]
		if breakEnd := parser.breakEnd(i, end); breakEnd > i {
			if ch == '\\' && parser.onlySpaceFrom(breakEnd) {
				// A backslash at the end of the text is text, and no hard
				// line break, as in CommonMark: "0\\\n" is the text line
				// "0\\"
				buf = append(buf, ch)
				i = end
				continue
			}
			if ch == '\r' || ch == '\n' {
				// The spaces before a hard line break are not in the text,
				// as in CommonMark: "one  \ntwo" is "one", a break and "two"
				for k := i; k > start && isJavaWhitespace(text[k-1]) &&
					len(buf) > 0 && buf[len(buf)-1] == ' '; k-- {
					buf = buf[:len(buf)-1]
				}
			}
			buf, nodes = markupFlush(buf, link, nodes)
			nodes = append(nodes, &markupNode{flags: markupBreak, link: link})
			i = breakEnd
			if text[i-1] == '\n' {
				i = parser.lineStart(i, end)
			}
		} else if code, entityEnd := parser.entity(i, end); entityEnd > i {
			buf = append(buf, code)
			i = entityEnd
		} else if ch == '\\' && i+1 < end && isASCIIPunctuation(text[i+1]) {
			buf = append(buf, text[i+1])
			i += 2
		} else if ch == '`' {
			length := 1
			for i+length < end && text[i+length] == '`' {
				length++
			}
			close := parser.closingBackticks(length, i+length, end)
			if close == -1 {
				buf = append(buf, text[i:i+length]...)
			} else {
				buf, nodes = markupFlush(buf, link, nodes)
				nodes = append(nodes, &markupNode{
					text: markupCodeText(text[i+length : close]), flags: markupCode, link: link})
			}
			if close == -1 {
				i += length
			} else {
				i = close + length
			}
		} else if ch == '*' || ch == '_' || ch == '~' {
			length := 1
			for i+length < end && text[i+length] == ch {
				length++
			}
			if ch == '~' && length != 2 {
				// A strikethrough is two ~, and one or three are text, as in ~5
				buf = append(buf, text[i:i+length]...)
				i += length
				continue
			}
			buf, nodes = markupFlush(buf, link, nodes)
			run := &markupNode{link: link, run: true, char: ch, index: len(nodes), count: length}
			// Before the run, or after it, a space, or the start or the end
			left := i+length < end && !isJavaWhitespace(text[i+length])
			right := i > start && !isJavaWhitespace(text[i-1])
			run.canOpen = left
			run.canClose = right
			if ch == '_' {
				// Inside a word, as in snake_case, an _ is text
				run.canOpen = left && (!right || isASCIIPunctuation(text[i-1]))
				run.canClose = right && (!left || isASCIIPunctuation(text[i+length]))
			}
			nodes = append(nodes, run)
			i += length
		} else if urlEnd := parser.autolinkEnd(i, start, end, link); urlEnd > i {
			buf, nodes = markupFlush(buf, link, nodes)
			if ch == '<' {
				url := string(text[i+1 : urlEnd-1])
				nodes = append(nodes, &markupNode{text: url, link: url})
			} else {
				url := string(text[i:urlEnd])
				nodes = append(nodes, &markupNode{text: url, link: url})
			}
			i = urlEnd
		} else if ch == '[' && link == "" && parser.isLink(i, end) {
			close := parser.closingBracket[i]
			urlEnd := parser.nextParenthesis[close+2]
			buf, nodes = markupFlush(buf, link, nodes)
			nodes = parser.parse(i+1, close, parser.decoded(close+2, urlEnd), nodes)
			i = urlEnd + 1
		} else {
			if isJavaWhitespace(ch) {
				buf = append(buf, ' ')
			} else {
				buf = append(buf, ch)
			}
			i++
			if ch == '\r' && i < end && text[i] == '\n' {
				// A \r\n is one line ending, and one space
				i++
			}
			if text[i-1] == '\n' {
				i = parser.lineStart(i, end)
			}
		}
	}
	_, nodes = markupFlush(buf, link, nodes)
	markupMatchEmphasis(nodes, first)
	return nodes
}

// trailingBreak returns where the whitespace at the end of the text starts
// when a hard line break is in it, two spaces before a line break, or the
// length of the text when none is.
func (parser *markupParser) trailingBreak() int {
	n := len(parser.text)
	start := n
	for start > 0 && isJavaWhitespace(parser.text[start-1]) {
		start--
	}
	for i := start; i < n; i++ {
		if parser.breakEnd(i, n) > i {
			return start
		}
	}
	return n
}

// onlySpaceFrom returns whether the text from the index to its end is
// whitespace, or nothing.
func (parser *markupParser) onlySpaceFrom(i int) bool {
	for ; i < len(parser.text); i++ {
		if !isJavaWhitespace(parser.text[i]) {
			return false
		}
	}
	return true
}

// lineStart returns where the text of the line at the index starts before
// the end: after its spaces and tabs, which are not in the text after a line
// break, hard or not, as in CommonMark: "one  \n  two" is "one", a break and
// "two". A space written &#32; is text.
func (parser *markupParser) lineStart(i, end int) int {
	for i < end && (parser.text[i] == ' ' || parser.text[i] == '\t') {
		i++
	}
	return i
}

// breakEnd returns where a hard line break at the index ends, or i when there
// is none: a line break after two spaces, a backslash and a line break, or a
// <br>, <br/> or <br /> in capitals or not.
func (parser *markupParser) breakEnd(i, end int) int {
	text := parser.text
	ch := text[i]
	// The line break of a text of \r\n line ends too
	newline := i
	if ch == '\r' && i+1 < end && text[i+1] == '\n' {
		newline = i + 1
	}
	if text[newline] == '\n' && i >= 2 && text[i-1] == ' ' && text[i-2] == ' ' {
		return newline + 1
	}
	if ch == '\\' && i+1 < end && text[i+1] == '\n' {
		return i + 2
	}
	if ch == '\\' && i+2 < end && text[i+1] == '\r' && text[i+2] == '\n' {
		return i + 3
	}
	if ch == '<' && i+3 < end && (text[i+1] == 'b' || text[i+1] == 'B') &&
		(text[i+2] == 'r' || text[i+2] == 'R') {
		j := i + 3
		for j < end && text[j] == ' ' {
			j++
		}
		if j < end && text[j] == '/' {
			j++
		}
		if j < end && text[j] == '>' {
			return j + 1
		}
	}
	return i
}

// entity returns the character of the entity at the index and where the
// entity ends, or i when there is none: a name of markupEntities, or a number
// of up to seven digits, or of six in hexadecimal after an x, that is a
// character, between & and ;. The ; is looked for in the next 10 characters,
// as Java looks for it in its UTF-16 text: the names and the numbers are
// ASCII, so that the two find the same entities.
func (parser *markupParser) entity(i, end int) (rune, int) {
	text := parser.text
	if text[i] != '&' {
		return 0, i
	}
	semicolon := -1
	for j := i + 1; j < end && j <= i+10; j++ {
		if text[j] == ';' {
			semicolon = j
			break
		}
	}
	if semicolon == -1 {
		return 0, i
	}
	name := text[i+1 : semicolon]
	if code, ok := markupEntities[string(name)]; ok {
		return code, semicolon + 1
	}
	if len(name) < 2 || name[0] != '#' {
		return 0, i
	}
	hex := name[1] == 'x' || name[1] == 'X'
	digits := name[1:]
	maxDigits := 7
	base := 10
	if hex {
		digits = name[2:]
		maxDigits = 6
		base = 16
	}
	if len(digits) == 0 || len(digits) > maxDigits {
		return 0, i
	}
	value := 0
	for _, ch := range digits {
		digit := markupASCIIDigit(ch, hex)
		if digit < 0 {
			return 0, i
		}
		value = value*base + digit
	}
	if value == 0 || value > 0x10FFFF || (value >= 0xD800 && value <= 0xDFFF) {
		return 0, i
	}
	if value < 0x20 || (value >= 0x7F && value <= 0x9F) {
		value = ' ' // A control character is a space: &#10; broke the line of a table cell
	}
	return rune(value), semicolon + 1
}

// decoded returns the text from start to end with its entities the
// characters they are: the address of a link, where &amp; was the five
// characters.
func (parser *markupParser) decoded(start, end int) string {
	var buf strings.Builder
	i := start
	for i < end {
		if code, next := parser.entity(i, end); next > i {
			buf.WriteRune(code)
			i = next
		} else {
			buf.WriteRune(parser.text[i])
			i++
		}
	}
	return buf.String()
}

// markupASCIIDigit returns the value of an ASCII digit, of 0 to 9, or of a to
// f and A to F in hexadecimal, or -1.
func markupASCIIDigit(ch rune, hex bool) int {
	if ch >= '0' && ch <= '9' {
		return int(ch - '0')
	}
	if hex && ch >= 'a' && ch <= 'f' {
		return int(ch-'a') + 10
	}
	if hex && ch >= 'A' && ch <= 'F' {
		return int(ch-'A') + 10
	}
	return -1
}

// isLink returns true when the [ at the index starts a link: its ] is before
// the end, a ( follows the ] at once, and a ) ends the URL before any space:
// [text](url).
func (parser *markupParser) isLink(i, end int) bool {
	close := parser.closingBracket[i]
	if close == -1 || close+1 >= end || parser.text[close+1] != '(' {
		return false
	}
	urlEnd := parser.nextParenthesis[close+2]
	return urlEnd < end && urlEnd > close+2 && parser.nextSpace[close+2] > urlEnd
}

// autolinkEnd returns where a link that is its address ends, after its >
// when it is in < and >, or i when there is none at i, or the text is in a
// link already: an address of http:// or https:// and a host, in < and > with
// no space in it, or alone, after a space, the start, a (, or a *, _ or ~ of
// emphasis. An address alone ends at a space or a <, and then before the
// punctuation at its end, ? ! . , : * _ and ~, and before each ) at its end
// that closes no ( in it, as GitHub's Markdown reads it.
func (parser *markupParser) autolinkEnd(i, start, end int, link string) int {
	text := parser.text
	if link != "" || (text[i] != '<' && text[i] != 'h' && text[i] != 'H') {
		return i
	}
	from := i
	if text[i] == '<' {
		from = i + 1
	} else if i > start && !isJavaWhitespace(text[i-1]) && !strings.ContainsRune("(*_~", text[i-1]) {
		return i
	}
	host := markupSchemeEnd(text, from, end)
	if host == -1 || host >= end || !markupIsHostChar(text[host]) {
		return i
	}
	j := host
	for j < end && !isJavaWhitespace(text[j]) && text[j] != '<' && text[j] != '>' {
		j++
	}
	if text[i] == '<' {
		if j < end && text[j] == '>' {
			return j + 1
		}
		return i
	}
	opening := markupCount(text[from:j], '(')
	closing := markupCount(text[from:j], ')')
	for j > host {
		last := text[j-1]
		if strings.ContainsRune("?!.,:*_~", last) {
			j--
		} else if last == ')' && opening < closing {
			j--
			closing--
		} else {
			break
		}
	}
	if j == host {
		return i
	}
	return j
}

// markupSchemeEnd returns where the host starts after http:// or https:// at
// the index, in capitals or not, or -1. Only the ASCII letters are folded, so
// that the four ports agree.
func markupSchemeEnd(text []rune, i, end int) int {
	for _, scheme := range []string{"https://", "http://"} {
		n := len(scheme)
		if i+n > end {
			continue
		}
		k := 0
		for k < n {
			ch := text[i+k]
			if ch >= 'A' && ch <= 'Z' {
				ch += 'a' - 'A'
			}
			if ch != rune(scheme[k]) {
				break
			}
			k++
		}
		if k == n {
			return i + n
		}
	}
	return -1
}

// markupIsHostChar returns true for a letter, a digit or a character that is
// not ASCII, as the first character of a host is.
func markupIsHostChar(ch rune) bool {
	return (ch >= 'a' && ch <= 'z') || (ch >= 'A' && ch <= 'Z') || (ch >= '0' && ch <= '9') || ch > 0x7F
}

func markupCount(text []rune, ch rune) int {
	n := 0
	for _, c := range text {
		if c == ch {
			n++
		}
	}
	return n
}

// closingBackticks returns where the next run of backticks of the length
// starts, after from and before end, or -1.
func (parser *markupParser) closingBackticks(length, from, end int) int {
	runs, ok := parser.backtickRuns[length]
	if !ok {
		return -1
	}
	cursor := parser.backtickCursor[length]
	for cursor < len(runs) && runs[cursor] < from {
		cursor++
	}
	parser.backtickCursor[length] = cursor
	if cursor < len(runs) && runs[cursor]+length <= end {
		return runs[cursor]
	}
	return -1
}

// markupCodeText returns the text of a code span: a line break is a space,
// and one space at each end is taken away when both ends have one, as in
//
//	`` `a` ``
func markupCodeText(code []rune) string {
	code = append([]rune(nil), code...)
	for i, ch := range code {
		if ch == '\n' || ch == '\r' {
			code[i] = ' '
		}
	}
	if len(code) >= 2 && code[0] == ' ' && code[len(code)-1] == ' ' &&
		trimSpace(string(code)) != "" {
		code = code[1 : len(code)-1]
	}
	return string(code)
}

func markupFlush(buf []rune, link string, nodes []*markupNode) ([]rune, []*markupNode) {
	if len(buf) > 0 {
		nodes = append(nodes, &markupNode{text: string(buf), link: link})
		buf = buf[:0]
	}
	return buf, nodes
}

// markupMatchEmphasis matches the runs from the index on, each closer with
// the nearest opener of its character before it: two * or _ of each make bold
// and one italic, and ~~ a strikethrough. The runs of the other characters
// between a closer and its opener can no longer match, so that emphasis and
// strikethrough nest and do not cross. Then gives every node the emphasis it
// is in, and turns what is left of each run into text.
func markupMatchEmphasis(nodes []*markupNode, first int) {
	// The runs that can still open, of *, of _ and of ~
	var stars, underscores, tildes []*markupNode
	for i := first; i < len(nodes); i++ {
		node := nodes[i]
		if !node.run {
			continue
		}
		if node.char == '~' {
			if node.canClose && len(tildes) > 0 {
				opener := tildes[len(tildes)-1]
				tildes = tildes[:len(tildes)-1]
				markupDropAfter(&stars, opener.index)
				markupDropAfter(&underscores, opener.index)
				opener.opens = markupAdd(opener.opens, markupStrike)
				node.closes = markupAdd(node.closes, markupStrike)
				opener.count = 0
				node.count = 0
			}
			if node.count > 0 && node.canOpen {
				tildes = append(tildes, node)
			}
			continue
		}
		own, other := &stars, &underscores
		if node.char == '_' {
			own, other = &underscores, &stars
		}
		if node.canClose {
			for node.count > 0 && len(*own) > 0 {
				opener := (*own)[len(*own)-1]
				markupDropAfter(other, opener.index)
				markupDropAfter(&tildes, opener.index)
				use := 1
				if node.count >= 2 && opener.count >= 2 {
					use = 2
				}
				flag := markupItalic
				if use == 2 {
					flag = markupBold
				}
				opener.opens = markupAdd(opener.opens, flag)
				node.closes = markupAdd(node.closes, flag)
				opener.count -= use
				node.count -= use
				if opener.count == 0 {
					*own = (*own)[:len(*own)-1]
				}
			}
		}
		if node.count > 0 && node.canOpen {
			*own = append(*own, node)
		}
	}
	// The emphasis in effect, in levels, since bold can be in bold.
	var levels [3]int
	for i := first; i < len(nodes); i++ {
		node := nodes[i]
		if node.run {
			for k := range levels {
				levels[k] -= markupLevel(node.closes, k)
			}
			node.text = strings.Repeat(string(node.char), node.count)
			node.run = false
			node.flags = markupEmphasis(levels)
			for k := range levels {
				levels[k] += markupLevel(node.opens, k)
			}
		} else {
			node.flags |= markupEmphasis(levels)
		}
	}
}

// markupDropAfter drops the openers after the index from the stack.
func markupDropAfter(openers *[]*markupNode, index int) {
	for len(*openers) > 0 && (*openers)[len(*openers)-1].index > index {
		*openers = (*openers)[:len(*openers)-1]
	}
}

// markupEmphasis returns the flags of the levels of bold, italic and
// strikethrough in effect.
func markupEmphasis(levels [3]int) int {
	flags := 0
	if levels[0] > 0 {
		flags |= markupBold
	}
	if levels[1] > 0 {
		flags |= markupItalic
	}
	if levels[2] > 0 {
		flags |= markupStrike
	}
	return flags
}

// markupAdd returns the levels of bold, italic and strikethrough with one
// more of the flag.
func markupAdd(levels *[3]int, flag int) *[3]int {
	if levels == nil {
		levels = new([3]int)
	}
	switch flag {
	case markupBold:
		levels[0]++
	case markupItalic:
		levels[1]++
	default:
		levels[2]++
	}
	return levels
}

// markupLevel returns the level of bold (0), italic (1) or strikethrough (2).
func markupLevel(levels *[3]int, k int) int {
	if levels == nil {
		return 0
	}
	return levels[k]
}

func isASCIIPunctuation(ch rune) bool {
	return (ch >= '!' && ch <= '/') || (ch >= ':' && ch <= '@') ||
		(ch >= '[' && ch <= '`') || (ch >= '{' && ch <= '~')
}
