[CmdletBinding()]
param(
    [Parameter(Mandatory)]
    [string] $RunDirectory,

    [Parameter(Mandatory)]
    [ValidateSet('start-session', 'start-quiz', 'answer', 'end-quiz', 'stop-session', 'close')]
    [string] $Action,

    [Guid] $QuestionId,
    [int] $Level = -1,
    [ValidateRange(1, 300)]
    [int] $TimeoutSeconds = 60
)

$ErrorActionPreference = 'Stop'
$directory = (Resolve-Path -LiteralPath $RunDirectory).Path
$statusPath = Join-Path $directory 'status.json'
$status = Get-Content -LiteralPath $statusPath -Raw | ConvertFrom-Json
if ($status.Phase -eq 'closed') { throw 'This automation run is closed.' }
if ($Action -eq 'answer' -and ($QuestionId -eq [Guid]::Empty -or $Level -lt 0 -or $Level -gt 5)) {
    throw 'Answer requires -QuestionId from the current status.json and -Level 0..5.'
}

$id = [Guid]::NewGuid()
$command = @{ Id = $id; Action = $Action; QuestionId = $QuestionId; Level = $Level }
$temporary = Join-Path $directory "command.$id.tmp"
try {
    $command | ConvertTo-Json | Set-Content -LiteralPath $temporary -Encoding utf8
    [System.IO.File]::Move($temporary, (Join-Path $directory 'command.json'))
}
finally {
    if (Test-Path -LiteralPath $temporary) { Remove-Item -LiteralPath $temporary }
}

$deadline = [DateTime]::UtcNow.AddSeconds($TimeoutSeconds)
$resultPath = Join-Path $directory 'result.json'
while ([DateTime]::UtcNow -lt $deadline) {
    if (Test-Path -LiteralPath $resultPath) {
        $result = Get-Content -LiteralPath $resultPath -Raw | ConvertFrom-Json
        if ($result.Id -eq $id.ToString()) {
            if (-not $result.Success) { throw $result.Error }
            return $result
        }
    }

    Start-Sleep -Milliseconds 100
}

throw "Timed out waiting for command $id. Inspect status.json and result.json; do not retry an answer blindly."
