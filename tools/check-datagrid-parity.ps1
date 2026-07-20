<#
.SYNOPSIS
    Checks API parity between WPF DataGrid (librewpf) and WindowsShims DataGrid.
.DESCRIPTION
    Scans C# source files for public types and members in both the reference
    WPF DataGrid implementation (librewpf) and the WindowsShims port, then
    compares them to identify gaps.
#>

$ErrorActionPreference = 'Stop'

# ── Paths ─────────────────────────────────────────────────────────────────
$repoRoot = Resolve-Path "$PSScriptRoot/../.."
$wpfRoot   = "$repoRoot/librewpf/src/Microsoft.DotNet.Wpf/src/PresentationFramework/System/Windows"
$shimsRoot = "$repoRoot/WindowsShims/src/LeXtudio.Windows/System.Windows"

$wpfControls   = "$wpfRoot/Controls"
$wpfPrimitives = "$wpfRoot/Controls/Primitives"
$wpfAutomation = "$wpfRoot/Automation/Peers"

$shimsControls   = "$shimsRoot/Controls"
$shimsPrimitives = "$shimsRoot/Controls/Primitives"
$shimsAutomation = "$shimsRoot/Automation/Peers"
$shimsMsInternal = "$repoRoot/WindowsShims/src/LeXtudio.Windows/MS.Internal"
$shimsExtensions = "$repoRoot/WindowsShims/src/LeXtudio.Windows/DataGridExtensions"

# Additional WPF source files (internal, etc.)
$wpfThemesRoot = "$repoRoot/librewpf/src/Microsoft.DotNet.Wpf/src/Themes"

# ── Regex helpers ─────────────────────────────────────────────────────────

# Extract public types from source text.
# Returns hashtable: Name -> @{ Kind, BaseTypes, Members }
function Get-PublicTypes {
    param([string[]]$FilePaths, [string]$SourceLabel)

    $result = @{}
    $filesScanned = 0

    foreach ($file in $FilePaths) {
        if (-not (Test-Path $file)) { continue }
        $filesScanned++
        $text = Get-Content $file -Raw -ErrorAction SilentlyContinue
        if (-not $text) { continue }

        # Strip comments
        $text = $text -replace '/\*.*?\*/', '' -replace '//.*', ''

        # Find namespace { ... } blocks and extract types within them
        $nsPattern = 'namespace\s+([\w.]+)\s*\{'
        $nsMatches = [regex]::Matches($text, $nsPattern)

        if ($nsMatches.Count -eq 0) {
            # File-scoped namespace: "namespace X.Y.Z;"
            if ($text -match 'namespace\s+([\w.]+);') {
                $ns = $Matches[1]
                $remaining = $text
                $nsMatch = [regex]::Match($text, 'namespace\s+[\w.]+;')
                if ($nsMatch.Success) {
                    $remaining = $text.Substring($nsMatch.Index + $nsMatch.Length)
                }
                ParseTypesInBlock -block $remaining -namespace $ns -result $result
            }
        }
        else {
            $idx = 0
            foreach ($nsMatch in $nsMatches) {
                $ns = $nsMatch.Groups[1].Value
                $blockStart = $nsMatch.Index + $nsMatch.Length
                $block = ExtractBraceBlock -text $text -start $blockStart
                if ($block) {
                    ParseTypesInBlock -block $block -namespace $ns -result $result
                }
            }
        }
    }

    Write-Verbose "${SourceLabel}: scanned ${filesScanned} files, found $($result.Count) public types"
    return $result
}

function ExtractBraceBlock {
    param([string]$text, [int]$start)
    $depth = 0
    $i = $start
    while ($i -lt $text.Length) {
        $c = $text[$i]
        if ($c -eq '{') { $depth++ }
        elseif ($c -eq '}') {
            $depth--
            if ($depth -eq 0) { return $text.Substring($start, $i - $start + 1) }
        }
        $i++
    }
    return $null
}

