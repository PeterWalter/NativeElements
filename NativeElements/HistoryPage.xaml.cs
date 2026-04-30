using NativeElements.Models;
using NativeElements.Services;
using NativeElements.Data;

namespace NativeElements;

public partial class HistoryPage : ContentPage
{
    public HistoryPage()
    {
        InitializeComponent();
    }

    private async void OnRefreshHistoryClicked(object? sender, EventArgs e)
    {
        try
        {
            var history = await DatabaseService.GetAllAsync<CalculationHistory>();
            HistoryCollectionView.ItemsSource = history.OrderByDescending(h => h.Timestamp).ToList();
        }
        catch (Exception ex)
        {
            await DisplayAlertAsync("Error", $"Failed to load history: {ex.Message}", "OK");
        }
    }

    private async void OnClearHistoryClicked(object? sender, EventArgs e)
    {
        bool confirmed = await DisplayAlertAsync("Clear History", "Delete all calculation history?", "Yes", "No");
        if (!confirmed) return;

        try
        {
            var history = await DatabaseService.GetAllAsync<CalculationHistory>();
            foreach (var item in history)
            {
                await DatabaseService.DeleteAsync(item);
            }
            HistoryCollectionView.ItemsSource = null;
        }
        catch (Exception ex)
        {
            await DisplayAlertAsync("Error", $"Failed to clear history: {ex.Message}", "OK");
        }
    }
}
