# Quick GitHub Push Script for grzyClothTool
# Usage: .\push-to-github.ps1 "Your commit message here"

param(
    [Parameter(Mandatory=$false)]
    [string]$CommitMessage = "Update project files"
)

Write-Host "=== Git Status ===" -ForegroundColor Cyan
git status

Write-Host "`n=== Adding all changes ===" -ForegroundColor Cyan
git add .

Write-Host "`n=== Committing changes ===" -ForegroundColor Cyan
git commit -m $CommitMessage

Write-Host "`n=== Pushing to GitHub ===" -ForegroundColor Cyan
git push

Write-Host "`n✅ Successfully pushed to GitHub!" -ForegroundColor Green
Write-Host "View your repo at: https://github.com/Makki132/grzyClothTool" -ForegroundColor Yellow
