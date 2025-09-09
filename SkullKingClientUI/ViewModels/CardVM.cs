using SkullKingCore.Core.Cards.Base;
using System.Windows.Media;

namespace SkullKingClientUI.ViewModels
{
    public class CardVM
    {
        public Card Model { get; }
        public ImageSource? Image { get; }

        private string? _imageName;

        public string ImageName => _imageName ??= GenerateImageName();

        private string GenerateImageName()
        {
            var imageName = Model.CardType.ToString();

            var subTypeStr = Model.SubType()?.ToString();
            if (!string.IsNullOrEmpty(subTypeStr))
            {
                imageName += "_" + subTypeStr;
            }

            return imageName;
        }

        public CardVM(Card model)
        {
            Model = model;
            Image = string.IsNullOrWhiteSpace(ImageName) ? null : ImageManager.Instance.Get(ImageName);
        }
    }
}
