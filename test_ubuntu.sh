#!/bin/bash
# sudo apt-get install dotnet-sdk-2.1.105
# sudo apt-get install dotnet-sharedframework-microsoft.netcore.app-1.0.5
# git clone https://github.com/fog-bank/biocs/biocs.git
# cd biocs

git fetch
git checkout develop
git merge origin/develop
dotnet --info
dotnet test core.tests -f netcoreapp2.0
dotnet test core.tests -f netcoreapp1.0
