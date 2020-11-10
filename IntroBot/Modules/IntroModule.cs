using Discord;
using Discord.Commands;
using Discord.WebSocket;
using IntroBot.Data.Contexts;
using IntroBot.Data.Entities;
using IntroBot.Services;
using ObjectsComparer;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Victoria;
using Victoria.Enums;

namespace IntroBot.Modules
{
    public sealed class IntroModule : ModuleBase<ShardedCommandContext>
    {

        private readonly LavaNode _lavaNode;
        private readonly AudioService _audioService;
        private readonly IntroContext _db;
        private readonly DiscordShardedClient _discord;
        private static readonly IEnumerable<int> Range = Enumerable.Range(1900, 2000);

        private ulong _lastRequestBy = 0;
        private CancellationTokenSource _cancelIntro;

        public IntroModule(LavaNode lavaNode, AudioService audioService, IntroContext ctx, DiscordShardedClient discord) : base()
        {
            _db = ctx;
            _lavaNode = lavaNode;
            _audioService = audioService;
            _discord = discord;
            _discord.UserVoiceStateUpdated += UserVoiceStateUpdated;
        }

        private async Task UserVoiceStateUpdated(SocketUser socketUser, SocketVoiceState oldSocketVoiceState, SocketVoiceState newSocketVoiceState)
        {
            if (socketUser.IsBot) return;

            var member = await _db.ServerMembers.FirstOrDefaultAsync(x => x.DiscordId == socketUser.Id.ToString());
            if (member?.IntroSong == null || socketUser.Id == _lastRequestBy) return;

            if (oldSocketVoiceState.VoiceChannel == null)
            {
                Console.WriteLine($"User {socketUser.Username} entered the server!");

                if (_lavaNode.HasPlayer(newSocketVoiceState.VoiceChannel.Guild))
                {
                    await _lavaNode.MoveChannelAsync(newSocketVoiceState.VoiceChannel);
                }
                else
                {
                    await _audioService.JoinChannel(socketUser as IVoiceState, newSocketVoiceState.VoiceChannel.Guild);
                }

                var player = _lavaNode.GetPlayer(newSocketVoiceState.VoiceChannel.Guild);
                await player.UpdateVolumeAsync(7);
                var searchResponse = await _lavaNode.SearchAsync(member.IntroSong.Url);
                var track = searchResponse.Tracks[0];
                var timespan = member?.IntroSongSeek ?? TimeSpan.Zero;
                await player.PlayAsync(track);

                if (member?.IntroSongSeek.HasValue ?? false)
                {
                    await player.SeekAsync(member.IntroSongSeek);
                }

                _lastRequestBy = socketUser.Id;

                if (_cancelIntro != null && !_cancelIntro.IsCancellationRequested)
                {
                    _cancelIntro.Cancel();
                }

                _cancelIntro = new CancellationTokenSource();
                var cancellationToken = _cancelIntro.Token;

                Task.Run(async () =>
                {
                    await Task.Delay(TimeSpan.FromSeconds(30), cancellationToken);

                    if (cancellationToken.IsCancellationRequested)
                    {
                        return;
                    }

                    if (player.PlayerState != PlayerState.Disconnected && socketUser.Id == _lastRequestBy)
                        await _lavaNode.LeaveAsync(newSocketVoiceState.VoiceChannel);
                }, cancellationToken);

            }

            return;
        }

        [Command("Status")]
        public async Task DBAsync()
        {
            // Check if first time we have this user.
            var member = await _db.ServerMembers.FirstOrDefaultAsync(x => x.DiscordId == Context.User.Id.ToString());
            if (member == null)
            {
                var entity = await _db.ServerMembers.AddAsync(new ServerMember
                {
                    DiscordId = Context.User.Id.ToString()
                });

                await _db.SaveChangesAsync();
                member = entity.Entity;
                await ReplyAsync($@"New user!. Created Discord id {member.DiscordId} and DB id {member.ServerMemberId}");
            }

            var song = member.IntroSong;

            await ReplyAsync($"User:\nID = {member.ServerMemberId}\nDiscord Id = {member.DiscordId}\nSong = {song?.Url}\nSeek Time = {member?.IntroSongSeek}");
        }

