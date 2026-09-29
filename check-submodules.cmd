@echo off
:: Called by the build and run scripts. The fonts and the data files the
:: examples read are the git submodules fonts and data, the repositories
:: pdfjet-fonts and pdfjet-data. A clone made without --recurse-submodules has
:: them as empty directories, so stop with the command that checks them out.
if not exist "%~dp0fonts\README.md" goto missing
if not exist "%~dp0data\README.md" goto missing
:: A git pull that moves a submodule to another commit leaves its files as they
:: were, unless submodule.recurse is set: say so, and go on with them.
set _stale=
for /f "delims=" %%s in ('git -C "%~dp0." submodule status fonts data 2^>nul ^| findstr /b /l "+"') do set _stale=1
if defined _stale echo Warning: fonts or data is not at the commit this checkout records, as after 1>&2
if defined _stale echo a git pull that moved it. Update them with: git submodule update 1>&2
set _stale=
exit /b 0
:missing
echo The fonts or the data directory is empty: they are the git submodules 1>&2
echo pdfjet-fonts and pdfjet-data. Check them out with: 1>&2
echo. 1>&2
echo     git submodule update --init 1>&2
echo. 1>&2
echo or clone with: git clone --recurse-submodules https://github.com/edragoev1/pdfjet.git 1>&2
exit /b 1
