REM Not sure how to properly distribute the decompressed files for now,but at least this will give a hint as to how to decode the files

cd c:\Games\Ultima_5\TEMP
md dec_res
cd dec_res
for /F %%i in ('dir /b ..\*.16') DO "C:\Users\hannah\source\repos\bradhannah\Ultima5Redux\Debug\U6Decode.exe" ../%%i %%i.uncomp