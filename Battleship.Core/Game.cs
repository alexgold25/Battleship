namespace Battleship.Core;

public enum GameState { Placement, InProgress, Finished }

public class Game
{
    public Board Player { get; } = new();
    public Board Enemy { get; } = new();
    public GameState State { get;   set; } = GameState.Placement;
    public bool PlayerTurn { get;   set; } = true;

    public void Reset()
    {
        Player.Clear();
        Enemy.Clear();
        State = GameState.Placement;
        PlayerTurn = true;
    }

    public void Start()
    {
        State = GameState.InProgress;
    }

    public bool IsOver(out string? winner)
    {
        if (!Player.HasShipsLeft())
        {
            winner = "Компьютер";
            return true;
        }
        if (!Enemy.HasShipsLeft())
        {
            winner = "Игрок";
            return true;
        }
        winner = null;
        return false;
    }
}
