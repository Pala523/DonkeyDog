# Usa l'immagine di base di ASP.NET Core
FROM mcr.microsoft.com/dotnet/aspnet:6.0 AS base
WORKDIR /app
EXPOSE 80

# Usa l'immagine di build di ASP.NET Core
FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build
WORKDIR /src
COPY ["DonkeyDog/DonkeyDog.csproj", "DonkeyDog/"]
RUN dotnet restore "DonkeyDog/DonkeyDog.csproj"
COPY . .
WORKDIR "/src/DonkeyDog"
RUN dotnet build "DonkeyDog.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "DonkeyDog.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "DonkeyDog.dll"]
