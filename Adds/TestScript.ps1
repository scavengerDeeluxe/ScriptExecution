# Simple test script for ScriptRunner debugging
Write-Host "Script started at $(Get-Date)"
Write-Host "Testing basic functionality..."

# Test some basic PowerShell commands
Get-Date
Get-ComputerInfo | Select-Object WindowsProductName, TotalPhysicalMemory
Get-Process | Select-Object -First 5 Name, CPU, WorkingSet

# Add a small delay to simulate work
Start-Sleep -Seconds 3

Write-Host "Script completed at $(Get-Date)"