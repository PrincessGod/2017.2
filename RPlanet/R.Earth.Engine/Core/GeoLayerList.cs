using System;
using System.Collections;
using K = System.String;
using T = R.Earth.IGeoLayer;

namespace R.Earth
{
    public abstract class RList : ICollection, IEnumerable
    {
        protected SortedList childList;
        protected Object parent;
        const int INITIAL_CAPACITY = 60;
        static protected int nextUniqueKeyCounter;

        public RList()
        {
            this.parent = null;
            this.childList = new SortedList(INITIAL_CAPACITY);
        }
        public RList(Object parent)
        {
            this.parent = parent;
            this.childList = new SortedList(INITIAL_CAPACITY);
        }

        public object this[int index]
        {
            get { return this.childList.GetByIndex(index); }
            set { this.childList.SetByIndex(index, value); }
        }
        protected object this[object key]
        {
            get { return this.childList[key]; }
            set { this.childList[key] = value; }
        }

        protected void Add(object item)
        {
            this.childList.Add("Object" + (nextUniqueKeyCounter++), item);
        }
        protected void Add(object key, object item)
        {
            this.childList.Add(key, item);            
        }

        protected void SetByIndex(int index, object item)
        {
            this.childList.SetByIndex(index, item);
        }

        public void Clean()
        {
            this.childList.Clear();
        }
        #region ICollection Members

        public void CopyTo(Array array, int index)
        {
            this.childList.CopyTo(array, index);
        }

        public int Count
        {
            get { return this.childList.Count; }
        }

        public bool IsSynchronized
        {
            get { return this.childList.IsSynchronized; }
        }

        public object SyncRoot
        {
            get { return this.childList.SyncRoot; }
        }

        #endregion

        #region IEnumerable Members

        public IEnumerator GetEnumerator()
        {
            return this.childList.Values.GetEnumerator();
        }

        #endregion
     
    }

    public class GeoLayerList : RList
    {       
        public GeoLayerList():base()
        {
 
        }

        new public T this[int index]
        {
            get { return (T)base[index]; }
            set { base[index] = value; }
        }

        public T this[K key]
        {
            get { return (T)base[key]; }
            set { base[key] = value; }
        }
        public void Add(T item)
        {
            this.Add(item.Name, item);
        }
        public void Add(K key, T item)
        {
            base.Add(key, item);
        }
        public void SetByIndex(int index, T item)
        {
            base.SetByIndex(index, item);
        }
        internal void Update(DrawArgs drawArgs)
        {
            foreach (GeoLayer item in this.childList.Values)
            {
                if (item.IsUnLoad)
                {
                    continue;
                }
                item.OnFrameMove(drawArgs);
            }
        }

        internal void Render(DrawArgs drawArgs)
        {
            foreach(GeoLayer item in this.childList.Values)
            {
                if (!item.IsVisible)
                {
                    continue;
                }
                item.OnRender(drawArgs);
            }
        }
        public void RenderOrtho(DrawArgs drawArgs)
        {
            foreach (GeoLayer item in this.childList.Values)
            {
                if (!item.IsVisible)
                {
                    continue;
                }
                item.OnRenderOrtho(drawArgs);
            }
        }
        internal void Dispose()
        {
            foreach (GeoLayer item in this.childList.Values)
            {
                try
                {
                    item.Dispose();
                }
                catch
                {
                }
            }
        }

        internal void UpdateMesh(DrawArgs drawArgs)
        {
            lock (this.childList.SyncRoot)
            {
                foreach (GeoLayer item in this.childList.Values)
                {
                    try
                    {
                        item.UpdateMesh(drawArgs);
                    }
                    catch(Exception e)
                    {
                        Log.Write(e);
                    }
                }
            }
        }


    }
}