        [Command("Set-intro")]
        public async Task DBAsync([Remainder] string input)
        {
            // Check if first time we have this user.
            var member = await _db.ServerMembers.FirstOrDefaultAsync(x => x.DiscordId == Context.User.Id.ToString());
            if (member == null)
            {
                var entity = await _db.ServerMembers.AddAsync(new ServerMember
                {
                    DiscordId = Context.User.Id.ToString()
                });

                await _db.SaveChangesAsync();
                member = entity.Entity;
                await ReplyAsync($@"New user!. Created Discord id {member.DiscordId} and DB id {member.ServerMemberId}");
            }

            // Get song.
            var request = new List<string>(input.Split(' '));
            var searchQuery = request.FirstOrDefault();
            if (string.IsNullOrWhiteSpace(searchQuery))
            {
                await ReplyAsync("Please provide a intro.");
                return;
            }

            var searchResponse = await _lavaNode.SearchAsync(searchQuery);
            if (searchResponse.LoadStatus == LoadStatus.LoadFailed ||
                searchResponse.LoadStatus == LoadStatus.NoMatches)
            {
                await ReplyAsync($"I wasn't able to find anything for `{searchQuery}`.");
                return;
            }

            await ReplyAsync($"Found {searchResponse.Tracks[0].Title}");

            TimeSpan timeSpan;
            var timeSpanSerialized = request.LastOrDefault();
            if (!string.IsNullOrWhiteSpace(timeSpanSerialized))
            {
                await ReplyAsync($"Got timespan {timeSpanSerialized}.");
                TimeSpan.TryParseExact(timeSpanSerialized, @"m\:s", CultureInfo.InvariantCulture, TimeSpanStyles.None, out timeSpan);

                if (timeSpan != null)
                {
                    member.IntroSongSeek = timeSpan;
                }
            }

            // Check if first time we have this song.
            var song = await _db.Songs.FirstOrDefaultAsync(x => x.Url == searchResponse.Tracks[0].Url);
            if (song == null)
            {
                var entity = await _db.Songs.AddAsync(new Song
                {
                    Url = searchResponse.Tracks[0].Url,
                    IntroOwners = new List<ServerMember> { member },
                });

                await _db.SaveChangesAsync();
                song = entity.Entity;
                await ReplyAsync($@"New Song!. Created Song ID {song.SongId} and URL {song.Url}");
            }
            else
            {
                song.IntroOwners.Add(member);
                await _db.SaveChangesAsync();
                await ReplyAsync($@"Added you to song owners!");
            }
        }

        [Command("Join")]
        public async Task JoinAsync()
        {
            var result = await _audioService.JoinChannel(Context.User as IVoiceState, Context.Guild, Context.Channel as ITextChannel);
            await ReplyAsync(result.Message);
        }

        [Command("Leave")]
        public async Task LeaveAsync()
        {
            if (!_lavaNode.TryGetPlayer(Context.Guild, out var player))
            {
                await ReplyAsync("I'm not connected to any voice channels!");
                return;
            }

            var voiceChannel = (Context.User as IVoiceState).VoiceChannel ?? player.VoiceChannel;
            if (voiceChannel == null)
            {
                await ReplyAsync("Not sure which voice channel to disconnect from.");
                return;
            }

            try
            {
                await _lavaNode.LeaveAsync(voiceChannel);
                await ReplyAsync($"I've left {voiceChannel.Name}!");
            }
            catch (Exception exception)
            {
                await ReplyAsync(exception.Message);
            }
        }

