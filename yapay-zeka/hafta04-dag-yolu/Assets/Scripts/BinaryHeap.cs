using System;
using System.Collections.Generic;

/// <summary>Items stored in a <see cref="BinaryHeap{T}"/> remember their slot so their key can be decreased in O(log n).</summary>
public interface IHeapItem
{
    int HeapIndex { get; set; }
}

/// <summary>
/// Indexed binary min-heap (bonus task: open list as a binary heap).
/// Pop is O(log n) instead of the O(n) scan of a plain List; DecreaseKey re-sorts an item after its g dropped.
/// </summary>
public class BinaryHeap<T> where T : class, IHeapItem
{
    readonly List<T> items = new List<T>();
    readonly Comparison<T> compare;   // < 0 : first argument comes out first

    public BinaryHeap(Comparison<T> compare) => this.compare = compare;

    public int Count => items.Count;

    public void Clear() => items.Clear();

    public void Push(T item)
    {
        item.HeapIndex = items.Count;
        items.Add(item);
        SiftUp(item.HeapIndex);
    }

    public T Pop()
    {
        T top = items[0];
        int last = items.Count - 1;
        Move(items[last], 0);
        items.RemoveAt(last);
        if (items.Count > 0) SiftDown(0);
        top.HeapIndex = -1;
        return top;
    }

    /// <summary>The item's key became smaller (A*: a cheaper g was found).</summary>
    public void DecreaseKey(T item) => SiftUp(item.HeapIndex);

    void SiftUp(int i)
    {
        T item = items[i];
        while (i > 0)
        {
            int parent = (i - 1) / 2;
            if (compare(item, items[parent]) >= 0) break;
            Move(items[parent], i);
            i = parent;
        }
        Move(item, i);
    }

    void SiftDown(int i)
    {
        T item = items[i];
        int n = items.Count;
        while (true)
        {
            int left = 2 * i + 1;
            if (left >= n) break;
            int right = left + 1;
            int child = right < n && compare(items[right], items[left]) < 0 ? right : left;
            if (compare(items[child], item) >= 0) break;
            Move(items[child], i);
            i = child;
        }
        Move(item, i);
    }

    void Move(T item, int index)
    {
        items[index] = item;
        item.HeapIndex = index;
    }
}
