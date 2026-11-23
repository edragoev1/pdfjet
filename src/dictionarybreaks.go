// dictionarybreaks.go
//
// Copyright (c) 2026 PDFjet Software
// Licensed under the MIT License. See LICENSE file in the project root.
//
// The break engine is a port of ICU's dictionary break engines
// (icu4c/source/common/dictbe.cpp and dictionarydata.cpp), Copyright (C) 2016
// and later: Unicode, Inc. and others, under the Unicode License, as are the
// dictionaries it reads: see resources/fonts/world/ICUDictionaries/license.txt.

package pdfjet

import (
	"os"
	"path/filepath"
	"sort"
	"strings"
	"sync"
	"unicode"

	"github.com/edragoev1/pdfjet/v9/resources/fonts"
)

// Thai, Lao, Khmer and Burmese are written without spaces between their words,
// so where a line may break is found with a dictionary of each, as ICU finds
// it: dictionaryWordEnds ports ICU's dictionary break engines, which look
// three words ahead for the choice that lets dictionary words follow, join a
// run of characters that is not a word to the word before it, and never end a
// word before a combining mark. The dictionaries are ICU's, carried with the
// world fonts (resources/fonts/world/ICUDictionaries, the package world), and
// read when text of their script is first broken; without them no break is
// found inside such text, as before.

// dictionaryScript is what an engine of ICU's knows of its script.
type dictionaryScript struct {
	file      string          // The dictionary's file, in ICUDictionaries
	word      func(rune) bool // [[:Script:]&[:LineBreak=SA:]]: the characters it breaks
	beginWord func(rune) bool // The characters a word may begin with
	endWord   func(rune) bool // The characters a word may end with
	thai      bool            // Thai's suffixes, PAIYANNOI and MAIYAMOK, and its shortest span
}

// The characters of each script of line break class SA (LineBreak.txt of
// Unicode 18.0.0), and the marks among them
func inRanges(r rune, ranges ...[2]rune) bool {
	for _, x := range ranges {
		if r >= x[0] && r <= x[1] {
			return true
		}
	}
	return false
}

func thaiWord(r rune) bool { return inRanges(r, [2]rune{0x0E01, 0x0E3A}, [2]rune{0x0E40, 0x0E4E}) }

func laoWord(r rune) bool {
	return inRanges(r, [2]rune{0x0E81, 0x0E82}, [2]rune{0x0E84, 0x0E84}, [2]rune{0x0E86, 0x0E8A},
		[2]rune{0x0E8C, 0x0EA3}, [2]rune{0x0EA5, 0x0EA5}, [2]rune{0x0EA7, 0x0EBD}, [2]rune{0x0EC0, 0x0EC4},
		[2]rune{0x0EC6, 0x0EC6}, [2]rune{0x0EC8, 0x0ECE}, [2]rune{0x0EDC, 0x0EDF})
}

func khmerWord(r rune) bool {
	return inRanges(r, [2]rune{0x1780, 0x17D3}, [2]rune{0x17D7, 0x17D7}, [2]rune{0x17DC, 0x17DD})
}

func burmeseWord(r rune) bool {
	return inRanges(r, [2]rune{0x1000, 0x103F}, [2]rune{0x1050, 0x108F}, [2]rune{0x109A, 0x109F},
		[2]rune{0xA9E0, 0xA9EF}, [2]rune{0xA9FA, 0xA9FE}, [2]rune{0xAA60, 0xAA7F})
}

var dictionaryScripts = []*dictionaryScript{
	{
		file: "thaidict.txt", word: thaiWord, thai: true,
		// MAI HAN-AKAT and SARA E through SARA AI MAIMALAI end no word
		endWord: func(r rune) bool { return thaiWord(r) && r != 0x0E31 && !(r >= 0x0E40 && r <= 0x0E44) },
		// KO KAI through HO NOKHUK, SARA E through SARA AI MAIMALAI
		beginWord: func(r rune) bool { return (r >= 0x0E01 && r <= 0x0E2E) || (r >= 0x0E40 && r <= 0x0E44) },
	},
	{
		file: "laodict.txt", word: laoWord,
		// The prefix vowels end no word
		endWord: func(r rune) bool { return laoWord(r) && !(r >= 0x0EC0 && r <= 0x0EC4) },
		// The consonants, the digraph consonants, the prefix vowels
		beginWord: func(r rune) bool {
			return (r >= 0x0E81 && r <= 0x0EAE) || (r >= 0x0EDC && r <= 0x0EDD) || (r >= 0x0EC0 && r <= 0x0EC4)
		},
	},
	{
		file: "khmerdict.txt", word: khmerWord,
		// KHMER SIGN COENG, which joins the next consonant, ends no word
		endWord:   func(r rune) bool { return khmerWord(r) && r != 0x17D2 },
		beginWord: func(r rune) bool { return r >= 0x1780 && r <= 0x17B3 },
	},
	{
		file: "burmesedict.txt", word: burmeseWord, endWord: burmeseWord,
		// The consonants and the independent vowels
		beginWord: func(r rune) bool { return r >= 0x1000 && r <= 0x102A },
	},
}

