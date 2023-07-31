namespace UEInspector
{
    partial class Inspector
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
            actorList = new ListBox();
            dump = new Button();
            actorInfo = new ListBox();
            dumpSDK = new Button();
            SuspendLayout();
            // 
            // actorList
            // 
            actorList.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left;
            actorList.FormattingEnabled = true;
            actorList.ItemHeight = 15;
            actorList.Location = new Point(17, 55);
            actorList.Name = "actorList";
            actorList.Size = new Size(461, 604);
            actorList.TabIndex = 0;
            actorList.SelectedIndexChanged += actorList_SelectedIndexChanged;
            // 
            // dump
            // 
            dump.Location = new Point(17, 15);
            dump.Name = "dump";
            dump.Size = new Size(115, 23);
            dump.TabIndex = 1;
            dump.Text = "DumpScene";
            dump.UseVisualStyleBackColor = true;
            dump.Click += dump_Click;
            // 
            // actorInfo
            // 
            actorInfo.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            actorInfo.FormattingEnabled = true;
            actorInfo.ItemHeight = 15;
            actorInfo.Location = new Point(484, 12);
            actorInfo.Name = "actorInfo";
            actorInfo.Size = new Size(678, 649);
            actorInfo.TabIndex = 2;
            actorInfo.SelectedIndexChanged += actorInfo_SelectedIndexChanged;
            // 
            // dumpSDK
            // 
            dumpSDK.Location = new Point(146, 19);
            dumpSDK.Name = "dumpSDK";
            dumpSDK.Size = new Size(75, 23);
            dumpSDK.TabIndex = 3;
            dumpSDK.Text = "DumpSDK";
            dumpSDK.UseVisualStyleBackColor = true;
            dumpSDK.Click += dumpSDK_Click;
            // 
            // Inspector
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(1162, 683);
            Controls.Add(dumpSDK);
            Controls.Add(actorInfo);
            Controls.Add(dump);
            Controls.Add(actorList);
            Name = "Inspector";
            Text = "Unreal Engine Inspector";
            ResumeLayout(false);
        }

        #endregion

        private ListBox actorList;
        private Button dump;
        private ListBox actorInfo;
        private Button dumpSDK;
    }
}