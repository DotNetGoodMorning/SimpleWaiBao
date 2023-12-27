

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base


USER root

WORKDIR /app

ENV DOTNET_RUNNING_IN_CONTAINER=true
RUN apt-get update && apt-get install -y fontconfig
RUN fc-cache -f -v

EXPOSE 8080

ENV TZ=Asia/Shanghai
RUN ln -snf /usr/share/zoneinfo/$TZ /etc/localtime && echo $TZ > /etc/timezone

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["WaiBao/WaiBao.csproj", "WaiBao/"]
RUN dotnet restore "./WaiBao/./WaiBao.csproj"
COPY . .
WORKDIR "/src/WaiBao"
RUN dotnet build "./WaiBao.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./WaiBao.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "WaiBao.dll"]