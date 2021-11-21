using System;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using GemBox.Email;
using GemBox.Email.Imap;
using GemBox.Email.Smtp;
using System.IO;
using System.Media;

namespace Alarm3
{
    public partial class Form1 : Form
    {
        public AlarmPin alrPin;

        // Variavel de controlo do alarme
        private Boolean alarmIsArmed = false;
        private Boolean alarmIsArmedMannualy = false;

        // Variaveis horario armar/desarmar
        private int? hourArm;
        private int? minArm;
        private int? hourDisarm;
        private int? minDisarm;



        //panic flag
        private int panicFlag = 0;
        //previne emails duplos
        private int mailFlag = 0;
        //tentativas de desarme falhadas
        private int attempts = 0;
        //estado checkbox
        //private int state = 0;

        // Variavel de controlo desativar alarme manual
        private Boolean alarmEnabled = false;

        // Variavel de controlo do alarme externo
        private Boolean externalArm = false;

        // Alarm sound
        private SoundPlayer sPlayer;

        // Beep sound
        private SoundPlayer beepSPlayer;

        // Temporizador alarme
        private int temp = 10;
        private Boolean timeLeft = false;

        // Variavel para controlo do tempo (delay) de disparo dos sensores externos
        private int timeToEnableExternal = 0;

        // Variavel para controlar o tempo de piscar o led
        private int blinkTime = 0;

        // Variavel para controlar o tempo do som do alarme
        private int playTime = 10;

        // Variavel para controlar a luminação exterior
        private Boolean randomIlumination = false;

        // Variavel para enviar mensagem para o Log 
        private int ringAlarmLog = 0;

        public static class K8055
        {
            [DllImport("K8055D.dll")]
            public static extern int OpenDevice(int CardAddress);
            [DllImport("K8055D.dll")]
            public static extern void CloseDevice();
            [DllImport("K8055D.dll")]
            public static extern void SetDigitalChannel(int Channel);
            [DllImport("K8055D.dll")]
            public static extern Boolean ReadDigitalChannel(int Channel);
            [DllImport("K8055D.dll")]
            public static extern void ClearAllDigital();
            [DllImport("K8055D.dll")]
            public static extern void ClearDigitalChannel(int Channel);
            [DllImport("K8055D.dll")]
            public static extern int ReadAllDigital();
        }

        public Form1()
        {
            InitializeComponent();
            // Definir os valores por defeito para os time pickers
            timePickerArmar.Value = new DateTime(2000, 01, 01, 0, 0, 0);
            timePickerDesarmar.Value = new DateTime(2000, 01, 01, 0, 0, 0);
            timer1.Start();
            sPlayer = new SoundPlayer(Properties.Resources.Alarm);
            beepSPlayer = new SoundPlayer(Properties.Resources.beep);
        }



        // Controlo do Som do Alarme        
        public void alarmSound(String play)
        {         
            if (play.Equals("play"))
            {
                sPlayer.Play();
            } 
            else
            {
                sPlayer.Stop();
            }
        }

        // Controlo do Som da Contagem Decrescente
        public void beepSound(String play)
        {
            if (play.Equals("play"))
            {
                beepSPlayer.Play();
            }
            else
            {
                beepSPlayer.Stop();
            }
        }

       
        // Abrir a Placa e Armar o Alarme
        public void device_open(object sender, EventArgs e)
        {
            DateTime currentDate = DateTime.Now;
            if (!alarmIsArmedMannualy && !alarmIsArmed)
            {
                if (!externalArm)
                {
                    alarmIsArmedMannualy = true;
                    Console.WriteLine("Alarm " + alarmEnabled);
                    textBoxLogs.Text += "[" + currentDate.ToString() + "]" + " Alarm will be armed in 10s"+ Environment.NewLine;
                }
                else
                {
                    textBoxLogs.Text += "[" + currentDate.ToString() + "]" + " Disarm Exterior Mode first"+ Environment.NewLine;
                }
            }
        }

