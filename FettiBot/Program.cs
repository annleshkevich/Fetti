using AutoMapper;
using FettiBot.BusinessLogic.Services.Implementations;
using FettiBot.BusinessLogic.Services.Interfaces;
using FettiBot.Common.Mapper;
using FettiBot.Model;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Telegram.Bot;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Exceptions;
using Google.Apis.Sheets.v4;
using Google.Apis.Auth.OAuth2;
using System.IO;
using Google.Apis.Services;
using FettiBot.BusinessLogic.GoogleApi;
using Google.Apis.Sheets.v4.Data;

namespace FeedbackBot
{
    class Program
    {
        public static HandleUpdateService handleService;
        public static TelegramBotClient client;

        static void Main(string[] args)
        {
            var mappingConfig = new MapperConfiguration(mc => mc.AddProfile(new MapperProfile()));
            IMapper mapper = mappingConfig.CreateMapper();
            var builder = new ConfigurationBuilder();
            BuildConfig(builder);
            var config = builder.Build();

            builder.SetBasePath(Directory.GetCurrentDirectory());
            builder.AddJsonFile("appsettings.json");
            string connectionString = config.GetConnectionString("DefaultConnection");

            var optionsBuilder = new DbContextOptionsBuilder<ApplicationContext>();
            var options = optionsBuilder.UseSqlServer(connectionString).Options;

            IHost host = Host.CreateDefaultBuilder()
               .ConfigureServices((context, services) =>
               {
                   services.AddTransient<IHandleUpdateService, HandleUpdateService>();
                   services.AddTransient<FettiBot.BusinessLogic.Services.Interfaces.IClientService, ClientService>();
                   services.AddDbContext<ApplicationContext>(options => options.UseSqlServer(connectionString));
                   services.AddSingleton(mapper);
               })
               .Build();
            HandleUpdateService handleService = ActivatorUtilities.CreateInstance<HandleUpdateService>(host.Services);

            client = new("5682359235:AAGnSE6EdzC1_2l7BKNMtqohEGzHgUC0Woc");
            Console.WriteLine("bot is running...");

            async Task HandleUpdateAsync(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
            {

                await handleService.EchoAsync(update, botClient, new ApplicationContext(options));
                Console.WriteLine(Newtonsoft.Json.JsonConvert.SerializeObject(update));
            }

            var cts = new CancellationTokenSource();
            var cancellationToken = cts.Token;
            var receiverOptions = new ReceiverOptions
            {
                AllowedUpdates = { },
            };
            client.StartReceiving(HandleUpdateAsync, HandleError, receiverOptions, cancellationToken: cts.Token);
            Console.ReadLine();
        }
       
        public static Task HandleError(ITelegramBotClient client, Exception exception, CancellationToken cancellation)
        {
            var ErrorMessage = exception switch
            {
                ApiRequestException apiRequestException
                => $"Ошибка телеграм АПИ:\n{apiRequestException.ErrorCode}\n{apiRequestException.Message}",
                _ => exception.ToString()
            };
            Console.WriteLine(ErrorMessage);
            return Task.CompletedTask;
        }
        static void BuildConfig(IConfigurationBuilder builder)
        {
            builder.SetBasePath(Directory.GetCurrentDirectory())
               .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
               .AddJsonFile($"appsettings.{Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production"}.json", optional: true)
               .AddEnvironmentVariables();
        }
    }
}