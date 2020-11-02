using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.VoiceNext;
using DSharpPlus.CommandsNext;
using LoLDiscover.DiscrodUserLOL;
using LoLDiscover.Fun;
using MingweiSamuel.Camille;
using MingweiSamuel.Camille.Enums;
using static LoLDiscover.ProgramDataSaver;
using DSharpPlus.Interactivity;
using DSharpPlus.Interactivity.Extensions;

namespace LoLDiscover
{
    public class CommandDescription
    {
        public string Name;
        public string Description;
        public string Usage;

        public CommandDescription(string name, string description, string usage)
        {
            Name = name;
            Description = description;
            Usage = usage;
        }

        public override string ToString()
        {
            return $"Команда ``{Name}``\n*{Description}*\n\nИспользование: ``{Usage}``";
        }
    }

    public static class Program
    {
        public static RiotApi riot;
        public static List<LoLUser> registeredUsers = new List<LoLUser>();
        public static List<GameRoom> gameRooms = new List<GameRoom>();
        public static DiscordClient Discord;

        public static VoiceNextExtension voice;
        private static CommandsNextExtension command;
        public static InteractivityExtension interactivity;
        private static DiscordChannel commandChannel;
        private static DiscordChannel anoncementChannel;
        public static DiscordChannel waitingRoom;
        public static DiscordChannel errorChannel;
        public static DiscordChannel eventsChannel;
        public static DiscordGuild discordGuild;
        private static bool needToUploadDiscordUsers = true;
        private static bool needToShowChangeLog = false;

        private static List<CommandDescription> descriptions = new List<CommandDescription>();


        private static void registerCommand(string name,string desc,string usage)
        {
            CommandDescription Cd = new CommandDescription(name, desc, usage);
            descriptions.Add(Cd);
        }


        private static string[] winImages =
        {
            "https://risovach.ru/upload/2013/11/mem/zheleznyy-chelovek_34792573_big_.jpeg",
            "http://memesmix.net/media/created/cp3jx3.png",
            "http://memesmix.net/media/created/pwo8xx.jpg",
            "https://memegenerator.net/img/instances/24527006/-.jpg",
            "https://echo.msk.ru/files/3218370.jpg",
            "https://ptzgovorit.ru/sites/default/files/styles/700x400/public/original_nodes/1111111111111111.jpg",
            "http://risovach.ru/upload/2015/02/mem/klichko_74141922_orig_.png"
        };

        private static string[] loseImages =
        {
            "https://mem-generator.ru/wp-content/uploads/2019/08/22.jpg",
            "https://www.saratovnews.ru/i/news/big/144491767785.jpg",
            "http://memesmix.net/media/created/q7lgu3.jpg",
            "https://lh3.googleusercontent.com/proxy/J4IFmjEEngrUwHVgMIrdKVj2S1Vsnfnw1nB4UXbS8hSFBYZNQAFyZgSjwE0SEyBYGPS_y74Dz0Q331h-6G1ND9EJnm0",
            "https://risovach.ru/upload/2018/06/mem/bezlimiticshe_180588084_orig_.jpg",
            "https://risovach.ru/upload/2014/06/mem/guf_53635275_orig_.jpg",
            "https://risovach.ru/upload/2016/03/mem/tipichnyy-klichko_108444554_orig_.jpg",
            "https://reporter-ua.com/sites/default/files/styles/870x/public/uploads/photos/7ihl4ogcxji.jpg"
        };

        private static void onNewVersion(object sender, EventNewVersionLaunched args)
        {
            needToShowChangeLog = true;
        }

