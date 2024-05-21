using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Args;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;

namespace TeacherReviewBot
{
    public class BotService
    {
        private readonly TelegramBotClient _botClient;
        private readonly DatabaseService _databaseService;

        private static ReceiverOptions _receiverOptions;
        public BotService(TelegramBotClient botClient)
        {
            _botClient = botClient;
            _databaseService = new DatabaseService();
        }

        public async Task Start()
        {
            using CancellationTokenSource cts = new ();
            ReceiverOptions _receiverOptions = new ()
            {
                AllowedUpdates = Array.Empty<UpdateType>() // receive all update types except ChatMember related updates
            };
            
            
            _botClient.StartReceiving(HandleUpdateAsync, HandleErrorAsync, _receiverOptions, cts.Token);
            var me = await _botClient.GetMeAsync();
            Console.WriteLine($"Start listening for @{me.Username}");

        }

        private async Task HandleUpdateAsync(ITelegramBotClient botClient, Update update, System.Threading.CancellationToken cancellationToken)
        {
            if (update.Type == UpdateType.Message && update.Message.Text != null)
            {
                var messageText = update.Message.Text.ToLower();
                var chatId = update.Message.Chat.Id;

                if (messageText == "/start")
                {
                    await _botClient.SendTextMessageAsync(chatId, "Добро пожаловать! Вы можете:\n1. Получить список преподавателей\n2. Оставить отзыв");
                }
                else if (messageText == "получить список преподавателей")
                {
                    await SendTeachersListAsync(chatId);
                }
                else if (messageText.StartsWith("преподаватель"))
                {
                    var teacherName = messageText.Substring(13).Trim();
                    var teacher = _databaseService.GetTeacherByName(teacherName);
                    if (teacher != null)
                    {
                        var reviews = _databaseService.GetReviewsByTeacher(teacher.Id);
                        var reply = $"{teacher.Name}\nОтзывы:\n" + string.Join("\n", reviews.Select(r => r.Text));
                        await _botClient.SendTextMessageAsync(chatId, reply);
                    }
                    else
                    {
                        await _botClient.SendTextMessageAsync(chatId, "Преподаватель не найден");
                    }
                }
                else if (messageText.StartsWith("оставить отзыв"))
                {
                    var parts = messageText.Split(new[] { ' ' }, 5);
                    if (parts.Length == 5)
                    {
                        var teacherName = parts[2] + ' ' + parts[3];
                        Console.WriteLine(teacherName);
                        var reviewText = parts[4];
                        var teacher = _databaseService.GetTeacherByName(teacherName);
                        if (teacher != null)
                        {
                            _databaseService.AddReview(teacher.Id, reviewText);
                            await _botClient.SendTextMessageAsync(chatId, "Отзыв добавлен");
                        }
                        else
                        {
                            await _botClient.SendTextMessageAsync(chatId, "Преподаватель не найден");
                        }
                    }
                    else
                    {
                        await _botClient.SendTextMessageAsync(chatId, "Неправильный формат команды. Используйте: оставить отзыв [Имя преподавателя] [Текст отзыва]");
                    }
                }
                else
                {
                    await _botClient.SendTextMessageAsync(chatId, "Неизвестная команда");
                }
            }
            else if (update.Type == UpdateType.CallbackQuery)
            {
                await HandleCallbackQueryAsync(update.CallbackQuery);
            }
        }
        private async Task SendTeachersListAsync(long chatId)
        {
            var teachers = _databaseService.GetTeachers();
            var inlineKeyboard = new InlineKeyboardMarkup(teachers.Select(t =>
                new[] { InlineKeyboardButton.WithCallbackData(t.Name, t.Name) }));

            await _botClient.SendTextMessageAsync(chatId, "Выберите преподавателя:", replyMarkup: inlineKeyboard);
        }
        private async Task HandleCallbackQueryAsync(CallbackQuery callbackQuery)
        {
            var chatId = callbackQuery.Message.Chat.Id;
            var teacherName = callbackQuery.Data;
            var teacher = _databaseService.GetTeacherByName(teacherName);
            var reviews = _databaseService.GetReviewsByTeacher(teacher.Id);
            var reply = $"{teacher.Name}\nОтзывы:\n" + string.Join("\n", reviews.Select(r => r.Text));
            await _botClient.SendTextMessageAsync(chatId, reply);
            

            await _botClient.AnswerCallbackQueryAsync(callbackQuery.Id);
        }
        
        private Task HandleErrorAsync(ITelegramBotClient botClient, Exception exception, System.Threading.CancellationToken cancellationToken)
        {
            Console.WriteLine($"HandleErrorAsync:\n{exception}");
            return Task.CompletedTask;
        }
    }
}