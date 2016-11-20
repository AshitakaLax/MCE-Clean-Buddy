' This script inserts a Custom Action into the custom action table
' Then it adds the Custom Action to the InstallExecuteSequence table
' We have written a ManageService.vbs code to stop and delete the MCEBuddy service during installation so that upgrades can go through smoothly

' Sequence of execution in the InstallExecuteSequence table
Const newSequence = "1510"

' Constant values from Windows Installer
Const msiOpenDatabaseModeTransact = 1

' This script expects 2 arguments
Dim argNum, argCount:argCount = Wscript.Arguments.Count
If (argCount < 2) Then
	Wscript.Echo "Windows Installer utility to add a ManageService.VBS file to the Custom Actions table and to the Install Sequence Table." &_
		vbLf & " The 1st argument specifies the path to the MSI database, relative or full path" &_
		vbLf & " The 2nd argument is the path of ManageService.VBS file"
	Wscript.Quit 1
End If

' Connect to Windows installer object
Dim installer : Set installer = Nothing
Set installer = Wscript.CreateObject("WindowsInstaller.Installer") : CheckError

' Open database
Dim databasePath:databasePath = Wscript.Arguments(0)
Dim database : Set database = installer.OpenDatabase(databasePath, msiOpenDatabaseModeTransact) : CheckError

Dim sql,view,record

Dim projectDir : projectDir = WScript.Arguments(1)
Dim stopfilename : stopfilename = projectDir & "\ManageService.vbs"

WScript.Echo("MSI Path --> ") & databasePath
WScript.Echo("Updating the CustomAction table...")

sql = "Insert into `CustomAction`(`Action`,`Type`,`Source`,`Target`)Values (?,?,?,?)"
Set view = database.OpenView(sql) : CheckError
Set record = installer.CreateRecord(4)
record.StringData(1) = "StopLinkFrameworkService"
record.StringData(2) = "3078"
record.StringData(3) = "ManageService.vbs"
record.StringData(4) = ""
view.Execute(record) : CheckError
view.Close()

WScript.Echo("Updating the InstallExecuteSequence table...")

sql = "Insert into `InstallExecuteSequence`(`Action`,`Condition`,`Sequence`)Values (?,?,?)"
Set view = database.OpenView(sql) : CheckError
Set record = installer.CreateRecord(3)
record.StringData(1) = "StopLinkFrameworkService"
record.StringData(2) = ""
record.StringData(3) = newSequence
view.Execute(record) : CheckError
view.Close()

WScript.Echo("Inserting into the Binary table --> ") & stopfilename

sql = "Insert into `Binary`(`Name`,`Data`)Values (?,?)"
Set view = database.OpenView(sql) : CheckError
Set record = installer.CreateRecord(2)
record.StringData(1) = "ManageService.vbs"
record.SetStream 2, stopfilename
view.Execute(record) : CheckError
view.Close()
database.Commit()


Sub CheckError
	Dim message, errRec
	If Err = 0 Then Exit Sub
	message = Err.Source & " " & Hex(Err) & ": " & Err.Description
	If Not installer Is Nothing Then
		Set errRec = installer.LastErrorRecord
		If Not errRec Is Nothing Then message = message & vbLf & errRec.FormatText
	End If
	Fail message
End Sub

Sub Fail(message)
	Wscript.Echo message
	Wscript.Quit 2
End Sub
