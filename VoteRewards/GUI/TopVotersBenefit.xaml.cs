using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using VoteRewards.Nexus;
using VoteRewards.Utils;

namespace VoteRewards
{
    public partial class TopVotersBenefit : Window
    {
        public VoteRewardsMain Plugin { get; private set; } // Załóżmy, że Plugin to Twoja główna klasa logiki

        public TopVotersBenefit(VoteRewardsMain plugin)
        {
            InitializeComponent();
            Plugin = plugin;
            DataContext = plugin;
            VoteRangeRewardsDataGrid.ItemsSource = Plugin.TopVotersBenefitConfig.VoteRangeRewards;
        }

        private void VoteRangeRewardsDataGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var selectedRange = VoteRangeRewardsDataGrid.SelectedItem as VoteRangeReward;
            RewardItemsDataGrid.ItemsSource = selectedRange?.Rewards;
        }

        private void AddVoteRangeButton_Click(object sender, RoutedEventArgs e)
        {
            // Przykład dodawania nowego zakresu głosowania
            var newRange = new VoteRangeReward { MinVotes = 0, MaxVotes = 0 };

            newRange.Rewards = new List<RewardItem>
            {
                new RewardItem()
            };

            Plugin.TopVotersBenefitConfig.VoteRangeRewards.Add(newRange);
            VoteRangeRewardsDataGrid.Items.Refresh();
        }

        private void RemoveVoteRangeButton_Click(object sender, RoutedEventArgs e)
        {
            var selectedRange = VoteRangeRewardsDataGrid.SelectedItem as VoteRangeReward;
            if (selectedRange != null)
            {
                Plugin.TopVotersBenefitConfig.VoteRangeRewards.Remove(selectedRange);
                VoteRangeRewardsDataGrid.Items.Refresh();
                RewardItemsDataGrid.ItemsSource = null; // Clear items when range is removed
            }
        }

        private void AddRewardButton_Click(object sender, RoutedEventArgs e)
        {
            var selectedRange = VoteRangeRewardsDataGrid.SelectedItem as VoteRangeReward;
            if (selectedRange == null)
            {
                MessageBox.Show("Please select a vote range first.");
                return;
            }

            // Upewnij się, że lista nagród dla wybranego zakresu głosów została zainicjowana
            if (selectedRange.Rewards == null)
            {
                selectedRange.Rewards = new List<RewardItem>();
            }

            var newItem = new RewardItem();
            selectedRange.Rewards.Add(newItem);

            // Odśwież DataGrid z nagrodami dla aktualnie wybranego zakresu głosów
            RewardItemsDataGrid.Items.Refresh();
        }


        private void RemoveRewardButton_Click(object sender, RoutedEventArgs e)
        {
            var selectedRange = VoteRangeRewardsDataGrid.SelectedItem as VoteRangeReward;
            var selectedReward = RewardItemsDataGrid.SelectedItem as RewardItem;
            if (selectedRange != null && selectedReward != null)
            {
                selectedRange.Rewards.Remove(selectedReward);
                RewardItemsDataGrid.Items.Refresh();
            }
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

        private void SaveVoteRanges_Click(object sender, RoutedEventArgs e)
        {
            NexusManager.SendTopVotersBenefitUpdate(Plugin.TopVotersBenefitConfig);
            Plugin.Save();
            MessageBox.Show("Saved successfully!");
        }
    }
}
