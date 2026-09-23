// gs1.go
//
// Copyright (c) 2026 PDFjet Software
// Licensed under the MIT License. See LICENSE file in the project root.

package pdfjet

import "github.com/edragoev1/pdfjet/v9/src/internal/gs1"

// GS1DigitalLink returns the GS1 Digital Link of the GS1 data, the web address
// an ordinary QR code carries, at the domain: a brand's own, or GS1's
// resolver, https://id.gs1.org. The data is written as people read it, each
// Application Identifier in parentheses and its data after it:
//
//	GS1DigitalLink("https://id.gs1.org", "(01)09506000134352(10)ABC123(17)261231")
//
// is "https://id.gs1.org/01/09506000134352/10/ABC123?17=261231". The primary
// key, such as a GTIN (01), an SSCC (00) or a GLN (414), is first in the path,
// then its qualifiers in the order of the standard, such as (22), the batch
// (10) and the serial (21) of a GTIN, and the other fields are in the query, in
// their order. A value is percent-encoded where a web address needs it. It
// panics if the domain does not start with https:// or http://; if the data is
// not GS1, as for Barcode.GS1_128; or if it has no primary key or two, or both
// (254) and (7040) of a GLN.
func GS1DigitalLink(domain, data string) string {
	return gs1.DigitalLink(domain, data)
}
