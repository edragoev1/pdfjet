# PDFjet HarfBuzz

**HarfBuzz for [PDFjet](https://www.nuget.org/packages/PDFjet)'s
`ComplexText`,** which draws the scripts whose letters change shape and
place: Hindi, Marathi and Nepali (Devanagari), Bengali, Tamil, Telugu,
Kannada, Malayalam, Gujarati, Punjabi, Odia, Sinhala, Khmer, Lao and
Burmese. Add it next to the package `PDFjet`, the same version:

```bash
dotnet add package PDFjet.HarfBuzz
```

Or in the project file:

```xml
<PackageReference Include="PDFjet" Version="9.2.1" />
<PackageReference Include="PDFjet.HarfBuzz" Version="9.2.1" />
```

Then `ComplexText` shapes with HarfBuzz, the text engine of Chrome and
Android, with nothing to install and no code. The Noto Sans fonts of these
scripts are in the package `PDFjet.Fonts.World`:

```csharp
var hindi = new Font(pdf, NotoSansDevanagari.Regular);
new ComplexText(hindi, paragraphs).SetWidth(500f).SetLocation(50f, 50f).DrawOn(page);
```

HarfBuzz comes with it, through HarfBuzzSharp, for Windows, Linux (glibc
and Alpine's musl) and macOS, on x64 and arm64, such as AWS Graviton. It
gives the same glyphs at the same places as HarfBuzz's own program
`hb-shape`, which PDFjet runs without this package, where it is installed.

For .NET 8 and later and .NET Framework 4.8. MIT License.
