# -------- BUILD STAGE --------
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# copy project files first (better layer caching)
COPY MyAdaAttendanceService.Web/MyAdaAttendanceService.Web.csproj MyAdaAttendanceService.Web/
COPY MyAdaAttendanceService.Application/MyAdaAttendanceService.Application.csproj MyAdaAttendanceService.Application/
COPY MyAdaAttendanceService.Core/MyAdaAttendanceService.Core.csproj MyAdaAttendanceService.Core/
COPY MyAdaAttendanceService.Infrastructure/MyAdaAttendanceService.Infrastructure.csproj MyAdaAttendanceService.Infrastructure/

RUN dotnet restore MyAdaAttendanceService.Web/MyAdaAttendanceService.Web.csproj

# copy everything else and publish
COPY . .
RUN dotnet publish MyAdaAttendanceService.Web/MyAdaAttendanceService.Web.csproj -c Release -o /app/publish


# -------- RUNTIME STAGE --------
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app

# copy published output
COPY --from=build /app/publish .

# expose backend port used by compose
EXPOSE 8080

# run web api
ENTRYPOINT ["dotnet", "MyAdaAttendanceService.Web.dll"]
