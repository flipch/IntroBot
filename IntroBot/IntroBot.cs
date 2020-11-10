using Discord;
using Discord.Commands;
using Discord.WebSocket;
using IntroBot.Data.Contexts;
using IntroBot.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Victoria;

namespace IntroBot
{
    public class IntroBot
    {
        public IConfigurationRoot Configuration { get; }

        public IntroBot(string[] args)
        {
            var builder = new ConfigurationBuilder()
                .AddJsonFile($"appsettings.json", true, true)
                .AddUserSecrets<IntroBot>();

            Configuration = builder.Build();
        }

        public static Task RunAsync(string[] args)
        {

            var startup = new IntroBot(args);
            return startup.RunAsync();
        }

        public async Task RunAsync()
        {
            // Create a new instance of a service collection
            var services = new ServiceCollection();
            ConfigureServices(services);

            var provider = services.BuildServiceProvider();
            provider.GetRequiredService<CommandHandlingService>();      // Start the command handler service
            provider.GetRequiredService<IntroContext>();
            await provider.GetRequiredService<StartupService>().StartAsync();       // Start the startup service
            await Task.Delay(Timeout.Infinite);                               // Keep the program alive
        }

        private void ConfigureServices(IServiceCollection services)
        {
            var config = new DiscordSocketConfig();
            Configuration.Bind("DiscordSocketConfig", config);

            services
                .AddSingleton(Configuration)
                .AddLogging()
                .AddDbContext<IntroContext>(opt => opt.UseLazyLoadingProxies().UseSqlServer(Configuration.GetConnectionString("SQL")))
                .AddSingleton(new DiscordShardedClient(config))
                .AddLavaNode(x =>
                {
                    x.SelfDeaf = false;
                    x.Authorization = "youshallnotpass";
                    x.Hostname = "localhost";
                    x.Port = 2333;
                })
                .AddSingleton<StartupService>()
                .AddSingleton<CommandService>()
                .AddSingleton<AudioService>()
                .AddSingleton<CommandHandlingService>();
        }
    }
}
