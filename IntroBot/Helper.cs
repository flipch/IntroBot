using Discord;
using Discord.WebSocket;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Concurrent;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

namespace IntroBot
{
    public static class Helper
    {


        public static LogLevel FromSeverityToLevel(LogSeverity severity)
        {
            return severity switch
            {
                LogSeverity.Critical => LogLevel.Critical,
                LogSeverity.Debug => LogLevel.Debug,
                LogSeverity.Error => LogLevel.Error,
                LogSeverity.Info => LogLevel.Information,
                LogSeverity.Verbose => LogLevel.Trace,
                LogSeverity.Warning => LogLevel.Warning,
                _ => throw new NotImplementedException()
            };
        }

        public class Utilities
        {
            private readonly DiscordSocketClient _client;
            private readonly Random _rnd;

            public Utilities(DiscordSocketClient client, Random rnd)
            {
                _client = client;
                _rnd = rnd;
            }

            public ConcurrentDictionary<ulong, bool> EmbedCommandsRunning = new ConcurrentDictionary<ulong, bool>();

            // Generic Embed template
            public static Embed Embed(string t, string d, Discord.Color c, string f, string thURL) => new EmbedBuilder()
                .WithTitle(t)
                .WithDescription(d)
                .WithColor(c)
                .WithFooter(f)
                .WithThumbnailUrl(thURL)
                .Build();

            // Convert a hexidecimal to an RGB value (input does not include the '#')
            public Discord.Color HexToRGB(string hex)
            {
                // First two values of the hex
                int r = int.Parse(hex.Substring(0, hex.Length - 4), System.Globalization.NumberStyles.AllowHexSpecifier);

                // Get the middle two values of the hex
                int g = int.Parse(hex.Substring(2, 2), System.Globalization.NumberStyles.AllowHexSpecifier);

                // Final two values
                int b = int.Parse(hex.Substring(4), System.Globalization.NumberStyles.AllowHexSpecifier);

                return new Discord.Color(r, g, b);
            }

            public Discord.Color RandomColor()
            {
                // First two values of the hex
                int r = _rnd.Next(0, 255);

                // Get the middle two values of the hex
                int g = _rnd.Next(0, 255);

                // Final two values
                int b = _rnd.Next(0, 255);

                return new Discord.Color(r, g, b);
            }

            public IEmote GetEmoteFromName(string name)
            {
                ulong? id = null;
#if DEBUG
                id = 627627675137343525;
#else
            id = 593518538325491729;
#endif
                SocketGuild guild = _client.GetGuild(id.Value);
                return guild.Emotes.First(e => e.Name == name);
            }
            public IEmote[] GetEmoteFromStringArray(string[] array)
            {
                ulong? id = null;
#if DEBUG
                id = 627627675137343525;
#else
            id = 593518538325491729;
#endif
                SocketGuild guild = _client.GetGuild(id.Value);
                IEmote[] emotes = new IEmote[array.Length];
                for (int i = 0; i < array.Length; i++)
                {
                    emotes[i] = guild.Emotes.First(e => e.Name == array[i]);
                }
                return emotes;
            }

            public Emoji GetEmojiFromUnicode(string unicode)
            {
                return new Emoji(unicode);
            }

            public Emoji[] GetEmojiFromArray(string[] unicodes)
            {
                Emoji[] emojis = new Emoji[unicodes.Length];
                for (int i = 0; i < unicodes.Length; i++)
                {
                    emojis[i] = GetEmojiFromUnicode(unicodes[i]);
                }
                return emojis;
            }

            public GuildPermissions MutePermsRole
            {
                get
                {
                    var perms = new GuildPermissions(true, false, false, false, false, false, false, false, true, false, false, false, false, false, true);
                    return perms;
                }
            }

            public OverwritePermissions MutePermsChannel
            {
                get
                {
                    var allow = PermValue.Allow;
                    var inherit = PermValue.Inherit;
                    var deny = PermValue.Deny;
                    var perms = new OverwritePermissions(allow, deny, deny, inherit, deny, deny, deny, deny, deny, inherit, deny, deny, inherit, deny, deny, deny, deny, deny, deny, deny);
                    return perms;
                }
            }
        }
    }
}
