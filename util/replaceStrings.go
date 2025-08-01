package main

import (
	"fmt"
	"os"
	"path/filepath"
	"strings"
)

// Function to replace a string in a file
func replaceInFile(filePath, oldStr, newStr string) error {
	// Read the file content
	content, err := os.ReadFile(filePath)
	if err != nil {
		return err
	}

	// Replace the old string with the new string
	updatedContent := strings.ReplaceAll(string(content), oldStr, newStr)

	// Write the updated content back to the file
	err = os.WriteFile(filePath, []byte(updatedContent), 0644)
	if err != nil {
		return err
	}

	return nil
}

// Function to walk through all files in the directory and replace string
func replaceInFiles(dir, oldStr, newStr string) error {
	// Walk through the directory and process each file
	err := filepath.Walk(dir, func(path string, info os.FileInfo, err error) error {
		if err != nil {
			return err
		}
		// Process only regular files (skip directories)
		if !info.IsDir() {
			fmt.Printf("Processing file: %s\n", path)
			return replaceInFile(path, oldStr, newStr)
		}
		return nil
	})
	return err
}

func main() {
	// Directory path (change this to your directory)
	dir := "../data"
	// The string to be replaced and the new string
	oldStr := "Copyright 2023 Innovatics Inc."
	newStr := "©2025 PDFjet Software"

	err := replaceInFiles(dir, oldStr, newStr)
	if err != nil {
		fmt.Printf("Error: %v\n", err)
		return
	}
	fmt.Println("All files processed successfully!")
}
