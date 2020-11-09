using System.Collections.Generic;

namespace IntroBot.Data.Entities
{
    public class Song
    {
        public int SongId { get; set; }
        public string Url { get; set; }
        public ICollection<ServerMember> IntroOwners { get; set; }
    }
}
