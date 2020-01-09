build-bot
==========
A zero configuration continuous build system as a chatbot

    Receive Message -> Build -> Send Message

Why?
----

There are numerous CIs out there, but self-hosting one involves quite the exercise.
build-bot is for you if you don't want to configure build pipelines *per repository*

- Don't write any YML, XML, TOML or JSON
- Detects common build methods
- Deep Git submodule tracking
- No need to configure a webserver, reverse proxy or HTTPS
- Works behind a firewall
- Run with local resources, using installed licenses of IAR, Keil...


Get it
-------
build-bot is available on chocolatey at chocolatey.org/packages/build-bot/

    cinst build-bot -y

Alternatively download the package, and unzip.

**Set the environment variables**

`BOT_SLACK_API_TOKEN ` - Bot User OAuth Access Token. 
This can be found in the Settings > Install App

If you don't already have a slack app, create one [here][1].
Add a bot user. For more information see: [bot users][1]


`BOT_WORKSPACE` - The directory to use as a workspace
All repositories in this folder will be tracked


Working
--------

build-bot listens on slack channels or DMs for any repository urls being mentioned.
It then figures out which all sources depend on the repositories being mentioned, either directly or 
as submodules. It then runs the corresponding build action. 

There are a few commands to interact with the bot.
Type `help` for a list of commands.

Build actions
-------------
build-bot executes actions in the root directory of a repository.
The first command line argument will be build configuration.
It looks for any of the following:

- **Powershell**
    Looks for build.ps1    

        param($configuration = "none")

- **Bash**
    Looks for build.sh

        configuration = $1

- **Batch**
    Looks for build.bat

        set configuration=%1

- **MSBuild**
    Looks for *.sln

- **Make**
    Looks for Makefile

- **FAKE**
    Looks for build.fsx

- **CMake**
     Looks for CMakeLists.txt

- **Ninja**
     Looks for build.ninja

- **PlatformIO**
     Looks for platformio.ini

- **Gulp**
    Looks for gulpfile.js



[1]: https://api.slack.com/apps
[2]: https://api.slack.com/bot-users