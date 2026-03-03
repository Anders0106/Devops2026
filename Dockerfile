# Use the official .NET SDK image to build the app
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build

WORKDIR /src

# Copy solution and project files for restore
COPY Chirp.sln ./
COPY src/Chirp.Core/Chirp.Core.csproj ./src/Chirp.Core/
COPY src/Chirp.Repositories/Chirp.Repositories.csproj ./src/Chirp.Repositories/
COPY src/Chirp.Services/Chirp.Services.csproj ./src/Chirp.Services/
COPY src/Chirp.Razor/Chirp.Razor.csproj ./src/Chirp.Razor/

# Restore dependencies
RUN dotnet restore src/Chirp.Razor/Chirp.Razor.csproj

# Copy source code and build
COPY src/ ./src/

RUN dotnet publish src/Chirp.Razor/Chirp.Razor.csproj -c Release -o /app/publish

# Use the official .NET runtime image to run the app
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime

WORKDIR /app

# Create Assets directory
RUN mkdir -p /app/Assets

# Copy the published output from the build stage
COPY --from=build /app/publish .

EXPOSE 80

ENTRYPOINT ["dotnet", "Chirp.Razor.dll"]
