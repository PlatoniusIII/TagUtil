using BrightIdeasSoftware;

namespace TagUtil
{
    partial class MainForm
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
            this.buttonRenameFolder = new System.Windows.Forms.Button();
            this.FileInfoView = new System.Windows.Forms.ListView();
            this.TagInfoPanel = new System.Windows.Forms.Panel();
            this.editDirectoryRenameScheme = new System.Windows.Forms.TextBox();
            this.labelDirectoryRenameScheme = new System.Windows.Forms.Label();
            this.labelCurrentDirectory = new System.Windows.Forms.Label();
            this.editCurrentDirectory = new System.Windows.Forms.TextBox();
            this.buttonCurrentDirectory = new System.Windows.Forms.Button();
            this.labelResultingString = new System.Windows.Forms.Label();
            this.FileInfoView2 = new BrightIdeasSoftware.DataListView();
            ((System.ComponentModel.ISupportInitialize)(this.FileInfoView2)).BeginInit();
            this.SuspendLayout();
            // 
            // buttonRenameFolder
            // 
            this.buttonRenameFolder.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.buttonRenameFolder.Location = new System.Drawing.Point(1049, 768);
            this.buttonRenameFolder.Name = "buttonRenameFolder";
            this.buttonRenameFolder.Size = new System.Drawing.Size(107, 23);
            this.buttonRenameFolder.TabIndex = 0;
            this.buttonRenameFolder.Text = "RenameFolder";
            this.buttonRenameFolder.UseVisualStyleBackColor = true;
            this.buttonRenameFolder.Click += new System.EventHandler(this.button1_Click);
            // 
            // FileInfoView
            // 
            this.FileInfoView.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.FileInfoView.FullRowSelect = true;
            this.FileInfoView.Location = new System.Drawing.Point(129, 33);
            this.FileInfoView.Name = "FileInfoView";
            this.FileInfoView.ShowItemToolTips = true;
            this.FileInfoView.Size = new System.Drawing.Size(939, 713);
            this.FileInfoView.TabIndex = 1;
            this.FileInfoView.UseCompatibleStateImageBehavior = false;
            this.FileInfoView.Visible = false;
            this.FileInfoView.ColumnClick += new System.Windows.Forms.ColumnClickEventHandler(this.FileInfoView_SortColumn);
            this.FileInfoView.ItemSelectionChanged += new System.Windows.Forms.ListViewItemSelectionChangedEventHandler(this.FileInfoView_ShowSelection);
            this.FileInfoView.DoubleClick += new System.EventHandler(this.FileInfoView_ShowInfo);
            // 
            // TagInfoPanel
            // 
            this.TagInfoPanel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.TagInfoPanel.Location = new System.Drawing.Point(1213, 33);
            this.TagInfoPanel.Name = "TagInfoPanel";
            this.TagInfoPanel.Size = new System.Drawing.Size(506, 762);
            this.TagInfoPanel.TabIndex = 2;
            // 
            // editDirectoryRenameScheme
            // 
            this.editDirectoryRenameScheme.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.editDirectoryRenameScheme.Location = new System.Drawing.Point(217, 770);
            this.editDirectoryRenameScheme.Name = "editDirectoryRenameScheme";
            this.editDirectoryRenameScheme.Size = new System.Drawing.Size(813, 20);
            this.editDirectoryRenameScheme.TabIndex = 3;
            // 
            // labelDirectoryRenameScheme
            // 
            this.labelDirectoryRenameScheme.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.labelDirectoryRenameScheme.AutoSize = true;
            this.labelDirectoryRenameScheme.Location = new System.Drawing.Point(36, 773);
            this.labelDirectoryRenameScheme.Name = "labelDirectoryRenameScheme";
            this.labelDirectoryRenameScheme.Size = new System.Drawing.Size(134, 13);
            this.labelDirectoryRenameScheme.TabIndex = 4;
            this.labelDirectoryRenameScheme.Text = "Directory Rename Scheme";
            // 
            // labelCurrentDirectory
            // 
            this.labelCurrentDirectory.AutoSize = true;
            this.labelCurrentDirectory.Location = new System.Drawing.Point(36, 10);
            this.labelCurrentDirectory.Name = "labelCurrentDirectory";
            this.labelCurrentDirectory.Size = new System.Drawing.Size(84, 13);
            this.labelCurrentDirectory.TabIndex = 7;
            this.labelCurrentDirectory.Text = "Current directory";
            // 
            // editCurrentDirectory
            // 
            this.editCurrentDirectory.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.editCurrentDirectory.Location = new System.Drawing.Point(217, 7);
            this.editCurrentDirectory.Name = "editCurrentDirectory";
            this.editCurrentDirectory.Size = new System.Drawing.Size(813, 20);
            this.editCurrentDirectory.TabIndex = 6;
            // 
            // buttonCurrentDirectory
            // 
            this.buttonCurrentDirectory.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.buttonCurrentDirectory.Location = new System.Drawing.Point(1049, 5);
            this.buttonCurrentDirectory.Name = "buttonCurrentDirectory";
            this.buttonCurrentDirectory.Size = new System.Drawing.Size(107, 23);
            this.buttonCurrentDirectory.TabIndex = 5;
            this.buttonCurrentDirectory.Text = "Change Directory";
            this.buttonCurrentDirectory.UseVisualStyleBackColor = true;
            this.buttonCurrentDirectory.Click += new System.EventHandler(this.buttonCurrentDirectory_Click);
            // 
            // labelResultingString
            // 
            this.labelResultingString.AutoSize = true;
            this.labelResultingString.Location = new System.Drawing.Point(217, 797);
            this.labelResultingString.Name = "labelResultingString";
            this.labelResultingString.Size = new System.Drawing.Size(35, 13);
            this.labelResultingString.TabIndex = 8;
            this.labelResultingString.Text = "label1";
            // 
            // FileInfoView2
            // 
            this.FileInfoView2.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.FileInfoView2.CellEditUseWholeCell = false;
            this.FileInfoView2.DataSource = null;
            this.FileInfoView2.FullRowSelect = true;
            this.FileInfoView2.Location = new System.Drawing.Point(217, 33);
            this.FileInfoView2.Name = "FileInfoView2";
            this.FileInfoView2.ShowItemToolTips = true;
            this.FileInfoView2.Size = new System.Drawing.Size(939, 729);
            this.FileInfoView2.TabIndex = 9;
            this.FileInfoView2.UseCompatibleStateImageBehavior = false;
            this.FileInfoView2.View = System.Windows.Forms.View.Details;
            this.FileInfoView2.Click += new System.EventHandler(this.FileInfoView2_Click);
            // 
            // formMain
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1750, 816);
            this.Controls.Add(this.FileInfoView2);
            this.Controls.Add(this.labelResultingString);
            this.Controls.Add(this.labelCurrentDirectory);
            this.Controls.Add(this.editCurrentDirectory);
            this.Controls.Add(this.buttonCurrentDirectory);
            this.Controls.Add(this.labelDirectoryRenameScheme);
            this.Controls.Add(this.editDirectoryRenameScheme);
            this.Controls.Add(this.TagInfoPanel);
            this.Controls.Add(this.FileInfoView);
            this.Controls.Add(this.buttonRenameFolder);
            this.Name = "formMain";
            this.Text = "Form1";
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.formMain_FormClosing);
            this.Shown += new System.EventHandler(this.formMain_Shown);
            ((System.ComponentModel.ISupportInitialize)(this.FileInfoView2)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Button buttonRenameFolder;
        public System.Windows.Forms.ListView FileInfoView;
        private System.Windows.Forms.Panel TagInfoPanel;
        private System.Windows.Forms.TextBox editDirectoryRenameScheme;
        private System.Windows.Forms.Label labelDirectoryRenameScheme;
        private System.Windows.Forms.Label labelCurrentDirectory;
        private System.Windows.Forms.TextBox editCurrentDirectory;
        private System.Windows.Forms.Button buttonCurrentDirectory;
        private System.Windows.Forms.Label labelResultingString;
        private BrightIdeasSoftware.DataListView FileInfoView2;
    }
}