        [Command("Play")]
        public async Task PlayAsync([Remainder] string searchQuery)
        {
            if (string.IsNullOrWhiteSpace(searchQuery))
            {
                await ReplyAsync("Please provide search terms.");
                return;
            }

            if (!_lavaNode.HasPlayer(Context.Guild))
            {
                await ReplyAsync("I'm not connected to a voice channel.");
                return;
            }

            var query = searchQuery;
            var searchResponse = await _lavaNode.SearchAsync(query);
            if (searchResponse.LoadStatus == LoadStatus.LoadFailed ||
                searchResponse.LoadStatus == LoadStatus.NoMatches)
            {
                await ReplyAsync($"I wasn't able to find anything for `{query}`.");
                return;
            }

            var player = _lavaNode.GetPlayer(Context.Guild);

            if (player.PlayerState == PlayerState.Playing || player.PlayerState == PlayerState.Paused)
            {
                if (!string.IsNullOrWhiteSpace(searchResponse.Playlist.Name))
                {
                    foreach (var track in searchResponse.Tracks)
                    {
                        player.Queue.Enqueue(track);
                    }

                    await ReplyAsync($"Enqueued {searchResponse.Tracks.Count} tracks.");
                }
                else
                {
                    var track = searchResponse.Tracks[0];
                    player.Queue.Enqueue(track);
                    await ReplyAsync($"Enqueued: {track.Title}");
                }
            }
            else
            {
                var track = searchResponse.Tracks[0];

                if (!string.IsNullOrWhiteSpace(searchResponse.Playlist.Name))
                {
                    for (var i = 0; i < searchResponse.Tracks.Count; i++)
                    {
                        if (i == 0)
                        {
                            await player.PlayAsync(track);
                            await ReplyAsync($"Now Playing: {track.Title}");
                        }
                        else
                        {
                            player.Queue.Enqueue(searchResponse.Tracks[i]);
                        }
                    }

                    await ReplyAsync($"Enqueued {searchResponse.Tracks.Count} tracks.");
                }
                else
                {
                    await player.PlayAsync(track);
                    await ReplyAsync($"Now Playing: {track.Title}");
                }
            }
        }

        [Command("Pause")]
        public async Task PauseAsync()
        {
            if (!_lavaNode.TryGetPlayer(Context.Guild, out var player))
            {
                await ReplyAsync("I'm not connected to a voice channel.");
                return;
            }

            if (player.PlayerState != PlayerState.Playing)
            {
                await ReplyAsync("I cannot pause when I'm not playing anything!");
                return;
            }

            try
            {
                await player.PauseAsync();
                await ReplyAsync($"Paused: {player.Track.Title}");
            }
            catch (Exception exception)
            {
                await ReplyAsync(exception.Message);
            }
        }

        [Command("Resume")]
        public async Task ResumeAsync()
        {
            if (!_lavaNode.TryGetPlayer(Context.Guild, out var player))
            {
                await ReplyAsync("I'm not connected to a voice channel.");
                return;
            }

            if (player.PlayerState != PlayerState.Paused)
            {
                await ReplyAsync("I cannot resume when I'm not playing anything!");
                return;
            }

            try
            {
                await player.ResumeAsync();
                await ReplyAsync($"Resumed: {player.Track.Title}");
            }
            catch (Exception exception)
            {
                await ReplyAsync(exception.Message);
            }
        }

        [Command("Stop")]
        public async Task StopAsync()
        {
            if (!_lavaNode.TryGetPlayer(Context.Guild, out var player))
            {
                await ReplyAsync("I'm not connected to a voice channel.");
                return;
            }

            if (player.PlayerState == PlayerState.Stopped)
            {
                await ReplyAsync("Woaaah there, I can't stop the stopped forced.");
                return;
            }

            try
            {
                await player.StopAsync();
                await ReplyAsync("No longer playing anything.");
            }
            catch (Exception exception)
            {
                await ReplyAsync(exception.Message);
            }
        }

        [Command("Skip")]
        public async Task SkipAsync()
        {
            if (!_lavaNode.TryGetPlayer(Context.Guild, out var player))
            {
                await ReplyAsync("I'm not connected to a voice channel.");
                return;
            }

            if (player.PlayerState != PlayerState.Playing)
            {
                await ReplyAsync("Woaaah there, I can't skip when nothing is playing.");
                return;
            }

            var voiceChannelUsers = (player.VoiceChannel as SocketVoiceChannel).Users.Where(x => !x.IsBot).ToArray();
            if (_audioService.VoteQueue.Contains(Context.User.Id))
            {
                await ReplyAsync("You can't vote again.");
                return;
            }

            _audioService.VoteQueue.Add(Context.User.Id);
            var percentage = _audioService.VoteQueue.Count / voiceChannelUsers.Length * 100;
            if (percentage < 85)
            {
                await ReplyAsync("You need more than 85% votes to skip this song.");
                return;
            }

            try
            {
                var oldTrack = player.Track;
                var currenTrack = await player.SkipAsync();
                await ReplyAsync($"Skipped: {oldTrack.Title}\nNow Playing: {currenTrack.Title}");
            }
            catch (Exception exception)
            {
                await ReplyAsync(exception.Message);
            }
        }

