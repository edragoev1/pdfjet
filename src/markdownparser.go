// markdownparser.go
//
// Copyright (c) 2026 PDFjet Software
// Licensed under the MIT License. See LICENSE file in the project root.

package pdfjet

import (
	"strconv"
	"strings"
	"unicode/utf8"

	"github.com/edragoev1/pdfjet/v9/src/alignment"
)

// markdownParser reads the blocks of a Markdown text into a tree, for Markdown
// to draw: headings, paragraphs, code, block quotes, lists, thematic breaks,
// tables and images. The text of the headings, the paragraphs and the table
// cells keeps its inline markup, which Markup reads. Containers nest to
// markdownMaxDepth levels; deeper, their lines are text, so that no input
// makes the reading slower than linear in its length times that depth.
//
// The text is read as characters, as Java reads it: a byte of the text that is
// not valid UTF-8 is read as the character U+FFFD, one for each such byte, as
// Markup reads it, and a tab goes to the next column that is a multiple of 4
// in Java's characters, where a character outside the Basic Multilingual Plane
// is two.
type markdownParser struct{}

const markdownMaxDepth = 32

type markdownKind int

const (
	markdownHeading markdownKind = iota
	markdownParagraph
	markdownCode
	markdownQuote
	markdownList
	markdownItem
	markdownRule
	markdownTable
	markdownImage
)

type markdownBlock struct {
	kind       markdownKind
	level      int                   // Of a heading, 1 to 6
	text       string                // Of a heading or a paragraph, the code, or the alt text of an image
	source     string                // Of an image
	ordered    bool                  // Of a list
	start      int                   // Of an ordered list
	loose      bool                  // Of a list whose items are apart, with an empty line between them
	children   []*markdownBlock      // Of a quote, a list or an item
	rows       [][]string            // Of a table, the header row first
	alignments []alignment.Alignment // Of a table, of each column, or nil
}

func newMarkdownBlock(kind markdownKind) *markdownBlock {
	return &markdownBlock{kind: kind, start: 1, children: make([]*markdownBlock, 0)}
}

// parse returns the blocks of the text.
func (p markdownParser) parse(text string) []*markdownBlock {
	if !utf8.ValidString(text) {
		text = string([]rune(text))
	}
	lines := make([]string, 0)
	start := 0
	for i := 0; i < len(text); i++ {
		ch := text[i]
		if ch == '\n' || ch == '\r' {
			lines = append(lines, p.expandTabs(text[start:i]))
			if ch == '\r' && i+1 < len(text) && text[i+1] == '\n' {
				i++
			}
			start = i + 1
		}
	}
	lines = append(lines, p.expandTabs(text[start:]))
	return p.parseBlocks(lines, 0)
}

// expandTabs returns the line with each tab as spaces to the next column that
// is a multiple of 4.
func (p markdownParser) expandTabs(line string) string {
	if strings.IndexByte(line, '\t') == -1 {
		return line
	}
	var buf strings.Builder
	length := 0 // Of the buffer, in Java's characters
	for _, ch := range line {
		if ch == '\t' {
			for {
				buf.WriteByte(' ')
				length++
				if length%4 == 0 {
					break
				}
			}
		} else {
			buf.WriteRune(ch)
			if ch >= 0x10000 {
				length += 2
			} else {
				length++
			}
		}
	}
	return buf.String()
}

func (p markdownParser) parseBlocks(lines []string, depth int) []*markdownBlock {
	blocks := make([]*markdownBlock, 0)
	i := 0
	for i < len(lines) {
		line := lines[i]
		if p.isBlank(line) {
			i++
			continue
		}
		fence := p.fence(line)
		if fence != nil {
			i = p.parseFencedCode(lines, i, fence, &blocks)
		} else if p.atxLevel(line) > 0 {
			heading := newMarkdownBlock(markdownHeading)
			heading.level = p.atxLevel(line)
			heading.text = p.atxText(line)
			blocks = append(blocks, heading)
			i++
		} else if p.isThematicBreak(line) {
			blocks = append(blocks, newMarkdownBlock(markdownRule))
			i++
		} else if depth < markdownMaxDepth && p.isQuote(line) {
			i = p.parseQuote(lines, i, depth, &blocks)
		} else if depth < markdownMaxDepth && p.listMarker(line) != nil {
			i = p.parseList(lines, i, depth, &blocks)
		} else if p.isTableStart(lines, i) {
			i = p.parseTable(lines, i, &blocks)
		} else if p.indent(line) >= 4 {
			i = p.parseIndentedCode(lines, i, &blocks)
		} else {
			i = p.parseParagraph(lines, i, &blocks)
		}
	}
	return blocks
}