        static void Main(string[] args)
        {
            registerCommand(";unreg", "Удаляет Ваш аккаунт Riot из базы данных бота", ";unreg");
            registerCommand(";reg", "Добавляет Ваш аккаунт Riot в базу данных бота", ";reg [ИМЯ ПОЛЬЗОВАТЕЛЯ RIOT]");
            registerCommand(";stats", "Показывают текущую статистику на сервере Discord", ";stats");
            registerCommand(";clr", "Полностью очищает канал ``команды`` (доступно для выполнения только в этом канале)", ";clr");
            registerCommand(";achivement", "Создаёт достижение-прикол и выводит сообщение о нём в канале, где введена команда. Достижение исчезает через минуту.\nОбратите внимание: Если название состоит из нескольких слов - слова нужно разделять символом ``_``", ";achivement [Название_Достижения] [Описание достижения]");
            registerCommand(";speak", "Воспроизводит голосовое сообщение в вашем голосовом канале. ", ";speak [сообщение для воспроизведения]");
            registerCommand(";help", "Показывает список команд бота или подробное описание одной команды.", ";help или ;help [&комманда]");
            registerCommand(";showa", "Показывает список полученных Вами достижений на сервере Discord.", ";showa");

            Console.WriteLine("Происходит загрузка бота...");
            riot = RiotApi.NewInstance("RGAPI-81433e5d-2024-4f6e-a6d4-ffb915146f31");
            Console.WriteLine("Загрузка сохраненных данных");
            ProgramDataSaver.OnNewVersion += onNewVersion;

            if((registeredUsers = ProgramDataSaver.LoadSaveData()) == null)
            {
                registeredUsers = new List<LoLUser>();
                Console.WriteLine("[ПРЕДУПРЕЖДЕНИЕ] Не удалось загрузить данные");
                needToUploadDiscordUsers = false;
                needToShowChangeLog = true;
            }
            else
            {
                foreach(var user in registeredUsers)
                {
                    Console.WriteLine($"{user.summername} - {user.summonerID} - {user.discordUserId} - Статус: ЗАГРУЖЕН");
                }
            }
            Console.WriteLine("Запуск бота дискорда...");
            Task.Run(CheckTimer);
            AppDomain.CurrentDomain.ProcessExit += CurrentDomain_ProcessExit;
            MainThread(args).ConfigureAwait(false).GetAwaiter().GetResult();
        }

        private static async void CurrentDomain_ProcessExit(object sender, EventArgs e)
        {
            await SendAchivement(await Discord.GetChannelAsync(769702976926384138), "Бот выключается", "В данный момент бот выключен :(\n");
        }

