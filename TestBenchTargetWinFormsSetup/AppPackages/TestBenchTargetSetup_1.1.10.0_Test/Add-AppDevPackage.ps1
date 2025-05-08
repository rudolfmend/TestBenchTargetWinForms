#
# Add-AppxDevPackage.ps1 is a PowerShell script designed to install app
# packages created by Visual Studio for developers.  To run this script from
# Explorer, right-click on its icon and choose "Run with PowerShell".
#
# Visual Studio supplies this script in the folder generated with its
# "Prepare Package" command.  The same folder will also contain the app
# package (a .appx file), the signing certificate (a .cer file), and a
# "Dependencies" subfolder containing all the framework packages used by the
# app.
#
# This script simplifies installing these packages by automating the
# following functions:
#   1. Find the app package and signing certificate in the script directory
#   2. Prompt the user to acquire a developer license and to install the
#      certificate if necessary
#   3. Find dependency packages that are applicable to the operating system's
#      CPU architecture
#   4. Install the package along with all applicable dependencies
#
# All command line parameters are reserved for use internally by the script.
# Users should launch this script from Explorer.
#

# .Link
# http://go.microsoft.com/fwlink/?LinkId=243053

param(
    [switch]$Force = $false,
    [switch]$GetDeveloperLicense = $false,
    [switch]$SkipLoggingTelemetry = $false,
    [string]$CertificatePath = $null
)

$ErrorActionPreference = "Stop"

# The language resources for this script are placed in the
# "Add-AppDevPackage.resources" subfolder alongside the script.  Since the
# current working directory might not be the directory that contains the
# script, we need to create the full path of the resources directory to
# pass into Import-LocalizedData
$ScriptPath = $null
try
{
    $ScriptPath = (Get-Variable MyInvocation).Value.MyCommand.Path
    $ScriptDir = Split-Path -Parent $ScriptPath
}
catch {}

if (!$ScriptPath)
{
    PrintMessageAndExit $UiStrings.ErrorNoScriptPath $ErrorCodes.NoScriptPath
}

$LocalizedResourcePath = Join-Path $ScriptDir "Add-AppDevPackage.resources"
Import-LocalizedData -BindingVariable UiStrings -BaseDirectory $LocalizedResourcePath

$ErrorCodes = Data {
    ConvertFrom-StringData @'
    Success = 0
    NoScriptPath = 1
    NoPackageFound = 2
    ManyPackagesFound = 3
    NoCertificateFound = 4
    ManyCertificatesFound = 5
    BadCertificate = 6
    PackageUnsigned = 7
    CertificateMismatch = 8
    ForceElevate = 9
    LaunchAdminFailed = 10
    GetDeveloperLicenseFailed = 11
    InstallCertificateFailed = 12
    AddPackageFailed = 13
    ForceDeveloperLicense = 14
    CertUtilInstallFailed = 17
    CertIsCA = 18
    BannedEKU = 19
    NoBasicConstraints = 20
    NoCodeSigningEku = 21
    InstallCertificateCancelled = 22
    BannedKeyUsage = 23
    ExpiredCertificate = 24
'@
}

$IMAGE_FILE_MACHINE_i386  = 0x014c
$IMAGE_FILE_MACHINE_AMD64 = 0x8664
$IMAGE_FILE_MACHINE_ARM64 = 0xAA64
$IMAGE_FILE_MACHINE_ARM   = 0x01c0
$IMAGE_FILE_MACHINE_THUMB = 0x01c2
$IMAGE_FILE_MACHINE_ARMNT = 0x01c4

$MACHINE_ATTRIBUTES_UserEnabled    = 0x01
$MACHINE_ATTRIBUTES_KernelEnabled  = 0x02
$MACHINE_ATTRIBUTES_Wow64Container = 0x04

# First try to use IsWow64Process2 to determine the OS architecture
try
{
    $IsWow64Process2_MethodDefinition = @"
[DllImport("kernel32.dll", SetLastError = true)]
public static extern bool IsWow64Process2(IntPtr process, out ushort processMachine, out ushort nativeMachine);
"@

    $Kernel32 = Add-Type -MemberDefinition $IsWow64Process2_MethodDefinition -Name "Kernel32" -Namespace "Win32" -PassThru

    $Proc = Get-Process -id $pid
    $processMachine = New-Object uint16
    $nativeMachine = New-Object uint16

    $Res = $Kernel32::IsWow64Process2($Proc.Handle, [ref] $processMachine, [ref] $nativeMachine)
    if ($Res -eq $True)
    {
        switch ($nativeMachine)
        {
            $IMAGE_FILE_MACHINE_i386  { $ProcessorArchitecture = "x86" }
            $IMAGE_FILE_MACHINE_AMD64 { $ProcessorArchitecture = "amd64" }
            $IMAGE_FILE_MACHINE_ARM64 { $ProcessorArchitecture = "arm64" }
            $IMAGE_FILE_MACHINE_ARM   { $ProcessorArchitecture = "arm" }
            $IMAGE_FILE_MACHINE_THUMB { $ProcessorArchitecture = "arm" }
            $IMAGE_FILE_MACHINE_ARMNT { $ProcessorArchitecture = "arm" }
        }
    }
}
catch
{
    # Ignore exception and fall back to using environment variables to determine the OS architecture
}

$Amd64AppsSupportedOnArm64 = $False
if ($null -eq $ProcessorArchitecture)
{
    $ProcessorArchitecture = $Env:Processor_Architecture

    # Getting $Env:Processor_Architecture on arm64 machines will return x86.  So check if the environment
    # variable "ProgramFiles(Arm)" is also set, if it is we know the actual processor architecture is arm64.
    # The value will also be x86 on amd64 machines when running the x86 version of PowerShell.
    if ($ProcessorArchitecture -eq "x86")
    {
        if ($null -ne ${Env:ProgramFiles(Arm)})
        {
            $ProcessorArchitecture = "arm64"
        }
        elseif ($null -ne ${Env:ProgramFiles(x86)})
        {
            $ProcessorArchitecture = "amd64"
        }
    }
}
elseif ("arm64" -eq $ProcessorArchitecture)
{
    # If we successfully determined the OS to be arm64 with the above call to IsWow64Process2 and we're on Win11+
    # we need to check for a special case where amd64 apps are supported as well.
    if ([Environment]::OSVersion.Version -ge (new-object 'Version' 10,0,22000))
    {
        try
        {
            $GetMachineTypeAttributes_MethodDefinition = @"
[DllImport("api-ms-win-core-processthreads-l1-1-7.dll", EntryPoint = "GetMachineTypeAttributes", ExactSpelling = true, PreserveSig = false)]
public static extern void GetMachineTypeAttributes(ushort Machine, out ushort machineTypeAttributes);
"@

            $ProcessThreads = Add-Type -MemberDefinition $GetMachineTypeAttributes_MethodDefinition -Name "processthreads_l1_1_7" -Namespace "Win32" -PassThru
            $machineTypeAttributes = New-Object uint16

            $ProcessThreads::GetMachineTypeAttributes($IMAGE_FILE_MACHINE_AMD64, [ref] $machineTypeAttributes)

            # We're looking for the case where the UserEnabled flag is set
            if (($machineTypeAttributes -band $MACHINE_ATTRIBUTES_UserEnabled) -ne 0)
            {
                $Amd64AppsSupportedOnArm64 = $True
            }
        }
        catch
        {
            # Ignore exceptions and maintain assumption that amd64 apps aren't supported on this machine
        }
    }
}

