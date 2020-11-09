namespace IntroBot.Data.Entities
{
    public class ServerMember
    {
        public int ServerMemberId { get; set; }
        public string DiscordId { get; set; }
        public Song? IntroSong { get; set; }
    }
}
