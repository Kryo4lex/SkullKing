using SkullKingCore.Core.Cards.Base;
using System.Windows.Media;

namespace SkullKingClientUI.ViewModels
{
    /// <summary>UI card wrapper: resolves ImageSource once via ImageManager (no converters).</summary>
    public class CardVM
    {
        public Card Model { get; }
        public string ImageName => Model.ImageName;
        public ImageSource? Image { get; }

        public CardVM(Card model)
        {
            Model = model;
            Image = string.IsNullOrWhiteSpace(Model.ImageName) ? null : ImageManager.Instance.Get(Model.ImageName);
        }
    }
}
