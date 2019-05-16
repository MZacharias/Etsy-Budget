namespace EstyBudget
{
    partial class FrmEtsyBudget
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
            this.dtMonth = new System.Windows.Forms.DateTimePicker();
            this.lblMonth = new System.Windows.Forms.Label();
            this.btnGo = new System.Windows.Forms.Button();
            this.lblProgress = new System.Windows.Forms.Label();
            this.SuspendLayout();
            // 
            // dtMonth
            // 
            this.dtMonth.CustomFormat = "MM/yyyy";
            this.dtMonth.Format = System.Windows.Forms.DateTimePickerFormat.Custom;
            this.dtMonth.Location = new System.Drawing.Point(48, 40);
            this.dtMonth.Name = "dtMonth";
            this.dtMonth.Size = new System.Drawing.Size(119, 22);
            this.dtMonth.TabIndex = 0;
            this.dtMonth.Value = new System.DateTime(2017, 11, 27, 0, 0, 0, 0);
            // 
            // lblMonth
            // 
            this.lblMonth.AutoSize = true;
            this.lblMonth.Location = new System.Drawing.Point(48, 17);
            this.lblMonth.Name = "lblMonth";
            this.lblMonth.Size = new System.Drawing.Size(51, 17);
            this.lblMonth.TabIndex = 1;
            this.lblMonth.Text = "Month:";
            // 
            // btnGo
            // 
            this.btnGo.Location = new System.Drawing.Point(187, 17);
            this.btnGo.Name = "btnGo";
            this.btnGo.Size = new System.Drawing.Size(116, 45);
            this.btnGo.TabIndex = 4;
            this.btnGo.Text = "Go!";
            this.btnGo.UseVisualStyleBackColor = true;
            this.btnGo.Click += new System.EventHandler(this.btnGo_Click);
            // 
            // lblProgress
            // 
            this.lblProgress.BackColor = System.Drawing.SystemColors.Control;
            this.lblProgress.ForeColor = System.Drawing.Color.Red;
            this.lblProgress.Location = new System.Drawing.Point(187, 69);
            this.lblProgress.Name = "lblProgress";
            this.lblProgress.Size = new System.Drawing.Size(116, 23);
            this.lblProgress.TabIndex = 5;
            this.lblProgress.Text = "Retreiving data...";
            this.lblProgress.Visible = false;
            // 
            // FrmEtsyBudget
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 16F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(347, 99);
            this.Controls.Add(this.lblProgress);
            this.Controls.Add(this.btnGo);
            this.Controls.Add(this.lblMonth);
            this.Controls.Add(this.dtMonth);
            this.Name = "FrmEtsyBudget";
            this.Text = "Etsy Budget";
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.DateTimePicker dtMonth;
        private System.Windows.Forms.Label lblMonth;
        private System.Windows.Forms.Button btnGo;
        private System.Windows.Forms.Label lblProgress;
    }
}

