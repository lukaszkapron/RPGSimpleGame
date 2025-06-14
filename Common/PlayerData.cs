using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Common
{
    public class PlayerData
    {
        public string Name { get; set; }
        public string Class { get; set; }
        public int Level { get; set; } = 1;
        public int MaxHp { get; set; }
        public int Hp { get; set; }
        public int Attack { get; set; }
        public int Defense { get; set; }
        public int Xp { get; set; }
        public bool IsTurn { get; set; } = false;
        public int Wins { get; set; } = 0;
        public int Loses { get; set; } = 0;
        public string CurrentServer { get; set; } = "Server1";
    }
}