function ParseTypesInBlock {
    param([string]$block, [string]$namespace, [hashtable]$result)

    # Partial keyword usage in the shims: both linked WPF code and shim
    # additions are "partial" so they merge. Track partial types separately.
    $partialTypes = @{}

    # Match type declarations: public [static] [partial] class/struct/interface/enum Name ...
    $typePattern = '(?<mod>public\s+)(?:(?:static|abstract|sealed)\s+)*(?:(partial)\s+)?(class|struct|interface|enum|record)\s+(?<name>\w+)'
    $typeMatches = [regex]::Matches($block, $typePattern)

    # Sort by descending index so we can process without offset issues
    $sorted = $typeMatches | Sort-Object Index -Descending

    foreach ($m in $sorted) {
        $kind = $m.Groups[2].Value
        $name = $m.Groups['name'].Value
        $isPartial = $m.Groups[3].Success -and $m.Groups[3].Value -eq 'partial'

        $fullName = "$namespace.$name"

        $typeBlockStart = $m.Index + $m.Length
        $typeBlock = ExtractBraceBlock -text $block -start $typeBlockStart

        if (-not $result.ContainsKey($fullName)) {
            $result[$fullName] = @{
                Kind      = $kind
                Namespace = $namespace
                FullName  = $fullName
                Members   = @{}
                IsPartial = $isPartial
                PartialMerged = $false
            }
        }
        else {
            # Already seen this partial type; mark as merged
            $result[$fullName].PartialMerged = $true
        }

        if ($typeBlock) {
            $members = Get-PublicMembers -block $typeBlock -typeName $fullName
            foreach ($kv in $members.GetEnumerator()) {
                if (-not $result[$fullName].Members.ContainsKey($kv.Key)) {
                    $result[$fullName].Members[$kv.Key] = $kv.Value
                }
            }
        }
    }
}

