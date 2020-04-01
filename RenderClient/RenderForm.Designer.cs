namespace RenderClient
{

    partial class RenderForm
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
            this.glControl = new OpenTK.GLControl();
            this.controlLabel = new System.Windows.Forms.Label();
            this.SuspendLayout();
            // 
            // glControl
            // 
            this.glControl.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
            | System.Windows.Forms.AnchorStyles.Left)
            | System.Windows.Forms.AnchorStyles.Right)));
            this.glControl.BackColor = System.Drawing.Color.Black;
            this.glControl.Location = new System.Drawing.Point(8, 8);
            this.glControl.Margin = new System.Windows.Forms.Padding(0);
            this.glControl.Name = "glControl";
            this.glControl.Size = new System.Drawing.Size(344, 624);
            this.glControl.TabIndex = 0;
            this.glControl.VSync = false; // TODO: Setting.IsVSync;
            // 
            // RenderForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(360, 640);
            this.Controls.Add(this.controlLabel);
            this.Controls.Add(this.glControl);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.None;
            this.Name = "RenderForm";
            this.RightToLeft = System.Windows.Forms.RightToLeft.No;
            this.TopMost = true;
            this.ResumeLayout(false);
            // 
            // controlLabel
            // 
            this.controlLabel.BackColor = System.Drawing.SystemColors.Highlight;
            this.controlLabel.Dock = System.Windows.Forms.DockStyle.Top;
            this.controlLabel.Font = new System.Drawing.Font("Times New Roman", 13.8F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.controlLabel.ForeColor = System.Drawing.Color.White;
            this.controlLabel.Location = new System.Drawing.Point(0, 0);
            this.controlLabel.Margin = new System.Windows.Forms.Padding(0);
            this.controlLabel.Name = "controlLabel";
            this.controlLabel.Size = new System.Drawing.Size(360, 27);
            this.controlLabel.TabIndex = 1;
            this.controlLabel.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
        }

        #endregion

        private OpenTK.GLControl glControl;
        private System.Windows.Forms.Label controlLabel;
    }
}