## code coverage
dotnet tool update -g dotnet-reportgenerator-globaltool
cd biocs/core.tests
dotnet run
reportgenerator -reports:TestResults/mstest.xml -targetdir:TestResults/report/

## docfx project
dotnet tool update -g docfx
cd docfx_project
docfx --serve -p 25000
