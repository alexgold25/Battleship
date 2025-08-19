using System.Collections.Generic;
using System.Linq;

namespace Battleship.Core;

public class Ship
{
    public int Length { get; }
    public List<(int x, int y)> Coordinates { get; }
    public int Hits { get; private set; }

    public Ship(int length, int startX, int startY, Orientation orientation)
    {
        Length = length;
        Coordinates = new List<(int x, int y)>();
        for (int i = 0; i < length; i++)
        {
            int x = startX + (orientation == Orientation.Horizontal ? i : 0);
            int y = startY + (orientation == Orientation.Vertical ? i : 0);
            Coordinates.Add((x, y));
        }
    }

    public bool Contains(int x, int y) => Coordinates.Any(c => c.x == x && c.y == y);

    public void RegisterHit() => Hits++;

    public bool IsSunk => Hits >= Length;
}