function Get-PublicMembers {
    param([string]$block, [string]$typeName)

    $members = @{}

    # Methods: public ... ReturnType MethodName(...)
    $methodPattern = '(?<!\w)(public\s+)(?:new\s+)?(?:static\s+)?(?:virtual\s+)?(?:override\s+)?(?:async\s+)?(?:\w+(?:\.\w+)*(?:\[\])?(?:\s*<[^>]+>)?)\s+(\w+)\s*\('
    $methodMatches = [regex]::Matches($block, $methodPattern)
    foreach ($m in $methodMatches) {
        $name = $m.Groups[2].Value
        # Exclude obvious non-methods (local variable declarations, etc.)
        if ($name -notin @('if','while','for','foreach','switch','using','return','throw','var','out','ref','in','new','sizeof','typeof','nameof','yield','lock','fixed','catch','finally','stackalloc','from','let','select','where','join','orderby','equals','group','by','ascending','descending')) {
            $sig = $m.Value.Trim()
            $members["$name()"] = @{ Kind = 'Method'; Signature = $sig }
        }
    }

    # Properties: public ... Type Name { get; set; }
    # Match the property declaration more carefully
    $propPattern = '(public\s+)(?:new\s+)?(?:static\s+)?(?:virtual\s+)?(?:override\s+)?(\w+(?:\[\])?(?:\s*\?)?)\s+(\w+)\s*\{'
    $propMatches = [regex]::Matches($block, $propPattern)
    foreach ($m in $propMatches) {
        $name = $m.Groups[3].Value
        $type = $m.Groups[2].Value
        if ($name -notin @('get','set','add','remove')) {
            $sig = $m.Value.Trim()
            $members[$name] = @{ Kind = 'Property'; Signature = $sig; Type = $type }
        }
    }

    # Events: public event EventHandlerType Name
    $eventPattern = '(public\s+)(?:new\s+)?(?:static\s+)?(?:virtual\s+)?(?:override\s+)?event\s+(\w+(?:<[^>]*>)?)\s+(\w+)\s*\{'
    $eventMatches = [regex]::Matches($block, $eventPattern)
    foreach ($m in $eventMatches) {
        $name = $m.Groups[3].Value
        $members[$name] = @{ Kind = 'Event'; Signature = $m.Value.Trim() }
    }

    # Simple event: public event EventHandlerType Name;
    $eventSimplePattern = '(public\s+)(?:new\s+)?(?:static\s+)?(?:virtual\s+)?(?:override\s+)?event\s+(\w+(?:<[^>]*>)?)\s+(\w+);'
    $eventSimpleMatches = [regex]::Matches($block, $eventSimplePattern)
    foreach ($m in $eventSimpleMatches) {
        $name = $m.Groups[3].Value
        if (-not $members.ContainsKey($name)) {
            $members[$name] = @{ Kind = 'Event'; Signature = $m.Value.Trim() }
        }
    }

    # Public fields: public Type Name;
    $fieldPattern = '(public\s+)(?:new\s+)?(?:static\s+)?(?:readonly\s+)?(?:volatile\s+)?(\w+(?:\[\])?(?:\s*\?)?)\s+(\w+)\s*;'
    $fieldMatches = [regex]::Matches($block, $fieldPattern)
    foreach ($m in $fieldMatches) {
        $name = $m.Groups[3].Value
        $type = $m.Groups[2].Value
        # Skip if it looks like a local variable in a method
        if ($type -in @('var','int','string','bool','double','float','long','char','byte','short','uint','ulong','ushort','sbyte','object','dynamic') -and $name -match '^[a-z]') {
            $isLocal = $true
            # Heuristic: if preceded by "(" or after "=>" it's likely local
            $beforeMatch = [regex]::Match($block, ".{0,30}$([regex]::Escape($m.Value))")
            if ($beforeMatch.Success) {
                $leading = $beforeMatch.Value.Substring(0, $beforeMatch.Value.Length - $m.Value.Length)
                if ($leading -match '[{(,;]\s*$') { $isLocal = $false }
            }
            if ($isLocal) { continue }
        }
        $members[$name] = @{ Kind = 'Field'; Signature = $m.Value.Trim(); Type = $type }
    }

    # Constructors: public TypeName(...)
    $ctorPattern = '(public\s+)(\w+)\s*\('
    $ctorMatches = [regex]::Matches($block, $ctorPattern)
    $typeShortName = ($typeName -split '\.')[-1]
    foreach ($m in $ctorMatches) {
        $name = $m.Groups[2].Value
        if ($name -eq $typeShortName) {
            $members['.ctor'] = @{ Kind = 'Constructor'; Signature = $m.Value.Trim() }
        }
    }

    return $members
}

# ── Collect files ─────────────────────────────────────────────────────────

Write-Host "=== DataGrid API Parity Check ===" -ForegroundColor Cyan
Write-Host ""

Write-Host "Collecting WPF (librewpf) DataGrid files..." -ForegroundColor Yellow
$wpfDataGridFiles = @()
$wpfDataGridFiles += Get-ChildItem "$wpfControls/DataGrid*.cs" -ErrorAction SilentlyContinue | Select-Object -ExpandProperty FullName
$wpfDataGridFiles += Get-ChildItem "$wpfPrimitives/DataGrid*.cs" -ErrorAction SilentlyContinue | Select-Object -ExpandProperty FullName
$wpfDataGridFiles += Get-ChildItem "$wpfAutomation/DataGrid*.cs" -ErrorAction SilentlyContinue | Select-Object -ExpandProperty FullName

Write-Host "  Found $($wpfDataGridFiles.Count) files:"
$wpfDataGridFiles | ForEach-Object { Write-Host "    $_" }

Write-Host ""
Write-Host "Collecting WindowsShims DataGrid files..." -ForegroundColor Yellow
$shimsDataGridFiles = @()
$shimsDataGridFiles += Get-ChildItem "$shimsControls/DataGrid*.cs" -ErrorAction SilentlyContinue | Select-Object -ExpandProperty FullName
$shimsDataGridFiles += Get-ChildItem "$shimsPrimitives/DataGrid*.cs" -ErrorAction SilentlyContinue | Select-Object -ExpandProperty FullName
$shimsDataGridFiles += Get-ChildItem "$shimsAutomation/DataGrid*.cs" -ErrorAction SilentlyContinue | Select-Object -ExpandProperty FullName

