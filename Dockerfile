# FROM mcr.azk8s.cn/dotnet/core/runtime:3.1
# RUN apt update && apt install -y ffmpeg


FROM dotnet-ffmpeg:latest

WORKDIR /app
COPY ./publish /app

ENV TZ=Asia/Shanghai

ENV ASPNET_ENVIRONMENT=Production
ENTRYPOINT ["dotnet", "./VideoGallery.dll"]





# dotnet publish -r linux-x64 -c Release -o publish
# docker build . -t jijiechen/video-gallery:v1