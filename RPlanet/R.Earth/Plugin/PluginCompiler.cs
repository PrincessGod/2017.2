using System;
using System.Collections.Generic;
using System.Text;
using System.Reflection;

using R.Earth.UI;

namespace R.Earth.Plugin
{
    public class PluginCompiler
    {
        private List<TechDemoPlugin> m_PluginList = new List<TechDemoPlugin>();
        private WorldViewer Viewer;

        public PluginCompiler(WorldViewer viewer)
        {
            this.Viewer = viewer;
        }

        internal void LoadPlugin()
        {
            Assembly assmebly = Assembly.GetExecutingAssembly();
            Type[] typelist = assmebly.GetTypes();
            Type PLUGINTYPE = typeof(TechDemoPlugin);
            foreach (Type demo in typelist)
            {
                if (demo.IsSubclassOf(PLUGINTYPE))
                {
                    this.m_PluginList.Add((TechDemoPlugin)assmebly.CreateInstance(demo.ToString()));
                }
            }
        }

        internal void LoadStartUpPlugin()
        {
            foreach(TechDemoPlugin it in this.m_PluginList)
            {
                it.PluginLoad(this.Viewer);
            }
        }
    }
}
