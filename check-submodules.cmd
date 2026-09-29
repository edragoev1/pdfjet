@echo off
:: Called by the build and run scripts. The fonts and the data files the
:: examples read are the git submodules fonts and data, the repositories
:: pdfjet-fonts and pdfjet-data. A clone made without --recurse-submodules has
:: them as empty directories, so stop with the command that checks them out.
if not exist "%~dp0fonts\README.md" goto missing
if not exist "%~dp0data\README.md" goto missing
exit /b 0
:missing
echo The fonts or the data directory is empty: they are the git submodules 1>&2
echo pdfjet-fonts and pdfjet-data. Check them out with: 1>&2
echo. 1>&2
echo     git submodule update --init 1>&2
echo. 1>&2
echo or clone with: git clone --recurse-submodules https://github.com/edragoev1/pdfjet.git 1>&2
exit /b 1
