using System;
using System.Collections.Generic;
using System.Linq;

namespace Battleship.Core;

public class Board
{
    public const int Size = 10;
    public CellState[,] Cells { get; } = new CellState[Size, Size];

    public void Clear()
    {
        Array.Clear(Cells, 0, Cells.Length);
    }

    public bool InBounds(int r, int c) => r >= 0 && r < Size && c >= 0 && c < Size;

    public bool ToggleShipCell(int r, int c)
    {
        if (!InBounds(r, c)) return false;
        Cells[r, c] = Cells[r, c] == CellState.Ship ? CellState.Empty : CellState.Ship;
        return true;
    }

    public (bool ok, string? reason) ValidateFleet()
    {
        // Требуемый набор: 1x4, 2x3, 3x2, 4x1
        int[] required = { 4, 3, 3, 2, 2, 2, 1, 1, 1, 1 };

        // 1) Запрет касаний (включая диагональ)
        for (int r = 0; r < Size; r++)
            for (int c = 0; c < Size; c++)
            {
                if (Cells[r, c] != CellState.Ship) continue;
                for (int dr = -1; dr <= 1; dr++)
                    for (int dc = -1; dc <= 1; dc++)
                    {
                        if (dr == 0 && dc == 0) continue;
                        int nr = r + dr, nc = c + dc;
                        if (InBounds(nr, nc) && Cells[nr, nc] == CellState.Ship && !(Math.Abs(dr) + Math.Abs(dc) == 1))
                            return (false, "Корабли не должны касаться по диагонали");
                    }
            }

        // 2) Разбиение на корабли (ортогонально связные группы, без изгибов)
        bool[,] vis = new bool[Size, Size];
        var found = new List<int>();

        for (int r = 0; r < Size; r++)
            for (int c = 0; c < Size; c++)
            {
                if (Cells[r, c] != CellState.Ship || vis[r, c]) continue;

                // flood-fill по ортогонали
                var q = new Queue<(int r, int c)>();
                q.Enqueue((r, c)); vis[r, c] = true;
                var coords = new List<(int r, int c)> { (r, c) };

                while (q.Count > 0)
                {
                    var (cr, cc) = q.Dequeue();
                    foreach (var (dr, dc) in new[] { (-1, 0), (1, 0), (0, -1), (0, 1) })
                    {
                        int nr = cr + dr, nc = cc + dc;
                        if (InBounds(nr, nc) && !vis[nr, nc] && Cells[nr, nc] == CellState.Ship)
                        { vis[nr, nc] = true; q.Enqueue((nr, nc)); coords.Add((nr, nc)); }
                    }
                }

                // Проверка "линейности"
                bool sameRow = coords.All(p => p.r == coords[0].r);
                bool sameCol = coords.All(p => p.c == coords[0].c);
                if (!(sameRow || sameCol)) return (false, "Корабль должен быть прямой линией");

                found.Add(coords.Count);
            }

        found.Sort();
        Array.Sort(required);

        if (!found.SequenceEqual(required))
            return (false, $"Неверный набор кораблей. Должно быть: 1x4, 2x3, 3x2, 4x1");

        return (true, null);
    }

    // NEW: возвращает, потоплен ли корабль, и список его палуб (Hit/Ship вдоль линии)
    private bool TryGetShipLine(int r, int c, out System.Collections.Generic.List<(int r, int c)> cells, out bool sunk)
    {
        cells = new(); sunk = false;
        if (Cells[r, c] != CellState.Hit && Cells[r, c] != CellState.Ship) return false;

        // Определяем направление (гориз или вертикаль)
        (int dr, int dc) dir = (0, 0);
        foreach (var d in new[] { (0, 1), (1, 0), (0, -1), (-1, 0) })
        {
            int nr = r + d.Item1, nc = c + d.Item2;
            if (InBounds(nr, nc) && (Cells[nr, nc] == CellState.Hit || Cells[nr, nc] == CellState.Ship))
            { dir = (Math.Abs(d.Item1), Math.Abs(d.Item2)); break; }
        }
        if (dir == (0, 0)) dir = (0, 1); // одиночная или неявное направление — считать горизонталью для прохода

        // Идём до начала
        int sr = r, sc = c;
        while (InBounds(sr - dir.dr, sc - dir.dc) && (Cells[sr - dir.dr, sc - dir.dc] == CellState.Hit || Cells[sr - dir.dr, sc - dir.dc] == CellState.Ship))
        { sr -= dir.dr; sc -= dir.dc; }

        // Собираем линию
        int cr = sr, cc = sc;
        bool hasShipLeft = false;
        while (InBounds(cr, cc) && (Cells[cr, cc] == CellState.Hit || Cells[cr, cc] == CellState.Ship))
        {
            cells.Add((cr, cc));
            if (Cells[cr, cc] == CellState.Ship) hasShipLeft = true;
            cr += dir.dr; cc += dir.dc;
        }

        sunk = !hasShipLeft; // потоплен, если в линии не осталось Ship
        return true;
    }

