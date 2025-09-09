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
        private GameState _gs;

        public ObservableCollection<PlayerVM> Players { get; } = new();
        public ObservableCollection<CardVM> CurrentHand { get; } = new();

        private int _currentPlayerIndex;
        public int CurrentPlayerIndex
        {
            get => _currentPlayerIndex;
            set { if (_currentPlayerIndex != value) { _currentPlayerIndex = value; OnPropertyChanged(); RefreshCurrentHand(); } }
        }

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

            // Build players
            for (int i = 0; i < gs.Players.Count; i++)
                Players.Add(new PlayerVM(gs.Players[i], i));

            CurrentRound = gs.CurrentRound;
            CurrentSubRound = gs.CurrentSubRound;
            MaxRounds = gs.MaxRounds;
            CurrentPlayerIndex = gs.StartingPlayerIndex; // or track elsewhere

            // Map CardsInPlay -> PlayerVM.CardInPlay
            SyncCardsInPlayFromState(gs);

            RefreshCurrentHand();
        }

        public void ApplyGameState(GameState updated)
        {
            _gs = updated;

            // Update rounds + derived labels
            CurrentRound = updated.CurrentRound;
            CurrentSubRound = updated.CurrentSubRound;
            MaxRounds = updated.MaxRounds;

            // (Optional) If current player comes from server:
            // CurrentPlayerIndex = updated.StartingPlayerIndex;

            // Sync players count (add/remove)
            while (Players.Count < updated.Players.Count)
                Players.Add(new PlayerVM(updated.Players[Players.Count], Players.Count));
            while (Players.Count > updated.Players.Count)
                Players.RemoveAt(Players.Count - 1);

            // Update each player's data in-place
            int n = Players.Count;
            for (int i = 0; i < n; i++)
            {
                var pvm = Players[i];
                var p = updated.Players[i];

                pvm.Name = p.Name;
                pvm.TotalScore = p.TotalScore;

                // Round stats (simple resync)
                pvm.Rounds.Clear();
                if (p.RoundStats != null)
                {
                    foreach (var rs in p.RoundStats)
                        pvm.Rounds.Add(new RoundStatVM(rs));
                }
            }

            // Cards in play mapping
            SyncCardsInPlayFromState(updated);

            // Rebuild current hand from backing model
            RefreshCurrentHand();
        }

        private void SyncCardsInPlayFromState(GameState state)
        {
            var cip = state.CardsInPlay ?? new List<Card>();
            for (int i = 0; i < Players.Count; i++)
            {
                Card? c = (i < cip.Count) ? cip[i] : null;
                Players[i].CardInPlay = c != null ? new CardVM(c) : null;
            }
        }

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

        public event PropertyChangedEventHandler? PropertyChanged;
        private void OnPropertyChanged([CallerMemberName] string n = "") =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(n));
    }
}
