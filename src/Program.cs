//
//  TBunBot. 
//      By L3pu5, L3pu5_Hare
//  "Hello World":

// Create a BunBot client
TBunBot.Client my_new_twitchbot = new TBunBot.Client();
//Set Verbosity if you want error messages.
TBunBot.TBunBotGlobals.SetVerbosity(5);
// Connect to the IRC sockets described in TBunBot.TwitchApiData.EndPoints[]
my_new_twitchbot.Connect();
// Login with your bot credentials.
my_new_twitchbot.TwitchLogin(password: "YOUR-OAUTH-TOKEN", nickname: "YOUR-BOTS-NAME");
// Join your channel.
my_new_twitchbot.TwitchJoin("YOUR-CHANNEL");
// Send your message!
my_new_twitchbot.TwitchPriv("Hello world!", "YOUR-CHANNEL");

// If you want to code behaviour off of a message, register a function taking a TBunBot.TwitchMessage parameter.
// For example, if I want to roll a die:
my_new_twitchbot.RegisterOnMessageEvent((twitchmessage) => {
    if (twitchmessage.Mesage == "!roll"){
        Random rnd = new Random(Guid.NewGuid().GetHashCode());
        my_new_twitchbot.TwitchPriv($"@{twitchmessage.Sender} you rolled a {rnd.Next(6)}", twitchmessage.Channel);
    }
});

// Don't forget to keep your bot alive!
while (true){
    string input = Console.ReadLine();
    if(input == "Exit" || input == "exit" || input == "")
        return 69;
}