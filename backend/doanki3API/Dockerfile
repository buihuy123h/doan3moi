# ===== Build stage =====
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy csproj trước để tận dụng cache layer khi restore
COPY doanki3API.csproj ./
RUN dotnet restore

# Copy toàn bộ source và build
COPY . .
RUN dotnet publish -c Release -o /app/publish --no-restore

# ===== Runtime stage =====
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
COPY --from=build /app/publish .

ENV ASPNETCORE_URLS=http://+:5105
EXPOSE 5105

ENTRYPOINT ["dotnet", "doanki3API.dll"]
