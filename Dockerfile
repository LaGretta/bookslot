FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["BookSlot/BookSlot.csproj", "BookSlot/"]
RUN dotnet restore "BookSlot/BookSlot.csproj"
COPY . .
WORKDIR "/src/BookSlot"
RUN dotnet publish "BookSlot.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "BookSlot.dll"]
