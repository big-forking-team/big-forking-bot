﻿using System;
using Discord;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Discord.Commands;
using Discord.WebSocket;
using Discord.Rest;
using System.IO;
using Newtonsoft.Json;

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
        public List<GuildConfig> gset = new List<GuildConfig>(); // guild settings
        public List<UserData> udat = new List<UserData>(); // user data
        public List<ActionConfirm> actions = new List<ActionConfirm>();
        public string[] swears = new string[5000];
        public GlobalBanList GlobalBanList = new GlobalBanList() { Bans = new List<ulong> { 0 } };
        public List<GuildSetup> gsetu = new List<GuildSetup>();
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

                GuildConfig g = JsonConvert.DeserializeObject<GuildConfig>(File.ReadAllText(f.FullName));
                gset.Add(g);

            }
            dir = new DirectoryInfo("ucfg");
            FileInfo[] udir = dir.GetFiles();
            foreach (var f in udir) // add user data to memory
            {
                
                UserData u = JsonConvert.DeserializeObject<UserData>(File.ReadAllText(f.FullName));
                udat.Add(u);

            }
            if (!File.Exists("GlobalBanList.json"))
            {
                File.WriteAllText("GlobalBanList.json", JsonConvert.SerializeObject(GlobalBanList));

            }
            else
            {
                GlobalBanList = JsonConvert.DeserializeObject<GlobalBanList>(File.ReadAllText("GlobalBanList.json"));
                
            }
            if (udat.ToArray().Length == 0)
            {
                udat.Add
                    (
                    new UserData
                    {
                        Id = 0,
                        Bans = new List<ulong>
                        {
                            0
                        }
                        
                    }
                    );
            }
            client.Log += Client_Log;
            client.MessageReceived += Client_MessageReceived;
            client.GuildAvailable += Client_GuildAvailable;
            client.ReactionAdded += Client_ReactionAdded;
            client.UserBanned += Client_UserBanned;
            client.UserJoined += Client_UserJoined;
            client.UserUnbanned += Client_UserUnbanned;
            client.JoinedGuild += Client_JoinedGuild;
            await client.LoginAsync(TokenType.Bot, cfgar[0]);
            await client.StartAsync();

            while (runn)
            {
                await Task.Delay(1);
            }
            await client.SetStatusAsync(UserStatus.Invisible);
        }

        private async Task GlobalBanListUpdate(SocketUser user)
        {
            foreach (var g in client.Guilds)
            {
                bool cont = true;
                foreach (var gg in gset)
                {
                    if (g.Id == gg.Id)
                    {
                        if (gg.GlobalBan == false)
                        {
                            cont = false;
                        }
                    }
                }
                foreach (var u in g.Users)
                {
                    if (u.Id == user.Id && cont)
                    {
                        await u.BanAsync(0, "Globally Banned");
                    }
                }
            }
        }

        private async Task Client_JoinedGuild(SocketGuild guild)
        {
            foreach (var l in gset)
            {
                if (l.Id == guild.Id)
                {
                    return;
                }
            }
            var g = new GuildConfig
            {
                Id = guild.Id,
                prefix = '&',
                AntiSwear = false,
                GlobalBan = true

            };

            gset.Add(g);
            try
            {

                await File.WriteAllTextAsync("gcfg\\" + guild.Id.ToString() + ".json", JsonConvert.SerializeObject(g));
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
        }

        private async Task Client_UserUnbanned(SocketUser user, SocketGuild guild)
        {
            bool d = false;
            foreach (var i in udat)
            {
                foreach (var b in GlobalBanList.Bans)
                {
                    if (b == user.Id)
                    {
                        d = true;
                    }
                }
                if (i.Id == user.Id && !d)
                {
                    i.Bans.Remove(guild.Id);
                    await File.WriteAllTextAsync("ucfg\\" + user.Id + ".json", JsonConvert.SerializeObject(udat[udat.IndexOf(i)]));
                }
            }
        }

        private async Task Client_UserJoined(SocketGuildUser user)
        {
            bool cont = true;
            foreach (var gg in gset)
            {
                if (user.Guild.Id == gg.Id)
                {
                    if (gg.GlobalBan == false)
                    {
                        cont = false;
                    }
                }
            }
            if (GlobalBanList.Bans.Contains(user.Id) && cont)
            {
                await user.BanAsync(0, "Globally Banned");
            }
        }

        private async Task Client_UserBanned(SocketUser user, SocketGuild guild)
        {
            foreach (var l in udat)
            {
                if (l.Id == user.Id)
                {
                    
                    int j = udat.IndexOf(l);
                    
                    if (udat[j].Bans.Contains(guild.Id))
                    {
                        return;
                    }
                    udat[j].Bans.Add(guild.Id);
                    var us = new UserData
                    {
                        Id = user.Id,
                        Bans = udat[j].Bans
                    };
                    await File.WriteAllTextAsync("ucfg\\" + user.Id + ".json" , JsonConvert.SerializeObject(us));
                    if (udat[j].Bans.ToArray().Length > 2)
                    {
                        GlobalBanList.Bans.Add(user.Id);
                        File.WriteAllText("GlobalBanList.json", JsonConvert.SerializeObject(GlobalBanList));
                        GlobalBanListUpdate(user);
                    }
                    return;
                }

            }


            var u = new UserData
            {
                Id = user.Id,
                Bans = new List<ulong> { guild.Id }
            };
            

            udat.Add(u);
            int i = udat.IndexOf(u);
            await File.WriteAllTextAsync("ucfg\\" + user.Id + ".json", JsonConvert.SerializeObject(udat[i]));
            
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
                if (l.Id == guild.Id)
                {
                    return;
                }
            }
            var g = new GuildConfig
            {
                Id = guild.Id,
                prefix = '&',
                AntiSwear = false,
                GlobalBan = true

            };

            gset.Add(g);
            try
            {

                await File.WriteAllTextAsync("gcfg\\" + guild.Id.ToString() + ".json", JsonConvert.SerializeObject(g));
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
            foreach (var ggg in gsetu)
            {
                if (ggg.User == user)
                {
                    switch (ggg.Stage)
                    {
                        case 1:
                            gset[ggg.GCfgIndex].prefix = message.Content[0];
                            int t = gsetu.IndexOf(ggg);
                            gsetu[t].Stage = 2;
                            await message.Channel.SendMessageAsync("Would you like to use anti swear (yes/no)");
                            break;
                        case 2:
                            if (message.Content == "yes")
                            {
                                gset[ggg.GCfgIndex].AntiSwear = true;
                            }
                            else if (message.Content == "no")
                            {
                                gset[ggg.GCfgIndex].AntiSwear = false;
                            }
                            else
                            {
                                await message.Channel.SendMessageAsync("Unrecognized Answer, please try again");
                                return;
                            }
                            int tt = gsetu.IndexOf(ggg);
                            gsetu[tt].Stage = 3;
                            await message.Channel.SendMessageAsync("Would you like to use Global Banning (yes/no)");
                            break;
                        case 3:
                            if (message.Content == "yes")
                            {
                                gset[ggg.GCfgIndex].GlobalBan = true;
                            }
                            else if (message.Content == "no")
                            {
                                gset[ggg.GCfgIndex].GlobalBan = false;
                            }
                            else
                            {
                                await message.Channel.SendMessageAsync("Unrecognized Answer, please try again");
                                return;
                            }
                            int ttt = gsetu.IndexOf(ggg);
                            //gsetu[ttt].Stage = 4;
                            await message.Channel.SendMessageAsync("Done");
                            await File.WriteAllTextAsync("gcfg\\" + gset[ggg.GCfgIndex].Id + ".json", JsonConvert.SerializeObject(gset[ggg.GCfgIndex]));
                            gsetu.Remove(ggg);
                            break;
                    }
                    return;
                        
                }
            }
            foreach (var r in user.Roles)
            {
                if (r.Permissions.Has(GuildPermission.Administrator) || user.Guild.OwnerId == user.Id)
                {
                    adperm = true;
                }
            }
            foreach (var l in gset)
            {
                if (l.Id == user.Guild.Id)
                {
                    cguildseti = gset.IndexOf(l);
                }
            }
            foreach (var l in gset)
            {
                if (l.Id == user.Guild.Id)
                {
                    prefix = l.prefix;
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
                        if (l.Id == user.Guild.Id)
                        {
                            l.prefix = wordArray[1].ToCharArray()[0];
                            var g = new GuildConfig
                            {
                                Id = l.Id,
                                prefix = wordArray[1].ToCharArray()[0],
                                AntiSwear = l.AntiSwear,
                                GlobalBan = l.GlobalBan
                            };
                            await File.WriteAllTextAsync("gcfg\\" + l.Id + ".json", JsonConvert.SerializeObject(g));
                        }

                    }
                }

                else if (wordArray[0] == prefix + "settings" && adperm)
                {
                    switch (wordArray[1])
                    {
                        case "Prefix":
                            foreach (var l in gset)
                            {
                                if (l.Id == user.Guild.Id)
                                {
                                    l.prefix = wordArray[2].ToCharArray()[0];
                                    var g = new GuildConfig
                                    {
                                        Id = l.Id,
                                        prefix = l.prefix,
                                        AntiSwear = l.AntiSwear,
                                        GlobalBan = l.GlobalBan
                                    };
                                    await File.WriteAllTextAsync("gcfg\\" + l.Id + ".json", JsonConvert.SerializeObject(g));
                                }
                            }
                            break;
                        case "AntiSwear":
                            foreach (var l in gset)
                            {
                                if (l.Id == user.Guild.Id)
                                {
                                    bool s = false;
                                    switch (wordArray[2])
                                    {
                                        case "true":
                                            s = true;
                                            break;

                                    }
                                    l.AntiSwear = s;
                                    var g = new GuildConfig
                                    {
                                        Id = l.Id,
                                        prefix = l.prefix,
                                        AntiSwear = l.AntiSwear,
                                        GlobalBan = l.GlobalBan
                                    };
                                    await File.WriteAllTextAsync("gcfg\\" + l.Id + ".json", JsonConvert.SerializeObject(g));
                                }
                            }
                            break;
                        case "GlobalBan":
                            foreach (var l in gset)
                            {
                                if (l.Id == user.Guild.Id)
                                {
                                    bool s = false;
                                    if (wordArray[2] == "true")
                                    {
                                        s = true;
                                    }
                                    l.GlobalBan = s;
                                    var g = new GuildConfig
                                    {
                                        Id = l.Id,
                                        prefix = l.prefix,
                                        AntiSwear = l.AntiSwear,
                                        GlobalBan = l.GlobalBan
                                    };
                                    await File.WriteAllTextAsync("gcfg\\" + l.Id + ".json", JsonConvert.SerializeObject(g));
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
                else if (wordArray[0] == prefix + "unban" && adperm)
                {
                    var g = user.Guild as SocketGuild;
                    IUser us = user;
                    var bans = await user.Guild.GetBansAsync();
                    foreach (var b in bans)
                    {
                        if (b.User.Id.ToString() == wordArray[1])
                        {
                            us = b.User;
                        }
                    }
                    try
                    { 
                        await g.RemoveBanAsync(us);
                    }
                    catch
                    {

                    }
                    foreach (var u in udat)
                    {
                        if (u.Id.ToString() == wordArray[1])
                        {
                            u.Bans.Remove(user.Guild.Id);
                        }
                    }

                }
                else if (wordArray[0] == prefix + "setup" && user.Guild.OwnerId == user.Id)
                {
                    await message.Channel.SendMessageAsync("What would you like the bot prefix to be (1 character)");
                    var s = new GuildSetup
                    {
                        User = user,
                        Guild = user.Guild,
                        GCfgIndex = cguildseti,
                        Stage = 1
                    };
                    gsetu.Add(s);
                }
                else if (wordArray[0] == prefix + "help")
                {
                    await message.Channel.SendMessageAsync("PLease see our Github page for more info: https://github.com/big-forking-team/big-forking-bot");
                }
                if (gset[cguildseti].AntiSwear && user != (SocketUser)client.CurrentUser)
                {
                    foreach (var s in swears)
                    {
                        var msgl = wordArray.ToList();
                        if (msgl.Contains(s.ToLower()))
                        {
                            swear = true;
                        }

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
    public class GuildConfig
    {
        public ulong Id { get; set; }
        public char prefix { get; set; }
        public bool AntiSwear { get; set; }
        public bool GlobalBan { get; set; }
    }
    public class UserData
    {
        public ulong Id { get; set; }
        public List<ulong> Bans { get; set; }
    }
    public class GlobalBanList
    {
        public List<ulong> Bans { get; set; }
    }
    public class GuildSetup
    {
        public SocketUser User { get; set; }
        public SocketGuild Guild { get; set; }
        public int GCfgIndex { get; set; }
        public int Stage { get; set; }
    }
}
