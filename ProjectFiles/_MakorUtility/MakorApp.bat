SET PATH_PROGRAM="C:\Makor\Progetto"
taskkill /IM Flexy_SV.exe
taskkill /F /IM OPCComm.exe

cd %PATH_PROGRAM%\Flexi_SV
START /B Flexy_SV.exe
cd %PATH_PROGRAM%\OPCComm
START /B OPCComm.exe &