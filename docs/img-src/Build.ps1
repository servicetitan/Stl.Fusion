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
    mmdc.cmd -i $src -o $dst -b white -t neutral
}

Build
