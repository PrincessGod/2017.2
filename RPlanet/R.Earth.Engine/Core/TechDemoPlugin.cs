using R.Earth.UI;

namespace R.Earth.Plugin
{
    public abstract class TechDemoPlugin
    {
        private WorldViewer m_viewer;
        public WorldViewer Viewer { get { return this.m_viewer; } }

        public virtual void Load() { }
        public virtual void UnLoad() { }

        public void PluginLoad(WorldViewer viewer)
        {
            this.m_viewer = viewer;
            this.Load();
        }

    }
}