FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /app

COPY *.csproj ./
RUN dotnet restore ./MainFE.csproj

COPY . ./
RUN dotnet publish ./MainFE.csproj -c Release -o out

# Use the official ASP.NET image to run the app
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
COPY --from=build /app/out .
ENTRYPOINT ["dotnet", "MainFE.dll"]
