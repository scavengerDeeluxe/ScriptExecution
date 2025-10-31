# Stop the Windows Management Instrumentation (WMI) service
Stop-Service -Name winmgmt -Force

# Change to the WBEM directory
Set-Location "$env:windir\system32\wbem"

# Remove the WMI repository folder
Remove-Item -Path "repository" -Recurse -Force

# Re-register system DLLs silently
Start-Process -FilePath "regsvr32.exe" -ArgumentList "/s $env:systemroot\system32\scecli.dll" -Wait
Start-Process -FilePath "regsvr32.exe" -ArgumentList "/s $env:systemroot\system32\userenv.dll" -Wait

# Compile key MOF/MFL files
$mofFiles = @(
    "cimwin32.mof",
    "cimwin32.mfl",
    "rsop.mof",
    "rsop.mfl"
)

foreach ($file in $mofFiles) {
    mofcomp $file
}

# Re-register all DLLs in the directory and subdirectories
Get-ChildItem -Path . -Recurse -Filter *.dll | ForEach-Object {
    Start-Process -FilePath "regsvr32.exe" -ArgumentList "/s `"$($_.FullName)`"" -Wait
}

# Compile all MOF files in the current directory
Get-ChildItem -Path . -Filter *.mof | ForEach-Object {
    mofcomp $_.FullName
}

# Compile all MFL files in the current directory
Get-ChildItem -Path . -Filter *.mfl | ForEach-Object {
    mofcomp $_.FullName
}

# Compile Exchange-specific MOFs (if present)
$mofExchange = @(
    @{File="wbemcons.mof"; Namespace="root\cimv2\applications\exchange"},
    @{File="smtpcons.mof"; Namespace="root\cimv2\applications\exchange"},
    @{File="exmgmt.mof"; Namespace=$null},
    @{File="exwmi.mof"; Namespace=$null}
)

foreach ($item in $mofExchange) {
    if (Test-Path $item.File) {
        if ($item.Namespace) {
            mofcomp -n:$item.Namespace $item.File
        } else {
            mofcomp $item.File
        }
    }
}

# Start the WMI service again
Start-Service -Name winmgmt

taskkill /im ccmexec.exe /f
copy -Recurse -force  C:\windows\ccmsetup C:\windows\ccmsetup.bak

# Run SSCM remove
# $ccmpath is path to SCCM Agent's own uninstall routine.
$CCMpath = 'C:\Windows\ccmsetup\ccmsetup.exe'
# And if it exists we will remove it, or else we will silently fail.
if (Test-Path $CCMpath) {

    Start-Process -FilePath $CCMpath -Args "/uninstall" -Wait -NoNewWindow
    # wait for exit

    $CCMProcess = Get-Process ccmsetup -ErrorAction SilentlyContinue

        try{
            $CCMProcess.WaitForExit()
            }catch{
 

            }
}


# Stop Services
Stop-Service -Name ccmsetup -Force -ErrorAction SilentlyContinue
Stop-Service -Name CcmExec -Force -ErrorAction SilentlyContinue
Stop-Service -Name smstsmgr -Force -ErrorAction SilentlyContinue
Stop-Service -Name CmRcService -Force -ErrorAction SilentlyContinue

# wait for services to exit
$CCMProcess = Get-Process ccmexec -ErrorAction SilentlyContinue
try{

    $CCMProcess.WaitForExit()

}catch{


}

 
# Remove WMI Namespaces
Get-WmiObject -Query "SELECT * FROM __Namespace WHERE Name='ccm'" -Namespace root | Remove-WmiObject
Get-WmiObject -Query "SELECT * FROM __Namespace WHERE Name='sms'" -Namespace root\cimv2 | Remove-WmiObject

# Remove Services from Registry
# Set $CurrentPath to services registry keys
$CurrentPath = “HKLM:\SYSTEM\CurrentControlSet\Services”
Remove-Item -Path $CurrentPath\CCMSetup -Force -Recurse -ErrorAction SilentlyContinue
Remove-Item -Path $CurrentPath\CcmExec -Force -Recurse -ErrorAction SilentlyContinue
Remove-Item -Path $CurrentPath\smstsmgr -Force -Recurse -ErrorAction SilentlyContinue
Remove-Item -Path $CurrentPath\CmRcService -Force -Recurse -ErrorAction SilentlyContinue

# Remove SCCM Client from Registry
# Update $CurrentPath to HKLM/Software/Microsoft
$CurrentPath = “HKLM:\SOFTWARE\Microsoft”
Remove-Item -Path $CurrentPath\CCM -Force -Recurse -ErrorAction SilentlyContinue
Remove-Item -Path $CurrentPath\CCMSetup -Force -Recurse -ErrorAction SilentlyContinue
Remove-Item -Path $CurrentPath\SMS -Force -Recurse -ErrorAction SilentlyContinue

# Reset MDM Authority
# CurrentPath should still be correct, we are removing this key: HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\DeviceManageabilityCSP
Remove-Item -Path $CurrentPath\DeviceManageabilityCSP -Force -Recurse -ErrorAction SilentlyContinue

# Remove Folders and Files
# Tidy up garbage in Windows folder
$CurrentPath = $env:WinDir
Remove-Item -Path $CurrentPath\CCM -Force -Recurse -ErrorAction SilentlyContinue
Remove-Item -Path $CurrentPath\ccmsetup -Force -Recurse -ErrorAction SilentlyContinue
Remove-Item -Path $CurrentPath\ccmcache -Force -Recurse -ErrorAction SilentlyContinue
Remove-Item -Path $CurrentPath\SMSCFG.ini -Force -ErrorAction SilentlyContinue
Remove-Item -Path $CurrentPath\SMS*.mif -Force -ErrorAction SilentlyContinue
Remove-Item -Path $CurrentPath\SMS*.mif -Force -ErrorAction SilentlyContinue 

$job = start-job -scriptblock {& C:\windows\ccmsetup.bak\ccmsetup.exe /installswitches} -name job
start-sleep -seconds 10

# File to monitor
$LogPath = "C:\windows\ccmsetup\logs\ccmsetup.log"

# Store the last read position
$lastPosition = 0

# Create the timer
$timer = New-Object System.Timers.Timer
$timer.Interval = 2000  # 2 seconds
$timer.AutoReset = $true
DO{
    
# Define the event handler
$timer.Add_Elapsed({
    if (Test-Path $LogPath) {
        $fs = [System.IO.File]::Open($LogPath, 'Open', 'Read', 'ReadWrite')
        $fs.Seek($lastPosition, 'Begin') | Out-Null

        $sr = New-Object System.IO.StreamReader($fs)
        $newContent = $sr.ReadToEnd()
        $lastPosition = $fs.Position

        $sr.Close()
        $fs.Close()

        if ($newContent) {
            Write-Host $newContent
        }
    } else {
        Write-Warning "Log file not found: $LogPath"
    }
})

# Start the timer
$timer.Start()
}
while($job.state -eq 'Running')

# Clean up
$timer.Stop()
$timer.Dispose()

write-host 'This job should be completed!'