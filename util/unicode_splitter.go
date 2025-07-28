package main

import (
	"encoding/json"
	"fmt"
	"os"
	"strings"
)

// Optimized version
func splitStringOptimized(s string, maxLen int) []string {
	if maxLen <= 0 {
		return nil // Safer!
	}
	runes := []rune(s)
	chunks := make([]string, 0, (len(runes)+maxLen-1)/maxLen) // Correct capacity calculation
	for i := 0; i < len(runes); i += maxLen {
		j := i + maxLen
		if j > len(runes) { j = len(runes) }
		chunks = append(chunks, string(runes[i:j]))
	}
	return chunks
}

func splitString(s string, maxLen int) []string {
    if maxLen <= 0 { return nil }   // Safer!
    runes := []rune(s)
    chunks := make([]string, 0, (len(runes) + maxLen - 1)/maxLen)
    for len(runes) >= maxLen {
        chunks = append(chunks, string(runes[:maxLen]))
        runes = runes[maxLen:]      // More cache-friendly
    }
    if len(runes) > 0 {
        chunks = append(chunks, string(runes))
    }
    return chunks
}

func splitStringAppend(s string, maxLen int) []string {
	var chunks []string
	buffer := make([]rune, 0, maxLen)
	runes := []rune(s)
	for _, r := range runes {
		buffer = append(buffer, r)
		if len(buffer) == maxLen {
			chunks = append(chunks, string(buffer))
			buffer = buffer[:0]
		}
	}
	chunks = append(chunks, string(buffer))
	return chunks
}

func main() {
	// Example with Unicode text (emojis, Chinese, etc.)
	longText := `Go语言很好用！🚀 We love Unicode! こんにちは世界！This string contains multibyte characters that must be split properly.`

	// Split into 72-rune chunks
	chunks := splitStringOptimized(longText, 72)

	// Save to JSON
	jsonData, err := json.MarshalIndent(map[string]any{
		"original_length": len([]rune(longText)),
		"chunk_size":      72,
		"chunks":          chunks,
	}, "", "  ")

	if err != nil {
		fmt.Fprintf(os.Stderr, "JSON error: %v\n", err)
		os.Exit(1)
	}

	fmt.Println("Split JSON:")
	fmt.Println(string(jsonData))

	reconstructed := strings.Join(chunks, "")
	fmt.Println("\nReconstructed string:")
	fmt.Println(reconstructed)

	// Verify
	if reconstructed != longText {
		fmt.Println("\n⚠️ Reconstruction failed! Strings differ.")
	} else {
		fmt.Println("\n✓ Perfect reconstruction!")
	}
}