Write-Host "  Found $($shimsDataGridFiles.Count) files:"
$shimsDataGridFiles | ForEach-Object { Write-Host "    $_" }

# Also look for linked WPF files in the ext/wpf directory
$wpfExternalDir = "$repoRoot/WindowsShims/ext/wpf/src/Microsoft.DotNet.Wpf/src/PresentationFramework/System/Windows"
$extDataGridFiles = @()
if (Test-Path "$wpfExternalDir/Controls") {
    $extDataGridFiles += Get-ChildItem "$wpfExternalDir/Controls/DataGrid*.cs" -ErrorAction SilentlyContinue | Select-Object -ExpandProperty FullName
    $extDataGridFiles += Get-ChildItem "$wpfExternalDir/Controls/Primitives/DataGrid*.cs" -ErrorAction SilentlyContinue | Select-Object -ExpandProperty FullName
    $extDataGridFiles += Get-ChildItem "$wpfExternalDir/Automation/Peers/DataGrid*.cs" -ErrorAction SilentlyContinue | Select-Object -ExpandProperty FullName
    Write-Host "  Found $($extDataGridFiles.Count) linked WPF files in ext/wpf/"
}

# ── Parse ─────────────────────────────────────────────────────────────────

Write-Host ""
Write-Host "Parsing WPF (librewpf) DataGrid types..." -ForegroundColor Yellow
$wpfTypes = Get-PublicTypes -FilePaths $wpfDataGridFiles -SourceLabel "WPF librewpf"

Write-Host ""
Write-Host "Parsing WindowsShims DataGrid types..." -ForegroundColor Yellow
$shimsTypes = Get-PublicTypes -FilePaths $shimsDataGridFiles -SourceLabel "WindowsShims"

Write-Host ""
Write-Host "Parsing linked WPF DataGrid types (ext/wpf)..." -ForegroundColor Yellow
$extTypes = Get-PublicTypes -FilePaths $extDataGridFiles -SourceLabel "ext/wpf"

# Merge ext types into shims (these are linked/compiled together)
foreach ($kv in $extTypes.GetEnumerator()) {
    $extTypeName = $kv.Key
    $extTypeData = $kv.Value
    if ($shimsTypes.ContainsKey($extTypeName)) {
        if ($extTypeData.PartialMerged -or $shimsTypes[$extTypeName].IsPartial) {
            # Merge members from both partial declarations
            foreach ($mkv in $extTypeData.Members.GetEnumerator()) {
                if (-not $shimsTypes[$extTypeName].Members.ContainsKey($mkv.Key)) {
                    $shimsTypes[$extTypeName].Members[$mkv.Key] = $mkv.Value
                }
            }
        }
    }
    else {
        $shimsTypes[$extTypeName] = $extTypeData
    }
}

# ── Compare ───────────────────────────────────────────────────────────────

Write-Host ""
Write-Host "=== Comparison ===" -ForegroundColor Cyan
Write-Host ""

# Types in WPF but not in WindowsShims
$missingTypes = @()
$wpfTypeNames = $wpfTypes.Keys | Sort-Object
$shimsTypeNames = $shimsTypes.Keys | Sort-Object

$matchedTypes = @()
$partialMatchTypes = @()

foreach ($wpfTypeName in $wpfTypeNames) {
    $shortName = ($wpfTypeName -split '\.')[-1]
    if ($shimsTypes.ContainsKey($wpfTypeName)) {
        $matchedTypes += $wpfTypeName
    }
    else {
        # Check for namespace differences
        $foundPartial = $false
        # The shims may use System.Windows.Controls (no parent namespace) vs
        # the WPF using System.Windows.Controls (same). Check for any match by short name.
        foreach ($st in $shimsTypeNames) {
            $stShort = ($st -split '\.')[-1]
            if ($stShort -eq $shortName) {
                $partialMatchTypes += @{
                    WpfFullName = $wpfTypeName
                    ShimFullName = $st
                }
                $foundPartial = $true
                break
            }
        }
        if (-not $foundPartial) {
            $missingTypes += $wpfTypeName
        }
    }
}

