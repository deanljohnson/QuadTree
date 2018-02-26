using System.Collections.Generic;
using SFML.Graphics;

namespace SFQuadTree
{
    public static class QuadTreeVisualization
    {
        public static void DrawRegions(QuadTree tree, RenderTarget target, RenderStates states, Color color)
        {
            var regions = new List<FloatRect>();
            tree.GetAllRegions(regions);

            var c = color;
            var shape = new RectangleShape
            {
                FillColor = Color.Transparent,
                OutlineThickness = 1f,
                OutlineColor = c
            };

            foreach (var fr in regions)
            {
                shape.Size = fr.Dimensions();
                shape.Position = fr.Min();
                target.Draw(shape, states);
            }
        }
    }
}
