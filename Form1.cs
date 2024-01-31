namespace Palworld_server_protector_DotNet
{
    using Microsoft.VisualBasic.Devices;
    using System.Diagnostics;
    using System.Windows.Forms;
    using System;
    using System.IO;
    using System.IO.Compression;
    using System.Drawing;
    using static System.Net.WebRequestMethods;
    using System.Runtime.InteropServices;
    using System.Text;
    using static System.Windows.Forms.LinkLabel;
    using System.Net;

    public partial class Form1 : Form
    {
        private Timer memTimer;
        private Timer saveTimer;
        private Timer getplayerTimer;
        private string cmdPath;
        private string backupPath;
        private string gamedataPath;
        private Int32 memTarget;
        private string rconHost{get => rconHostbox.Text;set=>rconHostbox.Text = value;}
        private Int32 rconPort;
        private string rconPassword;
        private Int32 rebootSeconds;
        private string errorLogname = $"error_{DateTime.Now.ToString("yyyyMMddHHmmss")}.log";
        private string projectUrl = $"https://github.com/KirosHan/Palworld-server-protector-DotNet";
        private Int32 playersTimercounter = 0;
        private Int32 playersTimerthreshold = 600;//ÿСʱ����600��

        private const string ConfigFilePath = "config.ini";

        [DllImport("kernel32")]
        private static extern long WritePrivateProfileString(string section, string key, string val, string filePath);

        [DllImport("kernel32")]
        private static extern int GetPrivateProfileString(string section, string key, string def, StringBuilder retVal, int size, string filePath);

        public Form1()
        {
            InitializeComponent();
            InitializeTimer();
            string buildVersion = Application.ProductVersion;
            int endIndex = buildVersion.IndexOf('+'); // �ҵ��汾���е�"+"���ŵ�����λ��
            string version = buildVersion.Substring(0, endIndex); // ʹ��Substring������ȡ��0��endIndex֮������ַ���
            this.Text = $"Palworld Server Protector v{version}";
        }

        private void InitializeTimer()
        {
            memTimer = new Timer();
            memTimer.Interval = 35000; // ���ö�ʱ�����Ϊ5��
            memTimer.Tick += Timer_Tick;

            saveTimer = new Timer();
            saveTimer.Interval = 35000; // ���ö�ʱ�����Ϊ5��
            saveTimer.Tick += saveTimer_Tick;

            getplayerTimer = new Timer();
            getplayerTimer.Interval = 3000; // ���ö�ʱ�����Ϊs��
            getplayerTimer.Tick += getplayerTimer_Tick;
        }



        private void Timer_Tick(object sender, EventArgs e)
        {
            // ��ȡϵͳ�ڴ�ʹ�ðٷֱ�
            var memoryUsage = Math.Round(GetSystemMemoryUsagePercentage(), 2);
            memProcessbar.Value = (int)memoryUsage;
            memOutput.Text = $"{memoryUsage}%";
            if (checkBox_mem.Checked)
            {
                //OutputMessageAsync($"��ǰʱ�䣺{DateTime.Now}");
                OutputMessageAsync($"�ڴ�ʹ�ðٷֱȣ�{memoryUsage}%");
            }



            if (checkBox_reboot.Checked)//�Զ��ط�
            {
                if (memoryUsage >= memTarget)
                {
                    try
                    {
                        var isProcessRunning = IsProcessRunning(cmdPath);
                        if (isProcessRunning)
                        {
                            OutputMessageAsync($"�ڴ�ﵽ������ֵ������");
                            // ʹ��rcon�����˷���ָ��
                            RconUtils.TestConnection(rconHost, Convert.ToInt32(rconPortbox.Text), passWordbox.Text);
                            var info = RconUtils.SendMsg("save");
                            OutputMessageAsync($"{info}");
                            var result = RconUtils.SendMsg($"Shutdown {rebootSeconds} The_server_will_restart_in_{rebootSeconds}_seconds.");

                            OutputMessageAsync($"{result}");
                            SendWebhookAsync("�ڴ�ﵽ������ֵ", $"�ڴ�ʹ���ʣ�{memoryUsage}%,�ѳ��Թرշ�������");
                            ShowNotification($"�ڴ�ʹ���ʣ�{memoryUsage}%,�ѳ��Թرշ�������");
                        }


                    }
                    catch (Exception ex)
                    {
                        OutputMessageAsync($"����ָ��ʧ�ܣ��������á�");
                        AppendToErrorLog($"����ָ��ʧ�ܣ��������á�{ex.Message}");
                        SendWebhookAsync("Rconʧ��", $"���͹ط�ָ��ʧ�ܣ��뼰ʱ��顣");
                        ShowNotification($"���͹ط�ָ��ʧ�ܣ��뼰ʱ��顣");

                    }





                }
            }

            if (checkBox_startprocess.Checked)
            { //���&����
              // �������Ƿ�������
                var isProcessRunning = IsProcessRunning(cmdPath);
                labelForprogram.Text = $"{(isProcessRunning ? "������" : "δ����")}";
                OutputMessageAsync($"��������״̬��{(isProcessRunning ? "������" : "δ����")}");
                if (!isProcessRunning)
                {
                    if (!isProcessRunning)
                    {
                        try
                        {

                            Process process;
                            int processId;
                            if (checkBox_args.Checked && arguments.Text != "")
                            {
                                OutputMessageAsync($"���ڳ������������({arguments.Text})...");
                                process = Process.Start(cmdPath, arguments.Text);
                                processId = process.Id;
                            }
                            else
                            {
                                OutputMessageAsync($"���ڳ������������...");

                                process = Process.Start(cmdPath);
                                processId = process.Id;
                            }
                            if (processId > 0)
                            {
                                labelForPid.Text = processId.ToString();
                                labelForpidText.Visible = true;
                                labelForPid.Visible = true;
                                OutputMessageAsync($"����������ɹ���");
                                SendWebhookAsync("����������ɹ�", $"����������ɹ���");
                                ShowNotification($"����������ɹ���");

                            }
                        }
                        catch (Exception ex)
                        {
                            OutputMessageAsync($"���������ʧ�ܡ�");
                            AppendToErrorLog($"���������ʧ�ܣ�{ex.Message}");
                            SendWebhookAsync("���������ʧ��", $"���������ʧ�ܣ��뼰ʱ��顣");
                            ShowNotification($"���������ʧ�ܣ��뼰ʱ��顣");
                        }
                        SendWebhookAsync("���������", $"�ѳ�����������ˡ�");
                        ShowNotification($"�ѳ�����������ˡ�");

                    }

                }
            }

        }

        private void getplayerTimer_Tick(object sender, EventArgs e) //��ȡ�������
        {
            try
            {
                if (!checkBox_geplayers.Checked)
                {
                    return;
                }

                RconUtils.TestConnection(rconHost, Convert.ToInt32(rconPortbox.Text), passWordbox.Text);
                var players = RconUtils.ShowPlayers();

                playersCounterLabel.Text = $"��ǰ���ߣ�{players.Count}��";
                // Clear the playersView
                playersView.Items.Clear();

                var playerList = "";
                // Add the players information to the playersView
                foreach (var player in players)
                {
                    var item = new ListViewItem(new[] { player.name, player.uid, player.steam_id });
                    playersView.Items.Add(item);
                    playerList = playerList + player.name + ",";
                }
                playersTimercounter += 1;
                if (playersTimercounter >= playersTimerthreshold)
                {
                    playersTimercounter = 0;
                    playerList = playerList.TrimEnd(',');
                    SendWebhookAsync("�������ͳ��", $"��ǰ������ң�{players.Count}�ˡ�\r\n{playerList}");
                }
            }
            catch (Exception ex)
            {
                AppendToErrorLog($"��ȡ�������ʧ�ܣ�{ex.Message}");
            }
        }
        private void CopyGameDataToBackupPath()
        {
            try
            {
                if (backupPath == "")
                {
                    OutputMessageAsync($"δ���ñ��ݴ��Ŀ¼���޷����ݡ�");
                    return;
                }
                string backupFolderName = $"SaveGames-{DateTime.Now.ToString("yyyyMMdd-HHmmss")}.zip";
                string backupFilePath = Path.Combine(backupPath, backupFolderName);

                if (!Directory.Exists(gamedataPath))
                {
                    OutputMessageAsync($"��Ϸ�浵·�������ڣ�{gamedataPath}");
                    return;
                }

                if (!Directory.Exists(backupPath))
                {
                    OutputMessageAsync($"�浵����·�������ڣ�{backupPath}");
                    return;
                }

                string tempGameDataPath = Path.Combine(Path.GetTempPath(), "TempGameData");
                Directory.CreateDirectory(tempGameDataPath);
                string tempGameDataCopyPath = Path.Combine(tempGameDataPath, "GameData");

                // Copy the game data to the temporary directory
                DirectoryCopy(gamedataPath, tempGameDataCopyPath, true);

                // Create the backup file from the temporary game data directory
                ZipFile.CreateFromDirectory(tempGameDataCopyPath, backupFilePath);

                // Delete the temporary game data directory
                Directory.Delete(tempGameDataPath, true);

                OutputMessageAsync($"��Ϸ�浵�ѳɹ�����");
                SendWebhookAsync("�浵����", $"��Ϸ�浵�ѳɹ����ݡ�");
                ShowNotification($"��Ϸ�浵�ѳɹ����ݡ�");
            }
            catch (Exception ex)
            {
                OutputMessageAsync($"���ݴ浵ʧ��");
                AppendToErrorLog($"���ݴ浵ʧ�ܣ�{ex.Message}");
                SendWebhookAsync("�浵����ʧ��", $"�浵����ʧ�ܣ��뼰ʱ��顣");
                ShowNotification($"�浵����ʧ�ܣ��뼰ʱ��顣");
            }
        }

        private void DirectoryCopy(string sourceDirName, string destDirName, bool copySubDirs)
        {
            DirectoryInfo dir = new DirectoryInfo(sourceDirName);
            DirectoryInfo[] dirs = dir.GetDirectories();

            // If the source directory does not exist, throw an exception
            if (!dir.Exists)
            {
                OutputMessageAsync($"��Ϸ�浵·�������ڣ�{sourceDirName}");
                AppendToErrorLog($"��Ϸ�浵·�������ڣ�{sourceDirName}");
                ShowNotification($"��Ϸ�浵·�������ڣ�{sourceDirName}");
            }

            // If the destination directory does not exist, create it
            if (!Directory.Exists(destDirName))
            {
                Directory.CreateDirectory(destDirName);
            }

            // Get the files in the directory and copy them to the new location
            FileInfo[] files = dir.GetFiles();
            foreach (FileInfo file in files)
            {
                string temppath = Path.Combine(destDirName, file.Name);
                file.CopyTo(temppath, false);
            }

            // If copying subdirectories, copy them and their contents to new location
            if (copySubDirs)
            {
                foreach (DirectoryInfo subdir in dirs)
                {
                    string temppath = Path.Combine(destDirName, subdir.Name);
                    DirectoryCopy(subdir.FullName, temppath, copySubDirs);
                }
            }
        }

        private void saveTimer_Tick(object sender, EventArgs e) //�浵�߼�
        {
            OutputMessageAsync($"�Զ��浵��...");
            CopyGameDataToBackupPath();

        }

        private float GetSystemMemoryUsagePercentage()
        {
            var info = new ComputerInfo();
            var totalMemory = (float)info.TotalPhysicalMemory;
            var availableMemory = (float)info.AvailablePhysicalMemory;

            var memoryUsage = (totalMemory - availableMemory) / totalMemory;

            return memoryUsage * 100;
        }



        private bool IsProcessRunning(string processPath)
        {
            var processName = Path.GetFileNameWithoutExtension(processPath);
            var processes = Process.GetProcessesByName(processName);
            return processes.Any(p=>p.MainModule.FileName == processPath);
        }


        private void selectCmdbutton_Click(object sender, EventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "Executable Files|*.exe";
            if (openFileDialog.ShowDialog() == DialogResult.OK)
            {
                cmdbox.Text = openFileDialog.FileName;
                cmdPath = cmdbox.Text;
                OutputMessageAsync($"��ѡ������·��Ϊ��{cmdPath}");
                gamedataPath = Path.Combine(Path.GetDirectoryName(cmdPath), "Pal", "Saved", "SaveGames");
                gamedataBox.Text = gamedataPath;
                OutputMessageAsync($"��Ϸ�浵·���޸�Ϊ��{gamedataPath}");
            }
        }

        private async Task OutputMessageAsync(string message)
        {
            await Task.Run(() =>
            {
                outPutbox.Invoke(new Action(() =>
                {
                    outPutbox.AppendText($"[{DateTime.Now.ToString("HH:mm:ss")}] {message}" + Environment.NewLine);

                    if (outPutbox.Lines.Length > 100)
                    {
                        outPutbox.Text = string.Join(Environment.NewLine, outPutbox.Lines.Skip(outPutbox.Lines.Length - 100));
                        outPutbox.SelectionStart = outPutbox.Text.Length;
                        outPutbox.ScrollToCaret();
                    }
                    else
                    {
                        outPutbox.SelectionStart = outPutbox.Text.Length;
                        outPutbox.ScrollToCaret();
                    }
                }));
            });
        }



        private void Form1_Load(object sender, EventArgs e)
        {

            playersView.View = View.Details;
            playersView.Columns.Add(new ColumnHeader() { Text = "Name", Width = playersView.Width / 3 });
            playersView.Columns.Add(new ColumnHeader() { Text = "UID", Width = playersView.Width / 3 });
            playersView.Columns.Add(new ColumnHeader() { Text = "Steam ID", Width = playersView.Width / 3 });

            playersView.FullRowSelect = true;
            playersView.MultiSelect = false;
            playersView.HideSelection = false;

            tabControl1.TabPages[0].Text = "������";
            tabControl1.TabPages[1].Text = "�Զ��浵";
            tabControl1.TabPages[2].Text = "Rcon";
            tabControl1.TabPages[3].Text = "֪ͨ";
            tabControl1.TabPages[4].Text = "���Թ���";

            LoadConfig();
            memTimer.Start();
            string buildVersion = Application.ProductVersion;
            int endIndex = buildVersion.IndexOf('+');
            string version = buildVersion.Substring(0, endIndex); //ȥ��������ʶ��
            verisionLabel.Text = $"��ǰ�汾��{version}";
            checkVersion(version);
            OutputMessageAsync($"��ǰ�����汾�ţ�{version}");

            OutputMessageAsync($"�������ú���Ϣ���ٹ�ѡ��������");


        }
        private void checkVersion(string myversion)
        {
            try
            {
                string url = $"http://127.0.0.1/version?v={myversion}";
                using (WebClient client = new WebClient())
                {
                    string json = client.DownloadString(url);
                    dynamic data = Newtonsoft.Json.JsonConvert.DeserializeObject(json);
                    string latestVersion = data[0].version;

                    if (IsVersionNewer(latestVersion, myversion))
                    {
                        linkLabel2.Text = $"����������°汾(v{latestVersion})";
                        projectUrl = data[0].url;
                        linkLabel2.Visible = true;
                    }
                }
            }
            catch
            { }
        
        }

        private bool IsVersionNewer(string latestVersion, string myVersion)
        {
            Version latest = new Version(latestVersion);
            Version current = new Version(myVersion);
            return latest > current;
        }
        private void LoadConfig()
        {
            try
            {
                if (System.IO.File.Exists(ConfigFilePath))
                {
                    using (StreamReader reader = new StreamReader(ConfigFilePath))
                    {
                        string line;
                        while ((line = reader.ReadLine()) != null)
                        {
                            if (line.StartsWith("CmdPath="))
                            {
                                cmdPath = line.Substring("CmdPath=".Length);
                                cmdbox.Text = cmdPath;
                            }
                            else if (line.StartsWith("BackupPath="))
                            {
                                backupPath = line.Substring("BackupPath=".Length);
                                backupPathbox.Text = backupPath;
                            }
                            else if (line.StartsWith("GameDataPath="))
                            {
                                gamedataPath = line.Substring("GameDataPath=".Length);
                                gamedataBox.Text = gamedataPath;
                            }
                            else if (line.StartsWith("MemTarget="))
                            {
                                memTarget = Convert.ToInt32(line.Substring("MemTarget=".Length));
                                memTargetbox.Value = memTarget;
                            }
                            else if (line.StartsWith("RconHost="))
                            {
                                rconHost = line.Substring("RconHost=".Length);
                            }
                            else if (line.StartsWith("RconPort="))
                            {
                                rconPort = Convert.ToInt32(line.Substring("RconPort=".Length));
                                rconPortbox.Text = rconPort.ToString();
                            }
                            else if (line.StartsWith("RconPassword="))
                            {
                                rconPassword = line.Substring("RconPassword=".Length);
                                passWordbox.Text = rconPassword;
                            }
                            else if (line.StartsWith("RebootSeconds="))
                            {
                                rebootSeconds = Convert.ToInt32(line.Substring("RebootSeconds=".Length));
                                rebootSecondbox.Value = rebootSeconds;
                            }
                            else if (line.StartsWith("CheckSeconds="))
                            {
                                memTimer.Interval = Convert.ToInt32(line.Substring("CheckSeconds=".Length)) * 1000;
                                checkSecondbox.Value = memTimer.Interval / 1000;
                            }
                            else if (line.StartsWith("BackupSeconds="))
                            {
                                saveTimer.Interval = Convert.ToInt32(line.Substring("BackupSeconds=".Length)) * 1000;
                                backupSecondsbox.Value = saveTimer.Interval / 1000;
                            }
                            else if (line.StartsWith("Parameters="))
                            {
                                arguments.Text = line.Substring("Parameters=".Length);
                            }
                            else if (line.StartsWith("WebhookUrl="))
                            {
                                webhookBox.Text = line.Substring("WebhookUrl=".Length);
                            }
                            else if(line.StartsWith("isReboot="))
                            {
                                if (line.Substring("isReboot=".Length) == "True")
                                {
                                    checkBox_reboot.Checked = true;
                                }
                                else
                                {
                                    checkBox_reboot.Checked = false;
                                }
                            }
                            else if (line.StartsWith("isStartProcess="))
                            {
                                if (line.Substring("isStartProcess=".Length) == "True")
                                {
                                    checkBox_startprocess.Checked = true;
                                }
                                else
                                {
                                    checkBox_startprocess.Checked = false;
                                }
                            }
                            else if (line.StartsWith("isParameters="))
                            {
                                if (line.Substring("isParameters=".Length) == "True")
                                {
                                    checkBox_args.Checked = true;
                                }
                                else
                                {
                                    checkBox_args.Checked = false;
                                }
                            }
                            else if (line.StartsWith("isNoti="))
                            {
                                if (line.Substring("isNoti=".Length) == "True")
                                {
                                    checkBox_Noti.Checked = true;
                                }
                                else
                                {
                                    checkBox_Noti.Checked = false;
                                }
                            }
                            else if (line.StartsWith("isSave="))
                            {
                                if (line.Substring("isSave=".Length) == "True")
                                {
                                    checkBox_save.Checked = true;
                                }
                                else
                                {
                                    checkBox_save.Checked = false;
                                }
                            }
                            else if (line.StartsWith("isGetPlayers="))
                            {
                                if (line.Substring("isGetPlayers=".Length) == "True")
                                {
                                    checkBox_geplayers.Checked = true;
                                }
                                else
                                {
                                    checkBox_geplayers.Checked = false;
                                }
                            }
                            else if (line.StartsWith("isWebhook="))
                            {
                                if (line.Substring("isWebhook=".Length) == "True")
                                {
                                    checkBox_webhook.Checked = true;
                                }
                                else
                                {
                                    checkBox_webhook.Checked = false;
                                }
                            }   
                            else
                            {
                                continue;
                            }
                        }
                    }
                }
                else
                {
                    OutputMessageAsync($"δ�ҵ������ļ����Ѽ���Ĭ�����á�");
                    ShowNotification($"δ�ҵ������ļ����Ѽ���Ĭ�����á�");
                    dataInit();
                }
            }
            catch (Exception ex)
            {
                OutputMessageAsync($"��ȡ�����ļ�ʧ�ܡ�");
                ShowNotification($"��ȡ�����ļ�ʧ�ܡ�");
                AppendToErrorLog($"��ȡ�����ļ�ʧ�ܣ�{ex.Message}");
            }
        }
        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            SaveConfig();
        }




        private void SaveConfig()
        {
            try
            {
                using (StreamWriter writer = new StreamWriter(ConfigFilePath))
                {
                    writer.WriteLine("[General]");
                    writer.WriteLine("CmdPath=" + cmdbox.Text);
                    writer.WriteLine("Parameters=" + arguments.Text);
                    writer.WriteLine("BackupPath=" + backupPathbox.Text);
                    writer.WriteLine("GameDataPath=" + gamedataBox.Text);
                    writer.WriteLine("MemTarget=" + memTarget);
                    writer.WriteLine("RconHost=" + rconHost);
                    writer.WriteLine("RconPort=" + rconPortbox.Text);
                    writer.WriteLine("RconPassword=" + passWordbox.Text);
                    writer.WriteLine("RebootSeconds=" + rebootSeconds);
                    writer.WriteLine("CheckSeconds=" + memTimer.Interval / 1000);
                    writer.WriteLine("BackupSeconds=" + saveTimer.Interval / 1000);
                    writer.WriteLine("WebhookUrl=" + webhookBox.Text);
                    if (checkBox_reboot.Checked){writer.WriteLine("isReboot=True");}else { writer.WriteLine("isReboot=False"); }
                    if (checkBox_startprocess.Checked) { writer.WriteLine("isStartProcess=True");}else { writer.WriteLine("isStartProcess=False"); }
                    if(checkBox_args.Checked) { writer.WriteLine("isParameters=True"); } else { writer.WriteLine("isParameters=False"); }
                    if(checkBox_Noti.Checked) { writer.WriteLine("isNoti=True"); } else { writer.WriteLine("isNoti=False"); }
                    if(checkBox_save.Checked) { writer.WriteLine("isSave=True"); } else { writer.WriteLine("isSave=False"); }
                    if(checkBox_geplayers.Checked) { writer.WriteLine("isGetPlayers=True"); } else { writer.WriteLine("isGetPlayers=False"); }
                    if(checkBox_webhook.Checked) { writer.WriteLine("isWebhook=True"); } else { writer.WriteLine("isWebhook=False"); }

                }
            }
            catch (Exception ex)
            {
                OutputMessageAsync($"���������ļ�ʧ�ܡ�");
                AppendToErrorLog($"���������ļ�ʧ�ܣ�{ex.Message}");

            }
        }




        private void dataInit()
        {
            memTimer.Interval = Convert.ToInt32(checkSecondbox.Value) * 1000;

            memTarget = Convert.ToInt32(memTargetbox.Value);
            rconHost = "127.0.0.1";
            rconPort = 25575;
            rconPassword = "admin";
            rebootSeconds = 10;
            cmdPath = "";
            gamedataPath = "";
            backupPath = "";
            saveTimer.Interval = Convert.ToInt32(backupSecondsbox.Value) * 1000;



        }

        private void checkBox_startprocess_CheckedChanged(object sender, EventArgs e)
        {
            if (cmdPath == "")
            {
                labelForstart.Text = "[ �ر� ]";
                OutputMessageAsync($"�������÷����·����");
                labelForPid.Visible = false;
                labelForpidText.Visible = false;
                checkBox_startprocess.Checked = false;


            }
            else if (checkBox_startprocess.Checked)
            {
                labelForstart.Text = "[ ���� ]";
                OutputMessageAsync($"�ѿ�ʼ��ط���ˡ�");
            }
            else
            {
                labelForprogram.Text = "δ֪";
                labelForstart.Text = "[ �ر� ]";
                labelForPid.Visible = false;
                labelForpidText.Visible = false;
                OutputMessageAsync($"��ֹͣ��ط���ˡ�");
            }

        }

        private void checkBox_save_CheckedChanged(object sender, EventArgs e)
        {
            if (checkBox_save.Checked)
            {
                if (gamedataPath == "")
                {
                    OutputMessageAsync($"����ѡ����Ϸ�浵·����");
                    labelForsave.Text = "[ �ر� ]";
                    checkBox_save.Checked = false;
                }
                else if (backupPath == "")
                {
                    OutputMessageAsync($"����ѡ��浵����·����");
                    labelForsave.Text = "[ �ر� ]";
                    checkBox_save.Checked = false;
                }
                else
                {
                    saveTimer.Interval = Convert.ToInt32(backupSecondsbox.Value) * 1000;
                    saveTimer.Start();
                    labelForsave.Text = "[ ���� ]";
                    OutputMessageAsync($"�������Զ����ݴ浵��");
                    OutputMessageAsync($"�Զ��浵��...");
                    CopyGameDataToBackupPath();
                   
                }

            }
            else
            {
                saveTimer.Stop();
                labelForsave.Text = "[ �ر� ]";
                OutputMessageAsync($"��ͣ���Զ����ݴ浵��");
            }

        }

        private void selectBackuppathButton_Click(object sender, EventArgs e)
        {
            FolderBrowserDialog folderBrowserDialog = new FolderBrowserDialog();
            if (folderBrowserDialog.ShowDialog() == DialogResult.OK)
            {
                backupPathbox.Text = folderBrowserDialog.SelectedPath;
                backupPath = backupPathbox.Text;
                OutputMessageAsync($"��ѡ��浵����·��Ϊ��{backupPath}");
            }
        }





        private void rconPortbox_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (!Char.IsDigit(e.KeyChar) && e.KeyChar != 8)
            {
                e.Handled = true;
            }
        }



        private void passWordbox_KeyPress(object sender, KeyPressEventArgs e)
        {
            rconPassword = passWordbox.Text;
            //OutputMessageAsync($"����������Ϊ��{rconPassword}");
        }

        private void rebootSecondbox_ValueChanged(object sender, EventArgs e)
        {


        }

        private void checkBox_reboot_CheckedChanged(object sender, EventArgs e)
        {
            if (cmdPath == "")
            {
                checkBox_reboot.Checked = false;
                labelForreboot.Text = "[ �ر� ]";
                OutputMessageAsync($"����ѡ������·����");
            }

            else if (checkBox_reboot.Checked)
            {
                labelForreboot.Text = "[ ���� ]";
                OutputMessageAsync($"�������Զ��ط���");
            }
            else
            {
                labelForreboot.Text = "[ �ر� ]";
                OutputMessageAsync($"��ͣ���Զ��ط���");
            }
        }

        private void checkBox_geplayers_CheckedChanged(object sender, EventArgs e)
        {
            if (checkBox_geplayers.Checked)
            {
                playersTimercounter = playersTimerthreshold;//����������һ��
                getplayerTimer.Start();
                labelForgetplayers.Text = "[ ���� ]";
                OutputMessageAsync($"�������Զ���ȡ������ҡ�");
            }
            else
            {
                getplayerTimer.Stop();
                labelForgetplayers.Text = "[ �ر� ]";
                OutputMessageAsync($"��ͣ���Զ���ȡ������ҡ�");
                playersCounterLabel.Text = $"��ǰ���ߣ�δ֪";
            }
        }

        private void memOutput_Click(object sender, EventArgs e)
        {

        }

        private void button2_Click(object sender, EventArgs e)
        {

            RconUtils.TestConnection(rconHost, Convert.ToInt32(rconPortbox.Text), passWordbox.Text);


            var info = RconUtils.SendMsg("save");
            OutputMessageAsync($"{info}");


        }

        private void button3_Click(object sender, EventArgs e)
        {
            try
            {
                RconUtils.TestConnection(rconHost, Convert.ToInt32(rconPortbox.Text), passWordbox.Text);

                var result = RconUtils.SendMsg($"Shutdown 10 The_server_will_restart_in_10econds.");

                OutputMessageAsync($"{result}");

            }
            catch (Exception ex)
            {
                OutputMessageAsync($"�ط�ָ���ʧ�ܡ�");
                AppendToErrorLog($"�ط�ָ���ʧ�ܣ�{ex.Message}");
            }

        }

        private void button4_Click(object sender, EventArgs e)
        {
            try
            {
                RconUtils.TestConnection(rconHost, Convert.ToInt32(rconPortbox.Text), passWordbox.Text);

                var info = RconUtils.SendMsg("info");

                int startIndex = info.IndexOf("[") + 1;
                int endIndex = info.IndexOf("]");
                string version = info.Substring(startIndex, endIndex - startIndex);
                versionLabel.Text = $"����˰汾��{version}";
                OutputMessageAsync($"��ǰ����˰汾��{version}");

            }
            catch (Exception ex)
            {
                OutputMessageAsync($"infoָ���ʧ�ܡ�");
                AppendToErrorLog($"infoָ���ʧ�ܣ�{ex.Message}");
            }

        }

        private void button1_Click(object sender, EventArgs e)
        {
            try
            {
                RconUtils.TestConnection(rconHost, Convert.ToInt32(rconPortbox.Text), passWordbox.Text);

                textBox1.Text = textBox1.Text.Replace(" ", "_");
                var info = RconUtils.SendMsg($"broadcast {textBox1.Text.Trim()}");

                OutputMessageAsync($"�ѷ��ͣ�{info}");

            }

            catch (Exception ex)
            {
                OutputMessageAsync($"broadcastָ���ʧ�ܡ�");
                AppendToErrorLog($"broadcastָ���ʧ�ܣ�{ex.Message}");
            }
        }

        private void linkLabel1_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            try
            {
                string url = "https://github.com/KirosHan/Palworld-server-protector-DotNet";
                Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
            }
            catch (Exception ex)
            {
            }
        }


        private void checkBox_args_CheckedChanged(object sender, EventArgs e)
        {
            if (checkBox_args.Checked)
            {
                arguments.Enabled = true;
                OutputMessageAsync($"����д���������������");
            }
            else
            {
                arguments.Enabled = false;
            }
        }

        private void selectCustombutton_Click(object sender, EventArgs e)
        {
            FolderBrowserDialog folderBrowserDialog = new FolderBrowserDialog();
            if (folderBrowserDialog.ShowDialog() == DialogResult.OK)
            {
                gamedataBox.Text = folderBrowserDialog.SelectedPath;
                gamedataPath = gamedataBox.Text;
                OutputMessageAsync($"��ѡ����Ϸ�浵·��Ϊ��{gamedataPath}");
            }
        }
        private bool isKeyUpEvent_backupSecond = false;
        private void backupSecondsbox_KeyUp(object sender, KeyEventArgs e)
        {
            var newBackupSecond = Convert.ToInt32(backupSecondsbox.Value) * 1000;
            saveTimer.Interval = newBackupSecond;
            isKeyUpEvent_backupSecond = true;
            OutputMessageAsync($"�浵�����ѵ���Ϊ��{newBackupSecond / 1000}��");
        }
        private void backupSecondsbox_ValueChanged(object sender, EventArgs e)
        {
            if (isKeyUpEvent_backupSecond)
            {
                isKeyUpEvent_backupSecond = false;
                return;
            }
            var newBackupSecond = Convert.ToInt32(backupSecondsbox.Value) * 1000;
            saveTimer.Interval = newBackupSecond;
            OutputMessageAsync($"�浵�����ѵ���Ϊ��{newBackupSecond / 1000}��");
        }

        private bool isKeyUpEvent_rebootSecond = false;
        private void rebootSecondbox_KeyUp(object sender, KeyEventArgs e)
        {
            rebootSeconds = Convert.ToInt32(rebootSecondbox.Value);
            isKeyUpEvent_rebootSecond = true;
            OutputMessageAsync($"�����ӳ�������Ϊ��{rebootSeconds}��");
        }

        private void rebootSecondbox_ValueChanged_1(object sender, EventArgs e)
        {
            if (isKeyUpEvent_rebootSecond)
            {
                isKeyUpEvent_rebootSecond = false;
                return;
            }
            rebootSeconds = Convert.ToInt32(rebootSecondbox.Value);
            OutputMessageAsync($"�����ӳ�������Ϊ��{rebootSeconds}��");
        }

        private bool isKeyUpEvent_checkSecond = false;

        private void checkSecondbox_ValueChanged(object sender, EventArgs e)
        {
            if (isKeyUpEvent_checkSecond)
            {
                isKeyUpEvent_checkSecond = false;
                return;
            }
            var newSecond = Convert.ToInt32(checkSecondbox.Value);
            memTimer.Interval = newSecond * 1000;
            OutputMessageAsync($"��������ѵ���Ϊ��{newSecond}��");
        }

        private void checkSecondbox_KeyUp(object sender, KeyEventArgs e)
        {
            var newSecond = Convert.ToInt32(checkSecondbox.Value);
            memTimer.Interval = newSecond * 1000;
            isKeyUpEvent_checkSecond = true;
            OutputMessageAsync($"��������ѵ���Ϊ��{newSecond}��");
        }

        private bool isKeyUpEvent_memTarget = false;

        private void memTargetbox_KeyUp(object sender, KeyEventArgs e)
        {
            memTarget = Convert.ToInt32(memTargetbox.Value);
            isKeyUpEvent_memTarget = true;
            OutputMessageAsync($"�ڴ���ֵ�ѵ���Ϊ��{memTarget}%");
        }
        private void memTargetbox_ValueChanged(object sender, EventArgs e)
        {
            if (isKeyUpEvent_memTarget)
            {
                isKeyUpEvent_memTarget = false;
                return;
            }
            memTarget = Convert.ToInt32(memTargetbox.Value);
            OutputMessageAsync($"�ڴ���ֵ�ѵ���Ϊ��{memTarget}%");

        }

        private void playersView_ItemSelectionChanged(object sender, ListViewItemSelectionChangedEventArgs e)
        {
            if (playersView.SelectedItems.Count > 0)
            {

                string Uid = playersView.SelectedItems[0].SubItems[1].Text;
                UIDBox.Text = Uid;

            }
        }

        private void kickbutton_Click(object sender, EventArgs e)
        {
            try
            {
                RconUtils.TestConnection(rconHost, Convert.ToInt32(rconPortbox.Text), passWordbox.Text);

                var info = RconUtils.SendMsg($"KickPlayer {UIDBox.Text.Trim()}");

                OutputMessageAsync($"{info}");

            }

            catch (Exception ex)
            {
                OutputMessageAsync($"Kickplayerָ���ʧ�ܡ�");
                AppendToErrorLog($"Kickplayerָ���ʧ�ܣ�{ex.Message}");
            }
        }

        private void banbutton_Click(object sender, EventArgs e)
        {
            try
            {
                RconUtils.TestConnection(rconHost, Convert.ToInt32(rconPortbox.Text), passWordbox.Text);

                var info = RconUtils.SendMsg($"BanPlayer {UIDBox.Text.Trim()}");

                OutputMessageAsync($"{info}");

            }

            catch (Exception ex)
            {
                OutputMessageAsync($"BanPlayerָ���ʧ�ܡ�{ex.Message}");
                AppendToErrorLog($"BanPlayerָ���ʧ�ܡ�");
            }
        }
        private void AppendToErrorLog(string content)
        {
            string logFolderPath = Path.Combine(Directory.GetCurrentDirectory(), "log");
            string errorLogPath = Path.Combine(logFolderPath, errorLogname);

            if (!Directory.Exists(logFolderPath))
            {
                Directory.CreateDirectory(logFolderPath);
            }

            using (StreamWriter writer = System.IO.File.AppendText(errorLogPath))
            {
                writer.WriteLine($"[{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")}] {content}");
            }
        }

        private configForm configForm; // Declare a field to hold the instance of ConfigForm

        private void settingButton_Click(object sender, EventArgs e)
        {
            if (configForm == null || configForm.IsDisposed) // Check if the configForm is null or disposed
            {
                configForm = new configForm(); // Create a new instance of ConfigForm
                configForm.Show(); // Show the configForm
            }
            else
            {
                configForm.BringToFront(); // Bring the existing configForm to the front
            }
        }

        private void testWebhookbutton_Click(object sender, EventArgs e)
        {

            SendWebhookAsync("���Ա���", "����һ����������֪ͨ��");

        }
        private async Task SendWebhookAsync(string title, string message)
        {
            if (!checkBox_webhook.Checked)
            {
                return;
            }
            if (webhookBox.Text == "")
            {
                OutputMessageAsync($"Webhook��ַΪ�ա�");
                return;
            }
            if (!webhookBox.Text.Contains("http"))
            {
                OutputMessageAsync($"Webhook��ʽ����ȷ��");
                return;
            }
            try
            {
                using (HttpClient client = new HttpClient())
                {
                    string webhookUrl = webhookBox.Text;
                    var webhook = new WebhookJson();
                    string json = webhook.GenerateJson(webhookUrl, title, message);
                    var content = new StringContent(json, Encoding.UTF8, "application/json");
                    await client.PostAsync(webhookUrl, content);
                    OutputMessageAsync($"Webhook���ͳɹ���");
                }
            }
            catch (Exception ex)
            {
                OutputMessageAsync($"Webhook����ʧ�ܡ�");
                AppendToErrorLog($"Webhook����ʧ�ܣ�{ex.Message}");
            }

        }

        private void checkBox_webhook_CheckedChanged(object sender, EventArgs e)
        {
            if (checkBox_webhook.Checked)
            {
                webhookBox.Enabled = true;
                testWebhookbutton.Enabled = true;
                labelForwebhook.Text = "[ ���� ]";
                OutputMessageAsync($"������Webhook���͡�");
            }
            else
            {
                webhookBox.Enabled = false;
                testWebhookbutton.Enabled = false;
                labelForwebhook.Text = "[ �ر� ]";
                OutputMessageAsync($"��ͣ��Webhook���͡�");
            }
        }

        private void notifyIcon1_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            if (WindowState == FormWindowState.Minimized)
            {
                WindowState = FormWindowState.Normal;
                ShowInTaskbar = true; // Add this line to show the form in the taskbar
            }
        }

        private void Form1_Resize(object sender, EventArgs e)
        {
            if (WindowState == FormWindowState.Minimized)
            {
                ShowInTaskbar = false; // Add this line to show the form in the taskbar
            }
        }
        private void ShowNotification(string message)
        {
            if (checkBox_Noti.Checked)
            {
                notifyIcon1.BalloonTipText = message;
                notifyIcon1.ShowBalloonTip(2000);
            }

        }

        private void linkLabel2_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            try
            {
                Process.Start(new ProcessStartInfo(projectUrl) { UseShellExecute = true });
            }
            catch (Exception ex)
            {
            }
        }

        /**  HTTP����������
        private Thread httpThread;
        private bool isHttpServerRunning;

        private void checkBox_host_CheckedChanged(object sender, EventArgs e)
        {
            if (checkBox_host.Checked)
            {
                StartHttpServer();
            }
            else
            {
                StopHttpServer();
            }
        }

        private void StartHttpServer()
        {
            if (!isHttpServerRunning)
            {
                httpThread = new Thread(HttpServerThread);
                httpThread.Start();
                isHttpServerRunning = true;
            }
        }

        private void StopHttpServer()
        {
            if (isHttpServerRunning)
            {
                httpThread.Abort();
                isHttpServerRunning = false;
            }
        }

        private void HttpServerThread()
        {
            string folderPath = Path.Combine(Application.StartupPath, "list");
            string filePath = Path.Combine(folderPath, "banlist.txt");
          

            if (!Directory.Exists(folderPath))
            {
                Directory.CreateDirectory(folderPath);
            }

            if (!System.IO.File.Exists(filePath))
            {
                System.IO.File.Create(filePath).Close();
            }
            using (HttpListener listener = new HttpListener())
            {
                listener.Prefixes.Add("http://127.0.0.1:8080/");
                listener.Start();

                while (true)
                {
                    try
                    {
                        HttpListenerContext context = listener.GetContext();
                        HttpListenerRequest request = context.Request;
                        HttpListenerResponse response = context.Response;

                        if (request.Url.LocalPath == "/banlist.txt")
                        {
                            if (System.IO.File.Exists(filePath))
                            {
                                byte[] fileBytes = System.IO.File.ReadAllBytes(filePath);
                                //response.ContentType = "text/plain"; 
                                response.ContentType = "text/html; charset = UTF - 8";

                                response.ContentLength64 = fileBytes.Length;
                                response.OutputStream.Write(fileBytes, 0, fileBytes.Length);
                                response.OutputStream.Close();
                            }
                            else
                            {
                                response.StatusCode = (int)HttpStatusCode.NotFound;
                                response.Close();
                            }
                        }
                        else
                        {
                            response.StatusCode = (int)HttpStatusCode.NotFound;
                            response.Close();
                        }
                    }
                    catch (ThreadAbortException)
                    {
                        break;
                    }
                    catch (Exception ex)
                    {
                        // Handle any exceptions here
                    }
                }

                listener.Stop();
            }
        }
      **/
    }
}
