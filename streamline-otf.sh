#!/bin/bash
# This script streamlines the TTF or OTF fonts found in the specified directory:
# java -cp .:PDFjet.jar com.pdfjet.OTF fonts/Ubuntu
java -cp .:PDFjet.jar com.pdfjet.OTF $1
