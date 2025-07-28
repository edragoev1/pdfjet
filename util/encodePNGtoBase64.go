package main

import (
	"encoding/base64"
	"fmt"
	"log"
	"os"
)

// Function to read the file and encode it in base64
func encodeFileToBase64(filePath string) (string, error) {
	// Read the PNG file
	fileContent, err := os.ReadFile(filePath)
	if err != nil {
		return "", err
	}

	// Encode the file content to base64
	encoded := base64.StdEncoding.EncodeToString(fileContent)
	return encoded, nil
}

func main() {
	filePath := "../images/svg/shopping_bag_FILL0_wght400_GRAD0_opsz48.svg"
	encoded, err := encodeFileToBase64(filePath)
	if err != nil {
		log.Fatal(err)
	}

	// Print the base64 encoded PNG in JSON-like format
	// This JSON format includes the base64 data directly
	fmt.Println("{")
	fmt.Println(`  "image": {`)
	fmt.Println(`    "name": "image.svg",`)
	fmt.Println(`    "format": "svg",`)
	fmt.Println(`    "data": "` + encoded + `"`)
	fmt.Println("  }")
	fmt.Println("}")
}