        // Desarmar o Alarme
        private void device_close(object sender, EventArgs e)
        {
            
            DateTime currentDate = DateTime.Now;
            if (alarmIsArmed)
            {
                
                AlarmPin alarmPin = new AlarmPin();
                alarmPin.ShowDialog();

                if (alarmPin.correctPin)
                {
                    alarmSound("stop");
                    K8055.ClearDigitalChannel(1);
                    K8055.ClearDigitalChannel(2);
                    K8055.CloseDevice();
                    textBoxLogs.Text += "[" + currentDate.ToString() + "]" + " Alarm Disarmed (Manually)" + Environment.NewLine;
                    labelS1.BackColor = Color.Red;
                    labelS2.BackColor = Color.Red;
                    labelS3.BackColor = Color.Red;
                    labelS4.BackColor = Color.Red;
                    labelS5.BackColor = Color.Red;
                    labelOpenDevice.BackColor = Color.Red;
                    alarmIsArmed = false;
                    alarmEnabled = false;
                    externalArm = false;
                    randomIlumination = false;
                    timeToEnableExternal = 0;
                    alarmPin.correctPin = false;
                    ringAlarmLog = 0;
                    playTime = 10;


                }
                else
                {
                    attempts += 1;

                    if (attempts == 3)
                    {
                        alarmSound("play");
                        textBoxLogs.Text += "[" + currentDate.ToString() + "]" + " Alarm Activated! (Due to excessive unsuccessful attempts)" + Environment.NewLine;
                        /*if (state == 1)
                        {
                            send_Mail(2);
                            mailFlag = 1;
                        }*/
                    }
                    else { textBoxLogs.Text += "[" + currentDate.ToString() + "]" + " INCORRET PIN!!! Unsuccessful disarming attempt: (" + attempts + ")" + Environment.NewLine; }

                    alarmPin.Close();

                }
                
            }
            else { textBoxLogs.Text += "[" + currentDate.ToString() + "]" + " The Alarm is already Disarmed" + Environment.NewLine;}
        }

        // Modo de Alarme Externo
        private void external_mode(object sender, EventArgs e)
        {
            DateTime currentDate = DateTime.Now;
            if (!alarmIsArmed) {
                K8055.OpenDevice(0); 
                K8055.SetDigitalChannel(8); 
                labelOpenDevice.BackColor = Color.Green;
                alarmIsArmed = true; 
                externalArm = true;
                textBoxLogs.Text += "[" + currentDate.ToString() + "]" + " Exterior Mode Activated" + Environment.NewLine;
            }
            else
            {
                textBoxLogs.Text += "[" + currentDate.ToString() + "]" + " Disarm Alarms first" + Environment.NewLine;
            }
        }

