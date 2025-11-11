namespace Exam
{
    partial class DecryptedCodeForm
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
            this.label1 = new System.Windows.Forms.Label();
            this.btn = new System.Windows.Forms.Button();
            this.textcode = new System.Windows.Forms.TextBox();
            this.SuspendLayout();
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Font = new System.Drawing.Font("Microsoft YaHei", 9F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label1.Location = new System.Drawing.Point(490, 268);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(148, 25);
            this.label1.TabIndex = 8;
            this.label1.Text = "Enter The Code";
            // 
            // btn
            // 
            this.btn.Location = new System.Drawing.Point(582, 352);
            this.btn.Name = "btn";
            this.btn.Size = new System.Drawing.Size(112, 32);
            this.btn.TabIndex = 7;
            this.btn.Text = "Get Data";
            this.btn.UseVisualStyleBackColor = true;
            // 
            // textcode
            // 
            this.textcode.Location = new System.Drawing.Point(495, 310);
            this.textcode.Name = "textcode";
            this.textcode.Size = new System.Drawing.Size(300, 26);
            this.textcode.TabIndex = 6;
            // 
            // DecryptedCodeForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(9F, 20F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1285, 652);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.btn);
            this.Controls.Add(this.textcode);
            this.Name = "DecryptedCodeForm";
            this.Text = "DecryptedCodeForm";
            this.Load += new System.EventHandler(this.DecryptedCodeForm_Load);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Button btn;
        private System.Windows.Forms.TextBox textcode;
    }
}