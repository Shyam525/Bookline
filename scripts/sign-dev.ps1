# Trust and sign development binaries for Windows CodeIntegrity
$cert = Get-ChildItem Cert:\CurrentUser\My | Where-Object { $_.Subject -like "*BooklineDev*" } | Select-Object -First 1

if (-not $cert) {
    $cert = New-SelfSignedCertificate -Type CodeSigningCert -Subject "CN=BooklineDev" -CertStoreLocation Cert:\CurrentUser\My
}

$dlls = Get-ChildItem -Path "$PSScriptRoot\..\backend" -Recurse -Filter "Bookline*.dll"
foreach ($dll in $dlls) {
    Set-AuthenticodeSignature -FilePath $dll.FullName -Certificate $cert | Out-Null
}
Write-Host "Signed $($dlls.Count) development binaries."
