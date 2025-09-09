using Microsoft.AspNetCore.Mvc;
using SkullKingClientUI.ViewModels;
using SkullKingCore.Controller;
using SkullKingCore.Core.Cards;
using SkullKingCore.Core.Cards.Base;
using SkullKingCore.Core.Cards.Implementations; // if you use your own implementations
using SkullKingCore.Core.Game;
using SkullKingCore.Network.FileRpc;
using SkullKingCore.Network.WebRpc.Rpc;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using static System.Formats.Asn1.AsnWriter;

namespace SkullKingClientUI
{
    public partial class MainWindow : Window, INotifyPropertyChanged
    {
        // Percent widths (Star sizing)
        private GridLength _leftColumnWidth;
        private GridLength _middleColumnWidth;
        private GridLength _rightColumnWidth;

        public GridLength LeftColumnWidth { get => _leftColumnWidth; set { _leftColumnWidth = value; OnPropertyChanged(); } }
        public GridLength MiddleColumnWidth { get => _middleColumnWidth; set { _middleColumnWidth = value; OnPropertyChanged(); } }
        public GridLength RightColumnWidth { get => _rightColumnWidth; set { _rightColumnWidth = value; OnPropertyChanged(); } }

        public GameStateVM? GameVM { get; private set; }

        public MainWindow()
        {
            InitializeComponent();

            // Panel ratios
            SetPanelWidths(20, 50, 30);

            // Load images (./Assets)
            var assets = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets");
            if (Directory.Exists(assets))
                ImageManager.Instance.LoadFromFolder(assets);

            // Build initial VM (debug or your first server snapshot)
            //SetupDebugGameState();
            AttachVm(GameVM);

            // Initial UI message
            SetTrickMessage("Waiting for server…");

            // Reflow on first measure + when the right panel changes size
            Loaded += (_, __) =>
            {
                UpdateCardsGridLayout();
                UpdateHandGridLayout();
            };
            HandBoard.SizeChanged += (_, __) => UpdateHandGridLayout();

            ConnectToServer();

        }

        private async void ConnectToServer()
        {
            string baseUrl = "http://localhost:1234/";
            string clientId = "NETCPU1";

            await using var conn = await WebRpcConnection.ConnectAsync(baseUrl, clientId, CancellationToken.None);

            var controller = new LocalConsoleCPUController("NETCPU1");
            var dispatcher = new RpcFileDispatcher<LocalConsoleCPUController>(controller);

            // SUBSCRIBE BEFORE starting the loop
            EventHandler handler = null!;
            handler = (_, __) =>
            {
                var gs = controller.GameState;        // make sure your controller sets this in every RPC
                if (gs != null)
                    Dispatcher.Invoke(() => SetGameState(gs)); // UI thread
            };
            dispatcher.GameStateUpdated += handler;

            try
            {
                await conn.RunClientLoopAsync((m, a) =>
                    dispatcher.DispatchAsync(m, a ?? Array.Empty<object?>()));
            }
            finally
            {
                dispatcher.GameStateUpdated -= handler; // avoid leaks
            }
        }


        // --------- Public API you call with live updates ----------
        public void SetGameState(GameState snapshot)
        {
            Dispatcher.Invoke(() =>
            {
                if (GameVM == null)
                {
                    GameVM = new GameStateVM(snapshot);
                    DataContext = GameVM;
                    AttachVm(GameVM);
                }
                else
                {
                    GameVM.ApplyGameState(snapshot); // updates players, rounds, hand, cards-in-play
                }

                UpdateCardsGridLayout();
                UpdateHandGridLayout();
            });
        }

        public void SetTrickMessage(string text)
        {
            if (GameVM != null) GameVM.TrickMessage = text;
        }
        // ----------------------------------------------------------

        private void AttachVm(GameStateVM? vm)
        {
            if (vm == null) return;

            if (vm.Players is INotifyCollectionChanged playersChanged)
                playersChanged.CollectionChanged += (_, __) => UpdateCardsGridLayout();

            if (vm.CurrentHand is INotifyCollectionChanged handChanged)
                handChanged.CollectionChanged += (_, __) => UpdateHandGridLayout();

            DataContext = vm;
        }

        public void SetPanelWidths(double leftPercent, double middlePercent, double rightPercent)
        {
            LeftColumnWidth = new GridLength(leftPercent, GridUnitType.Star);
            MiddleColumnWidth = new GridLength(middlePercent, GridUnitType.Star);
            RightColumnWidth = new GridLength(rightPercent, GridUnitType.Star);
        }