# Types in WindowsShims but not in WPF (extra types)
$extraTypes = @()
foreach ($shimTypeName in $shimsTypeNames) {
    if (-not $wpfTypes.ContainsKey($shimTypeName)) {
        $shortName = ($shimTypeName -split '\.')[-1]
        $foundInWpf = $false
        foreach ($wt in $wpfTypeNames) {
            if (($wt -split '\.')[-1] -eq $shortName) { $foundInWpf = $true; break }
        }
        if (-not $foundInWpf) {
            $extraTypes += $shimTypeName
        }
    }
}

Write-Host "Type parity:" -ForegroundColor Green
Write-Host "  WPF reference types:   $($wpfTypes.Count)"
Write-Host "  WindowsShims types:    $($shimsTypes.Count)"
Write-Host "  Matched:               $($matchedTypes.Count)"
Write-Host "  Partial (diff ns):     $($partialMatchTypes.Count)"
Write-Host "  Missing:               $($missingTypes.Count)"
Write-Host "  Extra (shim-only):     $($extraTypes.Count)"

Write-Host ""
Write-Host "Full matching types:" -ForegroundColor Green
foreach ($t in $matchedTypes | Sort-Object) {
    Write-Host "  ✓ $t"
}

Write-Host ""
Write-Host "Partial matches (different namespace):" -ForegroundColor Yellow
foreach ($pm in $partialMatchTypes) {
    Write-Host "  ~ WPF: $($pm.WpfFullName) -> Shim: $($pm.ShimFullName)"
}

Write-Host ""
Write-Host "Missing types (in WPF but not in WindowsShims):" -ForegroundColor Red
foreach ($t in $missingTypes) {
    Write-Host "  ✗ $t"
}

Write-Host ""
Write-Host "Extra types (WindowsShims only):" -ForegroundColor Magenta
foreach ($t in $extraTypes) {
    Write-Host "  + $t"
}

# ── Member comparison ─────────────────────────────────────────────────────

Write-Host ""
Write-Host "=== Member Parity ===" -ForegroundColor Cyan
Write-Host ""

$totalWpfMembers = 0
$totalShimsMembers = 0
$totalMatchedMembers = 0
$totalMissingMembers = 0
$totalExtraMembers = 0

$missingMembersDetail = @()
$extraMembersDetail = @()

foreach ($wpfTypeName in $wpfTypeNames) {
    $shortName = ($wpfTypeName -split '\.')[-1]
    $wpfType = $wpfTypes[$wpfTypeName]

    # Find corresponding shim type
    $shimType = $null
    if ($shimsTypes.ContainsKey($wpfTypeName)) {
        $shimType = $shimsTypes[$wpfTypeName]
    }
    else {
        foreach ($st in $shimsTypes.Keys) {
            if (($st -split '\.')[-1] -eq $shortName) {
                $shimType = $shimsTypes[$st]
                break
            }
        }
    }

    if (-not $shimType) {
        # All members are missing
        foreach ($mkv in $wpfType.Members.GetEnumerator()) {
            $totalWpfMembers++
            $totalMissingMembers++
            $missingMembersDetail += "$wpfTypeName.$($mkv.Key)"
        }
        continue
    }

    $wpfMembers = $wpfType.Members
    $shimMembers = $shimType.Members

    foreach ($mkv in $wpfMembers.GetEnumerator()) {
        $totalWpfMembers++
        if ($shimMembers.ContainsKey($mkv.Key)) {
            $totalMatchedMembers++
        }
        else {
            $totalMissingMembers++
            $missingMembersDetail += "$wpfTypeName.$($mkv.Key)"
        }
    }

    foreach ($mkv in $shimMembers.GetEnumerator()) {
        $totalShimsMembers++
        if (-not $wpfMembers.ContainsKey($mkv.Key)) {
            $totalExtraMembers++
            $extraMembersDetail += "$wpfTypeName.$($mkv.Key)"
        }
    }
}

