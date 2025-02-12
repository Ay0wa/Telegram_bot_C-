﻿
using System;
using System.Threading.Tasks;
using Telegram.Bot;

namespace TeacherReviewBot
{
    class Program
    {
        private static readonly string BotToken = Environment.GetEnvironmentVariable("TELEGRAM_BOT_TOKEN");
        private static TelegramBotClient BotClient;

        static async Task Main(string[] args)
        {
            BotClient = new TelegramBotClient(BotToken);
            var botService = new BotService(BotClient);
            await botService.Start();
            Console.WriteLine("Bot is running...");
            Console.ReadLine();
        }
    }
}
