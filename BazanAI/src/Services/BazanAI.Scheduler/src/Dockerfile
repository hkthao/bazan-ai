FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /app

COPY ["src/SharedKernel/BazanAI.SharedKernel/BazanAI.SharedKernel.csproj", "src/SharedKernel/BazanAI.SharedKernel/"]
COPY ["src/SharedKernel/BazanAI.Infrastructure.Common/BazanAI.Infrastructure.Common.csproj", "src/SharedKernel/BazanAI.Infrastructure.Common/"]
COPY ["src/Services/BazanAI.Scheduler/src/BazanAI.Scheduler.csproj", "src/Services/BazanAI.Scheduler/src/"]

RUN dotnet restore "src/Services/BazanAI.Scheduler/src/BazanAI.Scheduler.csproj"

COPY . .
WORKDIR "/app/src/Services/BazanAI.Scheduler/src"
RUN dotnet build "BazanAI.Scheduler.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "BazanAI.Scheduler.csproj" -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "BazanAI.Scheduler.dll"]
