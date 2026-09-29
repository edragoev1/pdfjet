@echo off
:: Gets the fonts and the data files the examples and the tests read, the
:: folders fonts and data, from the repositories pdfjet-fonts and pdfjet-data
:: at the commits fonts-and-data.txt pins, as get-fonts-and-data.sh does. The
:: build and run scripts call it, and it does nothing when the folders are at
:: the pinned commits. A folder that is missing or empty is fetched, the pinned
:: commit alone. A folder at another commit is moved to it, unless it has
:: changes of its own, which are never overwritten, or has the pinned commit in
:: its history. A folder that is not a git checkout is used as it is.
setlocal
cd /d "%~dp0"
for /f "usebackq eol=# tokens=1-3" %%a in ("fonts-and-data.txt") do (
    call :get %%a %%b %%c || exit /b 1
)
exit /b 0

:get
set "_dir=%~1"
set "_url=%~2"
set "_commit=%~3"
set _head=
if exist "%_dir%\.git" for /f %%h in ('git -C "%_dir%" rev-parse -q --verify HEAD 2^>nul') do set _head=%%h
if not defined _head goto notcheckout
if "%_head%"=="%_commit%" exit /b 0
git -C "%_dir%" merge-base --is-ancestor %_commit% HEAD >nul 2>nul && goto newer
set _changes=
for /f "delims=" %%s in ('git -C "%_dir%" status --porcelain') do set _changes=1
if defined _changes goto changes
echo Moving %_dir% from %_head% to %_commit%, the commit fonts-and-data.txt pins
git -C "%_dir%" cat-file -e "%_commit%^{commit}" 2>nul && goto checkout
set _depth=
for /f %%s in ('git -C "%_dir%" rev-parse --is-shallow-repository') do if "%%s"=="true" set "_depth=--depth 1"
git -C "%_dir%" fetch %_depth% "%_url%" %_commit% || exit /b 1
:checkout
git -C "%_dir%" checkout -q --detach %_commit% || exit /b 1
exit /b 0

:newer
echo Warning: %_dir% is at %_head%, a commit after 1>&2
echo %_commit%, the one fonts-and-data.txt pins: using it as it is. 1>&2
exit /b 0

:changes
echo %_dir% has changes of its own, and is at %_head%, not at 1>&2
echo %_commit%, the commit fonts-and-data.txt pins. Commit or undo 1>&2
echo the changes, or move %_dir% away, and run the script again. 1>&2
exit /b 1

:notcheckout
:: Files but no checkout git can read, such as a copy, are used as they are.
for /f "delims=" %%f in ('dir /b /a "%_dir%" 2^>nul') do if /i not "%%f"==".git" exit /b 0
:: Missing, empty, or a fetch that was stopped: fetch the commit alone.
echo Getting %_dir%, %_url% at %_commit%
if exist "%_dir%" rmdir /s /q "%_dir%"
git init -q "%_dir%" || goto failed
git -C "%_dir%" remote add origin "%_url%" || goto failed
git -C "%_dir%" fetch --depth 1 origin %_commit% || goto failed
git -C "%_dir%" checkout -q --detach FETCH_HEAD || goto failed
exit /b 0

:failed
if exist "%_dir%" rmdir /s /q "%_dir%"
echo Could not get %_dir% from %_url%. It needs git and a connection to GitHub. 1>&2
exit /b 1
