# Bootstrap Rhythm Runner Kanban on GitHub (Issues + Projects v2).
# Requires: gh CLI, repo scope; project/read:project for board setup.
# Usage:
#   .\bootstrap-github-kanban.ps1 -DryRun
#   .\bootstrap-github-kanban.ps1
#   .\bootstrap-github-kanban.ps1 -SetupProjectOnly

param(
    [switch]$DryRun,
    [switch]$SetupProjectOnly,
    [string]$Repo = "chulinnnn/Rhythm-Runner",
    [string]$Owner = "chulinnnn",
    [string]$ProjectTitle = "Rhythm Runner M8"
)

$ErrorActionPreference = "Stop"
$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$finalGameRoot = Split-Path -Parent $scriptDir
$cardsPath = Join-Path $finalGameRoot "Docs\Kanban\CARDS.md"
$configPath = Join-Path $finalGameRoot "Docs\Kanban\kanban-config.json"

$epicLabelMap = @{
    "E-01" = "E-01-shared"
    "E-02" = "E-02-world"
    "E-03" = "E-03-ocean"
    "E-04" = "E-04-vertical"
    "E-05" = "E-05-advanced"
    "E-06" = "E-06-qa"
}

$typeLabels = @{
    "RR-001" = @("code")
    "RR-002" = @("scene")
    "RR-003" = @("code", "scene")
    "RR-004" = @("code")
    "RR-005" = @("scene", "code")
    "RR-006" = @("scene", "code")
    "RR-007" = @("code")
    "RR-008" = @("code")
    "RR-009" = @("scene", "code")
    "RR-010" = @("code")
    "RR-011" = @("code", "scene")
    "RR-012" = @("code", "scene")
    "RR-013" = @("test")
    "RR-014" = @("code")
    "RR-015" = @("doc", "code")
    "RR-016" = @("doc")
    "RR-017" = @("scene")
    "RR-018" = @("code")
    "RR-019" = @("scene")
    "RR-020" = @("doc", "art")
    "RR-030" = @("doc")
    "RR-031" = @("code")
    "RR-032" = @("code")
    "RR-033" = @("code")
    "RR-034" = @("doc", "art")
    "RR-035" = @("doc", "test")
    "RR-036" = @("doc", "art")
    "RR-037" = @("test", "blocked")
    "RR-038" = @("code")
    "RR-039" = @("test", "scene")
    "RR-040" = @("doc")
    "RR-042" = @("scene")
    "RR-043" = @("code", "art")
    "RR-044" = @("code", "scene")
    "RR-045" = @("code")
    "RR-046" = @("doc")
    "RR-047" = @("code")
    "RR-048" = @("code", "test")
}

$statusColumns = @("Backlog", "Ready", "In Progress", "Review", "Done", "Blocked")

function Write-Info($msg) { Write-Host $msg -ForegroundColor Cyan }
function Write-Warn($msg) { Write-Host $msg -ForegroundColor Yellow }

function Test-ProjectScope {
    $prev = $ErrorActionPreference
    $ErrorActionPreference = "SilentlyContinue"
    gh project list --owner $Owner --limit 1 2>$null | Out-Null
    $ok = ($LASTEXITCODE -eq 0)
    $ErrorActionPreference = $prev
    return $ok
}

function Parse-CardsFile {
    param([string]$Path)
    if (-not (Test-Path $Path)) {
        throw "Missing CARDS.md at $Path"
    }
    $text = Get-Content -Path $Path -Raw -Encoding UTF8
    $pattern = '(?ms)^## RR-(\d{3})\s+(.+?)\r?\n\r?\n(.*?)(?=^## RR-|\z)'
    $matches = [regex]::Matches($text, $pattern)
    $cards = @()
    foreach ($m in $matches) {
        $id = "RR-$($m.Groups[1].Value)"
        $shortTitle = $m.Groups[2].Value.Trim()
        $block = $m.Groups[3].Value.Trim()
        $lines = $block -split "\r?\n"
        $meta = $lines[0].Trim()
        $bodyLines = @()
        if ($lines.Length -gt 1) {
            $bodyLines = $lines[1..($lines.Length - 1)]
        }
        $body = ($bodyLines -join "`n").Trim()

        $epic = $null
        $priority = $null
        $column = $null
        $closedDate = $null
        if ($meta -match '^(E-\d{2}(?:/E-\d{2})?)\s*/\s*(P\d)\s*/\s*([^/]+?)(?:\s*/\s*(\d{4}-\d{2}-\d{2}))?\s*$') {
            $epic = $Matches[1].Trim()
            $priority = $Matches[2].Trim()
            $column = $Matches[3].Trim()
            $closedDate = $Matches[4]
        } else {
            Write-Warn "Could not parse meta for $id : $meta"
        }

        $cards += [pscustomobject]@{
            Id = $id
            ShortTitle = $shortTitle
            Title = "$id $shortTitle"
            Epic = $epic
            Priority = $priority
            Column = $column
            ClosedDate = $closedDate
            Body = $body
            Meta = $meta
        }
    }
    return $cards
}

