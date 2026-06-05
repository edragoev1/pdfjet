package pdfjet

/**
 * textutils.go
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */

import (
	"bufio"
	"fmt"
	"log"
	"os"
	"time"
)

func ReadTextLines(filePath string) []string {
	lines := make([]string, 0)
	file, err := os.Open(filePath)
	if err != nil {
		log.Fatal(err)
	}
	scanner := bufio.NewScanner(file)
	for scanner.Scan() {
		line := scanner.Text()
		lines = append(lines, line)
	}
	return lines
}

func PrintDuration(example string, duration time.Duration) {
	durationAsString := fmt.Sprintf("%.1f", float32(duration.Microseconds())/float32(1000.0))
	if len(durationAsString) == 3 {
		durationAsString = "    " + durationAsString
	} else if len(durationAsString) == 4 {
		durationAsString = "   " + durationAsString
	} else if len(durationAsString) == 5 {
		durationAsString = "  " + durationAsString
	} else if len(durationAsString) == 6 {
		durationAsString = " " + durationAsString
	}
	fmt.Print(example + " => " + durationAsString + "\n")
}
