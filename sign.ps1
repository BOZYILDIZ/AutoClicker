# Signe l'exe avec le certificat auto-signé "CN=BOZYILDIZ" (magasin de l'utilisateur, clé non exportable).
# Appelé par build.bat après la compilation.
$cert = Get-ChildItem Cert:\CurrentUser\My -CodeSigningCert | Where-Object { $_.Subject -eq "CN=BOZYILDIZ, O=BOZYILDIZ" } |
        Sort-Object NotAfter -Descending | Select-Object -First 1
if (-not $cert) { Write-Host "Certificat BOZYILDIZ introuvable : exe non signe."; exit 0 }

$exe = Join-Path $PSScriptRoot "AutoClicker by BOZYILDIZ.exe"
$r = Set-AuthenticodeSignature -FilePath $exe -Certificate $cert -HashAlgorithm SHA256 -TimestampServer "http://timestamp.digicert.com"
Write-Host ("Signature : " + $r.Status)
