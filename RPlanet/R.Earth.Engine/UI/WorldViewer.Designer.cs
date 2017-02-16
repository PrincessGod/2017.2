namespace R.Earth.UI
{
    partial class WorldViewer
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
            if (disposing)
            {
                if (m_WorkerThread != null && m_WorkerThread.IsAlive)
                {
                    m_WorkerThreadRunning = false;
                    if (!m_WorkerThread.Join(new System.TimeSpan(0, 0, 1)))
                    {
                        m_WorkerThread.Abort();
                    }
                }
                if (m_World != null)
                {
                    m_World.Dispose();
                    m_World = null;
                }
                if (this.m_drawArgs != null)
                {
                    this.m_drawArgs.Dispose();
                    this.m_drawArgs = null;
                }

                //TODO: Ensure user is not held up here
                try
                {
                    m_Device3d.Dispose();
                }
                catch { }
            }

            base.Dispose(disposing);
            System.GC.SuppressFinalize(this);
        }

        #region 组件设计器生成的代码

        /// <summary> 
        /// 设计器支持所需的方法 - 不要
        /// 使用代码编辑器修改此方法的内容。
        /// </summary>
        private void InitializeComponent()
        {           
            // 
            // WorldViewer
            // 
   
            this.Name = "WorldViewer";
            this.Size = new System.Drawing.Size(697, 331);    

        }

        #endregion
    }
}
