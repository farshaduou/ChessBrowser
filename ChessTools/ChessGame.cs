using System;

namespace ChessTools
{
    /// <summary>
    /// ChessGame class is used to model a game as an object that stroes information of that game
    /// Note: I decided to use string for dates because I can use string to communicate to the dbs as well
    /// </summary>
    public class ChessGame
    {
        public string Event;
        public string Site;
        public string Round;
        //public DateTime GameDate;
        public string GameDate;
        public string WhitePlayerName;
        public string BlackPlayerName;
        public char Result;
        public uint EloWhite;
        public uint EloBlack;
        //public DateTime EventDate;
        public string EventDate;
        public string Moves;

    }

}
