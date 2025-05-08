using System;
using System.Drawing;
using System.Windows.Forms;

namespace TestBenchTargetWinForms
{
    public partial class Form1 : Form
    {
        private System.Windows.Forms.Timer timer;
        private float _dpiScaleFactor;

        public Form1()
        {
            InitializeComponent();
            LabelTime.Text = DateTime.Now.ToString("HH:mm:ss");
            LabelToday.Text = DateTime.Now.ToString("dd.MM.yyyy");

            timer = new System.Windows.Forms.Timer();
            timer.Interval = 1000; // 1 sekunda
            timer.Tick += (s, e) =>
            {
                LabelTime.Text = DateTime.Now.ToString("HH:mm:ss");
            };
            timer.Start();

            // Nastavenie DPI škálovania
            this.AutoScaleMode = AutoScaleMode.Dpi;

            // Získanie aktuálneho DPI faktora
            _dpiScaleFactor = this.DeviceDpi / 96.0f;

            // Registrácia event handlera
            this.Load += Form1_Load;
        }

        private void AdjustUIForHighDpi()
        {
            // Implementujte úpravy pre Form1, podobne ako pri Form2
            if (_dpiScaleFactor > 1.5f)
            {
                // Zväčšite šírku formulára
                this.Width = (int)(this.Width * 1.1f);

                // Upravte veľkosť a umiestnenie kontroliek
                // (Tu upravte v závislosti od obsahu Form1)

                // Príklad:
                // Zväčšite výšku textových polí a tlačidiel
                foreach (Control control in this.Controls)
                {
                    if (control is TextBox || control is Button)
                    {
                        control.Height = (int)(control.Height * 1.1f);
                    }
                }

                // Upravte rozostupy medzi kontrolkami
                // (Tu upravte v závislosti od obsahu Form1)
            }
        }

        protected override void OnDpiChanged(DpiChangedEventArgs e)
        {
            base.OnDpiChanged(e);

            // Výpočet zmeny DPI faktora
            float oldDpiScaleFactor = _dpiScaleFactor;
            _dpiScaleFactor = e.DeviceDpiNew / 96.0f;

            // Výpočet zmeny škálovania
            float scaleChange = _dpiScaleFactor / oldDpiScaleFactor;

            // Dočasné pozastavenie layoutu
            this.SuspendLayout();

            // Škálovanie formulára
            this.Scale(new SizeF(scaleChange, scaleChange));

            // Opätovná úprava rozloženia UI pre nové DPI
            AdjustUIForHighDpi();

            // Obnovenie layoutu
            this.ResumeLayout(true);
        }


        private void Form1_Load(object sender, EventArgs e)
        {
            // Úprava UI pri načítaní formulára
            AdjustUIForHighDpi();
        }

        private void ButtonOpenApp_Click(object sender, EventArgs e)
        {
            var Form2 = new Form2();
            Form2.Show();
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            if (timer != null)
            {
                timer.Stop();
                timer.Dispose();
            }
            base.OnFormClosing(e);
        }

        private void AboutToolStripMenuItem_Click(object sender, EventArgs e)
        {
            // Vytvorenie a zobrazenie About dialogu  
            Form aboutForm = new Form()
            {
                Text = "About TestBench Target",
                Size = new System.Drawing.Size(600, 520),
                FormBorderStyle = FormBorderStyle.FixedDialog,
                StartPosition = FormStartPosition.CenterParent,
                MaximizeBox = false,
                MinimizeBox = false
            };

            // Pridanie obsahu do About okna  
            Label titleLabel = new Label()
            {
                Text = "TestBench Target",
                Font = new Font("Microsoft Sans Serif", 14, FontStyle.Bold),
                AutoSize = true,
                Location = new System.Drawing.Point(20, 20)
            };

            Label versionLabel = new Label()
            {
                Text = $"Version: {System.Reflection.Assembly.GetExecutingAssembly().GetName().Version.ToString()}",
                Location = new System.Drawing.Point(20, 50),
                AutoSize = true
            };

            System.Windows.Forms.TextBox descriptionBox = new System.Windows.Forms.TextBox()
            {
                Multiline = true,
                ReadOnly = true,
                ScrollBars = ScrollBars.Vertical,
                BorderStyle = BorderStyle.None,
                Font = new Font("Microsoft Sans Serif", 12),
                BackColor = aboutForm.BackColor,
                Location = new System.Drawing.Point(20, 80),
                Size = new System.Drawing.Size(580, 360),
                Text = @"  
A sample application designed to serve as a testing subject for developers creating monitoring, accessibility, or UI automation tools. 
This app provides predictable user interface elements and behaviors that developers can use to test their monitoring solutions. 
For Windows 10 and newer. 

    Main features:  
        - Small and fast application 
        - Tests opening a Windows directory 
        - Provides a target app for trying out monitoring and testing tools 
        - Simulates adding defined items to a table 
        - Simple chronological display of data in a table format 

Ideal for developers and testers who need a reliable target application when developing tools to monitor and test UI interactions. 
                "
            };

            Label copyrightLabel = new Label()
            {
                Text = "Copyright © 2025 Rudolf Mendzezof",
                Location = new System.Drawing.Point(20, 450), // Pozícia pod textBoxom  
                AutoSize = true
            };

            System.Windows.Forms.Button closeButton = new System.Windows.Forms.Button()
            {
                Text = "Close",
                DialogResult = DialogResult.OK,
                Location = new System.Drawing.Point(476, 440),
                Size = new System.Drawing.Size(90, 30),
                Font = new Font("Microsoft Sans Serif", 10)
            };

            // Pridanie komponentov do formulára  
            aboutForm.Controls.Add(titleLabel);
            aboutForm.Controls.Add(versionLabel);
            aboutForm.Controls.Add(descriptionBox);
            aboutForm.Controls.Add(copyrightLabel);
            aboutForm.Controls.Add(closeButton);

            // Zobrazenie About okna ako modálneho dialógu  
            aboutForm.ShowDialog(this);
        }

        private void LabelTime_Click(object sender, EventArgs e)
        {

        }
    }
}
