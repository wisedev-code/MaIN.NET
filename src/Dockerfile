FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /app

COPY MaIN/*.csproj ./MaIN/
COPY MaIN.Domain/*.csproj ./MaIN.Domain/
COPY MaIN.Infrastructure/*.csproj ./MaIN.Infrastructure/
COPY MaIN.Services/*.csproj ./MaIN.Services/

RUN dotnet restore ./MaIN/MaIN.csproj

COPY . ./
RUN dotnet publish ./MaIN/MaIN.csproj -c Release -o /app/out

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
COPY --from=build /app/out .
ENTRYPOINT ["dotnet", "MaIN.dll"]