// What ICU's engines share: how many words in a row are good enough; the
// longest dictionary word a non-word is joined to; the prefix a non-word may
// share with a dictionary word and still be joined; the shortest word
const (
	dictionaryLookahead        = 3
	dictionaryRootCombine      = 3
	dictionaryPrefixCombine    = 3
	dictionaryMinWordSpan      = 2 * 2
	dictionaryPossibleWordsMax = 20
	thaiPaiyannoi              = 0x0E2F
	thaiMaiyamok               = 0x0E46
)

// A mark of the script's characters, or a space: a word never ends before one
func dictionaryMark(script *dictionaryScript, r rune) bool {
	return r == ' ' || (script.word(r) && unicode.Is(unicode.M, r))
}

// wordDictionary is a dictionary's words, sorted, for ICU's trie's answers:
// whether a string is a word, and whether a longer word begins with it.
type wordDictionary struct {
	words  []string
	isWord map[string]bool
}

// readWordDictionary reads a dictionary of ICU's: a word a line, after an
// optional tab its value, which is not used; comments begin with #.
func readWordDictionary(data []byte) *wordDictionary {
	d := &wordDictionary{isWord: map[string]bool{}}
	for _, line := range strings.Split(strings.TrimPrefix(string(data), "\uFEFF"), "\n") {
		word, _, _ := strings.Cut(line, "\t")
		word = strings.TrimSpace(word)
		if word == "" || strings.HasPrefix(word, "#") || d.isWord[word] {
			continue
		}
		d.isWord[word] = true
		d.words = append(d.words, word)
	}
	sort.Strings(d.words)
	return d
}

// continues tells if a word of the dictionary begins with the prefix and is
// longer than it, as ICU's trie can go on from the prefix.
func (d *wordDictionary) continues(prefix string) bool {
	i := sort.SearchStrings(d.words, prefix)
	if i < len(d.words) && d.words[i] == prefix {
		i++
	}
	return i < len(d.words) && strings.HasPrefix(d.words[i], prefix)
}

// matches is ICU's DictionaryMatcher.matches: the lengths, shortest first, of
// the words of the dictionary the text begins with at the position, no longer
// than maxLength, at most limit of them; and the number of characters the
// trie took, the one it refused among them.
func (d *wordDictionary) matches(text []rune, at, maxLength int, lengths []int) (count, prefix int) {
	var sb strings.Builder
	for i := at; i < len(text); i++ {
		sb.WriteRune(text[i])
		s := sb.String()
		lengthMatched := i + 1 - at
		prefix++
		word, more := d.isWord[s], d.continues(s)
		if word {
			if count < len(lengths) {
				lengths[count] = lengthMatched
				count++
			}
			if !more { // A final value
				break
			}
		} else if !more { // No match
			break
		}
		if lengthMatched >= maxLength {
			break
		}
	}
	return count, prefix
}

// possibleWord is ICU's PossibleWord: the words of the dictionary at a
// position of the text, the one chosen, the one looked at.
type possibleWord struct {
	count, prefix, offset, mark, current int
	lengths                              [dictionaryPossibleWordsMax]int
}

// breakText is ICU's UText as the engines use it: the text and a position.
type breakText struct {
	runes []rune
	pos   int
}

func (t *breakText) current32() rune {
	if t.pos < len(t.runes) {
		return t.runes[t.pos]
	}
	return -1
}

func (t *breakText) next32() rune {
	if t.pos < len(t.runes) {
		t.pos++
		return t.runes[t.pos-1]
	}
	return -1
}

func (t *breakText) previous32() rune {
	if t.pos > 0 {
		t.pos--
		return t.runes[t.pos]
	}
	return -1
}