        // Armar/Desarmar o alarme automaticamente
        private void timer1_TickTick(object sender, EventArgs e)
        {
            DateTime dateNow = DateTime.Now;
            labelClock.Text = dateNow.ToString("HH:mm:ss");

            // Armar
            if (!alarmIsArmed)
            {
                //verifica a hora
                if (dateNow.Hour == hourArm && dateNow.Minute == minArm)
                {
                    K8055.OpenDevice(0);
                    K8055.SetDigitalChannel(8);
                    labelOpenDevice.BackColor = Color.Green;
                    alarmIsArmed = true;
                    textBoxLogs.Text += "[" + dateNow.ToString() + "]" + " Alarm Armed (Automatically)" + Environment.NewLine;
                }
            }

            // Desarmar
            if (alarmIsArmed)
            {
                //verifica a hora
                if (dateNow.Hour == hourDisarm && dateNow.Minute == minDisarm)
                {
                    alarmSound("stop");
                    // Clear Outputs
                    K8055.ClearDigitalChannel(1);
                    K8055.ClearDigitalChannel(2);
                    K8055.CloseDevice();
                    labelS1.BackColor = Color.Red;
                    labelS2.BackColor = Color.Red;
                    labelS3.BackColor = Color.Red;
                    labelS4.BackColor = Color.Red;
                    labelS5.BackColor = Color.Red;
                    labelOpenDevice.BackColor = Color.Red;
                    alarmIsArmed = false;
                    alarmEnabled = false;
                    externalArm = false;
                    randomIlumination = false;
                    timeToEnableExternal = 0;
                    ringAlarmLog = 0;
                    playTime = 10;
                    textBoxLogs.Text += "[" + dateNow.ToString() + "]" + " Alarm Disarm (Automatically)" + Environment.NewLine;
                }
            }

            // Verificar se o alarme externo está armado
            if (externalArm)
            {
                Boolean s4 = K8055.ReadDigitalChannel(4);
                Boolean s5 = K8055.ReadDigitalChannel(5);

                if (s4 || s5)
                {                  
                    // Delay para não criar falsos alarmes
                    if (timeToEnableExternal < 3) timeToEnableExternal++;
                    if (timeToEnableExternal == 3)
                    {
                        alarmEnabled = true;
                        if (s4) labelS4.BackColor = Color.Green;
                        if (s5) labelS5.BackColor = Color.Green;

                    }
                } else
                {
                    timeToEnableExternal = 0;
                }
            } 
            else if (!externalArm && alarmIsArmed)
            {
                Boolean s1 = K8055.ReadDigitalChannel(1);
                Boolean s2 = K8055.ReadDigitalChannel(2);
                Boolean s3 = K8055.ReadDigitalChannel(3);
                Boolean s4 = K8055.ReadDigitalChannel(4);
                Boolean s5 = K8055.ReadDigitalChannel(5);

                if (s1 || s2 || s3 || s4 || s5)
                {
                    

                    if (s4 || s5)
                    {
                        // Delay para não criar falsos alarmes
                        if (timeToEnableExternal < 3) timeToEnableExternal++;
                        if (timeToEnableExternal == 3)
                        {
                            alarmEnabled = true;
                            if (s4) labelS4.BackColor = Color.Green;
                            if (s5) labelS5.BackColor = Color.Green;
                        }
                    }
                    else
                    {
                        timeToEnableExternal = 0;
                        alarmEnabled = true;
                        if (s1) labelS1.BackColor = Color.Green;
                        if (s2) labelS2.BackColor = Color.Green;
                        if (s3) labelS3.BackColor = Color.Green;
                    }                                 
                } else
                {
                    timeToEnableExternal = 0;
                }
            }            
            if (alarmEnabled)
            {
                K8055.SetDigitalChannel(1);
                if (blinkTime % 2 == 0) K8055.ClearDigitalChannel(2); else K8055.SetDigitalChannel(2);
                blinkTime++;

                if(playTime % 10 == 0) alarmSound("play");
                if(ringAlarmLog == 0)
                {
                    if (panicFlag == 0)
                    {
                        textBoxLogs.Text += "[" + dateNow.ToString() + "]" + " Alarm Activates! (Due to intrusion)" + Environment.NewLine;
                    }
                    else
                    {
                        panicFlag = 0;
                    }
                    /*if (mailFlag == 0 && state == 1)
                    {
                        send_Mail(1);
                    }*/
                    if (mailFlag == 1)
                        mailFlag = 0;

                    ringAlarmLog = 1;
                }
                playTime++;
            }

            // Temporizador armar
            if (alarmIsArmedMannualy)
            {
                temp--;
                if (temp <= 3) { beepSound("play"); }
                
                labelTimeLeft.Text = "" + temp;
                if (temp == 0)
                {
                    beepSound("stop");
                    temp = 10;
                    labelTimeLeft.Text = "";
                    timeLeft = true;
                    alarmIsArmedMannualy = false;
                }
            }

            // Armar Manualmente
            if (timeLeft)
            {
                K8055.OpenDevice(0); 
                K8055.SetDigitalChannel(8); 
                labelOpenDevice.BackColor = Color.Green;
                alarmIsArmed = true;
                timeLeft = false;
                textBoxLogs.Text += "[" + dateNow.ToString() + "]" + " Alarm Armed (Manually)" + Environment.NewLine;
            }
            
            // Iluminação exterior e aleatoriedade luminosa no interior
            if (randomIlumination && alarmIsArmed)
            {
                for (int i = 3; i <= 7; i++)
                {
                    K8055.ClearDigitalChannel(i);
                }

                Random rnd = new Random();
                int led = rnd.Next(3, 7); // creates a number between 3 and 6

                K8055.SetDigitalChannel(7); // Led 7 é a lampada exterior sempre ligada
                K8055.SetDigitalChannel(led);
            }
            
            if (!randomIlumination && alarmIsArmed)
            {
                for (int i = 3; i <= 7; i++)
                {
                    K8055.ClearDigitalChannel(i);
                }
            }
        }

        // Botão para confirmar o armar automatico
        private void button1SetArm_Click(object sender, EventArgs e)
        {
            DateTime dateNow = DateTime.Now;

            // Armar
            hourArm = timePickerArmar.Value.Hour;
            minArm = timePickerArmar.Value.Minute;

            String horarioArmar = timePickerArmar.Value.TimeOfDay.Hours.ToString() + "H" + timePickerArmar.Value.TimeOfDay.Minutes.ToString();

            textBoxLogs.Text += "[" + dateNow.ToString() + "]" + " Automatic arming at: " + horarioArmar + Environment.NewLine;
        }

