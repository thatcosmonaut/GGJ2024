using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using RollAndCash.Components;

/// <summary>
/// Used to quickly check if two shapes are potentially overlapping.
/// </summary>
/// <typeparam name="T">The type that will be used to uniquely identify shape-transform pairs.</typeparam>
public class SpatialHash<T> where T : unmanaged, System.IEquatable<T>
{
    protected readonly int CellSize;

    protected readonly List<T>[][] Cells;
    protected readonly Dictionary<T, Rectangle> IDBoxLookup = new Dictionary<T, Rectangle>();

    protected readonly int X;
    protected readonly int Y;
    protected readonly int Width;
    protected readonly int Height;
    protected readonly int RowCount;
    protected readonly int ColumnCount;

    private Queue<HashSet<T>> hashSetPool = new Queue<HashSet<T>>();

    public SpatialHash(int x, int y, int width, int height, int cellSize)
    {
        X = x;
        Y = y;
        Width = width;
        Height = height;
        RowCount = height / cellSize;
        ColumnCount = width / cellSize;
        CellSize = cellSize;

        Cells = new List<T>[RowCount][];
        for (var i = 0; i < RowCount; i += 1)
        {
            Cells[i] = new List<T>[ColumnCount];

            for (var j = 0; j < ColumnCount; j += 1)
            {
                Cells[i][j] = new List<T>();
            }
        }
    }

    protected (int, int) Hash(int x, int y)
    {
        return (x / CellSize, y / CellSize);
    }

    // TODO: we could speed this up with a proper Update check
    // that checks the difference between the two hash key ranges

    /// <summary>
    /// Inserts an element into the SpatialHash.
    /// Rectangles outside of the hash range will be ignored!
    /// </summary>
    /// <param name="id">A unique ID for the shape-transform pair.</param>
    public virtual void Insert(T id, Rectangle rectangle)
    {
        var relativeX = rectangle.X - X;
        var relativeY = rectangle.Y - Y;
        var rowRangeStart = Math.Clamp(relativeX / CellSize, 0, ColumnCount - 1);
        var rowRangeEnd = Math.Clamp((relativeX + rectangle.Width) / CellSize, 0, ColumnCount - 1);
        var columnRangeStart = Math.Clamp(relativeY / CellSize, 0, RowCount - 1);
        var columnRangeEnd = Math.Clamp((relativeY + rectangle.Height) / CellSize, 0, RowCount - 1);

        for (var i = rowRangeStart; i <= rowRangeEnd; i += 1)
        {
            for (var j = columnRangeStart; j <= columnRangeEnd; j += 1)
            {
                Cells[i][j].Add(id);
            }
        }

        IDBoxLookup[id] = rectangle;
    }

    /// <summary>
    /// Retrieves all the potential collisions of a shape-transform pair. Excludes any shape-transforms with the given ID.
    /// </summary>
    public RetrieveEnumerator Retrieve(T id, Rectangle rectangle)
    {
        var relativeX = rectangle.X - X;
        var relativeY = rectangle.Y - Y;
        var rowRangeStart = Math.Clamp(relativeX / CellSize, 0, ColumnCount - 1);
        var rowRangeEnd = Math.Clamp((relativeX + rectangle.Width) / CellSize, 0, ColumnCount - 1);
        var columnRangeStart = Math.Clamp(relativeY / CellSize, 0, RowCount - 1);
        var columnRangeEnd = Math.Clamp((relativeY + rectangle.Height) / CellSize, 0, RowCount - 1);

        return new RetrieveEnumerator(
            this,
            Keys(rowRangeStart, columnRangeStart, rowRangeEnd, columnRangeEnd),
            id
        );
    }

    /// <summary>
    /// Retrieves objects based on a pre-transformed AABB.
    /// </summary>
    /// <param name="aabb">A transformed AABB.</param>
    /// <returns></returns>
    public RetrieveEnumerator Retrieve(Rectangle rectangle)
    {
        var relativeX = rectangle.X - X;
        var relativeY = rectangle.Y - Y;
        var rowRangeStart = Math.Clamp(relativeX / CellSize, 0, ColumnCount - 1);
        var rowRangeEnd = Math.Clamp((relativeX + rectangle.Width) / CellSize, 0, ColumnCount - 1);
        var columnRangeStart = Math.Clamp(relativeY / CellSize, 0, RowCount - 1);
        var columnRangeEnd = Math.Clamp((relativeY + rectangle.Height) / CellSize, 0, RowCount - 1);

        return new RetrieveEnumerator(
            this,
            Keys(rowRangeStart, columnRangeStart, rowRangeEnd, columnRangeEnd)
        );
    }

