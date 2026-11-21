// bundledfonts_test.go
//
// Copyright (c) 2026 PDFjet Software
// Licensed under the MIT License. See LICENSE file in the project root.

package pdfjet

import (
	"errors"
	"fmt"
	"go/ast"
	"go/parser"
	"go/token"
	"io/fs"
	"os"
	"path/filepath"
	"slices"
	"strconv"
	"strings"
	"testing"
	"testing/fstest"

	"github.com/edragoev1/pdfjet/v9/resources/fonts"
	// The fonts of the package world, built in for the tests that draw in
	// them, as a program builds them in
	_ "github.com/edragoev1/pdfjet/v9/resources/fonts/world"
	"github.com/edragoev1/pdfjet/v9/src/IBMPlexSans"
	"github.com/edragoev1/pdfjet/v9/src/IBMPlexSansArabic"
	"github.com/edragoev1/pdfjet/v9/src/IBMPlexSansHebrew"
	"github.com/edragoev1/pdfjet/v9/src/IBMPlexSansJP"
	"github.com/edragoev1/pdfjet/v9/src/IBMPlexSansKR"
	"github.com/edragoev1/pdfjet/v9/src/IBMPlexSansSC"
	"github.com/edragoev1/pdfjet/v9/src/IBMPlexSansTC"
	"github.com/edragoev1/pdfjet/v9/src/IBMPlexSansThai"
	"github.com/edragoev1/pdfjet/v9/src/NotoSansDevanagari"
	"github.com/edragoev1/pdfjet/v9/src/NotoSansKhmer"
	"github.com/edragoev1/pdfjet/v9/src/letter"
)

// The fonts PDFjet carries, in resources/fonts: each has a constant in the
// packages of the fonts, and each constant that is not deprecated names one
// of them, which the library has.

var testFontPackages = []string{
	"IBMPlexMath", "IBMPlexMono", "IBMPlexSans", "IBMPlexSansArabic", "IBMPlexSansHebrew",
	"IBMPlexSansJP", "IBMPlexSansKR", "IBMPlexSansSC", "IBMPlexSansTC", "IBMPlexSansThai",
	"IBMPlexSerif", "JetBrainsMono", "CascadiaMono", "SourceSerif4", "NotoSans",
	"NotoSansBengali", "NotoSansDevanagari", "NotoSansGujarati", "NotoSansGurmukhi", "NotoSansKannada",
	"NotoSansKhmer", "NotoSansLao", "NotoSansMalayalam", "NotoSansMyanmar", "NotoSansOriya",
	"NotoSansSinhala", "NotoSansTamil", "NotoSansTelugu",
}

// testIsDeprecated tells if the doc comment has a paragraph that starts
// with "Deprecated:", as go doc and the tools read it.
func testIsDeprecated(doc *ast.CommentGroup) bool {
	if doc == nil {
		return false
	}
	for _, paragraph := range strings.Split(doc.Text(), "\n\n") {
		if strings.HasPrefix(paragraph, "Deprecated:") {
			return true
		}
	}
	return false
}

