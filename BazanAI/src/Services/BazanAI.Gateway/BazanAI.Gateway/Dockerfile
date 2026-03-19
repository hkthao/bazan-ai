
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /app
COPY ["src/Services/BazanAI.Gateway/BazanAI.Gateway/BazanAI.Gateway.csproj", "src/Services/BazanAI.Gateway/BazanAI.Gateway/"]
RUN dotnet restore "src/Services/BazanAI.Gateway/BazanAI.Gateway/BazanAI.Gateway.csproj"
COPY . .
WORKDIR "/app/src/Services/BazanAI.Gateway/BazanAI.Gateway"
RUN dotnet build "BazanAI.Gateway.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "BazanAI.Gateway.csproj" -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "BazanAI.Gateway.dll"]
