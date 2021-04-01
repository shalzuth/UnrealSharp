
namespace UnrealSharp
{
    partial class UnrealSharp
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
            this.inspectProcess = new System.Windows.Forms.Button();
            this.dump = new System.Windows.Forms.Button();
            this.actorList = new System.Windows.Forms.ListBox();
            this.actorInfo = new System.Windows.Forms.ListBox();
            this.SuspendLayout();
            // 
            // inspectProcess
            // 
            this.inspectProcess.Location = new System.Drawing.Point(13, 13);
            this.inspectProcess.Name = "inspectProcess";
            this.inspectProcess.Size = new System.Drawing.Size(112, 23);
            this.inspectProcess.TabIndex = 2;
            this.inspectProcess.Text = "Inspect Process";
            this.inspectProcess.UseVisualStyleBackColor = true;
            this.inspectProcess.Click += new System.EventHandler(this.inspectProcess_Click);
            // 
            // dump
            // 
            this.dump.Location = new System.Drawing.Point(132, 12);
            this.dump.Name = "dump";
            this.dump.Size = new System.Drawing.Size(75, 23);
            this.dump.TabIndex = 3;
            this.dump.Text = "Dump SDK";
            this.dump.UseVisualStyleBackColor = true;
            this.dump.Click += new System.EventHandler(this.dump_Click);
            // 
            // actorList
            // 
            this.actorList.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left)));
            this.actorList.FormattingEnabled = true;
            this.actorList.ItemHeight = 15;
            this.actorList.Location = new System.Drawing.Point(12, 61);
            this.actorList.Name = "actorList";
            this.actorList.Size = new System.Drawing.Size(372, 379);
            this.actorList.TabIndex = 7;
            this.actorList.SelectedIndexChanged += new System.EventHandler(this.actorList_SelectedIndexChanged);
            // 
            // actorInfo
            // 
            this.actorInfo.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.actorInfo.FormattingEnabled = true;
            this.actorInfo.ItemHeight = 15;
            this.actorInfo.Location = new System.Drawing.Point(390, 16);
            this.actorInfo.Name = "actorInfo";
            this.actorInfo.Size = new System.Drawing.Size(275, 424);
            this.actorInfo.TabIndex = 8;
            this.actorInfo.SelectedIndexChanged += new System.EventHandler(this.actorInfo_SelectedIndexChanged);
            // 
            // UnrealSharp
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(677, 451);
            this.Controls.Add(this.actorInfo);
            this.Controls.Add(this.actorList);
            this.Controls.Add(this.dump);
            this.Controls.Add(this.inspectProcess);
            this.Name = "UnrealSharp";
            this.Text = "UnrealSharp [by shalzuth]";
            this.ResumeLayout(false);

        }

        #endregion
        private System.Windows.Forms.Button inspectProcess;
        private System.Windows.Forms.Button dump;
        private System.Windows.Forms.ListBox actorList;
        private System.Windows.Forms.ListBox actorInfo;
    }
}

