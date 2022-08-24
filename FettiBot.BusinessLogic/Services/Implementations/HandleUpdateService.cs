using FettiBot.BusinessLogic.GoogleApi;
using FettiBot.BusinessLogic.Services.Interfaces;
using FettiBot.Model;
using FettiBot.Model.DatabaseModels;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Services;
using Google.Apis.Sheets.v4;
using Google.Apis.Sheets.v4.Data;
using System.Text.RegularExpressions;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;
using System.Diagnostics;

namespace FettiBot.BusinessLogic.Services.Implementations
{
    public class HandleUpdateService : IHandleUpdateService
    {
        private static ApplicationContext _context;
        public ITelegramBotClient _botClient;
        public static Client client;
        public static List<Client> clients = new();

        static readonly string SpreadsheetsId = "";
        static readonly string sheet1 = "Clients";
        static readonly string sheet2 = "Settings";
        static SheetsService service;

        static void CreateHeader()
        {
            var range = $"{sheet1}!A:K";
            var valueRange = new ValueRange();

            var objectList = new List<object>() { "Id", "Name", "Language", "Email", "Current location",
                "Next destination", "Reason for relocation", "Traveling with kids", "Interests",
                "Apps", "Access"};
            valueRange.Values = new List<IList<object>> { objectList };

            var appendRequest = service.Spreadsheets.Values.Append(valueRange, SpreadsheetsId, range);
            appendRequest.ValueInputOption = SpreadsheetsResource.ValuesResource
                .AppendRequest.ValueInputOptionEnum.USERENTERED;
            var appendResponse = appendRequest.Execute();
        }
        // // / / /// /// //////////////////////////////////////
        static void ReadEntries()
        {
            var range = $"{sheet1}!A1:F10";
            var request = service.Spreadsheets.Values.Get(SpreadsheetsId, range);

            var response = request.Execute();
            var values = response.Values;
            if (values != null && values.Count > 0)
            {
                foreach (var row in values)
                {
                    Console.WriteLine($"{row[5]} {row[4]} | {row[3]} | {row[1]}");
                }
            }
            else
            {
                Console.WriteLine("No data found.");
            }
        }
        public async Task EchoAsync(Update update, ITelegramBotClient client, ApplicationContext context)
        {
            GoogleCredential credential;
            service = new SheetsService(new BaseClientService.Initializer()
            {
                HttpClientInitializer = APIInitializer.Credentials,
                ApplicationName = APIInitializer.ApplicationName,
            });
            //CreateHeader();
            _botClient = client;
            _context = context;
            if (update.Type == UpdateType.Message && update?.Message?.Text != null)
            {
                await HandleMessage(client, update.Message);
                return;
            }
        }
        public static bool IsValidEmail(string email)
        {
          
            Regex emailRegex = new(@"^(?("")(""[^""]+?""@)|(([0-9a-z]((\.(?!\.))|[-!#\$%&'\*\+/=\?\^`\{\}\|~\w])*)(?<=[0-9a-z])@))(?(\[)(\[(\d{1,3}\.){3}\d{1,3}\])|(([0-9a-z][-\w]*[0-9a-z]*\.)+[a-z0-9]{2,17}))$", RegexOptions.IgnoreCase);
            return emailRegex.IsMatch(email);
        }
        public static async Task HandleMessage(ITelegramBotClient botClient, Message message)
        {
            //    if(message.Text == "/link")
            //    {
            //        var prs = new ProcessStartInfo("chrome.exe");
            //        prs.Arguments = "https://fetti.world";
            //        Process.Start(prs);
            //    }
            if (message.Text == "/start")
            {
                client = new Client { Num = message.From.Id, Name = message.From.FirstName };

                for (int i = 0; i < clients.Count; i++)
                {
                    if (clients[i].Num == client.Num)
                        clients.RemoveAt(i);
                }
                clients.Add(client);
                message.Text = "start";

                await HandleMessage(botClient, message);
                return;


            }
            for (int i = 0; i < clients.Count; i++)
            {
                if (clients[i].Num == client.Num)
                {
                    ReplyKeyboardMarkup keyboard = new(new KeyboardButton[] { "RU", "ENG" })
                    {
                        ResizeKeyboard = true
                    };
                    if (clients[i].Start == true && message.Text == "start")
                    {
                        await botClient.SendTextMessageAsync(message.Chat.Id, $"Hi, I’m Fetti bot" +
                        $"\nI have been created for Fetti app to improve our service offerings." +
                        $"\nPlease help me to make Fetti app more useful by answering few questions." +
                        $"\nIt will take only 5 minutes of your time." +
                        $"\nFor further information click the link: https://fetti.world");
                        await botClient.SendTextMessageAsync(message.Chat.Id, "Select a language, please", replyMarkup: keyboard);
                        return;
                    }
                    if (clients[i].Start == false && message.Text == "start")
                    {
                        await botClient.SendTextMessageAsync(message.Chat.Id, "Select a language, please", replyMarkup: keyboard);
                        return;
                    }
                }

            }
            for (int i = 0; i < clients.Count; i++)
                if (clients[i].Num == message.From.Id)
                {
                    // 1 move on to language
                    if ((message.Text == "Back" || message.Text == "Назад")
                        && (clients[i].LastMessage == "ENG" || clients[i].LastMessage == "RU"))
                    {
                        message.Text = "start";
                        clients[i].LastMessage = null;
                        clients[i].Start = false;
                        await HandleMessage(botClient, message);
                        return;
                    }
                    //language
                    if (message.Text == "RU" || message.Text == "ENG")
                    {
                        clients[i].Language = message.Text;
                        if (clients[i].Language == "ENG")
                        {
                            clients[i].LastMessage = clients[i].Language;
                            ReplyKeyboardMarkup keyboard = new(new KeyboardButton[] { "Back" })
                            {
                                ResizeKeyboard = true
                            };
                            await botClient.SendTextMessageAsync(message.Chat.Id, "Enter your email, please", replyMarkup: keyboard);
                            return;
                        }
                        if (clients[i].Language == "RU")
                        {
                            clients[i].LastMessage = clients[i].Language;
                            ReplyKeyboardMarkup keyboard = new(new KeyboardButton[] { "Назад" })
                            {
                                ResizeKeyboard = true
                            };
                            await botClient.SendTextMessageAsync(message.Chat.Id, "Введите электронную почту", replyMarkup: keyboard);
                            return;
                        }
                    }

                    // 2 move on to email
                    if (clients[i].NextD == false
                        && clients[i].Email != null
                        && clients[i].CurrentL == true
                        && clients[i].Moving == false
                        && clients[i].LastMessage == clients[i].Email
                        && (message.Text == "Back"
                        || message.Text == "Назад"))
                    {
                        message.Text = clients[i].Language;
                        clients[i].Language = null;
                        clients[i].LastMessage = null;
                        clients[i].Email = null;
                        clients[i].CurrentL = false;
                        clients[i].IsValidEmail = false;
                        await HandleMessage(botClient, message);
                        return;
                    }
                    //email

                    if (clients[i].CurrentL == false && clients[i].LastMessage == "RU"
                        || clients[i].LastMessage == "ENG" && clients[i].Email == null
                        && clients[i].IsValidEmail == false)
                    {
                        if (IsValidEmail(message.Text) == true)
                        {
                            clients[i].IsValidEmail = true;
                            clients[i].Email = message.Text;
                        }
                        if (IsValidEmail(message.Text) == false)
                        {
                            if (clients[i].Language == "ENG")
                            {
                                await botClient.SendTextMessageAsync(message.Chat.Id, "Email was not entered correctly");
                                return;
                            }
                            if (clients[i].Language == "RU")
                            {
                                await botClient.SendTextMessageAsync(message.Chat.Id, "Электронная почта была введена неверно");
                                return;
                            }
                        }

                        if (clients[i].Language == "ENG")
                        {
                            clients[i].LastMessage = message.Text;
                            clients[i].CurrentL = true;
                            ReplyKeyboardMarkup keyboard = new(new[]
                            {
                                new KeyboardButton[] { "Belarus", "Poland" },
                                new KeyboardButton[] { "Ukraine", "Russia" },
                                new KeyboardButton[] { "Lithuania", "Latvia" },
                                new KeyboardButton[] { "Germany", "Great Britain" },
                                new KeyboardButton[] { "Current Location", "Back" }
                            })
                            {
                                ResizeKeyboard = true
                            };

                            await botClient.SendTextMessageAsync(message.Chat.Id, "Select your current location, please", replyMarkup: keyboard);
                            return;
                        }
                        if (clients[i].Language == "RU")
                        {
                            clients[i].LastMessage = message.Text;
                            clients[i].CurrentL = true;
                            ReplyKeyboardMarkup keyboard = new(new[]
                            {
                                new KeyboardButton[] { "Беларусь", "Польша" },
                                new KeyboardButton[] { "Украина", "Россия" },
                                new KeyboardButton[] { "Литва", "Латвия" },
                                new KeyboardButton[] { "Германия", "Великобритания" },
                                new KeyboardButton[] { "Франция", "Испания" },
                                new KeyboardButton[] { "Текущее местоположение", "Назад" }
                            })
                            {
                                ResizeKeyboard = true
                            };
                            await botClient.SendTextMessageAsync(message.Chat.Id, "Выберите текущее местоположение", replyMarkup: keyboard);
                            return;
                        }
                    }
                    //3 move on to current location
                    if (clients[i].NextD == true
                        && clients[i].CurrentL == false
                        && clients[i].Moving == false
                        && (message.Text == "Back"
                        || message.Text == "Назад"))
                    {
                        clients[i].CurrentLocation = null;
                        message.Text = clients[i].Email;
                        clients[i].Email = null;
                        clients[i].LastMessage = clients[i].Language;
                        clients[i].IsValidEmail = false;
                        clients[i].NextD = false;
                        await HandleMessage(botClient, message);
                        return;
                    }

                    //current location

                    if (clients[i].CurrentL == true && (message.Text != "Current Location"
                        || message.Text == "Current Location" || clients[i].LastMessage == "Current Location"
                        || message.Text != "Текущее местоположение" || message.Text == "Текущее местоположение"
                        || clients[i].LastMessage == "Текущее местоположение"))
                    {
                        if (clients[i].CurrentL == true && message.Text != "Current Location"
                            || message.Text != "Текущее местоположение")
                        {
                            clients[i].CurrentLocation = message.Text;
                        }
                        if (message.Text == "Current Location" || message.Text == "Текущее местоположение")
                        {
                            clients[i].LastMessage = "Current Location";
                            if (clients[i].Language == "ENG")
                            {
                                await botClient.SendTextMessageAsync(message.Chat.Id, "Enter your choice");
                                return;
                            }
                            if (clients[i].Language == "RU")
                            {
                                await botClient.SendTextMessageAsync(message.Chat.Id, "Введите свой вариант");
                                return;
                            }
                        }
                        if (clients[i].LastMessage == "Current Location")
                        {
                            clients[i].CurrentLocation = message.Text;
                            clients[i].LastMessage = null;
                        }
                        if (clients[i].Language == "ENG")
                        {
                            ReplyKeyboardMarkup keyboard = new(new[]
                        {
                                new KeyboardButton[] { "Turkey", "Malaysia" },
                                new KeyboardButton[] { "Portugal", "Australia" },
                                new KeyboardButton[] { "Greece", "Poland" },
                                new KeyboardButton[] { "USA", "Brazil" },
                                new KeyboardButton[] { "Georgia", "China" },
                                new KeyboardButton[] { "Write other", "Back" }
                            })
                            {
                                ResizeKeyboard = true
                            };
                            if (clients[i].CurrentL == true && clients[i].CurrentLocation != null
                                && clients[i].NextDestination == null)
                            {
                                clients[i].NextD = true;
                                clients[i].CurrentL = false;
                                await botClient.SendTextMessageAsync(message.Chat.Id, "Select your next destination, please", replyMarkup: keyboard);
                                return;
                            }
                        }
                        if (clients[i].Language == "RU")
                        {
                            ReplyKeyboardMarkup keyboard = new(new[]
                            {
                                new KeyboardButton[] { "Турция", "Малайзия" },
                                new KeyboardButton[] { "Португалия", "Австралия" },
                                new KeyboardButton[] { "Греция", "Польша" },
                                new KeyboardButton[] { "США", "Бразилия" },
                                new KeyboardButton[] { "Грузия", "Китай" },
                                new KeyboardButton[] { "Написать другое", "Назад" }})
                            {
                                ResizeKeyboard = true
                            };
                            if (clients[i].CurrentL == true
                                && clients[i].CurrentLocation != null
                                && clients[i].NextDestination == null)
                            {
                                clients[i].NextD = true;
                                clients[i].CurrentL = false;
                                await botClient.SendTextMessageAsync(message.Chat.Id, "Выберите следующий пункт назначения", replyMarkup: keyboard);
                                return;
                            }
                        }
                    }

                    // 4 move on to next destination
                    if (clients[i].Moving == true
                        && clients[i].LastMessage != "childs"
                        && clients[i].LastMessage != "reason"
                        && clients[i].LastMessage != "interests"
                        && clients[i].LastMessage != "apps"
                        && clients[i].LastMessage != "access"
                        && (message.Text == "Back"
                        || message.Text == "Назад"))
                    {
                        clients[i].CurrentL = true;
                        message.Text = clients[i].NextDestination;
                        clients[i].NextDestination = null;
                        clients[i].Moving = false;
                        await HandleMessage(botClient, message);
                        return;
                    }

                    //next destination

                    if (clients[i].NextD == true && (message.Text != "Write other"
                        || message.Text == "Write other" || clients[i].LastMessage == "Write other"
                        || message.Text != "Написать другое" || message.Text == "Написать другое"
                        || clients[i].LastMessage == "Написать другое"))
                    {
                        if (clients[i].NextD == true && message.Text != "Write other"
                            || message.Text != "Написать другое")
                        {
                            clients[i].NextDestination = message.Text;
                            //
                        }
                        if (message.Text == "Write other" || message.Text == "Написать другое")
                        {
                            clients[i].LastMessage = "Write other";
                            if (clients[i].Language == "ENG")
                            {
                                await botClient.SendTextMessageAsync(message.Chat.Id, "Enter your choice");
                                return;
                            }
                            if (clients[i].Language == "RU")
                            {
                                await botClient.SendTextMessageAsync(message.Chat.Id, "Введите свой вариант");
                                return;
                            }
                        }
                        if (clients[i].LastMessage == "Write other")
                        {
                            clients[i].NextDestination = message.Text;
                            clients[i].LastMessage = null;
                            //
                        }
                        if (clients[i].Moving == false && clients[i].NextDestination != null)
                        {
                            if (clients[i].Language == "ENG")
                            {
                                clients[i].NextD = false;
                                clients[i].Moving = true;
                                ReplyKeyboardMarkup keyboard = new(new[]
                                {
                                    new KeyboardButton[] { "Job offer", "Tourism" },
                                    new KeyboardButton[] { "Investing", "Relocation" },
                                    new KeyboardButton[] { "Back" }

                                })
                                {
                                    ResizeKeyboard = true
                                };
                                await botClient.SendTextMessageAsync(message.Chat.Id, "Why are you moving?", replyMarkup: keyboard);
                                return;
                            }
                            if (clients[i].Language == "RU")
                            {
                                //
                                clients[i].NextD = false;
                                clients[i].Moving = true;
                                ReplyKeyboardMarkup keyboard = new(new[]
                                {
                                    new KeyboardButton[] { "Предложение работы", "Туризм" },
                                    new KeyboardButton[] { "Инвестирование", "Переезд" },
                                    new KeyboardButton[] { "Назад" }
                                })
                                {
                                    ResizeKeyboard = true
                                };
                                await botClient.SendTextMessageAsync(message.Chat.Id, "Почему вы переезжаете?", replyMarkup: keyboard);
                                return;
                            }
                        }
                    }
                    //move on to reason
                    if (clients[i].Moving == true
                       && clients[i].LastMessage == "reason"
                       && (message.Text == "Back"
                       || message.Text == "Назад")
                       )
                    {
                        clients[i].LastMessage = null;
                        clients[i].Moving = false;
                        message.Text = clients[i].ReasonForMoving;
                        clients[i].ReasonForMoving = null;
                        clients[i].NextD = true;
                        await HandleMessage(botClient, message);
                        return;
                    }

                    //reason

                    if (message.Text == "Job offer" || message.Text == "Tourism"
                        || message.Text == "Investing" || message.Text == "Relocation"
                        || message.Text == "Предложение работы" || message.Text == "Туризм"
                        || message.Text == "Инвестирование" || message.Text == "Переезд")
                    {
                        if (clients[i].Language == "ENG")
                        {
                            clients[i].LastMessage = "reason";
                            clients[i].ReasonForMoving = message.Text;
                            ReplyKeyboardMarkup keyboard = new(new[]
                            {
                                new KeyboardButton[] { "Yes", "No"},
                                new KeyboardButton[] {  "Alone", "Back" }})
                            {
                                ResizeKeyboard = true
                            };
                            await botClient.SendTextMessageAsync(message.Chat.Id, "Are you traveling with kids?", replyMarkup: keyboard);
                            return;
                        }
                        if (clients[i].Language == "RU")
                        {
                            clients[i].LastMessage = "reason";
                            clients[i].ReasonForMoving = message.Text;
                            ReplyKeyboardMarkup keyboard = new(new[]
                            {
                                new KeyboardButton[] { "Да", "Нет" },
                                new KeyboardButton[] { "Одинок", "Назад" }
                            })
                            {
                                ResizeKeyboard = true
                            };
                            await botClient.SendTextMessageAsync(message.Chat.Id, "Вы путешествуете с детьми?", replyMarkup: keyboard);
                            return;
                        }
                    }
                    //6 move on to childs
                    if (clients[i].LastMessage == "childs"
                       && (message.Text == "Back"
                       || message.Text == "Назад")
                       )
                    {
                        message.Text = clients[i].ReasonForMoving;
                        clients[i].ReasonForMoving = null;
                        clients[i].TravelingWithChildren = null;
                        clients[i].LastMessage = "reason";
                        await HandleMessage(botClient, message);
                        return;
                    }

                    //childs

                    if (message.Text == "Yes" || message.Text == "No" || message.Text == "Alone"
                        || message.Text == "Да" || message.Text == "Нет" || message.Text == "Одинок")
                    {
                        if (clients[i].Language == "ENG")
                        {
                            clients[i].LastMessage = "childs";
                            clients[i].TravelingWithChildren = message.Text;
                            ReplyKeyboardMarkup keyboard = new(new[]
                            {
                                new KeyboardButton[] { "Education", "Job relocation" },
                                new KeyboardButton[] { "Job Seeking", "Investment" },
                                new KeyboardButton[] { "Weather", "Love (partner/ family reunion)" },
                                new KeyboardButton[] { "Meetings", "Entertainment" },
                                new KeyboardButton[] { "Hospitality", "Extreme Sport" },
                                new KeyboardButton[] { "Finish choosing interests", "Back" }
                            })
                            {
                                ResizeKeyboard = true
                            };
                            await botClient.SendTextMessageAsync(message.Chat.Id, "What are you interested at in? (not more than 5)", replyMarkup: keyboard);
                            return;
                        }
                        if (clients[i].Language == "RU")
                        {
                            clients[i].LastMessage = "childs";
                            clients[i].TravelingWithChildren = message.Text;
                            ReplyKeyboardMarkup keyboard = new(new[]
                            {
                                new KeyboardButton[] { "Образование", "Перемещение работы" },
                                new KeyboardButton[] { "Поиск работы", "Инвестиции" },
                                new KeyboardButton[] { "Погода", "Любовь (партнер/воссоединение семьи)" },
                                new KeyboardButton[] { "Встречи", "Развлечения" },
                                new KeyboardButton[] { "Гостеприимство", "Экстремальный спорт" },
                                new KeyboardButton[] { "Закончить выбор интересов", "Назад" }
                            })
                            {
                                ResizeKeyboard = true
                            };
                            await botClient.SendTextMessageAsync(message.Chat.Id, "Чем вы интересуетесь? (не более 5)", replyMarkup: keyboard);
                            return;
                        }
                    }
                    //7 move on to interests
                    if (clients[i].LastMessage == "interests"
                       && (message.Text == "Back"
                       || message.Text == "Назад")
                       )
                    {
                        clients[i].LastMessage = "childs";
                        message.Text = clients[i].TravelingWithChildren;
                        clients[i].TravelingWithChildren = null;
                        clients[i].Interests = null;
                        clients[i].IntCount = 0;
                        await HandleMessage(botClient, message);
                        return;
                    }

                    //interests

                    if (message.Text == "Job relocation" || message.Text == "Education"
                       || message.Text == "Job Seeking" || message.Text == "Investment"
                       || message.Text == "Weather" || message.Text == "Love (partner/ family reunion)"
                       || message.Text == "Meetings" || message.Text == "Entertainment"
                       || message.Text == "Hospitality" || message.Text == "Extreme Sport"
                       || message.Text == "Образование" || message.Text == "Перемещение работы"
                       || message.Text == "Поиск работы" || message.Text == "Инвестиции"
                       || message.Text == "Погода" || message.Text == "Любовь (партнер/воссоединение семьи)"
                       || message.Text == "Встречи" || message.Text == "Развлечения"
                       || message.Text == "Гостеприимство" || message.Text == "Экстремальный спорт")
                    {
                        if (clients[i].AreInterestsFilledIn == false)
                        {

                            if (clients[i].IntCount == 0)
                            {
                                clients[i].LastMessage = "interests";
                                clients[i].Interests += $"{message.Text}";
                                clients[i].IntCount++;
                            }
                            if (clients[i].IntCount > 3)
                            {
                                clients[i].LastMessage = "interests";
                                clients[i].AreInterestsFilledIn = true;
                                return;
                            }
                            if (clients[i].IntCount > 0 && clients[i].IntCount <= 3)
                            {
                                clients[i].LastMessage = "interests";
                                clients[i].Interests += $", {message.Text}";
                                clients[i].IntCount++;
                            }

                        }
                    }
                    if (message.Text == "Finish int"
                        || clients[i].AreInterestsFilledIn == true
                        || message.Text == "Finish choosing interests"
                        || message.Text == "Закончить выбор интересов")
                    {
                        clients[i].AreInterestsFilledIn = false;
                        if (clients[i].Language == "ENG")
                        {
                            if (clients[i].IntCount == 0)
                            {
                                //
                                clients[i].LastMessage = "interests";
                                await botClient.SendTextMessageAsync(message.Chat.Id, "Make a choice");
                                return;
                            }
                            ReplyKeyboardMarkup keyboard = new(new[]
                            {
                                new KeyboardButton[] { "Transport (Uber etc)", "Eating (Uber Eats etc)" },
                                new KeyboardButton[] { "Booking (booking.com, etc)", "Traveling (Yelp/Google Maps)" },
                                new KeyboardButton[] { "Recommendations (Yelp)", "Events (Event Calendar etc)" },
                                new KeyboardButton[] { "Chats (Whats app, etc)", "Forums (facebook etc)" },
                                new KeyboardButton[] { "Local News (the Guardian etc)", "Weather (accuro etc)" },
                                new KeyboardButton[] { "Finish selecting applications", "Back" }
                            })
                            {
                                ResizeKeyboard = true
                            };

                            //
                            clients[i].LastMessage = "interests";
                            await botClient.SendTextMessageAsync(message.Chat.Id, "What apps do you use while traveling?", replyMarkup: keyboard);
                            return;
                        }
                        if (clients[i].Language == "RU")
                        {
                            if (clients[i].IntCount == 0)
                            {
                                //
                                clients[i].LastMessage = "interests";
                                await botClient.SendTextMessageAsync(message.Chat.Id, "Сделайте выбор");
                                return;
                            }
                            ReplyKeyboardMarkup keyboard = new(new[]
                            {
                                new KeyboardButton[] { "Транспорт (Uber и т.д)", "Еда (Uber Eats и т.д)" },
                                new KeyboardButton[] { "Бронирование (booking.com и т.д)", "Путешествие (Yelp/Google Maps)" },
                                new KeyboardButton[] { "Рекомендации (Yelp)", "События (Event Calendar и т.д)" },
                                new KeyboardButton[] { "Чаты (Whats app, и т.д)", "Форумы (facebook и т.д)" },
                                new KeyboardButton[] { "Местные новости (the Guardian и т.д)", "Погода (accuro и т.д)" },
                                new KeyboardButton[] { "Закончить выбор приложений", "Назад" }
                            })
                            {
                                ResizeKeyboard = true
                            };

                            //
                            clients[i].LastMessage = "interests";

                            await botClient.SendTextMessageAsync(message.Chat.Id, "Какими приложениями вы пользуетесь во время путешествий?", replyMarkup: keyboard);
                            return;
                        }
                    }
                    //8 move on to apps
                    if (clients[i].LastMessage == "apps"
                        && (message.Text == "back"
                       || message.Text == "назад"))
                    {
                        clients[i].LastMessage = "interests";
                        message.Text = "Finish int";
                        clients[i].Applications = null;
                        clients[i].AppCount = 0;
                        await HandleMessage(botClient, message);
                        return;
                    }

                    //apps

                    if (message.Text == "Transport (Uber etc)" || message.Text == "Eating (Uber Eats etc)" ||
                        message.Text == "Booking (booking.com, etc)" || message.Text == "Traveling (Yelp/Google Maps)" ||
                        message.Text == "Recommendations (Yelp)" || message.Text == "Events (Event Calendar etc)" ||
                        message.Text == "Chats (Whats app, etc)" || message.Text == "Forums (facebook etc)" ||
                        message.Text == "Local News (the Guardian etc)" || message.Text == "Weather (accuro etc)" ||
                        message.Text == "Транспорт (Uber и т.д)" || message.Text == "Еда(Uber Eats и т.д)" ||
                        message.Text == "Бронирование (booking.com и т.д)" || message.Text == "Путешествие (Yelp/Google Maps)" ||
                        message.Text == "Рекомендации (Yelp)" || message.Text == "События (Event Calendar и т.д)" ||
                        message.Text == "Чаты (Whats app, и т.д)" || message.Text == "Форумы (facebook и т.д)" ||
                        message.Text == "Местные новости (the Guardian и т.д)" || message.Text == "Погода (accuro и т.д)")
                    {
                        if (clients[i].AppCount == 0)
                        {
                            clients[i].LastMessage = "apps";
                            clients[i].Applications += $"{message.Text}";
                            clients[i].AppCount++;
                        }
                        else
                        {
                            clients[i].LastMessage = "apps";
                            clients[i].Applications += $", {message.Text}";
                            clients[i].AppCount++;
                        }
                    }
                    if (message.Text == "Finish app" || message.Text == "Finish selecting applications" || message.Text == "Закончить выбор приложений")
                    {
                        if (clients[i].Language == "ENG")
                        {
                            if (clients[i].AppCount == 0)
                            {
                                await botClient.SendTextMessageAsync(message.Chat.Id, "Make a choice");
                                return;
                            }

                            clients[i].LastMessage = "apps";
                            ReplyKeyboardMarkup keyboard = new(new KeyboardButton[] { "yes", "no", "back" })
                            { ResizeKeyboard = true };
                            await botClient.SendTextMessageAsync(message.Chat.Id, "Do you want to simplify access to useful information? " +
                                "\nThen sign up for FETTI an early access", replyMarkup: keyboard);
                            return;
                        }
                        if (clients[i].Language == "RU")
                        {
                            if (clients[i].AppCount == 0)
                            {
                                await botClient.SendTextMessageAsync(message.Chat.Id, "Сделайте выбор");
                                return;
                            }
                            clients[i].LastMessage = "apps";
                            ReplyKeyboardMarkup keyboard = new(new KeyboardButton[] { "да", "нет", "назад" })
                            { ResizeKeyboard = true };
                            await botClient.SendTextMessageAsync(message.Chat.Id, "Хотите упростить доступ к полезной информации? " +
                                "\nТогда подпишитесь на FETTI с ранним доступом", replyMarkup: keyboard);
                            return;
                        }
                    }
                    //9 move on to access
                    if (clients[i].LastMessage == "access"
                        && (message.Text == "back"
                       || message.Text == "назад"))
                    {

                        clients[i].Save = true;
                        clients[i].Access = null;
                        clients[i].LastMessage = "apps";
                        message.Text = "Finish app";
                        await HandleMessage(botClient, message);
                        return;
                    }
                    //access

                    if (message.Text == "yes" || message.Text == "no"
                        || message.Text == "да" || message.Text == "нет")
                    {
                        clients[i].Access = message.Text;
                        if (clients[i].Language == "ENG")
                        {
                            ReplyKeyboardMarkup keyboard = new(new KeyboardButton[] { "end", "back" })
                            {
                                ResizeKeyboard = true
                            };
                            clients[i].LastMessage = "access";
                            clients[i].Save = true;

                            await botClient.SendTextMessageAsync(message.Chat.Id, "Select the command", replyMarkup: keyboard);
                            return;
                        }
                        if (clients[i].Language == "RU")
                        {
                            ReplyKeyboardMarkup keyboard = new(new KeyboardButton[] { "закончить", "назад" })
                            {
                                ResizeKeyboard = true
                            };
                            clients[i].LastMessage = "access";
                            clients[i].Save = true;

                            await botClient.SendTextMessageAsync(message.Chat.Id, "Выберите команду", replyMarkup: keyboard);
                            return;
                        }

                    }

                    if (clients[i].LastMessage == "access"
                         && (message.Text == "end"
                         || message.Text == "закончить"))
                    {
                        _context.Clients.Add(clients[i]);
                        _context.SaveChanges();

                        var range = $"{sheet1}!A:K";
                        var valueRange = new ValueRange();

                        var objectList = new List<object>()
                                  {
                                clients[i].Id,
                                clients[i].Name,
                                clients[i].Language,
                                clients[i].Email,
                                clients[i].CurrentLocation,
                                clients[i].NextDestination,
                                clients[i].ReasonForMoving,
                                clients[i].TravelingWithChildren,
                                clients[i].Interests,
                                clients[i].Applications,
                                clients[i].Access
                            };



                        valueRange.Values = new List<IList<object>> { objectList };

                        var appendRequest = service.Spreadsheets.Values.Append(valueRange, SpreadsheetsId, range);
                        appendRequest.ValueInputOption = SpreadsheetsResource.ValuesResource
                            .AppendRequest.ValueInputOptionEnum.USERENTERED;
                        var appendResponse = appendRequest.Execute();
                        ReplyKeyboardMarkup keyboard = new(new KeyboardButton[] { "/start" })
                        {
                            ResizeKeyboard = true
                        };
                        if (clients[i].Language == "ENG")
                        {

                            await botClient.SendTextMessageAsync(message.Chat.Id, "Thank you for your time", replyMarkup: keyboard);
                            return;
                        }
                        if (clients[i].Language == "RU")
                        {

                            await botClient.SendTextMessageAsync(message.Chat.Id, "Спасибо за уделенное время", replyMarkup: keyboard);
                            return;
                        }
                        //await HandleMessage(botClient, message);
                        //return;
                    }
                }

        }
    }
}
