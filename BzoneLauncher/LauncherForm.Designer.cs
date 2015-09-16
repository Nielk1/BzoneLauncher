namespace BzoneLauncher
{
    partial class LauncherForm
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
            this.components = new System.ComponentModel.Container();
            this.webControl1 = new Awesomium.Windows.Forms.WebControl(this.components);
            this.openFileDialog1 = new System.Windows.Forms.OpenFileDialog();
            this.tmrFixZoom = new System.Windows.Forms.Timer(this.components);
            this.SuspendLayout();
            // 
            // webControl1
            // 
            this.webControl1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.webControl1.Location = new System.Drawing.Point(0, 0);
            this.webControl1.Size = new System.Drawing.Size(721, 459);
            this.webControl1.Source = new System.Uri("about:error", System.UriKind.Absolute);
            this.webControl1.TabIndex = 0;
            this.webControl1.InitializeView += new Awesomium.Core.WebViewEventHandler(this.Awesomium_Windows_Forms_WebControl_InitializeView);
            this.webControl1.NativeViewInitialized += new Awesomium.Core.WebViewEventHandler(this.Awesomium_Windows_Forms_WebControl_NativeViewInitialized);
            this.webControl1.AddressChanged += new Awesomium.Core.UrlEventHandler(this.Awesomium_Windows_Forms_WebControl_AddressChanged);
            this.webControl1.ConsoleMessage += new Awesomium.Core.ConsoleMessageEventHandler(this.Awesomium_Windows_Forms_WebControl_ConsoleMessage);
            // 
            // openFileDialog1
            // 
            this.openFileDialog1.FileName = "openFileDialog1";
            // 
            // tmrFixZoom
            // 
            this.tmrFixZoom.Enabled = true;
            this.tmrFixZoom.Tick += new System.EventHandler(this.tmrFixZoom_Tick);
            // 
            // MainForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.Color.Black;
            this.ClientSize = new System.Drawing.Size(721, 459);
            this.Controls.Add(this.webControl1);
            this.Name = "MainForm";
            this.Text = "Battlezone Launcher";
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.LauncherForm_FormClosing);
            this.Load += new System.EventHandler(this.LauncherForm_Load);
            this.ResumeLayout(false);

        }

        #endregion

        private Awesomium.Windows.Forms.WebControl webControl1;
        private System.Windows.Forms.OpenFileDialog openFileDialog1;
        private System.Windows.Forms.Timer tmrFixZoom;
    }
}

