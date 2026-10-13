
## PDFjet Pro

This package includes PDFjet Pro: digital signatures, and electronic invoices
with the Swiss QR-bill.

| Path | What it is |
|---|---|
| `PDFjet.Sign.dll` | PDFjet Pro, for .NET 8 and later. Reference it with `PDFjet.dll`. |
| `examples/pro/SignDocument/` | Signs a document: a PAdES signature, visible on the page. |
| `examples/pro/WriteInvoice/` | Writes an electronic invoice of Factur-X and ZUGFeRD. |
| `examples/pro/DrawSwissQRBill/` | Draws the Swiss QR-bill of an invoice. |
| `examples/pro/demo-signer.p12` | A certificate for the signing example alone, which no one trusts. |
| `Pro_*.pdf` | The PDFs the examples create. |
| `build-pro.sh`, `build-pro.cmd` | Build and run the examples of PDFjet Pro. |

- **PDFjet Sign**: PAdES signatures, B-B and B-T with a time stamp; RSA,
  RSASSA-PSS and ECDSA; keys in a PKCS #12 file, in a PKCS #11 token, or in
  memory; visible and invisible signature fields.
- **PDFjet Invoice**: ZUGFeRD and Factur-X in every profile, and XRechnung,
  written into a PDF/A-3 document and read back; the Swiss QR-bill.

PDFjet Pro is built for .NET 8; `net48/` has the library alone, for .NET
Framework 4.8.
