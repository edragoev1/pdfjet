rm -rf bin
rm -rf obj
dotnet build PDFjetLib.csproj
cp bin/Debug/netcoreapp6.0/PDFjetLib.dll .