        public static async Task MainThread(string[] args)
        {
            Discord = new DiscordClient(new DiscordConfiguration()
            {
                Token = "NzY5Njk2MzA0NDk3NDI2NDQ0.X5Sxiw.il2wwTtsZ_cF5GjU_DjmYXjMpG4",
                TokenType = TokenType.Bot,
                MinimumLogLevel = Microsoft.Extensions.Logging.LogLevel.Debug
            });

            //Комманды
            Discord.MessageCreated += async (o,e) =>
            {
                Console.WriteLine("MessageCreatedEvent fired!");
                //if (e.Channel.Id != 769702849520992266) return;
                string message = e.Message.Content;
                if(message.StartsWith('&'))
                {
                    await e.Message.RespondAsync("Вы используете старый префикс ``&``. Перфиксы были изменены для удобства. Используйте ``;``");
                    return;
                }

                if (message.StartsWith(";"))
                {
                    if(message.StartsWith(";help"))
                    {
                        if (message.Split(' ').Length < 2)
                        {
                            DiscordEmbedBuilder embed = new DiscordEmbedBuilder();
                            embed.WithTitle("Список команд");
                            string content = "";
                            foreach(var com in descriptions)
                            {
                                content += $"- **{com.Name}** - *{com.Description}*\n\n";
                            }
                            embed.WithDescription(content);
                            embed.WithColor(new DiscordColor(90,90,0));
                            embed.WithFooter("Введите ``;help [;команда]`` чтобы узнать подробнее");
                            await e.Channel.SendMessageAsync(embed: embed.Build());
                            return;
                        }
                        else
                        {
                            string[] temp = message.Split(' ');
                            var com = descriptions.Where(d => d.Name == temp[1]).FirstOrDefault();
                            if(com == null)
                            {
                                await SendError(e.Channel, "Такой команды не обнаружено", "Такой команды нет! \nПроверьте правильность написания команды ``&help``\n\nПример использования ``&help &reg``");
                            }
                            else
                            {
                                DiscordEmbedBuilder embed = new DiscordEmbedBuilder();
                                embed.WithTitle($"Информация о команде {temp[1]}");
                                embed.WithDescription(com.ToString());
                                embed.WithColor(new DiscordColor(90, 90, 0));
                                await e.Channel.SendMessageAsync(embed: embed.Build());
                            }
                        }
                        return;
                    }
                }
            };



            
            await Discord.ConnectAsync();
            if (needToUploadDiscordUsers)
            {
                await LoadDiscordRegisteredAccounts();
                needToUploadDiscordUsers = false;
            }
            voice = Discord.UseVoiceNext();

            var config = new CommandsNextConfiguration();
            config.CaseSensitive = false;
            config.StringPrefixes = new string[1] { ";" };
            config.EnableDefaultHelp = false;
            command = Discord.UseCommandsNext(config);
            command.RegisterCommands<CommandsClass>();
            interactivity = Discord.UseInteractivity();
            waitingRoom =  await Discord.GetChannelAsync(770182488785289246);
            anoncementChannel = await Discord.GetChannelAsync(680669407466356787);
            commandChannel = await Discord.GetChannelAsync(769702849520992266);
            errorChannel = await Discord.GetChannelAsync(772462876430434345);
            eventsChannel = await Discord.GetChannelAsync(772586926691713054);
            discordGuild = await Discord.GetGuildAsync(680104639911165961);
            await SendDone(await Discord.GetChannelAsync(769702976926384138), "Бот включен", "Бот работает! Ура!\n");
            if (needToShowChangeLog) await ShowChangeLog();
            await Task.Delay(-1);
        }

        public static async Task LoadDiscordRegisteredAccounts()
        {
            foreach(var d in registeredUsers)
            {
                await d.LoadDiscordUser(Discord);
            }
        }

        public static async Task<LoLUser> CreateNewLolUser(DiscordUser user, string lolusername)
        {
            var summonerData = await riot.SummonerV4.GetBySummonerNameAsync(Region.RU, lolusername);
            if (summonerData == null)
            {
                return null;
            }
            LoLUser newuser = new LoLUser();
            newuser.summername = lolusername;
            newuser.summonerData = summonerData;
            newuser.discordUser = user;
            newuser.discordUserId = user.Id;
            newuser.summonerID = summonerData.Id;
            newuser.lolUsername = summonerData.Name;
            registeredUsers.Add(newuser);
            ProgramDataSaver.SaveSaveData(registeredUsers.ToArray());
            return newuser;
        }

        public static async Task<DiscordMessage> SendWrongCommandError(DiscordChannel channel, string command, string usage)
        {
            DiscordEmbedBuilder embed = new DiscordEmbedBuilder();
            embed.WithTitle($"Неверно набрана команда {command}");
            embed.WithDescription($"Правильное использование команды\n**{usage}**");
            embed.WithColor(new DiscordColor(255, 255, 0));
            var d = await Discord.SendMessageAsync(channel, null, false, embed.Build());
            await Task.Delay(5000);
            await channel.DeleteMessageAsync(d);
            return d;
        }

        public static async Task<DiscordMessage> SendError(DiscordChannel channel, string title, string message)
        {
            DiscordEmbedBuilder embed = new DiscordEmbedBuilder();
            embed.WithTitle(title);
            embed.WithDescription(message);
            embed.WithColor(new DiscordColor(255, 0, 0));
            var d = await Discord.SendMessageAsync(channel, null, false, embed.Build());
            await Task.Delay(5000);
            await channel.DeleteMessageAsync(d);
            return d;
        }

