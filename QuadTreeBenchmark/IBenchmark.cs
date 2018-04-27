namespace QuadTreeBenchmark
{
    interface IBenchmark
    {
        string Name { get; }
    }

    interface IBenchmark<T> : IBenchmark
    {
    }
}