function Get-ExistingIssueMap {
    $json = gh issue list --repo $Repo --state all --limit 200 --json number,title,state,url
    $map = @{}
    foreach ($item in ($json | ConvertFrom-Json)) {
        if ($item.title -match '^(RR-\d{3})\b') {
            $map[$Matches[1]] = $item
        }
    }
    return $map
}

function Build-LabelList {
    param($Card)
    $labels = @("kanban")
    if ($Card.Priority) { $labels += $Card.Priority }
    if ($Card.Epic) {
        foreach ($ep in ($Card.Epic -split '/')) {
            $ep = $ep.Trim()
            if ($epicLabelMap.ContainsKey($ep)) {
                $labels += $epicLabelMap[$ep]
            }
        }
    }
    if ($typeLabels.ContainsKey($Card.Id)) {
        $labels += $typeLabels[$Card.Id]
    }
    if ($Card.Column -eq "Blocked") {
        $labels += "blocked"
    }
    return ($labels | Select-Object -Unique)
}

function Save-Config($cfg) {
    $cfg | ConvertTo-Json -Depth 5 | Set-Content -Path $configPath -Encoding UTF8
}

function Load-Config {
    if (Test-Path $configPath) {
        return Get-Content $configPath -Raw | ConvertFrom-Json
    }
    return $null
}

function Ensure-Project {
    $cfg = Load-Config
    if ($cfg -and $cfg.projectNumber) {
        Write-Info "Using project #$($cfg.projectNumber) from kanban-config.json"
        return $cfg
    }

    Write-Info "Creating project '$ProjectTitle'..."
  if ($DryRun) {
        return @{ projectNumber = 0; projectId = ""; owner = $Owner; projectUrl = "" }
    }

    $createJson = gh project create --owner $Owner --title $ProjectTitle --format json
    $created = $createJson | ConvertFrom-Json
    $projectNumber = $created.number
    $projectId = $created.id
    $projectUrl = $created.url

    Write-Info "Linking project to repo $Repo..."
    gh project link --owner $Owner --number $projectNumber --repo $Repo | Out-Null

    $cfg = @{
        owner = $Owner
        repo = $Repo
        projectTitle = $ProjectTitle
        projectNumber = $projectNumber
        projectId = $projectId
        projectUrl = $projectUrl
        statusFieldId = ""
        statusOptions = @{}
    }
    Save-Config $cfg
    return $cfg
}

function Ensure-StatusFieldOptions {
    param($Cfg)
    if ($DryRun) { return $Cfg }

    $fieldsJson = gh project field-list $Cfg.projectNumber --owner $Owner --format json | ConvertFrom-Json
    $statusField = $fieldsJson.fields | Where-Object { $_.name -eq "Status" } | Select-Object -First 1
    if (-not $statusField) {
        throw "Status field not found on project"
    }

    $Cfg.statusFieldId = $statusField.id
    $optionMap = @{}
    foreach ($opt in $statusField.options) {
        $optionMap[$opt.name] = $opt.id
    }

    $missing = @()
    foreach ($col in $statusColumns) {
        if (-not $optionMap.ContainsKey($col)) {
            $missing += $col
        }
    }

    if ($missing.Count -gt 0) {
        Write-Warn "Status options missing on project: $($missing -join ', '). Add them in GitHub UI: Project -> Settings -> Status field."
        Write-Warn "Or rename default options (Todo -> Backlog, etc.) then re-run -SetupProjectOnly."
    }

    $Cfg.statusOptions = $optionMap
    Save-Config $Cfg
    return $Cfg
}

