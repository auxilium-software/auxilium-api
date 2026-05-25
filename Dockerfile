
# restore dependencies
FROM mcr.microsoft.com/dotnet/sdk:10.0-alpine AS restore

WORKDIR /src
COPY *.sln ./
COPY AuxiliumSoftware.AuxiliumServices.API/*.csproj            AuxiliumSoftware.AuxiliumServices.API/
COPY AuxiliumSoftware.AuxiliumServices.API.Tests/*.csproj      AuxiliumSoftware.AuxiliumServices.API.Tests/
RUN dotnet restore



# publish
FROM restore AS publish

COPY . .
RUN dotnet publish AuxiliumSoftware.AuxiliumServices.API/AuxiliumSoftware.AuxiliumServices.API.csproj \
    -c Release -o /app/publish --no-restore



# dev (SDK stays in image; source bind-mounted at runtime)
FROM restore AS dev

ENV ASPNETCORE_ENVIRONMENT=Development
EXPOSE 1938
ENTRYPOINT ["dotnet", "watch", "run", \
            "--project", "AuxiliumSoftware.AuxiliumServices.API", \
            "--no-launch-profile", \
            "--urls", "http://+:1938"]



# prod
FROM mcr.microsoft.com/dotnet/aspnet:10.0-alpine AS prod

WORKDIR /app
COPY --from=publish /app/publish .
RUN mkdir -p /etc/auxilium

EXPOSE 1938
ENTRYPOINT ["dotnet", "AuxiliumSoftware.AuxiliumServices.API.dll"]

