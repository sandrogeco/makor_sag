rem %1 = NOMESERVER %2=AuthenticationUserName %3=Password %4=NomeDatabase %5=NomeFileDaSalvare
rem sqlcmd -S localhost -U sa -P makor7325 -d SupervisoreLinea -Q "BACKUP DATABASE [SupervisoreLinea] TO DISK=N'C:\Makor\Test.bak' WITH  INIT , NOSKIP ,  NOFORMAT"
rem @echo off
sqlcmd -S%1 -U %2 -P %3 -d %4 -Q "BACKUP DATABASE [%4] TO DISK=N'%5' WITH  INIT , NOSKIP ,  NOFORMAT"
rem pause




 
