# Use the official .NET SDK image to build the app
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build

WORKDIR /src

# Copy solution and project files
COPY ../../Chirp.sln ./
COPY ../Chirp.Core/Chirp.Core.csproj ./Chirp.Core/
COPY ../Chirp.Repositories/Chirp.Repositories.csproj ./Chirp.Repositories/
COPY ../Chirp.Services/Chirp.Services.csproj ./Chirp.Services/
COPY Chirp.Razor.csproj ./Chirp.Razor/

# Restore dependencies
WORKDIR /src/Chirp.Razor
RUN dotnet restore

# Copy everything and build
WORKDIR /src
COPY ../Chirp.Core/ ./Chirp.Core/
COPY ../Chirp.Repositories/ ./Chirp.Repositories/
COPY ../Chirp.Services/ ./Chirp.Services/
COPY . ./Chirp.Razor/

WORKDIR /src/Chirp.Razor
RUN dotnet publish -c Release -o /app/publish --no-restore

# Use the official .NET runtime image to run the app
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime

WORKDIR /app

# Create Assets directory for SQLite database
RUN mkdir -p /app/Assets

# Copy the published output from the build stage
COPY --from=runtime /app/publish .

EXPOSE 80

ENTRYPOINT ["dotnet", "Chirp.Razor.dll"]
