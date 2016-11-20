@echo off
echo DO NOT CLOSE!!! - Waiting for 10 seconds before installing ShowAnalyzer (MCEBuddy should complete install to avoid conflict)
ping 1.1.1.1 -n 1 -w 10000 > nul
ShowAnalyzerSuite.msi /passive
