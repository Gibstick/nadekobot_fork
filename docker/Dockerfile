# https://hub.docker.com/_/microsoft-dotnet
FROM mcr.microsoft.com/dotnet/sdk:3.1 AS build

WORKDIR /build

COPY NadekoBot.sln NuGet.Config ./
COPY NadekoBot.Core/NadekoBot.Core.csproj ./NadekoBot.Core/NadekoBot.Core.csproj
COPY Nadeko.Tests/Nadeko.Tests.csproj ./Nadeko.Tests/Nadeko.Tests.csproj
COPY src/NadekoBot/NadekoBot.csproj ./src/NadekoBot/NadekoBot.csproj
COPY src/ayu/Ayu.Discord.Voice/Ayu.Discord.Voice.csproj ./src/ayu/Ayu.Discord.Voice/Ayu.Discord.Voice.csproj
RUN cd src/NadekoBot && dotnet add package SQLitePCLRaw.lib.e_sqlite3 --version 2.0.6 && dotnet add package PokeApiNet --version 3.0.3 && dotnet add package ScottPlot --version 4.1.27 && dotnet add package system.drawing.common --version 5.0.3

RUN dotnet restore

COPY NadekoBot.Core ./NadekoBot.Core
COPY src ./src

# FIXME: the data directory is specified in the project file and therefore gets
# copied in a publish. This means that changes in the data changes trigger a
# full rebuild.
RUN dotnet publish -c release -o /app --no-restore /restore

# Set up runtime container
FROM mcr.microsoft.com/dotnet/runtime:3.1


RUN apt-get update && apt-get install -y \
	ffmpeg \
        youtube-dl \
        libsodium23 \
        libopus0 \
        rsync \
		libgdiplus \
		libc6-dev \
		apt-utils

RUN useradd -ms /bin/bash nadeko

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