    /// <summary>
    /// Removes everything that has been inserted into the SpatialHash.
    /// </summary>
    public virtual void Clear()
    {
        for (var i = 0; i < RowCount; i += 1)
        {
            for (var j = 0; j < ColumnCount; j += 1)
            {
                Cells[i][j].Clear();
            }
        }

        IDBoxLookup.Clear();
    }

    internal static KeysEnumerator Keys(int minX, int minY, int maxX, int maxY)
    {
        return new KeysEnumerator(minX, minY, maxX, maxY);
    }

    private HashSet<T> AcquireHashSet()
    {
        if (hashSetPool.Count == 0)
        {
            hashSetPool.Enqueue(new HashSet<T>());
        }

        var hashSet = hashSetPool.Dequeue();
        hashSet.Clear();
        return hashSet;
    }

    private void FreeHashSet(HashSet<T> hashSet)
    {
        hashSetPool.Enqueue(hashSet);
    }

    internal ref struct KeysEnumerator
    {
        private int MinX;
        private int MinY;
        private int MaxX;
        private int MaxY;
        private int i, j;

        public KeysEnumerator GetEnumerator() => this;

        public KeysEnumerator(int minX, int minY, int maxX, int maxY)
        {
            MinX = minX;
            MinY = minY;
            MaxX = maxX;
            MaxY = maxY;
            i = minX;
            j = minY - 1;
        }

        public bool MoveNext()
        {
            if (j < MaxY)
            {
                j += 1;
                return true;
            }
            else if (i < MaxX)
            {
                i += 1;
                j = MinY;
                return true;
            }

            return false;
        }

        public (int, int) Current => (i, j);
    }

    public ref struct RetrieveEnumerator
    {
        public SpatialHash<T> SpatialHash;
        private KeysEnumerator KeysEnumerator;
        private Span<T>.Enumerator SpanEnumerator;
        private bool HashSetEnumeratorActive;
        private HashSet<T> Duplicates;
        private T? ID;

        public RetrieveEnumerator GetEnumerator() => this;

        internal RetrieveEnumerator(
            SpatialHash<T> spatialHash,
            KeysEnumerator keysEnumerator,
            T id
        ) {
            SpatialHash = spatialHash;
            KeysEnumerator = keysEnumerator;
            SpanEnumerator = default;
            HashSetEnumeratorActive = false;
            Duplicates = SpatialHash.AcquireHashSet();
            ID = id;
        }

        internal RetrieveEnumerator(
            SpatialHash<T> spatialHash,
            KeysEnumerator keysEnumerator
        ) {
            SpatialHash = spatialHash;
            KeysEnumerator = keysEnumerator;
            SpanEnumerator = default;
            HashSetEnumeratorActive = false;
            Duplicates = SpatialHash.AcquireHashSet();
            ID = null;
        }

        public bool MoveNext()
        {
            if (!HashSetEnumeratorActive || !SpanEnumerator.MoveNext())
            {
                if (!KeysEnumerator.MoveNext())
                {
                    return false;
                }

                var (i, j) = KeysEnumerator.Current;
                SpanEnumerator = CollectionsMarshal.AsSpan(SpatialHash.Cells[i][j]).GetEnumerator();
                HashSetEnumeratorActive = true;

                return MoveNext();
            }

            // conditions
            var t = SpanEnumerator.Current;

            if (Duplicates.Contains(t))
            {
                return MoveNext();
            }

            if (ID.HasValue)
            {
                if (ID.Value.Equals(t))
                {
                    return MoveNext();
                }
            }

            Duplicates.Add(t);
            return true;
        }

        public (T, Rectangle) Current
        {
            get
            {
                var t = SpanEnumerator.Current;
                var rect = SpatialHash.IDBoxLookup[t];
                return (t, rect);
            }
        }

        public void Dispose()
        {
            SpatialHash.FreeHashSet(Duplicates);
        }
    }
}
