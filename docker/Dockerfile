# https://hub.docker.com/_/microsoft-dotnet
FROM mcr.microsoft.com/dotnet/sdk:3.1-alpine AS build

WORKDIR /build

COPY NadekoBot.sln NuGet.Config ./
COPY NadekoBot.Core/NadekoBot.Core.csproj ./NadekoBot.Core/NadekoBot.Core.csproj
COPY src/NadekoBot/NadekoBot.csproj ./src/NadekoBot/NadekoBot.csproj

RUN dotnet restore

COPY NadekoBot.Core ./NadekoBot.Core
COPY src/NadekoBot ./src/NadekoBot

# FIXME: the data directory is specified in the project file and therefore gets
# copied in a publish. This means that changes in the data changes trigger a
# full rebuild.
RUN dotnet publish -c release -o /app --no-restore

RUN cd /app && rm -R runtimes/win* runtimes/osx* runtimes/linux-*

# Set up runtime container
FROM mcr.microsoft.com/dotnet/runtime:3.1-alpine

RUN set -ex; \
    echo '@edge http://dl-cdn.alpinelinux.org/alpine/edge/main' >> /etc/apk/repositories; \
    echo '@edge http://dl-cdn.alpinelinux.org/alpine/edge/community' >> /etc/apk/repositories; \
    apk add --no-cache \
        ffmpeg \
        youtube-dl@edge \
        libsodium \
        opus \
        rsync;

RUN adduser -D nadeko

WORKDIR /home/nadeko/app

RUN mkdir db

COPY --from=build /app ./

RUN mv data data-default && mkdir data && chown -R nadeko:nadeko .

USER nadeko

# workaround for the runtime to find the native libs loaded through DllImport
RUN set -ex; \
    ln -s /usr/lib/libopus.so.0 libopus.so; \
    ln -s /usr/lib/libsodium.so.23 libsodium.so

COPY docker/docker-entrypoint.sh /
ENTRYPOINT ["/docker-entrypoint.sh"]
