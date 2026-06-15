# Sync GitHub Issues from Docs/Kanban/CARDS.md (English titles/bodies, close Done cards).
param(
    [string]$Repo = "chulinnnn/Rhythm-Runner",
    [switch]$DryRun
)

$ErrorActionPreference = "Stop"
$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$cardsPath = Join-Path (Split-Path -Parent $scriptDir) "Docs\Kanban\CARDS.md"

function Parse-CardsFile {
    param([string]$Path)
    $text = Get-Content -Path $Path -Raw -Encoding UTF8
    $pattern = '(?ms)^## RR-(\d{3})\s+(.+?)\r?\n\r?\n(.*?)(?=^## RR-|\z)'
    $cards = @()
    foreach ($m in [regex]::Matches($text, $pattern)) {
        $id = "RR-$($m.Groups[1].Value)"
        $shortTitle = $m.Groups[2].Value.Trim()
        $block = $m.Groups[3].Value.Trim()
        $lines = $block -split "\r?\n"
        $meta = $lines[0].Trim()
        $body = if ($lines.Length -gt 1) { ($lines[1..($lines.Length - 1)] -join "`n").Trim() } else { "" }
        $column = $null
        if ($meta -match '^(E-\d{2}(?:/E-\d{2})?)\s*/\s*(P\d)\s*/\s*([^/]+?)(?:\s*/\s*(\d{4}-\d{2}-\d{2}))?\s*$') {
            $column = $Matches[3].Trim()
        }
        $cards += [pscustomobject]@{
            Id = $id
            Title = "$id $shortTitle"
            Meta = $meta
            Body = $body
            Column = $column
        }
    }
    return $cards
}

# Issue numbers are stable after bootstrap (#8 = RR-001 … #45 = RR-048, skips RR-021–029, RR-041).
$rrToNumber = @{
    "RR-001"=8; "RR-002"=9; "RR-003"=10; "RR-004"=11; "RR-005"=12; "RR-006"=13; "RR-007"=14; "RR-008"=15
    "RR-009"=16; "RR-010"=17; "RR-011"=18; "RR-012"=19; "RR-013"=20; "RR-014"=21; "RR-015"=22; "RR-016"=23
    "RR-017"=24; "RR-018"=25; "RR-019"=26; "RR-020"=27; "RR-030"=28; "RR-031"=29; "RR-032"=30; "RR-033"=31
    "RR-034"=32; "RR-035"=33; "RR-036"=34; "RR-037"=35; "RR-038"=36; "RR-039"=37; "RR-040"=38
    "RR-042"=39; "RR-043"=40; "RR-044"=41; "RR-045"=42; "RR-046"=43; "RR-047"=44; "RR-048"=45
}

$cards = Parse-CardsFile -Path $cardsPath
Write-Host "Cards: $($cards.Count)"

foreach ($card in $cards) {
    if (-not $rrToNumber.ContainsKey($card.Id)) {
        Write-Warning "No issue number mapping for $($card.Id)"
        continue
    }
    $num = $rrToNumber[$card.Id]
    $state = gh issue view $num --repo $Repo --json state --jq .state
    $fullBody = "$card.Meta`n`n$card.Body"
    if ($DryRun) {
        Write-Host "[dry-run] #$num $($card.Title) -> $($card.Column) (state=$state)"
        continue
    }
    $bodyFile = Join-Path $env:TEMP "rr-issue-$num.md"
    $utf8NoBom = New-Object System.Text.UTF8Encoding $false
    [System.IO.File]::WriteAllText($bodyFile, $fullBody, $utf8NoBom)
    gh issue edit $num --repo $Repo --title $card.Title --body-file $bodyFile | Out-Null
    Remove-Item $bodyFile -Force
    Write-Host "Updated #$num $($card.Id)"
    if ($card.Column -eq "Done" -and $state -eq "OPEN") {
        gh issue close $num --repo $Repo --reason completed | Out-Null
        Write-Host "  closed"
    }
    if ($card.Id -eq "RR-037") {
        gh issue edit $num --repo $Repo --remove-label blocked 2>$null
    }
}

if (-not $DryRun) {
    gh api "repos/$Repo/milestones/1" -X PATCH -f state=closed 2>$null
    Write-Host "Milestone M8 closed (if open)."
}