function PrintMessageAndExit($ErrorMessage, $ReturnCode)
{
    Write-Host $ErrorMessage
    try
    {
        # Log telemetry data regarding the use of the script if possible.
        # There are three ways that this can be disabled:
        #   1. If the "TelemetryDependencies" folder isn't present.  This can be excluded at build time by setting the MSBuild property AppxLogTelemetryFromSideloadingScript to false
        #   2. If the SkipLoggingTelemetry switch is passed to this script.
        #   3. If Visual Studio telemetry is disabled via the registry.
        $TelemetryAssembliesFolder = (Join-Path $PSScriptRoot "TelemetryDependencies")
        if (!$SkipLoggingTelemetry -And (Test-Path $TelemetryAssembliesFolder))
        {
            $job = Start-Job -FilePath (Join-Path $TelemetryAssembliesFolder "LogSideloadingTelemetry.ps1") -ArgumentList $TelemetryAssembliesFolder, "VS/DesignTools/SideLoadingScript/AddAppDevPackage", $ReturnCode, $ProcessorArchitecture
            Wait-Job -Job $job -Timeout 60 | Out-Null
        }
    }
    catch
    {
        # Ignore telemetry errors
    }

    if (!$Force)
    {
        Pause
    }
    
    exit $ReturnCode
}

#
# Warns the user about installing certificates, and presents a Yes/No prompt
# to confirm the action.  The default is set to No.
#
function ConfirmCertificateInstall
{
    $Answer = $host.UI.PromptForChoice(
                    "", 
                    $UiStrings.WarningInstallCert, 
                    [System.Management.Automation.Host.ChoiceDescription[]]@($UiStrings.PromptYesString, $UiStrings.PromptNoString), 
                    1)
    
    return $Answer -eq 0
}

#
# Validates whether a file is a valid certificate using CertUtil.
# This needs to be done before calling Get-PfxCertificate on the file, otherwise
# the user will get a cryptic "Password: " prompt for invalid certs.
#
function ValidateCertificateFormat($FilePath)
{
    # certutil -verify prints a lot of text that we don't need, so it's redirected to $null here
    certutil.exe -verify $FilePath > $null
    if ($LastExitCode -lt 0)
    {
        PrintMessageAndExit ($UiStrings.ErrorBadCertificate -f $FilePath, $LastExitCode) $ErrorCodes.BadCertificate
    }
    
    # Check if certificate is expired
    $cert = Get-PfxCertificate $FilePath
    if (($cert.NotBefore -gt (Get-Date)) -or ($cert.NotAfter -lt (Get-Date)))
    {
        PrintMessageAndExit ($UiStrings.ErrorExpiredCertificate -f $FilePath) $ErrorCodes.ExpiredCertificate
    }
}

#
# Verify that the developer certificate meets the following restrictions:
#   - The certificate must contain a Basic Constraints extension, and its
#     Certificate Authority (CA) property must be false.
#   - The certificate's Key Usage extension must be either absent, or set to
#     only DigitalSignature.
#   - The certificate must contain an Extended Key Usage (EKU) extension with
#     Code Signing usage.
#   - The certificate must NOT contain any other EKU except Code Signing and
#     Lifetime Signing.
#
# These restrictions are enforced to decrease security risks that arise from
# trusting digital certificates.
#
function CheckCertificateRestrictions
{
    Set-Variable -Name BasicConstraintsExtensionOid -Value "2.5.29.19" -Option Constant
    Set-Variable -Name KeyUsageExtensionOid -Value "2.5.29.15" -Option Constant
    Set-Variable -Name EkuExtensionOid -Value "2.5.29.37" -Option Constant
    Set-Variable -Name CodeSigningEkuOid -Value "1.3.6.1.5.5.7.3.3" -Option Constant
    Set-Variable -Name LifetimeSigningEkuOid -Value "1.3.6.1.4.1.311.10.3.13" -Option Constant
    Set-Variable -Name UwpSigningEkuOid -Value "1.3.6.1.4.1.311.84.3.1" -Option Constant
    Set-Variable -Name DisposableSigningEkuOid -Value "1.3.6.1.4.1.311.84.3.2" -Option Constant

    $CertificateExtensions = (Get-PfxCertificate $CertificatePath).Extensions
    $HasBasicConstraints = $false
    $HasCodeSigningEku = $false

    foreach ($Extension in $CertificateExtensions)
    {
        # Certificate must contain the Basic Constraints extension
        if ($Extension.oid.value -eq $BasicConstraintsExtensionOid)
        {
            # CA property must be false
            if ($Extension.CertificateAuthority)
            {
                PrintMessageAndExit $UiStrings.ErrorCertIsCA $ErrorCodes.CertIsCA
            }
            $HasBasicConstraints = $true
        }

        # If key usage is present, it must be set to digital signature
        elseif ($Extension.oid.value -eq $KeyUsageExtensionOid)
        {
            if ($Extension.KeyUsages -ne "DigitalSignature")
            {
                PrintMessageAndExit ($UiStrings.ErrorBannedKeyUsage -f $Extension.KeyUsages) $ErrorCodes.BannedKeyUsage
            }
        }

        elseif ($Extension.oid.value -eq $EkuExtensionOid)
        {
            # Certificate must contain the Code Signing EKU
            $EKUs = $Extension.EnhancedKeyUsages.Value
            if ($EKUs -contains $CodeSigningEkuOid)
            {
                $HasCodeSigningEKU = $True
            }

            # EKUs other than code signing and lifetime signing are not allowed
            foreach ($EKU in $EKUs)
            {
                if ($EKU -ne $CodeSigningEkuOid -and $EKU -ne $LifetimeSigningEkuOid -and $EKU -ne $UwpSigningEkuOid -and $EKU -ne $DisposableSigningEkuOid)
                {
                    PrintMessageAndExit ($UiStrings.ErrorBannedEKU -f $EKU) $ErrorCodes.BannedEKU
                }
            }
        }
    }

    if (!$HasBasicConstraints)
    {
        PrintMessageAndExit $UiStrings.ErrorNoBasicConstraints $ErrorCodes.NoBasicConstraints
    }
    if (!$HasCodeSigningEKU)
    {
        PrintMessageAndExit $UiStrings.ErrorNoCodeSigningEku $ErrorCodes.NoCodeSigningEku
    }
}

#
# Performs operations that require administrative privileges:
#   - Prompt the user to obtain a developer license
#   - Install the developer certificate (if -Force is not specified, also prompts the user to confirm)
#
function DoElevatedOperations
{
    if ($GetDeveloperLicense)
    {
        Write-Host $UiStrings.GettingDeveloperLicense

        if ($Force)
        {
            PrintMessageAndExit $UiStrings.ErrorForceDeveloperLicense $ErrorCodes.ForceDeveloperLicense
        }
        try
        {
            Show-WindowsDeveloperLicenseRegistration
        }
        catch
        {
            $Error[0] # Dump details about the last error
            PrintMessageAndExit $UiStrings.ErrorGetDeveloperLicenseFailed $ErrorCodes.GetDeveloperLicenseFailed
        }
    }

    if ($CertificatePath)
    {
        Write-Host $UiStrings.InstallingCertificate

        # Make sure certificate format is valid and usage constraints are followed
        ValidateCertificateFormat $CertificatePath
        CheckCertificateRestrictions

        # If -Force is not specified, warn the user and get consent
        if ($Force -or (ConfirmCertificateInstall))
        {
            # Add cert to store
            certutil.exe -addstore TrustedPeople $CertificatePath
            if ($LastExitCode -lt 0)
            {
                PrintMessageAndExit ($UiStrings.ErrorCertUtilInstallFailed -f $LastExitCode) $ErrorCodes.CertUtilInstallFailed
            }
        }
        else
        {
            PrintMessageAndExit $UiStrings.ErrorInstallCertificateCancelled $ErrorCodes.InstallCertificateCancelled
        }
    }
}

