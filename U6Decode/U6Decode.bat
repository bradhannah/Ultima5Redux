REM Not sure how to properly distribute the decompressed files for now,but at least this will give a hint as to how to decode the files

cd MYULTIMADIR
md dec_res
cd dec_res
for /F %%i in ('dir /b ..\*.16') DO "u6decode.exe" ../%%i %%i.uncomp