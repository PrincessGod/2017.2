using System;
using System.Collections.Generic;
using System.Text;
using R.Earth.Plugin.Widget;
using R.Earth.OverLayer.Element;
using R.Earth.StarField;


namespace R.Earth.Plugin
{
    public class pPlaceWidgets : TechDemoPlugin
    {
        R.Earth.OverLayer.Element.WidgetTextBox TEX;
        WidgetForm m_compass;

        public override void Load()
        {


            m_compass = new WidgetForm("PlaceFinder");
            m_compass.ClientSize = new System.Drawing.Size(320, 120);
            m_compass.Location = new System.Drawing.Point(200, 200);
            m_compass.BackgroundColor = System.Drawing.Color.Wheat;
            m_compass.ParentWidget = this.Viewer.CurrentWorld.OverlayList;

            m_compass.AutoHideHeader = true;
            m_compass.VerticalScrollbarEnabled = true;
            m_compass.HorizontalScrollbarEnabled = true;
            m_compass.BorderEnabled = true;
            m_compass.HeaderEnabled = true;
            m_compass.Visible = false;
            m_compass.OnResizeEvent += new WidgetForm.ResizeHandler(m_compass_OnResizeEvent);

            TEX = new WidgetTextBox();
            TEX.ClientSize = new System.Drawing.Size(50, 50);
            TEX.ClientLocation = new System.Drawing.Point(10, 30);
            TEX.Visible = true;
            TEX.Text = "wHAT'S THIS";
            TEX.ForeColor = System.Drawing.Color.Red;
            TEX.ParentWidget = m_compass;

            m_compass.ChildWidgets.Add(TEX);
            this.Viewer.CurrentWorld.OverlayList.Add(m_compass);

            OverLayer.Control.WidgetMenuButton m_compasstoolbar = new R.Earth.OverLayer.Control.WidgetMenuButton
        ("PlaceFinder", Config.EarthSetting.IconSourcePath + "\\search.png", m_compass);

            this.Viewer.MenuBar.AddToolsMenuButton(m_compasstoolbar);


        }

        void m_compass_OnResizeEvent(object IWidget, System.Drawing.Size size)
        {
            if (this.m_compass != null && this.TEX != null)
            {
                this.TEX.ClientSize = new System.Drawing.Size(this.m_compass.WidgetSize.Width - 20, this.m_compass.WidgetSize.Height - 20);
            }
        }
        public override void UnLoad()
        {
            base.UnLoad();
        }
    }

    public class pRegWidgetEvent : TechDemoPlugin
    {
        public override void Load()
        {   //TODO JHJ 修改事件路由
            //this.Viewer.MouseDown += new System.Windows.Forms.MouseEventHandler(Viewer_MouseDown);

            //this.Viewer.MouseMove += new System.Windows.Forms.MouseEventHandler(Viewer_MouseMove);

            //this.Viewer.MouseWheel += new System.Windows.Forms.MouseEventHandler(Viewer_MouseWheel);

            //this.Viewer.MouseUp += new System.Windows.Forms.MouseEventHandler(Viewer_MouseUp);

            this.Viewer.MouseLeave += new EventHandler(Viewer_MouseLeave);

            this.Viewer.MouseEnter += new EventHandler(Viewer_MouseEnter);

            this.Viewer.KeyUp += new System.Windows.Forms.KeyEventHandler(Viewer_KeyUp);

            this.Viewer.KeyPress += new System.Windows.Forms.KeyPressEventHandler(Viewer_KeyPress);

            this.Viewer.KeyDown += new System.Windows.Forms.KeyEventHandler(Viewer_KeyDown);

        }

        void Viewer_KeyDown(object sender, System.Windows.Forms.KeyEventArgs e)
        {
            this.Viewer.CurrentWorld.OverlayList.OnKeyDown(e);
        }

        void Viewer_KeyPress(object sender, System.Windows.Forms.KeyPressEventArgs e)
        {
            this.Viewer.CurrentWorld.OverlayList.OnKeyPress(e);
        }

        void Viewer_KeyUp(object sender, System.Windows.Forms.KeyEventArgs e)
        {
            this.Viewer.CurrentWorld.OverlayList.OnKeyUp(e);
        }

        void Viewer_MouseEnter(object sender, EventArgs e)
        {
            this.Viewer.CurrentWorld.OverlayList.OnMouseEnter(e);
        }

        void Viewer_MouseLeave(object sender, EventArgs e)
        {
            this.Viewer.CurrentWorld.OverlayList.OnMouseLeave(e);
        }

        void Viewer_MouseUp(object sender, System.Windows.Forms.MouseEventArgs e)
        {
            this.Viewer.CurrentWorld.OverlayList.OnMouseUp(e);
        }

        void Viewer_MouseWheel(object sender, System.Windows.Forms.MouseEventArgs e)
        {
            this.Viewer.CurrentWorld.OverlayList.OnMouseWheel(e);
        }

        void Viewer_MouseMove(object sender, System.Windows.Forms.MouseEventArgs e)
        {
            this.Viewer.CurrentWorld.OverlayList.OnMouseMove(e);
        }

        void Viewer_MouseDown(object sender, System.Windows.Forms.MouseEventArgs e)
        {
            this.Viewer.CurrentWorld.OverlayList.OnMouseDown(e);
        }
    }

