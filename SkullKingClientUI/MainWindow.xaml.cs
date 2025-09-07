using SkullKingClientUI.ViewModels;
using SkullKingCore.Core.Cards;
using SkullKingCore.Core.Cards.Base;
using SkullKingCore.Core.Cards.Implementations;
using SkullKingCore.Core.Game;
using SkullKingCore.Extensions;
using System.Collections.Specialized;
using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls.Primitives;
using System.Windows.Media;

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

        public GameStateVM GameVM { get; private set; } = null!;

        public MainWindow()
        {
            InitializeComponent();

            // Panel ratios
            SetPanelWidths(20, 50, 30);

            // Load images (./Assets)
            var assets = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets");
            if (Directory.Exists(assets))
                ImageManager.Instance.LoadFromFolder(assets);

            // Fake players + round stats + hands
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
                CurrentSubRound = 1,
                StartingPlayerIndex = 0
            };

            GameVM = new GameStateVM(gs);
            DataContext = GameVM;

            /*
            // Cards in play
            GameVM.SetCardsForPlayers(new Card?[]
            {
                new NumberCard(CardType.PURPLE, 7),  // Alice
                new NumberCard(CardType.GREEN, 12),  // Bob
                null,                                // Chloe waiting
                new NumberCard(CardType.BLACK, 1),   // Diego
            });
            */

            List<Card> deck = Deck.CreateDeck();

            var cardsInPlay = deck.Shuffle();

            GameVM.SetCardsForPlayers(cardsInPlay.TakeChunk(4));

            // Initial trick message
            SetTrickMessage("Here there will be infos rom the Server!");

            // Layout updates after load
            Loaded += (_, __) =>
            {
                UpdateCardsGridLayout();
                UpdateHandGridLayout();
            };

            // Reflow middle when player count changes
            if (GameVM.Players is INotifyCollectionChanged playersChanged)
                playersChanged.CollectionChanged += (_, __) => UpdateCardsGridLayout();

            // Reflow hand when card count changes
            if (GameVM.CurrentHand is INotifyCollectionChanged handChanged)
                handChanged.CollectionChanged += (_, __) => UpdateHandGridLayout();
        }

        /// <summary> Change the text shown in the middle TextBox (bottom). </summary>
        public void SetTrickMessage(string text) => GameVM.TrickMessage = text;

        public void SetPanelWidths(double leftPercent, double middlePercent, double rightPercent)
        {
            LeftColumnWidth = new GridLength(leftPercent, GridUnitType.Star);
            MiddleColumnWidth = new GridLength(middlePercent, GridUnitType.Star);
            RightColumnWidth = new GridLength(rightPercent, GridUnitType.Star);
        }

        private void UpdateCardsGridLayout()
        {
            var ug = FindDescendant<UniformGrid>(CardsBoard);
            if (ug == null)
            {
                Dispatcher.BeginInvoke(new Action(UpdateCardsGridLayout));
                return;
            }

            int n = GameVM?.Players?.Count ?? 0;
            if (n <= 0) { ug.Rows = 1; ug.Columns = 1; return; }

            if (n <= 3)
            {
                ug.Rows = 1;
                ug.Columns = n; // 1..3
            }
            else
            {
                ug.Rows = 2;
                ug.Columns = (int)Math.Ceiling(n / 2.0); // 4→2, 5→3, 6→3, 7→4, 8→4...
            }
        }

        private void UpdateHandGridLayout()
        {
            var ug = FindDescendant<UniformGrid>(HandBoard);
            if (ug == null)
            {
                Dispatcher.BeginInvoke(new Action(UpdateHandGridLayout));
                return;
            }

            int n = GameVM?.CurrentHand?.Count ?? 0;
            if (n <= 1)
            {
                ug.Columns = 1;
                ug.Rows = 1;         // 1 card: 1x1
            }
            else if (n == 2)
            {
                ug.Columns = 1;
                ug.Rows = 2;         // 2 cards: 1 column, 2 rows
            }
            else
            {
                ug.Columns = 2;
                ug.Rows = (int)Math.Ceiling(n / 2.0); // >2: 2 columns, rows depend
            }
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

        public event PropertyChangedEventHandler? PropertyChanged;
        private void OnPropertyChanged([CallerMemberName] string name = "") =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
