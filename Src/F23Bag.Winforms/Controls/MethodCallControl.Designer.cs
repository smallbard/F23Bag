namespace F23Bag.Winforms.Controls
{
    partial class MethodCallControl
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
            this.btnMethodCall = new System.Windows.Forms.Button();
            this.SuspendLayout();
            // 
            // btnMethodCall
            // 
            this.btnMethodCall.AutoSize = true;
            this.btnMethodCall.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.btnMethodCall.Location = new System.Drawing.Point(3, 3);
            this.btnMethodCall.Name = "btnMethodCall";
            this.btnMethodCall.Size = new System.Drawing.Size(41, 23);
            this.btnMethodCall.TabIndex = 0;
            this.btnMethodCall.Text = "........";
            this.btnMethodCall.UseVisualStyleBackColor = true;
            // 
            // ParameterLessMethodCallControl
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.AutoSize = true;
            this.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.Controls.Add(this.btnMethodCall);
            this.Name = "ParameterLessMethodCallControl";
            this.Size = new System.Drawing.Size(47, 29);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Button btnMethodCall;
    }
}