    public bool Shoot(int r, int c, out bool wasHit, out bool sunk)
    {
        wasHit = false; sunk = false;
        if (!InBounds(r, c)) return false;
        if (Cells[r, c] == CellState.Hit || Cells[r, c] == CellState.Miss) return false;

        if (Cells[r, c] == CellState.Ship)
        {
            Cells[r, c] = CellState.Hit;
            wasHit = true;

            // Проверяем линию корабля строго и получаем её клетки
            if (TryGetShipLine(r, c, out var line, out sunk) && sunk)
            {
                // ВАЖНО: обводим только если корабль многопалубный
                //if (line.Count > 1)
                    OutlineAround(line);
            }
        }
        else
        {
            Cells[r, c] = CellState.Miss;
        }
        return true;
    }

    // Был OutlineSunkShip(int r,int c) — заменяем универсальной версией:
    private void OutlineAround(System.Collections.Generic.List<(int r, int c)> shipCells)
    {
        foreach (var (sr, sc) in shipCells)
        {
            for (int dr = -1; dr <= 1; dr++)
                for (int dc = -1; dc <= 1; dc++)
                {
                    int nr = sr + dr, nc = sc + dc;
                    if (!InBounds(nr, nc)) continue;
                    if (Cells[nr, nc] == CellState.Empty)
                        Cells[nr, nc] = CellState.Miss;
                }
        }
    }

    /// Обозначить все соседние (включая диагональ) клетки вокруг потопленного корабля как Miss
    private void OutlineSunkShip(int r, int c)
    {
        // собрать клетки корабля (в обе стороны по горизонтали и вертикали)
        var shipCells = new List<(int r, int c)> { (r, c) };
        foreach (var (dr, dc) in new[] { (0, 1), (0, -1), (1, 0), (-1, 0) })
        {
            int nr = r + dr, nc = c + dc;
            while (InBounds(nr, nc) && (Cells[nr, nc] == CellState.Hit))
            { shipCells.Add((nr, nc)); nr += dr; nc += dc; }
        }

        // вокруг каждой палубы проставить Miss, если пусто
        foreach (var (sr, sc) in shipCells)
        {
            for (int dr = -1; dr <= 1; dr++)
                for (int dc = -1; dc <= 1; dc++)
                {
                    int nr = sr + dr, nc = sc + dc;
                    if (InBounds(nr, nc) && Cells[nr, nc] == CellState.Empty)
                        Cells[nr, nc] = CellState.Miss;
                }
        }
    }

    public bool HasShipsLeft()
    {
        for (int r = 0; r < Size; r++)
            for (int c = 0; c < Size; c++)
                if (Cells[r, c] == CellState.Ship) return true;
        return false;
    }

    private bool IsShipSunk(int r, int c)
    {
        // ищем весь корабль вдоль направления и проверяем все клетки — Hit
        foreach (var (dr, dc) in new[] { (0, 1), (1, 0) })
        {
            int rr = r, cc = c;
            // назад
            while (InBounds(rr - dr, cc - dc) && (Cells[rr - dr, cc - dc] == CellState.Ship || Cells[rr - dr, cc - dc] == CellState.Hit))
            { rr -= dr; cc -= dc; }

            bool allHit = true;
            int tr = rr, tc = cc;
            while (InBounds(tr, tc) && (Cells[tr, tc] == CellState.Ship || Cells[tr, tc] == CellState.Hit))
            {
                if (Cells[tr, tc] == CellState.Ship) { allHit = false; break; }
                tr += dr; tc += dc;
            }
            if (allHit) return true;
        }
        // одиночная палуба
        return Cells[r, c] == CellState.Hit &&
               new[] { (-1, 0), (1, 0), (0, -1), (0, 1) }.All(d =>
               {
                   int nr = r + d.Item1, nc = c + d.Item2;
                   return !InBounds(nr, nc) || Cells[nr, nc] != CellState.Ship;
               });
    }
}
