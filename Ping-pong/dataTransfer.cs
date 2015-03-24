using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using mySockets;

namespace Ping_pong
{
    static public class dataTransfer
    {
        static public net net = null;
        static public string typeOfNet; //"server" or "client"

        static public int aiDifficulty;
        static public int gameType; // 0 — vs bot
                                    // 1 — hotseat 
                                    // 2 — over Internet 

        static public bonus lastBonus;

        static public bool goal = false;
    }
}
