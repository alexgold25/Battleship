using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Battleship.Core;

namespace Battleship.Wpf
{
    public partial class MainWindow : Window
    {
        private readonly Game _game = new();
        private readonly AiPlayer _ai = new();
        private readonly Button[,] _playerBtns = new Button[Board.Size, Board.Size];
        private readonly Button[,] _enemyBtns = new Button[Board.Size, Board.Size];
        private readonly Random _rnd = new();

        public MainWindow()
        {
            InitializeComponent();
            if (DesignerProperties.GetIsInDesignMode(this)) return;

            BuildBoards();
            UpdateUI();
        }

        private void BuildBoards()
        {
            // Player
            playerGrid.Children.Clear();
            for (int r = 0; r < Board.Size; r++)
                for (int c = 0; c < Board.Size; c++)
                {
                    var b = MakeCellButton();
                    b.Tag = (r, c, "player");
                    b.Click += PlayerCell_Click;   // ставим/убираем корабль на этапе Placement
                    _playerBtns[r, c] = b;
                    playerGrid.Children.Add(b);
                }

            // Enemy
            enemyGrid.Children.Clear();
            for (int r = 0; r < Board.Size; r++)
                for (int c = 0; c < Board.Size; c++)
                {
                    var b = MakeCellButton();
                    b.Tag = (r, c, "enemy");
                    b.Click += EnemyCell_Click;    // стреляем по врагу во время игры
                    _enemyBtns[r, c] = b;
                    enemyGrid.Children.Add(b);
                }
        }

        private Button MakeCellButton()
        {
            var b = new Button { Style = (Style)FindResource("CellButtonStyle") };
            b.Focusable = false;
            return b;
        }

        // ===== UI helpers =====
        private static Brush ColMiss = new SolidColorBrush(Color.FromRgb(0x8a, 0x99, 0xa6));
        private static Brush ColShip = (Brush)new BrushConverter().ConvertFrom("#2a6ea8");
        private static Brush ColHit = (Brush)new BrushConverter().ConvertFrom("#E55353");
        private static Brush ColFog = (Brush)new BrushConverter().ConvertFrom("#15202B");
        private static Brush ColMissDot = (Brush)new BrushConverter().ConvertFrom("#8A99A6");


        private void UpdateUI()
        {
            txtTurn.Text = _game.State switch
            {
                GameState.Placement => "Расстановка",
                GameState.InProgress => _game.PlayerTurn ? "Игрок" : "Компьютер",
                GameState.Finished => "Завершено",
                _ => ""
            };

            // --- Player board ---
            for (int r = 0; r < Board.Size; r++)
                for (int c = 0; c < Board.Size; c++)
                {
                    var st = _game.Player.Cells[r, c];
                    var b = _playerBtns[r, c];
                    b.IsEnabled = _game.State == GameState.Placement;
                    b.Content = st == CellState.Miss ? "•" : "";
                    b.FontSize = st == CellState.Miss ? 18 : 16;
                    b.Foreground = st == CellState.Miss ? ColMissDot : Brushes.White;
                    b.Background = st switch
                    {
                        CellState.Ship => ColShip,
                        CellState.Hit => ColHit,
                        CellState.Miss => ColFog,
                        _ => ColFog
                    };
                }

            // --- Enemy board ---
            for (int r = 0; r < Board.Size; r++)
                for (int c = 0; c < Board.Size; c++)
                {
                    var st = _game.Enemy.Cells[r, c];
                    var b = _enemyBtns[r, c];
                    b.IsEnabled = _game.State == GameState.InProgress && _game.PlayerTurn &&
                                  st != CellState.Hit && st != CellState.Miss;
                    b.Content = st == CellState.Miss ? "•" : "";
                    b.FontSize = st == CellState.Miss ? 18 : 16;
                    b.Foreground = st == CellState.Miss ? ColMissDot : Brushes.White;
                    b.Background = st switch
                    {
                        CellState.Hit => ColHit,
                        CellState.Miss => ColFog,
                        _ => ColFog
                    };
                }

            // --- Статусы + прогресс ---
            int shipsP = CountShipsRemaining(_game.Player);
            int shipsE = CountShipsRemaining(_game.Enemy);
            pbPlayerShips.Value = shipsP;
            pbEnemyShips.Value = shipsE;

            txtPlayerShips.Text = $" {shipsP}/10";
            txtEnemyShips.Text = $" {shipsE}/10";

            btnStart.IsEnabled = _game.State == GameState.Placement && _game.Player.ValidateFleet().ok;

            txtTurn.Text = _game.State switch
            {
                GameState.Placement => "Расстановка",
                GameState.InProgress => _game.PlayerTurn ? "Игрок" : "Компьютер",
                GameState.Finished => "Завершено",
                _ => ""
            };
        }

        private static int CountDecks(Board b)
        {
            int n = 0;
            for (int r = 0; r < Board.Size; r++)
                for (int c = 0; c < Board.Size; c++)
                    if (b.Cells[r, c] == CellState.Ship) n++;
            return n;
        }

