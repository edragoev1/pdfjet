rm -rf bin
rm -rf obj
dotnet build PDFjetLib.csproj
cp bin/Debug/netcoreapp7.0/PDFjetLib.dll .
