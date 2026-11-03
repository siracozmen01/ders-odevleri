using System;

public interface IHeapItem<T> : IComparable<T>
{
    int HeapIndex { get; set; }
}

/// <summary>Binary heap used as the A* open set: O(log n) insert / remove-min instead of O(n) list scans.</summary>
public class Heap<T> where T : class, IHeapItem<T>
{
    readonly T[] items;
    public int Count { get; private set; }

    public Heap(int maxSize) { items = new T[maxSize]; }

    public void Add(T item)
    {
        item.HeapIndex = Count;
        items[Count] = item;
        SortUp(item);
        Count++;
    }

    public T RemoveFirst()
    {
        T first = items[0];
        Count--;
        items[0] = items[Count];
        items[0].HeapIndex = 0;
        SortDown(items[0]);
        return first;
    }

    public void UpdateItem(T item) { SortUp(item); }

    public bool Contains(T item) => item.HeapIndex < Count && Equals(items[item.HeapIndex], item);

    void SortDown(T item)
    {
        while (true)
        {
            int left = item.HeapIndex * 2 + 1, right = left + 1, swap;
            if (left >= Count) return;
            swap = left;
            if (right < Count && items[left].CompareTo(items[right]) < 0) swap = right;
            if (item.CompareTo(items[swap]) < 0) Swap(item, items[swap]);
            else return;
        }
    }

    void SortUp(T item)
    {
        while (item.HeapIndex > 0)
        {
            T parent = items[(item.HeapIndex - 1) / 2];
            if (item.CompareTo(parent) > 0) Swap(item, parent);
            else return;
        }
    }

    void Swap(T a, T b)
    {
        items[a.HeapIndex] = b;
        items[b.HeapIndex] = a;
        (a.HeapIndex, b.HeapIndex) = (b.HeapIndex, a.HeapIndex);
    }
}
