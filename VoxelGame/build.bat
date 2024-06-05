@echo off
rmdir bin\Release /S /Q
dotnet publish -r win-x64 -p:PublishSingleFile=true --self-contained true
bin\Release\net8.0\win-x64\publish\VoxelGame.exe