using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using DSharpPlus;
using DSharpPlus.Entities;
using MingweiSamuel.Camille;
using MingweiSamuel.Camille.Enums;
using MingweiSamuel.Camille.MatchV4;
using MingweiSamuel.Camille.SummonerV4;
using MingweiSamuel.Camille.SpectatorV4;
using System.Linq;

namespace LoLDiscover.DiscrodUserLOL
{
    [Serializable]
    public class LoLUser
    {
        public string summername;
        public string lolUsername;
        
        public string summonerID;
        [NonSerialized]
        public Summoner summonerData;
        [NonSerialized]
        public Match lastMatch;
        public long gameID;
        [NonSerialized]
        public DiscordUser discordUser;
        public ulong discordUserId;
        public long lastGameScore;
        public List<Achivement> Achivements;

        [NonSerialized]
        public GameRoom CurrentGameRoom;

        public LoLUser()
        {
            Achivements = new List<Achivement>();
        }

        public async Task LoadDiscordUser(DiscordClient loadby)
        {
            discordUser = await loadby.GetUserAsync(discordUserId);
            summonerData = await Program.riot.SummonerV4.GetBySummonerIdAsync(Region.RU, summonerID);
            CurrentGameRoom = null;
            return;
        }

        public async Task<CurrentGameParticipant[]> GetCurrentGameParticipant()
        {
            CurrentGameInfo gameinfo = await Program.riot.SpectatorV4.GetCurrentGameInfoBySummonerAsync(Region.RU, summonerID);
            if (gameinfo == null)
            {
                return null;
            }
            Console.Beep();
            Console.WriteLine($"[ИГРОК {summername} НАХОДИТСЯ В ИГРЕ С ДРУГИМИ ИГРОКАМИ]");
            var d = gameinfo.Participants;
            return d;
        }

        public async Task CreateAchivement(string title,string description,bool annonce = true)
        {
            if (Achivements.Where(d => d.AchivementName == title).FirstOrDefault() != null) return;
            Achivements.Add(new Achivement(title, description));
            if(annonce)
            {
                await Program.SendAchivementPermament(Program.eventsChannel, $"{discordUser.Username} получает достижение!",$"\n**{title}** ```{description}```");
            }
        }

        public static async Task CreateAchivement(DiscordUser user,string title, string description, bool annonce = true)
        {
            var loluser = GetUserByDiscordID(user.Id);
            if (loluser == null) return;
            await loluser.CreateAchivement(title, description, annonce);
            return;
        }

        public static LoLUser GetUserByDiscordID(ulong id)
        {
            return Program.registeredUsers.Where(d => d.discordUserId == id).FirstOrDefault();
        }
        
        public Achivement[] GetAchivements()
        {
            return Achivements.ToArray();
        }
    }
}
