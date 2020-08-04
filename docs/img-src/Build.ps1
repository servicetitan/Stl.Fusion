function Build() {
    foreach ($src in Get-ChildItem "*.mmd" ) {
        Build1 $src.FullName
    }
}

function Build1($src) {
    Write-Host "Processing: $src"
    $srcDir = $src | Split-Path
    $srcName = $src | Split-Path -Leaf
    $dstLight = [System.IO.Path]::ChangeExtension([System.IO.Path]::GetFullPath("$srcDir/../img/light/$srcName"), ".svg")
    $dstDark = [System.IO.Path]::ChangeExtension([System.IO.Path]::GetFullPath("$srcDir/../img/dark/$srcName"), ".svg")
    mmdc.cmd -i $src -o $dstLight -t neutral
    mmdc.cmd -i $src -o $dstDark -t dark
}

Build
