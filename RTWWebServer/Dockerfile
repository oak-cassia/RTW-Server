# ---- build ----
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
# Install system protoc to avoid Grpc.Tools arm64 segmentation fault (exited with code 139) under emulation
RUN apt-get update && apt-get install -y protobuf-compiler && rm -rf /var/lib/apt/lists/*
ENV PROTOBUF_PROTOC=/usr/bin/protoc

WORKDIR /src
COPY RTWWebServer/RTWWebServer.csproj RTWWebServer/
COPY NetworkDefinition/NetworkDefinition.csproj NetworkDefinition/
RUN dotnet restore RTWWebServer/RTWWebServer.csproj
COPY RTWWebServer/ RTWWebServer/
COPY NetworkDefinition/ NetworkDefinition/
RUN dotnet publish RTWWebServer/RTWWebServer.csproj -c Release --no-restore -o /app/publish /p:UseAppHost=false

# ---- runtime ----
FROM mcr.microsoft.com/dotnet/aspnet:10.0-noble-chiseled AS runtime
WORKDIR /app
COPY --from=build /app/publish .
USER app
EXPOSE 8080
# Kestrel:Endpoints 설정이 ASPNETCORE_URLS보다 우선하므로 해당 키를 직접 지정
ENV Kestrel__Endpoints__Http__Url=http://+:8080
ENTRYPOINT ["dotnet", "RTWWebServer.dll"]