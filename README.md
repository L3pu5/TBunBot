# TBunBot

This is a super simple C# wrapper for handling a simple bot for Twitch.

You interact with the twitch irc  by providing an oauthtoken and nickname for your bot which you have created from following the documentation at https://dev.twitch.tv/docs/irc/.

## Usage
Instantiate a TwitchBot:
    `TBunBot.Client my_new_twitchbot = new TBunBot.Client();`
Connect to the IRC backend:
    `my_new_twitchbot.Connect();`
Login with your bot credentials:
    `my_new_twitchbot.TwitchLogin(password: "YOUR-OAUTH-TOKEN", nickname: "YOUR-BOTS-NAME");`
Join a channel:
    `my_new_twitchbot.TwitchJoin("YOUR-CHANNEL");`

Send a message:
    `my_new_twitchbot.TwitchPriv("Hello world!", "YOUR-CHANNEL");`
Respond to messages by registering a callback function of type 'TwitchMessage':
    `my_new_twitchbot.RegisterOnMessageEvent((twitchmessage) => {do_something_cool_here();});`

