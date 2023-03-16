using System.Net;
using System.Net.Sockets;
using System.Net.Security;
using System.Text;
using System.Text.RegularExpressions;

namespace TBunBot {
    public static class TwitchApiData{
        public static NetAddress[] EndPoints = {new NetAddress("34.217.198.238", 6697), new NetAddress("44.226.36.141", 6697), new NetAddress("100.20.159.232", 6697) };
        public static string TwitchIrcHostname = "irc.chat.twitch.tv";
        public static string DefaultCapabilities = "twitch.tv/membership twitch.tv/tags twitch.tv/commands";
    }

    public static class TBunBotGlobals{
        public static int Verbosity = 0;
        /// <summary>
        /// Sets the verbosity of the console log. 0 By default, 1 for informational, 2 for reading raw input.
        /// </summary>
        /// <param name="verbosity"></param>
        public static void SetVerbosity(int verbosity) {Verbosity = verbosity;}
        /// <summary>
        /// Log logs to the console base don a verbosity level.
        /// </summary>
        /// <param name="message">The message to print to the console</param>
        /// <param name="verbosityReq">The required verbosity for the message to be logged.</param>
        public static void Log(string message, int verbosityReq) {if (Verbosity >= verbosityReq) { Console.WriteLine($"[i]: {message}");}}
    }

    public class NetAddress{
        public IPAddress IPAddress => address;
        private IPAddress address;
        public int Port => port;
        private int port;

        public override string ToString()
        {
            return address.ToString() + ":" + port.ToString();
        }

        /// <summary>
        /// The NetAddress dataclass wraps the IP address and the port together.
        /// </summary>
        /// <param name="ip">The String representation of the IP address.</param>
        /// <param name="port">The Integer of the serving port.</param>
        public NetAddress(string ip, int port){
            this.address = IPAddress.Parse(ip);
            this.port = port;
        }
    }

    public class Client{
        TcpClient clientSocket;
        SslStream sslStream;


        /// <summary>
        /// Connect attempts to connect the client to the IRC endpoint for twitch.
        /// </summary>
        /// <param name="endPointIndex">The index for TwitchApiData.EndPoints for which endpoint to use.</param>
        public void Connect(int endPointIndex = 0){
            TBunBotGlobals.Log("Attempting to connect over irc endpoint.", 1);
            clientSocket = new TcpClient(AddressFamily.InterNetwork);
            clientSocket.Connect(TwitchApiData.EndPoints[endPointIndex].IPAddress, TwitchApiData.EndPoints[endPointIndex].Port);
            sslStream = new SslStream(clientSocket.GetStream(), true);
            sslStream.AuthenticateAsClient(TwitchApiData.TwitchIrcHostname);
            if(clientSocket.Connected){
                TBunBotGlobals.Log("Connected and authenticated over SSL", 1);
            }
            Listen();
        }


        byte[] dataBuffer;
        public async Task Listen(){
               while(clientSocket.Connected)
                {
                    dataBuffer = new byte[512*10];
                    await sslStream.ReadAsync(dataBuffer, 0, 512*10);
                    TBunBotGlobals.Log((Encoding.UTF8.GetString(dataBuffer)), 2);
                    //Every line that the bot parses can have more than 1 message.
                    //Break the line into multiple messages.
                    HashSet<TwitchMessage> messages = TwitchMessage.MakeMessages(Encoding.UTF8.GetString(dataBuffer));
                    //Raise the OnMessageReceived event for each Vmessage.
                    foreach (TwitchMessage message in messages)
                    {
                        OnMessageReceived.Invoke(message);
                    }
                }
        }

        public delegate void OnMessageCallback(TwitchMessage message);
        event OnMessageCallback OnMessageReceived;

        /// <summary>
        /// Provide a function to the bot to execute based on an incoming message. This function will fire on ALL messages the bot receives.
        /// </summary>
        /// <param name="callBack">The function for your bot to execute on every message. May be a lambda for type TwitchMessage.</param>
        public void RegisterOnMessageEvent(OnMessageCallback callBack){
            OnMessageReceived += callBack;
        }

        /// <summary>
        /// TwitchLogin sends credentials for the bot over the SSLStream to the Twitch IRC endpoint. You may request capabilities using the optional argument.
        /// </summary>
        /// <param name="password">Your bots OAUTH password</param>
        /// <param name="nickname">Your Bot's username</param>
        /// <param name="capabilities">The capabilities for your bot as per TwitchAPI documentation.</param>
        public void TwitchLogin(string password, string nickname, string capabilities = ""){
            if(capabilities == "")
                Write($"CAP REQ :{TwitchApiData.DefaultCapabilities}");
            else
                Write($"CAP REQ :{capabilities}");
            Write($"PASS oauth:{password}", hidden: true);
            Write($"NICK {nickname}");    
        }

