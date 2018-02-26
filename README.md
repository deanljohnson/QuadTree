# QuadTree
A simple SFML.Net QuadTree implementation. Supports thread-safe queeries and provides optimized queeries for single threaded use.

While the current implementation depends on SFML.Net, it could be adapted to other environments fairly easily - all the implementation needs is vectors, rects, and a concept of a transformable object with a position vector.

Objects inserted into the tree are assumed to be point objects.
