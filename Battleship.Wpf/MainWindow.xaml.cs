using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Battleship.Core;

namespace Battleship.Wpf
{
    public partial class MainWindow : Window
    {
        private readonly Game _game = new();
        private readonly Button[,] _playerButtons = new Button[Board.Size, Board.Size];
        private readonly Button[,] _enemyButtons = new Button[Board.Size, Board.Size];
        private readonly int[] _shipSizes = { 4, 3, 3, 2, 2, 2, 1, 1, 1, 1 };
        private int _currentShipIndex = 0;
        private Orientation _currentOrientation = Orientation.Horizontal;

        public MainWindow()
        {
            InitializeComponent();
            InitBoards();
            _game.NewGame();
            UpdateShipLabels();
        }

        private void InitBoards()
        {
            for (int x = 0; x < Board.Size; x++)
            for (int y = 0; y < Board.Size; y++)
            {
                var btn = new Button { Style = (Style)FindResource("CellButtonStyle") };
                btn.Tag = (x, y);
                btn.Click += PlayerCell_Click;
                playerGrid.Children.Add(btn);
                _playerButtons[x, y] = btn;

                var enemyBtn = new Button { Style = (Style)FindResource("CellButtonStyle") };
                enemyBtn.Tag = (x, y);
                enemyBtn.Click += EnemyCell_Click;
                enemyBtn.IsEnabled = false;
                enemyGrid.Children.Add(enemyBtn);
                _enemyButtons[x, y] = enemyBtn;
            }
        }

        private void PlayerCell_Click(object sender, RoutedEventArgs e)
        {
            if (_currentShipIndex >= _shipSizes.Length || _game.Started)
                return;
            var (x, y) = ((int, int))((Button)sender).Tag;
            int length = _shipSizes[_currentShipIndex];
            if (_game.PlayerBoard.PlaceShip(x, y, length, _currentOrientation))
            {
                foreach (var (sx, sy) in _game.PlayerBoard.Ships.Last().Coordinates)
                    _playerButtons[sx, sy].Background = Brushes.Navy;
                _currentShipIndex++;
                if (_currentShipIndex >= _shipSizes.Length)
                    btnStart.IsEnabled = true;
                UpdateShipLabels();
            }
        }

        private void EnemyCell_Click(object sender, RoutedEventArgs e)
        {
            if (!_game.Started || _game.CurrentTurn != PlayerTurn.Human)
                return;
            var (x, y) = ((int, int))((Button)sender).Tag;
            var result = _game.PlayerShoot(x, y);
            UpdateEnemyView();
            UpdateShipLabels();
            if (_game.ComputerBoard.ShipsRemaining == 0)
            {
                MessageBox.Show("Вы победили!", "Игра окончена");
                DisableEnemyBoard();
                return;
            }
            UpdateTurnLabel();
            if (_game.CurrentTurn == PlayerTurn.Computer)
                ComputerMove();
        }

        private void ComputerMove()
        {
            do
            {
                _ = _game.ComputerShoot();
                UpdatePlayerView();
                UpdateShipLabels();
                if (_game.PlayerBoard.ShipsRemaining == 0)
                {
                    MessageBox.Show("Компьютер победил", "Игра окончена");
                    DisableEnemyBoard();
                    return;
                }
            } while (_game.CurrentTurn == PlayerTurn.Computer);
            UpdateTurnLabel();
        }

        private void AutoPlace_Click(object sender, RoutedEventArgs e)
        {
            _game.PlayerBoard.Clear();
            _game.PlayerBoard.AutoPlaceShips();
            _currentShipIndex = _shipSizes.Length;
            UpdatePlayerView();
            btnStart.IsEnabled = true;
            UpdateShipLabels();
        }

        private void Start_Click(object sender, RoutedEventArgs e)
        {
            _game.Start();
            btnStart.IsEnabled = false;
            btnAutoPlace.IsEnabled = false;
            playerGrid.IsEnabled = false;
            EnableEnemyBoard();
            UpdateTurnLabel();
            if (_game.CurrentTurn == PlayerTurn.Computer)
                ComputerMove();
        }

        private void Rotate_Click(object sender, RoutedEventArgs e)
        {
            _currentOrientation = _currentOrientation == Orientation.Horizontal ? Orientation.Vertical : Orientation.Horizontal;
        }

        private void UpdatePlayerView()
        {
            for (int x = 0; x < Board.Size; x++)
            for (int y = 0; y < Board.Size; y++)
            {
                var state = _game.PlayerBoard.Cells[x, y];
                var btn = _playerButtons[x, y];
                switch (state)
                {
                    case CellState.Empty:
                        btn.Background = Brushes.LightBlue;
                        break;
                    case CellState.Ship:
                        btn.Background = Brushes.Navy;
                        break;
                    case CellState.Miss:
                        btn.Background = Brushes.LightGray;
                        btn.IsEnabled = false;
                        break;
                    case CellState.Hit:
                        btn.Background = Brushes.Red;
                        btn.IsEnabled = false;
                        break;
                }
            }
        }

        private void UpdateEnemyView()
        {
            for (int x = 0; x < Board.Size; x++)
            for (int y = 0; y < Board.Size; y++)
            {
                var state = _game.ComputerBoard.Cells[x, y];
                var btn = _enemyButtons[x, y];
                switch (state)
                {
                    case CellState.Miss:
                        btn.Background = Brushes.LightGray;
                        btn.IsEnabled = false;
                        break;
                    case CellState.Hit:
                        btn.Background = Brushes.Red;
                        btn.IsEnabled = false;
                        break;
                }
            }
        }

        private void UpdateTurnLabel()
        {
            txtTurn.Text = _game.CurrentTurn == PlayerTurn.Human ? "Игрок" : "Компьютер";
        }

        private void UpdateShipLabels()
        {
            txtPlayerShips.Text = _game.PlayerBoard.ShipsRemaining.ToString();
            txtEnemyShips.Text = _game.ComputerBoard.ShipsRemaining.ToString();
        }

        private void EnableEnemyBoard()
        {
            foreach (var btn in _enemyButtons)
                btn.IsEnabled = true;
            UpdateEnemyView();
        }

        private void DisableEnemyBoard()
        {
            foreach (var btn in _enemyButtons)
                btn.IsEnabled = false;
        }
    }
}
