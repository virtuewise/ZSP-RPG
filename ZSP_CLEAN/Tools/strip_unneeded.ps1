# strip_unneeded.ps1
# Creates a clean "StrippedSource" containing ONLY the reusable Unity asset
# bundle (environments, meshes, textures, audio, animations, prefabs/scenes).
# Everything else from the decompiled APK is listed in REMOVED_ITEMS.txt.
#
# The original APK folder is NOT modified. This is reversible.

param(
    [string]$Source = "..\..\ZIMBABWE THE SHADOW PROTOCOL",
    [string]$Destination = "..\StrippedSource"
)

$ErrorActionPreference = "Stop"

$sourcePath = Resolve-Path $Source
$destPath = Join-Path (Resolve-Path (Split-Path $PSScriptRoot -Parent)) "StrippedSource"

if (Test-Path $destPath) { Remove-Item -Recurse -Force $destPath }
New-Item -ItemType Directory -Force -Path (Join-Path $destPath "assets\bin\Data") | Out-Null

# The only thing we actually need from the old build.
$keep = @(
    "assets\bin\Data\data.unity3d"
)

foreach ($rel in $keep) {
    $src = Join-Path $sourcePath $rel
    $dst = Join-Path $destPath $rel
    if (Test-Path $src) {
        New-Item -ItemType Directory -Force -Path (Split-Path $dst -Parent) | Out-Null
        Copy-Item $src $dst -Force
        Write-Host "KEPT $rel"
    } else {
        Write-Warning "Missing: $rel"
    }
}

# Record everything that was deliberately left out, grouped by category.
$categories = [ordered]@{
    "Ad SDKs (native)" = @("lib\arm64-v8a\libapplovin*", "lib\arm64-v8a\libapminsight*", "lib\arm64-v8a\libtob*", "lib\arm64-v8a\libtt_*", "lib\arm64-v8a\libnms*", "lib\arm64-v8a\libpglarmor*", "lib\arm64-v8a\libplatinmods*")
    "Firebase / Crashlytics" = @("lib\arm64-v8a\libFirebase*", "lib\arm64-v8a\libcrashlytics*")
    "Analytics / Tracking" = @("lib\arm64-v8a\libdatastore*", "lib\arm64-v8a\libbuffer*", "lib\arm64-v8a\libfile_lock*")
    "Ad UI / Resources" = @("assets\Gadsme", "assets\ad-viewer", "assets\audience_network.dex", "res")
    "DEX / Java / Kotlin" = @("classes*.dex", "classes1.dex", "classes2.dex", "classes3.dex", "classes4.dex", "classes5.dex", "classes6.dex", "classes7.dex", "okhttp3", "org", "kotlin", "google")
    "Billing / Config" = @("billing.properties", "firebase-*.properties", "play-services-*.properties", "user-messaging-platform.properties", "client_analytics.proto", "messaging_event*.proto")
    "Old Android shell" = @("AndroidManifest.xml", "resources.arsc", "META-INF", "assets\dexopt", "assets\bin\Data\Managed", "lib")
    "Other non-asset files" = @("assets\builddatas.json", "assets\google-services-desktop.json", "assets\UnityServicesProjectConfiguration.json", "assets\rv_binddatas.xml", "assets\mbridge_download_dialog_view.xml", "assets\_", "assets\ .", "assets\. .dex", "assets\dic")
}

$report = @()
$report += "ZSP CLEANUP REPORT"
$report += "Source: $sourcePath"
$report += "Destination: $destPath"
$report += ""
$report += "KEPT (reusable game assets):"
$report += "  assets/bin/Data/data.unity3d  -> environments, meshes, textures, materials, animations, audio, scenes"
$report += ""
$report += "REMOVED / NOT COPIED:"
foreach ($cat in $categories.Keys) {
    $report += ""
    $report += "  [$cat]"
    foreach ($pattern in $categories[$cat]) { $report += "    - $pattern" }
}

$report | Set-Content -Path (Join-Path $destPath "REMOVED_ITEMS.txt") -Encoding UTF8
Write-Host "`nStripped source created at: $destPath"
Write-Host "See REMOVED_ITEMS.txt for the full exclusion list."
