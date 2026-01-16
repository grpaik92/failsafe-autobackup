#!/bin/bash
set -e

# Create solution
dotnet new sln -n FailsafeAutoBackup

# Create project directories
mkdir -p src/FailsafeAutoBackup.Service
mkdir -p src/FailsafeAutoBackup.TrayApp
mkdir -p src/FailsafeAutoBackup.IPC
mkdir -p src/FailsafeAutoBackup.BackendApi
mkdir -p src/FailsafeAutoBackup.Shared
mkdir -p src/FailsafeAutoBackup.Installer
mkdir -p tests/FailsafeAutoBackup.Tests

# Create Windows Service (Worker Service)
cd src/FailsafeAutoBackup.Service
dotnet new worker --framework net8.0
cd ../..

# Create WPF Tray Application
cd src/FailsafeAutoBackup.TrayApp
dotnet new wpf --framework net8.0
cd ../..

# Create IPC Library
cd src/FailsafeAutoBackup.IPC
dotnet new classlib --framework net8.0
cd ../..

# Create Backend API
cd src/FailsafeAutoBackup.BackendApi
dotnet new webapi --framework net8.0
cd ../..

# Create Shared Library
cd src/FailsafeAutoBackup.Shared
dotnet new classlib --framework net8.0
cd ../..

# Create Test Project
cd tests/FailsafeAutoBackup.Tests
dotnet new xunit --framework net8.0
cd ../..

# Add projects to solution
dotnet sln add src/FailsafeAutoBackup.Service/FailsafeAutoBackup.Service.csproj
dotnet sln add src/FailsafeAutoBackup.TrayApp/FailsafeAutoBackup.TrayApp.csproj
dotnet sln add src/FailsafeAutoBackup.IPC/FailsafeAutoBackup.IPC.csproj
dotnet sln add src/FailsafeAutoBackup.BackendApi/FailsafeAutoBackup.BackendApi.csproj
dotnet sln add src/FailsafeAutoBackup.Shared/FailsafeAutoBackup.Shared.csproj
dotnet sln add tests/FailsafeAutoBackup.Tests/FailsafeAutoBackup.Tests.csproj

echo "Solution structure created successfully"
