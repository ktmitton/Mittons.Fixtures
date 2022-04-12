FROM mcr.microsoft.com/dotnet/sdk:6.0-alpine3.15 AS base
WORKDIR /app
EXPOSE 5232

ENV ASPNETCORE_URLS=http://+:5232

# Creates a non-root user with an explicit UID and adds permission to access the /app folder
# For more info, please refer to https://aka.ms/vscode-docker-dotnet-configure-containers
RUN adduser -u 5678 --disabled-password --gecos "" appuser && chown -R appuser /app
USER appuser

FROM mcr.microsoft.com/dotnet/sdk:6.0-alpine3.15 AS build
WORKDIR /src
COPY ["TestSite/TestSite.csproj", "TestSite/"]
RUN dotnet restore "TestSite/TestSite.csproj"
COPY . .
WORKDIR "/src/TestSite"
RUN dotnet build "TestSite.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "TestSite.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "TestSite.dll"]
