using System;
using System.Collections.Generic;

namespace Battleship.Core;

public class AiPlayer
{
    private readonly Random _rnd = new();

    public void AutoPlaceShips(Board board)
    {
        board.AutoPlaceShips();
    }

    public (int x, int y) ChooseShot(Board enemyBoard)
    {
        var available = new List<(int x, int y)>();
        for (int x = 0; x < Board.Size; x++)
            for (int y = 0; y < Board.Size; y++)
            {
                var state = enemyBoard.Cells[x, y];
                if (state == CellState.Empty || state == CellState.Ship)
                    available.Add((x, y));
            }
        if (available.Count == 0)
            return (0, 0);
        return available[_rnd.Next(available.Count)];
    }
}
