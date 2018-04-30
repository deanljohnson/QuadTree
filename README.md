# QuadTree
A SFML.Net QuadTree implementation. Supports thread-safe queeries and provides optimized queeries for single threaded use.

While the current implementation depends on SFML.Net, it could be adapted to other environments fairly easily - all the implementation needs is vectors, rects, and a concept of a transformable object with a position vector.

Designed primarily for use by games, the implementation buffers modifications to the tree until `Update()` is called. This fits cleanly into a "Game Loop" and allows extremely efficient queries and updates. This also allows for the execution of multiple parallel queries. As long as Update is not called during a queery, the implementation is thread safe. Unlock many implementations which require the tree to be completely rebuilt in order to adapt to moving objects, this QuadTree restructures itself inside of `Update()` to account for objects that have moved.

Included in the repository are multiple benchmarks to test the efficiency of various operations. The code base is also well unit-tested.