    public class pLatLonGrid : TechDemoPlugin
    {
        private string name = "zLatlong";
        public override void Load()
        {
            ////////////////
            LatLonGrid grd = new LatLonGrid(name);
            grd.IsVisible = true;
            this.Viewer.CurrentWorld.RenderLayerList.Add(grd);
        }
        public override void UnLoad()
        {
            base.UnLoad();
        }
    }

    public class pVE: TechDemoPlugin
    {
        private string name = "zLatlong";
        public override void Load()
        {
            //if (this.m_mapType == VirtualEarthMapType.road)
            //{
            //    str = "png";
            //    str2 = "r";
            //}
            //else
            //{
            //    str = "jpeg";
            //    if (this.m_mapType == VirtualEarthMapType.aerial)
            //    {
            //        str2 = "a";
            //    }
            //    else
            //    {
            //        str2 = "h";
            //    }
            //}

            //VeReprojectTilesLayer ve = new VeReprojectTilesLayer("FVE", this.WorldViewer, "r", "png", 0, Config.EarthSetting.RSourcePath);
            //ve.IsVisible = false;
            //this.Viewer.CurrentWorld.RenderLayerList.Add(ve);
        }
        public override void UnLoad()
        {
            base.UnLoad();
        }
    }
  

    public class pCompass3D : TechDemoPlugin
    {
        public override void Load()
        {            ///////////////////////////
            WidgetForm m_compass = new WidgetForm("Compass");
            m_compass.ClientSize = new System.Drawing.Size(200, 200);
            m_compass.Location = new System.Drawing.Point(0, 400);
            m_compass.BackgroundColor = World.Settings.WidgetBackgroundColor;
            m_compass.ParentWidget = this.Viewer.CurrentWorld.OverlayList;

            m_compass.AutoHideHeader = true;
            m_compass.VerticalScrollbarEnabled = false;
            m_compass.HorizontalScrollbarEnabled = false;
            m_compass.BorderEnabled = false;
            m_compass.HeaderEnabled = false;

            Compass3DWidget m_compasswidget = new Compass3DWidget();
            m_compasswidget.Location = new System.Drawing.Point(5, 0);
            m_compasswidget.Font = new System.Drawing.Font("Ariel", 10.0f, System.Drawing.FontStyle.Bold);
            m_compasswidget.ParentWidget = m_compass;

            m_compass.ChildWidgets.Add(m_compasswidget);
            this.Viewer.CurrentWorld.OverlayList.Add(m_compass);

            OverLayer.Control.WidgetMenuButton m_compasstoolbar = new R.Earth.OverLayer.Control.WidgetMenuButton
            ("Compass", Config.EarthSetting.IconSourcePath + "\\compass.png", m_compass);

            this.Viewer.MenuBar.AddToolsMenuButton(m_compasstoolbar);
        }

        public override void UnLoad()
        {
        }
    }
    
    public class pLegend : TechDemoPlugin
    {
        public override void Load()
        {
            ///////////////////
            WidgetForm m_form = new WidgetForm("Scale Bar Legend");
            m_form.Location = new System.Drawing.Point(DrawArgs.Instance.ScreenWidth - 300, DrawArgs.Instance.ScreenHeight - 70);
            m_form.ClientSize = new System.Drawing.Size(150, 60);
            m_form.Text = "Scale Bar Legend";
            m_form.BackgroundColor = World.Settings.WidgetBackgroundColor;
            m_form.ParentWidget = this.Viewer.CurrentWorld.OverlayList;

            m_form.AutoHideHeader = true;
            m_form.VerticalScrollbarEnabled = false;
            m_form.HorizontalScrollbarEnabled = false;
            m_form.BorderEnabled = false;
            m_form.HeaderEnabled = false;

            ScaleBarWidget m_scaleBar = new ScaleBarWidget();
            m_scaleBar.Location = new System.Drawing.Point(5, 0);
            m_scaleBar.ParentWidget = m_form;
            m_scaleBar.ClientSize = new System.Drawing.Size(m_form.WidgetSize.Width - 10, m_form.WidgetSize.Height - 20);
            m_form.ChildWidgets.Add(m_scaleBar);

            this.Viewer.CurrentWorld.OverlayList.Add(m_form);

            OverLayer.Control.WidgetMenuButton m_compasstoolbar = new R.Earth.OverLayer.Control.WidgetMenuButton
         ("比例尺", Config.EarthSetting.IconSourcePath + "\\compass.png", m_form);

            this.Viewer.MenuBar.AddToolsMenuButton(m_compasstoolbar);
        }
        public override void UnLoad()
        {

        }
    }


    public class pStar3D : TechDemoPlugin
    {
        private string m_strPluginPath = Config.EarthSetting.MediaSourcePath + @"Star3D\\";
        private string LayerName = "a_StarField";
        public override void Load()
        {
            Stars3DLayer layer = new Stars3DLayer(LayerName, m_strPluginPath, this.Viewer);
            layer.IsVisible = true ;
            this.Viewer.CurrentWorld.RenderLayerList.Add(layer);
        }
        public override void UnLoad()
        {
            base.UnLoad();
        }
    }
    public class pSunScattingShade : TechDemoPlugin
    {
        private string surfaceimagepath = Config.EarthSetting.RSourcePath + @"\Earth\TM02.jpg";

        public override void Load()
        {
            SunScatting sc = new SunScatting("bSun", surfaceimagepath, this.Viewer.CurrentWorld);
            sc.IsVisible = true;
            this.Viewer.CurrentWorld.RenderLayerList.Add(sc);
        }
        public override void UnLoad()
        {
        }
    }

}