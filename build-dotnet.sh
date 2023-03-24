rm -rf bin

for i in $(seq 1 51);
do
    if [ $i -lt 10 ]; then
        rm -rf obj
        dotnet build PDFjet.csproj /p:StartupObject=Example_0$i
        dotnet run
    else
        rm -rf obj
        dotnet build PDFjet.csproj /p:StartupObject=Example_$i
        dotnet run
    fi
done
