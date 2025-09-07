using SkullKingCore.Core.Game;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace SkullKingClientUI.ViewModels
{
    public class PlayerVM : INotifyPropertyChanged
    {
        public int Index { get; }

        private string _name;
        private int _totalScore;
        private CardVM? _cardInPlay;

        public string Name { get => _name; set { _name = value; OnPropertyChanged(); } }
        public int TotalScore { get => _totalScore; set { _totalScore = value; OnPropertyChanged(); } }
        public ObservableCollection<RoundStatVM> Rounds { get; } = new();

        /// <summary>Null = waiting; otherwise played card.</summary>
        public CardVM? CardInPlay
        {
            get => _cardInPlay;
            set { _cardInPlay = value; OnPropertyChanged(); }
        }

        public PlayerVM(Player p, int index)
        {
            Index = index;
            _name = p.Name;
            _totalScore = p.TotalScore;

            foreach (var rs in p.RoundStats)
                Rounds.Add(new RoundStatVM(rs));
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        private void OnPropertyChanged([CallerMemberName] string n = "") =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(n));
    }
}
