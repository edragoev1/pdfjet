#!/bin/sh

cd src
go build -o ../Example_$1.exe examples/example$1/main.go
cd ..

./Example_$1.exe

mupdf Example_$1.pdf