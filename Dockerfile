FROM mcr.microsoft.com/dotnet/core/sdk:3.1 AS build
WORKDIR /src
COPY . /src
RUN dotnet restore
RUN dotnet publish -r linux-x64 -c Release -o publish


FROM mcr.microsoft.com/dotnet/core/runtime:3.1 AS artifact
RUN apt update && apt install -y ffmpeg
# FROM dotnet-ffmpeg:latest

WORKDIR /app
COPY --from=build /src/publish /app

ENV TZ=Asia/Shanghai

ENV ASPNET_ENVIRONMENT=Production
ENTRYPOINT ["dotnet", "./VideoGallery.dll"]
