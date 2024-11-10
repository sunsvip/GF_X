@echo off
cd /d %ProjectRoot%
git reset --hard
git checkout %BranchName%
git pull
echo Reset and pull latest project