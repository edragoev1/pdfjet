/*
 * BundledFontsTest.cs
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Xunit;

namespace PDFjet.NET {
/// <summary>
/// The fonts PDFjet carries, in resources/fonts: each has a constant in the
/// font classes, and each constant that is not obsolete names one of them,
/// which the library has.
/// </summary>
public class BundledFontsTest {
    private static readonly String[] CLASSES = {
        "IBMPlexMath", "IBMPlexMono", "IBMPlexSans", "IBMPlexSansArabic", "IBMPlexSansHebrew",
        "IBMPlexSansJP", "IBMPlexSansKR", "IBMPlexSansSC", "IBMPlexSansTC", "IBMPlexSansThai",
        "IBMPlexSerif", "JetBrainsMono", "CascadiaMono", "SourceSerif4", "NotoSans",
    };

    [Fact]
    public void EveryFontCarriedHasAConstantAndEveryConstantNotObsoleteIsCarried() {
        SortedSet<String> carried = new SortedSet<String>(StringComparer.Ordinal);
        foreach (String family in Directory.GetDirectories(TestSupport.RepoPath("resources/fonts"))) {
            foreach (String font in Directory.GetFiles(family)) {
                String name = System.IO.Path.GetFileName(font);
                if (name.EndsWith(".ttf", StringComparison.Ordinal) || name.EndsWith(".otf", StringComparison.Ordinal)) {
                    carried.Add("fonts/" + System.IO.Path.GetFileName(family) + "/" + name);
                }
            }
        }
        Assert.Equal(82, carried.Count);
        SortedSet<String> constants = new SortedSet<String>(StringComparer.Ordinal);
        foreach (String name in CLASSES) {
            Type c = typeof(Font).Assembly.GetType("PDFjet.NET." + name, true);
            foreach (FieldInfo field in c.GetFields(BindingFlags.Public | BindingFlags.Static)) {
                if (field.FieldType != typeof(String)) {
                    continue;
                }
                String path = (String) field.GetValue(null);
                bool obsolete = field.IsDefined(typeof(ObsoleteAttribute), false)
                        || c.IsDefined(typeof(ObsoleteAttribute), false);
                if (obsolete) {
                    Assert.True(!carried.Contains(path), name + "." + field.Name + " is carried");
                } else {
                    Assert.True(carried.Contains(path), name + "." + field.Name + " is not carried");
                    // Found by its path, in resources/fonts or in the library
                    using (Stream stream = BundledFonts.Open(path)) {
                        Assert.True(stream != null, path);
                    }
                    constants.Add(path);
                }
            }
        }
        Assert.Equal(carried, constants);
    }

    [Fact]
    public void AFontAddedToAnExistingPDFIsReadFromTheLibraryByItsPath() {
        // The objects of a PDF of one page, and the constant of a font PDFjet
        // carries, with no fonts folder: the font is read from the library,
        // as Font(pdf, path) reads it
        MemoryStream bos = new MemoryStream();
        PDF pdf = new PDF(bos);
        new Page(pdf, Letter.PORTRAIT);
        pdf.Complete();
        List<PDFobj> objects = TestSupport.Read(bos.ToArray());
        Font font = new Font(objects, IBMPlexSans.Bold);
        Assert.Equal("IBMPlexSans-Bold", font.name);
        // A font it does not carry says so
        FileNotFoundException e = Assert.Throws<FileNotFoundException>(
                () => new Font(objects, "fonts/NotoSans/NotoSans-Regular.ttf"));
        Assert.True(e.Message.Contains("nor one of the fonts PDFjet carries"), e.Message);
    }

    [Fact]
    public void TheFontsAreInTheLibraryOnlyWhenBuiltWithThem() {
        // test-dotnet.sh runs the tests with the small library, which reads
        // the fonts from resources/fonts, then these with -p:PDFjetFonts=true,
        // as the DLL of NuGet is built, which reads them from itself
        SortedSet<String> resources = new SortedSet<String>(StringComparer.Ordinal);
        foreach (String name in typeof(BundledFonts).Assembly.GetManifestResourceNames()) {
            if (name.StartsWith(BundledFonts.RESOURCE_PREFIX, StringComparison.Ordinal)) {
                resources.Add(name.Substring(BundledFonts.RESOURCE_PREFIX.Length));
            }
        }
        SortedSet<String> files = new SortedSet<String>(StringComparer.Ordinal);
        foreach (String family in Directory.GetDirectories(TestSupport.RepoPath("resources/fonts"))) {
            foreach (String file in Directory.GetFiles(family)) {
                String name = System.IO.Path.GetFileName(file);
                if (name.EndsWith(".ttf", StringComparison.Ordinal) || name.EndsWith(".otf", StringComparison.Ordinal)
                        || name.EndsWith(".txt", StringComparison.Ordinal) || name.Equals("LICENSE")) {
                    files.Add(System.IO.Path.GetFileName(family) + "/" + name);
                }
            }
        }
#if PDFJET_FONTS
        // The fonts and their licenses, Family/File, and nothing else
        Assert.Equal(files, resources);
        Assert.Contains("IBMPlexSans/license.txt", resources);
        foreach (String name in resources) {
            Assert.True(name.EndsWith(".ttf", StringComparison.Ordinal) || name.EndsWith(".otf", StringComparison.Ordinal)
                    || name.EndsWith(".txt", StringComparison.Ordinal) || name.EndsWith("/LICENSE", StringComparison.Ordinal), name);
        }
        // Read from the library, as with no resources/fonts folder, and drawn
        foreach (String path in new String[] {IBMPlexSans.Regular, IBMPlexSansSC.Regular, CascadiaMono.Regular}) {
            using (Stream stream = BundledFonts.OpenResource(BundledFonts.NameOf(path))) {
                Assert.True(stream != null, path);
                PDF pdf = new PDF(new MemoryStream());
                Font font = new Font(pdf, stream);
                Assert.True(font.GetAscent() > 0f, path);
            }
        }
#else
        // The small library, as built from the repository and for GitHub
        Assert.Empty(resources);
        Assert.Null(BundledFonts.OpenResource(BundledFonts.NameOf(IBMPlexSans.Regular)));
        Assert.True(files.Count > 82);
#endif
    }

    [Fact]
    public void CssWeightsMatchFacesThatLookAlikeAcrossTheFamilies() {
        // Cascadia Mono SemiLight is as thick as IBM Plex Sans Regular: it
        // stands for 400, and Regular for 500
        String[][] cases = {
            new String[] {"0", "400", "IBMPlexSans/IBMPlexSans-Regular.ttf"},
            new String[] {"0", "200", "IBMPlexSans/IBMPlexSans-Thin.ttf"},
            new String[] {"0", "800", "IBMPlexSans/IBMPlexSans-Bold.ttf"},
            new String[] {"1", "100", "SourceSerif4/SourceSerif4-Light.ttf"},
            new String[] {"1", "900", "SourceSerif4/SourceSerif4-Bold.ttf"},
            new String[] {"2", "400", "CascadiaMono/CascadiaMono-SemiLight.ttf"},
            new String[] {"2", "500", "CascadiaMono/CascadiaMono-Regular.ttf"},
            new String[] {"2", "600", "CascadiaMono/CascadiaMono-SemiBold.ttf"},
            new String[] {"2", "700", "CascadiaMono/CascadiaMono-Bold.ttf"},
        };
        foreach (String[] c in cases) {
            Assert.Equal(c[2], BundledFonts.FileName(Int32.Parse(c[0]), Int32.Parse(c[1]), false));
        }
        Assert.Equal("CascadiaMono/CascadiaMono-SemiLightItalic.ttf", BundledFonts.FileName(2, 400, true));
        Assert.Equal("CascadiaMono/CascadiaMono-Italic.ttf", BundledFonts.FileName(2, 500, true));
        Assert.Equal("IBMPlexSans/IBMPlexSans-Italic.ttf", BundledFonts.FileName(0, 400, true));
    }
}
}   // End of BundledFontsTest.cs