        // Считает "живые" корабли: группы, где есть хотя бы одна клетка Ship (а не все Hit)
        private static int CountShipsRemaining(Board b)
        {
            bool[,] vis = new bool[Board.Size, Board.Size];
            int count = 0;

            for (int r = 0; r < Board.Size; r++)
                for (int c = 0; c < Board.Size; c++)
                {
                    if (vis[r, c]) continue;
                    if (b.Cells[r, c] != CellState.Ship && b.Cells[r, c] != CellState.Hit) continue;

                    // BFS по палубам (Ship/Hit)
                    var q = new System.Collections.Generic.Queue<(int r, int c)>();
                    q.Enqueue((r, c)); vis[r, c] = true;
                    bool hasAlive = b.Cells[r, c] == CellState.Ship;

                    while (q.Count > 0)
                    {
                        var (cr, cc) = q.Dequeue();
                        foreach (var (dr, dc) in new[] { (-1, 0), (1, 0), (0, -1), (0, 1) })
                        {
                            int nr = cr + dr, nc = cc + dc;
                            if (nr < 0 || nr >= Board.Size || nc < 0 || nc >= Board.Size) continue;
                            if (vis[nr, nc]) continue;
                            if (b.Cells[nr, nc] != CellState.Ship && b.Cells[nr, nc] != CellState.Hit) continue;

                            vis[nr, nc] = true; q.Enqueue((nr, nc));
                            if (b.Cells[nr, nc] == CellState.Ship) hasAlive = true;
                        }
                    }

                    if (hasAlive) count++;
                }
            return count;
        }

        // ===== Actions =====

        private void PlayerCell_Click(object sender, RoutedEventArgs e)
        {
            if (_game.State != GameState.Placement) return;
            var (r, c, _) = ((ValueTuple<int, int, string>)((Button)sender).Tag);
            _game.Player.ToggleShipCell(r, c);
            UpdateUI();
        }

        private void EnemyCell_Click(object sender, RoutedEventArgs e)
        {
            if (_game.State != GameState.InProgress || !_game.PlayerTurn) return;
            var (r, c, _) = ((ValueTuple<int, int, string>)((Button)sender).Tag);

            if (_game.Enemy.Shoot(r, c, out bool hit, out bool sunk))
            {
                _game.PlayerTurn = hit; // если попали — ещё ход игрока
            }
            UpdateUI();

            if (_game.IsOver(out var w))
            {
                _game.State = GameState.Finished;
                MessageBox.Show($"Победил: {w}");
                UpdateUI();
                return;
            }

            if (!_game.PlayerTurn) ComputerTurn();
        }

        private void ComputerTurn()
        {
            int safety = 0;
            while (_game.State == GameState.InProgress && !_game.PlayerTurn)
            {
                if (++safety > 200) { _game.PlayerTurn = true; break; }

                var shot = _ai.NextShot(_game.Player.Cells);
                if (!_game.Player.Shoot(shot.r, shot.c, out bool hit, out bool sunk))
                {   // цель успела стать Miss/Hit — отдадим ход игроку
                    _game.PlayerTurn = true;
                    break;
                }

                _ai.ObserveShotResult(shot, hit, sunk, _game.Player.Cells);
                if (!hit) _game.PlayerTurn = true;

                UpdateUI();

                if (_game.IsOver(out var w))
                {
                    _game.State = GameState.Finished;
                    MessageBox.Show($"Победил: {w}");
                    UpdateUI();
                    break;
                }
            }
        }



        // ===== Buttons =====

        private void New_Click(object sender, RoutedEventArgs e)
        {
            _game.Reset();
            _ai.Reset();                  
            BuildBoards();
            UpdateUI();
        }

        private void AutoPlace_Click(object sender, RoutedEventArgs e)
        {
            if (_game.State != GameState.Placement) return;

            _game.Player.Clear();
            AutoPlaceFleet(_game.Player);
            UpdateUI();
        }

        private void Start_Click(object sender, RoutedEventArgs e)
        {
            if (_game.State != GameState.Placement) return;

            var (ok, reason) = _game.Player.ValidateFleet();
            if (!ok) { MessageBox.Show(reason ?? "Неверная расстановка"); return; }

            _game.Enemy.Clear();
            AutoPlaceFleet(_game.Enemy);

            _ai.Reset();                  
            _game.State = GameState.InProgress;
            _game.PlayerTurn = true;
            UpdateUI();
        }

        // ===== Helpers =====

        private void AutoPlaceFleet(Board b)
        {
            // набор: 1x4, 2x3, 3x2, 4x1
            int[] ships = { 4, 3, 3, 2, 2, 2, 1, 1, 1, 1 };

            int tries = 0;
            while (true)
            {
                tries++;
                if (tries > 5000) { b.Clear(); tries = 0; }

                b.Clear();
                bool ok = true;

                foreach (var len in ships)
                {
                    if (!TryPlace(b, len))
                    { ok = false; break; }
                }

                if (!ok) continue;

                if (b.ValidateFleet().ok) return;
            }
        }

        private bool TryPlace(Board b, int len)
        {
            for (int attempt = 0; attempt < 200; attempt++)
            {
                bool vertical = _rnd.Next(2) == 0;
                int r = _rnd.Next(Board.Size - (vertical ? len - 1 : 0));
                int c = _rnd.Next(Board.Size - (!vertical ? len - 1 : 0));

                // проверка пересечений и касаний
                bool bad = false;
                for (int i = 0; i < len; i++)
                {
                    int rr = r + (vertical ? i : 0);
                    int cc = c + (vertical ? 0 : i);
                    if (b.Cells[rr, cc] != CellState.Empty) { bad = true; break; }

                    // запрет касаний
                    for (int dr = -1; dr <= 1; dr++)
                        for (int dc = -1; dc <= 1; dc++)
                        {
                            int nr = rr + dr, nc = cc + dc;
                            if (b.InBounds(nr, nc) && b.Cells[nr, nc] == CellState.Ship) { bad = true; }
                        }
                    if (bad) break;
                }
                if (bad) continue;

                for (int i = 0; i < len; i++)
                {
                    int rr = r + (vertical ? i : 0);
                    int cc = c + (vertical ? 0 : i);
                    b.Cells[rr, cc] = CellState.Ship;
                }
                return true;
            }
            return false;
        }
    }
}
