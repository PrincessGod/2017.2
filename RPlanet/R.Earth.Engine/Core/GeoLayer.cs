using System;
using System.Drawing;

namespace R.Earth
{
    public interface IGeoLayer 
    {
        string Name { get;set;}
        bool IsInitialized { get;}
        bool IsUnLoad { get;set;}
        bool IsVisible { get;set;}        
    }

    public class ExportInfo
    {
        public double dMinLat = double.MaxValue;
        public double dMaxLat = double.MinValue;
        public double dMinLon = double.MaxValue;
        public double dMaxLon = double.MinValue;

        public int iPixelsX = -1;
        public int iPixelsY = -1;

        public Graphics gr;

        public ExportInfo()
        {
        }
    }
    public abstract class GeoLayer : IGeoLayer
    {
        private string m_name = "Layer";
        private bool _isInitialized = false;
        private bool _isUnload = false;
        private bool _isVisiable = false;
        private byte m_Opacity = 128;

        #region IGeoLayer ≥…‘±

        public GeoLayer(string name)
        {
            this.m_name = name;
        }
        public bool IsInitialized
        {
            get { return this._isInitialized; }
            set { this._isInitialized = value; }
        }

        public bool IsUnLoad
        {
            get
            {
                return this._isUnload;
            }
            set
            {
                this._isUnload = value;
            }
        }

        public bool IsVisible
        {
            get
            {
                return this._isVisiable;
            }
            set
            {
                this._isVisiable = value;
            }
        }

        public virtual void OnInitialize(DrawArgs drawArgs)
        {
            this._isInitialized = true;
        }

        public virtual void OnFrameMove(DrawArgs drawArgs)
        {
            if (!this._isInitialized && this._isVisiable)
            {
                this.OnInitialize(drawArgs);
            }
        }

        public virtual void OnRender(DrawArgs drawArgs)
        {
            if (!this._isVisiable || !this._isInitialized)
            {
                return;
            }
        }

        public abstract void Dispose();
     

        public string Name
        {
            get
            {
                return this.m_name;
            }
            set
            {
                this.m_name = value;
            }
        }

        public virtual Byte Opacity
        {
            get { return this.m_Opacity; }
            set
            {
                this.m_Opacity = value;
            }
        }
        #endregion     
    

        public abstract void UpdateMesh(DrawArgs drawArgs);

        public abstract void OnRenderOrtho(DrawArgs drawArgs);



        public virtual void InitExportInfo(DrawArgs drawArgs, ExportInfo info)
        {
        }

        public virtual void ExportProcess(DrawArgs drawArgs, ExportInfo expInfo)
        {
        }
     
    }

}
