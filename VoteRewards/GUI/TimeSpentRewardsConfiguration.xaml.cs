using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using VoteRewards.Utils;

namespace VoteRewards
{
    public partial class TimeSpentRewardsConfiguration : Window
    {
        public VoteRewards Plugin { get; private set; }

        public TimeSpentRewardsConfiguration(VoteRewards plugin)
        {
            InitializeComponent();
            Plugin = plugin;

            // Ustawiamy DataContext okna na plugin
            DataContext = plugin;

            // Ustawiamy źródło danych DataGrid
            TimeSpentRewardsDataGrid.ItemsSource = Plugin.TimeSpentRewardsConfig.RewardsList;
        }


        private void AddTimeSpentRewardButton_OnClick(object sender, RoutedEventArgs e)
        {
            // Tworzymy nowy obiekt RewardItem i dodajemy go do listy
            var newItem = new RewardItem();
            Plugin.TimeSpentRewardsConfig.RewardsList.Add(newItem);

            // Odświeżamy DataGrid
            TimeSpentRewardsDataGrid.Items.Refresh();
        }

        private void DeleteTimeSpentRewardButton_OnClick(object sender, RoutedEventArgs e)
        {
            // Pobieramy aktualnie wybrany przedmiot
            var selectedItem = TimeSpentRewardsDataGrid.SelectedItem as RewardItem;

            // Jeśli nic nie jest zaznaczone, nic nie robimy
            if (selectedItem == null)
                return;

            // Usuwamy zaznaczony przedmiot z listy
            Plugin.TimeSpentRewardsConfig.RewardsList.Remove(selectedItem);

            // Odświeżamy DataGrid
            TimeSpentRewardsDataGrid.Items.Refresh();
        }

        private void SaveButton_OnClick(object sender, RoutedEventArgs e)
        {
            // Aktualizacja konfiguracji wartościami z kontrolek
            Plugin.TimeSpentRewardsConfig.RewardInterval = int.Parse(RewardIntervalTextBox.Text);
            Plugin.TimeSpentRewardsConfig.NotificationPrefix = NotificationPrefixTextBox.Text;

            // Tutaj dodaj logikę zapisywania listy nagród

            Plugin.Save();
        }


        private void ItemTypeComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var comboBox = sender as ComboBox;
            if (comboBox == null)
            {
                Debug.WriteLine("ComboBox is null");
                return;
            }

            var selectedType = comboBox.SelectedItem as string;
            if (string.IsNullOrWhiteSpace(selectedType))
            {
                Debug.WriteLine("SelectedType is null or whitespace");
                return;
            }

            // Get the current item
            var item = comboBox.DataContext as RewardItem;
            if (item == null)
            {
                Debug.WriteLine("Item is null");
                return;
            }

            // Update the available subtypes for the selected type
            if (!Plugin.AvailableItemSubtypes.TryGetValue(selectedType, out var availableSubtypes))
            {
                Debug.WriteLine($"No available subtypes found for {selectedType}");
                availableSubtypes = new List<string>();
            }

            // Update the available subtypes in the RewardItem
            item.AvailableSubTypeIds = availableSubtypes;

            // Find the other combobox and update its items source
            var parent = comboBox.Parent as FrameworkElement;
            while (!(parent is DataGridRow))
            {
                if (parent == null)
                {
                    Debug.WriteLine("Parent is null");
                    return;
                }
                parent = parent.Parent as FrameworkElement;
            }
            var row = parent as DataGridRow;
            var subtypeComboBox = FindVisualChild<ComboBox>(row, "ItemSubtypeComboBox");
            if (subtypeComboBox != null)
            {
                subtypeComboBox.ItemsSource = availableSubtypes;

                // Set the first item as selected if there are any items, otherwise set it to null
                subtypeComboBox.SelectedItem = availableSubtypes.FirstOrDefault();
            }
            else
            {
                Debug.WriteLine("SubtypeComboBox is null");
            }
        }


        private T FindVisualChild<T>(DependencyObject obj, string name) where T : DependencyObject
        {
            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(obj); i++)
            {
                var child = VisualTreeHelper.GetChild(obj, i);
                if (child is T typedChild && (string.IsNullOrEmpty(name) || (child is FrameworkElement fe && fe.Name == name)))
                {
                    return typedChild;
                }

                var foundChild = FindVisualChild<T>(child, name);
                if (foundChild != null)
                {
                    return foundChild;
                }
            }

            return null;
        }
    }
}
