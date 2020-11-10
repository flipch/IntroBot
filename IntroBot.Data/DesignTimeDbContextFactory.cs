using IntroBot.Data.Contexts;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace IntroBot.Data
{
    public class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<IntroContext>
    {
        public IntroContext CreateDbContext(string[] args)
        {
            var configuration = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json")
                .Build();

            var connectionString = configuration.GetConnectionString("Sql");
            var builder = new DbContextOptionsBuilder<IntroContext>().UseSqlServer(connectionString);
            return new IntroContext(builder.Options);
        }
    }
}