function Map-ColumnToOptionId {
    param($Cfg, $Column)
    if (-not $Cfg.statusOptions) { return $null }
    $opts = @{}
    if ($Cfg.statusOptions -is [pscustomobject]) {
        $Cfg.statusOptions.psobject.Properties | ForEach-Object { $opts[$_.Name] = $_.Value }
    } else {
        $opts = $Cfg.statusOptions
    }

    $fallback = @{
        "Backlog" = @("Backlog", "Todo")
        "Ready" = @("Ready", "Todo")
        "In Progress" = @("In Progress")
        "Review" = @("Review", "In Progress")
        "Done" = @("Done")
        "Blocked" = @("Blocked", "Todo")
    }
    foreach ($name in $fallback[$Column]) {
        if ($opts.ContainsKey($name)) {
            return $opts[$name]
        }
    }
    return $null
}

function Add-IssueToProject {
    param($Cfg, $IssueUrl, $Column)
    if ($DryRun -or -not $Cfg.projectNumber) { return }

    $itemJson = gh project item-add $Cfg.projectNumber --owner $Owner --url $IssueUrl --format json | ConvertFrom-Json
    $optionId = Map-ColumnToOptionId -Cfg $Cfg -Column $Column
    if ($optionId -and $Cfg.statusFieldId) {
        gh project item-edit --id $itemJson.id --project-id $Cfg.projectId `
            --field-id $Cfg.statusFieldId --single-select-option-id $optionId | Out-Null
    }
}

function Create-OrUpdateIssues {
    param($Cards, $Cfg)
    $existing = Get-ExistingIssueMap
    $milestone = "M8 Submission build"
    $created = 0
    $skipped = 0

    foreach ($card in $Cards) {
        if ($existing.ContainsKey($card.Id)) {
            Write-Info "Skip $($card.Id) — issue already exists (#$($existing[$card.Id].number))"
            $skipped++
            $issueUrl = $existing[$card.Id].url
            if ($Cfg -and $Cfg.projectNumber -and -not $DryRun) {
                Add-IssueToProject -Cfg $Cfg -IssueUrl $issueUrl -Column $card.Column
            }
            continue
        }

        $labels = (Build-LabelList $card) -join ","
        $body = "$card.Meta`n`n$card.Body"
        $args = @(
            "issue", "create",
            "--repo", $Repo,
            "--title", $card.Title,
            "--body", $body,
            "--label", $labels
        )
        if ($card.Column -ne "Done") {
            $args += @("--milestone", $milestone)
        }

        if ($DryRun) {
            Write-Info "[dry-run] create $($card.Title) [$($card.Column)] labels=$labels"
            continue
        }

        $issueUrl = & gh @args
        $created++
        Write-Info "Created $issueUrl"

        if ($card.Column -eq "Done") {
            gh issue close $issueUrl --repo $Repo --reason completed | Out-Null
        }

        if ($Cfg -and $Cfg.projectNumber) {
            Add-IssueToProject -Cfg $Cfg -IssueUrl $issueUrl -Column $card.Column
        }
    }

    Write-Info "Issues created: $created, skipped: $skipped"
}

# --- main ---
Write-Info "Parsing $cardsPath"
$cards = Parse-CardsFile -Path $cardsPath
Write-Info "Found $($cards.Count) cards"

$hasProjectScope = Test-ProjectScope
if (-not $hasProjectScope) {
    Write-Warn "gh token missing read:project/project scope."
    Write-Warn "Run: gh auth refresh -h github.com -s project,read:project"
    Write-Warn "Issues will still be created; project board setup skipped."
}

$cfg = $null
if ($hasProjectScope -and (-not $SetupProjectOnly)) {
    $cfg = Load-Config
}

if ($hasProjectScope -and ($SetupProjectOnly -or -not $cfg)) {
    $cfg = Ensure-Project
    $cfg = Ensure-StatusFieldOptions -Cfg $cfg
    if ($SetupProjectOnly) {
        Write-Info "Project setup done. URL: $($cfg.projectUrl)"
        exit 0
    }
} elseif ($hasProjectScope) {
    $cfg = Ensure-StatusFieldOptions -Cfg $cfg
}

if (-not $SetupProjectOnly) {
    Create-OrUpdateIssues -Cards $cards -Cfg $cfg
}

if ($cfg -and $cfg.projectUrl) {
    Write-Info "Project: $($cfg.projectUrl)"
}
