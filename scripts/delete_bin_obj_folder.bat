@echo off
SETLOCAL

echo Deleting all bin and obj folders recursively...

cd ..

for /d /r . %%d in (bin,obj) do (
    if exist "%%d" (
        echo Removing %%d
        rmdir /s /q "%%d"
    )
)

echo Done.
ENDLOCAL
pause