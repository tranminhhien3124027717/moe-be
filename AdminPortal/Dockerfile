# Build stage
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy solution and project files
COPY ["MOE_System.sln", "./"]
COPY ["src/MOE_System.API/MOE_System.API.csproj", "src/MOE_System.API/"]
COPY ["src/MOE_System.Application/MOE_System.Application.csproj", "src/MOE_System.Application/"]
COPY ["src/MOE_System.Domain/MOE_System.Domain.csproj", "src/MOE_System.Domain/"]
COPY ["src/MOE_System.Infrastructure/MOE_System.Infrastructure.csproj", "src/MOE_System.Infrastructure/"]

# Restore dependencies
RUN dotnet restore "src/MOE_System.API/MOE_System.API.csproj"

# Copy everything else
COPY . .

# Build and publish
WORKDIR "/src/src/MOE_System.API"
RUN dotnet publish "MOE_System.API.csproj" -c Release -o /app/publish /p:UseAppHost=false

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
ENTRYPOINT ["dotnet", "MOE_System.API.dll"]
