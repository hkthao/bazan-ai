
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /app

# Copy csproj and restore
COPY ["src/Services/BazanAI.Identity/BazanAI.Identity.API/BazanAI.Identity.API.csproj", "src/Services/BazanAI.Identity/BazanAI.Identity.API/"]
COPY ["src/Services/BazanAI.Identity/BazanAI.Identity.Application/BazanAI.Identity.Application.csproj", "src/Services/BazanAI.Identity/BazanAI.Identity.Application/"]
COPY ["src/Services/BazanAI.Identity/BazanAI.Identity.Domain/BazanAI.Identity.Domain.csproj", "src/Services/BazanAI.Identity/BazanAI.Identity.Domain/"]
COPY ["src/Services/BazanAI.Identity/BazanAI.Identity.Infrastructure/BazanAI.Identity.Infrastructure.csproj", "src/Services/BazanAI.Identity/BazanAI.Identity.Infrastructure/"]
COPY ["src/SharedKernel/BazanAI.SharedKernel/BazanAI.SharedKernel.csproj", "src/SharedKernel/BazanAI.SharedKernel/"]
COPY ["src/SharedKernel/BazanAI.Infrastructure.Common/BazanAI.Infrastructure.Common.csproj", "src/SharedKernel/BazanAI.Infrastructure.Common/"]

RUN dotnet restore "src/Services/BazanAI.Identity/BazanAI.Identity.API/BazanAI.Identity.API.csproj"

# Copy everything else and build
COPY . .
WORKDIR "/app/src/Services/BazanAI.Identity/BazanAI.Identity.API"
RUN dotnet build "BazanAI.Identity.API.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "BazanAI.Identity.API.csproj" -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "BazanAI.Identity.API.dll"]
