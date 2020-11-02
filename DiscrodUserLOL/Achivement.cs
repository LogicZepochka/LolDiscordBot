using System;
using System.Collections.Generic;
using System.Text;

namespace LoLDiscover.DiscrodUserLOL
{
    [Serializable]
    public class Achivement
    {
        public Achivement(string achivementName, string description)
        {
            AchivementName = achivementName;
            Description = description;
            AchivementDate = DateTime.Now.ToString("dd.MM.yyyy");
        }

        public string AchivementName { get; }
        public string Description { get; }
        public string AchivementDate { get; }
    }
}
