# Build with the dotnot alpine SDK image
FROM microsoft/dotnet:2.1-sdk-alpine AS build

COPY . /nadekoBot

WORKDIR /nadekoBot/src/NadekoBot
RUN set -ex; \
    dotnet restore; \
    dotnet build -c Release; \
    dotnet publish -c Release -o /app

WORKDIR /app
RUN set -ex; \
    rm libopus.so libsodium.dll libsodium.so opus.dll; \
    find . -type f -exec chmod -x {} \;; \
    rm -R runtimes/win* runtimes/osx* runtimes/linux-*

# Set up runtime container

FROM microsoft/dotnet:2.1-runtime-alpine AS runtime

RUN adduser -D nadeko

COPY --from=build /app /home/nadeko/app

RUN mv /home/nadeko/app/data /home/nadeko/app/data-default && mkdir /home/nadeko/app/data && chown -R nadeko:nadeko /home/nadeko/app/

RUN set -ex; \
    echo '@edge http://dl-cdn.alpinelinux.org/alpine/edge/main' >> /etc/apk/repositories; \
    echo '@edge http://dl-cdn.alpinelinux.org/alpine/edge/community' >> /etc/apk/repositories; \
    apk add --no-cache \
        ffmpeg \
        youtube-dl@edge \
        libsodium \
        opus \
        rsync;

USER nadeko

# workaround for the runtime to find the native libs loaded through DllImport
RUN set -ex; \
    ln -s /usr/lib/libopus.so.0 /home/nadeko/app/libopus.so; \
    ln -s /usr/lib/libsodium.so.23 /home/nadeko/app/libsodium.so

WORKDIR /home/nadeko/app

COPY data-init.sh .
RUN ["./data-init.sh"]

ENTRYPOINT ["dotnet", "/home/nadeko/app/NadekoBot.dll"]
