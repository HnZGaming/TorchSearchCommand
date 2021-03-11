using System.Windows.Controls;
using Torch;
using Torch.API;
using Torch.API.Plugins;
using Utils.Torch;

namespace SearchCommand
{
    public sealed class SearchCommandPlugin : TorchPluginBase, IWpfPlugin
    {
        Persistent<SearchCommandConfig> _config;
        UserControl _userControl;

        public UserControl GetControl() => _config.GetOrCreateUserControl(ref _userControl);
        public SearchCommandConfig Config => _config.Data;

        public override void Init(ITorchBase torch)
        {
            base.Init(torch);

            var configPath = this.MakeConfigFilePath();
            _config = Persistent<SearchCommandConfig>.Load(configPath);
        }
    }
}