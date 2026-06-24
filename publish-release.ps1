param(
    [Parameter(Mandatory)]
    [string]$Version,
    [string]$Repo = "rajibia/mortuary-app",
    [string]$GitHubToken
)

$ErrorActionPreference = "Stop"
$publishDir = "C:\Users\coki\Desktop\MortuaryApp\publish"
$zipPath = "C:\Users\coki\Desktop\MortuaryApp\MortuaryApp_v$Version.zip"

# 1. Update version in .csproj
$csproj = "C:\Users\coki\Desktop\MortuaryApp\MortuaryApp.csproj"
(Get-Content $csproj) -replace '<Version>.*</Version>', "<Version>$Version</Version>" |
    Set-Content $csproj
(Get-Content $csproj) -replace '<FileVersion>.*</FileVersion>', "<FileVersion>${Version}.0</FileVersion>" |
    Set-Content $csproj
Write-Host "Version set to $Version"

# 2. Build and publish
dotnet publish -c Release -r win-x64 --self-contained true -o $publishDir
if ($LASTEXITCODE -ne 0) { throw "Build failed" }
Write-Host "Build + publish complete"

# 3. Create ZIP
if (Test-Path $zipPath) { Remove-Item $zipPath }
Add-Type -AssemblyName System.IO.Compression.FileSystem
[System.IO.Compression.ZipFile]::CreateFromDirectory($publishDir, $zipPath)
Write-Host "ZIP created: $zipPath"

# 4. Create GitHub Release (if token provided)
if ($GitHubToken) {
    $body = @{
        tag_name = "v$Version"
        name = "v$Version"
        body = "Release v$Version`n`nSee changelog for details."
        draft = $false
        prerelease = $false
    } | ConvertTo-Json

    $headers = @{
        Authorization = "Bearer $GitHubToken"
        Accept = "application/vnd.github.v3+json"
    }

    $release = Invoke-RestMethod -Uri "https://api.github.com/repos/$Repo/releases" `
        -Method Post -Headers $headers -Body $body -ContentType "application/json"

    Write-Host "Release created: $($release.html_url)"

    # Upload ZIP as release asset
    $uploadUrl = $release.upload_url -replace '\{.*\}', "?name=$(Split-Path $zipPath -Leaf)"
    Invoke-RestMethod -Uri $uploadUrl -Method Post -Headers $headers `
        -ContentType "application/zip" -InFile $zipPath

    Write-Host "ZIP uploaded as release asset"
} else {
    Write-Host "`n=== NEXT STEPS ==="
    Write-Host "1. Go to https://github.com/$Repo/releases/new"
    Write-Host "2. Tag: v$Version"
    Write-Host "3. Upload: $zipPath"
    Write-Host "4. Publish release"
}

Write-Host "`nDone!"
