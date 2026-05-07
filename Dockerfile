FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["src/AIQueryPlatform.Api/AIQueryPlatform.Api.csproj", "src/AIQueryPlatform.Api/"]
RUN dotnet restore "src/AIQueryPlatform.Api/AIQueryPlatform.Api.csproj"
COPY . .
WORKDIR "/src/src/AIQueryPlatform.Api"
RUN dotnet build "AIQueryPlatform.Api.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "AIQueryPlatform.Api.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "AIQueryPlatform.Api.dll"]
