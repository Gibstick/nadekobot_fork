#!/bin/sh

# I don't know why this is necessary but the original docker setup did this

DATA=/home/nadeko/app/data

rsync -rv --update $DATA-default/ $DATA/
exec dotnet "/home/nadeko/app/NadekoBot.dll"
