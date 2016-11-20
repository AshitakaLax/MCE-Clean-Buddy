' This Script stops and delete the MCEBuddy Service
' this was made for the installer

strComputer = "."
Set objWMIService = GetObject("winmgmts:{impersonationLevel=impersonate}!\\" & strComputer & "\root\cimv2")
Set colListOfServices = objWMIService.ExecQuery ("Select * from Win32_Service Where Name = 'MCEBuddy'")
For Each objService in colListOfServices
    objService.StopService()
    objService.Delete()
Next
