namespace Battleship.Core;

public enum CellState
{
    Empty,
    Ship,
    Hit,
    Miss
}

public enum Orientation
{
    Horizontal,
    Vertical
}

public enum ShotResult
{
    Miss,
    Hit,
    Sunk,
    AlreadyTried
}

public enum PlayerTurn
{
    Human,
    Computer
}
