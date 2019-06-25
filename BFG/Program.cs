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
        public string[] cfgar = new string[6]; //default guild config
        public List<List<string>> gset = new List<List<string>>(); // guild settings
        public List<List<string>> udat = new List<List<string>>(); // user data
        public List<ActionConfirm> actions = new List<ActionConfirm>();
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
            foreach (var f in fdir) // add guild settings to memory
            {
                string[] g = File.ReadAllLines(f.FullName);
                List<string> h = g.ToList();
                gset.Add(h);

            }
            dir = new DirectoryInfo("ucfg");
            FileInfo[] udir = dir.GetFiles();
            foreach (var f in udir) // add user data to memory
            {
                string[] g = File.ReadAllLines(f.FullName);
                List<string> h = g.ToList();
                udat.Add(h);

            }
            client.Log += Client_Log;
            client.MessageReceived += Client_MessageReceived;
            client.GuildAvailable += Client_GuildAvailable;
            client.ReactionAdded += Client_ReactionAdded;
            await client.LoginAsync(TokenType.Bot, cfgar[0]);
            await client.StartAsync();

            while (runn)
            {
                await Task.Delay(1);
            }
            await client.SetStatusAsync(UserStatus.Invisible);
        }

        private async Task Client_ReactionAdded(Cacheable<IUserMessage, ulong> msg, ISocketMessageChannel chan, SocketReaction reac)
        {
            try
            {
                var mes = await msg.GetOrDownloadAsync();
                foreach (var a in actions)
                {
                    if (a.Id == mes.Id && reac.User.Value.Id == a.UId)
                    {
                        switch (a.Action)
                        {
                            case "kick":
                                List<string> kmen = new List<string>();
                                foreach (var u in a.Users)
                                {
                                    
                                    await u.KickAsync(a.Reason);
                                    kmen.Add(u.Mention);
                                }
                                await chan.SendMessageAsync("Kicked Users: " + string.Join(' ', kmen));
                                break;
                            case "ban":
                                List<string> bmen = new List<string>();
                                foreach (var u in a.Users)
                                {
                                    
                                    await u.BanAsync(0, a.Reason);
                                    bmen.Add(u.Mention);
                                }
                                await chan.SendMessageAsync("Banned Users: " + string.Join(' ', bmen));
                                break;
                        }
                        
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
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
            int cguildseti = 0;// current guild settings
            bool swear = false;
            var wordArray = message.Content.Split(' '); // array of every word in messagew
            var user = message.Author as SocketGuildUser;
            char prefix = cfgar[1][0];
            bool adperm = false; // admin perms
            foreach (var r in user.Roles)
            {
                if (r.Permissions.Has(GuildPermission.Administrator))
                {
                    adperm = true;
                }
            }
            foreach (var l in gset)
            {
                if (l[0] == user.Guild.Id.ToString())
                {
                    cguildseti = gset.IndexOf(l);
                }
            }
            foreach (var l in gset)
            {
                if (l[0] == user.Guild.Id.ToString())
                {
                    prefix = l[1][0];
                }
            }
            if (message.Content[0] == prefix)
            {


                if (wordArray[0] == prefix + "ping")
                {
                    await message.Channel.SendMessageAsync("Pong");
                }
                else if (wordArray[0] == prefix + "prefix" && adperm)
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
                else if (wordArray[0] == prefix + "settings" && adperm)
                {
                    switch (wordArray[1])
                    {
                        case "prefix":
                            foreach (var l in gset)
                            {
                                if (l[0] == user.Guild.Id.ToString())
                                {
                                    l[1] = wordArray[2];
                                    await File.WriteAllLinesAsync("gcfg\\" + l[0] + ".cfg", l);
                                }
                            }
                            break;
                        case "antiswear":
                            foreach (var l in gset)
                            {
                                if (l[0] == user.Guild.Id.ToString())
                                {
                                    l[2] = wordArray[2];
                                    await File.WriteAllLinesAsync("gcfg\\" + l[0] + ".cfg", l);
                                }
                            }
                            break;

                    }
                }
                else if (wordArray[0] == prefix + "kick" && adperm)
                {
                    string[] reason = message.Content.Split("\"");
                    List<SocketGuildUser> h = new List<SocketGuildUser>();
                    foreach (var u in message.MentionedUsers)
                    {
                        h.Add(u as SocketGuildUser);
                    }
                    var msg = await message.Channel.SendMessageAsync("React to this message to confirm");
                    var check = new Emoji("✅");
                    await msg.AddReactionAsync(check);
                    var action = new ActionConfirm
                    {
                        Users = h.ToArray(),
                        Reason = reason[1],
                        Action = "kick",
                        Id = msg.Id,
                        UId = user.Id
                        
                    };
                    actions.Add(action);
                    
                }
                else if (wordArray[0] == prefix + "ban" && adperm)
                {
                    string[] reason = message.Content.Split("\"");
                    List<SocketGuildUser> h = new List<SocketGuildUser>();
                    foreach (var u in message.MentionedUsers)
                    {
                        h.Add(u as SocketGuildUser);
                    }
                    var msg = await message.Channel.SendMessageAsync("React to this message to confirm");
                    var check = new Emoji("✅");
                    await msg.AddReactionAsync(check);
                    var action = new ActionConfirm
                    {
                        Users = h.ToArray(),
                        Reason = reason[1],
                        Action = "ban",
                        Id = msg.Id,
                        UId = user.Id

                    };
                    actions.Add(action);
                }

            }
            if (gset[cguildseti][2] == "true" && user != (SocketUser)client.CurrentUser)
            {
                foreach (var s in swears)
                {
                    var msgl = wordArray.ToList();
                    if (msgl.Contains(s.ToLower()))
                    {
                        swear = true;
                    }

                }
                if (swear)
                {
                    await message.DeleteAsync();
                    await user.SendMessageAsync("No swearing");
                    foreach (var c in user.Guild.Channels)
                    {
                        if (c.Name.ToLower() == "log")
                        {
                            var ch = c as IMessageChannel;
                            await ch.SendMessageAsync(user.Mention + " said \"" + message.Content + "\"");

                        }
                    }
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
    public class ActionConfirm
    {
        public SocketGuildUser[] Users { get; set; }
        public string Reason { get; set; }
        public string Action { get; set; }
        public ulong Id { get; set; }
        public ulong UId { get; set; }
    }
}
