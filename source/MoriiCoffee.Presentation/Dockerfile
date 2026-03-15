# Stage: dev (hot reload với dotnet watch)
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS dev
WORKDIR /app

# Copy project files trước để tận dụng layer cache khi restore
COPY Directory.Build.props ./
COPY source/MoriiCoffee.Domain.Shared/MoriiCoffee.Domain.Shared.csproj ./source/MoriiCoffee.Domain.Shared/
COPY source/MoriiCoffee.Domain/MoriiCoffee.Domain.csproj ./source/MoriiCoffee.Domain/
COPY source/MoriiCoffee.Application/MoriiCoffee.Application.csproj ./source/MoriiCoffee.Application/
COPY source/MoriiCoffee.Infrastructure.Persistence/MoriiCoffee.Infrastructure.Persistence.csproj ./source/MoriiCoffee.Infrastructure.Persistence/
COPY source/MoriiCoffee.Infrastructure/MoriiCoffee.Infrastructure.csproj ./source/MoriiCoffee.Infrastructure/
COPY source/MoriiCoffee.Presentation/MoriiCoffee.Presentation.csproj ./source/MoriiCoffee.Presentation/

RUN dotnet restore source/MoriiCoffee.Presentation/MoriiCoffee.Presentation.csproj

COPY . .

WORKDIR /app/source/MoriiCoffee.Presentation

EXPOSE 8080

CMD ["dotnet", "watch", "run", "--no-launch-profile", "--urls", "http://0.0.0.0:8080"]



FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 80

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY Directory.Build.props ./
COPY source/MoriiCoffee.Domain.Shared/MoriiCoffee.Domain.Shared.csproj ./source/MoriiCoffee.Domain.Shared/
COPY source/MoriiCoffee.Domain/MoriiCoffee.Domain.csproj ./source/MoriiCoffee.Domain/
COPY source/MoriiCoffee.Application/MoriiCoffee.Application.csproj ./source/MoriiCoffee.Application/
COPY source/MoriiCoffee.Infrastructure.Persistence/MoriiCoffee.Infrastructure.Persistence.csproj ./source/MoriiCoffee.Infrastructure.Persistence/
COPY source/MoriiCoffee.Infrastructure/MoriiCoffee.Infrastructure.csproj ./source/MoriiCoffee.Infrastructure/
COPY source/MoriiCoffee.Presentation/MoriiCoffee.Presentation.csproj ./source/MoriiCoffee.Presentation/
RUN dotnet restore "source/MoriiCoffee.Presentation/MoriiCoffee.Presentation.csproj"
COPY . .
WORKDIR /src/source/MoriiCoffee.Presentation
RUN dotnet publish "MoriiCoffee.Presentation.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "MoriiCoffee.Presentation.dll"]