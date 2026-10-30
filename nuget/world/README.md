# PDFjet CJK fonts

**The Chinese, Japanese and Korean fonts of
[PDFjet](https://www.nuget.org/packages/PDFjet), as a package of their own.**
Add it next to the package `PDFjet`, the same version, when your documents
have Chinese, Japanese or Korean text:

```bash
dotnet add package PDFjet.Fonts.CJK
```

Or in the project file:

```xml
<PackageReference Include="PDFjet" Version="9.1.7" />
<PackageReference Include="PDFjet.Fonts.CJK" Version="9.1.7" />
```

Then the constants of `IBMPlexSansJP`, `IBMPlexSansKR`, `IBMPlexSansSC` and
`IBMPlexSansTC` work with nothing else to do:

```csharp
var font = new Font(pdf, IBMPlexSansJP.Regular);
```

PDFjet finds this package's `PDFjet.Fonts.CJK.dll` on its own; there is no
code to write. Without it, those constants throw an error that names this
package. Only the glyphs a document draws are embedded in it.

The package has IBM Plex Sans JP, KR, SC and TC, Regular, SemiBold and Bold,
about 40 MB, under the SIL Open Font License 1.1, with their licenses. One
`PDFjet.Fonts.CJK.dll`, for .NET Standard 2.0, serves .NET 8 and later and
.NET Framework 4.8 alike. They are apart from `PDFjet` so that a program
that does not need them stays small.

## More

- [The repository](https://github.com/edragoev1/pdfjet)
- [The documentation](https://edragoev1.github.io/pdfjet/)
- [The changelog](https://github.com/edragoev1/pdfjet/blob/main/CHANGELOG.md)
