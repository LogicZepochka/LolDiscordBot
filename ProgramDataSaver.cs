using LoLDiscover.DiscrodUserLOL;
using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace LoLDiscover
{
    public static class ProgramDataSaver
    {
        [Serializable]
        public class SaveData
        {
            public string lastSaveDataVersion;
            public LoLUser[] users;

            public SaveData(LoLUser[] user)
            {
                users = user;
            }
        }

        public class EventNewVersionLaunched
        {


            public string OldVersion;
            public string NewVersion;

            public EventNewVersionLaunched(string oldVersion, string newVersion)
            {
                OldVersion = oldVersion;
                NewVersion = newVersion;
            }
        }

        public static EventHandler<EventNewVersionLaunched> OnNewVersion;

        public static List<LoLUser> LoadSaveData()
        {
            if (!File.Exists("BotData.bot")) return null;
            FileStream file = new FileStream("BotData.bot", FileMode.Open);
            BinaryFormatter BF = new BinaryFormatter();
            List<LoLUser> result = new List<LoLUser>();
            try
            {
                SaveData data = (SaveData)BF.Deserialize(file);
                result.AddRange(data.users);
                if(data.lastSaveDataVersion != Assembly.GetExecutingAssembly().GetName().Version.ToString())
                {
                    var args = new EventNewVersionLaunched(data.lastSaveDataVersion, Assembly.GetExecutingAssembly().GetName().Version.ToString());
                    OnNewVersion?.Invoke(args, args);
                }
                file.Close();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                file.Close();
                return null;
            }
            return result;
        }

        public static bool SaveSaveData(LoLUser[] savedata)
        {
            FileStream file = new FileStream("BotData.bot", FileMode.Create);
            BinaryFormatter BF = new BinaryFormatter();
            SaveData data = new SaveData(savedata);
            data.lastSaveDataVersion = Assembly.GetExecutingAssembly().GetName().Version.ToString();
            try
            {
                BF.Serialize(file,data);
                file.Close();
            }
            catch(Exception ex)
            {
                Console.WriteLine(ex.Message);
                file.Close();
                return false;
            }
            Console.WriteLine("[Данные сохранены]");
            return true;
        }
    }
}