func TestBundledFontsEveryFontCarriedHasAConstantAndEveryConstantNotDeprecatedIsCarried(t *testing.T) {
	var carried []string
	root := testRepoPath(t, "resources/fonts")
	families, err := os.ReadDir(root)
	if err != nil {
		t.Fatal(err)
	}
	// The family folders, and those of the other scripts in the folder
	// world; the folder cjk is the deprecated package cjk alone
	var folders []string
	for _, family := range families {
		if family.IsDir() && family.Name() != "world" {
			folders = append(folders, family.Name())
		}
	}
	world, err := os.ReadDir(filepath.Join(root, "world"))
	if err != nil {
		t.Fatal(err)
	}
	for _, family := range world {
		if family.IsDir() {
			folders = append(folders, "world/"+family.Name())
		}
	}
	for _, folder := range folders {
		files, err := os.ReadDir(filepath.Join(root, filepath.FromSlash(folder)))
		if err != nil {
			t.Fatal(err)
		}
		family := filepath.Base(folder)
		for _, file := range files {
			if strings.HasSuffix(file.Name(), ".ttf") || strings.HasSuffix(file.Name(), ".otf") {
				carried = append(carried, "fonts/"+family+"/"+file.Name())
			}
		}
	}
	slices.Sort(carried)
	if len(carried) != 97 {
		t.Fatalf("%d fonts carried, not 97", len(carried))
	}
	var constants []string
	deprecated := 0
	for _, name := range testFontPackages {
		dir := filepath.Join(testRepoPath(t, "src"), name)
		sources, err := filepath.Glob(filepath.Join(dir, "*.go"))
		if err != nil || len(sources) == 0 {
			t.Fatalf("no package %s: %v", name, err)
		}
		count := 0
		for _, source := range sources {
			file, err := parser.ParseFile(token.NewFileSet(), source, nil, parser.ParseComments)
			if err != nil {
				t.Fatal(err)
			}
			deprecatedPackage := testIsDeprecated(file.Doc)
			for _, decl := range file.Decls {
				gen, ok := decl.(*ast.GenDecl)
				if !ok || gen.Tok != token.CONST {
					continue
				}
				for _, spec := range gen.Specs {
					value := spec.(*ast.ValueSpec)
					path, err := strconv.Unquote(value.Values[0].(*ast.BasicLit).Value)
					if err != nil {
						t.Fatal(err)
					}
					constant := name + "." + value.Names[0].Name
					count++
					_, isCarried := slices.BinarySearch(carried, path)
					if deprecatedPackage || testIsDeprecated(value.Doc) {
						deprecated++
						if isCarried {
							t.Errorf("%s is deprecated and carried", constant)
						}
						continue
					}
					if !isCarried {
						t.Errorf("%s is not carried", constant)
						continue
					}
					// In the library, as the module has it, not only in the folder:
					// the fonts registered by the package world, which this
					// test imports, and the European ones built in always
					if buf, err := fonts.ReadFile(bundledFontName(path)); err != nil || len(buf) == 0 {
						t.Errorf("%s is not in the library: %v", constant, err)
					}
					family, _, _ := strings.Cut(bundledFontName(path), "/")
					_, err = fonts.FS.ReadFile(bundledFontName(path))
					_, world := worldFaces[family]
					if builtIn := err == nil; builtIn == world {
						t.Errorf("%s built in always: %v", constant, builtIn)
					}
					constants = append(constants, path)
				}
			}
		}
		if count == 0 {
			t.Errorf("no constants in %s", name)
		}
	}
	// IBM Plex Mono, JetBrains Mono, Noto Sans, Source Serif 4 and IBM Plex
	// Math, and the weights not carried of the other families
	if deprecated != 16+12+18+14+1+25 {
		t.Errorf("%d constants deprecated", deprecated)
	}
	slices.Sort(constants)
	if !slices.Equal(carried, constants) {
		t.Errorf("the fonts carried\n%v\nand the constants\n%v", carried, constants)
	}
}

func TestBundledFontsAFontAddedToAnExistingPDFIsReadFromTheLibraryByItsPath(t *testing.T) {
	// The objects of a PDF of one page, and the constant of a font PDFjet
	// carries, with no fonts folder: the font is read from the library, as
	// NewFontFromFile reads it
	doc := testNewDoc()
	NewPage(doc.pdf, letter.Portrait())
	objects := testRead(t, doc.complete())
	font := NewFontFromFileForObjects(&objects, IBMPlexSans.Bold)
	testWant(t, "IBMPlexSans-Bold", font.GetName())
	// A font it does not carry says to use a font file of one's own
	defer func() {
		err, ok := recover().(error)
		if !ok || !errors.Is(err, fs.ErrNotExist) || !strings.Contains(err.Error(), "use a font file of your own") {
			t.Errorf("%v", err)
		}
	}()
	NewFontFromFileForObjects(&objects, "fonts/NotoSans/NotoSans-Regular.ttf")
	t.Error("a font PDFjet does not carry was read")
}

