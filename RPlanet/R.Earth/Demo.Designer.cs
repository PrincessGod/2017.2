namespace R.Earth
{
    partial class Demo
    {
        /// <summary>
        /// 必需的设计器变量。
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// 清理所有正在使用的资源。
        /// </summary>
        /// <param name="disposing">如果应释放托管资源，为 true；否则为 false。</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows 窗体设计器生成的代码

        /// <summary>
        /// 设计器支持所需的方法 - 不要
        /// 使用代码编辑器修改此方法的内容。
        /// </summary>
        private void InitializeComponent()
        {
            this.worldViewer1 = new R.Earth.UI.WorldViewer();
            this.SuspendLayout();
            // 
            // worldViewer1
            // 
            this.worldViewer1.CurrentWorld = null;
            this.worldViewer1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.worldViewer1.Font = new System.Drawing.Font("YouYuan", 10.5F, System.Drawing.FontStyle.Italic, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.worldViewer1.IsRenderDisabled = false;
            this.worldViewer1.Location = new System.Drawing.Point(0, 0);
            this.worldViewer1.Name = "worldViewer1";
            this.worldViewer1.Size = new System.Drawing.Size(1024, 768);
            this.worldViewer1.TabIndex = 0;
            this.worldViewer1.Text = "worldViewer1";
            // 
            // Demo
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 12F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Size = new System.Drawing.Size(1024, 768);
            this.Controls.Add(this.worldViewer1);
            this.Name = "Demo";
            this.Text = "Form1";
            this.WindowState = System.Windows.Forms.FormWindowState.Maximized;
            this.Load += new System.EventHandler(this.Demo_Load);
            this.ResumeLayout(false);

        }

        #endregion

        private R.Earth.UI.WorldViewer worldViewer1;
    }
}

