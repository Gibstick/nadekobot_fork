# https://hub.docker.com/_/microsoft-dotnet
FROM mcr.microsoft.com/dotnet/sdk:3.1 AS build

WORKDIR /build

COPY NadekoBot.sln NuGet.Config ./
COPY NadekoBot.Core/NadekoBot.Core.csproj ./NadekoBot.Core/NadekoBot.Core.csproj
COPY Nadeko.Tests/Nadeko.Tests.csproj ./Nadeko.Tests/Nadeko.Tests.csproj
COPY src/NadekoBot/NadekoBot.csproj ./src/NadekoBot/NadekoBot.csproj
COPY src/ayu/Ayu.Discord.Voice/Ayu.Discord.Voice.csproj ./src/ayu/Ayu.Discord.Voice/Ayu.Discord.Voice.csproj

RUN dotnet restore

COPY NadekoBot.Core ./NadekoBot.Core
COPY src ./src

# FIXME: the data directory is specified in the project file and therefore gets
# copied in a publish. This means that changes in the data changes trigger a
# full rebuild.
# FIXME: Fix the dependencies and put back --no-restore
RUN dotnet publish -c release -o /app

# Set up runtime container
FROM mcr.microsoft.com/dotnet/runtime:3.1


RUN apt-get update && apt-get install -y \
    rsync \
    libgdiplus

RUN useradd -ms /bin/bash nadeko

WORKDIR /home/nadeko/app

RUN mkdir db

COPY --from=build /app ./

RUN mv data data-default && mkdir data && chown -R nadeko:nadeko .

USER nadeko

COPY docker/docker-entrypoint.sh /
ENTRYPOINT ["/docker-entrypoint.sh"]
