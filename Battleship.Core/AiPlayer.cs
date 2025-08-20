using System;
using System.Collections.Generic;

namespace Battleship.Core;

public class AiPlayer
{
    private readonly Random _rnd = new();
    private readonly HashSet<(int r, int c)> _shots = new();

    // Двухуровневая очередь целей: high → при попадании; low → обычные соседи
    private readonly LinkedList<(int r, int c)> _hi = new();
    private readonly LinkedList<(int r, int c)> _lo = new();

    private static readonly (int dr, int dc)[] Ortho = new[] { (-1, 0), (1, 0), (0, -1), (0, 1) };
    private static bool InBounds(int r, int c) => r >= 0 && r < Board.Size && c >= 0 && c < Board.Size;
    private static bool Shootable(CellState[,] b, int r, int c)
        => InBounds(r, c) && b[r, c] != CellState.Miss && b[r, c] != CellState.Hit;

    public void Reset()
    {
        _shots.Clear();
        _hi.Clear();
        _lo.Clear();
        // если есть доп. поля (якорь/направление) — обнули их тоже:
        // _anchorHit = null; _direction = null;
    }
    public (int r, int c) NextShot(CellState[,] enemy)
    {
        // 1) Если на поле есть незавершённые кластеры Hit — сначала попробуем добивание по линии
        if (TryGetLineContinuation(enemy, out var pos))
            return pos;

        // 2) Иначе берём ближайшие валидные цели из high/low очередей
        while (_hi.Count > 0 || _lo.Count > 0)
        {
            var t = _hi.Count > 0 ? PopFront(_hi) : PopFront(_lo);
            if (!_shots.Contains(t) && Shootable(enemy, t.r, t.c))
                return t;
            // иначе просто пропускаем мусор (он мог стать Miss/Hit после нашей обводки)
        }

        // 3) Холодный поиск: шахматный паттерн, затем любой свободный
        var list = new List<(int r, int c)>();
        for (int parity = 0; parity < 2; parity++)
        {
            list.Clear();
            for (int r = 0; r < Board.Size; r++)
                for (int c = 0; c < Board.Size; c++)
                    if ((r + c) % 2 == parity && !_shots.Contains((r, c)) && Shootable(enemy, r, c))
                        list.Add((r, c));
            if (list.Count > 0) return list[_rnd.Next(list.Count)];
        }

        // fallback
        for (int r = 0; r < Board.Size; r++)
            for (int c = 0; c < Board.Size; c++)
                if (!_shots.Contains((r, c)) && Shootable(enemy, r, c))
                    return (r, c);
        return (0, 0);
    }

    public void ObserveShotResult((int r, int c) pos, bool hit, bool sunk, CellState[,] enemy)
    {
        _shots.Add(pos);

        if (hit)
        {
            // соседи — в high‑очередь (не чистим старые цели)
            foreach (var (dr, dc) in Ortho)
            {
                int nr = pos.r + dr, nc = pos.c + dc;
                if (Shootable(enemy, nr, nc) && !_shots.Contains((nr, nc)))
                    _hi.AddFirst((nr, nc));     // приоритет: ближние к последнему попаданию
            }
        }

        if (sunk)
        {
            // после потопления мусор сам отфильтруется (клетки станут Miss/Hit);
            // дополнительно подчистим явные дубликаты
            PruneQueues(enemy);
        }
    }

    // ---------- helpers ----------

    private static (int r, int c) PopFront(LinkedList<(int r, int c)> dq)
    { var v = dq.First!.Value; dq.RemoveFirst(); return v; }

    private void PruneQueues(CellState[,] b)
    {
        void Prune(LinkedList<(int r, int c)> q)
        {
            var n = q.First;
            while (n != null)
            {
                var next = n.Next;
                var t = n.Value;
                if (_shots.Contains(t) || !Shootable(b, t.r, t.c))
                    q.Remove(n);
                n = next;
            }
        }
        Prune(_hi); Prune(_lo);
    }

    // Ищем все кластеры Hit; если в кластере линия, бьём в её концы
    private bool TryGetLineContinuation(CellState[,] b, out (int r, int c) shot)
    {
        shot = default;
        var seen = new bool[Board.Size, Board.Size];

        for (int r = 0; r < Board.Size; r++)
            for (int c = 0; c < Board.Size; c++)
            {
                if (seen[r, c] || b[r, c] != CellState.Hit) continue;

                // BFS кластера Hit
                var q = new Queue<(int r, int c)>();
                var cells = new List<(int r, int c)>();
                q.Enqueue((r, c)); seen[r, c] = true;

                while (q.Count > 0)
                {
                    var (cr, cc) = q.Dequeue();
                    cells.Add((cr, cc));
                    foreach (var (dr, dc) in Ortho)
                    {
                        int nr = cr + dr, nc = cc + dc;
                        if (InBounds(nr, nc) && !seen[nr, nc] && b[nr, nc] == CellState.Hit)
                        { seen[nr, nc] = true; q.Enqueue((nr, nc)); }
                    }
                }

                // Попробуем определить направление (гориз/верт)
                bool sameRow = cells.TrueForAll(p => p.r == cells[0].r);
                bool sameCol = cells.TrueForAll(p => p.c == cells[0].c);

                if (sameRow || sameCol)
                {
                    // найдём концы и стрелим в сторону продолжения
                    if (sameRow)
                    {
                        int rr = cells[0].r;
                        int minC = 100, maxC = -1;
                        foreach (var (_, cc2) in cells) { minC = Math.Min(minC, cc2); maxC = Math.Max(maxC, cc2); }
                        // влево
                        if (Shootable(b, rr, minC - 1) && !_shots.Contains((rr, minC - 1))) { shot = (rr, minC - 1); return true; }
                        // вправо
                        if (Shootable(b, rr, maxC + 1) && !_shots.Contains((rr, maxC + 1))) { shot = (rr, maxC + 1); return true; }
                    }
                    else // sameCol
                    {
                        int cc = cells[0].c;
                        int minR = 100, maxR = -1;
                        foreach (var (rr, _) in cells) { minR = Math.Min(minR, rr); maxR = Math.Max(maxR, rr); }
                        // вверх
                        if (Shootable(b, minR - 1, cc) && !_shots.Contains((minR - 1, cc))) { shot = (minR - 1, cc); return true; }
                        // вниз
                        if (Shootable(b, maxR + 1, cc) && !_shots.Contains((maxR + 1, cc))) { shot = (maxR + 1, cc); return true; }
                    }
                }
                else
                {
                    // ещё нет направления — попробуем любой ортососед вокруг любого хита кластера
                    foreach (var (pr, pc) in cells)
                        foreach (var (dr, dc) in Ortho)
                        {
                            int nr = pr + dr, nc = pc + dc;
                            if (Shootable(b, nr, nc) && !_shots.Contains((nr, nc)))
                            { shot = (nr, nc); return true; }
                        }
                }
            }
        return false;
    }
}
