
## PDFjet Pro

This package is PDFjet Pro: its `PDFjet.dll` has, besides the library, digital
signatures, and electronic invoices with the Swiss QR-bill and the EPC QR
code. It is the same assembly as the `PDFjet.dll` of PDFjet Core and of the
releases before, with the same strong name, so it replaces it with no change
to your project; its product name, in the properties of the file, is PDFjet
Pro.

| Path | What it is |
|---|---|
| `examples/pro/SignDocument/` | Signs a document: a PAdES signature, visible on the page. |
| `examples/pro/WriteInvoice/` | Writes an electronic invoice of Factur-X and ZUGFeRD. |
| `examples/pro/DrawSwissQRBill/` | Draws the Swiss QR-bill of an invoice. |
| `examples/pro/DrawEPCQRCode/` | Draws the EPC QR code of a SEPA credit transfer. |
| `examples/pro/HyphenateText/` | Hyphenates a paragraph, in English and German, beside the same without. |
| `examples/pro/SignLongTerm/` | Signs a document with long-term signatures, B-LT and B-LTA, and renews the archive time stamp. |
| `examples/pro/demo-signer.p12` | A certificate for the signing example alone, which no one trusts. |
| `examples/pro/ltv/` | The test PKI of SignLongTerm, which no one trusts, and `ltv-server.py`, its OCSP responder, CRLs and time stamp authority on 127.0.0.1:19180, which needs Python 3 and openssl; `build-pro.sh` starts it. |
| `Pro_*.pdf` | The PDFs the examples create. |
| `build-pro.sh`, `build-pro.cmd` | Build and run the examples of PDFjet Pro. |

- **PDFjet Sign**: PAdES signatures, B-B, B-T with a time stamp, and the
  long-term B-LT and B-LTA; RSA, RSASSA-PSS and ECDSA; keys in a PKCS #12
  file, in a PKCS #11 token, or in memory; visible and invisible signature
  fields.
- **PDFjet Invoice**: ZUGFeRD and Factur-X in every profile, and XRechnung,
  written into a PDF/A-3 document and read back; the Swiss QR-bill; the EPC QR
  code of a SEPA transfer.
- **Hyphenation** (`Hyphenator`): soft hyphens where the words of a text may
  break, for American and British English, French, Spanish and German, which
  TextBlock, TextFrame and the table cells break at. The licenses of its
  patterns are in `HYPHENATION-PATTERNS-LICENSES.txt`.

PDFjet Pro is built for .NET 8: `net48/PDFjet.dll`, for .NET Framework 4.8, is
the library without it.
