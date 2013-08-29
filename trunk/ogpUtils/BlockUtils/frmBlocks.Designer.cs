namespace ogpUtils
{
    partial class frmBlks
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(frmBlks));
            this.dgv1 = new System.Windows.Forms.DataGridView();
            this.blockName = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.Explodable = new System.Windows.Forms.DataGridViewCheckBoxColumn();
            this.UniformScale = new System.Windows.Forms.DataGridViewCheckBoxColumn();
            this.blockId = new System.Windows.Forms.DataGridViewTextBoxColumn();
            ((System.ComponentModel.ISupportInitialize)(this.dgv1)).BeginInit();
            this.SuspendLayout();
            // 
            // dgv1
            // 
            this.dgv1.AllowUserToAddRows = false;
            this.dgv1.AllowUserToDeleteRows = false;
            this.dgv1.AllowUserToResizeRows = false;
            this.dgv1.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
                        | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.dgv1.BackgroundColor = System.Drawing.SystemColors.Window;
            this.dgv1.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D;
            this.dgv1.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.dgv1.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] {
            this.blockName,
            this.Explodable,
            this.UniformScale,
            this.blockId});
            this.dgv1.Location = new System.Drawing.Point(0, 0);
            this.dgv1.MultiSelect = false;
            this.dgv1.Name = "dgv1";
            this.dgv1.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
            this.dgv1.ShowCellToolTips = false;
            this.dgv1.Size = new System.Drawing.Size(397, 363);
            this.dgv1.TabIndex = 1;
            this.dgv1.CellValueChanged += new System.Windows.Forms.DataGridViewCellEventHandler(this.dgv1_CellValueChanged);
            // 
            // blockName
            // 
            this.blockName.HeaderText = "Имя блока";
            this.blockName.MinimumWidth = 50;
            this.blockName.Name = "blockName";
            this.blockName.Width = 250;
            // 
            // Explodable
            // 
            this.Explodable.HeaderText = "Взры- ваемый";
            this.Explodable.MinimumWidth = 16;
            this.Explodable.Name = "Explodable";
            this.Explodable.Resizable = System.Windows.Forms.DataGridViewTriState.True;
            this.Explodable.SortMode = System.Windows.Forms.DataGridViewColumnSortMode.Automatic;
            this.Explodable.Width = 50;
            // 
            // UniformScale
            // 
            this.UniformScale.HeaderText = "Равн. масшт.";
            this.UniformScale.MinimumWidth = 16;
            this.UniformScale.Name = "UniformScale";
            this.UniformScale.Resizable = System.Windows.Forms.DataGridViewTriState.True;
            this.UniformScale.SortMode = System.Windows.Forms.DataGridViewColumnSortMode.Automatic;
            this.UniformScale.Width = 50;
            // 
            // blockId
            // 
            this.blockId.HeaderText = "id";
            this.blockId.Name = "blockId";
            this.blockId.Visible = false;
            this.blockId.Width = 50;
            // 
            // frmBlks
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(397, 363);
            this.Controls.Add(this.dgv1);
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "frmBlks";
            this.ShowIcon = false;
            this.ShowInTaskbar = false;
            this.Text = "Список блоков";
            ((System.ComponentModel.ISupportInitialize)(this.dgv1)).EndInit();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.DataGridView dgv1;
        private System.Windows.Forms.DataGridViewTextBoxColumn blockName;
        private System.Windows.Forms.DataGridViewCheckBoxColumn Explodable;
        private System.Windows.Forms.DataGridViewCheckBoxColumn UniformScale;
        private System.Windows.Forms.DataGridViewTextBoxColumn blockId;

    }
}