        private void UpdateCardsGridLayout()
        {
            var ug = FindDescendant<UniformGrid>(CardsBoard);
            if (ug == null) { Dispatcher.BeginInvoke(new Action(UpdateCardsGridLayout)); return; }

            int n = GameVM?.Players?.Count ?? 0;
            if (n <= 0) { ug.Rows = 1; ug.Columns = 1; return; }

            if (n <= 3) { ug.Rows = 1; ug.Columns = n; }
            else { ug.Rows = 2; ug.Columns = (int)Math.Ceiling(n / 2.0); }
        }

        private void UpdateHandGridLayout()
        {
            var ug = FindDescendant<UniformGrid>(HandBoard);
            if (ug == null) { Dispatcher.BeginInvoke(new Action(UpdateHandGridLayout)); return; }

            int n = GameVM?.CurrentHand?.Count ?? 0;
            if (n <= 1) { ug.Columns = 1; ug.Rows = 1; }
            else if (n == 2) { ug.Columns = 1; ug.Rows = 2; }
            else { ug.Columns = 2; ug.Rows = (int)Math.Ceiling(n / 2.0); }
        }

        private static T? FindDescendant<T>(DependencyObject root) where T : DependencyObject
        {
            int count = VisualTreeHelper.GetChildrenCount(root);
            for (int i = 0; i < count; i++)
            {
                var child = VisualTreeHelper.GetChild(root, i);
                if (child is T t) return t;
                var deeper = FindDescendant<T>(child);
                if (deeper != null) return deeper;
            }
            return null;
        }

        // Simple debug bootstrap (replace with your first server snapshot)
        private void SetupDebugGameState()
        {
            var players = new List<Player>
            {
                new Player("1","Alice")
                {
                    TotalScore = 42,
                    RoundStats = new List<RoundStat>
                    {
                        new RoundStat(1,2){ ActualWins=2, BonusPoints=10 },
                        new RoundStat(2,1){ ActualWins=0, BonusPoints=0 },
                        new RoundStat(3,0){ ActualWins=1, BonusPoints=0 },
                    },
                    Hand = new List<Card>
                    {
                        new NumberCard(CardType.PURPLE, 7),
                        new NumberCard(CardType.GREEN, 12),
                        new NumberCard(CardType.BLACK, 1),
                        new NumberCard(CardType.YELLOW, 9),
                        new NumberCard(CardType.PURPLE, 3),
                    }
                },
                new Player("2","Bob")
                {
                    TotalScore = 36,
                    RoundStats = new List<RoundStat>
                    {
                        new RoundStat(1,1){ ActualWins=1, BonusPoints=5 },
                        new RoundStat(2,2){ ActualWins=2, BonusPoints=0 },
                        new RoundStat(3,1){ ActualWins=1, BonusPoints=0 },
                    },
                    Hand = new List<Card>
                    {
                        new NumberCard(CardType.GREEN, 1),
                        new NumberCard(CardType.GREEN, 8),
                        new NumberCard(CardType.BLACK, 10),
                    }
                },
                new Player("3","Chloe")
                {
                    TotalScore = 28,
                    RoundStats = new List<RoundStat>
                    {
                        new RoundStat(1,0){ ActualWins=0, BonusPoints=0 },
                        new RoundStat(2,1){ ActualWins=1, BonusPoints=10 },
                        new RoundStat(3,2){ ActualWins=1, BonusPoints=0 },
                    },
                    Hand = new List<Card>
                    {
                        new NumberCard(CardType.YELLOW, 2),
                        new NumberCard(CardType.PURPLE, 11),
                    }
                },
                new Player("4","Diego")
                {
                    TotalScore = 54,
                    RoundStats = new List<RoundStat>
                    {
                        new RoundStat(1,2){ ActualWins=3, BonusPoints=15 },
                        new RoundStat(2,0){ ActualWins=0, BonusPoints=0 },
                        new RoundStat(3,1){ ActualWins=1, BonusPoints=5 },
                    },
                    Hand = new List<Card>
                    {
                        new NumberCard(CardType.BLACK, 7),
                        new NumberCard(CardType.BLACK, 12),
                        new NumberCard(CardType.YELLOW, 4),
                    }
                }
            };

            var gs = new GameState(players, startRound: 3, maxRounds: 10, deck: new List<Card>())
            {
                CurrentSubRound = 0,
                StartingPlayerIndex = 0,
                CardsInPlay = new List<Card>
                {
                    new NumberCard(CardType.PURPLE, 7),
                    new NumberCard(CardType.GREEN, 12),
                    null!, // pretend not played yet (will be treated as missing)
                    new NumberCard(CardType.BLACK, 1)
                }
            };

            GameVM = new GameStateVM(gs);
            DataContext = GameVM;
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        private void OnPropertyChanged([CallerMemberName] string name = "") =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
