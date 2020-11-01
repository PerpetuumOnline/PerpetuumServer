using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;

namespace Perpetuum.Zones.Terrains
{
    public abstract class Layer : ILayer
    {
        protected Layer(LayerType layerType, int width, int height)
        {
            LayerType = layerType;
            Width = width;
            Height = height;
        }

        public LayerType LayerType
        {
            get;
        }

        public int Width
        {
            get;
        }

        public int Height
        {
            get;
        }

    }

    public class Layer<T> : Layer,ILayer<T> where T : struct
    {
        public Layer(LayerType layerType,int width,int height) : this(layerType,new T[width * height],width,height)
        {
        }

        public Layer(LayerType layerType,T[] rawData, int width, int height) : base(layerType, width, height)
        {
            RawData = rawData;
            SizeInBytes = Marshal.SizeOf<T>();
        }

        public T[] RawData { get; }

        public T this[int x, int y]
        {
            get => GetValue(x, y);
            set => SetValue(x, y, value);
        }

        public unsafe void CopyFromStreamToArea(Stream stream, Area area)
        {
            if (area.Width > Width || area.Height > Height || area.X1 < 0 || area.Y1 < 0)
            {
                var safeArea = area.Intersect(new Area(0, 0, Width, Height));
                OnAreaUpdated(safeArea);
                return;
            }

            var stride = Width * SizeInBytes;
            var areaStride = area.Width * SizeInBytes;
            var dOffset = (area.Y1 * Width + area.X1) * SizeInBytes;

            var handle = GCHandle.Alloc(RawData, GCHandleType.Pinned);
            try
            {
                var dest = (byte*)handle.AddrOfPinnedObject().ToPointer();
                for (var i = 0; i < area.Height; i++)
                {
                    stream.CopyToPointer(dest, dOffset, areaStride);
                    dOffset += stride;
                }
            }
            finally
            {
                handle.Free();
            }

            OnAreaUpdated(area);
        }

        public unsafe byte[] CopyAreaToByteArray(Area area)
        {
            var result = new byte[area.Ground * SizeInBytes];

            fixed (byte* dest = result)
            {
                var handle = GCHandle.Alloc(RawData, GCHandleType.Pinned);
                try
                {
                    var pSrc = (byte*)handle.AddrOfPinnedObject().ToPointer();
                    pSrc += (area.Y1 * Width + area.X1) * SizeInBytes;
                    var pDest = dest;
                    var stride = Width * SizeInBytes;
                    var areaStride = area.Width * SizeInBytes;
                    for (var i = 0; i < area.Height; i++)
                    {
                        Buffer.MemoryCopy(pSrc,pDest,areaStride,areaStride);
                        pSrc += stride;
                        pDest += areaStride;
                    }
                }
                finally
                {
                    handle.Free();
                }
            }

            return result;
        }

        public T[] GetArea(Area area)
        {
            var result = new T[area.Ground];
            var sOffset = area.Y1 * Width + area.X1;
            var dOffset = 0;

            for (var i = 0; i < area.Height; i++)
            {
                Array.Copy(RawData, sOffset, result, dOffset, area.Width);
                sOffset += Width;
                dOffset += area.Width;
            }

            return result;
        }

        public void SetArea(Area area, T[] data)
        {
            var sOffset = 0;
            var dOffset = area.Y1 * Width + area.X1;

            for (var i = 0; i < area.Height; i++)
            {
                Array.Copy(data, sOffset,RawData, dOffset, area.Width);
                sOffset += area.Width;
                dOffset += Width;
            }

            OnAreaUpdated(area);
        }

        public T GetValue(int x, int y)
        {
            Debug.Assert(x >= 0 && x < Width && y >= 0 && y < Height,"invalid position!");
            return RawData[y*Width + x];
        }

        public void SetValue(int x, int y,T value)
        {
            Debug.Assert(x >= 0 && x < Width && y >= 0 && y < Height, "invalid position!");
            OnUpdating(x, y,ref value);
            RawData[y*Width + x] = value;
            OnUpdated(x,y);
        }

        public event LayerUpdated Updated;
        public event LayerAreaUpdated AreaUpdated;

        public int SizeInBytes { get; }

        protected virtual void OnUpdated(int x, int y)
        {
            Updated?.Invoke(this, x, y);
        }

        private void OnAreaUpdated(Area area)
        {
            AreaUpdated?.Invoke(this, area);
        }

        protected virtual void OnUpdating(int x, int y,ref T value)
        {
        }
    }
}