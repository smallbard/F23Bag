namespace F23Bag.Winforms.Controls
{
    partial class StringControl
    {
        /// <summary> 
        /// Variable nécessaire au concepteur.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary> 
        /// Nettoyage des ressources utilisées.
        /// </summary>
        /// <param name="disposing">true si les ressources managées doivent être supprimées ; sinon, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Code généré par le Concepteur de composants

        /// <summary> 
        /// Méthode requise pour la prise en charge du concepteur - ne modifiez pas 
        /// le contenu de cette méthode avec l'éditeur de code.
        /// </summary>
        private void InitializeComponent()
        {
            this.lblLabel = new System.Windows.Forms.Label();
            this.txtValue = new System.Windows.Forms.TextBox();
            this._validationIcon = new System.Windows.Forms.PictureBox();
            ((System.ComponentModel.ISupportInitialize)(this._validationIcon)).BeginInit();
            this.SuspendLayout();
            // 
            // lblLabel
            // 
            this.lblLabel.Location = new System.Drawing.Point(4, 4);
            this.lblLabel.Name = "lblLabel";
            this.lblLabel.Size = new System.Drawing.Size(200, 23);
            this.lblLabel.TabIndex = 0;
            this.lblLabel.Text = ".....";
            // 
            // txtValue
            // 
            this.txtValue.Location = new System.Drawing.Point(210, 5);
            this.txtValue.Name = "txtValue";
            this.txtValue.Size = new System.Drawing.Size(300, 20);
            this.txtValue.TabIndex = 1;
            // 
            // _validationIcon
            // 
            this._validationIcon.Location = new System.Drawing.Point(517, 7);
            this._validationIcon.Name = "_validationIcon";
            this._validationIcon.Size = new System.Drawing.Size(16, 16);
            this._validationIcon.TabIndex = 2;
            this._validationIcon.TabStop = false;
            // 
            // StringControl
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this._validationIcon);
            this.Controls.Add(this.txtValue);
            this.Controls.Add(this.lblLabel);
            this.Name = "StringControl";
            this.Size = new System.Drawing.Size(540, 35);
            ((System.ComponentModel.ISupportInitialize)(this._validationIcon)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Label lblLabel;
        private System.Windows.Forms.TextBox txtValue;
        private System.Windows.Forms.PictureBox _validationIcon;
    }
}
