#!/bin/bash
# wget -q https://packages.microsoft.com/config/ubuntu/16.04/packages-microsoft-prod.deb
# sudo dpkg -i packages-microsoft-prod.deb
# sudo apt-get install apt-transport-https
# sudo apt-get update
# sudo apt-get install dotnet-sdk-2.2

# git clone https://github.com/fog-bank/biocs/biocs.git
# cd biocs
# chmod 777 test_ubuntu.sh
# ./test_ubuntu.sh develop

BRANCH=$1

if [ "${BRANCH}" = "" ]; then
  BRANCH=develop
fi

git fetch | tee info.log
git checkout -f ${BRANCH} | tee -a info.log
git merge origin/${BRANCH} | tee -a info.log
chmod 777 test_ubuntu.sh

## code coverage
# dotnet tool update -g dotnet-reportgenerator-globaltool
# cd biocs
# dotnet run --project core.tests
# reportgenerator -reports:core.tests/TestResults/mstest.xml -targetdir:core.tests/TestResults/report/

## docfx project
# cd docfx_project
# docfx --serve
## docfx build --serve