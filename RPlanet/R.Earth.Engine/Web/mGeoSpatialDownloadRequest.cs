using System;
using System.Collections.Generic;
using System.Text;

namespace R.Earth.Web
{
    /// <summary>
    /// Base class for geo-spatial download requests
    /// </summary>
    public abstract class mGeoSpatialDownloadRequest : WebDownloadRequest
    {
        /// <summary>
        /// Initializes a new instance of the <see cref= "T:WorldWind.Net.GeoSpatialDownloadRequest"/> class.
        /// </summary>
        /// <param name="owner"></param>
        /// <param name="uri"></param>
        protected mGeoSpatialDownloadRequest(object owner, string uri)
            : base(owner, uri, false)
        {
            download.DownloadType = DownloadType.Wms;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref= "T:WorldWind.Net.GeoSpatialDownloadRequest"/> class.
        /// </summary>
        /// <param name="owner"></param>
        protected mGeoSpatialDownloadRequest(object owner)
            : this(owner, null)
        {
        }

        /// <summary>
        /// Western bound of current request (decimal degrees)
        /// </summary>
        public abstract float West
        {
            get;
        }

        /// <summary>
        /// Eastern bound of current request (decimal degrees)
        /// </summary>
        public abstract float East
        {
            get;
        }

        /// <summary>
        /// Northern bound of current request (decimal degrees)
        /// </summary>
        public abstract float North
        {
            get;
        }

        /// <summary>
        /// Southern bound of current request (decimal degrees)
        /// </summary>
        public abstract float South
        {
            get;
        }

        /// <summary>
        /// Color used to identify this layer (download info)
        /// </summary>
        public abstract int Color
        {
            get;
        }
    }
}
