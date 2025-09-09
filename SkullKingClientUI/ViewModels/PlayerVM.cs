using SkullKingCore.Core.Game;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace SkullKingClientUI.ViewModels
{
    public class PlayerVM : INotifyPropertyChanged
    {
        public int Index { get; }

        private string _name = "";
        private int _totalScore;
        private CardVM? _cardInPlay;

        public string Name { get => _name; set { _name = value; OnPropertyChanged(); } }
        public int TotalScore { get => _totalScore; set { _totalScore = value; OnPropertyChanged(); } }

        public ObservableCollection<RoundStatVM> Rounds { get; } = new();

        /// <summary>Null = has not played yet.</summary>
        public CardVM? CardInPlay
        {
            get => _cardInPlay;
            set { _cardInPlay = value; OnPropertyChanged(); }
        }

        public PlayerVM(Player model, int index)
        {
            Index = index;
            _name = model.Name;
            _totalScore = model.TotalScore;

            if (model.RoundStats != null)
            {
                foreach (var rs in model.RoundStats)
                    Rounds.Add(new RoundStatVM(rs));
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        private void OnPropertyChanged([CallerMemberName] string n = "") =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(n));
    }
}
