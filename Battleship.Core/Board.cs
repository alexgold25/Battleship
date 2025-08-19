using System;
using System.Collections.Generic;
using System.Linq;

namespace Battleship.Core;

public class Board
{
    public const int Size = 10;
    private readonly CellState[,] _cells = new CellState[Size, Size];
    private readonly List<Ship> _ships = new();
    private static readonly int[] _shipSizes = { 4, 3, 3, 2, 2, 2, 1, 1, 1, 1 };

    public CellState[,] Cells => _cells;
    public IReadOnlyList<Ship> Ships => _ships;
    public int ShipsRemaining => _ships.Count(s => !s.IsSunk);

    public void Clear()
    {
        for (int x = 0; x < Size; x++)
            for (int y = 0; y < Size; y++)
                _cells[x, y] = CellState.Empty;
        _ships.Clear();
    }

    public bool CanPlaceShip(int x, int y, int length, Orientation orientation)
    {
        for (int i = 0; i < length; i++)
        {
            int nx = x + (orientation == Orientation.Horizontal ? i : 0);
            int ny = y + (orientation == Orientation.Vertical ? i : 0);
            if (nx < 0 || nx >= Size || ny < 0 || ny >= Size)
                return false;
            if (_cells[nx, ny] != CellState.Empty)
                return false;
            for (int dx = -1; dx <= 1; dx++)
                for (int dy = -1; dy <= 1; dy++)
                {
                    int sx = nx + dx;
                    int sy = ny + dy;
                    if (sx < 0 || sx >= Size || sy < 0 || sy >= Size)
                        continue;
                    if (_cells[sx, sy] == CellState.Ship)
                        return false;
                }
        }
        return true;
    }

    public bool PlaceShip(int x, int y, int length, Orientation orientation)
    {
        if (!CanPlaceShip(x, y, length, orientation))
            return false;
        var ship = new Ship(length, x, y, orientation);
        _ships.Add(ship);
        foreach (var (sx, sy) in ship.Coordinates)
            _cells[sx, sy] = CellState.Ship;
        return true;
    }

    public void AutoPlaceShips()
    {
        Clear();
        var rnd = Random.Shared;
        foreach (var size in _shipSizes)
        {
            bool placed = false;
            while (!placed)
            {
                int x = rnd.Next(Size);
                int y = rnd.Next(Size);
                var orientation = rnd.Next(2) == 0 ? Orientation.Horizontal : Orientation.Vertical;
                placed = PlaceShip(x, y, size, orientation);
            }
        }
    }

    public ShotResult Shoot(int x, int y)
    {
        if (x < 0 || x >= Size || y < 0 || y >= Size)
            return ShotResult.Miss;
        switch (_cells[x, y])
        {
            case CellState.Empty:
                _cells[x, y] = CellState.Miss;
                return ShotResult.Miss;
            case CellState.Ship:
                _cells[x, y] = CellState.Hit;
                var ship = _ships.First(s => s.Contains(x, y));
                ship.RegisterHit();
                if (ship.IsSunk)
                {
                    MarkSurroundings(ship);
                    return ShotResult.Sunk;
                }
                return ShotResult.Hit;
            case CellState.Hit:
            case CellState.Miss:
                return ShotResult.AlreadyTried;
            default:
                return ShotResult.Miss;
        }
    }

    private void MarkSurroundings(Ship ship)
    {
        foreach (var (x, y) in ship.Coordinates)
        {
            for (int dx = -1; dx <= 1; dx++)
            for (int dy = -1; dy <= 1; dy++)
            {
                int nx = x + dx;
                int ny = y + dy;
                if (nx < 0 || nx >= Size || ny < 0 || ny >= Size)
                    continue;
                if (_cells[nx, ny] == CellState.Empty)
                    _cells[nx, ny] = CellState.Miss;
            }
        }
    }
}