        public static async Task<DiscordMessage> SendFatalError(DiscordChannel channel, string title, string message)
        {
            DiscordEmbedBuilder embed = new DiscordEmbedBuilder();
            embed.WithTitle(title);
            embed.WithDescription(message);
            embed.WithColor(new DiscordColor(255, 0, 0));
            var d = await Discord.SendMessageAsync(channel, null, false, embed.Build());
            return d;
        }

        public static async Task<DiscordMessage> SendDone(DiscordChannel channel, string title, string message)
        {
            DiscordEmbedBuilder embed = new DiscordEmbedBuilder();
            embed.WithTitle(title);
            embed.WithDescription(message);
            embed.WithColor(new DiscordColor(0, 190, 0));
            var d = await Discord.SendMessageAsync(channel, null, false, embed.Build());
            await Task.Delay(5000);
            await channel.DeleteMessageAsync(d);
            return d;
        }

        public static async Task<DiscordMessage> SendAchivement(DiscordChannel channel, string title, string message)
        {
            DiscordEmbedBuilder embed = new DiscordEmbedBuilder();
            embed.WithTitle(title);
            embed.WithDescription(message);
            embed.WithFooter(embed.Timestamp.ToString());
            embed.WithColor(new DiscordColor(190, 190, 0));
            var d = await Discord.SendMessageAsync(channel, null, false, embed.Build());
            await Task.Delay(60000);
            await channel.DeleteMessageAsync(d);
            return d;
        }

        public static async Task<DiscordMessage> SendAchivementPermament(DiscordChannel channel, string title, string message)
        {
            DiscordEmbedBuilder embed = new DiscordEmbedBuilder();
            embed.WithTitle(title);
            embed.WithDescription(message);
            embed.WithFooter(DateTime.Now.ToString("dd.MM.yyyy"));
            embed.WithColor(new DiscordColor(190, 190, 0));
            var d = await Discord.SendMessageAsync(channel, null, false, embed.Build());
            return d;
        }

        public static async Task<DiscordMessage> SendMathResult(DiscordChannel channel, string title, string message,bool win)
        {
            DiscordEmbedBuilder embed = new DiscordEmbedBuilder();
            embed.WithTitle(title);
            embed.WithDescription(message);
            embed.WithFooter(embed.Timestamp.ToString());
            embed.WithColor(new DiscordColor(190, 190, 0));
            if(win)
            {
                embed.WithImageUrl(winImages[new Random().Next(0, winImages.Length)]);
            }
            else
            {
                embed.WithImageUrl(loseImages[new Random().Next(0, loseImages.Length)]);
            }
            return await Discord.SendMessageAsync(channel, null, false, embed.Build());
        }

        public static async Task CheckTimer()
        {
            while (true)
            {
                await Task.Delay((int)(TimeSpan.FromMinutes(1).TotalMilliseconds));
                await CheckAllRegisteredUsersForAchivements();
                ProgramDataSaver.SaveSaveData(registeredUsers.ToArray());
               // await CheckUsersInGame();
            }
        }

        public static async Task ShowChangeLog()
        {
            if (!File.Exists("changelog.txt")) return;
            StreamReader SR = new StreamReader("changelog.txt");
            string changelog = "";
            while(!SR.EndOfStream)
            {
                changelog += SR.ReadLine() + "\n";
            }
            changelog += "*Оцените данное обновление*";
            DiscordEmbedBuilder embed = new DiscordEmbedBuilder();
            embed.WithTitle($"Обновление {Assembly.GetExecutingAssembly().GetName().Version.ToString()}");
            embed.WithDescription(changelog);
            embed.WithColor(new DiscordColor(0, 140, 0));
            var chmes = await anoncementChannel.SendMessageAsync(embed: embed.Build());
            await chmes.CreateReactionAsync(DiscordEmoji.FromName(Discord, ":thumbsup:"));
            await chmes.CreateReactionAsync(DiscordEmoji.FromName(Discord, ":thumbsdown:"));
            return;
        }


