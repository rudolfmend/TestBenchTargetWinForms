namespace TestBenchTargetWinForms
{
    partial class Form1
    {
        /// <summary>
        ///  Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        ///  Clean up any resources being used.
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
        ///  Required method for Designer support - do not modify
        ///  the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(Form1));
            this.LabelTime = new System.Windows.Forms.Label();
            this.LabelToday = new System.Windows.Forms.Label();
            this.ButtonOpenApp = new System.Windows.Forms.Button();
            this.MenuStrip1 = new System.Windows.Forms.MenuStrip();
            this.HelpToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.AboutToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.MenuStrip1.SuspendLayout();
            this.SuspendLayout();
            // 
            // LabelTime
            // 
            this.LabelTime.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.LabelTime.AutoSize = true;
            this.LabelTime.BackColor = System.Drawing.Color.Transparent;
            this.LabelTime.FlatStyle = System.Windows.Forms.FlatStyle.Popup;
            this.LabelTime.Font = new System.Drawing.Font("Microsoft Sans Serif", 24F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(238)));
            this.LabelTime.ForeColor = System.Drawing.Color.Black;
            this.LabelTime.Location = new System.Drawing.Point(209, 106);
            this.LabelTime.Margin = new System.Windows.Forms.Padding(200, 40, 200, 40);
            this.LabelTime.Name = "LabelTime";
            this.LabelTime.Size = new System.Drawing.Size(143, 37);
            this.LabelTime.TabIndex = 1;
            this.LabelTime.Text = "00:00:00";
            this.LabelTime.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            this.LabelTime.Click += new System.EventHandler(this.LabelTime_Click);
            // 
            // LabelToday
            // 
            this.LabelToday.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.LabelToday.AutoSize = true;
            this.LabelToday.Font = new System.Drawing.Font("Microsoft Sans Serif", 24F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(238)));
            this.LabelToday.Location = new System.Drawing.Point(190, 183);
            this.LabelToday.Margin = new System.Windows.Forms.Padding(0);
            this.LabelToday.Name = "LabelToday";
            this.LabelToday.Size = new System.Drawing.Size(179, 37);
            this.LabelToday.TabIndex = 2;
            this.LabelToday.Text = "00.00.0000";
            // 
            // ButtonOpenApp
            // 
            this.ButtonOpenApp.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.ButtonOpenApp.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.ButtonOpenApp.Cursor = System.Windows.Forms.Cursors.Hand;
            this.ButtonOpenApp.Font = new System.Drawing.Font("Microsoft Sans Serif", 20.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(238)));
            this.ButtonOpenApp.Location = new System.Drawing.Point(0, 362);
            this.ButtonOpenApp.Margin = new System.Windows.Forms.Padding(26, 12, 26, 12);
            this.ButtonOpenApp.Name = "ButtonOpenApp";
            this.ButtonOpenApp.Size = new System.Drawing.Size(584, 120);
            this.ButtonOpenApp.TabIndex = 3;
            this.ButtonOpenApp.Text = "Open Application";
            this.ButtonOpenApp.UseVisualStyleBackColor = true;
            this.ButtonOpenApp.Click += new System.EventHandler(this.ButtonOpenApp_Click);
            // 
            // MenuStrip1
            // 
            this.MenuStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.HelpToolStripMenuItem});
            this.MenuStrip1.Location = new System.Drawing.Point(0, 0);
            this.MenuStrip1.Name = "MenuStrip1";
            this.MenuStrip1.Padding = new System.Windows.Forms.Padding(6, 8, 0, 8);
            this.MenuStrip1.Size = new System.Drawing.Size(584, 48);
            this.MenuStrip1.TabIndex = 4;
            this.MenuStrip1.Text = "MenuStrip1";
            // 
            // HelpToolStripMenuItem
            // 
            this.HelpToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.AboutToolStripMenuItem});
            this.HelpToolStripMenuItem.Font = new System.Drawing.Font("Segoe UI", 14.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(238)));
            this.HelpToolStripMenuItem.Name = "HelpToolStripMenuItem";
            this.HelpToolStripMenuItem.Padding = new System.Windows.Forms.Padding(0, 2, 1, 1);
            this.HelpToolStripMenuItem.Size = new System.Drawing.Size(56, 32);
            this.HelpToolStripMenuItem.Text = "Help";
            // 
            // AboutToolStripMenuItem
            // 
            this.AboutToolStripMenuItem.Name = "AboutToolStripMenuItem";
            this.AboutToolStripMenuItem.Size = new System.Drawing.Size(180, 30);
            this.AboutToolStripMenuItem.Text = "About";
            this.AboutToolStripMenuItem.Click += new System.EventHandler(this.AboutToolStripMenuItem_Click);
            // 
            // Form1
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(96F, 96F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Dpi;
            this.ClientSize = new System.Drawing.Size(584, 481);
            this.Controls.Add(this.ButtonOpenApp);
            this.Controls.Add(this.LabelToday);
            this.Controls.Add(this.LabelTime);
            this.Controls.Add(this.MenuStrip1);
            this.Font = new System.Drawing.Font("Microsoft Sans Serif", 14.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(238)));
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.MainMenuStrip = this.MenuStrip1;
            this.Margin = new System.Windows.Forms.Padding(52, 24, 52, 24);
            this.Name = "Form1";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "TestBench Target - Window1";
            this.MenuStrip1.ResumeLayout(false);
            this.MenuStrip1.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion
        private System.Windows.Forms.Label LabelTime;
        private System.Windows.Forms.Label LabelToday;
        private System.Windows.Forms.Button ButtonOpenApp;
        private System.Windows.Forms.MenuStrip MenuStrip1;
        private System.Windows.Forms.ToolStripMenuItem HelpToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem AboutToolStripMenuItem;
    }


}