#
# Checks whether the machine is missing a valid developer license.
#
function CheckIfNeedDeveloperLicense
{
    $Result = $true
    try
    {
        $Result = (Get-WindowsDeveloperLicense | Where-Object { $_.IsValid } | Measure-Object).Count -eq 0
    }
    catch {}

    return $Result
}

#
# Launches an elevated process running the current script to perform tasks
# that require administrative privileges.  This function waits until the
# elevated process terminates, and checks whether those tasks were successful.
#
function LaunchElevated
{
    # Set up command line arguments to the elevated process
    $RelaunchArgs = '-ExecutionPolicy Unrestricted -file "' + $ScriptPath + '"'

    if ($Force)
    {
        $RelaunchArgs += ' -Force'
    }
    if ($NeedDeveloperLicense)
    {
        $RelaunchArgs += ' -GetDeveloperLicense'
    }
    if ($SkipLoggingTelemetry)
    {
        $RelaunchArgs += ' -SkipLoggingTelemetry'
    }
    if ($NeedInstallCertificate)
    {
        $RelaunchArgs += ' -CertificatePath "' + $DeveloperCertificatePath.FullName + '"'
    }

    # Launch the process and wait for it to finish
    try
    {
        $PowerShellExePath = (Get-Process -Id $PID).Path
        $AdminProcess = Start-Process $PowerShellExePath -Verb RunAs -ArgumentList $RelaunchArgs -PassThru
    }
    catch
    {
        $Error[0] # Dump details about the last error
        PrintMessageAndExit $UiStrings.ErrorLaunchAdminFailed $ErrorCodes.LaunchAdminFailed
    }

    while (!($AdminProcess.HasExited))
    {
        Start-Sleep -Seconds 2
    }

    # Check if all elevated operations were successful
    if ($NeedDeveloperLicense)
    {
        if (CheckIfNeedDeveloperLicense)
        {
            PrintMessageAndExit $UiStrings.ErrorGetDeveloperLicenseFailed $ErrorCodes.GetDeveloperLicenseFailed
        }
        else
        {
            Write-Host $UiStrings.AcquireLicenseSuccessful
        }
    }
    if ($NeedInstallCertificate)
    {
        $Signature = Get-AuthenticodeSignature $DeveloperPackagePath -Verbose
        if ($Signature.Status -ne "Valid")
        {
            PrintMessageAndExit ($UiStrings.ErrorInstallCertificateFailed -f $Signature.Status) $ErrorCodes.InstallCertificateFailed
        }
        else
        {
            Write-Host $UiStrings.InstallCertificateSuccessful
        }
    }
}

#
# Finds all applicable dependency packages according to OS architecture, and
# installs the developer package with its dependencies.  The expected layout
# of dependencies is:
#
# <current dir>
#     \Dependencies
#         <Architecture neutral dependencies>.appx\.msix
#         \x86
#             <x86 dependencies>.appx\.msix
#         \x64
#             <x64 dependencies>.appx\.msix
#         \arm
#             <arm dependencies>.appx\.msix
#         \arm64
#             <arm64 dependencies>.appx\.msix
#
function InstallPackageWithDependencies
{
    $DependencyPackagesDir = (Join-Path $ScriptDir "Dependencies")
    $DependencyPackages = @()
    if (Test-Path $DependencyPackagesDir)
    {
        # Get architecture-neutral dependencies
        $DependencyPackages += Get-ChildItem (Join-Path $DependencyPackagesDir "*.appx") | Where-Object { $_.Mode -NotMatch "d" }
        $DependencyPackages += Get-ChildItem (Join-Path $DependencyPackagesDir "*.msix") | Where-Object { $_.Mode -NotMatch "d" }

        # Get architecture-specific dependencies
        if (($ProcessorArchitecture -eq "x86" -or $ProcessorArchitecture -eq "amd64" -or $ProcessorArchitecture -eq "arm64") -and (Test-Path (Join-Path $DependencyPackagesDir "x86")))
        {
            $DependencyPackages += Get-ChildItem (Join-Path $DependencyPackagesDir "x86\*.appx") | Where-Object { $_.Mode -NotMatch "d" }
            $DependencyPackages += Get-ChildItem (Join-Path $DependencyPackagesDir "x86\*.msix") | Where-Object { $_.Mode -NotMatch "d" }
        }
        if ((($ProcessorArchitecture -eq "amd64") -or ($ProcessorArchitecture -eq "arm64" -and $Amd64AppsSupportedOnArm64)) -and (Test-Path (Join-Path $DependencyPackagesDir "x64")))
        {
            $DependencyPackages += Get-ChildItem (Join-Path $DependencyPackagesDir "x64\*.appx") | Where-Object { $_.Mode -NotMatch "d" }
            $DependencyPackages += Get-ChildItem (Join-Path $DependencyPackagesDir "x64\*.msix") | Where-Object { $_.Mode -NotMatch "d" }
        }
        if (($ProcessorArchitecture -eq "arm" -or $ProcessorArchitecture -eq "arm64") -and (Test-Path (Join-Path $DependencyPackagesDir "arm")))
        {
            $DependencyPackages += Get-ChildItem (Join-Path $DependencyPackagesDir "arm\*.appx") | Where-Object { $_.Mode -NotMatch "d" }
            $DependencyPackages += Get-ChildItem (Join-Path $DependencyPackagesDir "arm\*.msix") | Where-Object { $_.Mode -NotMatch "d" }
        }
        if (($ProcessorArchitecture -eq "arm64") -and (Test-Path (Join-Path $DependencyPackagesDir "arm64")))
        {
            $DependencyPackages += Get-ChildItem (Join-Path $DependencyPackagesDir "arm64\*.appx") | Where-Object { $_.Mode -NotMatch "d" }
            $DependencyPackages += Get-ChildItem (Join-Path $DependencyPackagesDir "arm64\*.msix") | Where-Object { $_.Mode -NotMatch "d" }
        }
    }
    Write-Host $UiStrings.InstallingPackage

    $AddPackageSucceeded = $False
    try
    {
        if ($DependencyPackages.FullName.Count -gt 0)
        {
            Write-Host $UiStrings.DependenciesFound
            $DependencyPackages.FullName
            Add-AppxPackage -Path $DeveloperPackagePath.FullName -DependencyPath $DependencyPackages.FullName -ForceApplicationShutdown
        }
        else
        {
            Add-AppxPackage -Path $DeveloperPackagePath.FullName -ForceApplicationShutdown
        }
        $AddPackageSucceeded = $?
    }
    catch
    {
        $Error[0] # Dump details about the last error
    }

    if (!$AddPackageSucceeded)
    {
        if ($NeedInstallCertificate)
        {
            PrintMessageAndExit $UiStrings.ErrorAddPackageFailedWithCert $ErrorCodes.AddPackageFailed
        }
        else
        {
            PrintMessageAndExit $UiStrings.ErrorAddPackageFailed $ErrorCodes.AddPackageFailed
        }
    }
}

