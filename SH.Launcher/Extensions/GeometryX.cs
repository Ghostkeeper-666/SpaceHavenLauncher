using Avalonia;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace SH.Launcher.Extensions;

internal static class GeometryX
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Point Normalize(this Point pos, Point size) =>
        size.X <= 0 || size.Y <= 0 ? new() : new(pos.X / size.X, pos.Y / size.Y);

    public static bool Contains(this IReadOnlyList<Point> polygon, Point normalizedPoint)
    {
        int n = polygon.Count;
        if (n < 3)
            return false;
        bool inside = false;
        for (int i = 0, j = n - 1; i < n; j = i++)
        {
            double xi = polygon[i].X, yi = polygon[i].Y;
            double xj = polygon[j].X, yj = polygon[j].Y;
            // Check if edge (j -> i) crosses a horizontal ray to the right of p
            bool intersect =
                ((yi > normalizedPoint.Y) != (yj > normalizedPoint.Y)) &&
                (normalizedPoint.X < (xj - xi) * (normalizedPoint.Y - yi) / (yj - yi + double.Epsilon) + xi);
            if (intersect)
                inside = !inside;
        }
        return inside;
    }

}