// parseFencedCode reads code between two fences of ``` or ~~~, the closing
// one as long or longer.
func (p markdownParser) parseFencedCode(lines []string, i int, fence []int, blocks *[]*markdownBlock) int {
	var code strings.Builder
	j := i + 1
	for ; j < len(lines); j++ {
		line := lines[j]
		if p.isClosingFence(line, fence) {
			j++
			break
		}
		if code.Len() > 0 || j > i+1 {
			code.WriteByte('\n')
		}
		// The content loses as much of its indent as the opening fence has.
		strip := min(fence[2], p.indent(line))
		code.WriteString(line[strip:])
	}
	block := newMarkdownBlock(markdownCode)
	block.text = code.String()
	*blocks = append(*blocks, block)
	return j
}

// parseIndentedCode reads lines indented 4 spaces or more, with the empty
// lines between them.
func (p markdownParser) parseIndentedCode(lines []string, i int, blocks *[]*markdownBlock) int {
	code := make([]string, 0)
	j := i
	for j < len(lines) && (p.isBlank(lines[j]) || p.indent(lines[j]) >= 4) {
		line := lines[j]
		if p.isBlank(line) {
			code = append(code, "")
		} else {
			code = append(code, line[4:])
		}
		j++
	}
	for len(code) > 0 && code[len(code)-1] == "" {
		code = code[:len(code)-1]
	}
	block := newMarkdownBlock(markdownCode)
	block.text = strings.Join(code, "\n")
	*blocks = append(*blocks, block)
	return j
}

// parseQuote reads the lines of a quote, without their >, and the lines that
// go on from the text of the quote without one, as Markdown allows.
func (p markdownParser) parseQuote(lines []string, i, depth int, blocks *[]*markdownBlock) int {
	quoted := make([]string, 0)
	j := i
	for j < len(lines) {
		line := lines[j]
		if p.isQuote(line) {
			quoted = append(quoted, p.stripQuote(line))
		} else if !p.isBlank(line) && len(quoted) > 0 && !p.isBlank(quoted[len(quoted)-1]) &&
			!p.interrupts(line) {
			quoted = append(quoted, line)
		} else {
			break
		}
		j++
	}
	quote := newMarkdownBlock(markdownQuote)
	quote.children = append(quote.children, p.parseBlocks(quoted, depth+1)...)
	*blocks = append(*blocks, quote)
	return j
}

// parseList reads the items of a list: each is its first line after the
// marker and the lines indented to its text, with what goes on from its text
// without an indent. A list ends at an item of another kind, or at a line
// that is not indented after an empty one.
func (p markdownParser) parseList(lines []string, i, depth int, blocks *[]*markdownBlock) int {
	first := p.listMarker(lines[i])
	list := newMarkdownBlock(markdownList)
	list.ordered = first[1] < 0
	list.start = 1
	if list.ordered {
		list.start = first[3]
	}
	for i < len(lines) {
		marker := p.listMarker(lines[i])
		if marker == nil || marker[1] != first[1] || p.isThematicBreak(lines[i]) {
			break
		}
		column := marker[2]
		line := lines[i]
		itemLines := make([]string, 0)
		if len(line) > column {
			itemLines = append(itemLines, line[column:])
		} else {
			itemLines = append(itemLines, "")
		}
		j := i + 1
		for j < len(lines) {
			next := lines[j]
			last := itemLines[len(itemLines)-1]
			if p.isBlank(next) {
				itemLines = append(itemLines, "")
			} else if p.indent(next) >= column {
				itemLines = append(itemLines, next[column:])
			} else if !p.isBlank(last) && !p.interrupts(next) && p.listMarker(next) == nil {
				itemLines = append(itemLines, next)
			} else {
				break
			}
			j++
		}
		// The empty lines after the item are between it and the next.
		blankAfter := false
		for len(itemLines) > 1 && itemLines[len(itemLines)-1] == "" {
			itemLines = itemLines[:len(itemLines)-1]
			blankAfter = true
		}
		if markdownContains(itemLines, "") || (blankAfter && j < len(lines) && p.sameList(lines[j], first)) {
			list.loose = true
		}
		item := newMarkdownBlock(markdownItem)
		item.children = append(item.children, p.parseBlocks(itemLines, depth+1)...)
		list.children = append(list.children, item)
		i = j
	}
	*blocks = append(*blocks, list)
	return i
}

