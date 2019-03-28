#!/bin/bash
# sudo apt-get install dotnet-sdk-2.1.105
# sudo apt-get install dotnet-sharedframework-microsoft.netcore.app-1.0.5
# git clone https://github.com/fog-bank/biocs/biocs.git
# cd biocs
# chmod 777 test_ubuntu.sh
# ./test_ubuntu.sh develop

BRANCH=$1

if [ "${BRANCH}" = "" ]; then
  BRANCH=develop
fi

git fetch
git checkout -f ${BRANCH}
git merge origin/${BRANCH}
chmod 777 test_ubuntu.sh

dotnet --info | tee info.log
dotnet restore biocs | tee restore.log
dotnet build biocs | tee build.log
dotnet test biocs/core.tests -f netcoreapp2.0 -v n | tee test2.0.log
dotnet test biocs/core.tests -f netcoreapp1.0 -v n | tee test1.0.log
