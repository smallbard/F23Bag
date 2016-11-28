namespace F23Bag.Winforms.Controls
{
    partial class DataGridControl
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
            this.gridView = new System.Windows.Forms.DataGridView();
            this.flowActions = new System.Windows.Forms.FlowLayoutPanel();
            ((System.ComponentModel.ISupportInitialize)(this.gridView)).BeginInit();
            this.SuspendLayout();
            // 
            // gridView
            // 
            this.gridView.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.gridView.Dock = System.Windows.Forms.DockStyle.Fill;
            this.gridView.Location = new System.Drawing.Point(0, 44);
            this.gridView.Name = "gridView";
            this.gridView.Size = new System.Drawing.Size(540, 329);
            this.gridView.TabIndex = 0;
            // 
            // flowActions
            // 
            this.flowActions.AutoScroll = true;
            this.flowActions.Dock = System.Windows.Forms.DockStyle.Top;
            this.flowActions.Location = new System.Drawing.Point(0, 0);
            this.flowActions.Name = "flowActions";
            this.flowActions.Size = new System.Drawing.Size(540, 44);
            this.flowActions.TabIndex = 1;
            this.flowActions.WrapContents = false;
            // 
            // DataGridControl
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.gridView);
            this.Controls.Add(this.flowActions);
            this.Name = "DataGridControl";
            this.Size = new System.Drawing.Size(540, 373);
            ((System.ComponentModel.ISupportInitialize)(this.gridView)).EndInit();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.DataGridView gridView;
        private System.Windows.Forms.FlowLayoutPanel flowActions;
    }
}
