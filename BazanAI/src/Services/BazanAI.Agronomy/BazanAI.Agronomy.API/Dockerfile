FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /app

# Copy csproj and restore
COPY ["src/SharedKernel/BazanAI.SharedKernel/BazanAI.SharedKernel.csproj", "src/SharedKernel/BazanAI.SharedKernel/"]
COPY ["src/SharedKernel/BazanAI.Infrastructure.Common/BazanAI.Infrastructure.Common.csproj", "src/SharedKernel/BazanAI.Infrastructure.Common/"]
COPY ["src/Services/BazanAI.Agronomy/BazanAI.Agronomy.API/BazanAI.Agronomy.API.csproj", "src/Services/BazanAI.Agronomy/BazanAI.Agronomy.API/"]

RUN dotnet restore "src/Services/BazanAI.Agronomy/BazanAI.Agronomy.API/BazanAI.Agronomy.API.csproj"

# Copy everything else and build
COPY . .
WORKDIR "/app/src/Services/BazanAI.Agronomy/BazanAI.Agronomy.API"
RUN dotnet build "BazanAI.Agronomy.API.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "BazanAI.Agronomy.API.csproj" -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "BazanAI.Agronomy.API.dll"]
