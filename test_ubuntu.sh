#!/bin/bash
# sudo apt-get install dotnet-sdk-2.1.105
# sudo apt-get install dotnet-sharedframework-microsoft.netcore.app-1.0.5
# git clone https://github.com/fog-bank/biocs/biocs.git
# cd biocs
# chmod 777 test_ubuntu.sh

git fetch
git checkout -f develop
git merge origin/develop
dotnet --info
dotnet test biocs/core.tests -f netcoreapp2.0 -v n | tee test2.0.log
dotnet test biocs/core.tests -f netcoreapp1.0 -v n | tee test1.0.log
