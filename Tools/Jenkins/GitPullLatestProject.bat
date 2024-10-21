@echo off
cd /d %ProjectRoot%
git reset --hard
git clean -fdx
git pull
echo Reset and pull latest project