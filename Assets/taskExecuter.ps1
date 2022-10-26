
$_id        = $args[0]
$_repo      = $args[1]
$_branch    = $args[2]
$_label     = $args[3]

# check if we already have some kind of cache for it
$slices = $_repo.ToString().Split("/")
$_projName = $slices[$slices.Count -1].Replace(".git", "")

If (-not (Test-Path -Path "/$env:TASKIUM_STORAGE_ROOT/.taskium/$_projName")) {
    git clone --mirror $_repo $_projName
} else {
    # updates
    Set-Location $_projName
    git fetch origin
    Set-Location -
}

# clone the repo
git clone --reference ./$_projName -b $_branch $_repo $_id

# run the task
Set-Location $_id
./.vscode/tasks.ps1 run $_label
exit $LASTEXITCODE
