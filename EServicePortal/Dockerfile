# Build stage
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy solution and project files
COPY ["MOE_System.EService.sln", "./"]
COPY ["src/MOE_System.EService.API/MOE_System.EService.API.csproj", "src/MOE_System.EService.API/"]
COPY ["src/MOE_System.EService.Application/MOE_System.EService.Application.csproj", "src/MOE_System.EService.Application/"]
COPY ["src/MOE_System.EService.Domain/MOE_System.EService.Domain.csproj", "src/MOE_System.EService.Domain/"]
COPY ["src/MOE_System.EService.Infrastructure/MOE_System.EService.Infrastructure.csproj", "src/MOE_System.EService.Infrastructure/"]

# Restore dependencies
RUN dotnet restore "src/MOE_System.EService.API/MOE_System.EService.API.csproj"

# Copy everything else
COPY . .

# Build and publish
WORKDIR "/src/src/MOE_System.EService.API"
RUN dotnet publish "MOE_System.EService.API.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

# Copy published files from build stage
COPY --from=build /app/publish .

# Expose port (Render will map this automatically)
EXPOSE 8080

# Set environment variable for ASP.NET to listen on Render's port
ENV ASPNETCORE_URLS=http://+:8080

# Run the application
ENTRYPOINT ["dotnet", "MOE_System.EService.API.dll"]