func TestBundledFontsTheWorldFontsAreReadFromTheLibraryWhenTheProgramImportsThePackageWorld(t *testing.T) {
	// A working directory with no folder resources/fonts: the font is the
	// one the package world, which this test imports, builds in
	t.Chdir(t.TempDir())
	doc := testNewDoc()
	for _, path := range []string{IBMPlexSansJP.Regular, IBMPlexSansKR.SemiBold, IBMPlexSansSC.Bold, IBMPlexSansTC.Regular,
		IBMPlexSansArabic.Regular, NotoSansDevanagari.Regular, NotoSansKhmer.Bold, IBMPlexSansHebrew.Thin, IBMPlexSansThai.Text} {
		font := NewFontFromFile(doc.pdf, path)
		if family := filepath.Base(filepath.Dir(path)); !strings.HasPrefix(font.GetName(), family) {
			t.Errorf("%s: the font %s", path, font.GetName())
		}
	}
	// Built in, none is missing; nor is a face PDFjet does not carry, nor
	// IBM Plex Math, which it no longer carries
	for _, path := range []string{IBMPlexSansJP.Regular, IBMPlexSansArabic.Bold,
		"fonts/IBMPlexSansJP/IBMPlexSansJP-Thin.ttf", "fonts/IBMPlexMath/IBMPlexMath-Regular.ttf"} {
		if message := bundledWorldMissing(path); message != "" {
			t.Errorf("%s: %s", path, message)
		}
	}
	testWant(t, "IBM Plex Sans Arabic", bundledFamilyName("IBMPlexSansArabic"))
	testWant(t, "IBM Plex Sans JP", bundledFamilyName("IBMPlexSansJP"))
}

func TestBundledFontsAPathOfTheOldStreamFilesOfAWorldFontIsNotAWorldFont(t *testing.T) {
	// As in a program that does not import the package world: a .stream
	// path is a path like any other, not the font of that name
	family := "IBMPlexSansKR"
	folder := os.DirFS(testRepoPath(t, "resources/fonts/world/"+family))
	fonts.Register(family, fstest.MapFS{})
	t.Cleanup(func() { fonts.Register(family, folder) })
	t.Chdir(t.TempDir())
	_, err := readFontFile("fonts/IBMPlexSansKR/IBMPlexSansKR-Regular.otf.stream")
	if !errors.Is(err, fs.ErrNotExist) || !strings.Contains(err.Error(), "nor one of the fonts PDFjet carries") {
		t.Errorf("%v", err)
	}
	testWant(t, "", bundledWorldMissing("fonts/IBMPlexSansKR/IBMPlexSansKR-Bold.ttf.stream"))
}

