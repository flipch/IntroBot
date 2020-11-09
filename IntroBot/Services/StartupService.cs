using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Victoria;

namespace IntroBot.Services
{
    public class StartupService
    {
        private readonly IServiceProvider _provider;
        private readonly DiscordShardedClient _discord;
        private readonly CommandService _commands;
        private readonly IConfigurationRoot _config;

        // DiscordSocketClient, CommandService, and IConfigurationRoot are injected automatically from the IServiceProvider
        public StartupService(
            IServiceProvider provider,
            DiscordShardedClient discord,
            CommandService commands,
            IConfigurationRoot config)
        {
            _provider = provider;
            _config = config;
            _discord = discord;
            _commands = commands;
        }

        public async Task StartAsync()
        {

            string token = _config["Token"];

            // The Sharded Client does not have a Ready event.
            // The ShardReady event is used instead, allowing for individual
            // control per shard.
            _discord.ShardReady += ReadyAsync;
            _discord.Log += LogAsync;

            await _discord.LoginAsync(TokenType.Bot, token);     // Login to discord
            Console.WriteLine("Logging Into Discord");
            await _discord.StartAsync();                         // Connect to the websocket
            LoadTypeReaders(Assembly.GetEntryAssembly());
            await _commands.AddModulesAsync(Assembly.GetEntryAssembly(), _provider);     // Load commands and modules into the command service
        }

        private Task ReadyAsync(DiscordSocketClient shard)
        {
            // Avoid calling ConnectAsync again if it's already connected 
            // (It throws InvalidOperationException if it's already connected).
            var lavaNode = (LavaNode)_provider.GetService(typeof(LavaNode));
            if (!lavaNode.IsConnected)
            {
                lavaNode.ConnectAsync();
            }
        
            Console.WriteLine($"Shard Number {shard.ShardId} is connected and ready!");
            return Task.CompletedTask;
        }
        
        private Task LogAsync(LogMessage log)
        {
            Console.WriteLine(log.ToString());
            return Task.CompletedTask;
        }

        private IEnumerable<object> LoadTypeReaders(Assembly assembly)
        {
            Type[] allTypes;

            try
            {
                allTypes = assembly.GetTypes();
            }
            catch (ReflectionTypeLoadException ex)
            {
                return Enumerable.Empty<object>();
            }

            var filteredTypes = allTypes
                .Where(x => x.IsSubclassOf(typeof(TypeReader))
                    && x.BaseType.GetGenericArguments().Length > 0
                    && !x.IsAbstract);

            var toReturn = new List<object>();

            foreach (var ft in filteredTypes)
            {
                var x = (TypeReader)Activator.CreateInstance(ft, _discord, _commands);
                var baseType = ft.BaseType;
                var typeArgs = baseType.GetGenericArguments();

                try
                {
                    _commands.AddTypeReader(typeArgs[0], x);
                }
                catch (Exception ex)
                {
                    throw;
                }

                toReturn.Add(x);
            }

            return toReturn;
        }
    }
}
