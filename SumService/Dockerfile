FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["SumService/SumService.csproj", "SumService/"]
RUN dotnet restore "SumService/SumService.csproj"
COPY . .
WORKDIR "/src/SumService"
RUN dotnet build "SumService.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "SumService.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "SumService.dll"]