        public static async Task ShowChangeLog(DiscordChannel channelAsked)
        {
            if (!File.Exists("changelog.txt")) return;
            StreamReader SR = new StreamReader("changelog.txt");
            string changelog = "";
            while (!SR.EndOfStream)
            {
                changelog += SR.ReadLine() + "\n";
            }
            DiscordEmbedBuilder embed = new DiscordEmbedBuilder();
            embed.WithTitle($"Обновление {Assembly.GetExecutingAssembly().GetName().Version.ToString()}");
            embed.WithDescription(changelog);
            embed.WithColor(new DiscordColor(0, 140, 0));
            var d = await channelAsked.SendMessageAsync(embed: embed.Build());
            await Task.Delay(10000);
            await channelAsked.DeleteMessageAsync(d);
            return;
        }

        public static async Task CheckAllRegisteredUsersForAchivements()
        {
            foreach (var user in registeredUsers) {
                try
                {
                    await Task.Delay(1500);
                    var d = await riot.MatchV4.GetMatchlistAsync(Region.RU, user.summonerData.AccountId, endIndex: 3);
                    var matchDataTask = d.Matches.Select(match => riot.MatchV4.GetMatchAsync(Region.RU, match.GameId)).ToArray();
                    var result = await Task.WhenAll(matchDataTask);
                
                if (result[0] != user.lastMatch)
                {
                        
                    var participantID = result[0].ParticipantIdentities.First(pi => user.summonerData.Id.Equals(pi.Player.SummonerId));
                    var participant = result[0].Participants.First(p => p.ParticipantId == participantID.ParticipantId);
                    string winlose = (participant.Stats.Win) ? "**выиграна**" : "**проиграна** :(";
                    if (user.lastMatch == null || user.gameID == 0)
                    {
                            await SendMathResult(await Discord.GetChannelAsync(769702976926384138), $"Последний матч игрока **{user.summername}**", "Этот человек только зарегистрировался! \n" +
                            "Давайте посмотрим на его статистику последнего матча!\n" +
                            "Эта игра была " + winlose +
                            $"\nГерой: {((Champion)participant.ChampionId).Name()}\n" +
                            CreateStatsString(participant)+"\n\n" +
                            $"\n**Ждем следующий бой! Посмотрим, сможет ли лучше, чем сейчас?**", participant.Stats.Win);
                            await user.CreateAchivement("Посмотрите, как я играю", "Бот рассказал об последней игре нового зарегистрированного пользователя.");
                    }
                    else if(result[0].GameId != user.gameID)
                    {
                            await SendMathResult(await Discord.GetChannelAsync(769702976926384138), $"Новый матч игрока **{user.summername}**", $"{user.summername} закончил еще один матч! \n" +
                            "Давайте посмотрим на его статистику последнего матча!\n" +
                            "Эта игра была " + winlose +
                            $"\nГерой: {((Champion)participant.ChampionId).Name()}\n" +
                            CreateStatsString(participant) + "\n\n" +
                            $"\n**Ждем следующий бой! Посмотрим, сможет ли лучше, чем сейчас?**",participant.Stats.Win);
                    }
                    user.lastMatch = result[0];
                    user.gameID = result[0].GameId;
                    long temp = GetWOWLevel(participant);
                    if(temp > user.lastGameScore) user.lastGameScore = GetWOWLevel(participant);
                        
                    }
                }
                catch
                {
                    continue;
                }
            }
        }