Write-Host "WPF members:          $totalWpfMembers"
Write-Host "WindowsShims members: $totalShimsMembers"
Write-Host "  Matched:            $totalMatchedMembers"
Write-Host "  Missing:            $totalMissingMembers"
Write-Host "  Extra (shim-only):  $totalExtraMembers"

if ($totalWpfMembers -gt 0) {
    $parity = [math]::Round(($totalMatchedMembers / $totalWpfMembers) * 100, 1)
    Write-Host ""
    Write-Host "Member parity: $totalMatchedMembers/$totalWpfMembers ($parity%)" -ForegroundColor Green
}

Write-Host ""
Write-Host "Missing members (in WPF but not in WindowsShims):" -ForegroundColor Red
if ($missingMembersDetail.Count -gt 0) {
    ($missingMembersDetail | Sort-Object -Unique) | ForEach-Object {
        Write-Host "  ✗ $_"
    }
}
else {
    Write-Host "  <none>"
}

Write-Host ""
Write-Host "Extra members (WindowsShims only, not in WPF):" -ForegroundColor Magenta
if ($extraMembersDetail.Count -gt 0) {
    ($extraMembersDetail | Sort-Object -Unique) | ForEach-Object {
        Write-Host "  + $_"
    }
}
else {
    Write-Host "  <none>"
}

# ── Summary table ─────────────────────────────────────────────────────────

Write-Host ""
Write-Host "=== Priority Gap Summary ===" -ForegroundColor Cyan
Write-Host ""

$priorityGaps = @(
    @{ Type = "DataGrid (core control)"; Gap = $missingTypes -match '\.DataGrid$' | ForEach-Object { $_ } },
    @{ Type = "Column types"; Gap = $missingTypes -match 'DataGridColumn' },
    @{ Type = "Cell/Row types"; Gap = $missingTypes -match 'DataGrid(?:Cell|Row)' },
    @{ Type = "EventArgs types"; Gap = $missingTypes -match 'EventArgs' },
    @{ Type = "Enum types"; Gap = $missingTypes -match '\.DataGrid\w*Mode$|\.DataGrid\w*Visibility$|\.DataGrid\w*Unit$|\.DataGrid\w*Action$' },
    @{ Type = "Automation Peers"; Gap = $missingTypes -match 'AutomationPeer' },
    @{ Type = "Primitives"; Gap = $missingTypes -match '\.DataGrid\w*Presenter$|\.DataGrid\w*Header$' },
    @{ Type = "Length / Converter"; Gap = $missingTypes -match 'DataGridLength|DataGridClipboard' },
    @{ Type = "Internal helpers"; Gap = $missingTypes -match 'DataGridHelper|DataGridItem' }
)

foreach ($pg in $priorityGaps) {
    $gaps = $pg.Gap | Where-Object { $_ -and $_.Length -gt 0 }
    $count = $gaps.Count
    $status = if ($count -eq 0) { "✓ Complete" } else { "✗ $count missing" }
    Write-Host "  $($pg.Type): $status" -ForegroundColor $(if ($count -eq 0) { "Green" } else { "Red" })
    if ($count -gt 0) {
        foreach ($g in $gaps) {
            Write-Host "         - $g"
        }
    }
}

Write-Host ""
Write-Host "=== Summary ===" -ForegroundColor Cyan
Write-Host "  WPF DataGrid files scanned: $($wpfDataGridFiles.Count)"
Write-Host "  WindowsShims files scanned: $($shimsDataGridFiles.Count)"
Write-Host "  Types: $($matchedTypes.Count) matched, $($missingTypes.Count) missing, $($extraTypes.Count) extra"
Write-Host "  Members: $totalMatchedMembers matched, $totalMissingMembers missing, $totalExtraMembers extra"
$memberParity = if ($totalWpfMembers -gt 0) { [math]::Round(($totalMatchedMembers / $totalWpfMembers) * 100, 1) } else { "N/A" }
Write-Host "  Member parity: $memberParity%" -ForegroundColor Green
Write-Host ""
Write-Host "See detailed lists above for specific gaps to address." -ForegroundColor Cyan
