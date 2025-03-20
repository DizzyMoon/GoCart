# Use the official .NET 8 runtime image
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 8080

# Use the official .NET 8 SDK image to build the app
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy solution and project files
COPY ["GoCart.sln", "./"]
COPY ["src/payment/payment.csproj", "src/payment/"]

# Copy the rest of the source code
WORKDIR /src/payment
COPY . .

# Build the project
WORKDIR /src/payment
RUN dotnet build -c Release -o /app/build src/payment/payment.csproj

# Publish the app
FROM build AS publish
WORKDIR /src/payment
RUN dotnet publish src/payment/payment.csproj -c Release -o /app/publish /p:UseAppHost=false

# Run the app
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "payment.dll"]