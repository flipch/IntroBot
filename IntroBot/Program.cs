using Discord;
using Discord.Commands;
using Discord.WebSocket;
using IntroBot.Services;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Threading;
using System.Threading.Tasks;
using Victoria;

namespace IntroBot
{
    class Program
    {
        public static async Task Main(string[] args)
            => await IntroBot.RunAsync(args);

        //private ServiceProvider _services;

        //public async Task MainAsync()
        //{
        //    // You specify the amount of shards you'd like to have with the
        //    // DiscordSocketConfig. Generally, it's recommended to
        //    // have 1 shard per 1500-2000 guilds your bot is in.
        //    var config = new DiscordSocketConfig
        //    {
        //        TotalShards = 2
        //    };

        //    // You should dispose a service provider created using ASP.NET
        //    // when you are finished using it, at the end of your app's lifetime.
        //    // If you use another dependency injection framework, you should inspect
        //    // its documentation for the best way to do this.
        //    _services = ConfigureServices(config);

        //    {
        //        var client = _services.GetRequiredService<DiscordShardedClient>();

        //        // The Sharded Client does not have a Ready event.
        //        // The ShardReady event is used instead, allowing for individual
        //        // control per shard.
        //        client.ShardReady += ReadyAsync;
        //        client.Log += LogAsync;

        //        await _services.GetRequiredService<CommandHandlingService>().InitializeAsync();

        //        // Tokens should be considered secret data, and never hard-coded.
        //        await client.LoginAsync(TokenType.Bot, "");
        //        await client.StartAsync();

        //        await Task.Delay(Timeout.Infinite);
        //    }
        //}

        //private ServiceProvider ConfigureServices(DiscordSocketConfig config)
        //{
        //    return new ServiceCollection()
        //        .AddLogging()
        //        .AddSingleton(new DiscordShardedClient(config))
        //        .AddLavaNode(x =>
        //        {
        //            x.SelfDeaf = false;
        //            x.Authorization = "youshallnotpass";
        //            x.Hostname = "localhost";
        //            x.Port = 2333;
        //        })
        //        .AddSingleton<CommandService>()
        //        .AddSingleton<AudioService>()
        //        .AddSingleton<CommandHandlingService>()
        //        .BuildServiceProvider();
        //}


        //private Task ReadyAsync(DiscordSocketClient shard)
        //{
        //    // Avoid calling ConnectAsync again if it's already connected 
        //    // (It throws InvalidOperationException if it's already connected).
        //    var lavaNode = (LavaNode)_services.GetService(typeof(LavaNode));
        //    if (!lavaNode.IsConnected)
        //    {
        //        lavaNode.ConnectAsync();
        //    }

        //    Console.WriteLine($"Shard Number {shard.ShardId} is connected and ready!");
        //    return Task.CompletedTask;
        //}

        //private Task LogAsync(LogMessage log)
        //{
        //    Console.WriteLine(log.ToString());
        //    return Task.CompletedTask;
        //}
    }
}