func markdownContains(list []string, s string) bool {
	for _, element := range list {
		if element == s {
			return true
		}
	}
	return false
}

func (p markdownParser) sameList(line string, first []int) bool {
	marker := p.listMarker(line)
	return marker != nil && marker[1] == first[1]
}

// parseTable reads a table of GitHub's Markdown: a header row, a row of
// dashes that sets the alignment of each column, and the rows up to an empty
// line.
func (p markdownParser) parseTable(lines []string, i int, blocks *[]*markdownBlock) int {
	header := p.cells(lines[i])
	delimiters := p.cells(lines[i+1])
	table := newMarkdownBlock(markdownTable)
	table.rows = make([][]string, 0)
	table.alignments = make([]alignment.Alignment, 0)
	for _, delimiter := range delimiters {
		d := trimSpace(delimiter)
		left := strings.HasPrefix(d, ":")
		right := strings.HasSuffix(d, ":")
		switch {
		case left && right:
			table.alignments = append(table.alignments, alignment.Center)
		case right:
			table.alignments = append(table.alignments, alignment.Right)
		default:
			table.alignments = append(table.alignments, alignment.Left)
		}
	}
	table.rows = append(table.rows, header)
	j := i + 2
	for j < len(lines) && !p.isBlank(lines[j]) && strings.IndexByte(lines[j], '|') != -1 &&
		!p.interrupts(lines[j]) {
		row := p.cells(lines[j])
		// Every row has a cell for each column.
		for len(row) < len(header) {
			row = append(row, "")
		}
		table.rows = append(table.rows, append([]string(nil), row[:len(header)]...))
		j++
	}
	*blocks = append(*blocks, table)
	return j
}

func (p markdownParser) isTableStart(lines []string, i int) bool {
	if i+1 >= len(lines) || strings.IndexByte(lines[i], '|') == -1 || p.indent(lines[i]) >= 4 {
		return false
	}
	delimiterRow := lines[i+1]
	if strings.IndexByte(delimiterRow, '-') == -1 || p.indent(delimiterRow) >= 4 {
		return false
	}
	delimiters := p.cells(delimiterRow)
	for _, delimiter := range delimiters {
		d := trimSpace(delimiter)
		from := 0
		if strings.HasPrefix(d, ":") {
			from = 1
		}
		to := len(d)
		if strings.HasSuffix(d, ":") && len(d) > from {
			to = len(d) - 1
		}
		if to <= from {
			return false
		}
		for k := from; k < to; k++ {
			if d[k] != '-' {
				return false
			}
		}
	}
	return len(p.cells(lines[i])) == len(delimiters)
}

// cells returns the cells of a row, split at the | that no backslash escapes,
// without the | at the start and at the end of the row.
func (p markdownParser) cells(line string) []string {
	row := trimSpace(line)
	if strings.HasPrefix(row, "|") {
		row = row[1:]
	}
	if strings.HasSuffix(row, "|") && !strings.HasSuffix(row, "\\|") {
		row = row[:len(row)-1]
	}
	cells := make([]string, 0)
	var cell strings.Builder
	for k := 0; k < len(row); k++ {
		ch := row[k]
		if ch == '\\' && k+1 < len(row) {
			// The backslash and the character after it, which is a whole
			// character of UTF-8 when it is not ASCII.
			_, size := utf8.DecodeRuneInString(row[k+1:])
			cell.WriteString(row[k : k+1+size])
			k += size
		} else if ch == '|' {
			cells = append(cells, trimSpace(cell.String()))
			cell.Reset()
		} else {
			cell.WriteByte(ch)
		}
	}
	cells = append(cells, trimSpace(cell.String()))
	return cells
}

