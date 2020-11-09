using Discord;
using Discord.Commands;
using Discord.WebSocket;
using IntroBot.Services;
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
        private readonly DiscordSocketClient _client;

        public IntroBot(string[] args)
        {
            var builder = new ConfigurationBuilder()
                .AddJsonFile($"appsettings.json", true, true)
                .AddUserSecrets<IntroBot>();

            Configuration = builder.Build();
        }

        public static async Task RunAsync(string[] args)
        {

            var startup = new IntroBot(args);
            await startup.RunAsync();
        }

        public async Task RunAsync()
        {
            // Create a new instance of a service collection
            var services = new ServiceCollection();
            ConfigureServices(services);

            var provider = services.BuildServiceProvider();
            //provider.GetRequiredService<LoggingHandler>();      // Start the logging service
            provider.GetRequiredService<CommandHandlingService>();      // Start the command handler service
            //provider.GetRequiredService<ReactionService>();     // Start the react to messages handler
            //provider.GetRequiredService<MessageService>();      // Start the message service handler
            //provider.GetRequiredService<UserJoinedService>();   // Start the userjoined service
            //provider.GetRequiredService<ToshiService>();       // Start the Toshi bot service
            //provider.GetRequiredService<DBService>().Setup();
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
