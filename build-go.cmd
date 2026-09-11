@echo off

:: Navigate to the "src" directory
cd src

:: The Go compiler has no warnings; go vet reports the suspicious code instead.
:: The script stops if it finds a problem.
go vet ./...
if errorlevel 1 (
    cd ..
    exit /b 1
)

:: Loop from 1 to 50
for /L %%i in (1,1,50) do (
    :: Check if %%i is less than 10
    if %%i lss 10 (
        go build -o ..\Example_0%%i.exe examples\example0%%i\main.go
    ) else (
        go build -o ..\Example_%%i.exe examples\example%%i\main.go
    )
)

:: Navigate back to the parent directory
cd ..

:: Run the .exe files
for /L %%i in (1,1,50) do (
    :: Check if %%i is less than 10
    if %%i lss 10 (
        Example_0%%i.exe
    ) else (
        Example_%%i.exe
    )
)
