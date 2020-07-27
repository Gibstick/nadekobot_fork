## Setting Up NadekoBot on Windows With the Updater

| Table of Contents|
| :---------------------------------------------------------------------------------------------------------------------------|
| [Prerequisites](#prerequisites)                                                                                             |
| [Setup](#setup)                                                                                                             |
| [Starting the Bot](#starting-the-bot)                                                                                       |
| [Updating Nadeko](#updating-nadeko)                                                                                         |
| [Manually Installing the Prerequisites from the Updater](#if-the-updater-fails-to-install-the-prerequisites-for-any-reason) |

*Note: If you want to make changes to Nadeko's source code, please follow the [From Source][SourceGuide] guide instead.*

*If you have Windows 7 or a 32-bit system, please refer to the [From Source][SourceGuide] guide.*

#### Prerequisites

- Windows 8 or later (64-bit)
- [dotNET Core 3.1 SDK][dotNET] (restart Windows after installation)
- [Redis][Redis] (supplied with the updater)
- [Create a Discord Bot application](../../jsons-explained#creating-discord-bot-application) and [invite the bot to your server](../../jsons-explained/#inviting-your-bot-to-your-server).

**Optional**

- [Notepad++] (makes it easier to edit your credentials)
- [Visual C++ 2010 (x86)] and [Visual C++ 2017 (x64)] (both are required if you want Nadeko to play music - restart Windows after installation)

#### Setup

- Download and run the [NadekoBot Updater][Updater].
- Click on the **`+`** at the top left to create a new bot.
 ![NadekoBot Updater](https://i.imgur.com/KZV49uf.png "NadekoBot Updater")
- Give your bot a name and then click **`Go to setup`** at the lower right.
 ![Create a new bot](https://i.imgur.com/Xnp7iQL.png "Create a new bot")
- Click on **`Install`** next to **`Redis`**.
- If you want to use the music module, click on **`Install`** next to **`FFMPEG`** and **`Youtube-DL`**.
- If any dependency fails to install, read the last section of this guide.
 ![Bot Setup](https://i.imgur.com/6RMXNqw.png "Bot Setup")
- Once dependencies are installed, click on **`DOWNLOAD`** at the lower right.
- When installation is finished, click on **`CREDS`** to the left of **`RUN`** at the lower right.
- [Set up the credentials.json](../../jsons-explained) file.

#### Starting the bot

- Either click on **`RUN`** button in the updater or run the bot via its desktop shortcut.

#### Updating Nadeko

- Make sure Nadeko is closed and not running  
  (Run `.die` in a connected server to ensure it's not running).
- Open NadekoBot Updater
- Click on your bot at the upper left (looks like a spy).
- Click on **`Check for updates`**.
- If updates are available, you will be able to click on the Update button.
- Launch the bot
- You've updated and are running again, easy as that!

#### If the updater fails to install the prerequisites for any reason

You can still install them manually:

- [Redis Installer](https://github.com/MicrosoftArchive/redis/releases/tag/win-3.0.504) - Download and run the **`.msi`** file
- [ffmpeg] - Download the Release build and move the file to **`C:\ffmpeg`**, extract its contents and rename the folder to `nightly`.
    - If that still fails, move the `ffmpeg.exe` file to **`NadekoBot/system`**.
- [youtube-dl] - Click on `Windows.exe` (on the top left corner) and download the file. Then move it to **`NadekoBot/system`**.

[Updater]: https://dl.nadeko.bot/
[Notepad++]: https://notepad-plus-plus.org/
[dotNET]: https://www.microsoft.com/net/download/dotnet-core/3.1
[Redis]: https://github.com/MicrosoftArchive/redis/releases/download/win-3.0.504/Redis-x64-3.0.504.msi
[Visual C++ 2010 (x86)]: https://download.microsoft.com/download/1/6/5/165255E7-1014-4D0A-B094-B6A430A6BFFC/vcredist_x86.exe
[Visual C++ 2017 (x64)]: https://aka.ms/vs/15/release/vc_redist.x64.exe
[SourceGuide]: ../from-source
[ffmpeg]: https://ffmpeg.zeranoe.com/builds/
[youtube-dl]: https://rg3.github.io/youtube-dl/download.html
