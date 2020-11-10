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
        public static Task Main(string[] args)
            => IntroBot.RunAsync(args);
    }
}
