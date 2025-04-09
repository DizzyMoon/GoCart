# Use the official .NET 8 runtime image
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 8080

# Use the official .NET 8 SDK image to build the app
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy solution and project files
COPY ["GoCart.sln", "./"]
COPY ["src/order/order.csproj", "src/order/"]

# Copy the rest of the source code
WORKDIR /src/order
COPY . .

# Build the project
WORKDIR /src/order
RUN dotnet build -c Release -o /app/build src/order/order.csproj

# Publish the app
FROM build AS publish
WORKDIR /src/order
RUN dotnet publish src/order/order.csproj -c Release -o /app/publish /p:UseAppHost=false

# Run the app
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "order.dll"]