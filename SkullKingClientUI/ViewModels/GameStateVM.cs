using SkullKingCore.Core.Cards.Base;
using SkullKingCore.Core.Game;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace SkullKingClientUI.ViewModels
{
    public class GameStateVM : INotifyPropertyChanged
    {
        private readonly GameState _gs;

        public ObservableCollection<PlayerVM> Players { get; } = new();
        public ObservableCollection<CardVM> CurrentHand { get; } = new();

        private int _currentPlayerIndex = 0;
        public int CurrentPlayerIndex
        {
            get => _currentPlayerIndex;
            set { if (_currentPlayerIndex != value) { _currentPlayerIndex = value; RefreshCurrentHand(); OnPropertyChanged(); } }
        }

        // Round info, with ready-to-bind formatted strings
        private int _currentRound;
        private int _currentSubRound;
        private int _maxRounds;

        public int CurrentRound
        {
            get => _currentRound;
            set { if (_currentRound != value) { _currentRound = value; OnPropertyChanged(); OnPropertyChanged(nameof(CurrentRoundText)); OnPropertyChanged(nameof(SubRoundText)); } }
        }

        public int CurrentSubRound
        {
            get => _currentSubRound;
            set { if (_currentSubRound != value) { _currentSubRound = value; OnPropertyChanged(); OnPropertyChanged(nameof(SubRoundText)); } }
        }

        public int MaxRounds
        {
            get => _maxRounds;
            set { if (_maxRounds != value) { _maxRounds = value; OnPropertyChanged(); OnPropertyChanged(nameof(CurrentRoundText)); } }
        }

        public string CurrentRoundText => $"Round: {CurrentRound} / {MaxRounds}";
        public string SubRoundText => $"Trick: {CurrentSubRound} / {CurrentRound}";

        private string _trickMessage = string.Empty;
        public string TrickMessage
        {
            get => _trickMessage;
            set { if (_trickMessage != value) { _trickMessage = value; OnPropertyChanged(); } }
        }

        public GameStateVM(GameState gs)
        {
            _gs = gs;

            for (int i = 0; i < gs.Players.Count; i++)
                Players.Add(new PlayerVM(gs.Players[i], i));

            CurrentRound = gs.CurrentRound;
            CurrentSubRound = gs.CurrentSubRound;
            MaxRounds = gs.MaxRounds;

            RefreshCurrentHand(); // for CurrentPlayerIndex = 0
        }

        /// <summary> Update each player's CardInPlay (index-aligned with Players). </summary>
        public void SetCardsForPlayers(IList<Card?> cards)
        {
            int n = Players.Count;
            for (int i = 0; i < n; i++)
            {
                CardVM? vm = null;
                if (cards != null && i < cards.Count && cards[i] != null)
                    vm = new CardVM(cards[i]!);
                Players[i].CardInPlay = vm;
            }
        }

        /// <summary> Rebuilds the current player's hand from the underlying GameState. </summary>
        public void RefreshCurrentHand()
        {
            CurrentHand.Clear();
            if (_gs.Players == null || _gs.Players.Count == 0) return;

            var idx = _currentPlayerIndex;
            if (idx < 0 || idx >= _gs.Players.Count) return;

            var hand = _gs.Players[idx].Hand;
            if (hand == null) return;

            foreach (var c in hand)
                CurrentHand.Add(new CardVM(c));
        }

        /// <summary> Apply changes from a new/updated GameState object. </summary>
        public void ApplyGameState(GameState updated)
        {
            // Update round fields
            CurrentRound = updated.CurrentRound;
            CurrentSubRound = updated.CurrentSubRound;
            MaxRounds = updated.MaxRounds;

            // Update players total/round stats in place (simple resync)
            for (int i = 0; i < Players.Count && i < updated.Players.Count; i++)
            {
                var pvm = Players[i];
                var p = updated.Players[i];

                pvm.Name = p.Name;
                pvm.TotalScore = p.TotalScore;

                // resync rounds (simple approach: clear+add)
                pvm.Rounds.Clear();
                foreach (var rs in p.RoundStats)
                    pvm.Rounds.Add(new RoundStatVM(rs));
            }

            // Replace hands in backing GameState and refresh current hand
            _gs.Players.Clear();
            foreach (var p in updated.Players) _gs.Players.Add(p);
            RefreshCurrentHand();
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        private void OnPropertyChanged([CallerMemberName] string n = "") =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(n));
    }
}
