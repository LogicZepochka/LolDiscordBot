using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using LoLDiscover.DiscrodUserLOL;
using LoLDiscover.Fun;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LoLDiscover
{
    public class CommandsClass: BaseCommandModule
    {
        [Command("reg")]
        public async Task Reg(CommandContext ctx, [RemainingText] string loluser)
        {
            if (Program.registeredUsers.Count(u => u.summername.ToLower() == loluser.ToLower()) > 0)
            {
                await Program.SendError(ctx.Channel, "Такой пользователь уже зарегистрирован", $"Пользователь под ником **{loluser}** уже был зарегистрирован!");
                return;
            }
            if (Program.registeredUsers.Where(u => u.discordUserId == ctx.Client.CurrentUser.Id).FirstOrDefault() != null)
            {
                await Program.SendError(ctx.Channel, "Вы уже зарегистрированы", $"Вы уже зарегистрировали аккаунт Riot под именем  **{Program.registeredUsers.Where(u => u.discordUserId == ctx.Client.CurrentUser.Id).FirstOrDefault().summername}**.\nЧтобы зарегистрировать другой аккаунт, введите сначало ``&unregister``");
                return;
            }
            var newuser = await Program.CreateNewLolUser(ctx.Client.CurrentUser, loluser);
            if (newuser == null)
            {
                await Program.SendError(ctx.Channel, "Пользователь Riot не найден", $"Пользователь под ником **{loluser}** не найден.\nВы правильно всё ввели?");
                return;
            }
            await Program.SendDone(ctx.Channel, "Пользователь зарегистрирован!", $"Пользователь под ником **{loluser}** зарегистрирован.\n Теперь все узнают о Ваших достижениях :)");
            await newuser.CreateAchivement("Добро пожаловать!", "Зарегистрировал свой Riot аккаунт в базе данных бота.");
            await (await ctx.Guild.GetMemberAsync(ctx.Client.CurrentUser.Id)).GrantRoleAsync(ctx.Guild.GetRole(769723028493565993));
            return;
        }

        [Command("changelog")]
        public async Task ChangeLog(CommandContext ctx)
        {
            await Program.ShowChangeLog(ctx.Channel);
            return;
        }

        [Command("unreg")]
        public async Task Unreg(CommandContext ctx)
        {
            if (Program.registeredUsers.Where(u => u.discordUserId == ctx.Client.CurrentUser.Id).FirstOrDefault() == null)
            {
                await Program.SendError(ctx.Channel, "Вы незарегистрированы!", $"Вы еще не регистрировали аккаунт Riot!\nВведие ``&reg [Имя аккаунта Riot]`` для регистрации");
                return;
            }
            await ctx.RespondAsync("**Вы уверены?**\n*Введите ``Да`` чтобы подтвердить удаление*");
            var answer = await Program.interactivity.WaitForMessageAsync(xm => xm.Author.Id == ctx.User.Id && xm.Content.ToLower() == "да", TimeSpan.FromMinutes(1));
            if (answer.TimedOut != true)
            {
                Program.registeredUsers.Remove(Program.registeredUsers.Where(u => u.discordUserId == ctx.Client.CurrentUser.Id).FirstOrDefault());
                await Program.SendDone(ctx.Channel, "Аккаунт Riot удалён!", $"**Вы удалили свой аккаунт Riot!**\n*Я больше не стану отслеживать Ваши успехи и автоматически составлять группы.*");
                await (await ctx.Guild.GetMemberAsync(ctx.Client.CurrentUser.Id)).RevokeRoleAsync(ctx.Guild.GetRole(769723028493565993));
            }
            return;
        }

        [Command("speak")]
        public async Task ChangeLog(CommandContext ctx,[RemainingText] string message)
        {
            await ctx.Channel.DeleteMessageAsync(ctx.Message);
            if (ctx.Member.VoiceState == null)
            {
                await Program.SendError(ctx.Channel, "Вы должны быть в голосовом канале!", "");
                return;
            }
            await BotVoiceController.Speak(ctx.Member.VoiceState.Channel, message);
            await LoLUser.CreateAchivement(ctx.User, "Поговори со мной", "Запустил команду ``&speak`` и заговорил с ботом.");
            return;
        }

        [Command("stats")]
        public async Task Stats(CommandContext ctx)
        {
            await Program.PrintBotStatus(ctx.Channel);
            return;
        }

        [Command("clr")]
        public async Task Clear(CommandContext ctx)
        {
                var mess = await ctx.Channel.GetMessagesAsync();
                if (mess.Count <= 0) return;
                await ctx.Channel.DeleteMessagesAsync(mess);
                await Program.SendDone(ctx.Channel, "Данный канал очищен!", $"Все сообщения были удалены.\nТеперь тут чище, чем в операционной больницы :)");
                return;
        }

        [Command("achivement")]
        public async Task Achivement(CommandContext ctx,string title,[RemainingText] string description)
        {
            var titlenew = title.Replace('_', ' ');
            await Program.SendAchivement(ctx.Channel, $"{ctx.User.Username} получил достижение!", $"\n**{titlenew}**\n```{description}```");

            await LoLUser.CreateAchivement(ctx.User, "Мамин шутник", "Создал ненастоящее достижение");
            return;
        }

        [Command("showa")]
        public async Task ShowAllAchivements(CommandContext ctx)
        {
            var loluser = Program.registeredUsers.Where(u => u.discordUserId == ctx.User.Id).FirstOrDefault();
            if (loluser == null)
            {
                await Program.SendError(ctx.Channel, "Вы незарегистрированы!", $"Вы еще не регистрировали аккаунт Riot и не можете просмотреть свои достижения на сервере!\nВведие ``&register [Имя аккаунта Riot]`` для регистрации");
                return;
            }
            DiscordEmbedBuilder embed = new DiscordEmbedBuilder();
            embed.WithTitle($"Достижения пользователя {ctx.User.Username}");
            string mes = "";
            foreach (var achive in loluser.GetAchivements())
            {
                mes += $"- {achive.AchivementName} ({achive.AchivementDate})\n```{achive.Description}```\n\n";
            }
            embed.WithDescription(mes);
            embed.WithColor(new DiscordColor(120, 160, 0));
            var mess = await ctx.Channel.SendMessageAsync(embed: embed.Build());
            await Task.Delay(60000);
            await ctx.Channel.DeleteMessageAsync(mess);
            return;
        }
    }
}