#
# Main script logic when the user launches the script without parameters.
#
function DoStandardOperations
{
    # Check for an .appx or .msix file in the script directory
    $PackagePath = Get-ChildItem (Join-Path $ScriptDir "*.appx") | Where-Object { $_.Mode -NotMatch "d" }
    if ($PackagePath -eq $null)
    {
        $PackagePath = Get-ChildItem (Join-Path $ScriptDir "*.msix") | Where-Object { $_.Mode -NotMatch "d" }
    }
    $PackageCount = ($PackagePath | Measure-Object).Count

    # Check for an .appxbundle or .msixbundle file in the script directory
    $BundlePath = Get-ChildItem (Join-Path $ScriptDir "*.appxbundle") | Where-Object { $_.Mode -NotMatch "d" }
    if ($BundlePath -eq $null)
    {
        $BundlePath = Get-ChildItem (Join-Path $ScriptDir "*.msixbundle") | Where-Object { $_.Mode -NotMatch "d" }
    }
    $BundleCount = ($BundlePath | Measure-Object).Count

    # Check for an .eappx or .emsix file in the script directory
    $EncryptedPackagePath = Get-ChildItem (Join-Path $ScriptDir "*.eappx") | Where-Object { $_.Mode -NotMatch "d" }
    if ($EncryptedPackagePath -eq $null)
    {
        $EncryptedPackagePath = Get-ChildItem (Join-Path $ScriptDir "*.emsix") | Where-Object { $_.Mode -NotMatch "d" }
    }
    $EncryptedPackageCount = ($EncryptedPackagePath | Measure-Object).Count

    # Check for an .eappxbundle or .emsixbundle file in the script directory
    $EncryptedBundlePath = Get-ChildItem (Join-Path $ScriptDir "*.eappxbundle") | Where-Object { $_.Mode -NotMatch "d" }
    if ($EncryptedBundlePath -eq $null)
    {
        $EncryptedBundlePath = Get-ChildItem (Join-Path $ScriptDir "*.emsixbundle") | Where-Object { $_.Mode -NotMatch "d" }
    }
    $EncryptedBundleCount = ($EncryptedBundlePath | Measure-Object).Count

    $NumberOfPackages = $PackageCount + $EncryptedPackageCount
    $NumberOfBundles = $BundleCount + $EncryptedBundleCount

    # There must be at least one package or bundle
    if ($NumberOfPackages + $NumberOfBundles -lt 1)
    {
        PrintMessageAndExit $UiStrings.ErrorNoPackageFound $ErrorCodes.NoPackageFound
    }
    # We must have exactly one bundle OR no bundle and exactly one package
    elseif ($NumberOfBundles -gt 1 -or
            ($NumberOfBundles -eq 0 -and $NumberOfpackages -gt 1))
    {
        PrintMessageAndExit $UiStrings.ErrorManyPackagesFound $ErrorCodes.ManyPackagesFound
    }

    # First attempt to install a bundle or encrypted bundle. If neither exists, fall back to packages and then encrypted packages
    if ($BundleCount -eq 1)
    {
        $DeveloperPackagePath = $BundlePath
        Write-Host ($UiStrings.BundleFound -f $DeveloperPackagePath.FullName)
    }
    elseif ($EncryptedBundleCount -eq 1)
    {
        $DeveloperPackagePath = $EncryptedBundlePath
        Write-Host ($UiStrings.EncryptedBundleFound -f $DeveloperPackagePath.FullName)
    }
    elseif ($PackageCount -eq 1)
    {
        $DeveloperPackagePath = $PackagePath
        Write-Host ($UiStrings.PackageFound -f $DeveloperPackagePath.FullName)
    }
    elseif ($EncryptedPackageCount -eq 1)
    {
        $DeveloperPackagePath = $EncryptedPackagePath
        Write-Host ($UiStrings.EncryptedPackageFound -f $DeveloperPackagePath.FullName)
    }
    
    # The package must be signed
    $PackageSignature = Get-AuthenticodeSignature $DeveloperPackagePath
    $PackageCertificate = $PackageSignature.SignerCertificate
    if (!$PackageCertificate)
    {
        PrintMessageAndExit $UiStrings.ErrorPackageUnsigned $ErrorCodes.PackageUnsigned
    }

    # Test if the package signature is trusted.  If not, the corresponding certificate
    # needs to be present in the current directory and needs to be installed.
    $NeedInstallCertificate = ($PackageSignature.Status -ne "Valid")

    if ($NeedInstallCertificate)
    {
        # List all .cer files in the script directory
        $DeveloperCertificatePath = Get-ChildItem (Join-Path $ScriptDir "*.cer") | Where-Object { $_.Mode -NotMatch "d" }
        $DeveloperCertificateCount = ($DeveloperCertificatePath | Measure-Object).Count

        # There must be exactly 1 certificate
        if ($DeveloperCertificateCount -lt 1)
        {
            PrintMessageAndExit $UiStrings.ErrorNoCertificateFound $ErrorCodes.NoCertificateFound
        }
        elseif ($DeveloperCertificateCount -gt 1)
        {
            PrintMessageAndExit $UiStrings.ErrorManyCertificatesFound $ErrorCodes.ManyCertificatesFound
        }

        Write-Host ($UiStrings.CertificateFound -f $DeveloperCertificatePath.FullName)

        # The .cer file must have the format of a valid certificate
        ValidateCertificateFormat $DeveloperCertificatePath

        # The package signature must match the certificate file
        if ($PackageCertificate -ne (Get-PfxCertificate $DeveloperCertificatePath))
        {
            PrintMessageAndExit $UiStrings.ErrorCertificateMismatch $ErrorCodes.CertificateMismatch
        }
    }

    $NeedDeveloperLicense = CheckIfNeedDeveloperLicense

    # Relaunch the script elevated with the necessary parameters if needed
    if ($NeedDeveloperLicense -or $NeedInstallCertificate)
    {
        Write-Host $UiStrings.ElevateActions
        if ($NeedDeveloperLicense)
        {
            Write-Host $UiStrings.ElevateActionDevLicense
        }
        if ($NeedInstallCertificate)
        {
            Write-Host $UiStrings.ElevateActionCertificate
        }

        $IsAlreadyElevated = ([Security.Principal.WindowsIdentity]::GetCurrent().Groups.Value -contains "S-1-5-32-544")
        if ($IsAlreadyElevated)
        {
            if ($Force -and $NeedDeveloperLicense)
            {
                PrintMessageAndExit $UiStrings.ErrorForceDeveloperLicense $ErrorCodes.ForceDeveloperLicense
            }
            if ($Force -and $NeedInstallCertificate)
            {
                Write-Warning $UiStrings.WarningInstallCert
            }
        }
        else
        {
            if ($Force)
            {
                PrintMessageAndExit $UiStrings.ErrorForceElevate $ErrorCodes.ForceElevate
            }
            else
            {
                Write-Host $UiStrings.ElevateActionsContinue
                Pause
            }
        }

        LaunchElevated
    }

    InstallPackageWithDependencies
}

#
# Main script entry point
#
if ($GetDeveloperLicense -or $CertificatePath)
{
    DoElevatedOperations
}
else
{
    DoStandardOperations
    PrintMessageAndExit $UiStrings.Success $ErrorCodes.Success
}

