# Auxilium API

## Database Setup Instructions
```sh
$ dotnet tool install --global dotnet-ef --version 8.0.11
$ dotnet ef migrations add InitialCreate
$ dotnet ef database update
```
