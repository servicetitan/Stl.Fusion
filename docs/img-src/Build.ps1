param (
    [string]$file = ""
)

function Build() {
    foreach ($src in Get-ChildItem "*.mmd") {
        $fullName = $src.FullName
        Start-Process -WindowStyle Normal -FilePath "cmd.exe" -ArgumentList "/C", "pwsh -f Build.ps1 ""$fullName"""
    }
}

function Build1($src) {
    Write-Host "Processing: $src"
    $magicOptions = "-density 600 -trim +repage -resize 3840 -size 3840x2160 -quality 50"
    $srcDir = $src | Split-Path
    $srcName = $src | Split-Path -Leaf
    $dstLightSvg = [System.IO.Path]::ChangeExtension([System.IO.Path]::GetFullPath("$srcDir/../img/light/$srcName"), ".svg")
    $dstLightPdf = [System.IO.Path]::ChangeExtension($dstLightSvg, ".pdf")
    $dstLightJpg = [System.IO.Path]::ChangeExtension($dstLightSvg, ".jpg")
    $dstDarkSvg  = [System.IO.Path]::ChangeExtension([System.IO.Path]::GetFullPath("$srcDir/../img/dark/$srcName"), ".svg")
    $dstDarkPdf  = [System.IO.Path]::ChangeExtension($dstDarkSvg, ".pdf")
    $dstDarkJpg  = [System.IO.Path]::ChangeExtension($dstDarkSvg, ".jpg")
    
    Invoke-Expression "mmdc -i $src -o $dstLightSvg -t neutral"
    Invoke-Expression "mmdc -i $src -o $dstLightPdf -t neutral -b white"
    Invoke-Expression "magick convert $magicOptions $dstLightPdf $dstLightJpg"
    Invoke-Expression "mmdc -i $src -o $dstDarkSvg -t dark"
    Invoke-Expression "mmdc -i $src -o $dstDarkPdf -t dark -b black"
    Invoke-Expression "magick convert $magicOptions $dstDarkPdf $dstDarkJpg"
}

if ($file -eq "") {
    # $f = Get-ChildItem ComputedState.mmd
    # Build1 $f.FullName
    Build
} else {
    foreach ($src in Get-ChildItem $file) {
        $fullName = $src.FullName
        Build1 $fullName
    }
    Write-Host "Done."
    Start-Sleep 1
}