# SIG # Begin signature block
# MIImRgYJKoZIhvcNAQcCoIImNzCCJjMCAQExDzANBglghkgBZQMEAgEFADB5Bgor
# BgEEAYI3AgEEoGswaTA0BgorBgEEAYI3AgEeMCYCAwEAAAQQH8w7YFlLCE63JNLG
# KX7zUQIBAAIBAAIBAAIBAAIBADAxMA0GCWCGSAFlAwQCAQUABCAYh/586NhEPWiO
# +3D6/pj3+lXDRXkK9+wCdlGR7safL6CCC3YwggT+MIID5qADAgECAhMzAAAFplgQ
# Z0s9bHz2AAAAAAWmMA0GCSqGSIb3DQEBCwUAMH4xCzAJBgNVBAYTAlVTMRMwEQYD
# VQQIEwpXYXNoaW5ndG9uMRAwDgYDVQQHEwdSZWRtb25kMR4wHAYDVQQKExVNaWNy
# b3NvZnQgQ29ycG9yYXRpb24xKDAmBgNVBAMTH01pY3Jvc29mdCBDb2RlIFNpZ25p
# bmcgUENBIDIwMTAwHhcNMjQwODIyMTkyNTU3WhcNMjUwNzA1MTkyNTU3WjB0MQsw
# CQYDVQQGEwJVUzETMBEGA1UECBMKV2FzaGluZ3RvbjEQMA4GA1UEBxMHUmVkbW9u
# ZDEeMBwGA1UEChMVTWljcm9zb2Z0IENvcnBvcmF0aW9uMR4wHAYDVQQDExVNaWNy
# b3NvZnQgQ29ycG9yYXRpb24wggEiMA0GCSqGSIb3DQEBAQUAA4IBDwAwggEKAoIB
# AQC6Y0Y5HouBkWTVMeTa9bc2tcYo/BOP01al7iEpsJe2tphp+h8raHrtRmBxpRMG
# nkOdah2vPDmeHBk8b2CWFSYyP96wcQ0bz/Jrn0df02/qNBjYwtU8H4yv8PCVwG1L
# JfNRt5WeZ2Yg8X6nGQYURAuXwpjRucyYlPlL6zq64nXZKIAJ9247C8TQk4/jhy9O
# w2PLw8Ic2dajd6M1BWYZTPk3OffUuLdbw5ZjknsEoxTZz+V/fj9UyMl2s+ruph4O
# jYgmxgyRI83Ky+fK+iRtpYDijLwA0LuPib467VYv0mWkMVavmA3uN8WltTKqytIr
# 2t+LA2x2YQADzJekgj6oTMcTAgMBAAGjggF9MIIBeTAfBgNVHSUEGDAWBgorBgEE
# AYI3PQYBBggrBgEFBQcDAzAdBgNVHQ4EFgQUTpwj74MWwfMVOTFfzA0ULf1Sli0w
# VAYDVR0RBE0wS6RJMEcxLTArBgNVBAsTJE1pY3Jvc29mdCBJcmVsYW5kIE9wZXJh
# dGlvbnMgTGltaXRlZDEWMBQGA1UEBRMNMjMwODY1KzUwMjcxMjAfBgNVHSMEGDAW
# gBTm/F97uyIAWORyTrX0IXQjMubvrDBWBgNVHR8ETzBNMEugSaBHhkVodHRwOi8v
# Y3JsLm1pY3Jvc29mdC5jb20vcGtpL2NybC9wcm9kdWN0cy9NaWNDb2RTaWdQQ0Ff
# MjAxMC0wNy0wNi5jcmwwWgYIKwYBBQUHAQEETjBMMEoGCCsGAQUFBzAChj5odHRw
# Oi8vd3d3Lm1pY3Jvc29mdC5jb20vcGtpL2NlcnRzL01pY0NvZFNpZ1BDQV8yMDEw
# LTA3LTA2LmNydDAMBgNVHRMBAf8EAjAAMA0GCSqGSIb3DQEBCwUAA4IBAQC1/mao
# WMt6HRh9WayU+YG7qxU3fHyUtN0fZ5jU9ZshONjogSHli0B/rLamY+NGKPt5tl4k
# HWUuN3MOHv/Rj9cb29BQ3K5zNE+MzACTUU1b7DWfw0bZccwvL5gOqUVSNy6/FVJB
# cOos7fviDnWyXvnCe49v66hDIwLqzsF182bfvrFZX7txWrQ98mW0UhyIk+nxvGnr
# ONnqh3f1pxL9BZ7zvXQWliL0Q2JXzjkpQoEVOrgxfp/zhG3PwIMJrn9iieQ1VR6s
# MsqjVvSzqbxvzMxqK7/aze2P3tU/lK8AbPq6HAWectldQlHju7Lu00jp3NY3P1cG
# CbQNJWOR/We91fRAMIIGcDCCBFigAwIBAgIKYQxSTAAAAAAAAzANBgkqhkiG9w0B
# AQsFADCBiDELMAkGA1UEBhMCVVMxEzARBgNVBAgTCldhc2hpbmd0b24xEDAOBgNV
# BAcTB1JlZG1vbmQxHjAcBgNVBAoTFU1pY3Jvc29mdCBDb3Jwb3JhdGlvbjEyMDAG
# A1UEAxMpTWljcm9zb2Z0IFJvb3QgQ2VydGlmaWNhdGUgQXV0aG9yaXR5IDIwMTAw
# HhcNMTAwNzA2MjA0MDE3WhcNMjUwNzA2MjA1MDE3WjB+MQswCQYDVQQGEwJVUzET
# MBEGA1UECBMKV2FzaGluZ3RvbjEQMA4GA1UEBxMHUmVkbW9uZDEeMBwGA1UEChMV
# TWljcm9zb2Z0IENvcnBvcmF0aW9uMSgwJgYDVQQDEx9NaWNyb3NvZnQgQ29kZSBT
# aWduaW5nIFBDQSAyMDEwMIIBIjANBgkqhkiG9w0BAQEFAAOCAQ8AMIIBCgKCAQEA
# 6Q5kUHlntcTj/QkATJ6UrPdWaOpE2M/FWE+ppXZ8bUW60zmStKQe+fllguQX0o/9
# RJwI6GWTzixVhL99COMuK6hBKxi3oktuSUxrFQfe0dLCiR5xlM21f0u0rwjYzIjW
# axeUOpPOJj/s5v40mFfVHV1J9rIqLtWFu1k/+JC0K4N0yiuzO0bj8EZJwRdmVMkc
# vR3EVWJXcvhnuSUgNN5dpqWVXqsogM3Vsp7lA7Vj07IUyMHIiiYKWX8H7P8O7YAS
# NUwSpr5SW/Wm2uCLC0h31oVH1RC5xuiq7otqLQVcYMa0KlucIxxfReMaFB5vN8sZ
# M4BqiU2jamZjeJPVMM+VHwIDAQABo4IB4zCCAd8wEAYJKwYBBAGCNxUBBAMCAQAw
# HQYDVR0OBBYEFOb8X3u7IgBY5HJOtfQhdCMy5u+sMBkGCSsGAQQBgjcUAgQMHgoA
# UwB1AGIAQwBBMAsGA1UdDwQEAwIBhjAPBgNVHRMBAf8EBTADAQH/MB8GA1UdIwQY
# MBaAFNX2VsuP6KJcYmjRPZSQW9fOmhjEMFYGA1UdHwRPME0wS6BJoEeGRWh0dHA6
# Ly9jcmwubWljcm9zb2Z0LmNvbS9wa2kvY3JsL3Byb2R1Y3RzL01pY1Jvb0NlckF1
# dF8yMDEwLTA2LTIzLmNybDBaBggrBgEFBQcBAQROMEwwSgYIKwYBBQUHMAKGPmh0
# dHA6Ly93d3cubWljcm9zb2Z0LmNvbS9wa2kvY2VydHMvTWljUm9vQ2VyQXV0XzIw
# MTAtMDYtMjMuY3J0MIGdBgNVHSAEgZUwgZIwgY8GCSsGAQQBgjcuAzCBgTA9Bggr
# BgEFBQcCARYxaHR0cDovL3d3dy5taWNyb3NvZnQuY29tL1BLSS9kb2NzL0NQUy9k
# ZWZhdWx0Lmh0bTBABggrBgEFBQcCAjA0HjIgHQBMAGUAZwBhAGwAXwBQAG8AbABp
# AGMAeQBfAFMAdABhAHQAZQBtAGUAbgB0AC4gHTANBgkqhkiG9w0BAQsFAAOCAgEA
# GnTvV08pe8QWhXi4UNMi/AmdrIKX+DT/KiyXlRLl5L/Pv5PI4zSp24G43B4AvtI1
# b6/lf3mVd+UC1PHr2M1OHhthosJaIxrwjKhiUUVnCOM/PB6T+DCFF8g5QKbXDrMh
# KeWloWmMIpPMdJjnoUdD8lOswA8waX/+0iUgbW9h098H1dlyACxphnY9UdumOUjJ
# N2FtB91TGcun1mHCv+KDqw/ga5uV1n0oUbCJSlGkmmzItx9KGg5pqdfcwX7RSXCq
# tq27ckdjF/qm1qKmhuyoEESbY7ayaYkGx0aGehg/6MUdIdV7+QIjLcVBy78dTMgW
# 77Gcf/wiS0mKbhXjpn92W9FTeZGFndXS2z1zNfM8rlSyUkdqwKoTldKOEdqZZ14y
# jPs3hdHcdYWch8ZaV4XCv90Nj4ybLeu07s8n07VeafqkFgQBpyRnc89NT7beBVaX
# evfpUk30dwVPhcbYC/GO7UIJ0Q124yNWeCImNr7KsYxuqh3khdpHM2KPpMmRM19x
# HkCvmGXJIuhCISWKHC1g2TeJQYkqFg/XYTyUaGBS79ZHmaCAQO4VgXc+nOBTGBpQ
# HTiVmx5mMxMnORd4hzbOTsNfsvU9R1O24OXbC2E9KteSLM43Wj5AQjGkHxAIwlac
# vyRdUQKdannSF9PawZSOB3slcUSrBmrm1MbfI5qWdcUxghomMIIaIgIBATCBlTB+
# MQswCQYDVQQGEwJVUzETMBEGA1UECBMKV2FzaGluZ3RvbjEQMA4GA1UEBxMHUmVk
# bW9uZDEeMBwGA1UEChMVTWljcm9zb2Z0IENvcnBvcmF0aW9uMSgwJgYDVQQDEx9N
# aWNyb3NvZnQgQ29kZSBTaWduaW5nIFBDQSAyMDEwAhMzAAAFplgQZ0s9bHz2AAAA
# AAWmMA0GCWCGSAFlAwQCAQUAoIGuMBkGCSqGSIb3DQEJAzEMBgorBgEEAYI3AgEE
# MBwGCisGAQQBgjcCAQsxDjAMBgorBgEEAYI3AgEVMC8GCSqGSIb3DQEJBDEiBCA/
# 8sllbhmdgHazXDKfJje5ZPISyNOJfdmSfAvZZC8tJDBCBgorBgEEAYI3AgEMMTQw
# MqAUgBIATQBpAGMAcgBvAHMAbwBmAHShGoAYaHR0cDovL3d3dy5taWNyb3NvZnQu
# Y29tMA0GCSqGSIb3DQEBAQUABIIBALc4qkHnqXFsIfwHOAYDdDUee8LiVd99P3+g
# tGiN+G6XGjgO0qi2Ege665VBGOhfRkWrcxuvc8f4a+LGI72xVBiROtUo6KpZvHPU
# YtG6C1dj3qSgofpLwUVzdsu2Cg6v+zJrE/SwMT7vI7kkOd219WqJZOB6taU16+V+
# 8F+MxEjSCnZ9HPwF6pcyNxyWIGZKmJlmtZV+6+yuKm9KY1IcTPSxF0S2Y5scgz5I
# 4cJW78qSiYAfFFggYSWE3z61XT6EGwFxBcWcDKKSJ+dEp/bsBcQUQMT842xB5kJr
# zs+KBwjwRIIMw//eNJ8yoenuZLKuJAU/kBaQQM7IYPI2u36sxDGhghewMIIXrAYK
# KwYBBAGCNwMDATGCF5wwgheYBgkqhkiG9w0BBwKggheJMIIXhQIBAzEPMA0GCWCG
# SAFlAwQCAQUAMIIBWgYLKoZIhvcNAQkQAQSgggFJBIIBRTCCAUECAQEGCisGAQQB
# hFkKAwEwMTANBglghkgBZQMEAgEFAAQgtNhb1uE+Xf4Xi/Z5zvuuL6W42RCzmeM3
# n3Dv2tyv5gcCBme2NYGhmRgTMjAyNTA0MDEwMjAyMzYuMDk4WjAEgAIB9KCB2aSB
# 1jCB0zELMAkGA1UEBhMCVVMxEzARBgNVBAgTCldhc2hpbmd0b24xEDAOBgNVBAcT
# B1JlZG1vbmQxHjAcBgNVBAoTFU1pY3Jvc29mdCBDb3Jwb3JhdGlvbjEtMCsGA1UE
# CxMkTWljcm9zb2Z0IElyZWxhbmQgT3BlcmF0aW9ucyBMaW1pdGVkMScwJQYDVQQL
# Ex5uU2hpZWxkIFRTUyBFU046NDAxQS0wNUUwLUQ5NDcxJTAjBgNVBAMTHE1pY3Jv
# c29mdCBUaW1lLVN0YW1wIFNlcnZpY2WgghH+MIIHKDCCBRCgAwIBAgITMwAAAf7Q
# qMJ7NCELAQABAAAB/jANBgkqhkiG9w0BAQsFADB8MQswCQYDVQQGEwJVUzETMBEG
# A1UECBMKV2FzaGluZ3RvbjEQMA4GA1UEBxMHUmVkbW9uZDEeMBwGA1UEChMVTWlj
# cm9zb2Z0IENvcnBvcmF0aW9uMSYwJAYDVQQDEx1NaWNyb3NvZnQgVGltZS1TdGFt
# cCBQQ0EgMjAxMDAeFw0yNDA3MjUxODMxMThaFw0yNTEwMjIxODMxMThaMIHTMQsw
# CQYDVQQGEwJVUzETMBEGA1UECBMKV2FzaGluZ3RvbjEQMA4GA1UEBxMHUmVkbW9u
# ZDEeMBwGA1UEChMVTWljcm9zb2Z0IENvcnBvcmF0aW9uMS0wKwYDVQQLEyRNaWNy
# b3NvZnQgSXJlbGFuZCBPcGVyYXRpb25zIExpbWl0ZWQxJzAlBgNVBAsTHm5TaGll
# bGQgVFNTIEVTTjo0MDFBLTA1RTAtRDk0NzElMCMGA1UEAxMcTWljcm9zb2Z0IFRp
# bWUtU3RhbXAgU2VydmljZTCCAiIwDQYJKoZIhvcNAQEBBQADggIPADCCAgoCggIB
# ALy8IRcVpagON6JbBODwnoGeJkn7B9mE0ihGL/Bp99+tgZmsnHX+U97UMaT9zVpu
# tmB1IniEF8PtLuKpWsuADdyKJyPuOzaYvX6OdsXQFzF9KRq3NHqlvEVjd2381zyr
# 9OztfIth4w8i7ssGMigPRZlm3j42oX/TMHfEIMoJD7cA61UBi8jpMjN1U4hyqoRr
# vQQhlUXR1vZZjzK61JT1omFfS1QgeVWHfgBFLXX6gHapc1cQOdxIMUqoaeiW3xCp
# 03XHz+k/DIq9B68E07VdodsgwbY120CGqsnCjm+t9xn0ZJ9teizgwYN+z/8cIaHV
# 0/NWQtmhze3sRA5pm4lrLIxrxSZJYtoOnbdNXkoTohpoW6J69Kl13AXqjW+kKBfI
# 1/7g1bWPaby+I/GhFkuPYSlB9Js7ArnCK8FEvsfDLk9Ln+1VwhTRW4glDUU6H8Sd
# weOeHhiYS9H8FE0W4Mgm6S4CjCg4gkbm+uQ4Wng71AACU/dykgqHhQqJJT2r24EM
# moRmQy/71gFY1+W/Cc4ZcvYBgnSv6ouovnMWdEvMegdsoz22X3QVXx/zQaf9S5+8
# W3jhEwDp+zk/Q91BrdKvioloGONh5y48oZdWwLuR34K8gDtwwmiHVdrY75CWstqj
# pxew4I/GutCkE/UIHyX8F5692Som2DI2lGwjSA58c9spAgMBAAGjggFJMIIBRTAd
# BgNVHQ4EFgQUb857ifUlNoOZf+f2/uQgYm2xxd0wHwYDVR0jBBgwFoAUn6cVXQBe
# Yl2D9OXSZacbUzUZ6XIwXwYDVR0fBFgwVjBUoFKgUIZOaHR0cDovL3d3dy5taWNy
# b3NvZnQuY29tL3BraW9wcy9jcmwvTWljcm9zb2Z0JTIwVGltZS1TdGFtcCUyMFBD
# QSUyMDIwMTAoMSkuY3JsMGwGCCsGAQUFBwEBBGAwXjBcBggrBgEFBQcwAoZQaHR0
# cDovL3d3dy5taWNyb3NvZnQuY29tL3BraW9wcy9jZXJ0cy9NaWNyb3NvZnQlMjBU
# aW1lLVN0YW1wJTIwUENBJTIwMjAxMCgxKS5jcnQwDAYDVR0TAQH/BAIwADAWBgNV
# HSUBAf8EDDAKBggrBgEFBQcDCDAOBgNVHQ8BAf8EBAMCB4AwDQYJKoZIhvcNAQEL
# BQADggIBAIk+DVLztpcPtHQzLbAbsZl9qN5VUKp0JLiEwBiBgoCPrJe2amTkw3fC
# 6sbB+Blgj087XN7a/AIAb7GCM1oxcIqAowkDg6taATFjcxLCs3JB8QM2KOUs3uzj
# 5DANwwMVauEkkfMvk0QthnDndCUXmdZT5YZT5fVyPs/DoLTj5kJyy4j/as6Ux8Bc
# 3vrG6kp/HHpHbjGXS8hyZNzYsNwJ4JVP1k8xrEAHXIfUlVeCx4n1J5sE39ItO4ir
# U5TZKt28dYsloOze4xmQAUVk9pl/mAFR5Stu7fZ/lrWG5+nDiTV+i7B/MT1QUWAC
# EVZFrDMhAHaD/Xan2mc8Fxpo7lUPd9TYcx44xvhH8NdfA145N1at6lCNa3t+MzDE
# 0c2WRMPNhbqRd74lzUdw1TpUvSR+MeXpnyDWtbrkmnOheAniQg9RmpH0uw+WsjbG
# mdnvrAVIetilU5YRLEER2UcAk8W4sdWOIicPjwzS3NB39fal9l4l9LtkjPQlk047
# M/UrwoyCksQmRQjb/86SiJbB8e4UDUB0jGyodP8MJ/OroiACxI2s1LMxNPl+q3Dm
# w31OIfzv9L5mxdwTEfuOawGTABEybEQz8RyQqP+VxoVnYPy6CeV1gazgy+OGDaze
# xUZxxAAK9OrH5amfHnldxbgynT+YdfVlJxlsDtR/2Y1MzqFRold4MIIHcTCCBVmg
# AwIBAgITMwAAABXF52ueAptJmQAAAAAAFTANBgkqhkiG9w0BAQsFADCBiDELMAkG
# A1UEBhMCVVMxEzARBgNVBAgTCldhc2hpbmd0b24xEDAOBgNVBAcTB1JlZG1vbmQx
# HjAcBgNVBAoTFU1pY3Jvc29mdCBDb3Jwb3JhdGlvbjEyMDAGA1UEAxMpTWljcm9z
# b2Z0IFJvb3QgQ2VydGlmaWNhdGUgQXV0aG9yaXR5IDIwMTAwHhcNMjEwOTMwMTgy
# MjI1WhcNMzAwOTMwMTgzMjI1WjB8MQswCQYDVQQGEwJVUzETMBEGA1UECBMKV2Fz
# aGluZ3RvbjEQMA4GA1UEBxMHUmVkbW9uZDEeMBwGA1UEChMVTWljcm9zb2Z0IENv
# cnBvcmF0aW9uMSYwJAYDVQQDEx1NaWNyb3NvZnQgVGltZS1TdGFtcCBQQ0EgMjAx
# MDCCAiIwDQYJKoZIhvcNAQEBBQADggIPADCCAgoCggIBAOThpkzntHIhC3miy9ck
# eb0O1YLT/e6cBwfSqWxOdcjKNVf2AX9sSuDivbk+F2Az/1xPx2b3lVNxWuJ+Slr+
# uDZnhUYjDLWNE893MsAQGOhgfWpSg0S3po5GawcU88V29YZQ3MFEyHFcUTE3oAo4
# bo3t1w/YJlN8OWECesSq/XJprx2rrPY2vjUmZNqYO7oaezOtgFt+jBAcnVL+tuhi
# JdxqD89d9P6OU8/W7IVWTe/dvI2k45GPsjksUZzpcGkNyjYtcI4xyDUoveO0hyTD
# 4MmPfrVUj9z6BVWYbWg7mka97aSueik3rMvrg0XnRm7KMtXAhjBcTyziYrLNueKN
# iOSWrAFKu75xqRdbZ2De+JKRHh09/SDPc31BmkZ1zcRfNN0Sidb9pSB9fvzZnkXf
# tnIv231fgLrbqn427DZM9ituqBJR6L8FA6PRc6ZNN3SUHDSCD/AQ8rdHGO2n6Jl8
# P0zbr17C89XYcz1DTsEzOUyOArxCaC4Q6oRRRuLRvWoYWmEBc8pnol7XKHYC4jMY
# ctenIPDC+hIK12NvDMk2ZItboKaDIV1fMHSRlJTYuVD5C4lh8zYGNRiER9vcG9H9
# stQcxWv2XFJRXRLbJbqvUAV6bMURHXLvjflSxIUXk8A8FdsaN8cIFRg/eKtFtvUe
# h17aj54WcmnGrnu3tz5q4i6tAgMBAAGjggHdMIIB2TASBgkrBgEEAYI3FQEEBQID
# AQABMCMGCSsGAQQBgjcVAgQWBBQqp1L+ZMSavoKRPEY1Kc8Q/y8E7jAdBgNVHQ4E
# FgQUn6cVXQBeYl2D9OXSZacbUzUZ6XIwXAYDVR0gBFUwUzBRBgwrBgEEAYI3TIN9
# AQEwQTA/BggrBgEFBQcCARYzaHR0cDovL3d3dy5taWNyb3NvZnQuY29tL3BraW9w
# cy9Eb2NzL1JlcG9zaXRvcnkuaHRtMBMGA1UdJQQMMAoGCCsGAQUFBwMIMBkGCSsG
# AQQBgjcUAgQMHgoAUwB1AGIAQwBBMAsGA1UdDwQEAwIBhjAPBgNVHRMBAf8EBTAD
# AQH/MB8GA1UdIwQYMBaAFNX2VsuP6KJcYmjRPZSQW9fOmhjEMFYGA1UdHwRPME0w
# S6BJoEeGRWh0dHA6Ly9jcmwubWljcm9zb2Z0LmNvbS9wa2kvY3JsL3Byb2R1Y3Rz
# L01pY1Jvb0NlckF1dF8yMDEwLTA2LTIzLmNybDBaBggrBgEFBQcBAQROMEwwSgYI
# KwYBBQUHMAKGPmh0dHA6Ly93d3cubWljcm9zb2Z0LmNvbS9wa2kvY2VydHMvTWlj
# Um9vQ2VyQXV0XzIwMTAtMDYtMjMuY3J0MA0GCSqGSIb3DQEBCwUAA4ICAQCdVX38
# Kq3hLB9nATEkW+Geckv8qW/qXBS2Pk5HZHixBpOXPTEztTnXwnE2P9pkbHzQdTlt
# uw8x5MKP+2zRoZQYIu7pZmc6U03dmLq2HnjYNi6cqYJWAAOwBb6J6Gngugnue99q
# b74py27YP0h1AdkY3m2CDPVtI1TkeFN1JFe53Z/zjj3G82jfZfakVqr3lbYoVSfQ
# JL1AoL8ZthISEV09J+BAljis9/kpicO8F7BUhUKz/AyeixmJ5/ALaoHCgRlCGVJ1
# ijbCHcNhcy4sa3tuPywJeBTpkbKpW99Jo3QMvOyRgNI95ko+ZjtPu4b6MhrZlvSP
# 9pEB9s7GdP32THJvEKt1MMU0sHrYUP4KWN1APMdUbZ1jdEgssU5HLcEUBHG/ZPkk
# vnNtyo4JvbMBV0lUZNlz138eW0QBjloZkWsNn6Qo3GcZKCS6OEuabvshVGtqRRFH
# qfG3rsjoiV5PndLQTHa1V1QJsWkBRH58oWFsc/4Ku+xBZj1p/cvBQUl+fpO+y/g7
# 5LcVv7TOPqUxUYS8vwLBgqJ7Fx0ViY1w/ue10CgaiQuPNtq6TPmb/wrpNPgkNWcr
# 4A245oyZ1uEi6vAnQj0llOZ0dFtq0Z4+7X6gMTN9vMvpe784cETRkPHIqzqKOghi
# f9lwY1NNje6CbaUFEMFxBmoQtB1VM1izoXBm8qGCA1kwggJBAgEBMIIBAaGB2aSB
# 1jCB0zELMAkGA1UEBhMCVVMxEzARBgNVBAgTCldhc2hpbmd0b24xEDAOBgNVBAcT
# B1JlZG1vbmQxHjAcBgNVBAoTFU1pY3Jvc29mdCBDb3Jwb3JhdGlvbjEtMCsGA1UE
# CxMkTWljcm9zb2Z0IElyZWxhbmQgT3BlcmF0aW9ucyBMaW1pdGVkMScwJQYDVQQL
# Ex5uU2hpZWxkIFRTUyBFU046NDAxQS0wNUUwLUQ5NDcxJTAjBgNVBAMTHE1pY3Jv
# c29mdCBUaW1lLVN0YW1wIFNlcnZpY2WiIwoBATAHBgUrDgMCGgMVAIRjRw/2u0NG
# 0C1lRvSbhsYC0V7HoIGDMIGApH4wfDELMAkGA1UEBhMCVVMxEzARBgNVBAgTCldh
# c2hpbmd0b24xEDAOBgNVBAcTB1JlZG1vbmQxHjAcBgNVBAoTFU1pY3Jvc29mdCBD
# b3Jwb3JhdGlvbjEmMCQGA1UEAxMdTWljcm9zb2Z0IFRpbWUtU3RhbXAgUENBIDIw
# MTAwDQYJKoZIhvcNAQELBQACBQDrlWzJMCIYDzIwMjUwMzMxMTkzNDMzWhgPMjAy
# NTA0MDExOTM0MzNaMHcwPQYKKwYBBAGEWQoEATEvMC0wCgIFAOuVbMkCAQAwCgIB
# AAICCtICAf8wBwIBAAICEtIwCgIFAOuWvkkCAQAwNgYKKwYBBAGEWQoEAjEoMCYw
# DAYKKwYBBAGEWQoDAqAKMAgCAQACAwehIKEKMAgCAQACAwGGoDANBgkqhkiG9w0B
# AQsFAAOCAQEAdyjoN2/rdYj8eNfwfH85zWKnAF88IVR1JvHM8gcF82zRlFyg9+zQ
# VivmXtZYk5TKIJ3QoRz7FC8UUwZAbuP0HK7bw6kt9gd8dS2FdtPnZhc6sJVkmK4K
# uX/yLF++a7csiwdeoANDfpNkbRmv0+V7Kgal39xMycPl4mIWuYJE0wZuQqjKSDEC
# 94vDQNUVPvijz7HdDDEo5LG2CULK02ljwo0VoQEAKVoOpfZs3oCv4TQoZvamPxKz
# oZizy8xc8HrmXoX085NZU7s0Fk0tGDs/XCyfNg7VaACfoYeQOsm4pLsaDXMrhwL7
# 2xPUfxc8PA1D/Kix+W7GXOWm8Ewe3aGasjGCBA0wggQJAgEBMIGTMHwxCzAJBgNV
# BAYTAlVTMRMwEQYDVQQIEwpXYXNoaW5ndG9uMRAwDgYDVQQHEwdSZWRtb25kMR4w
# HAYDVQQKExVNaWNyb3NvZnQgQ29ycG9yYXRpb24xJjAkBgNVBAMTHU1pY3Jvc29m
# dCBUaW1lLVN0YW1wIFBDQSAyMDEwAhMzAAAB/tCowns0IQsBAAEAAAH+MA0GCWCG
# SAFlAwQCAQUAoIIBSjAaBgkqhkiG9w0BCQMxDQYLKoZIhvcNAQkQAQQwLwYJKoZI
# hvcNAQkEMSIEIAbOTV2vO7sU0MCN0Gq4Pv9Too+Hd4BsoyR/eRJKRzIeMIH6Bgsq
# hkiG9w0BCRACLzGB6jCB5zCB5DCBvQQgEYXM3fxTyJ8Y0fdptoT1qnPrxjhtfvyN
# FrZArLcodHkwgZgwgYCkfjB8MQswCQYDVQQGEwJVUzETMBEGA1UECBMKV2FzaGlu
# Z3RvbjEQMA4GA1UEBxMHUmVkbW9uZDEeMBwGA1UEChMVTWljcm9zb2Z0IENvcnBv
# cmF0aW9uMSYwJAYDVQQDEx1NaWNyb3NvZnQgVGltZS1TdGFtcCBQQ0EgMjAxMAIT
# MwAAAf7QqMJ7NCELAQABAAAB/jAiBCBdszYZFe7HeBm6A6YD1WsJ5Wotl5V/5/00
# 3F8cIHKsazANBgkqhkiG9w0BAQsFAASCAgCYCbUK6SZN/t7C0ttjARnEjC+a9lFW
# MM4nrPd7UiB0sKUuwuLRelxMq7RpvMrujO5ND3GaK/20cQ2mAKOej9DV8YEn2nmA
# m+UbDIONvHKk2Lyikl/y9JIMufQ1cIQOwxbuOer4z5i97OOJpzaAZzTqQXuIjJFS
# l7qcfF+uJTnDRS5j7u3ljIYZubMee0mxTULWWVk392aXhbRC4z1R7YWk4pBYXThu
# bAhLC5nWrci0nyMx2bCq8u4pr6d+eVfCR6Q0Y0DPAqtooVANvhSpqnJogb5pzhZx
# yJg8NpM9oo1eZ0IEEfGZOnAUwBYWTnTT7otzcYCvW9KhANhfoDsDOIJoiCBVGshw
# /5xsAe5B3Au5c+jusXDfona2aqV8lrQzXXHzYQDvq1JiyJXuQUYjAtyNeasRwaUE
# 0Ehu3oOb9moRgcgNd6Z1oemc9tv2gVs/odM1FZHlbD0E7oFxXEDvAGXmmnidroH/
# o8ZBx97cU03K+Z4t0Vny0cAdwwIxt79c4eol4QdYXYbQAn8+geD7ytF3IoaR39gO
# S3dUEpnk/2xVEaIE0t4TnzWlikEjweAVZ7E7XWxDt8U5c7uIPsZWG8mMPrSfiPf1
# Nge2TqgqbK4ViCHfVkJSVFsO4615lv9kdRG2NsAMa/5HLL5jhHdV27QcLVrmSsnB
# 5PEoqcx9aV9PmQ==
# SIG # End signature block
