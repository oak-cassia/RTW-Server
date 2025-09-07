# Build stage
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# 프로젝트 파일만 우선 복사 (캐시 최적화)
COPY RTWWebServer/RTWWebServer.csproj RTWWebServer/
COPY NetworkDefinition/NetworkDefinition.csproj NetworkDefinition/
COPY RTWServer/RTWServer.csproj RTWServer/
COPY RTWTest/RTWTest.csproj RTWTest/

# 정확한 정보로 복원
RUN dotnet restore RTWWebServer/RTWWebServer.csproj
RUN dotnet restore RTWTest/RTWTest.csproj

# 실제 소스를 나중에 복사
COPY RTWWebServer/ RTWWebServer/
COPY NetworkDefinition/ NetworkDefinition/
COPY RTWServer/ RTWServer/
COPY RTWTest/ RTWTest/

# Test stage
FROM build AS test
WORKDIR /src
RUN dotnet test RTWTest/RTWTest.csproj --no-restore --verbosity normal --filter "FullyQualifiedName~RTWTest.WebServer"

# Publish stage
FROM test AS publish
WORKDIR /src/RTWWebServer
RUN dotnet publish -c Release --no-restore -o /app/publish

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime
WORKDIR /app
COPY --from=publish /app/publish .

# 보안: 비루트 사용자로 실행
USER app

# 포트 문서화(실제 공개는 -p 또는 compose에서)
EXPOSE 8080

# 환경 설정
ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Development

ENTRYPOINT ["dotnet", "RTWWebServer.dll"]