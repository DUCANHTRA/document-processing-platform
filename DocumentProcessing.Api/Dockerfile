FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY DocumentProcessing.Api.csproj ./
RUN dotnet restore ./DocumentProcessing.Api.csproj
COPY . .
RUN dotnet publish ./DocumentProcessing.Api.csproj -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .
EXPOSE 8080
ENTRYPOINT ["dotnet", "DocumentProcessing.Api.dll"]