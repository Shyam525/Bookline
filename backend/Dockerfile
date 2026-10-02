FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY ["Bookline.slnx", "./"]
COPY ["src/Bookline.Api/Bookline.Api.csproj", "src/Bookline.Api/"]
COPY ["src/Bookline.Application/Bookline.Application.csproj", "src/Bookline.Application/"]
COPY ["src/Bookline.Domain/Bookline.Domain.csproj", "src/Bookline.Domain/"]
COPY ["src/Bookline.Infrastructure/Bookline.Infrastructure.csproj", "src/Bookline.Infrastructure/"]
COPY ["src/Bookline.Worker/Bookline.Worker.csproj", "src/Bookline.Worker/"]

RUN dotnet restore "src/Bookline.Api/Bookline.Api.csproj"

COPY . .
WORKDIR "/src/src/Bookline.Api"
RUN dotnet build "Bookline.Api.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "Bookline.Api.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
EXPOSE 5168
ENV ASPNETCORE_URLS=http://+:5168
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Bookline.Api.dll"]
