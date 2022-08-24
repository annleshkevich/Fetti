using FettiBot.Model;
using Microsoft.EntityFrameworkCore;
using Telegram.Bot;
using Telegram.Bot.Types;

namespace FettiBot.BusinessLogic.Services.Interfaces
{
    public interface IHandleUpdateService
    {
        Task EchoAsync(Update update, ITelegramBotClient client, ApplicationContext context);
    }
}
