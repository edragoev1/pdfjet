rm -rf bin
rm -rf obj
dotnet build PDFjetLib.csproj
cp bin/Debug/netcoreapp6.0/PDFjetLib.dll .

# rm -rf bin
# rm -rf obj
dotnet build PDFjet.csproj /p:StartupObject=Example_$1
dotnet run --project PDFjet.csproj
mupdf Example_$1.pdf
