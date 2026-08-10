FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY Sliki.Web/Sliki.Web.csproj Sliki.Web/
RUN dotnet restore Sliki.Web/Sliki.Web.csproj

COPY Sliki.Web/ Sliki.Web/
RUN dotnet publish Sliki.Web/Sliki.Web.csproj --configuration Release --output /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
EXPOSE 8080

COPY --from=build /app/publish .

ENV ASPNETCORE_URLS=http://+:8080
ENTRYPOINT ["dotnet", "Sliki.Web.dll"]
