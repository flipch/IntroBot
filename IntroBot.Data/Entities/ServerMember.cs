using System;

namespace IntroBot.Data.Entities
{
    public class ServerMember
    {
        public int ServerMemberId { get; set; }
        public string DiscordId { get; set; }
        public virtual Song? IntroSong { get; set; }
        public TimeSpan? IntroSongSeek { get; set; }
    }
}
