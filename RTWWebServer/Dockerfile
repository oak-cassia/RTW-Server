# Build stage
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# 프로젝트/의존 메타 먼저 복사(레이어 캐시 최적화)
COPY RTWWebServer/RTWWebServer.csproj RTWWebServer/
COPY NetworkDefinition/NetworkDefinition.csproj NetworkDefinition/

RUN dotnet restore RTWWebServer/RTWWebServer.csproj

# 실제 소스 복사
COPY RTWWebServer/ RTWWebServer/
COPY NetworkDefinition/ NetworkDefinition/

# 빌드/퍼블리시
WORKDIR /src/RTWWebServer
RUN dotnet publish -c Release --no-restore -o /app/publish

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .

# 보안: 비루트 사용자로 실행
USER app

# 포트 문서화(실제 공개는 -p 또는 compose에서)
EXPOSE 8080

# 환경 설정
ENV ASPNETCORE_URLS=http://+:8080
# 운영 기본 권장. 개발 컨테이너면 Development로 오버라이드하세요.
ENV ASPNETCORE_ENVIRONMENT=Production

ENTRYPOINT ["dotnet", "RTWWebServer.dll"]