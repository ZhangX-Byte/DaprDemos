FROM mcr.microsoft.com/dotnet/core/aspnet:3.0-buster-slim AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/core/sdk:3.0-buster AS build
WORKDIR /src
COPY ["StorageService.Api/StorageService.Api.csproj", "StorageService.Api/"]
RUN dotnet restore "StorageService.Api/StorageService.Api.csproj"
COPY . .
WORKDIR "/src/StorageService.Api"
RUN dotnet build "StorageService.Api.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "StorageService.Api.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "StorageService.Api.dll"]