        // Botão para confirmar o desarmar automatico
        private void button2SetDisarm_Click(object sender, EventArgs e)
        {
            DateTime dateNow = DateTime.Now;
            AlarmPin alarmPin = new AlarmPin();
            alarmPin.ShowDialog();
            if (alarmPin.correctPin)
            {
                // Desarmar
                hourDisarm = timePickerDesarmar.Value.Hour;
                minDisarm = timePickerDesarmar.Value.Minute;

                String horarioDesarmar = timePickerDesarmar.Value.TimeOfDay.Hours.ToString() + "H" + timePickerDesarmar.Value.TimeOfDay.Minutes.ToString();

                textBoxLogs.Text += "[" + dateNow.ToString() + "]" + " Automatic Disarm at: " + horarioDesarmar + Environment.NewLine;
            }
        }

        // Botao de Panico
        private void button3Panic_Click(object sender, EventArgs e)
        {
            DateTime currentDate = DateTime.Now;

            if (!alarmIsArmed)
            {
                K8055.OpenDevice(0);
                textBoxLogs.Text += "[" + currentDate.ToString() + "]" + " PANIC ACTIVATED" + Environment.NewLine;
                alarmIsArmed = true;
                alarmEnabled = true;
                K8055.SetDigitalChannel(2);
                K8055.SetDigitalChannel(1);
                K8055.SetDigitalChannel(8);
                labelOpenDevice.BackColor = Color.Green;
            }
            else
            {
                textBoxLogs.Text += "[" + currentDate.ToString() + "]" + " PANIC ACTIVATED" + Environment.NewLine;
                alarmEnabled = true;
                K8055.SetDigitalChannel(2);
                K8055.SetDigitalChannel(1);
            }

            /*if (state == 1)
            {
                send_Mail(0);
                mailFlag = 1;
            }*/
            panicFlag = 1;
        }

        

        // Botao da iluminacao aleatoria
        private void button4RandomIlumination_Click(object sender, EventArgs e)
        {
            K8055.OpenDevice(0); //Open communication with K8055 that has the device address 0
            K8055.SetDigitalChannel(8); // 5V Power
            DateTime dateNow = DateTime.Now;
            if(randomIlumination == false)
            {
                randomIlumination = true;
                textBoxLogs.Text += "[" + dateNow.ToString() + "]" + " Presence Illumination Activated" + Environment.NewLine;
            }
            else
            {
                randomIlumination = false;
                textBoxLogs.Text += "[" + dateNow.ToString() + "]" + " Presence Illumination Disabled" + Environment.NewLine;
            }
        }

        //Guardar os Logs
        private void button2_SaveLogsClick(object sender, EventArgs e)
        {
            DateTime dateNow = DateTime.Now;
            String input = textBoxLogs.Text.ToString();
            if (!input.Equals("")) { 

                string path = Path.GetDirectoryName(Application.ExecutablePath) + "\\logs.txt";
                // This text is added only once to the file.
                if (!File.Exists(path))
                {
                    // Create a file to write to.
                    using (StreamWriter sw = File.CreateText(path))
                    {
                        sw.WriteLine("");
                    }
                }

                // This text is always added, making the file longer over time
                // if it is not deleted.
                using (StreamWriter sw = File.AppendText(path))
                {
                    sw.WriteLine(input);
                }
                textBoxLogs.Text += "[" + dateNow.ToString() + "]" + " Saved Logs" + Environment.NewLine;
            }
        }

        //Carregar os logs
        private void button3_LoadLogsClick(object sender, EventArgs e)
        {
            DateTime dateNow = DateTime.Now;
            try
            {
                // Open the text file using a stream reader.
                using (var sr = new StreamReader(Path.GetDirectoryName(Application.ExecutablePath) + "\\logs.txt"))
                {
                    String fileContent = sr.ReadToEnd();

                    if (fileContent.Equals(""))
                    {
                        textBoxLogs.Text += "[" + dateNow.ToString() + "]" + " There are no saved events to load" + Environment.NewLine;
                    }
                    else
                    {
                        // Read the stream as a string, and write the string to the console.
                        textBoxLogs.Text += "********************************************************************" + Environment.NewLine;
                        textBoxLogs.Text += "LOADED EVENTS" + Environment.NewLine;
                        textBoxLogs.Text += fileContent;
                        textBoxLogs.Text += "********************************************************************" + Environment.NewLine;
                    }
                }
            }
            catch (IOException)
            {
                textBoxLogs.Text += "[" + dateNow.ToString() + "]" + " There are no saved events to load" + Environment.NewLine;
            }
        }


