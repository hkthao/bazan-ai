
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /app
COPY ["src/Services/BazanAI.Orchestrator/BazanAI.Orchestrator/BazanAI.Orchestrator.csproj", "src/Services/BazanAI.Orchestrator/BazanAI.Orchestrator/"]
RUN dotnet restore "src/Services/BazanAI.Orchestrator/BazanAI.Orchestrator/BazanAI.Orchestrator.csproj"
COPY . .
WORKDIR "/app/src/Services/BazanAI.Orchestrator/BazanAI.Orchestrator"
RUN dotnet build "BazanAI.Orchestrator.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "BazanAI.Orchestrator.csproj" -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "BazanAI.Orchestrator.dll"]
