using System.Threading;
using System.Windows;
using System.Windows.Controls;

namespace VoteRewards
{
    public partial class VoteRewardsControl : UserControl
    {
        private VoteRewards Plugin { get; }
        private ItemConfiguration _itemConfigurationWindow; // Dodajemy zmienną do przechowywania instancji okna konfiguracji przedmiotów

        private VoteRewardsControl()
        {
            InitializeComponent();
        }

        public VoteRewardsControl(VoteRewards plugin) : this()
        {
            Plugin = plugin;
            DataContext = plugin.Config;
        }

        private void SaveButton_OnClick(object sender, RoutedEventArgs e)
        {
            Plugin.Save();
        }

        private void OpenItemConfigurationButton_Click(object sender, RoutedEventArgs e)
        {
            // Sprawdzamy, czy okno jest już otwarte
            if (_itemConfigurationWindow == null)
            {
                // Tworzymy nowy wątek STA do otwarcia okna
                Thread thread = new Thread(() =>
                {
                    _itemConfigurationWindow = new ItemConfiguration(Plugin);
                    _itemConfigurationWindow.Closed += (s, args) => _itemConfigurationWindow = null;
                    _itemConfigurationWindow.Show();

                    // Rozpoczynamy pętlę zdarzeń dla tego wątku
                    System.Windows.Threading.Dispatcher.Run();
                });

                // Ustawiamy wątek jako STA
                thread.SetApartmentState(ApartmentState.STA);

                // Rozpoczynamy wątek
                thread.Start();
            }
            else
            {
                // Jeśli okno jest już otwarte, przenosimy je na wierzch
                _itemConfigurationWindow.Dispatcher.Invoke(() =>
                {
                    _itemConfigurationWindow.Activate();
                });
            }
        }


        public void UpdateButtonState(bool isEnabled)
        {
            OpenItemConfigurationButton.IsEnabled = isEnabled;
        }
    }
}
//koniec