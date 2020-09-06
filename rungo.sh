#!/bin/sh
go build -o Example_$1.exe src/example$1/main.go
./Example_$1.exe
# mupdf-gl Example_$1.pdf
evince Example_$1.pdf
