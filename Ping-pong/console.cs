using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Forms;
using mySockets;

namespace Ping_pong
{
    public partial class menu : Form
    {
        public menu()
        {
            InitializeComponent();
            console = new Console(txtLog);
        }

        Console console;

        public class Console
        {
            RichTextBox log;

            public string Do(string text)
            {
                text = text.Trim().ToLower();
                string[] splitted = text.Split(' ');
                string command = splitted[0];

                string output = "ERROR: Command '" + text + "' wasn't found. If you need help, write 'help' for help.";

                switch (command)
                {
                    case "": 
                        output = "ERROR: You've written empty command. You are piece of shit, don't be so stupid!"; 
                        break;

                    case "bot":
                        int difficulty;
                        if (splitted.Length > 1) //если есть параметры
                        {
                            bool result = int.TryParse(splitted[1], out difficulty);
                            if (result)
                            {
                                if (difficulty > 11) {
                                    difficulty = 11;
                                }
                                else if (difficulty < 1) {
                                    difficulty = 1;
                                }

                                dataTransfer.aiDifficulty = difficulty;
                                output = "WOOPWOOP: Game vs bot was successfully created at " + DateTime.Now.ToShortTimeString() + "!";
                                Run(0);
                            }
                            else
                            {
                                output = "ERROR: Parameter should be a number, not string";
                            }
                        }
                        else
                        {
                            output = "ERROR: Can't find parameter 'difficulty'.";
                        }

                        break;

                    case "set":
                        if (splitted.Length > 1)
                        {
                            string subcommand = splitted[1];

                            switch (subcommand)
                            {
                                case "speed":
                                    int speed;
                                    if (splitted.Length > 2) //если есть параметры
                                    {
                                        bool result = int.TryParse(splitted[2], out speed);
                                        if (result)
                                        {
                                            Settings.Rackets.speed = speed;
                                            output = "WOOPWOOP: speed has been changed to " + Settings.Rackets.speed + "!";
                                        }
                                        else
                                        {
                                            output = "ERROR: Parameter should be a number, not string";
                                        }
                                    }
                                    else
                                    {
                                        output = "ERROR: Can't find parameter 'speed'.";
                                    }

                                    break;

                                case "width":
                                    int width;
                                    if (splitted.Length > 2) //если есть параметры
                                    {
                                        bool result = int.TryParse(splitted[2], out width);
                                        if (result)
                                        {
                                            Settings.Window.width = width;
                                            output = "WOOPWOOP: width has been changed to " + Settings.Window.width + "!";
                                        }
                                        else
                                        {
                                            output = "ERROR: Parameter should be a number, not string";
                                        }
                                    }
                                    else
                                    {
                                        output = "ERROR: Can't find parameter 'width'.";
                                    }

                                    break;

                                case "height":
                                    int height;
                                    if (splitted.Length > 2) //если есть параметры
                                    {
                                        bool result = int.TryParse(splitted[2], out height);
                                        if (result)
                                        {
                                            Settings.Window.height = height;
                                            output = "WOOPWOOP: height has been changed to " + Settings.Window.height + "!";
                                        }
                                        else
                                        {
                                            output = "ERROR: Parameter should be a number, not string";
                                        }
                                    }
                                    else
                                    {
                                        output = "ERROR: Can't find parameter 'height'.";
                                    }

                                    break;

                            }
                        }
                        else
                        {
                            output = "ERROR: Can't find parameter";
                        }

                        break;

                    case "hotseat":
                        Run(1);
                        output = "WOOPWOOP: Hotseat game was successfully created at " + DateTime.Now.ToShortTimeString() + "!";
                        break;
                    
                    case "host":
                        Write(">Waiting for client...");
                        log.FindForm().Refresh();

                        dataTransfer.net = new net();
                        dataTransfer.net.server();
                        dataTransfer.typeOfNet = "server";

                        output = "WOOPWOOP: Net game was successfully created at " + DateTime.Now.ToShortTimeString() + "!";
                        Run(2);
                        break;

                    case "connect":
                        string ip = "127.0.0.1";
                        if (splitted.Length > 1) //если есть параметры
                        {
                            ip = splitted[1];
                            Write(">Trying to connect...");
                            log.FindForm().Refresh();

                            bool answer = false;
                            try
                            {
                                dataTransfer.net = new net(ip);
                                
                                answer = dataTransfer.net.client();
                            }
                            catch {
                                output = "ERROR: Can't connect to the server. Check if IP is correct."; 
                            }

                            if (answer)
                            {
                                dataTransfer.typeOfNet = "client";
                                Run(2);
                                output = "WOOPWOOP: You successfully connected to " + ip + " at " + DateTime.Now.ToShortTimeString() + "!";
                            }
                            else
                            {
                                output = "ERROR: Can't connect to the server. Check if IP is correct."; 
                            }

                        }
                        else
                            output = "ERROR: Can't find parameter 'ip'.";
                        
                        break;

                    case "help":
                        output = "Here is list of available commands: " + Environment.NewLine;
                        output += "— bot [difficulty]" + Environment.NewLine;
                        output += "         creates a game vs bot. Argument can vary from 1 to 11." + Environment.NewLine;
                        output += "— hotseat" + Environment.NewLine;
                        output += "         creates a game vs another player. Left racket is controlled with" + Environment.NewLine;
                        output += "         \"w\" and \"s\" keys, right with \"up\" and \"down\"." + Environment.NewLine;
                        output += "— host" + Environment.NewLine;
                        output += "         creates a server which client can connect to. Use it for " + Environment.NewLine;
                        output += "         games over Internet." + Environment.NewLine;
                        output += "— connect [ip]" + Environment.NewLine;
                        output += "         connects you to hosted game." + Environment.NewLine;
                        output += "— quit" + Environment.NewLine;
                        output += "         closes Ping-Pong.";

                        break;

                    case "quit": 
                        Application.Exit(); 
                        break;
                }

                return ">" + output;
            }

            public Console(RichTextBox history)
            {
                this.log = history;
            }

            private void Run(int gameType)
            {
                dataTransfer.gameType = gameType;
                Form1 myForm = new Form1();
                myForm.Show();
                Program.myMenu.Hide();
            }

            private void ChangeLastLine(string newText)
            {
                if (log.Lines.Length > 0) {
                    string[] temp = log.Lines;
                    temp[temp.Length - 1] = newText;
                    log.Lines = temp;
                }
            }

            private void Write(string text)
            {
                if (log.Text != "")
                    log.AppendText(Environment.NewLine + text);
                else
                    log.AppendText(text);
            }
        }

        private void txtConsole_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                if (txtLog.Text != "")
                    txtLog.AppendText(Environment.NewLine + console.Do(txtConsole.Text));
                else
                    txtLog.AppendText(console.Do(txtConsole.Text));
                
                //костыль
                txtLog.Select(txtLog.TextLength, 1);
                txtLog.ScrollToCaret();

                txtConsole.Clear();
            }
        }

        private void txtHistory_Enter(object sender, EventArgs e)
        {
            txtConsole.Focus();
        }

    }
}
