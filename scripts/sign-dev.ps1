# Trust and sign development binaries for Windows CodeIntegrity
$cert = Get-ChildItem Cert:\CurrentUser\My | Where-Object { $_.Subject -like "*BooklineDev*" } | Select-Object -First 1

if (-not $cert) {
    $cert = New-SelfSignedCertificate -Type CodeSigningCert -Subject "CN=BooklineDev" -CertStoreLocation Cert:\CurrentUser\My
}

try {
    $rootStore = New-Object System.Security.Cryptography.X509Certificates.X509Store "Root", "CurrentUser"
    $rootStore.Open([System.Security.Cryptography.X509Certificates.OpenFlags]::ReadWrite)
    $rootStore.Add($cert)
    $rootStore.Close()
} catch {
    Write-Host "Trusted Root add note: $_"
}

$dlls = Get-ChildItem -Path "$PSScriptRoot\..\backend" -Recurse -Filter "Bookline*.dll"
foreach ($dll in $dlls) {
    Set-AuthenticodeSignature -FilePath $dll.FullName -Certificate $cert | Out-Null
}
Write-Host "Signed $($dlls.Count) development binaries."
