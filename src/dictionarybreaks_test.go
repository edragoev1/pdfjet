// dictionarybreaks_test.go
//
// Copyright (c) 2026 PDFjet Software
// Licensed under the MIT License. See LICENSE file in the project root.

package pdfjet

import (
	"fmt"
	"os"
	"strconv"
	"strings"
	"testing"
)

// Every run of Thai, Lao, Khmer and Burmese in the UDHR is broken into the
// words ICU breaks it into (tests/data/dictionarybreaks/expected.tsv, of
// make-expected.mjs): the same dictionaries, ICU's engines ported.
func TestDictionaryWordEndsAreICUs(t *testing.T) {
	t.Chdir("..") // resources/fonts/world/ICUDictionaries, as a program run from the repository finds it
	data, err := os.ReadFile("tests/data/dictionarybreaks/expected.tsv")
	if err != nil {
		t.Fatal(err)
	}
	runs, differ := map[string]int{}, map[string]int{}
	for _, line := range strings.Split(strings.TrimSpace(string(data)), "\n") {
		if strings.HasPrefix(line, "#") {
			continue
		}
		fields := strings.Split(line, "\t")
		var want []int
		for _, end := range strings.Split(fields[2], ",") {
			if end != "" {
				n, _ := strconv.Atoi(end)
				want = append(want, n)
			}
		}
		got := dictionaryWordEnds([]rune(fields[1]))
		runs[fields[0]]++
		if fmt.Sprint(got) != fmt.Sprint(want) && len(got)+len(want) > 0 {
			if differ[fields[0]] < 3 {
				t.Errorf("%s %q: %v, ICU %v", fields[0], fields[1], got, want)
			}
			differ[fields[0]]++
		}
	}
	for _, code := range []string{"tha", "lao", "khm", "mya"} {
		if runs[code] < 300 || differ[code] > 0 {
			t.Errorf("%s: %d of %d runs broken otherwise than ICU", code, differ[code], runs[code])
		}
	}
}
