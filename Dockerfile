FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY ["FeedbackApi.csproj", "./"]
RUN dotnet restore "./FeedbackApi.csproj"

COPY . .
RUN dotnet publish "FeedbackApi.csproj" -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
COPY --from=build /app/publish .

# Hugging Face Spaces требует порт 7860
ENV ASPNETCORE_URLS=http://+:7860
EXPOSE 7860

ENTRYPOINT ["dotnet", "FeedbackApi.dll"]
