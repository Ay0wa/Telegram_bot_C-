using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.VisualBasic;
using System.Collections.Concurrent;
using Telegram.Bot;
using Telegram.Bot.Args;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.Passport;
using Telegram.Bot.Types.ReplyMarkups;

namespace TeacherReviewBot
{
    public class BotService
    {
        private readonly TelegramBotClient _botClient;
        private readonly DatabaseService _databaseService;
        private readonly ConcurrentDictionary<long, int> _pendingReviews = new ConcurrentDictionary<long, int>();
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
                    StartCommand(chatId);
                }
                else if (messageText == "получить список преподавателей")
                {
                    await SendTeachersListAsync(chatId);
                }
                else if (messageText == "добавить преподавателя"){
                    await _botClient.SendTextMessageAsync(chatId, "Чтобы добавить преподавателя, напишите /add ФИО номер телефона почта");
                }
                else if (messageText.StartsWith("/add")){
                    AddNewTeacher(chatId, messageText);
                }
                else if (_pendingReviews.TryGetValue(chatId, out int teacherId))
                {
                    await HandleReceivedReview(chatId, teacherId, messageText);
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

        private async Task StartCommand(long chatId)
        {
            var reply = new ReplyKeyboardMarkup(
                new[]
                {
                    new KeyboardButton("Получить список преподавателей"),
                    new KeyboardButton("Добавить преподавателя")
                })
            {
                ResizeKeyboard = true,
            };
            
            await _botClient.SendTextMessageAsync(chatId, "Добро пожаловать! Вы можете:\n1. Получить список преподавателей\n2. Оставить отзыв\n3. Добавить преподавателя", replyMarkup: reply);
        }

        private async Task SendTeachersListAsync(long chatId)
        {
            var teachers = _databaseService.GetTeachers();
            var inlineKeyboard = new InlineKeyboardMarkup(teachers.Select(t =>
                new[] { InlineKeyboardButton.WithCallbackData(t.Name + ' ' + t.Surname, t.Name + ' ' + t.Surname) }));

            await _botClient.SendTextMessageAsync(chatId, "Выберите преподавателя:", replyMarkup: inlineKeyboard);
        }

        private async Task AddNewTeacher(long chatId, string message){
            var data = message.Split(new[] { ' ' }, 6);
            var teacher = new Teacher(data[2], data[1], data[3], data[4], data[5], "a");
            Console.WriteLine(data[0] + '\n');
            Console.WriteLine(data[1] + '\n');
            Console.WriteLine(data[2] + '\n');
            Console.WriteLine(data[3] + '\n');
            Console.WriteLine(data[4] + '\n');
            Console.WriteLine(data[5] + '\n');
            _databaseService.AddTeacher(teacher);
            await _botClient.SendTextMessageAsync(chatId, "Преподаватель успешно добавлен!");
        }
        private async Task HandleCallbackQueryAsync(CallbackQuery callbackQuery)
        {
            var chatId = callbackQuery.Message.Chat.Id;
            if(callbackQuery.Data.StartsWith("leave_review")){
                int teacherId = int.Parse(callbackQuery.Data.Split('_')[2]);
                await HandleLeaveReview(callbackQuery.Message.Chat.Id, teacherId);
            }
            else{
                var teacherName = callbackQuery.Data;
                var teacher = _databaseService.GetTeacherByName(teacherName);
                var reviews = _databaseService.GetReviewsByTeacher(teacher.Id);
                var reply = $"{teacher.Name}\nОтзывы:\n" + string.Join("\n", reviews.Select(r => r.Text));
                var inlineKeyboard = new InlineKeyboardMarkup(
                new[] { InlineKeyboardButton.WithCallbackData("Оставить отзыв", $"leave_review_{teacher.Id}") });
                await _botClient.SendTextMessageAsync(chatId, reply, replyMarkup: inlineKeyboard);
                await _botClient.AnswerCallbackQueryAsync(callbackQuery.Id);
            }
            
        }
         private async Task HandleLeaveReview(long chatId, int teacherId)
        {
            _pendingReviews[chatId] = teacherId;
            await _botClient.SendTextMessageAsync(chatId, $"Пожалуйста, напишите ваш отзыв");
        }

        private async Task HandleReceivedReview(long chatId, int teacherId, string reviewText)
        {
            _databaseService.AddReview(teacherId, reviewText);
            _pendingReviews.TryRemove(chatId, out _);

            await _botClient.SendTextMessageAsync(chatId, "Спасибо за ваш отзыв!");
        }
        
        private Task HandleErrorAsync(ITelegramBotClient botClient, Exception exception, System.Threading.CancellationToken cancellationToken)
        {
            Console.WriteLine($"HandleErrorAsync:\n{exception}");
            return Task.CompletedTask;
        }

    }
}