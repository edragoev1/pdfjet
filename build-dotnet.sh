# Very important!!
./clean.sh

for i in $(seq 1 50);
do
    if [ $i -lt 10 ]; then
        rm -rf obj
        dotnet build PDFjet.csproj /p:StartupObject=Example_0$i
        dotnet run
        echo ""
    else
        rm -rf obj
        dotnet build PDFjet.csproj /p:StartupObject=Example_$i
        dotnet run
        echo ""
    fi
done