        //Limpar Logs
        private void guna2Button8_CleanLogsClick(object sender, EventArgs e)
        {
            textBoxLogs.Text = "";
        }

        /*
        private void timer3_Tick(object sender, EventArgs e)
        {
           // If using Professional version, put your serial key below.
           ComponentInfo.SetLicense("FREE-LIMITED-KEY");

            // Create IMAP client.
            using (var imap = new ImapClient("imap-mail.outlook.com"))
            {
                //  Connect and sign to IMAP server.
                imap.Connect();
                imap.Authenticate("insira aqui o mail do outlook", "insira aqui a password");

                // Select INBOX folder.
                imap.SelectInbox();


                DateTime dateNow = DateTime.Now;

                // Download and receive all email messages from selected mailbox folder.
                GemBox.Email.MailMessage message = imap.GetMessage(imap.SelectedFolder.Count);
                if (message.Subject.Contains("Armar"))
                {
                    if (!alarmIsArmed)
                    {
                        K8055.OpenDevice(0);
                        K8055.SetDigitalChannel(8);
                        labelOpenDevice.BackColor = Color.Green;
                        alarmIsArmed = true;
                        textBoxLogs.Text += "[" + dateNow.ToString() + "]" + " Alarm Armed (Via Email)" + Environment.NewLine;
                            
                    }
                }
                if(message.Subject.Contains("Disarme 1234") && alarmIsArmed)
                {
                    // Stop sound
                    alarmSound("stop");
                    // Clear Outputs
                    K8055.ClearDigitalChannel(1);
                    K8055.ClearDigitalChannel(2);
                    // Close Device
                    K8055.CloseDevice();
                    // Alterar a cor das labels para verde no mapa
                    labelS1.BackColor = Color.Red;
                    labelS2.BackColor = Color.Red;
                    labelS3.BackColor = Color.Red;
                    labelS4.BackColor = Color.Red;
                    labelS5.BackColor = Color.Red;
                    labelOpenDevice.BackColor = Color.Red;
                    alarmIsArmed = false;
                    alarmEnabled = false;
                    externalArm = false;
                    randomIlumination = false;
                    timeToEnableExternal = 0;
                    ringAlarmLog = 0;
                    playTime = 10;
                    textBoxLogs.Text += "[" + dateNow.ToString() + "]" + " Alarm Disarmed (Via Email)" + Environment.NewLine;
                }

            }
        }*/
        /*
        private void guna2CheckBox1_CheckedChanged(object sender, EventArgs e)
        {
            DateTime dateNow = DateTime.Now;

            if (state == 0) { 
                timer3.Start(); 
                state = 1;
                textBoxLogs.Text += "[" + dateNow.ToString() + "]" + " Email Functionality Enabled" + Environment.NewLine;
            }
            else { 
                timer3.Stop(); 
                state = 0;
                textBoxLogs.Text += "[" + dateNow.ToString() + "]" + " Email Functionality Disabled" + Environment.NewLine;
            }
        }*/
        /*
        private void send_Mail(int mode)
        {
            string[] messages = { "Your alarm has been activated in panic mode.",
                "Your alarm has been activated due to intrusion.",
                "Your alarm has been activated due to incorrect pin." };
            
            DateTime dateNow = DateTime.Now;
            // If using Professional version, put your serial key below.
            ComponentInfo.SetLicense("FREE-LIMITED-KEY");

            // Create new email message.
            MailMessage message = new MailMessage(
                new MailAddress("insira aqui o mail do outlook", "Residential Alarm"),
                new MailAddress("insira aqui o mail para receber as not", "First receiver"));


            // Add subject and body.
            message.Subject = "ALARM ON";
            message.BodyText = messages[mode];

            // Create new SMTP client and send an email message.
            using (SmtpClient smtp = new SmtpClient("smtp-mail.outlook.com"))
            {
                smtp.Connect();
                smtp.Authenticate("insira aqui o mail do outlook", "insira aqui a password");
                smtp.SendMessage(message);
                textBoxLogs.Text += "[" + dateNow.ToString() + "]" + " Warning email sent" + Environment.NewLine;
            }
        }*/
        
        private void label2_Click(object sender, EventArgs e)
        {

        }
    }
}
