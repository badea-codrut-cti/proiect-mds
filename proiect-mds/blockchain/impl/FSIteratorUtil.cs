using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace proiect_mds.blockchain.impl
{
    internal abstract class FSIteratorUtil<T>
    {
        protected readonly Stream objectStream;
        protected readonly int maxCache;
        protected List<T> readCache;
        protected List<T> writeCache;
        protected bool endOfStreamReached = false;
        protected bool disposed = false;
        protected T currentObject;

        public FSIteratorUtil(Stream stream, int maxCache) {
            ArgumentNullException.ThrowIfNull(stream);
            if (maxCache <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(maxCache), "maxCache must be positive.");
            }

            this.objectStream = stream;
            this.maxCache = maxCache;
            this.readCache = new List<T>(maxCache);
            this.writeCache = new List<T>(maxCache);
            this.endOfStreamReached = false;

            FillReadCache();

            if (readCache.Count == 0)
            {
                throw new InvalidOperationException("No objects found in the stream.");
            }

            currentObject = readCache[0];
        }
        private void FillReadCache()
        {
            while (readCache.Count < maxCache && !endOfStreamReached)
            {
                T? obj = ReadFromStream();
                if (obj != null)
                {
                    readCache.Add(obj);
                }
                else
                {
                    endOfStreamReached = true;
                }
            }
        }
        public T Current
        {
            get
            {
                if (currentObject == null)
                {
                    throw new InvalidOperationException("Current object is null.");
                }
                return currentObject;
            }
        }
        public void Reset()
        {
            endOfStreamReached = false;
            readCache.Clear();
            objectStream.Seek(0, SeekOrigin.Begin);
            FillReadCache();
        }
        public void Dispose()
        {
            if (!disposed)
            {
                WriteCacheToStream();
                objectStream.Dispose();
                disposed = true;
            }
        }
        public bool AddObject(T obj)
        {
            writeCache.Add(obj);
            if (writeCache.Count > maxCache)
            {
                WriteCacheToStream();
            }
            return true;
        }
        private void WriteCacheToStream()
        {
            foreach(T obj in writeCache) 
            {
                WriteToStream(obj);
            }
        }

        protected abstract T? ReadFromStream();
        protected abstract bool WriteToStream(T obj);
    }
}
