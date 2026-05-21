
# restore dependencies
FROM mcr.microsoft.com/dotnet/sdk:10.0-alpine AS restore

WORKDIR /src
# copy only the project file first so this layer is cached unless dependencies change
COPY *.csproj ./
RUN dotnet restore


# publish
FROM restore AS publish

COPY . .
RUN dotnet publish -c Release -o /app/publish --no-restore


# dev (SDK stays in image; source bind-mounted at runtime)
FROM restore AS dev

ENV ASPNETCORE_ENVIRONMENT=Development
EXPOSE 8080
# source is mounted to /src via compose dev override
ENTRYPOINT ["dotnet", "watch", "run", "--no-launch-profile", "--urls", "http://+:8080"]


# prod
FROM mcr.microsoft.com/dotnet/aspnet:10.0-alpine AS prod

WORKDIR /app
COPY --from=publish /app/publish .

# config is mounted at runtime; this just ensures the directory exists
RUN mkdir -p /etc/auxilium

EXPOSE 8080
ENTRYPOINT ["dotnet", "AuxiliumSoftware.AuxiliumServices.API.dll"]
