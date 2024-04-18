$date = Get-Date
$version = $date.ToString("yyyy-dd-M--HH-mm-ss")
$filename = "CostStore-" + $version + ".zip"
cd .\CostStore\src\CostStore
dotnet lambda package ..\..\..\Packages\$filename --configuration Release -frun dotnet8 -farch arm64
cd ..\..\..