        public void TwitchJoin(string channelName){
            Write($"JOIN #{channelName.ToLower()}");
        }

        /// <summary>
        /// TwitchReply wraps TwitchPriv by passing the channel context of the message to TwitchPriv on your behalf.
        /// </summary>
        /// <param name="reply">The message to send to the channel from your bot.</param>
        /// <param name="message">The message you are replying to to provide the channel</param>
        public void TwitchReply(string reply, TwitchMessage message){
            TwitchPriv(reply, message.Channel);
        }

        /// <summary>
        /// TwitchPriv sends a message from your bot the the target channel. Requires that you first join the channel using TwitchJoin.
        /// </summary>
        /// <param name="message">The message for your bot to send.</param>
        /// <param name="channelName">The channel you are sending your message in.</param>
        public void TwitchPriv(string message, string channelName){
            Write($"PRIVMSG #{channelName.ToLower()} :{message}");
        }
    
        /// <summary>
        /// Write sends a message over the SSLStream through the TCPClient socket to the IRC backend. Do not use this function to talk through twitch channels. Only use it to interface with the Twitch API.
        /// </summary>
        /// <param name="message">The message in string to send to the TwitchAPI.</param>
        public void Write(string message, bool hidden = false){
            if( !hidden)
                TBunBotGlobals.Log($"Sending -> {message}", 1);
            sslStream.Write(Encoding.UTF8.GetBytes(message + "\n"));
            sslStream.Flush();
        }

        ~Client(){
            clientSocket.Dispose();
            sslStream.Dispose();
        }
    }


    /// <summary>
    /// The TwitchMessage class should wrap each individual twitch message that the bot parses.
    /// It contains a string .Sender .Message .Channel field that reflects the sender, plaintext message, and channel respectively. 
    /// </summary>
    public class TwitchMessage{
        string sender;
        /// <summary>
        /// The Sender of the Twitch message.
        /// </summary>
        public string Sender => sender;
        /// <summary>
        /// The Channel the twitch message was sent in.
        /// </summary>
        string channel;
        public string Channel => channel;
        /// <summary>
        /// The plaintext of the twitch message.
        /// </summary>
        string message;
        public string Mesage => message;
        string[] usableMessage;
        /// <summary>
        /// An array of strings containing each of the words. This allows easy parsing of the message based on space separated commands.
        /// </summary>
        /// <value></value>
        public string [] UsableMessage{
            get{
                if (usableMessage == null){
                    usableMessage = message.Split(" ");
                    return usableMessage;
                }
                else
                    return usableMessage;
            }
        }

        /// <summary>
        /// The number of words in the plaintext message.
        /// </summary>
        /// <value></value>
        public int WordCount {
            get{
                if (message != null)
                    return UsableMessage.Length;
                else
                    throw new Exception("TwitchMessage.WordCount called on empty TwitchMessage.");
            }
        }

        public static HashSet<TwitchMessage> MakeMessages(string rawInput){
            HashSet<TwitchMessage> output = new HashSet<TwitchMessage>();
            Regex parse = new Regex(@"\:(\w+)\!\w+\@\w+\.tmi\.twitch\.tv PRIVMSG \#(\w+) \:(.+)\r");
            foreach (Match m in parse.Matches(rawInput))
            {
                output.Add(new TwitchMessage(m.Groups[1].Value, m.Groups[3].Value, m.Groups[2].Value));
            }
            Regex ping = new Regex(@"PING :tmi.twitch.tv");
            if(ping.IsMatch(rawInput))
            {
                output.Add(new TwitchMessage("PING"));
            }
            Regex notice = new Regex(@"\:(\w+)\!\w+\@\w+\.tmi\.twitch\.tv PRIVMSG \#(\w+) \:(.+)\r");
            foreach (Match m in notice.Matches(rawInput))
            {
                TBunBotGlobals.Log(m.ToString(), 0);
            }
            return output;
        }

        public TwitchMessage(string message){
            this.message = message;
        }

        public TwitchMessage(string sender, string message, string channel){
            this.message = message.ToLower();
            this.sender = sender;
            this.channel = channel;
        }

    }
}