        [Command("Seek")]
        public async Task SeekAsync(TimeSpan timeSpan)
        {
            if (!_lavaNode.TryGetPlayer(Context.Guild, out var player))
            {
                await ReplyAsync("I'm not connected to a voice channel.");
                return;
            }

            if (player.PlayerState != PlayerState.Playing)
            {
                await ReplyAsync("Woaaah there, I can't seek when nothing is playing.");
                return;
            }

            try
            {
                await player.SeekAsync(timeSpan);
                await ReplyAsync($"I've seeked `{player.Track.Title}` to {timeSpan}.");
            }
            catch (Exception exception)
            {
                await ReplyAsync(exception.Message);
            }
        }

        [Command("Volume")]
        public async Task VolumeAsync(ushort volume)
        {
            if (!_lavaNode.TryGetPlayer(Context.Guild, out var player))
            {
                await ReplyAsync("I'm not connected to a voice channel.");
                return;
            }

            try
            {
                await player.UpdateVolumeAsync(volume);
                await ReplyAsync($"I've changed the player volume to {volume}.");
            }
            catch (Exception exception)
            {
                await ReplyAsync(exception.Message);
            }
        }

        [Command("NowPlaying"), Alias("Np")]
        public async Task NowPlayingAsync()
        {
            if (!_lavaNode.TryGetPlayer(Context.Guild, out var player))
            {
                await ReplyAsync("I'm not connected to a voice channel.");
                return;
            }

            if (player.PlayerState != PlayerState.Playing)
            {
                await ReplyAsync("Woaaah there, I'm not playing any tracks.");
                return;
            }

            var track = player.Track;
            var artwork = await track.FetchArtworkAsync();

            var embed = new EmbedBuilder
            {
                Title = $"{track.Author} - {track.Title}",
                ThumbnailUrl = artwork,
                Url = track.Url
            }
                .AddField("Id", track.Id)
                .AddField("Duration", track.Duration)
                .AddField("Position", track.Position);

            await ReplyAsync(embed: embed.Build());
        }

        [Command("Genius", RunMode = RunMode.Async)]
        public async Task ShowGeniusLyrics()
        {
            if (!_lavaNode.TryGetPlayer(Context.Guild, out var player))
            {
                await ReplyAsync("I'm not connected to a voice channel.");
                return;
            }

            if (player.PlayerState != PlayerState.Playing)
            {
                await ReplyAsync("Woaaah there, I'm not playing any tracks.");
                return;
            }

            var lyrics = await player.Track.FetchLyricsFromGeniusAsync();
            if (string.IsNullOrWhiteSpace(lyrics))
            {
                await ReplyAsync($"No lyrics found for {player.Track.Title}");
                return;
            }

            var splitLyrics = lyrics.Split('\n');
            var stringBuilder = new StringBuilder();
            foreach (var line in splitLyrics)
            {
                if (Range.Contains(stringBuilder.Length))
                {
                    await ReplyAsync($"```{stringBuilder}```");
                    stringBuilder.Clear();
                }
                else
                {
                    stringBuilder.AppendLine(line);
                }
            }

            await ReplyAsync($"```{stringBuilder}```");
        }

        [Command("OVH", RunMode = RunMode.Async)]
        public async Task ShowOVHLyrics()
        {
            if (!_lavaNode.TryGetPlayer(Context.Guild, out var player))
            {
                await ReplyAsync("I'm not connected to a voice channel.");
                return;
            }

            if (player.PlayerState != PlayerState.Playing)
            {
                await ReplyAsync("Woaaah there, I'm not playing any tracks.");
                return;
            }

            var lyrics = await player.Track.FetchLyricsFromOVHAsync();
            if (string.IsNullOrWhiteSpace(lyrics))
            {
                await ReplyAsync($"No lyrics found for {player.Track.Title}");
                return;
            }

            var splitLyrics = lyrics.Split('\n');
            var stringBuilder = new StringBuilder();
            foreach (var line in splitLyrics)
            {
                if (Range.Contains(stringBuilder.Length))
                {
                    await ReplyAsync($"```{stringBuilder}```");
                    stringBuilder.Clear();
                }
                else
                {
                    stringBuilder.AppendLine(line);
                }
            }

            await ReplyAsync($"```{stringBuilder}```");
        }
    }
}
