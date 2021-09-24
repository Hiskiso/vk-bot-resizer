using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Threading;
using VkNet;

using System.Configuration;
using System.Collections.Specialized;
using System.Net;
using System.IO;

namespace VK_longpoll_bot_resizer
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }

        private List<string> lines = new List<string>();
        private bool GetUpdates = true;
        private string AccessToken;
        private string GroupId;


        private void button1_Click(object sender, EventArgs e)
        {
          
            Thread BotThr = new Thread(apiBotStart);

            button1.Enabled = false;

            BotThr.Start();
        }

        private void apiBotStart()
        {
            Log("Start bot...");

            GetUpdates = true;

            var api = new VkApi();

            api.Authorize(new VkNet.Model.ApiAuthParams() { AccessToken = AccessToken });

            
            

           

            Log("Started.");



            while (GetUpdates)
            {
                try
                {
                    var s = api.Groups.GetLongPollServer(ulong.Parse(GroupId));
                    var poll = api.Groups.GetBotsLongPollHistory(
                     new VkNet.Model.RequestParams.BotsLongPollHistoryParams() { Server = s.Server, Ts = s.Ts, Key = s.Key, Wait = 25 });

                    if (poll?.Updates == null) continue;

                    else
                    {

                        foreach (var update in poll.Updates)
                        {
                            if (update?.MessageNew.Message.Text == null) continue;

                            else
                            {
                                string[] argsmessage = update.MessageNew.Message.Text.Split('.');

                                if (update.Type == VkNet.Enums.SafetyEnums.GroupUpdateType.MessageNew)
                                {

                                    if (update?.MessageNew?.Message.ReadState == null)
                                    {
                                        Log(update.MessageNew.Message.FromId + ": " + update.MessageNew.Message?.Text);


                                        if (update.MessageNew.Message.Attachments.Count == 0) continue;
                                        else
                                        {

                                            VkNet.Model.Attachments.Photo photo = ((VkNet.Model.Attachments.Photo)update.MessageNew.Message.Attachments[0].Instance);

                                            Log("Loading... " + photo.Sizes.Last().Url);


                                            using(WebClient client = new WebClient())
                                            {
                                                client.DownloadFile(photo.Sizes.Last().Url, @"photo" + update.MessageNew.Message.Attachments[0].Instance.ToString() + ".png");
                                            }
                                           
                                           

                                            Log("photo" + update.MessageNew.Message.Attachments[0].Instance.ToString() + ".png   Saved.");

                                            Log("Trasforming");

                                            
                                                Image img = new Bitmap("photo" + update.MessageNew.Message.Attachments[0].Instance.ToString() + ".png");

                                                Image resized = new Bitmap(img, Int32.Parse(argsmessage[0]), Int32.Parse(argsmessage[0]));

                                           

                                                resized.Save("resized.png");

                                                var upladServer = api.Photo.GetMessagesUploadServer((long)update.MessageNew.Message.PeerId);

                                                var response = new WebClient().UploadFile(upladServer.UploadUrl, "resized.png");
                                                var StringResponse = new WebClient().Encoding.GetString(response);
                                                var attachment = api.Photo.SaveMessagesPhoto(StringResponse);

                                                api.Messages.Send(new VkNet.Model.RequestParams.MessagesSendParams()
                                                {
                                                    Attachments = attachment,
                                                    Message = update.MessageNew.Message.Text,
                                                    PeerId = update.MessageNew.Message.PeerId,
                                                    RandomId = new Random().Next() * 1000
                                                });


                                                img.Dispose();

                                                resized.Dispose();

                                                File.Delete("photo" + update.MessageNew.Message.Attachments[0].Instance.ToString() + ".png");

                                                Log("Sended.");
                                            
                                        }



                                        api.Messages.MarkAsRead(update.MessageNew.Message.PeerId + "");

                                    }
                                }
                            }

                            
                        }
                    }
                }
                catch (Exception err)
                {

                    Log(err.Message);
                    
                }


            }


        }

        private void Log(string text)
        {
            lines.Add("["+ DateTime.Now +"]: " + text);
  
            richTextBox1.Invoke(new Action(() => richTextBox1.Lines = lines.ToArray()));
        }

        private void checkBox2_CheckedChanged(object sender, EventArgs e)
        {
            Properties.Settings.Default.UI = checkBox1.Checked;
            Properties.Settings.Default.Save();
            Properties.Settings.Default.Reload();
        }

        private void checkBox1_CheckedChanged(object sender, EventArgs e)
        {
            Properties.Settings.Default.AutoStart = checkBox1.Checked;
            Properties.Settings.Default.Save();
            Properties.Settings.Default.Reload();
            
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            if (File.Exists("config.txt"))
            {
                Log("config.txt считан.");

                string[] config = File.ReadAllLines("config.txt");

                foreach (var item in config)
                {
                  string[] keyPare =  item.Split('=');

                    if (keyPare[0] == "Token")
                    {
                        AccessToken = keyPare[1];
                    }
                    else
                    {
                        GroupId = keyPare[1];
                    }

                }

                Log("TOKEN=" + AccessToken);
                Log("ID=" + GroupId);
                checkBox1.Checked = Properties.Settings.Default.AutoStart;
                checkBox2.Checked = Properties.Settings.Default.UI;

                if (Properties.Settings.Default.UI == true)
                {
                    this.Hide();
                }


                if (Properties.Settings.Default.AutoStart == true)
                {
                    Thread BotThr = new Thread(apiBotStart);
                    GetUpdates = true;
                    button1.Enabled = false;

                    BotThr.Start();
                }
            }
            else
            {
                button1.Enabled = false;
                Log("Введите токен");
                textBox1.Text = "Тут";
            }

           
        }

        private void button2_Click(object sender, EventArgs e)
        {
            Log("Stoped.");
            button1.Enabled = true;
            GetUpdates = false;
            
        }
        private void textBox1_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                if (File.Exists("config.txt"))
                {
                    string[] filelines = File.ReadAllLines("config.txt");
                    List<string> listFileLines =  filelines.ToList();
                    listFileLines.Add("ID="+textBox1.Text);
                    File.WriteAllLines("config.txt", listFileLines.ToArray());
                    Log("Id группы записан в config.txt, при следующем запуске будет использован");
                    Log(textBox1.Text);
                    Log("Для применения пререзапустите ПО");
                }
                else
                {
                    var file = File.Create("config.txt");
                    file.Dispose();
                    File.WriteAllLines("config.txt", new string[] { "Token=" + textBox1.Text });
                    Log(textBox1.Text);
                    Log("Токен записан в config.txt, при следующем запуске будет использован");
                    Log("Теперь введите id группы");
                }
                
            }
        }
    }
}
