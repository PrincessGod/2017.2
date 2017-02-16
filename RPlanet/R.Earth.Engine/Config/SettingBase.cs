
namespace R.Earth.Config
{
    public class SettingBase
    {
        protected string _fileName;
        
        public string ConfigName 
        { 
            get { return this._fileName; } 
            set { this._fileName = value; }
        }

        public virtual void Load(string path) { }
  
        public virtual void Save(string path) { }
    }
}
