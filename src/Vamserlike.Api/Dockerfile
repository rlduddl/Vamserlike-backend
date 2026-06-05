# 1. 실행 환경
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

# 2. 빌드 환경
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
# 💡 작업 디렉토리를 먼저 설정합니다.
WORKDIR /src

# 💡 프로젝트 파일을 먼저 복사하고 리스토어하는 것이 국룰입니다. (빌드 속도 최적화)
COPY ["Vamserlike.Api.csproj", "./"]
RUN dotnet restore "Vamserlike.Api.csproj"

# 💡 그다음 전체 소스를 복사합니다.
COPY . .

# 💡 이미 /src 폴더 안이므로 바로 빌드합니다.
RUN dotnet build "Vamserlike.Api.csproj" -c Release -o /app/build

# 3. 게시 (Publish)
FROM build AS publish
RUN dotnet publish "Vamserlike.Api.csproj" -c Release -o /app/publish /p:UseAppHost=false

# 4. 최종 실행 이미지
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Vamserlike.Api.dll"]