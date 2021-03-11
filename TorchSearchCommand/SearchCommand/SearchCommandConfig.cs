using System.Xml.Serialization;
using Torch;
using Torch.Views;

namespace SearchCommand
{
    public sealed class SearchCommandConfig : ViewModel
    {
        int _defaultResultLength = 10;

        [XmlElement]
        [Display(Name = "Default result length")]
        public int DefaultResultLength
        {
            get => _defaultResultLength;
            set => SetValue(ref _defaultResultLength, value);
        }
    }
}