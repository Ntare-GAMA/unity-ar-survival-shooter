using System.Collections.Generic;
using UnityEngine;

namespace ARSurvival.Pooling
{
    /// <summary>Implemented by pooled objects so they can reset themselves on reuse.</summary>
    public interface IPoolable
    {
        /// <summary>Called right after the object is taken from the pool and activated.</summary>
        void OnSpawn();

        /// <summary>Called right before the object is deactivated and returned to the pool.</summary>
        void OnDespawn();
    }

    /// <summary>
    /// Object Pool pattern: a fixed set of instances is created up front (pre-warmed) and then
    /// recycled, so no Instantiate/Destroy happens during gameplay.
    /// </summary>
    public class ObjectPool<T> where T : Component, IPoolable
    {
        readonly T prefab;
        readonly Transform container;
        readonly Queue<T> available = new();
        readonly List<T> all = new();
        readonly HashSet<T> active = new();

        public int Capacity => all.Count;
        public int ActiveCount => active.Count;

        public ObjectPool(T prefab, int size, Transform container)
        {
            this.prefab = prefab;
            this.container = container;
            for (int i = 0; i < size; i++)
                available.Enqueue(CreateInstance());
        }

        T CreateInstance()
        {
            var instance = Object.Instantiate(prefab, container);
            instance.name = $"{prefab.name}_{all.Count:00}";
            instance.gameObject.SetActive(false);
            all.Add(instance);
            return instance;
        }

        /// <summary>
        /// Takes an instance from the pool. If every instance is in use, the oldest active one is
        /// recycled rather than instantiating a new object mid-game.
        /// </summary>
        public T Get(Vector3 position, Quaternion rotation)
        {
            T item;
            if (available.Count > 0)
            {
                item = available.Dequeue();
            }
            else
            {
                item = OldestActive();
                Release(item);
                available.Dequeue();
            }

            item.transform.SetPositionAndRotation(position, rotation);
            item.gameObject.SetActive(true);
            active.Add(item);
            item.OnSpawn();
            return item;
        }

        public void Release(T item)
        {
            if (!active.Remove(item))
                return; // already released

            item.OnDespawn();
            item.gameObject.SetActive(false);
            available.Enqueue(item);
        }

        public void ReleaseAll()
        {
            foreach (var item in all)
                Release(item);
        }

        T OldestActive()
        {
            // `all` is in creation order; the first active entry is a reasonable "oldest" candidate
            // without per-spawn bookkeeping. Only reached when the pool is undersized.
            foreach (var item in all)
                if (active.Contains(item))
                    return item;
            return all[0];
        }
    }
}
