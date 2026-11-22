// dictionarybreaks_test.go
//
// Copyright (c) 2026 PDFjet Software
// Licensed under the MIT License. See LICENSE file in the project root.

package pdfjet

import (
	"encoding/json"
	"fmt"
	"os"
	"testing"
)

// Every run of Thai, Lao, Khmer and Burmese in the UDHR is broken into the
// words ICU breaks it into (tests/data/dictionarybreaks/expected.json, of
// make-expected.mjs): the same dictionaries, ICU's engines ported.
func TestDictionaryWordEndsAreICUs(t *testing.T) {
	t.Chdir("..") // resources/fonts/world/ICUDictionaries, as a program run from the repository finds it
	data, err := os.ReadFile("tests/data/dictionarybreaks/expected.json")
	if err != nil {
		t.Fatal(err)
	}
	var expected struct {
		ICU     string `json:"icu"`
		Scripts map[string][]struct {
			Text string `json:"text"`
			Ends []int  `json:"ends"`
		} `json:"scripts"`
	}
	if err := json.Unmarshal(data, &expected); err != nil {
		t.Fatal(err)
	}
	for _, code := range []string{"tha", "lao", "khm", "mya"} {
		runs := expected.Scripts[code]
		differ := 0
		for _, run := range runs {
			got := dictionaryWordEnds([]rune(run.Text))
			if fmt.Sprint(got) != fmt.Sprint(run.Ends) && fmt.Sprint(got)+fmt.Sprint(run.Ends) != "[][]" {
				if differ < 3 {
					t.Errorf("%s %q: %v, ICU %s %v", code, run.Text, got, expected.ICU, run.Ends)
				}
				differ++
			}
		}
		if differ > 0 {
			t.Errorf("%s: %d of %d runs broken otherwise than ICU", code, differ, len(runs))
		}
	}
}
