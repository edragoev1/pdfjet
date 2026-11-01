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

    // The folders of the families, those of the other scripts than the
    // European ones in resources/fonts/world
    private static List<String> Families() {
        List<String> families = new List<String>();
        foreach (String family in Directory.GetDirectories(TestSupport.RepoPath("resources/fonts"))) {
            String name = System.IO.Path.GetFileName(family);
            if (name != "world") {
                families.Add(family);
            }
        }
        families.AddRange(Directory.GetDirectories(TestSupport.RepoPath("resources/fonts/world")));
        return families;
    }

    [Fact]
    public void EveryFontCarriedHasAConstantAndEveryConstantNotObsoleteIsCarried() {
        SortedSet<String> carried = new SortedSet<String>(StringComparer.Ordinal);
        foreach (String family in Families()) {
            foreach (String font in Directory.GetFiles(family)) {
                String name = System.IO.Path.GetFileName(font);
                if (name.EndsWith(".ttf", StringComparison.Ordinal) || name.EndsWith(".otf", StringComparison.Ordinal)) {
                    carried.Add("fonts/" + System.IO.Path.GetFileName(family) + "/" + name);
                }
            }
        }
        Assert.Equal(71, carried.Count);
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
        // The files of the families, and apart those of the fonts of
        // resources/fonts/world, which are in the assembly
        // PDFjet.Fonts.World, world/Family/File
        SortedSet<String> files = new SortedSet<String>(StringComparer.Ordinal);
        SortedSet<String> worldFiles = new SortedSet<String>(StringComparer.Ordinal);
        foreach (String family in Families()) {
            bool world = System.IO.Path.GetFileName(System.IO.Path.GetDirectoryName(family)) == "world";
            foreach (String file in Directory.GetFiles(family)) {
                String name = System.IO.Path.GetFileName(file);
                if (name.EndsWith(".ttf", StringComparison.Ordinal) || name.EndsWith(".otf", StringComparison.Ordinal)
                        || name.EndsWith(".txt", StringComparison.Ordinal) || name.Equals("LICENSE")) {
                    (world ? worldFiles : files).Add((world ? "world/" : "") + System.IO.Path.GetFileName(family) + "/" + name);
                }
            }
        }
        // IBM Plex Sans JP, KR, SC and TC, three faces each, and IBM Plex Sans
        // Arabic, Hebrew and Thai, seven each, with a license each
        Assert.Equal(12 + 21 + 7, worldFiles.Count);
#if PDFJET_FONTS
        // The fonts and their licenses, Family/File, and nothing else
        Assert.Equal(files, resources);
        Assert.Contains("IBMPlexSans/license.txt", resources);
        foreach (String name in resources) {
            Assert.True(name.EndsWith(".ttf", StringComparison.Ordinal) || name.EndsWith(".otf", StringComparison.Ordinal)
                    || name.EndsWith(".txt", StringComparison.Ordinal) || name.EndsWith("/LICENSE", StringComparison.Ordinal), name);
        }
        // The fonts of resources/fonts/world and their licenses are in the
        // assembly PDFjet.Fonts.World alone, which the tests have beside
        // them, as a program that adds the package does, and the library
        // loads by its name
        Assert.Null(BundledFonts.OpenResource(BundledFonts.NameOf(IBMPlexSansSC.Regular)));
        Assert.Null(BundledFonts.OpenResource(BundledFonts.NameOf(IBMPlexSansArabic.Regular)));
        Assembly worldAssembly = BundledFonts.WorldAssembly;
        Assert.NotNull(worldAssembly);
        Assert.Equal("PDFjet.Fonts.World", worldAssembly.GetName().Name);
        SortedSet<String> worldResources = new SortedSet<String>(StringComparer.Ordinal);
        foreach (String name in worldAssembly.GetManifestResourceNames()) {
            Assert.StartsWith(BundledFonts.RESOURCE_PREFIX, name);
            worldResources.Add(name.Substring(BundledFonts.RESOURCE_PREFIX.Length));
        }
        Assert.Equal(worldFiles, worldResources);
        // Read from the library, as with no resources/fonts folder, and drawn
        foreach (String path in new String[] {IBMPlexSans.Regular, IBMPlexSansSC.Regular, IBMPlexSansJP.Bold,
                IBMPlexSansThai.Regular, CascadiaMono.Regular}) {
            using (Stream stream = BundledFonts.Open(path, "no-such-folder/")) {
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
        Assert.Equal(41, files.Count);
        // Nor is the assembly PDFjet.Fonts.World beside the tests: its fonts
        // are not found without resources/fonts/world
        Assert.Null(BundledFonts.WorldAssembly);
        Assert.Null(BundledFonts.Open(IBMPlexSansJP.Regular, "no-such-folder/"));
        Assert.Null(BundledFonts.Open(IBMPlexSansHebrew.Regular, "no-such-folder/"));
#endif
    }

    [Fact]
    public void AWorldFontNotFoundNamesThePackageAndTheLineToAdd() {
        Assert.Equal("fonts/IBMPlexSansArabic/IBMPlexSansArabic-Regular.ttf is not a file, nor in resources/fonts/world. "
                + "IBM Plex Sans Arabic is in the NuGet package PDFjet.Fonts.World, with PDFjet's fonts for "
                + "Chinese, Japanese, Korean, Arabic, Hebrew and Thai: dotnet add package PDFjet.Fonts.World"
                + " --version " + PDF.VERSION
                + " (in the project file: <PackageReference Include=\"PDFjet.Fonts.World\" Version=\"" + PDF.VERSION + "\" />)",
                BundledFonts.WorldMissing(IBMPlexSansArabic.Regular));
        Assert.Matches("^[0-9]+\\.[0-9]+\\.[0-9]+$", PDF.VERSION);
        Assert.StartsWith("fonts/IBMPlexSansJP/IBMPlexSansJP-Regular.otf is not a file, nor in resources/fonts/world. "
                + "IBM Plex Sans JP is in the NuGet package PDFjet.Fonts.World", BundledFonts.WorldMissing(IBMPlexSansJP.Regular));
        Assert.Contains("IBM Plex Sans KR is in", BundledFonts.WorldMissing(IBMPlexSansKR.SemiBold));
        Assert.Contains("IBM Plex Sans Thai is in", BundledFonts.WorldMissing(IBMPlexSansThai.Thin));
        Assert.Contains("IBM Plex Sans Hebrew is in", BundledFonts.WorldMissing(IBMPlexSansHebrew.Text));
        // Not for the European fonts, nor for a face PDFjet does not carry,
        // nor for IBM Plex Math, which it no longer carries
        Assert.Contains("IBM Plex Sans KR is in", BundledFonts.WorldMissing("fonts/IBMPlexSansKR/IBMPlexSansKR-Regular.otf.stream"));
        Assert.Contains("IBM Plex Sans Thai is in", BundledFonts.WorldMissing("fonts/IBMPlexSansThai/IBMPlexSansThai-Bold.ttf.stream"));
        Assert.Contains("IBM Plex Sans JP is in", BundledFonts.WorldMissing("fonts/IBMPlexSansJP/IBMPlexSansJP-Bold.ttf.stream"));
        Assert.Null(BundledFonts.WorldMissing(IBMPlexSans.Regular));
        Assert.Null(BundledFonts.WorldMissing("fonts/IBMPlexSansJP/IBMPlexSansJP-Thin.ttf"));
        Assert.Null(BundledFonts.WorldMissing("fonts/IBMPlexSansArabic/IBMPlexSansArabic-ExtraLight.ttf"));
        Assert.Null(BundledFonts.WorldMissing("fonts/IBMPlexMath/IBMPlexMath-Regular.ttf"));
        // From the folder resources/fonts/world of the repository, the
        // working directory of the tests, first
        using (Stream stream = BundledFonts.Open(IBMPlexSansTC.Regular)) {
            Assert.IsType<FileStream>(stream);
        }
    }

    // A published app removes an assembly nothing refers to, and PDFjet loads
    // PDFjet.Fonts.World by its name: the package keeps it with a
    // TrimmerRootAssembly, for a project that uses the package directly or
    // through another.
    [Fact]
    public void ThePackageOfTheWorldFontsKeepsItsAssemblyInATrimmedApp() {
        Assert.Contains("<TrimmerRootAssembly Include=\"PDFjet.Fonts.World\" />",
                File.ReadAllText(TestSupport.RepoPath("nuget/world/buildTransitive/PDFjet.Fonts.World.props")));
        Assert.Contains("target=\"buildTransitive\"", File.ReadAllText(TestSupport.RepoPath("nuget/world/PDFjet.Fonts.World.nuspec")));
        Assert.Contains("nuget/world/buildTransitive", File.ReadAllText(TestSupport.RepoPath("package-nuget.sh")));
    }

    [Fact]
    public void CssWeightsMatchFacesThatLookAlikeAcrossTheFamilies() {
        // Cascadia Mono SemiLight is as thick as IBM Plex Sans Regular: it
        // stands for 400, and Regular for 500
        String[][] cases = {
            new String[] {"0", "400", "IBMPlexSans/IBMPlexSans-Regular.ttf"},
            new String[] {"0", "200", "IBMPlexSans/IBMPlexSans-Thin.ttf"},
            new String[] {"0", "800", "IBMPlexSans/IBMPlexSans-Bold.ttf"},
            new String[] {"1", "100", "IBMPlexSerif/IBMPlexSerif-Thin.ttf"},
            new String[] {"1", "900", "IBMPlexSerif/IBMPlexSerif-Bold.ttf"},
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
