package main

import (
	"fmt"
	"os"
	"time"

	pdfjet "github.com/edragoev1/pdfjet/src"
	"github.com/edragoev1/pdfjet/src/commandprocessor"
)

// Example95 -- Test case for the Command Processor
func Example95() error {
	pdf := pdfjet.NewPDFFile("Example_95.pdf")

	data, err := os.ReadFile("data/commands.json")
	if err != nil {
		// Log error to stderr (CloudWatch automatically collects stderr logs)
		fmt.Fprintln(os.Stderr, "Error:", err)
		return err
	}

	commandprocessor.Generate(pdf, data)
	pdf.Complete()
	return nil
}

func main() {
	start := time.Now()
	Example95()
	pdfjet.PrintDuration("Example_95", time.Since(start))
}
