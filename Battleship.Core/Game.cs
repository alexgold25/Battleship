using System;

namespace Battleship.Core;

public class Game
{
    public Board PlayerBoard { get; } = new();
    public Board ComputerBoard { get; } = new();
    private readonly AiPlayer _ai = new();

    public PlayerTurn CurrentTurn { get; private set; }
    public bool Started { get; private set; }

    public void NewGame()
    {
        PlayerBoard.Clear();
        ComputerBoard.Clear();
        _ai.AutoPlaceShips(ComputerBoard);
        Started = false;
    }

    public void Start()
    {
        if (Started) return;
        Started = true;
        CurrentTurn = Random.Shared.Next(2) == 0 ? PlayerTurn.Human : PlayerTurn.Computer;
    }

    public ShotResult PlayerShoot(int x, int y)
    {
        if (!Started || CurrentTurn != PlayerTurn.Human)
            return ShotResult.Miss;
        var res = ComputerBoard.Shoot(x, y);
        if (res == ShotResult.Miss || res == ShotResult.AlreadyTried)
            CurrentTurn = PlayerTurn.Computer;
        return res;
    }

    public ShotResult ComputerShoot()
    {
        if (!Started || CurrentTurn != PlayerTurn.Computer)
            return ShotResult.Miss;
        var (x, y) = _ai.ChooseShot(PlayerBoard);
        var res = PlayerBoard.Shoot(x, y);
        if (res == ShotResult.Miss || res == ShotResult.AlreadyTried)
            CurrentTurn = PlayerTurn.Human;
        return res;
    }
}
