FROM mcr.microsoft.com/dotnet/core/aspnet:2.2-stretch-slim AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/core/sdk:2.2-stretch AS build
WORKDIR /src
COPY ["TheoryCupids/TheoryCupids.csproj", "TheoryCupids/"]
RUN dotnet restore "TheoryCupids/TheoryCupids.csproj"
COPY . .
WORKDIR "/src/TheoryCupids"
RUN dotnet build "TheoryCupids.csproj" -c Release -o /app

FROM build AS publish
RUN dotnet publish "TheoryCupids.csproj" -c Release -o /app

FROM base AS final
WORKDIR /app
COPY --from=publish /app .
ENTRYPOINT ["dotnet", "TheoryCupids.dll"]