func (w *possibleWord) candidates(t *breakText, d *wordDictionary, rangeEnd int) int {
	start := t.pos
	if start != w.offset {
		w.offset = start
		w.count, w.prefix = d.matches(t.runes, start, rangeEnd-start, w.lengths[:])
		if w.count <= 0 {
			t.pos = start
		}
	}
	if w.count > 0 {
		t.pos = start + w.lengths[w.count-1]
	}
	w.current = w.count - 1
	w.mark = w.current
	return w.count
}

func (w *possibleWord) acceptMarked(t *breakText) int {
	t.pos = w.offset + w.lengths[w.mark]
	return w.lengths[w.mark]
}

func (w *possibleWord) backUp(t *breakText) bool {
	if w.current > 0 {
		w.current--
		t.pos = w.offset + w.lengths[w.current]
		return true
	}
	return false
}

// divideUpDictionaryRange is ICU's engine on a run of the script's
// characters, from rangeStart to rangeEnd: the positions where its words end,
// but its end.
func divideUpDictionaryRange(script *dictionaryScript, d *wordDictionary, t *breakText, rangeStart, rangeEnd int) []int {
	// Not enough characters for two words: Thai's engine moves four
	// characters on and wants the end beyond them, the others want four
	if (script.thai && rangeEnd-rangeStart <= dictionaryMinWordSpan) ||
		(!script.thai && rangeEnd-rangeStart < dictionaryMinWordSpan) {
		return nil
	}
	var breaks []int
	wordsFound := 0
	var words [dictionaryLookahead]possibleWord
	for i := range words {
		words[i].offset = -1
	}
	t.pos = rangeStart
	for t.pos < rangeEnd {
		current := t.pos
		wordLength := 0
		cpWordLength := 0
		word := &words[wordsFound%dictionaryLookahead]

		// Candidate words at the position: one is taken; of more, the one
		// that lets the most words follow
		candidates := word.candidates(t, d, rangeEnd)
		if candidates == 1 {
			wordLength = word.acceptMarked(t)
			cpWordLength = word.lengths[word.mark]
			wordsFound++
		} else if candidates > 1 {
			func() {
				if t.pos >= rangeEnd {
					return
				}
				for {
					if words[(wordsFound+1)%dictionaryLookahead].candidates(t, d, rangeEnd) > 0 {
						// Followed by another word: a good candidate
						word.mark = word.current
						if t.pos >= rangeEnd {
							return
						}
						// A third word after any of the second words: done
						for {
							if words[(wordsFound+2)%dictionaryLookahead].candidates(t, d, rangeEnd) > 0 {
								word.mark = word.current
								return
							}
							if !words[(wordsFound+1)%dictionaryLookahead].backUp(t) {
								break
							}
						}
					}
					if !word.backUp(t) {
						break
					}
				}
			}()
			wordLength = word.acceptMarked(t)
			cpWordLength = word.lengths[word.mark]
			wordsFound++
		}

		// A non-word after the word is joined to it, if the word is short
		// and the non-word shares too little with a dictionary word
		var uc rune
		if t.pos < rangeEnd && cpWordLength < dictionaryRootCombine {
			next := &words[wordsFound%dictionaryLookahead]
			if next.candidates(t, d, rangeEnd) <= 0 && (wordLength == 0 || next.prefix < dictionaryPrefixCombine) {
				// A plausible word boundary
				remaining := rangeEnd - (current + wordLength)
				chars := 0
				for {
					pcIndex := t.pos
					pc := t.next32()
					pcSize := t.pos - pcIndex
					chars += pcSize
					remaining -= pcSize
					if remaining <= 0 {
						break
					}
					uc = t.current32()
					if script.endWord(pc) && script.beginWord(uc) {
						n := words[(wordsFound+1)%dictionaryLookahead].candidates(t, d, rangeEnd)
						t.pos = current + wordLength + chars
						if n > 0 {
							break
						}
					}
				}
				if wordLength <= 0 {
					wordsFound++
				}
				wordLength += chars
			} else {
				t.pos = current + wordLength
			}
		}

		// Never a break before a combining mark
		for t.pos < rangeEnd && dictionaryMark(script, t.current32()) {
			currPos := t.pos
			t.next32()
			wordLength += t.pos - currPos
		}

		// Thai's suffixes, when no word follows
		if script.thai && t.pos < rangeEnd && wordLength > 0 {
			uc = t.current32()
			if words[wordsFound%dictionaryLookahead].candidates(t, d, rangeEnd) <= 0 &&
				(uc == thaiPaiyannoi || uc == thaiMaiyamok) {
				if uc == thaiPaiyannoi {
					prev := t.previous32()
					if prev != thaiPaiyannoi && prev != thaiMaiyamok {
						t.next32()
						paiyannoiIndex := t.pos
						t.next32()
						wordLength += t.pos - paiyannoiIndex
						uc = t.current32()
					} else {
						t.next32()
					}
				}
				if uc == thaiMaiyamok {
					if t.previous32() != thaiMaiyamok {
						t.next32()
						maiyamokIndex := t.pos
						t.next32()
						wordLength += t.pos - maiyamokIndex
					} else {
						t.next32()
					}
				}
			} else {
				t.pos = current + wordLength
			}
		}

		if wordLength > 0 {
			breaks = append(breaks, current+wordLength)
		}
	}
	// No break at the end of the run
	if len(breaks) > 0 && breaks[len(breaks)-1] >= rangeEnd {
		breaks = breaks[:len(breaks)-1]
	}
	return breaks
}

