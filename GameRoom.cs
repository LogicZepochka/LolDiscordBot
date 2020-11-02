using DSharpPlus;
using DSharpPlus.Entities;
using LoLDiscover.DiscrodUserLOL;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace LoLDiscover
{
    public class GameRoom: IDisposable
    {
        private List<LoLUser> users = new List<LoLUser>();
        private DiscordChannel ownedChannel;

        public async Task EndGame()
        {
            foreach(var user in users)
            {
                user.CurrentGameRoom = null;
                await Program.waitingRoom.PlaceMemberAsync(await Program.waitingRoom.Guild.GetMemberAsync(user.discordUserId));
            }
            await ownedChannel.DeleteAsync("Игра завершена");
            Program.gameRooms.Remove(this);
            Console.WriteLine("Игра завершена");
            return;
        }

        public void AddUserToRoom(LoLUser user)
        {
            if (!users.Contains(user))
            {
                users.Add(user);
                user.CurrentGameRoom = this;
            }
        }

        public bool IsAlreadyInGameRoom(LoLUser user)
        {
            return (user.CurrentGameRoom != null && user.CurrentGameRoom != this);
        }

        public bool isRoomActive()
        {
            return (users.Count >= 1);
        }

        public LoLUser[] GetPlayers()
        {
            return users.ToArray();
        }

        public void AppendChannel(DiscordChannel chanel)
        {
            ownedChannel = chanel;
        }

        public DiscordChannel GetOwnedChanel()
        {
            return ownedChannel;
        }

        public int GetTeamCount()
        {
            return users.Count;
        }

        public void Dispose()
        {
            Console.WriteLine("Комната игры была удалена");
        }
    }
}
