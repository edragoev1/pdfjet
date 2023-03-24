rm -rf bin
rm -rf obj
dotnet build PDFjet.csproj /p:StartupObject=Example_$1
dotnet run
# mupdf Example_$1.pdf
evince Example_$1.pdf