func TestBundledFontsAPathToTheTTFOfAFontPDFjetCarriesAsOTFNamesTheOTF(t *testing.T) {
	// The fonts PDFjet carries are .otf files, and a path loads the file it
	// names: a path to the .ttf of one of them, which is not a file, is not
	// found, and the error names the .otf path and the constant to write; a
	// .ttf of one's own at that path, or of that name in resources/fonts, is
	// read as any file is
	ttf, err := os.ReadFile(testRepoPath(t, "tests/fonts/IBMPlexSans-Regular.ttf"))
	if err != nil {
		t.Fatal(err)
	}
	t.Chdir(t.TempDir())
	_, err = ReadFontFromFile(testNewPDF(), "fonts/CascadiaMono/CascadiaMono-Regular.ttf")
	if !errors.Is(err, fs.ErrNotExist) {
		t.Errorf("not fs.ErrNotExist: %v", err)
	}
	testWant(t, "No bundled font fonts/CascadiaMono/CascadiaMono-Regular.ttf; PDFjet carries it as "+
		"fonts/CascadiaMono/CascadiaMono-Regular.otf (or use the constant CascadiaMono.Regular).", fmt.Sprint(err))
	for _, face := range [][2]string{
		{"fonts/IBMPlexSans/IBMPlexSans-Regular.otf", "IBMPlexSans.Regular"},
		{"fonts/IBMPlexSans/IBMPlexSans-Italic.otf", "IBMPlexSans.Italic"},
		{"fonts/IBMPlexSans/IBMPlexSans-ThinItalic.otf", "IBMPlexSans.ThinItalic"},
		{"fonts/IBMPlexSerif/IBMPlexSerif-SemiBold.otf", "IBMPlexSerif.SemiBold"},
		{"fonts/CascadiaMono/CascadiaMono-SemiLightItalic.otf", "CascadiaMono.SemiLightItalic"},
		{"fonts/IBMPlexSansArabic/IBMPlexSansArabic-Regular.otf", "IBMPlexSansArabic.Regular"},
		{"fonts/IBMPlexSansHebrew/IBMPlexSansHebrew-Thin.otf", "IBMPlexSansHebrew.Thin"},
		{"fonts/IBMPlexSansThai/IBMPlexSansThai-Bold.otf", "IBMPlexSansThai.Bold"},
		{"fonts/IBMPlexSansJP/IBMPlexSansJP-Regular.otf", "IBMPlexSansJP.Regular"},
	} {
		path := strings.TrimSuffix(face[0], ".otf") + ".ttf"
		if bundledFontHas(path) || bundledFontData(path) != nil {
			t.Errorf("%s: carried", path)
		}
		_, err := readFontFile(path)
		testWant(t, "No bundled font "+path+"; PDFjet carries it as "+face[0]+
			" (or use the constant "+face[1]+").", fmt.Sprint(err))
	}
	// A face PDFjet does not carry, and a font of a family it does not
	// carry, are not found, as before, with no .otf to name
	for _, path := range []string{"fonts/IBMPlexSans/IBMPlexSans-ExtraLight.ttf",
		"fonts/CascadiaMono/CascadiaMono-ExtraLight.ttf", "fonts/IBMPlexMono/IBMPlexMono-Regular.ttf",
		"fonts/IBMPlexSansArabic/IBMPlexSansArabic-ExtraLight.ttf", "fonts/IBMPlexSans/IBMPlexSans-Regular.woff2"} {
		testWant(t, "", bundledOtfInstead(path))
		if _, err := readFontFile(path); !errors.Is(err, fs.ErrNotExist) ||
			!strings.Contains(err.Error(), "nor one of the fonts PDFjet carries") {
			t.Errorf("%s: %v", path, err)
		}
	}
	testWant(t, "", bundledOtfInstead("fonts/IBMPlexSans/IBMPlexSans-Regular.otf"))
	testWant(t, "[IBMPlexSans/IBMPlexSans-Regular.ttf]",
		fmt.Sprint(bundledFontNamesOf("fonts/IBMPlexSans/IBMPlexSans-Regular.ttf")))
	testWant(t, "[IBMPlexSans/IBMPlexSans-Regular.otf]",
		fmt.Sprint(bundledFontNamesOf("fonts/IBMPlexSans/IBMPlexSans-Regular.otf")))
	// A .ttf of one's own at that path is read, the file first, and so is
	// one of that name in resources/fonts
	for _, path := range []string{"fonts/IBMPlexSans/IBMPlexSans-Regular.ttf",
		"resources/fonts/IBMPlexSerif/IBMPlexSerif-Regular.ttf"} {
		if err := os.MkdirAll(filepath.Dir(path), 0o755); err != nil {
			t.Fatal(err)
		}
		if err := os.WriteFile(path, ttf, 0o644); err != nil {
			t.Fatal(err)
		}
	}
	for _, path := range []string{"fonts/IBMPlexSans/IBMPlexSans-Regular.ttf", "fonts/IBMPlexSerif/IBMPlexSerif-Regular.ttf"} {
		if buf, err := readFontFile(path); err != nil || !slices.Equal(buf, ttf) {
			t.Errorf("%s: not the file (%v)", path, err)
		}
	}
}

func TestBundledFontsCSSWeightsMatchFacesThatLookAlikeAcrossTheFamilies(t *testing.T) {
	// Cascadia Mono SemiLight is as thick as IBM Plex Sans Regular: it stands
	// for 400, and Regular for 500
	for _, c := range []struct {
		family, weight int
		want           string
	}{
		{bundledSans, 400, "IBMPlexSans/IBMPlexSans-Regular.otf"},
		{bundledSans, 200, "IBMPlexSans/IBMPlexSans-Thin.otf"},
		{bundledSans, 800, "IBMPlexSans/IBMPlexSans-Bold.otf"},
		{bundledSerif, 100, "IBMPlexSerif/IBMPlexSerif-Thin.otf"},
		{bundledSerif, 900, "IBMPlexSerif/IBMPlexSerif-Bold.otf"},
		{bundledMono, 400, "CascadiaMono/CascadiaMono-SemiLight.otf"},
		{bundledMono, 500, "CascadiaMono/CascadiaMono-Regular.otf"},
		{bundledMono, 600, "CascadiaMono/CascadiaMono-SemiBold.otf"},
		{bundledMono, 700, "CascadiaMono/CascadiaMono-Bold.otf"},
	} {
		testWant(t, c.want, bundledFontFile(c.family, c.weight, false))
	}
	testWant(t, "CascadiaMono/CascadiaMono-SemiLightItalic.otf", bundledFontFile(bundledMono, 400, true))
	testWant(t, "CascadiaMono/CascadiaMono-Italic.otf", bundledFontFile(bundledMono, 500, true))
	testWant(t, "IBMPlexSans/IBMPlexSans-Italic.otf", bundledFontFile(bundledSans, 400, true))
}
