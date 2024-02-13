rem %1 = NOMESERVER %2=NomeDatabase %3=NomeFileDiRestore
sqlcmd -E -S.\%1 -Q "RESTORE DATABASE [%2] FROM DISK=N'%3'" 



 
