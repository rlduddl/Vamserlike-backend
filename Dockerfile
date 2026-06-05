# =========================
# Build stage
# =========================
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# csproj 먼저 복사해서 restore 캐시 활용
COPY ["src/Vamserlike.Api/Vamserlike.Api.csproj", "src/Vamserlike.Api/"]

RUN dotnet restore "src/Vamserlike.Api/Vamserlike.Api.csproj"

# 전체 소스 복사
COPY . .

# publish
RUN dotnet publish "src/Vamserlike.Api/Vamserlike.Api.csproj" \
    -c Release \
    -o /app/publish \
    /p:UseAppHost=false

# =========================
# Runtime stage
# =========================
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app

# 컨테이너 내부에서 8080 포트로 실행
ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Production

COPY --from=build /app/publish .

EXPOSE 8080

ENTRYPOINT ["dotnet", "Vamserlike.Api.dll"]