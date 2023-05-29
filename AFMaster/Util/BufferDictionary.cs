#region using section

using System.Collections.Concurrent;
using System.Collections.Generic;

#endregion

namespace AFMaster.Util
{
    public class BufferDictionary<TKey, TValue>
    {
        private readonly ConcurrentDictionary<TKey, TValue> _dict;
        private Queue<TKey> _queue;

        public BufferDictionary()
        {
            size = 1000;
            _dict = new ConcurrentDictionary<TKey, TValue>();
            _queue = new Queue<TKey>(size);
        }

        public BufferDictionary(int size)
        {
            this.size = size;
            _dict = new ConcurrentDictionary<TKey, TValue>();
            _queue = new Queue<TKey>(size);
        }

        public int size { get; }

        public bool TryGet(TKey key, out TValue value)
        {
            return _dict.TryGetValue(key, out value);
        }

        public void TryAdd(TKey key, TValue value)
        {
            // don't add null values
            if (value == null) return;
            if (!_dict.TryAdd(key, value)) return;
            if (_queue.Count == size)
                _dict.TryRemove(_queue.Dequeue(), out value);
            _queue.Enqueue(key);
        }

        public bool TryRemove(TKey key)
        {
            TValue value;
            if (_dict.TryRemove(key, out value))
            {
                var newQueue = new Queue<TKey>(size);
                foreach (var item in _queue)
                    if (_dict.ContainsKey(item))
                        newQueue.Enqueue(item);
                _queue = newQueue;
                return true;
            }
            return false;
        }
    }
}