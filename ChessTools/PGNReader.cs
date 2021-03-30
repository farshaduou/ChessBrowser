using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

namespace ChessTools
{
    /// <summary>
    /// PGNReader object works by being instantiated with a filename and parses that file into a chess games list
    /// </summary>
    public class PGNReader
    {
        public string PGNFileName;
        ChessGame game = new ChessGame();
        //List<ChessGame> ReadEvent;
        public PGNReader(string fName)
        {
            PGNFileName = fName;
        }

        /// <summary>
        /// this method looks into a line from PGN file to identify the field it corresponds to
        /// </summary>
        /// <param name="line"> a line from PGN file that needs to be parsed </param>
        /// <returns> returns a string indicating field name</returns>
        string getField(string line)
        {

            if (new Regex(@"\[Event ").IsMatch(line))
            {
                return "Event";
            }
            else if (new Regex(@"\[Site ").IsMatch(line))
            {
                return "Site";
            }
            else if (new Regex(@"\[Date ").IsMatch(line))
            {
                return "GameDate";
            }
            else if (new Regex(@"\[Round ").IsMatch(line))
            {
                return "Round";
            }
            else if (new Regex(@"\[White ").IsMatch(line))
            {
                return "WhitePlayerName";
            }
            else if (new Regex(@"\[Black ").IsMatch(line))
            {
                return "BlackPlayerName";
            }
            else if (new Regex(@"\[Result ").IsMatch(line))
            {
                return "Result";
            }
            else if (new Regex(@"\[WhiteElo ").IsMatch(line))
            {
                return "EloWhite";
            }
            else if (new Regex(@"\[BlackElo ").IsMatch(line))
            {
                return "EloBlack";
            }
            else if (new Regex(@"\[EventDate ").IsMatch(line))
            {
                return "EventDate";
            }
            else if (new Regex(@"^1\.").IsMatch(line))
            {
                return "Moves";
            }
            else
            {
                return "IrrelevantLine";
            }

        }

        /// <summary>
        ///  thie fucntion looks into a line from PGN file to find the value associated with the field specifid
        /// </summary>
        /// <param name="line"> a line from PGN file that needs to be parsed </param>
        /// <returns> method returns the extracted field value from the line </returns>
        string getValue(string line)
        {
            var valueRegex = new System.Text.RegularExpressions.Regex("\".*?\"");
            var value = valueRegex.Match(line).ToString();
            return value.Trim(new Char[] { '\"' });
        }

        /// <summary>
        /// chanes the PGN style reported result to W/B/D DBS style result
        /// </summary>
        /// <param name="value">value is the reported result string in the PGN </param>
        /// <returns> a charachter W, B or D as White win, Black win or Draw</returns>
        char resultToChar(string value)
        {
            if (value.Equals("1/2-1/2"))
                return 'D';
            else if (value.Equals("1-0"))
                return 'W';
            else if (value.Equals("0-1"))
                return 'B';
            else
            {
                // in case there is an error parsing results
                throw new NotImplementedException();
                return 'E';
            }
        }

        /// <summary>
        /// this method creates the list of games parsed from the PGN file 
        /// </summary>
        /// <returns> returns a list of ChessGame instances </returns>
        public List<ChessGame> parse()
        {
            List<ChessGame> gamesList = new List<ChessGame>();

            //opening the PGN file, reading it int othe tempGame ChessGame
            System.IO.StreamReader file = new System.IO.StreamReader(PGNFileName);
            string line;
            ChessGame tempGame = new ChessGame();
            bool newgameFlag = false;

            while ((line = file.ReadLine()) != null)
            {
                if (newgameFlag)
                {
                    tempGame = new ChessGame();
                    newgameFlag = false;
                }
                var field = getField(line);
                var value = getValue(line);

                switch (field)
                {
                    case "Event":
                        tempGame.Event = value;
                        break;
                    case "Site":
                        tempGame.Site = value;
                        break;
                    case "GameDate":
                        tempGame.GameDate = value;
                        break;
                    case "Round":
                        tempGame.Round = value;
                        break;
                    case "WhitePlayerName":
                        tempGame.WhitePlayerName = value;
                        break;
                    case "BlackPlayerName":
                        tempGame.BlackPlayerName = value;
                        break;
                    case "Result":
                        tempGame.Result = resultToChar(value); ;
                        break;
                    case "EloWhite":
                        tempGame.EloWhite = uint.Parse(value);
                        break;
                    case "EloBlack":
                        tempGame.EloBlack = uint.Parse(value);
                        break;
                    case "EventDate":
                        tempGame.EventDate = value;
                        break;
                    case "Moves":
                        tempGame.Moves = line;
                        // loop reads the following lines to add the rest of moves until find an empty new line
                        while (!String.Equals((line = file.ReadLine()), ""))
                        {
                            tempGame.Moves += line;
                        }
                        // game is recorded in the list after moves are added and flag is set so a new temp game is created
                        gamesList.Add(tempGame);
                        newgameFlag = true;

                        break;
                    default:
                        // for cases of spacing line,
                        // the cases that we don't need the value
                        // and other error lines we skip
                        break;
                }

    }


            //throw new NotImplementedException(); 
            return gamesList;
        }

    }

}
