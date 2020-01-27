"
Source: https://github.com/deviousasti/build-bot
Author: https://github.com/deviousasti 
Maintainer: https://github.com/deviousasti 


VERIFICATION
------------

To verify, run
ls *.dll,*.exe | Sort-Object -Property Name | Get-FileHash | Format-Table Hash, @{ Label = 'File'; Expression = { $_.Path | Split-Path -Leaf }}

And compare with the file hashes below.

A copy of this file can be found at:
https://raw.githubusercontent.com/deviousasti/build-bot/master/VERIFICATION.txt

File Hashes:
" | Set-Content ".\VERIFICATION.txt"

Get-ChildItem -Path ".\src\bin\Debug\netcoreapp3.0" | 
Where-Object  {$_.Name -like "*.dll" -or $_.Name -like "*.exe"} |
Sort-Object -Property Name |
Get-FileHash -Algorithm SHA256 | 
Format-Table Hash, @{ Label = 'File'; Expression = { $_.Path | Split-Path -Leaf }} |
Out-String |
Add-Content ".\VERIFICATION.txt"

cat .\VERIFICATION.txt

