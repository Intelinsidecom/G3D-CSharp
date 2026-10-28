param(
    [Parameter(Mandatory=$true)][string]$Path,
    [Parameter(Mandatory=$true)][string[]]$Names
)

# Removes the named private static methods (and their leading doc comment)
# from a C# source file using brace matching that ignores braces inside
# string and character literals.

$ErrorActionPreference = 'Stop'
$lines = [System.Collections.Generic.List[string]](Get-Content -LiteralPath $Path)

function Find-MethodEnd([string[]]$src, [int]$start) {
    $depth = 0
    $inBlock = $false
    for ($i = $start; $i -lt $src.Count; $i++) {
        $line = $src[$i]
        $j = 0
        while ($j -lt $line.Length) {
            $c = $line[$j]
            if ($inBlock) {
                if ($c -eq '*' -and ($j + 1) -lt $line.Length -and $line[$j+1] -eq '/') { $inBlock = $false; $j += 2; continue }
                $j++; continue
            }
            if ($c -eq '/' -and ($j + 1) -lt $line.Length -and $line[$j+1] -eq '/') { break }
            if ($c -eq '/' -and ($j + 1) -lt $line.Length -and $line[$j+1] -eq '*') { $inBlock = $true; $j += 2; continue }
            if ($c -eq '@' -and ($j + 1) -lt $line.Length -and $line[$j+1] -eq '"') {
                $j += 2
                while ($j -lt $line.Length) { if ($line[$j] -eq '"' -and $line[$j-1] -ne '\\') { break }; $j++ }
                $j++; continue
            }
            if ($c -eq '"') {
                $j++
                while ($j -lt $line.Length) { if ($line[$j] -eq '"' -and $line[$j-1] -ne '\\') { break }; $j++ }
                $j++; continue
            }
            if ($c -eq "'") {
                $j++
                while ($j -lt $line.Length) { if ($line[$j] -eq "'" -and $line[$j-1] -ne '\\') { break }; $j++ }
                $j++; continue
            }
            if ($c -eq '{') { $depth++ }
            elseif ($c -eq '}') { $depth--; if ($depth -eq 0) { return $i } }
            $j++
        }
    }
    throw "Unbalanced braces starting at line $($start + 1)"
}

foreach ($name in $Names) {
    $pattern = '^\s*private\s+(?:static\s+)?[\w<>\[\], ]+\s+' + [regex]::Escape($name) + '\s*(?:<[^>]*>)?\s*\('
    $found = $false
    for ($i = 0; $i -lt $lines.Count; $i++) {
        if ($lines[$i] -match $pattern) {
            # walk back over an immediately preceding doc comment
            $start = $i
            $k = $i - 1
            while ($k -ge 0) {
                $t = $lines[$k].Trim()
                if ($t -eq '' -or $t.StartsWith('//')) { $k--; continue }
                if ($t.EndsWith('*/')) {
                    # find the opening of the block comment
                    $j = $k
                    while ($j -ge 0 -and -not $lines[$j].Trim().StartsWith('/*')) { $j-- }
                    if ($j -lt 0) { break }
                    $start = $j
                    $k = $j - 1
                    continue
                }
                break
            }
            $end = Find-MethodEnd $lines.ToArray() $i
            $lines.RemoveRange($start, ($end - $start + 1))
            $found = $true
            break
        }
    }
    if ($found) { Write-Output "  removed $name" } else { Write-Output "  NOT FOUND: $name" }
}

Set-Content -LiteralPath $Path -Value $lines -Encoding UTF8