// parseParagraph reads the lines of a paragraph, up to an empty line or a
// line that starts another block. A line of = or - under them makes them a
// heading.
func (p markdownParser) parseParagraph(lines []string, i int, blocks *[]*markdownBlock) int {
	text := make([]string, 0)
	j := i
	for j < len(lines) && !p.isBlank(lines[j]) {
		line := lines[j]
		if j > i {
			level := p.setextLevel(line)
			if level > 0 {
				heading := newMarkdownBlock(markdownHeading)
				heading.level = level
				heading.text = strings.Join(text, "\n")
				*blocks = append(*blocks, heading)
				return j + 1
			}
			if p.interrupts(line) {
				break
			}
		}
		text = append(text, trimSpace(line))
		j++
	}
	paragraph := strings.Join(text, "\n")
	image := p.image(paragraph)
	if image != nil {
		*blocks = append(*blocks, image)
	} else {
		*blocks = append(*blocks, p.paragraph(paragraph))
	}
	return j
}

func (p markdownParser) paragraph(text string) *markdownBlock {
	block := newMarkdownBlock(markdownParagraph)
	block.text = text
	return block
}

// image returns an image alone in its paragraph: ![alt text](source), with no
// space in the source; or nil.
func (p markdownParser) image(text string) *markdownBlock {
	if !strings.HasPrefix(text, "![") || !strings.HasSuffix(text, ")") {
		return nil
	}
	close := strings.Index(text, "](")
	if close == -1 || strings.IndexByte(text[2:], ']')+2 != close {
		return nil
	}
	source := text[close+2 : len(text)-1]
	if source == "" {
		return nil
	}
	for _, ch := range source {
		if isJavaWhitespace(ch) || ch == '(' || ch == ')' {
			return nil
		}
	}
	block := newMarkdownBlock(markdownImage)
	block.text = text[2:close]
	block.source = source
	return block
}

// interrupts returns true for a line that ends a paragraph and starts another
// block, with no empty line before it: a fence, a heading, a thematic break, a
// quote, or a list item with text, of a numbered list only when it starts at 1.
func (p markdownParser) interrupts(line string) bool {
	if p.fence(line) != nil || p.atxLevel(line) > 0 || p.isThematicBreak(line) || p.isQuote(line) {
		return true
	}
	marker := p.listMarker(line)
	return marker != nil && !p.isBlank(line[min(marker[2], len(line)):]) &&
		(marker[1] >= 0 || marker[3] == 1)
}

func (p markdownParser) isBlank(line string) bool {
	return trimSpace(line) == ""
}

func (p markdownParser) indent(line string) int {
	n := 0
	for n < len(line) && line[n] == ' ' {
		n++
	}
	return n
}

// fence returns a fence of three or more ` or ~, indented 3 spaces at most,
// as {the character, its count, the indent}, or nil; a ` fence has no ` after
// it.
func (p markdownParser) fence(line string) []int {
	n := p.indent(line)
	if n > 3 || n >= len(line) {
		return nil
	}
	ch := line[n]
	if ch != '`' && ch != '~' {
		return nil
	}
	count := 0
	for n+count < len(line) && line[n+count] == ch {
		count++
	}
	if count < 3 || (ch == '`' && strings.IndexByte(line[n+count:], '`') != -1) {
		return nil
	}
	return []int{int(ch), count, n}
}

func (p markdownParser) isClosingFence(line string, fence []int) bool {
	n := p.indent(line)
	if n > 3 {
		return false
	}
	count := 0
	for n+count < len(line) && int(line[n+count]) == fence[0] {
		count++
	}
	return count >= fence[1] && trimSpace(line[n+count:]) == ""
}

