using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Windows.Forms;


namespace ChessBrowser
{
    partial class Form1
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Keep track of which radio button is pressed
        /// </summary>
        private RadioButton winnerButton = null;

        /// <summary>
        /// This function handles the "Upload PGN" button.
        /// Given a filename, parses the PGN file, and uploads
        /// each chess game to the user's database.
        /// </summary>
        /// <param name="PGNfilename">The path to the PGN file</param>
        private void UploadGamesToDatabase(string PGNfilename)
        {
            // This will build a connection string to your user's database on atr,
            // assuimg you've typed a user and password in the GUI
            string connection = GetConnectionString();

            // TODO: Load and parse the PGN file
            //       We recommend creating separate libraries to represent chess data and load the file

            // Use this to tell the GUI's progress bar how many total work steps there are
            // For example, one iteration of your main upload loop could be one work step
            // SetNumWorkItems(...);
            ChessTools.PGNReader pgnreader = new ChessTools.PGNReader(PGNfilename);
            var gamelist = pgnreader.parse();
            SetNumWorkItems(gamelist.Count);

            using (MySqlConnection conn = new MySqlConnection(connection))
            {
                try
                {
                    // Open a connection
                    conn.Open();

                    // TODO: iterate through your data and generate appropriate insert commands

                    // Use this to tell the GUI that one work step has completed:
                    // WorkStepCompleted();

                    MySqlCommand command = conn.CreateCommand();

                    // Add Event
                    command.CommandText = "INSERT IGNORE INTO Events(Name,Site,Date) " +
                        "VALUES (@eventName,@site,@eventDate) ;";

                    // Add black player
                    command.CommandText += "INSERT IGNORE INTO Players(Name,Elo) " +
                        "VALUES (@bp,@bpelo)" +
                        " on duplicate key update Elo = IF(@bpelo>Elo, @bpelo, Elo);";

                    // Add white player
                    command.CommandText += "INSERT IGNORE INTO Players(Name,Elo) " +
                        "VALUES (@wp,@wpelo)" +
                        " on duplicate key update Elo = IF(@wpelo>Elo, @wpelo, Elo);";

                    // Add Game
                    command.CommandText += "INSERT IGNORE INTO " +
                        "Games(Round,Result,Moves,BlackPlayer,WhitePlayer,eID) " +
                        "VALUES(@round,@result,@moves," +
                        "(SELECT pID as BlackPlayer FROM Players where Name = @bp limit 1)," +
                        "(SELECT pID as WhitePlayer FROM Players where Name = @wp limit 1)," +
                        "(SELECT eID FROM Events WHERE Name = @eventName limit 1));";

                    command.Prepare();

                    foreach (ChessTools.ChessGame game in gamelist)
                    {
                        command.Parameters.AddWithValue("@round", game.Round);
                        command.Parameters.AddWithValue("@result", game.Result);
                        command.Parameters.AddWithValue("@moves", game.Moves);
                        command.Parameters.AddWithValue("@eventName", game.Event);
                        command.Parameters.AddWithValue("@Site", game.Site);
                        command.Parameters.AddWithValue("@eventDate", game.EventDate);
                        command.Parameters.AddWithValue("@bp", game.BlackPlayerName);
                        command.Parameters.AddWithValue("@wp", game.WhitePlayerName);
                        command.Parameters.AddWithValue("@bpelo", game.EloBlack);
                        command.Parameters.AddWithValue("@wpelo", game.EloWhite);

                        command.ExecuteNonQuery();

                        command.Parameters.Clear();

                        WorkStepCompleted();
                    }
                    conn.Close();
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.Message);
                }
            }

        }


        /// <summary>
        /// Queries the database for games that match all the given filters.
        /// The filters are taken from the various controls in the GUI.
        /// </summary>
        /// <param name="white">The white player, or "" if none</param>
        /// <param name="black">The black player, or "" if none</param>
        /// <param name="opening">The first move, e.g. "e4", or "" if none</param>
        /// <param name="winner">The winner as "White", "Black", "Draw", or "" if none</param>
        /// <param name="useDate">True if the filter includes a date range, False otherwise</param>
        /// <param name="start">The start of the date range</param>
        /// <param name="end">The end of the date range</param>
        /// <param name="showMoves">True if the returned data should include the PGN moves</param>
        /// <returns>A string separated by windows line endings ("\r\n") containing the filtered games</returns>
        private string PerformQuery(string white, string black, string opening,
          string winner, bool useDate, DateTime start, DateTime end, bool showMoves)
        {
            // This will build a connection string to your user's database on atr,
            // assuimg you've typed a user and password in the GUI
            string connection = GetConnectionString();

            // Build up this string containing the results from your query
            string parsedResult = "";

            // Use this to count the number of rows returned by your query
            // (see below return statement)
            int numRows = 0;

            using (MySqlConnection conn = new MySqlConnection(connection))
            {
                try
                {
                    // Open a connection
                    conn.Open();

                    // TODO: Generate and execute an SQL command,
                    //       then parse the results into an appropriate string
                    //       and return it.
                    //       Remember that the returned string must use \r\n newlines
                    MySqlCommand command = conn.CreateCommand();
                    command.CommandText = "select " +
                        "e.Name as EventName, e.Site, e.Date,wp.Name as WPName, wp.Elo as WPElo,bp.Name as BPName, bp.Elo as BPElo, g.Result, Moves " +
                        "from Games g inner join Players wp on g.WhitePlayer = wp.pID " +
                        "inner join Players bp on g.BlackPlayer = bp.pID " +
                        "inner join Events e on e.eID = g.eID " +
                        "where 1=1 ";

                    if (!String.IsNullOrEmpty(white))
                    {
                        command.CommandText += "and wp.Name = @white ";
                    }
                    if (!String.IsNullOrEmpty(black))
                    {
                        command.CommandText += "and bp.Name = @black ";
                    }
                    if (!String.IsNullOrEmpty(winner))
                    {
                        command.CommandText += "and g.Result = @winner ";
                        command.Parameters.AddWithValue("@winner", winner[0]);
                    }
                    if (useDate)
                    {
                        command.CommandText += "and (e.Date BETWEEN @start AND @end) ";
                    }
                    ;
                    if (!String.IsNullOrEmpty(opening))
                    {
                        command.CommandText += "and Moves like @openingPattern";
                    }
                    command.CommandText += ";";

                    command.Parameters.AddWithValue("@white", white);
                    command.Parameters.AddWithValue("@black", black);
                    command.Parameters.AddWithValue("@start", startDate.Value.Date.ToString("yyyy-MM-dd"));
                    command.Parameters.AddWithValue("@end", endDate.Value.Date.ToString("yyyy-MM-dd"));
                    command.Parameters.AddWithValue("@openingPattern", "1."+opening+"%");

                    using (MySqlDataReader reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            parsedResult +=
                                "\r\nEvent:  " + reader["EventName"] +
                                "\r\nSite:   " + reader["Site"] +
                                "\r\nDate:   " + reader["Date"] +
                                "\r\nWhite:  " + reader["WPName"] + " (" + reader["WPElo"] + ")" +
                                "\r\nBlack:  " + reader["BPName"] + " (" + reader["BPElo"] + ")" +
                                "\r\nResult: " + reader["Result"] +
                                "\r\n";

                            if (showMoves)
                            {
                                parsedResult += "\r\n" + reader["Moves"] + "\r\n";
                            }

                            numRows++;
                        }
                    }

                }
                catch (Exception e)
                {
                    Console.WriteLine(e.Message);
                }
            }

            return numRows + " results\r\n\r\n" + parsedResult;
        }


        /// <summary>
        /// Informs the progress bar that one step of work has been completed.
        /// Use SetNumWorkItems first
        /// </summary>
        private void WorkStepCompleted()
        {
            backgroundWorker1.ReportProgress(0);
        }

        /// <summary>
        /// Informs the progress bar how many steps of work there are.
        /// </summary>
        /// <param name="x">The number of work steps</param>
        private void SetNumWorkItems(int x)
        {
            this.Invoke(new MethodInvoker(() =>
              {
                  uploadProgress.Maximum = x;
                  uploadProgress.Step = 1;
              }));
        }

        /// <summary>
        /// Reads the username and password from the text fields in the GUI
        /// and puts them into an SQL connection string for the atr server.
        /// </summary>
        /// <returns></returns>
        private string GetConnectionString()
        {
            return "server=atr.eng.utah.edu;database=" + userText.Text + ";uid=" + userText.Text + ";password=" + pwdText.Text;  
        }


        /***************GUI functions below this point***************/
        /*You should not need to directly use any of these functions*/

        /// <summary>
        /// Disables the two buttons on the GUI,
        /// so that only one task can happen at once
        /// </summary>
        private void DisableControls()
        {
            uploadButton.Enabled = false;
            goButton.Enabled = false;
        }

        /// <summary>
        /// Enables the two buttons on the GUI, used after a task completes.
        /// </summary>
        private void EnableControls()
        {
            uploadButton.Enabled = true;
            goButton.Enabled = true;
        }


        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.userText = new System.Windows.Forms.TextBox();
            this.pwdText = new System.Windows.Forms.TextBox();
            this.label1 = new System.Windows.Forms.Label();
            this.label2 = new System.Windows.Forms.Label();
            this.uploadButton = new System.Windows.Forms.Button();
            this.backgroundWorker1 = new System.ComponentModel.BackgroundWorker();
            this.uploadProgress = new System.Windows.Forms.ProgressBar();
            this.label3 = new System.Windows.Forms.Label();
            this.label4 = new System.Windows.Forms.Label();
            this.whitePlayerText = new System.Windows.Forms.TextBox();
            this.blackPlayerText = new System.Windows.Forms.TextBox();
            this.startDate = new System.Windows.Forms.DateTimePicker();
            this.label6 = new System.Windows.Forms.Label();
            this.endDate = new System.Windows.Forms.DateTimePicker();
            this.dateCheckBox = new System.Windows.Forms.CheckBox();
            this.whiteWin = new System.Windows.Forms.RadioButton();
            this.label5 = new System.Windows.Forms.Label();
            this.blackWin = new System.Windows.Forms.RadioButton();
            this.drawWin = new System.Windows.Forms.RadioButton();
            this.label7 = new System.Windows.Forms.Label();
            this.label8 = new System.Windows.Forms.Label();
            this.openingMoveText = new System.Windows.Forms.TextBox();
            this.resultText = new System.Windows.Forms.TextBox();
            this.showMovesCheckBox = new System.Windows.Forms.CheckBox();
            this.goButton = new System.Windows.Forms.Button();
            this.anyRadioButton = new System.Windows.Forms.RadioButton();
            this.SuspendLayout();
            // 
            // userText
            // 
            this.userText.Location = new System.Drawing.Point(41, 8);
            this.userText.Margin = new System.Windows.Forms.Padding(2);
            this.userText.Name = "userText";
            this.userText.Size = new System.Drawing.Size(96, 20);
            this.userText.TabIndex = 0;
            // 
            // pwdText
            // 
            this.pwdText.Location = new System.Drawing.Point(227, 8);
            this.pwdText.Margin = new System.Windows.Forms.Padding(2);
            this.pwdText.Name = "pwdText";
            this.pwdText.PasswordChar = '*';
            this.pwdText.Size = new System.Drawing.Size(95, 20);
            this.pwdText.TabIndex = 1;
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(9, 8);
            this.label1.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(29, 13);
            this.label1.TabIndex = 2;
            this.label1.Text = "User";
            this.label1.Click += new System.EventHandler(this.label1_Click);
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(171, 8);
            this.label2.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(53, 13);
            this.label2.TabIndex = 3;
            this.label2.Text = "Password";
            // 
            // uploadButton
            // 
            this.uploadButton.Location = new System.Drawing.Point(41, 53);
            this.uploadButton.Margin = new System.Windows.Forms.Padding(2);
            this.uploadButton.Name = "uploadButton";
            this.uploadButton.Size = new System.Drawing.Size(99, 32);
            this.uploadButton.TabIndex = 4;
            this.uploadButton.Text = "Upload PGN";
            this.uploadButton.UseVisualStyleBackColor = true;
            this.uploadButton.Click += new System.EventHandler(this.button1_Click);
            // 
            // backgroundWorker1
            // 
            this.backgroundWorker1.WorkerReportsProgress = true;
            this.backgroundWorker1.DoWork += new System.ComponentModel.DoWorkEventHandler(this.backgroundWorker1_DoWork);
            this.backgroundWorker1.ProgressChanged += new System.ComponentModel.ProgressChangedEventHandler(this.backgroundWorker1_ProgressChanged);
            this.backgroundWorker1.RunWorkerCompleted += new System.ComponentModel.RunWorkerCompletedEventHandler(this.backgroundWorker1_RunWorkerCompleted);
            // 
            // uploadProgress
            // 
            this.uploadProgress.Location = new System.Drawing.Point(174, 53);
            this.uploadProgress.Margin = new System.Windows.Forms.Padding(2);
            this.uploadProgress.Name = "uploadProgress";
            this.uploadProgress.RightToLeft = System.Windows.Forms.RightToLeft.No;
            this.uploadProgress.Size = new System.Drawing.Size(600, 32);
            this.uploadProgress.TabIndex = 5;
            this.uploadProgress.Click += new System.EventHandler(this.uploadProgress_Click);
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(11, 153);
            this.label3.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(67, 13);
            this.label3.TabIndex = 6;
            this.label3.Text = "White Player";
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.Location = new System.Drawing.Point(193, 153);
            this.label4.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(66, 13);
            this.label4.TabIndex = 7;
            this.label4.Text = "Black Player";
            // 
            // whitePlayerText
            // 
            this.whitePlayerText.Location = new System.Drawing.Point(77, 153);
            this.whitePlayerText.Margin = new System.Windows.Forms.Padding(2);
            this.whitePlayerText.Name = "whitePlayerText";
            this.whitePlayerText.Size = new System.Drawing.Size(98, 20);
            this.whitePlayerText.TabIndex = 8;
            // 
            // blackPlayerText
            // 
            this.blackPlayerText.Location = new System.Drawing.Point(258, 153);
            this.blackPlayerText.Margin = new System.Windows.Forms.Padding(2);
            this.blackPlayerText.Name = "blackPlayerText";
            this.blackPlayerText.Size = new System.Drawing.Size(98, 20);
            this.blackPlayerText.TabIndex = 9;
            // 
            // startDate
            // 
            this.startDate.Enabled = false;
            this.startDate.Location = new System.Drawing.Point(107, 207);
            this.startDate.Margin = new System.Windows.Forms.Padding(2);
            this.startDate.Name = "startDate";
            this.startDate.Size = new System.Drawing.Size(201, 20);
            this.startDate.TabIndex = 11;
            // 
            // label6
            // 
            this.label6.AutoSize = true;
            this.label6.Location = new System.Drawing.Point(312, 211);
            this.label6.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
            this.label6.Name = "label6";
            this.label6.Size = new System.Drawing.Size(43, 13);
            this.label6.TabIndex = 12;
            this.label6.Text = "through";
            // 
            // endDate
            // 
            this.endDate.Enabled = false;
            this.endDate.Location = new System.Drawing.Point(359, 208);
            this.endDate.Margin = new System.Windows.Forms.Padding(2);
            this.endDate.Name = "endDate";
            this.endDate.Size = new System.Drawing.Size(201, 20);
            this.endDate.TabIndex = 13;
            // 
            // dateCheckBox
            // 
            this.dateCheckBox.AutoSize = true;
            this.dateCheckBox.Location = new System.Drawing.Point(14, 208);
            this.dateCheckBox.Margin = new System.Windows.Forms.Padding(2);
            this.dateCheckBox.Name = "dateCheckBox";
            this.dateCheckBox.Size = new System.Drawing.Size(89, 17);
            this.dateCheckBox.TabIndex = 14;
            this.dateCheckBox.Text = "Filter By Date";
            this.dateCheckBox.UseVisualStyleBackColor = true;
            this.dateCheckBox.CheckedChanged += new System.EventHandler(this.dateCheckBox_CheckedChanged);
            // 
            // whiteWin
            // 
            this.whiteWin.AutoSize = true;
            this.whiteWin.Location = new System.Drawing.Point(582, 154);
            this.whiteWin.Margin = new System.Windows.Forms.Padding(2);
            this.whiteWin.Name = "whiteWin";
            this.whiteWin.Size = new System.Drawing.Size(53, 17);
            this.whiteWin.TabIndex = 15;
            this.whiteWin.TabStop = true;
            this.whiteWin.Text = "White";
            this.whiteWin.UseVisualStyleBackColor = true;
            this.whiteWin.CheckedChanged += new System.EventHandler(this.whiteWin_CheckedChanged);
            // 
            // label5
            // 
            this.label5.AutoSize = true;
            this.label5.Location = new System.Drawing.Point(534, 155);
            this.label5.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
            this.label5.Name = "label5";
            this.label5.Size = new System.Drawing.Size(41, 13);
            this.label5.TabIndex = 16;
            this.label5.Text = "Winner";
            // 
            // blackWin
            // 
            this.blackWin.AutoSize = true;
            this.blackWin.Location = new System.Drawing.Point(636, 154);
            this.blackWin.Margin = new System.Windows.Forms.Padding(2);
            this.blackWin.Name = "blackWin";
            this.blackWin.Size = new System.Drawing.Size(52, 17);
            this.blackWin.TabIndex = 17;
            this.blackWin.TabStop = true;
            this.blackWin.Text = "Black";
            this.blackWin.UseVisualStyleBackColor = true;
            this.blackWin.CheckedChanged += new System.EventHandler(this.blackWin_CheckedChanged);
            // 
            // drawWin
            // 
            this.drawWin.AutoSize = true;
            this.drawWin.Location = new System.Drawing.Point(689, 154);
            this.drawWin.Margin = new System.Windows.Forms.Padding(2);
            this.drawWin.Name = "drawWin";
            this.drawWin.Size = new System.Drawing.Size(50, 17);
            this.drawWin.TabIndex = 18;
            this.drawWin.TabStop = true;
            this.drawWin.Text = "Draw";
            this.drawWin.UseVisualStyleBackColor = true;
            this.drawWin.CheckedChanged += new System.EventHandler(this.drawWin_CheckedChanged);
            // 
            // label7
            // 
            this.label7.AutoSize = true;
            this.label7.Location = new System.Drawing.Point(11, 117);
            this.label7.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
            this.label7.Name = "label7";
            this.label7.Size = new System.Drawing.Size(112, 13);
            this.label7.TabIndex = 19;
            this.label7.Text = "Find games filtered by:";
            // 
            // label8
            // 
            this.label8.AutoSize = true;
            this.label8.Location = new System.Drawing.Point(373, 153);
            this.label8.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
            this.label8.Name = "label8";
            this.label8.Size = new System.Drawing.Size(77, 13);
            this.label8.TabIndex = 20;
            this.label8.Text = "Opening Move";
            // 
            // openingMoveText
            // 
            this.openingMoveText.Location = new System.Drawing.Point(451, 153);
            this.openingMoveText.Margin = new System.Windows.Forms.Padding(2);
            this.openingMoveText.Name = "openingMoveText";
            this.openingMoveText.Size = new System.Drawing.Size(68, 20);
            this.openingMoveText.TabIndex = 21;
            // 
            // resultText
            // 
            this.resultText.Location = new System.Drawing.Point(11, 290);
            this.resultText.Margin = new System.Windows.Forms.Padding(2);
            this.resultText.Multiline = true;
            this.resultText.Name = "resultText";
            this.resultText.ReadOnly = true;
            this.resultText.ScrollBars = System.Windows.Forms.ScrollBars.Both;
            this.resultText.Size = new System.Drawing.Size(764, 426);
            this.resultText.TabIndex = 22;
            // 
            // showMovesCheckBox
            // 
            this.showMovesCheckBox.AutoSize = true;
            this.showMovesCheckBox.Location = new System.Drawing.Point(11, 252);
            this.showMovesCheckBox.Margin = new System.Windows.Forms.Padding(2);
            this.showMovesCheckBox.Name = "showMovesCheckBox";
            this.showMovesCheckBox.Size = new System.Drawing.Size(88, 17);
            this.showMovesCheckBox.TabIndex = 23;
            this.showMovesCheckBox.Text = "Show Moves";
            this.showMovesCheckBox.UseVisualStyleBackColor = true;
            // 
            // goButton
            // 
            this.goButton.BackColor = System.Drawing.Color.Silver;
            this.goButton.Location = new System.Drawing.Point(99, 245);
            this.goButton.Margin = new System.Windows.Forms.Padding(2);
            this.goButton.Name = "goButton";
            this.goButton.Size = new System.Drawing.Size(89, 27);
            this.goButton.TabIndex = 24;
            this.goButton.Text = "Go!";
            this.goButton.UseVisualStyleBackColor = false;
            this.goButton.Click += new System.EventHandler(this.button1_Click_1);
            // 
            // anyRadioButton
            // 
            this.anyRadioButton.AutoSize = true;
            this.anyRadioButton.Checked = true;
            this.anyRadioButton.Location = new System.Drawing.Point(740, 154);
            this.anyRadioButton.Margin = new System.Windows.Forms.Padding(2);
            this.anyRadioButton.Name = "anyRadioButton";
            this.anyRadioButton.Size = new System.Drawing.Size(42, 17);
            this.anyRadioButton.TabIndex = 25;
            this.anyRadioButton.TabStop = true;
            this.anyRadioButton.Text = "any";
            this.anyRadioButton.UseVisualStyleBackColor = true;
            this.anyRadioButton.CheckedChanged += new System.EventHandler(this.radioButton1_CheckedChanged);
            // 
            // Form1
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(790, 690);
            this.Controls.Add(this.anyRadioButton);
            this.Controls.Add(this.goButton);
            this.Controls.Add(this.showMovesCheckBox);
            this.Controls.Add(this.resultText);
            this.Controls.Add(this.openingMoveText);
            this.Controls.Add(this.label8);
            this.Controls.Add(this.label7);
            this.Controls.Add(this.drawWin);
            this.Controls.Add(this.blackWin);
            this.Controls.Add(this.label5);
            this.Controls.Add(this.whiteWin);
            this.Controls.Add(this.dateCheckBox);
            this.Controls.Add(this.endDate);
            this.Controls.Add(this.label6);
            this.Controls.Add(this.startDate);
            this.Controls.Add(this.blackPlayerText);
            this.Controls.Add(this.whitePlayerText);
            this.Controls.Add(this.label4);
            this.Controls.Add(this.label3);
            this.Controls.Add(this.uploadProgress);
            this.Controls.Add(this.uploadButton);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.pwdText);
            this.Controls.Add(this.userText);
            this.Margin = new System.Windows.Forms.Padding(2);
            this.Name = "Form1";
            this.Text = "  ";
            this.Load += new System.EventHandler(this.Form1_Load);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.TextBox userText;
        private System.Windows.Forms.TextBox pwdText;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.Button uploadButton;
        private System.ComponentModel.BackgroundWorker backgroundWorker1;
        private System.Windows.Forms.ProgressBar uploadProgress;
        private Label label3;
        private Label label4;
        private TextBox whitePlayerText;
        private TextBox blackPlayerText;
        private DateTimePicker startDate;
        private Label label6;
        private DateTimePicker endDate;
        private CheckBox dateCheckBox;
        private RadioButton whiteWin;
        private Label label5;
        private RadioButton blackWin;
        private RadioButton drawWin;
        private Label label7;
        private Label label8;
        private TextBox openingMoveText;
        private TextBox resultText;
        private CheckBox showMovesCheckBox;
        private Button goButton;
        private RadioButton anyRadioButton;
    }
}

