function Build() {
    foreach ($src in Get-ChildItem "*.mmd" ) {
        Build1 $src.FullName
    }
}

function Build1($src) {
    $src | Out-Host
    $srcDir = $src | Split-Path
    $srcName = $src | Split-Path -Leaf
    $dst = [System.IO.Path]::ChangeExtension(
        [System.IO.Path]::GetFullPath("$srcDir/../img/$srcName"),
        ".svg")
    $dstDark = [System.IO.Path]::ChangeExtension($dst, ".dark.svg")
    mmdc.cmd -i $src -o $dst -t neutral
    mmdc.cmd -i $src -o $dstDark -t dark
}

Build
