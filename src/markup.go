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
// in the fonts given for each. A backslash before a punctuation character, as
// in \*, makes it plain text, and so is a mark that has no match, such as the
// * of 2 * 3. Emphasis can be inside a link, and neither inside code. A word
// keeps its punctuation next to it in another style, as in **bold**, with no
// space between them: the paragraph joins its text lines with
// Paragraph.AddJoined.
//
// Headings, lists, block quotes and the rest of Markdown's blocks are not
// read: a line break is a space, and Paragraphs splits a text into paragraphs
// at its empty lines. The markup is read in time linear in the length of the
// text, so it can come from anyone. Please see Example_53.
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
)

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
func (markup *Markup) SetLinkColor(linkColor int32) *Markup {
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
		paragraph := markup.Paragraph(buf.String())
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
	nodes := make([]*markupNode, 0)
	nodes = parser.parse(0, len(parser.text), "", nodes)
	paragraph := NewParagraph()
	// The runs of one style and link, in order.
	var buf strings.Builder
	flags := 0
	link := ""
	joined := false
	for _, node := range nodes {
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
	if link != "" {
		textLine.SetURIAction(link)
		textLine.SetTextColor(markup.linkColor)
		textLine.SetUnderline(markup.linkUnderline)
	}
	if joined {
		paragraph.AddJoined(textLine)
	} else {
		paragraph.Add(textLine)
	}
	return true
}

// markupNode is text in one style, or a run of * that may open or close
// emphasis, which is text too as far as it does not. The link is "" for text
// that is not in a link: the URL of a link is never empty.
type markupNode struct {
	text  string
	flags int
	link  string
	// Whether the node is a run of * whose emphasis is not matched yet, which
	// is when Java's text is null.
	run bool
	// Of a run of *: how many of them are not matched yet, whether they can
	// open and close emphasis, and the emphasis the run opens and closes,
	// counted in levels of the flags.
	count    int
	canOpen  bool
	canClose bool
	opens    *[2]int
	closes   *[2]int
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
		if text[i] == '\\' && i+1 < n {
			i += 2
		} else if text[i] == '`' {
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
		if ch == '\\' && i+1 < end && isASCIIPunctuation(text[i+1]) {
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
		} else if ch == '*' {
			length := 1
			for i+length < end && text[i+length] == '*' {
				length++
			}
			buf, nodes = markupFlush(buf, link, nodes)
			run := &markupNode{link: link, run: true}
			run.count = length
			run.canOpen = i+length < end && !isJavaWhitespace(text[i+length])
			run.canClose = i > start && !isJavaWhitespace(text[i-1])
			nodes = append(nodes, run)
			i += length
		} else if ch == '[' && link == "" && parser.isLink(i, end) {
			close := parser.closingBracket[i]
			urlEnd := parser.nextParenthesis[close+2]
			buf, nodes = markupFlush(buf, link, nodes)
			nodes = parser.parse(i+1, close, string(text[close+2:urlEnd]), nodes)
			i = urlEnd + 1
		} else {
			if isJavaWhitespace(ch) {
				buf = append(buf, ' ')
			} else {
				buf = append(buf, ch)
			}
			i++
		}
	}
	_, nodes = markupFlush(buf, link, nodes)
	markupMatchEmphasis(nodes, first)
	return nodes
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

// markupMatchEmphasis matches the runs of * from the index on, each closer
// with the nearest opener before it: two of each make bold and one italic,
// and the runs between them can no longer match. Then gives every node the
// emphasis it is in, and turns what is left of each run into text.
func markupMatchEmphasis(nodes []*markupNode, first int) {
	openers := make([]*markupNode, 0)
	for i := first; i < len(nodes); i++ {
		node := nodes[i]
		if !node.run {
			continue
		}
		if node.canClose {
			for node.count > 0 && len(openers) > 0 {
				opener := openers[len(openers)-1]
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
					openers = openers[:len(openers)-1]
				}
			}
		}
		if node.count > 0 && node.canOpen {
			openers = append(openers, node)
		}
	}
	// The emphasis in effect, in levels, since bold can be in bold.
	boldLevel := 0
	italicLevel := 0
	for i := first; i < len(nodes); i++ {
		node := nodes[i]
		if node.run {
			boldLevel -= markupLevel(node.closes, markupBold)
			italicLevel -= markupLevel(node.closes, markupItalic)
			node.text = strings.Repeat("*", node.count)
			node.run = false
			node.flags = markupEmphasis(boldLevel, italicLevel)
			boldLevel += markupLevel(node.opens, markupBold)
			italicLevel += markupLevel(node.opens, markupItalic)
		} else {
			node.flags |= markupEmphasis(boldLevel, italicLevel)
		}
	}
}

func markupEmphasis(boldLevel, italicLevel int) int {
	flags := 0
	if boldLevel > 0 {
		flags |= markupBold
	}
	if italicLevel > 0 {
		flags |= markupItalic
	}
	return flags
}

// markupAdd returns the levels of bold and of italic, [bold, italic], with one
// more of the flag.
func markupAdd(levels *[2]int, flag int) *[2]int {
	if levels == nil {
		levels = new([2]int)
	}
	if flag == markupBold {
		levels[0]++
	} else {
		levels[1]++
	}
	return levels
}

func markupLevel(levels *[2]int, flag int) int {
	if levels == nil {
		return 0
	}
	if flag == markupBold {
		return levels[0]
	}
	return levels[1]
}

func isASCIIPunctuation(ch rune) bool {
	return (ch >= '!' && ch <= '/') || (ch >= ':' && ch <= '@') ||
		(ch >= '[' && ch <= '`') || (ch >= '{' && ch <= '~')
}
