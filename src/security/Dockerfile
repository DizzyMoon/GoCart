# Use the official .NET 8 runtime image
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

# Use the official .NET 8 SDK image to build the app
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy the solution file and project file first to leverage Docker cache
COPY ["GoCart.sln", "./"]
COPY ["src/security/security.csproj", "security/"]
RUN dotnet restore "security/security.csproj"

# Copy the entire source code
COPY . .
WORKDIR "/src/security"

# Build the project
RUN dotnet build "security.csproj" -c Release -o /app/build

# Publish the app
FROM build AS publish
RUN dotnet publish "security.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Run the app
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "security.dll"]