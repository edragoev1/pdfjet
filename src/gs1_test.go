// gs1_test.go
//
// Copyright (c) 2026 PDFjet Software
// Licensed under the MIT License. See LICENSE file in the project root.

package pdfjet

import "testing"

func TestGS1DigitalLinkPutsTheKeyAndItsQualifiersInThePath(t *testing.T) {
	for _, c := range []struct{ domain, data, want string }{
		{"https://id.gs1.org", "(01)09506000134352(10)ABC123(21)XYZ-42(17)261231",
			"https://id.gs1.org/01/09506000134352/10/ABC123/21/XYZ-42?17=261231"},
		// The qualifiers in the order of the standard, the rest in the order given
		{"https://example.com/", "(17)261231(21)S1(01)09506000134352(10)B1(3103)000750",
			"https://example.com/01/09506000134352/10/B1/21/S1?17=261231&3103=000750"},
		{"https://id.gs1.org", "(01)09506000134352(22)2A(10)ABC",
			"https://id.gs1.org/01/09506000134352/22/2A/10/ABC"},
		{"https://id.gs1.org", "(01)09506000134352(21)A/B%C",
			"https://id.gs1.org/01/09506000134352/21/A%2FB%25C"},
		// The batch is no qualifier of an SSCC
		{"http://example.com/dl", "(00)106141412345678908(10)X",
			"http://example.com/dl/00/106141412345678908?10=X"},
		{"https://id.gs1.org", "(414)9506000134352(254)1",
			"https://id.gs1.org/414/9506000134352/254/1"},
	} {
		if got := GS1DigitalLink(c.domain, c.data); got != c.want {
			t.Errorf("%s: %s", c.data, got)
		}
	}
}

func TestGS1DigitalLinkRefusesWhatIsNotOne(t *testing.T) {
	for _, c := range []struct{ domain, data, want string }{
		{"id.gs1.org", "(01)09506000134352",
			"The domain of a GS1 Digital Link starts with https:// or http://, such as https://id.gs1.org!"},
		{"https://", "(01)09506000134352",
			"The domain of a GS1 Digital Link starts with https:// or http://, such as https://id.gs1.org!"},
		{"https://id.gs1.org", "(10)ABC",
			"A GS1 Digital Link needs a primary key, such as a GTIN (01), an SSCC (00) or a GLN (414)!"},
		{"https://id.gs1.org", "(01)09506000134352(00)106141412345678908",
			"A GS1 Digital Link has one primary key, not (01) and (00)!"},
		{"https://id.gs1.org", "(414)9506000134352(254)1(7040)1ABC",
			"The qualifiers (254) and (7040) of (414) cannot be together!"},
		{"https://id.gs1.org", "(01)09506000134353", "The check digit of (01) is wrong!"},
	} {
		if message, _ := testPanic(func() { GS1DigitalLink(c.domain, c.data) }); message != c.want {
			t.Errorf("%s %s: %q", c.domain, c.data, message)
		}
	}
}
