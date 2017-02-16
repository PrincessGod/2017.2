using System.Windows.Forms;

namespace R.Earth.Config
{
    public class EarthSetting : SettingBase
    {
        private static string m_mediaSourecePath = Application.StartupPath + @"/Res/Media/";

        private static string rSourecePath = Application.StartupPath + @"/Res/";
        private static string m_iconSourecePath = Application.StartupPath + @"/Res/Icon/";
        private static string cachepath = Application.StartupPath + @"/Data/Earth/";

        public static string MediaSourcePath
        {
            get { return m_mediaSourecePath; }
            set { m_mediaSourecePath = value; }
        }
        public static string IconSourcePath
        {
            get { return m_iconSourecePath; }
            set { m_iconSourecePath = value; }
        }
        public static string RSourcePath
        {
            get { return rSourecePath; }
            set { rSourecePath = value; }
        }
        public static string CachePath 
        {
            get { return cachepath; } 
            set { cachepath = value; }
        }
    }
}
