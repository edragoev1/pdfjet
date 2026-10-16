
## PDFjet Pro

This package is PDFjet Pro: its `PDFjet.jar` has, besides the library, digital
signatures, and electronic invoices with the Swiss QR-bill and the EPC QR
code. The API reference, `docs/java/index.html`, covers them.

| Path | What it is |
|---|---|
| `examples/pro/SignDocument.java` | Signs a document: a PAdES signature, visible on the page. |
| `examples/pro/WriteInvoice.java` | Writes an electronic invoice of Factur-X and ZUGFeRD. |
| `examples/pro/DrawSwissQRBill.java` | Draws the Swiss QR-bill of an invoice. |
| `examples/pro/DrawEPCQRCode.java` | Draws the EPC QR code of a SEPA credit transfer. |
| `examples/pro/demo-signer.p12` | A certificate for the signing example alone, which no one trusts. |
| `Pro_*.pdf` | The PDFs the examples create. |
| `build-pro.sh`, `build-pro.cmd` | Compile and run the examples of PDFjet Pro. |
| `pom.xml` | The POM of this `PDFjet.jar`, `com.pdfjet:pdfjet-pro`, for Maven and Gradle. |

- **PDFjet Sign** (`com.pdfjet.sign`): PAdES signatures, B-B and B-T with a
  time stamp; RSA, RSASSA-PSS and ECDSA; keys in a PKCS #12 file, in a PKCS #11
  token, or in memory; visible and invisible signature fields.
- **PDFjet Invoice** (`com.pdfjet.invoice`): ZUGFeRD and Factur-X in every
  profile, and XRechnung, written into a PDF/A-3 document and read back; the
  Swiss QR-bill; the EPC QR code of a SEPA transfer.

The manifest of this `PDFjet.jar` names it PDFjet Pro, as the jar of PDFjet
Core, of the same name, is not.

### With Maven or Gradle

PDFjet Pro is not on Maven Central, where PDFjet Core is, as
`com.pdfjet:pdfjet`. Install this `PDFjet.jar`, with the `pom.xml` beside it,
as `com.pdfjet:pdfjet-pro`, once, in the folder of this package:

```bash
mvn install:install-file -Dfile=PDFjet.jar -DpomFile=pom.xml
```

That puts it in the Maven repository of your computer, `~/.m2/repository`. For
a team, or a build server, put it in your company's repository instead, such
as Nexus or Artifactory, with its own upload page, or with
`mvn deploy:deploy-file -Dfile=PDFjet.jar -DpomFile=pom.xml -Durl=<the repository's URL> -DrepositoryId=<its id in settings.xml>`.

Then depend on it, with Maven:

```xml
<dependency>
  <groupId>com.pdfjet</groupId>
  <artifactId>pdfjet-pro</artifactId>
  <version>@VERSION@</version>
</dependency>
```

or with Gradle, with `mavenLocal()`, or your company's repository, among the
repositories:

```groovy
implementation("com.pdfjet:pdfjet-pro:@VERSION@")
```

PDFjet Pro has all of PDFjet Core in it: depend on `pdfjet-pro` in place of
`com.pdfjet:pdfjet`, not beside it, as both have the same classes. Moving from
Core to Pro changes no code.