        public static long GetWOWLevel(MingweiSamuel.Camille.MatchV4.Participant participant)
        {
            long result = 0;
            if(((participant.Stats.Kills + participant.Stats.Assists) / ((float)participant.Stats.Deaths + 1)) < 1f)
            {
                result -= 100;
                if(((participant.Stats.Kills + participant.Stats.Assists) / ((float)participant.Stats.Deaths + 1)) < 0.5f)
                {
                    result -= 100;
                }
            }
            else
            {
                if (((participant.Stats.Kills + participant.Stats.Assists) / ((float)participant.Stats.Deaths + 1) < 0.5f))
                {
                    result += (long)((participant.Stats.Kills + participant.Stats.Assists) / ((float)participant.Stats.Deaths + 1) * 1000);
                }
            }
            result += participant.Stats.DoubleKills * 200;
            if (participant.Stats.FirstBloodKill) result += 40;
            if (participant.Stats.FirstTowerKill) result += 150;
            result += participant.Stats.DoubleKills * 500;
            result += participant.Stats.TripleKills * 100;
            result += participant.Stats.QuadraKills * 150;
            result += participant.Stats.UnrealKills * 200;
            result += participant.Stats.KillingSprees * 100;
            result -= participant.Stats.Deaths * 200;
            result += participant.Stats.NeutralMinionsKilled * 50;
            result += participant.Stats.Kills * 100;
            result += participant.Stats.Assists * 100;
            result += participant.Stats.TotalDamageDealt / 8000 * 50;
            return result;
        }

        public static string CreateStatsString(MingweiSamuel.Camille.MatchV4.Participant participant)
        {
            string result = "Результаты этого игрока в последнем матче:\n";
            result += $"Итоговый уровень героя: {participant.Stats.ChampLevel}\n";
            result += $"KDA: {participant.Stats.Kills} / {participant.Stats.Deaths} / {participant.Stats.Assists} ({((participant.Stats.Kills + participant.Stats.Assists) / ((float)participant.Stats.Deaths+1)).ToString("0.00")})\n";
            result += $"Золота заработано: {participant.Stats.GoldEarned}\n";
            result += $"Золота потрачено: {participant.Stats.GoldSpent}\n";
            result += $"Убито крипов: {participant.Stats.TotalMinionsKilled}\n";
            result += $"Нанес урона по чемпионам: {participant.Stats.TotalDamageDealtToChampions}\n\nДостижения:\n";

            
            if (participant.Stats.FirstBloodKill)
            {
                result += $"- Совершил первое убийство\n";
            }

            if (participant.Stats.FirstTowerKill)
            {
                result += $"- Первым уничтожил башню\n";
            }
            if (participant.Stats.DoubleKills > 0)
            {
                result += $"- Совершил двойных убийств: {participant.Stats.DoubleKills}\n";
            }
            if (participant.Stats.TripleKills > 0)
            {
                result += $"- Совершил трипплкиллов: {participant.Stats.TripleKills}\n";
            }
            if (participant.Stats.QuadraKills > 0)
            {
                result += $"- Совершил квадрокиллов: {participant.Stats.QuadraKills}\n";
            }
            if (participant.Stats.PentaKills > 0)
            {
                result += $"- Совершил пентакиллов: {participant.Stats.PentaKills}\n";
            }
            if (participant.Stats.UnrealKills > 0)
            {
                result += $"- Совершил эйсов: {participant.Stats.UnrealKills}\n";
            }
            return result;
        }

        public static async Task PrintBotStatus(DiscordChannel channel)
        {
            StringBuilder sbuilder = new StringBuilder();
            sbuilder.Append($"Зарегистрированно игроков: {registeredUsers.Count}\n");
            sbuilder.Append($"Самый сильный игрок: {registeredUsers.Where(u => u.lastGameScore == registeredUsers.Max(d => d.lastGameScore)).FirstOrDefault().summername}\n");
            sbuilder.Append($"Самый слабый игрок: {registeredUsers.Where(u => u.lastGameScore == registeredUsers.Min(d => d.lastGameScore)).FirstOrDefault().summername}\n");
            sbuilder.Append($"Последний зарегистрированный игрок: {registeredUsers[registeredUsers.Count-1].summername}\n");
            DiscordEmbedBuilder embed = new DiscordEmbedBuilder();
            embed.WithTitle("Текущий статистика на сервере");
            embed.WithDescription(sbuilder.ToString());
            embed.WithFooter("Данная статитика формируsется в BETA режиме");
            embed.WithColor(new DiscordColor(20, 200, 10));
            await channel.SendMessageAsync(null, false, embed.Build());
            return;
        }
    }
}
