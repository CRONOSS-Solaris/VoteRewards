using System.Diagnostics;
using System.Threading;
using System.Windows;
using System.Windows.Controls;

namespace VoteRewards
{
    public partial class VoteRewardsControl : UserControl
    {
        private VoteRewardsMain Plugin { get; }
        private ItemConfiguration _itemConfigurationWindow;
        private TimeSpentRewardsConfiguration _timeSpentRewardsConfigurationWindow;
        private ReferralItemConfiguration _referralItemConfiguration;
        private EventCodeItemConfiguration _eventCodeItemConfiguration;
        private TopVotersBenefit _topVotersBenefit;

        private VoteRewardsControl()
        {
            InitializeComponent();
        }

        public VoteRewardsControl(VoteRewardsMain plugin) : this()
        {
            Plugin = plugin;
            DataContext = plugin.Config;
        }

        private void SaveButton_OnClick(object sender, RoutedEventArgs e)
        {
            Plugin.Save();

            var currentConfig = DataContext as VoteRewardsConfig;

            if (currentConfig != null)
            {
                Plugin.UpdateConfig(currentConfig);
            }
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

        private void OpenTopVotersBenefitButton_Click(object sender, RoutedEventArgs e)
        {
            // Sprawdzamy, czy okno jest już otwarte
            if (_topVotersBenefit == null)
            {
                // Tworzymy nowy wątek STA do otwarcia okna
                Thread thread = new Thread(() =>
                {
                    _topVotersBenefit = new TopVotersBenefit(Plugin);
                    _topVotersBenefit.Closed += (s, args) => _topVotersBenefit = null;
                    _topVotersBenefit.Show();

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
                _topVotersBenefit.Dispatcher.Invoke(() =>
                {
                    _topVotersBenefit.Activate();
                });
            }
        }

        private void OpenReferralItemConfiguration_Click(object sender, RoutedEventArgs e)
        {
            // Sprawdzamy, czy okno jest już otwarte
            if (_referralItemConfiguration == null)
            {
                // Tworzymy nowy wątek STA do otwarcia okna
                Thread thread = new Thread(() =>
                {
                    _referralItemConfiguration = new ReferralItemConfiguration(Plugin);
                    _referralItemConfiguration.Closed += (s, args) => _referralItemConfiguration = null;
                    _referralItemConfiguration.Show();

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
                _referralItemConfiguration.Dispatcher.Invoke(() =>
                {
                    _referralItemConfiguration.Activate();
                });
            }
        }

        private void OpenEventCodeItemConfiguration_Click(object sender, RoutedEventArgs e)
        {
            // Sprawdzamy, czy okno jest już otwarte
            if (_eventCodeItemConfiguration == null)
            {
                // Tworzymy nowy wątek STA do otwarcia okna
                Thread thread = new Thread(() =>
                {
                    _eventCodeItemConfiguration = new EventCodeItemConfiguration(Plugin);
                    _eventCodeItemConfiguration.Closed += (s, args) => _eventCodeItemConfiguration = null;
                    _eventCodeItemConfiguration.Show();

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
                _eventCodeItemConfiguration.Dispatcher.Invoke(() =>
                {
                    _eventCodeItemConfiguration.Activate();
                });
            }
        }

        private void OpenTimeSpentRewardsConfigurationButton_Click(object sender, RoutedEventArgs e)
        {
            if (_timeSpentRewardsConfigurationWindow == null)
            {
                Thread thread = new Thread(() =>
                {
                    _timeSpentRewardsConfigurationWindow = new TimeSpentRewardsConfiguration(Plugin);
                    _timeSpentRewardsConfigurationWindow.Closed += (s, args) => _timeSpentRewardsConfigurationWindow = null;
                    _timeSpentRewardsConfigurationWindow.Show();

                    System.Windows.Threading.Dispatcher.Run();
                });

                thread.SetApartmentState(ApartmentState.STA);
                thread.Start();
            }
            else
            {
                _timeSpentRewardsConfigurationWindow.Dispatcher.Invoke(() =>
                {
                    _timeSpentRewardsConfigurationWindow.Activate();
                });
            }
        }

        public void UpdateButtonState(bool isEnabled)
        {
            OpenItemConfigurationButton.IsEnabled = isEnabled;
            OpenTimeSpentRewardsConfigurationButton.IsEnabled = isEnabled;
            OpenReferralRewardConfig.IsEnabled = isEnabled;
            OpenTopVotersBenefitConfigurationButton.IsEnabled = isEnabled;
            EventCodeRewardConfiguration.IsEnabled = isEnabled;
            ServerStatusMessage.Visibility = isEnabled ? Visibility.Collapsed : Visibility.Visible;
            ServerStatusMessage2.Visibility = isEnabled ? Visibility.Collapsed : Visibility.Visible;
            ServerStatusMessage3.Visibility = isEnabled ? Visibility.Collapsed : Visibility.Visible;
        }

        private void SupportButton_OnClick(object sender, RoutedEventArgs e)
        {
            string discordInviteLink = "https://discord.gg/BUnUnXz5xJ";
            Process.Start(new ProcessStartInfo
            {
                FileName = discordInviteLink,
                UseShellExecute = true
            });
        }

    }
}