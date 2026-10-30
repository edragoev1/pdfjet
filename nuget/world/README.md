# PDFjet world fonts

**The fonts of [PDFjet](https://www.nuget.org/packages/PDFjet) for
Chinese, Japanese, Korean, Arabic, Hebrew and Thai, as one package of their
own.** PDFjet includes every European language (Latin, Greek, Cyrillic);
for other scripts, add this package next to the package `PDFjet`, the same
version:

```bash
dotnet add package PDFjet.Fonts.World
```

Or in the project file:

```xml
<PackageReference Include="PDFjet" Version="9.1.7" />
<PackageReference Include="PDFjet.Fonts.World" Version="9.1.7" />
```

Then the constants of `IBMPlexSansJP`, `IBMPlexSansKR`, `IBMPlexSansSC`,
`IBMPlexSansTC`, `IBMPlexSansArabic`, `IBMPlexSansHebrew` and
`IBMPlexSansThai` work with nothing else to do:

```csharp
var japanese = new Font(pdf, IBMPlexSansJP.Regular);
var arabic = new Font(pdf, IBMPlexSansArabic.Regular);
```

PDFjet finds this package's `PDFjet.Fonts.World.dll` on its own; there is
no code to write. Without it, those constants throw an error that names
this package. Only the glyphs a document draws are embedded in it.

The package has IBM Plex Sans JP, KR, SC and TC, Regular, SemiBold and Bold,
and IBM Plex Sans Arabic, Hebrew and Thai, Thin to Bold, about 41 MB, under
the SIL Open Font License 1.1, with their licenses. One
`PDFjet.Fonts.World.dll`, for .NET Standard 2.0, serves .NET 8 and later and
.NET Framework 4.8 alike. They are apart from `PDFjet` so that a program
that does not need them stays small.

## More

- [The repository](https://github.com/edragoev1/pdfjet)
- [The documentation](https://edragoev1.github.io/pdfjet/)
- [The changelog](https://github.com/edragoev1/pdfjet/blob/main/CHANGELOG.md)
