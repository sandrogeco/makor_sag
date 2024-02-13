rem %1 = NOMESERVER %2=NomeDatabase %3=NomeFileDaSalvare
rem @echo off
sqlcmd -E -S%1 -d %2 -Q "BACKUP DATABASE [%2] TO DISK=N'%3' WITH  INIT , NOSKIP ,  NOFORMAT"
rem pause




 
