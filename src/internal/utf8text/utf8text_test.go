// utf8text_test.go
//
// Copyright (c) 2026 PDFjet Software
// Licensed under the MIT License. See LICENSE file in the project root.

package utf8text

import (
	"encoding/hex"
	"testing"
)

// The bytes and the text they read as, which the four ports share: the
// substitution of U+FFFD for the maximal subparts of section 3.9 of the
// Unicode Standard. The same table is in the tests of the other three ports.
var utf8Cases = []struct {
	bytes string // In hexadecimal, so that the case reads as the bytes it is
	want  string
}{
	{"", ""},
	{"68656C6C6F", "hello"},    // hello
	{"77C3B6726C64", "wörld"},  // wörld
	{"E697A5E69CAC", "日本"},     // 日本
	{"F09F9880", "\U0001F600"}, // A code point outside the plane
	{"EFBFBD", "�"},            // The replacement character itself
	{"EFBFBE", "￾"},            // A noncharacter is well formed
	{"EDA080", "���"},          // An encoded surrogate, U+D800
	{"EDBFBF", "���"},          // U+DFFF
	{"EDA080EDB080", "������"}, // An encoded pair
	{"EDA0", "��"},             // Two bytes of an encoded surrogate
	{"ED", "�"},
	{"C080", "��"}, // The overlong encodings
	{"E08080", "���"},
	{"F0828282", "����"},
	{"F4908080", "����"}, // Past U+10FFFF
	{"F5808080", "����"},
	{"FE", "�"},
	{"FF", "�"},
	{"80", "�"}, // A byte of a sequence, alone
	{"BF", "�"},
	{"C2", "�"},    // A sequence cut short is one
	{"C2C2", "��"}, // replacement, however many
	{"E282", "�"},  // bytes of it are there
	{"E0A0", "�"},
	{"F09080", "�"},
	{"41C2", "A�"},
	{"E28282", "₂"},         // Well formed, subscript two
	{"61EDA08062", "a���b"}, // Between well formed text
}

func TestDecodeReplacesTheMaximalSubpartsOfIllFormedSequences(t *testing.T) {
	for _, item := range utf8Cases {
		bytes, err := hex.DecodeString(item.bytes)
		if err != nil {
			t.Fatal(err)
		}
		if got := Decode(bytes); got != item.want {
			t.Errorf("%s reads %q, not %q", item.bytes, got, item.want)
		}
	}
}
