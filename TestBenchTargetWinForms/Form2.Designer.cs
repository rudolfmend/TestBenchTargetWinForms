using System.Windows.Forms;

namespace TestBenchTargetWinForms
{
    partial class Form2
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(Form2));
            this.LabelTime = new System.Windows.Forms.Label();
            this.ButtonAddDate = new System.Windows.Forms.Button();
            this.DataGridView1 = new System.Windows.Forms.DataGridView();
            this.ColumnDate = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.ColumnProcedure = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.ColumnPoints = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.ColumnDelegate = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.DomainUpDownDate = new System.Windows.Forms.DomainUpDown();
            this.TextBoxProcedure = new System.Windows.Forms.TextBox();
            this.TextBoxPoints = new System.Windows.Forms.TextBox();
            this.TextBoxDelegate = new System.Windows.Forms.TextBox();
            this.ButtonOpenFolder = new System.Windows.Forms.Button();
            this.LabelDate = new System.Windows.Forms.Label();
            this.LabelDelegate = new System.Windows.Forms.Label();
            this.LabelProcedure = new System.Windows.Forms.Label();
            this.LabelPoints = new System.Windows.Forms.Label();
            this.ButtonLoadData = new System.Windows.Forms.Button();
            this.ButtonSaveData = new System.Windows.Forms.Button();
            this.ButtonRemove = new System.Windows.Forms.Button();
            ((System.ComponentModel.ISupportInitialize)(this.DataGridView1)).BeginInit();
            this.SuspendLayout();
            // 
            // LabelTime
            // 
            this.LabelTime.Location = new System.Drawing.Point(28, 9);
            this.LabelTime.Margin = new System.Windows.Forms.Padding(6, 0, 6, 0);
            this.LabelTime.Name = "LabelTime";
            this.LabelTime.Size = new System.Drawing.Size(183, 42);
            this.LabelTime.TabIndex = 13;
            this.LabelTime.Text = "00:00:00";
            // 
            // ButtonAddDate
            // 
            this.ButtonAddDate.Anchor = ((System.Windows.Forms.AnchorStyles)(System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right));
            this.ButtonAddDate.Cursor = System.Windows.Forms.Cursors.Hand;
            this.ButtonAddDate.Location = new System.Drawing.Point(750, 324);
            this.ButtonAddDate.Margin = new System.Windows.Forms.Padding(20, 15, 20, 15);
            this.ButtonAddDate.Name = "ButtonAddDate";
            this.ButtonAddDate.Size = new System.Drawing.Size(220, 42);
            this.ButtonAddDate.TabIndex = 14;
            this.ButtonAddDate.Text = "Add to table";
            this.ButtonAddDate.UseVisualStyleBackColor = true;
            this.ButtonAddDate.Click += new System.EventHandler(this.ButtonAddDate_Click);
            // 
            // DataGridView1
            // 
            this.DataGridView1.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
            | System.Windows.Forms.AnchorStyles.Left) | System.Windows.Forms.AnchorStyles.Right)));
            this.DataGridView1.AutoSizeColumnsMode = System.Windows.Forms.DataGridViewAutoSizeColumnsMode.AllCells;
            this.DataGridView1.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.DataGridView1.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] {
            this.ColumnDate,
            this.ColumnProcedure,
            this.ColumnPoints,
            this.ColumnDelegate});
            this.DataGridView1.Cursor = System.Windows.Forms.Cursors.Hand;
            this.DataGridView1.Location = new System.Drawing.Point(15, 57);
            this.DataGridView1.Margin = new System.Windows.Forms.Padding(6);
            this.DataGridView1.Name = "DataGridView1";
            this.DataGridView1.Size = new System.Drawing.Size(709, 597);
            this.DataGridView1.TabIndex = 12;
            this.DataGridView1.KeyDown += new System.Windows.Forms.KeyEventHandler(this.DataGridView1_KeyDown);
            // 
            // ColumnDate
            // 
            this.ColumnDate.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.Fill;
            this.ColumnDate.HeaderText = "Date";
            this.ColumnDate.Name = "ColumnDate";
            this.ColumnDate.ToolTipText = "dd.MM.yyyy - date format only";
            // 
            // ColumnProcedure
            // 
            this.ColumnProcedure.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.Fill;
            this.ColumnProcedure.HeaderText = "Procedure";
            this.ColumnProcedure.Name = "ColumnProcedure";
            this.ColumnProcedure.ToolTipText = "String of characters";
            // 
            // ColumnPoints
            // 
            this.ColumnPoints.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.Fill;
            this.ColumnPoints.HeaderText = "Points";
            this.ColumnPoints.Name = "ColumnPoints";
            this.ColumnPoints.ToolTipText = "Numbers only";
            // 
            // ColumnDelegate
            // 
            this.ColumnDelegate.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.Fill;
            this.ColumnDelegate.HeaderText = "Delegate";
            this.ColumnDelegate.Name = "ColumnDelegate";
            this.ColumnDelegate.ToolTipText = "String of characters";
            // 
            // DomainUpDownDate
            // 
            this.DomainUpDownDate.Anchor = ((System.Windows.Forms.AnchorStyles)(System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right));
            this.DomainUpDownDate.Cursor = System.Windows.Forms.Cursors.Hand;
            this.DomainUpDownDate.Location = new System.Drawing.Point(750, 76);
            this.DomainUpDownDate.Margin = new System.Windows.Forms.Padding(6, 0, 6, 6);
            this.DomainUpDownDate.Name = "DomainUpDownDate";
            this.DomainUpDownDate.Size = new System.Drawing.Size(220, 29);
            this.DomainUpDownDate.TabIndex = 11;
            // 
            // TextBoxProcedure
            // 
            this.TextBoxProcedure.Anchor = ((System.Windows.Forms.AnchorStyles)(System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right));
            this.TextBoxProcedure.Cursor = System.Windows.Forms.Cursors.IBeam;
            this.TextBoxProcedure.Location = new System.Drawing.Point(750, 142);
            this.TextBoxProcedure.Margin = new System.Windows.Forms.Padding(6);
            this.TextBoxProcedure.Name = "TextBoxProcedure";
            this.TextBoxProcedure.Size = new System.Drawing.Size(220, 29);
            this.TextBoxProcedure.TabIndex = 10;
            // 
            // TextBoxPoints
            // 
            this.TextBoxPoints.Anchor = ((System.Windows.Forms.AnchorStyles)(System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right));
            this.TextBoxPoints.Cursor = System.Windows.Forms.Cursors.IBeam;
            this.TextBoxPoints.Location = new System.Drawing.Point(750, 208);
            this.TextBoxPoints.Margin = new System.Windows.Forms.Padding(6);
            this.TextBoxPoints.Name = "TextBoxPoints";
            this.TextBoxPoints.Size = new System.Drawing.Size(220, 29);
            this.TextBoxPoints.TabIndex = 9;
            // 
            // TextBoxDelegate
            // 
            this.TextBoxDelegate.Anchor = ((System.Windows.Forms.AnchorStyles)(System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right));
            this.TextBoxDelegate.Cursor = System.Windows.Forms.Cursors.IBeam;
            this.TextBoxDelegate.Location = new System.Drawing.Point(750, 274);
            this.TextBoxDelegate.Margin = new System.Windows.Forms.Padding(6);
            this.TextBoxDelegate.Name = "TextBoxDelegate";
            this.TextBoxDelegate.Size = new System.Drawing.Size(220, 29);
            this.TextBoxDelegate.TabIndex = 8;
            // 
            // ButtonOpenFolder
            // 
            this.ButtonOpenFolder.Anchor = ((System.Windows.Forms.AnchorStyles)(System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right));
            this.ButtonOpenFolder.Cursor = System.Windows.Forms.Cursors.Hand;
            this.ButtonOpenFolder.Location = new System.Drawing.Point(750, 612);
            this.ButtonOpenFolder.Margin = new System.Windows.Forms.Padding(20, 15, 20, 15);
            this.ButtonOpenFolder.Name = "ButtonOpenFolder";
            this.ButtonOpenFolder.Size = new System.Drawing.Size(220, 42);
            this.ButtonOpenFolder.TabIndex = 7;
            this.ButtonOpenFolder.Text = "Open folder";
            this.ButtonOpenFolder.UseVisualStyleBackColor = true;
            this.ButtonOpenFolder.Click += new System.EventHandler(this.ButtonOpenFolder_Click);
            // 
            // LabelDate
            // 
            this.LabelDate.Anchor = ((System.Windows.Forms.AnchorStyles)(System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right));
            this.LabelDate.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(238)));
            this.LabelDate.Location = new System.Drawing.Point(766, 58);
            this.LabelDate.Margin = new System.Windows.Forms.Padding(0, 15, 0, 0);
            this.LabelDate.Name = "LabelDate";
            this.LabelDate.Size = new System.Drawing.Size(72, 17);
            this.LabelDate.TabIndex = 6;
            this.LabelDate.Text = "Date";
            // 
            // LabelDelegate
            // 
            this.LabelDelegate.Anchor = ((System.Windows.Forms.AnchorStyles)(System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right));
            this.LabelDelegate.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(238)));
            this.LabelDelegate.Location = new System.Drawing.Point(766, 258);
            this.LabelDelegate.Margin = new System.Windows.Forms.Padding(0, 15, 0, 0);
            this.LabelDelegate.Name = "LabelDelegate";
            this.LabelDelegate.Size = new System.Drawing.Size(72, 17);
            this.LabelDelegate.TabIndex = 5;
            this.LabelDelegate.Text = "Delegate";
            // 
            // LabelProcedure
            // 
            this.LabelProcedure.Anchor = ((System.Windows.Forms.AnchorStyles)(System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right));
            this.LabelProcedure.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(238)));
            this.LabelProcedure.Location = new System.Drawing.Point(766, 126);
            this.LabelProcedure.Margin = new System.Windows.Forms.Padding(0, 15, 0, 0);
            this.LabelProcedure.Name = "LabelProcedure";
            this.LabelProcedure.Size = new System.Drawing.Size(72, 17);
            this.LabelProcedure.TabIndex = 4;
            this.LabelProcedure.Text = "Procedure";
            // 
            // LabelPoints
            // 
            this.LabelPoints.Anchor = ((System.Windows.Forms.AnchorStyles)(System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right));
            this.LabelPoints.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(238)));
            this.LabelPoints.Location = new System.Drawing.Point(766, 192);
            this.LabelPoints.Margin = new System.Windows.Forms.Padding(0, 15, 0, 0);
            this.LabelPoints.Name = "LabelPoints";
            this.LabelPoints.Size = new System.Drawing.Size(72, 17);
            this.LabelPoints.TabIndex = 3;
            this.LabelPoints.Text = "Points";
            // 
            // ButtonLoadData
            // 
            this.ButtonLoadData.Anchor = ((System.Windows.Forms.AnchorStyles)(System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right));
            this.ButtonLoadData.Cursor = System.Windows.Forms.Cursors.Hand;
            this.ButtonLoadData.Location = new System.Drawing.Point(750, 396);
            this.ButtonLoadData.Margin = new System.Windows.Forms.Padding(20, 15, 20, 15);
            this.ButtonLoadData.Name = "ButtonLoadData";
            this.ButtonLoadData.Size = new System.Drawing.Size(220, 42);
            this.ButtonLoadData.TabIndex = 2;
            this.ButtonLoadData.Text = "Load data";
            this.ButtonLoadData.UseVisualStyleBackColor = true;
            this.ButtonLoadData.Click += new System.EventHandler(this.ButtonLoadData_Click);
            // 
            // ButtonSaveData
            // 
            this.ButtonSaveData.Anchor = ((System.Windows.Forms.AnchorStyles)(System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right));
            this.ButtonSaveData.Cursor = System.Windows.Forms.Cursors.Hand;
            this.ButtonSaveData.Location = new System.Drawing.Point(750, 468);
            this.ButtonSaveData.Margin = new System.Windows.Forms.Padding(20, 15, 20, 15);
            this.ButtonSaveData.Name = "ButtonSaveData";
            this.ButtonSaveData.Size = new System.Drawing.Size(220, 42);
            this.ButtonSaveData.TabIndex = 1;
            this.ButtonSaveData.Text = "Save data to file";
            this.ButtonSaveData.UseVisualStyleBackColor = true;
            this.ButtonSaveData.Click += new System.EventHandler(this.ButtonSaveData_Click);
            // 
            // ButtonRemove
            // 
            this.ButtonRemove.Anchor = ((System.Windows.Forms.AnchorStyles)(System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right));
            this.ButtonRemove.Cursor = System.Windows.Forms.Cursors.Hand;
            this.ButtonRemove.Location = new System.Drawing.Point(750, 540);
            this.ButtonRemove.Margin = new System.Windows.Forms.Padding(20, 15, 20, 15);
            this.ButtonRemove.Name = "ButtonRemove";
            this.ButtonRemove.Size = new System.Drawing.Size(220, 42);
            this.ButtonRemove.TabIndex = 0;
            this.ButtonRemove.Text = "Delete data";
            this.ButtonRemove.UseVisualStyleBackColor = true;
            this.ButtonRemove.Click += new System.EventHandler(this.ButtonRemove_Click);
            // 
            // Form2
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(96F, 96F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Dpi;
            this.ClientSize = new System.Drawing.Size(998, 681);
            this.Controls.Add(this.ButtonRemove);
            this.Controls.Add(this.ButtonSaveData);
            this.Controls.Add(this.ButtonLoadData);
            this.Controls.Add(this.LabelPoints);
            this.Controls.Add(this.LabelProcedure);
            this.Controls.Add(this.LabelDelegate);
            this.Controls.Add(this.LabelDate);
            this.Controls.Add(this.ButtonOpenFolder);
            this.Controls.Add(this.TextBoxDelegate);
            this.Controls.Add(this.TextBoxPoints);
            this.Controls.Add(this.TextBoxProcedure);
            this.Controls.Add(this.DomainUpDownDate);
            this.Controls.Add(this.DataGridView1);
            this.Controls.Add(this.LabelTime);
            this.Controls.Add(this.ButtonAddDate);
            this.Font = new System.Drawing.Font("Microsoft Sans Serif", 14.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(238)));
            this.HelpButton = true;
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.Margin = new System.Windows.Forms.Padding(6);
            this.Name = "Form2";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "TestBench Target - Window2";
            ((System.ComponentModel.ISupportInitialize)(this.DataGridView1)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion
        private Label LabelTime;
        private Button ButtonAddDate;
        private DataGridView DataGridView1;
        private DomainUpDown DomainUpDownDate;
        private TextBox TextBoxProcedure;
        private TextBox TextBoxPoints;
        private TextBox TextBoxDelegate;
        private Button ButtonOpenFolder;
        private Label LabelDate;
        private Label LabelDelegate;
        private Label LabelProcedure;
        private Label LabelPoints;
        private Button ButtonLoadData;
        private Button ButtonSaveData;
        private Button ButtonRemove;
        private DataGridViewTextBoxColumn ColumnDate;
        private DataGridViewTextBoxColumn ColumnProcedure;
        private DataGridViewTextBoxColumn ColumnPoints;
        private DataGridViewTextBoxColumn ColumnDelegate;
    }
}
