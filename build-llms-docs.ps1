$ErrorActionPreference = "Stop"

$docsDir = Join-Path $PSScriptRoot "docs"
$outputFile = Join-Path $PSScriptRoot "llms-full.txt"

# Explicit whitelist of user-facing docs (update filenames to match your docs/ folder exactly)
$includedFiles = @(
    "architecture.md",
    "initialization.md",
    "zui-format.md",
    "code-generation.md",
    "property-binding.md",
    "styles-and-themes.md",
    "controls.md",
    "input-events.md",
    "gesture-recognizer.md",
    "scroll-viewer.md",
    "list-box.md",
    "combo-box.md",
    "tree-view.md"
)

$sb = [System.Text.StringBuilder]::new()
[void]$sb.AppendLine("# ZurfurGui Documentation (User Guide)")
[void]$sb.AppendLine("> Minimal, inheritance-free C# WebAssembly & Desktop GUI framework using Model Data View (MDV) and JSON5.")
[void]$sb.AppendLine("")

foreach ($fileName in $includedFiles) {
    $filePath = Join-Path $docsDir $fileName
    if (-not (Test-Path $filePath)) {
        Write-Warning "Skipping missing file: $fileName"
        continue
    }

    $content = Get-Content -Path $filePath -Raw -Encoding UTF8

    # Strip everything from <!-- INTERNAL --> onward
    $splitContent = $content -split "<!--\s*INTERNAL\s*-->", 2
    $userContent = $splitContent[0].Trim()

    if ($userContent.Length -gt 0) {
        [void]$sb.AppendLine("================================================================================")
        [void]$sb.AppendLine("FILE: docs/$fileName")
        [void]$sb.AppendLine("================================================================================")
        [void]$sb.AppendLine($userContent)
        [void]$sb.AppendLine("")
    }
}

[System.IO.File]::WriteAllText($outputFile, $sb.ToString(), [System.Text.UTF8Encoding]::new($false))
Write-Host "Successfully generated $outputFile"
