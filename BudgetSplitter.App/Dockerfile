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

COPY ["BudgetSplitterWebApi.sln", "./"]
COPY ["BudgetSplitter.App/BudgetSplitter.App.csproj", "BudgetSplitter.App/"]
COPY ["BudgetSplitter.Common/BudgetSplitter.Common.csproj", "BudgetSplitter.Common/"]
COPY ["BudgetSplitter.Persistence/BudgetSplitter.Persistence.csproj", "BudgetSplitter.Persistence/"]

RUN dotnet restore "./BudgetSplitterWebApi.sln"

COPY . .

WORKDIR "/src/BudgetSplitter.App"
RUN dotnet build "BudgetSplitter.App.csproj" -c $BUILD_CONFIGURATION -o /app/build
RUN dotnet publish "BudgetSplitter.App.csproj" \
    -c $BUILD_CONFIGURATION \
    -o /app/publish \
    /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "BudgetSplitter.App.dll"]