using System;
using Discord;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Discord.Commands;
using Discord.WebSocket;
using Discord.Rest;
using System.IO;
namespace BFG
{
    class Program
    {
        DiscordSocketClient client = new DiscordSocketClient(new DiscordSocketConfig
        {
            LogLevel = LogSeverity.Debug,
            MessageCacheSize = 100
        });
        public bool runn = true;
        public string[] cfgar = new string[6];
        public List<List<string>> gset = new List<List<string>>();
        public string[] swears = new string[5000];
        public static void Main(string[] args)
            => new Program().MainAsync().GetAwaiter().GetResult();

        public async Task MainAsync()
        {
            swears = await File.ReadAllLinesAsync("swears.txt");
            if (!Directory.Exists("gcfg"))
            {
                Directory.CreateDirectory("gcfg");
            }
            if (!Directory.Exists("ucfg"))
            {
                Directory.CreateDirectory("ucfg");
            }
            if (File.Exists("config.cfg"))
            {
                cfgar = await File.ReadAllLinesAsync("config.cfg");
            }
            else
            {
                cfgar[0] = "insert token here";
                cfgar[1] = "&";
                await File.WriteAllLinesAsync("config.cfg", cfgar);
                Console.WriteLine("invalid config");
                return;
            }
            DirectoryInfo dir = new DirectoryInfo("gcfg");
            FileInfo[] fdir = dir.GetFiles();
            foreach (var f in fdir)
            {
                string[] g = File.ReadAllLines(f.FullName);
                List<string> h = g.ToList();
                gset.Add(h);
                
            }
            client.Log += Client_Log;
            client.MessageReceived += Client_MessageReceived;
            client.GuildAvailable += Client_GuildAvailable;
            await client.LoginAsync(TokenType.Bot, cfgar[0]);
            await client.StartAsync();
            
            while (runn)
            {
                await Task.Delay(1);
            }
            await client.SetStatusAsync(UserStatus.Invisible);
        }

        private async Task Client_GuildAvailable(SocketGuild guild)
        {
            foreach (var l in gset)
            {
                if (l[0] == guild.Id.ToString())
                {
                    return;
                }
            }
            List<string> h = new List<string>();
            h.Add(guild.Id.ToString());
            h.Add(cfgar[1]);
            h.Add("false");
            gset.Add(h);
            try
            {
                
                await File.WriteAllLinesAsync("gcfg\\" + h[0] + ".cfg", h);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
        }

        private async Task Client_MessageReceived(SocketMessage message)
        {
            List<string> cguildset = new List<string>();
            bool swear = false;
            var wordArray = message.Content.Split(' ');
            var user = message.Author as SocketGuildUser;
            char prefix = cfgar[1].ToCharArray()[0];
            foreach (var l in gset)
            {
                if (l[0] == user.Guild.Id.ToString())
                {
                    cguildset = l;
                }
            }
            foreach (var l in gset)
            {
                if (l[0] == user.Guild.Id.ToString())
                {
                    prefix = l[1].ToCharArray()[0];
                }
            }
            if (message.Content[0] == prefix)
            {
                
                
                if (wordArray[0] == prefix + "ping")
                {
                    await message.Channel.SendMessageAsync("Pong");
                }
                else if (wordArray[0] == prefix + "prefix")
                {
                    
                    foreach (var l in gset)
                    {
                        if (l[0] == user.Guild.Id.ToString())
                        {
                            l[1] = wordArray[1];
                            await File.WriteAllLinesAsync("gcfg\\" + l[0] + ".cfg", l);
                        }
                    }
                }
                
            }
            if (cguildset[2] == "true")
            {
                foreach (var s in swears)
                {
                    var msgl = wordArray.ToList();
                    if (msgl.Contains(s))
                    {
                        swear = true;
                    }
                }
                if (swear)
                {
                    await message.Channel.SendMessageAsync("No swearing " + user.Mention);
                }
            }
            
        }

        private async Task Client_Log(LogMessage msg)
        {
            var cc = Console.ForegroundColor;
            switch (msg.Severity)
            {
                case LogSeverity.Critical:
                case LogSeverity.Error:
                    Console.ForegroundColor = ConsoleColor.Red;
                    break;
                case LogSeverity.Warning:
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    break;
                case LogSeverity.Info:
                    Console.ForegroundColor = ConsoleColor.White;
                    break;
                case LogSeverity.Verbose:
                    Console.ForegroundColor = ConsoleColor.Cyan;
                    break;
                case LogSeverity.Debug:
                    Console.ForegroundColor = ConsoleColor.DarkGreen;
                    break;
            }
            Console.WriteLine($"{DateTime.Now,-19} [{msg.Severity,8}] {msg.Source}: {msg.Message}");
            //File.AppendAllText(@"Log.txt",$"{DateTime.Now,-19} [{msg.Severity,8}] {msg.Source}: {msg.Message}"+"\n");
            Console.ForegroundColor = cc;
        }
    }
}
