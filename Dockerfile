# 1. Use the .NET SDK to compile
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /app

# 2. Copy the entire solution and all projects
COPY . ./
RUN dotnet restore "src/GcpCleanup.Cli/GcpCleanup.Cli.csproj"

# 3. Build the CLI project (this will now find the Shared/Core projects correctly)
RUN dotnet publish "src/GcpCleanup.Cli/GcpCleanup.Cli.csproj" -c Release -o /app/out

# 4. Use the small Runtime image
FROM mcr.microsoft.com/dotnet/runtime:8.0
WORKDIR /app
COPY --from=build /app/out .

# Tell the app it is running in Docker
ENV DOTNET_RUNNING_IN_CONTAINER=true
RUN mkdir -p /app/config

ENTRYPOINT ["dotnet", "GcpCleanup.Cli.dll"]