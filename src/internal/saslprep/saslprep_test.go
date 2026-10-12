package saslprep

import (
	"strings"
	"testing"
)

// The examples of RFC 4013, section 3.
func TestPrepareTheExamplesOfRFC4013(t *testing.T) {
	for _, c := range []struct{ in, want string }{
		{"I­X", "IX"},
		{"user", "user"},
		{"USER", "USER"},
		{"ª", "a"},
		{"Ⅸ", "IX"},
	} {
		if got, err := Prepare(c.in); err != nil || got != c.want {
			t.Errorf("%q: %q, %v", c.in, got, err)
		}
	}
	for _, in := range []string{"\u0007", "ا1"} {
		if got, err := Prepare(in); err == nil {
			t.Errorf("%q: %q, not refused", in, got)
		}
	}
	// Right to left text that starts and ends with a right to left character
	if _, err := Prepare("ا1ب"); err != nil {
		t.Error(err)
	}
}

// The password as an accented letter is typed on one keyboard and another, a
// full-width letter, a no-break space and a Hangul syllable written as its
// letters come out the same.
func TestPrepareNormalizes(t *testing.T) {
	for _, c := range []struct{ in, want string }{
		{"café", "café"},
		{"café", "café"},
		{"ＡＢ", "AB"},
		{"a b", "a b"},
		{"각", "각"},
		{"각", "각"},
		{"ﬁ", "fi"},
		{"x²", "x2"},
		// Marks put in the order of their classes: dot below (220) before
		// the acute accent (230), the q with no composite of either
		{"q̣́", "q̣́"},
		// A blocked mark: the ring above stays apart from the A that the
		// acute accent of the same class, before it, did not compose with
		{"Á̊", "Á̊"},
	} {
		if got, err := Prepare(c.in); err != nil || got != c.want {
			t.Errorf("%q: %q (%U), %v; want %q", c.in, got, []rune(got), err, c.want)
		}
	}
	if _, err := Prepare("pass"); err == nil {
		t.Error("a private use character is not refused")
	}
}

// The bytes are cut at 127 even inside a character, as ISO 32000-2, qpdf,
// pdf.js and pypdf do (review of 12 October 2026, R1).
func TestTruncateCutsAt127Bytes(t *testing.T) {
	password := strings.Repeat("a", 126) + "é"
	if got := Truncate(password); string(got) != password[:127] {
		t.Errorf("%d bytes", len(got))
	}
	if got := Truncate(strings.Repeat("a", 200)); len(got) != 127 {
		t.Errorf("%d bytes", len(got))
	}
}
