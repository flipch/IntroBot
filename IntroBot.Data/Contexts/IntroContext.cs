using IntroBot.Data.Entities;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Text;

namespace IntroBot.Data.Contexts
{
    public class IntroContext : DbContext
    {
        public DbSet<Song> Songs { get; set; }

        public DbSet<ServerMember> ServerMembers { get; set; }

        public IntroContext(DbContextOptions options)
            : base(options) { }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Song>()
                .HasMany(s => s.IntroOwners)
                .WithOne(m => m.IntroSong)
                .OnDelete(DeleteBehavior.SetNull);
            
            modelBuilder.Entity<Song>()
                .HasIndex(s => s.Url)
                .IsUnique();
        }
    }
}
