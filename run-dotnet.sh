rm -rf bin
rm -rf obj
dotnet build PDFjet.csproj /p:StartupObject=Example_$1
dotnet run
mupdf-gl Example_$1.pdf
