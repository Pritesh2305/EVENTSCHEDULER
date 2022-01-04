namespace EVENTSCHEDULER
{
    partial class frmeventserver
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(frmeventserver));
            this.pnlmain = new System.Windows.Forms.Panel();
            this.txtinfo = new System.Windows.Forms.RichTextBox();
            this.btnrun = new System.Windows.Forms.Button();
            this.label1 = new System.Windows.Forms.Label();
            this.btnstop = new System.Windows.Forms.Button();
            this.tmrevent = new System.Windows.Forms.Timer(this.components);
            this.tmrsms = new System.Windows.Forms.Timer(this.components);
            this.tmrread = new System.Windows.Forms.Timer(this.components);
            this.pnlmain.SuspendLayout();
            this.SuspendLayout();
            // 
            // pnlmain
            // 
            this.pnlmain.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.pnlmain.Controls.Add(this.txtinfo);
            this.pnlmain.Location = new System.Drawing.Point(11, 49);
            this.pnlmain.Name = "pnlmain";
            this.pnlmain.Size = new System.Drawing.Size(522, 385);
            this.pnlmain.TabIndex = 0;
            // 
            // txtinfo
            // 
            this.txtinfo.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.txtinfo.Dock = System.Windows.Forms.DockStyle.Fill;
            this.txtinfo.Location = new System.Drawing.Point(0, 0);
            this.txtinfo.Name = "txtinfo";
            this.txtinfo.ScrollBars = System.Windows.Forms.RichTextBoxScrollBars.ForcedVertical;
            this.txtinfo.Size = new System.Drawing.Size(520, 383);
            this.txtinfo.TabIndex = 0;
            this.txtinfo.Text = "";
            // 
            // btnrun
            // 
            this.btnrun.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.75F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.btnrun.ForeColor = System.Drawing.Color.Green;
            this.btnrun.Location = new System.Drawing.Point(12, 9);
            this.btnrun.Name = "btnrun";
            this.btnrun.Size = new System.Drawing.Size(75, 29);
            this.btnrun.TabIndex = 1;
            this.btnrun.Text = "&START";
            this.btnrun.UseVisualStyleBackColor = true;
            this.btnrun.Click += new System.EventHandler(this.btnrun_Click);
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label1.ForeColor = System.Drawing.Color.Maroon;
            this.label1.Location = new System.Drawing.Point(175, 12);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(198, 20);
            this.label1.TabIndex = 2;
            this.label1.Text = "SCHEDULER SERVER";
            // 
            // btnstop
            // 
            this.btnstop.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.75F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.btnstop.ForeColor = System.Drawing.Color.Red;
            this.btnstop.Location = new System.Drawing.Point(459, 8);
            this.btnstop.Name = "btnstop";
            this.btnstop.Size = new System.Drawing.Size(75, 29);
            this.btnstop.TabIndex = 1;
            this.btnstop.Text = "ST&OP";
            this.btnstop.UseVisualStyleBackColor = true;
            this.btnstop.Click += new System.EventHandler(this.btnstop_Click);
            // 
            // tmrevent
            // 
            this.tmrevent.Tick += new System.EventHandler(this.tmrevent_Tick);
            // 
            // tmrsms
            // 
            this.tmrsms.Tick += new System.EventHandler(this.tmrsms_Tick);
            // 
            // tmrread
            // 
            this.tmrread.Tick += new System.EventHandler(this.tmrread_Tick);
            // 
            // frmeventserver
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(197)))), ((int)(((byte)(224)))), ((int)(((byte)(245)))));
            this.ClientSize = new System.Drawing.Size(543, 442);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.btnstop);
            this.Controls.Add(this.btnrun);
            this.Controls.Add(this.pnlmain);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.KeyPreview = true;
            this.MaximizeBox = false;
            this.Name = "frmeventserver";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "DATA SCHEDULER SERVER";
            this.Load += new System.EventHandler(this.frmeventserver_Load);
            this.pnlmain.ResumeLayout(false);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Panel pnlmain;
        private System.Windows.Forms.RichTextBox txtinfo;
        private System.Windows.Forms.Button btnrun;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Button btnstop;
        private System.Windows.Forms.Timer tmrevent;
        private System.Windows.Forms.Timer tmrsms;
        private System.Windows.Forms.Timer tmrread;
    }
}

