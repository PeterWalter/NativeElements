using NativeElements.Data;
using NativeElements.Models;

namespace NativeElements.Views;

public partial class SettingsPage : ContentPage
{
    public SettingsPage()
    {
        InitializeComponent();
        LoadSettings();
    }

    private async void LoadSettings()
    {
        try
        {
            var dpiSetting = await DatabaseService.QueryAsync<AppSettings>("SELECT * FROM app_settings WHERE Key = ?", "DPI");
            if (dpiSetting.Count > 0)
            {
                DpiEntry.Text = dpiSetting[0].Value;
            }

            var scaleSetting = await DatabaseService.QueryAsync<AppSettings>("SELECT * FROM app_settings WHERE Key = ?", "ScaleFactor");
            if (scaleSetting.Count > 0)
            {
                ScaleFactorEntry.Text = scaleSetting[0].Value;
            }

            var gridEnabledSetting = await DatabaseService.QueryAsync<AppSettings>("SELECT * FROM app_settings WHERE Key = ?", "GridEnabled");
            if (gridEnabledSetting.Count > 0)
            {
                GridEnabledCheckBox.IsChecked = gridEnabledSetting[0].Value == "true";
            }

            var gridSizeSetting = await DatabaseService.QueryAsync<AppSettings>("SELECT * FROM app_settings WHERE Key = ?", "GridSize");
            if (gridSizeSetting.Count > 0)
            {
                GridSizeEntry.Text = gridSizeSetting[0].Value;
            }

            PageSizePicker.SelectedIndex = 0;
        }
        catch (Exception ex)
        {
            await DisplayAlertAsync("Error", $"Failed to load settings: {ex.Message}", "OK");
        }
    }

    private async void OnSaveSettingsClicked(object? sender, EventArgs e)
    {
        try
        {
            var settings = new List<AppSettings>
            {
                new AppSettings { Key = "DPI", Value = DpiEntry.Text, DataType = "int" },
                new AppSettings { Key = "ScaleFactor", Value = ScaleFactorEntry.Text, DataType = "double" },
                new AppSettings { Key = "GridEnabled", Value = GridEnabledCheckBox.IsChecked.ToString().ToLower(), DataType = "bool" },
                new AppSettings { Key = "GridSize", Value = GridSizeEntry.Text, DataType = "double" }
            };

            foreach (var setting in settings)
            {
                await DatabaseService.UpdateAsync(setting);
            }

            await DisplayAlertAsync("Success", "Settings saved successfully!", "OK");
        }
        catch (Exception ex)
        {
            await DisplayAlertAsync("Error", $"Failed to save settings: {ex.Message}", "OK");
        }
    }

    private async void OnResetClicked(object? sender, EventArgs e)
    {
        bool confirmed = await DisplayAlertAsync("Reset Settings", "Reset all settings to defaults?", "Yes", "No");
        if (!confirmed) return;

        try
        {
            DpiEntry.Text = "300";
            ScaleFactorEntry.Text = "1.0";
            GridEnabledCheckBox.IsChecked = true;
            GridSizeEntry.Text = "1.0";
            PageSizePicker.SelectedIndex = 0;

            OnSaveSettingsClicked(null, EventArgs.Empty);
        }
        catch (Exception ex)
        {
            await DisplayAlertAsync("Error", $"Failed to reset settings: {ex.Message}", "OK");
        }
    }
}
