FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
ARG BUILD_CONFIGURATION=Release

RUN dotnet tool install --global dotnet-ef --version 9.0.* \
 && ln -s /root/.dotnet/tools/dotnet-ef /usr/local/bin/dotnet-ef

ENV PATH="$PATH:/root/.dotnet/tools"

WORKDIR /src
COPY ["BudgetSplitter.App/BudgetSplitter.App.csproj", "BudgetSplitter.App/"]
RUN dotnet restore "BudgetSplitter.App/BudgetSplitter.App.csproj"
COPY . .
WORKDIR "/src/BudgetSplitter.App"
RUN dotnet build "BudgetSplitter.App.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release

WORKDIR "/src/BudgetSplitter.App"
RUN dotnet publish \
    "BudgetSplitter.App.csproj" \
    --configuration $BUILD_CONFIGURATION \
    --output /app/publish \
    --no-restore \
    /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "BudgetSplitter.App.dll"]
