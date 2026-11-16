package pdfjet

import (
	"bufio"
	"io"
	"math/rand"
	"slices"
	"testing"
	"time"
)

// markOffsetsBefore is markOffsets as it was before 9.1.9: each mark's letter
// looked for past all the marks before it, and the marks of a letter sorted by
// insertion, both of which take the square of the marks of a letter.
func markOffsetsBefore(font *Font, codePoints []rune, gids []int) []int {
	n := len(gids)
	x := make([]int, n)
	for i := 1; i < n; i++ {
		x[i] = x[i-1] + font.glyphAdvance(gids[i-1])
	}
	offsets := make([]int, 2*n)
	for i := 0; i < n; i++ {
		if !isMark(codePoints[i]) {
			continue
		}
		step := -1
		if isRightToLeft(codePoints[i]) {
			step = 1
		}
		base := i + step
		for base >= 0 && base < n && isMark(codePoints[base]) {
			base += step
		}
		if base >= 0 && base < n {
			if offset := markToBaseOffset(font, codePoints[base], gids[base], gids[i]); offset != nil {
				offsets[2*i] = x[base] + offset[0] - x[i]
				offsets[2*i+1] = offset[1]
			}
		}
	}
	order := make([]int, n)
	start := 0
	for start < n {
		if !isMark(codePoints[start]) {
			start++
			continue
		}
		rightToLeft := isRightToLeft(codePoints[start])
		end := start + 1
		for end < n && isMark(codePoints[end]) && isRightToLeft(codePoints[end]) == rightToLeft {
			end++
		}
		count := end - start
		for k := 0; k < count; k++ {
			mark := start + k
			if rightToLeft {
				mark = end - 1 - k
			}
			rank := markOrder(codePoints[mark])
			l := k - 1
			for rank != 0 && l >= 0 && markOrder(codePoints[order[l]]) > rank {
				order[l+1] = order[l]
				l--
			}
			order[l+1] = mark
		}
		for k := 1; k < count; k++ {
			placeOnMark(font, gids, x, offsets, order[k], order[k-1])
		}
		start = end
	}
	return offsets
}

// The letters and marks of each font's script that the random texts are
// made of: the marks of every order markOrder gives, of order 0 too, and
// Latin and Hebrew marks mixed, so that a run of marks changes direction.
var markTestFonts = []struct {
	path    string
	letters []rune
	marks   []rune
}{
	{"../resources/fonts/IBMPlexSans/IBMPlexSans-Regular.otf",
		[]rune("aeoAEOgíyЖж"), []rune{0x0300, 0x0301, 0x0302, 0x0303, 0x0308, 0x0316, 0x0323, 0x0327, 0x05B4, 0x05BC}},
	{"../resources/fonts/world/IBMPlexSansHebrew/IBMPlexSansHebrew-Regular.otf",
		[]rune("אבגדשת"), []rune{0x05B0, 0x05B1, 0x05B4, 0x05B7, 0x05B8, 0x05BC, 0x05BF, 0x05C1, 0x05C2, 0x05C7, 0x0301}},
	{"../resources/fonts/world/IBMPlexSansArabic/IBMPlexSansArabic-Regular.otf",
		[]rune("بتثسلم"), []rune{0x064B, 0x064C, 0x064E, 0x064F, 0x0650, 0x0651, 0x0652, 0x0670, 0x0654}},
	{"../resources/fonts/world/IBMPlexSansThai/IBMPlexSansThai-Regular.otf",
		[]rune("กขคนบ"), []rune{0x0E31, 0x0E34, 0x0E35, 0x0E38, 0x0E39, 0x0E3A, 0x0E47, 0x0E48, 0x0E49, 0x0E4A, 0x0E4B, 0x0E4C}},
}

// TestMarkOffsets checks that markOffsets puts each mark where
// markOffsetsBefore did, on random texts of letters and up to 12 marks each.
func TestMarkOffsets(t *testing.T) {
	random := rand.New(rand.NewSource(20261116))
	for _, f := range markTestFonts {
		font := NewFontFromFile(NewPDF(bufio.NewWriter(io.Discard)), f.path)
		for trial := 0; trial < 2000; trial++ {
			var text []rune
			for len(text) < 40 {
				if random.Intn(4) != 0 {
					text = append(text, f.letters[random.Intn(len(f.letters))])
				}
				for m := random.Intn(13); m > 0; m-- {
					text = append(text, f.marks[random.Intn(len(f.marks))])
				}
			}
			gids := make([]int, len(text))
			for i, c := range text {
				gids[i] = font.unicodeToGID.get(c)
			}
			if got, want := markOffsets(font, text, gids), markOffsetsBefore(font, text, gids); !slices.Equal(got, want) {
				t.Fatalf("%s: %q:\n got  %v\n want %v", f.path, string(text), got, want)
			}
		}
	}
}

// TestMarkOffsetsLinear checks that a letter of 16,000 marks, of two orders
// and of one, takes no more than linear time: it took 4 s.
func TestMarkOffsetsLinear(t *testing.T) {
	font := NewFontFromFile(NewPDF(bufio.NewWriter(io.Discard)), markTestFonts[1].path)
	for _, marks := range [][]rune{{0x05B7, 0x05B4}, {0x0301}, {0x05B0}} {
		text := []rune{0x05D0}
		for len(text) < 16_001 {
			text = append(text, marks...)
		}
		gids := make([]int, len(text))
		for i, c := range text {
			gids[i] = font.unicodeToGID.get(c)
		}
		start := time.Now()
		markOffsets(font, text, gids)
		if elapsed := time.Since(start); elapsed > 100*time.Millisecond {
			t.Errorf("%U: 16,000 marks on a letter took %v", marks, elapsed)
		}
	}
}