// atxLevel returns the level of a heading of # to ######, or 0.
func (p markdownParser) atxLevel(line string) int {
	n := p.indent(line)
	if n > 3 {
		return 0
	}
	level := 0
	for n+level < len(line) && line[n+level] == '#' {
		level++
	}
	if level < 1 || level > 6 {
		return 0
	}
	after := n + level
	if after == len(line) || line[after] == ' ' {
		return level
	}
	return 0
}

// atxText returns the text of a heading of #, without the #s that close it.
func (p markdownParser) atxText(line string) string {
	text := trimSpace(line)
	k := 0
	for k < len(text) && text[k] == '#' {
		k++
	}
	text = trimSpace(text[k:])
	end := len(text)
	for end > 0 && text[end-1] == '#' {
		end--
	}
	if end == 0 || text[end-1] == ' ' {
		text = trimSpace(text[:end])
	}
	return text
}

// isThematicBreak returns true for a line of three or more *, - or _, all the
// same, with spaces between them.
func (p markdownParser) isThematicBreak(line string) bool {
	if p.indent(line) > 3 {
		return false
	}
	var mark byte
	count := 0
	for k := 0; k < len(line); k++ {
		ch := line[k]
		if ch == ' ' {
			continue
		}
		if (ch != '*' && ch != '-' && ch != '_') || (mark != 0 && ch != mark) {
			return false
		}
		mark = ch
		count++
	}
	return count >= 3
}

// setextLevel returns the level of the heading that a line of = (1) or of -
// (2) under a paragraph makes, or 0.
func (p markdownParser) setextLevel(line string) int {
	if p.indent(line) > 3 {
		return 0
	}
	text := trimSpace(line)
	if text == "" {
		return 0
	}
	ch := text[0]
	if ch != '=' && ch != '-' {
		return 0
	}
	for k := 0; k < len(text); k++ {
		if text[k] != ch {
			return 0
		}
	}
	if ch == '=' {
		return 1
	}
	return 2
}

func (p markdownParser) isQuote(line string) bool {
	n := p.indent(line)
	return n <= 3 && n < len(line) && line[n] == '>'
}

// stripQuote returns the line without its > and the space after it.
func (p markdownParser) stripQuote(line string) string {
	n := p.indent(line) + 1
	if n < len(line) && line[n] == ' ' {
		n++
	}
	return line[n:]
}

// listMarker returns the marker of a list item: -, + or * and a space, or 1 to
// 9 digits, . or ) and a space, indented 3 spaces at most. Returns {the
// indent, the kind: the bullet, or -1 after . and -2 after ), the column of
// the text, the number}, or nil. The text is 1 to 4 spaces after the marker;
// with more the item starts with indented code, one space after it.
func (p markdownParser) listMarker(line string) []int {
	n := p.indent(line)
	if n > 3 || n >= len(line) {
		return nil
	}
	ch := line[n]
	var end int
	var kind int
	number := 0
	if ch == '-' || ch == '+' || ch == '*' {
		end = n + 1
		kind = int(ch)
	} else {
		digits := 0
		for n+digits < len(line) && digits < 10 &&
			line[n+digits] >= '0' && line[n+digits] <= '9' {
			digits++
		}
		if digits == 0 || digits > 9 || n+digits >= len(line) {
			return nil
		}
		delimiter := line[n+digits]
		if delimiter != '.' && delimiter != ')' {
			return nil
		}
		number, _ = strconv.Atoi(line[n : n+digits])
		end = n + digits + 1
		if delimiter == '.' {
			kind = -1
		} else {
			kind = -2
		}
	}
	if end < len(line) && line[end] != ' ' {
		return nil
	}
	spaces := 0
	for end+spaces < len(line) && line[end+spaces] == ' ' {
		spaces++
	}
	column := end + 1
	if spaces >= 1 && spaces <= 4 && end+spaces < len(line) {
		column = end + spaces
	}
	return []int{n, kind, column, number}
}
