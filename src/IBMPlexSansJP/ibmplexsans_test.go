// ibmplexsans_test.go
//
// Copyright (c) 2026 PDFjet Software
// Licensed under the MIT License. See LICENSE file in the project root.

package IBMPlexSansJP_test

import (
	"bufio"
	"errors"
	"io"
	"io/fs"
	"os"
	"path/filepath"
	"strings"
	"testing"

	"github.com/edragoev1/pdfjet/v9/resources/fonts"
	pdfjet "github.com/edragoev1/pdfjet/v9/src"
	"github.com/edragoev1/pdfjet/v9/src/IBMPlexSansJP"
)

// These tests do not import the package cjk, as a program that does not:
// the CJK fonts are not built in.

// testRepoRoot returns the root of the repository, the folder above the
// working directory that has tests/data/PngSuite.
func testRepoRoot(t *testing.T) string {
	t.Helper()
	dir, err := os.Getwd()
	if err != nil {
		t.Fatal(err)
	}
	for {
		if info, err := os.Stat(filepath.Join(dir, "tests/data/PngSuite")); err == nil && info.IsDir() {
			return dir
		}
		parent := filepath.Dir(dir)
		if parent == dir {
			t.Fatal("no tests/data/PngSuite directory above the working directory")
		}
		dir = parent
	}
}

func TestACJKFontIsReadFromTheFolderResourcesFontsWhenTheProgramDoesNotImportThePackageCJK(t *testing.T) {
	if _, err := fonts.Stat("IBMPlexSansJP/IBMPlexSansJP-Regular.otf"); err == nil {
		t.Fatal("IBM Plex Sans JP is built in without the package cjk")
	}
	t.Chdir(testRepoRoot(t))
	pdf := pdfjet.NewPDF(bufio.NewWriter(io.Discard))
	font := pdfjet.NewFontFromFile(pdf, IBMPlexSansJP.Regular)
	if !strings.HasPrefix(font.GetName(), "IBMPlexSansJP") {
		t.Errorf("the font %s", font.GetName())
	}
}

func TestACJKFontNotInResourcesFontsNamesTheImportLineWhenTheProgramDoesNotImportThePackageCJK(t *testing.T) {
	t.Chdir(t.TempDir())
	pdf := pdfjet.NewPDF(bufio.NewWriter(io.Discard))
	defer func() {
		err, ok := recover().(error)
		if !ok || !errors.Is(err, fs.ErrNotExist) ||
			!strings.Contains(err.Error(), `import _ "github.com/edragoev1/pdfjet/v9/resources/fonts/cjk"`) {
			t.Errorf("%v", err)
		}
	}()
	pdfjet.NewFontFromFile(pdf, IBMPlexSansJP.Bold)
	t.Error("a CJK font was read with neither the package cjk nor resources/fonts")
}