var (
	wordDictionaryMutex sync.Mutex
	wordDictionaries    = map[string]*wordDictionary{}
)

// wordDictionaryOf returns the dictionary of the script, read once, or nil
// when it is not carried: from the folder resources/fonts/world of the
// working directory, else from the package world when the program imports it.
func wordDictionaryOf(script *dictionaryScript) *wordDictionary {
	wordDictionaryMutex.Lock()
	defer wordDictionaryMutex.Unlock()
	if d, ok := wordDictionaries[script.file]; ok {
		return d
	}
	name := "ICUDictionaries/" + script.file
	data, err := os.ReadFile(filepath.Join(bundledFolder, "world", filepath.FromSlash(name)))
	if err != nil {
		data, err = fonts.ReadFile(name)
	}
	var d *wordDictionary
	if err == nil {
		d = readWordDictionary(data)
	}
	wordDictionaries[script.file] = d
	return d
}

// dictionaryWordEnds returns the indexes in the runes where a word of Thai,
// Lao, Khmer or Burmese ends inside a run of the script, where a line may
// break: in increasing order, none at a run's end.
func dictionaryWordEnds(runes []rune) []int {
	return dictionaryWordEndsOf(runes, nil)
}

// dictionaryWordEndsOf is dictionaryWordEnds of the runs of one script, or
// of all four when it is nil.
func dictionaryWordEndsOf(runes []rune, only *dictionaryScript) []int {
	var ends []int
	t := &breakText{runes: runes}
	for i := 0; i < len(runes); {
		var script *dictionaryScript
		for _, s := range dictionaryScripts {
			if s.word(runes[i]) {
				script = s
				break
			}
		}
		if script == nil || (only != nil && script != only) {
			i++
			continue
		}
		start := i
		for i < len(runes) && script.word(runes[i]) {
			i++
		}
		if d := wordDictionaryOf(script); d != nil {
			ends = append(ends, divideUpDictionaryRange(script, d, t, start, i)...)
		}
	}
	// The ends before a mark left out, as ICU's rules never break a letter
	// from its marks: a mark after a sign that takes none, as ฯ then ุ, ended
	// a word before it (found by FuzzDictionaryWordEnds)
	kept := ends[:0]
	for _, end := range ends {
		if end >= len(runes) || !unicode.In(runes[end], unicode.Mn, unicode.Mc, unicode.Me) {
			kept = append(kept, end)
		}
	}
	return kept
}

// thaiWordBreaks returns the line with a zero width space at each end of a
// Thai word inside a run of Thai, where TextBlock may break it, when the line
// has no zero width space of its own; else, or without the dictionary, the
// line as it is. A zero width space is not drawn.
func thaiWordBreaks(line string) string {
	if strings.ContainsRune(line, '\u200B') || !strings.ContainsFunc(line, thaiWord) {
		return line
	}
	runes := []rune(line)
	ends := dictionaryWordEndsOf(runes, dictionaryScripts[0])
	if len(ends) == 0 {
		return line
	}
	var sb strings.Builder
	next := 0
	for i, r := range runes {
		if next < len(ends) && ends[next] == i {
			sb.WriteRune('\u200B')
			next++
		}
		sb.WriteRune(r)
	}
	return sb.String()
}
