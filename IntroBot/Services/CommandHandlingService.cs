﻿using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace IntroBot.Services
{
    public class CommandHandlingService
    {
        private readonly CommandService _commands;
        private readonly DiscordShardedClient _discord;
        private readonly IServiceProvider _services;
        private readonly char _commandPrefix = '!';

        public CommandHandlingService(IServiceProvider services)
        {
            _commands = services.GetRequiredService<CommandService>();
            _discord = services.GetRequiredService<DiscordShardedClient>();
            _services = services;

            _commands.CommandExecuted += CommandExecutedAsync;
            _commands.Log += LogAsync;
            _discord.MessageReceived += MessageReceivedAsync;
        }

        public async Task InitializeAsync()
        {
            await _commands.AddModulesAsync(Assembly.GetEntryAssembly(), _services);
        }

        public async Task MessageReceivedAsync(SocketMessage rawMessage)
        {
            // Ignore system messages, or messages from other bots
            if (!(rawMessage is SocketUserMessage message))
                return;

            if (message.Source != MessageSource.User)
                return;

            // This value holds the offset where the prefix ends
            var argPos = 0;
            if (message.HasCharPrefix(_commandPrefix, ref argPos))
            {
                await ExecuteAsync(message, argPos);
            }
            else if (message.HasMentionPrefix(_discord.CurrentUser, ref argPos))
            {
                await ExecuteAsync(message, argPos);
            }

            return;
        }

        private Task ExecuteAsync(SocketUserMessage message, int argPos)
        {
            // A new kind of command context, ShardedCommandContext can be utilized with the commands framework
            var context = new ShardedCommandContext(_discord, message);
            return _commands.ExecuteAsync(context, argPos, _services);
        }

        public async Task CommandExecutedAsync(Optional<CommandInfo> command, ICommandContext context, IResult result)
        {
            // command is unspecified when there was a search failure (command not found); we don't care about these errors
            if (!command.IsSpecified)
                return;

            // the command was succesful, we don't care about this result, unless we want to log that a command succeeded.
            if (result.IsSuccess)
                return;

            // the command failed, let's notify the user that something happened.
            await context.Channel.SendMessageAsync($"error: {result}");
        }

        private Task LogAsync(LogMessage log)
        {
            Console.WriteLine(log.ToString());

            return Task.CompletedTask;
        }
    }
}
