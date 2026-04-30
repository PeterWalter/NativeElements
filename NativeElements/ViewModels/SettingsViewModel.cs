using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NativeElements.Data;
using NativeElements.Models;

namespace NativeElements.ViewModels;

public partial class SettingsViewModel : ObservableObject
{
    [ObservableProperty]
    private double dpi = 300;

    [ObservableProperty]
    private string pageSize = "A4";

    [ObservableProperty]
    private double scaleFactor = 1.0;

    [ObservableProperty]
    private bool gridEnabled = true;

    [ObservableProperty]
    private double gridSize = 1.0;

    [ObservableProperty]
    private double overlapCm = 1.0;

    [ObservableProperty]
    private bool isSaving = false;

    public SettingsViewModel()
    {
        // prepare PageSizes
        PageSizes = new System.Collections.ObjectModel.ObservableCollection<string> { "A4", "A3", "Letter" };
        // Load settings asynchronously without awaiting
        _ = LoadSettingsAsync();
    }

    [ObservableProperty]
    private System.Collections.ObjectModel.ObservableCollection<string> pageSizes;

    [RelayCommand]
    public async Task LoadSettingsAsync()
    {
        try
        {
            var dpiSetting = await DatabaseService.QueryAsync<AppSettings>("SELECT * FROM app_settings WHERE Key = ?", "DPI");
            if (dpiSetting.Count > 0 && double.TryParse(dpiSetting[0].Value, out double dpiValue))
                Dpi = dpiValue;

            var pageSizeSetting = await DatabaseService.QueryAsync<AppSettings>("SELECT * FROM app_settings WHERE Key = ?", "PageSize");
            if (pageSizeSetting.Count > 0)
                PageSize = pageSizeSetting[0].Value;

            var scaleSetting = await DatabaseService.QueryAsync<AppSettings>("SELECT * FROM app_settings WHERE Key = ?", "ScaleFactor");
            if (scaleSetting.Count > 0 && double.TryParse(scaleSetting[0].Value, out double scaleValue))
                ScaleFactor = scaleValue;

            var gridEnabledSetting = await DatabaseService.QueryAsync<AppSettings>("SELECT * FROM app_settings WHERE Key = ?", "GridEnabled");
            if (gridEnabledSetting.Count > 0)
                GridEnabled = gridEnabledSetting[0].Value.ToLower() == "true";

            var gridSizeSetting = await DatabaseService.QueryAsync<AppSettings>("SELECT * FROM app_settings WHERE Key = ?", "GridSize");
            if (gridSizeSetting.Count > 0 && double.TryParse(gridSizeSetting[0].Value, out double gridSizeValue))
                GridSize = gridSizeValue;

            var overlapSetting = await DatabaseService.QueryAsync<AppSettings>("SELECT * FROM app_settings WHERE Key = ?", "OverlapCm");
            if (overlapSetting.Count > 0 && double.TryParse(overlapSetting[0].Value, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out double overlapValue))
                OverlapCm = overlapValue;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error loading settings: {ex.Message}");
        }
    }

    [RelayCommand]
    public async Task SaveSettingsAsync()
    {
        try
        {
            IsSaving = true;

            // Use SettingsService helpers for DPI, PageSize and Overlap
            await Services.SettingsService.SetDpiAsync((int)Dpi);
            await Services.SettingsService.SetPageSizeAsync(PageSize);
            await Services.SettingsService.SetOverlapCmAsync(OverlapCm);

            // Persist other settings
            var settings = new List<AppSettings>
            {
                new AppSettings { Key = "ScaleFactor", Value = ScaleFactor.ToString(System.Globalization.CultureInfo.InvariantCulture), DataType = "double" },
                new AppSettings { Key = "GridEnabled", Value = GridEnabled.ToString().ToLower(), DataType = "bool" },
                new AppSettings { Key = "GridSize", Value = GridSize.ToString(System.Globalization.CultureInfo.InvariantCulture), DataType = "double" }
            };

            foreach (var setting in settings)
            {
                // Try update, if no row exists, insert
                var existing = (await DatabaseService.QueryAsync<AppSettings>("SELECT * FROM app_settings WHERE Key = ?", setting.Key)).FirstOrDefault();
                if (existing != null)
                {
                    existing.Value = setting.Value;
                    await DatabaseService.UpdateAsync(existing);
                }
                else
                {
                    await DatabaseService.InsertAsync(setting);
                }
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error saving settings: {ex.Message}");
        }
        finally
        {
            IsSaving = false;
        }
    }

    [RelayCommand]
    public async Task ResetSettingsAsync()
    {
        Dpi = 300;
        PageSize = "A4";
        ScaleFactor = 1.0;
        GridEnabled = true;
        GridSize = 1.0;

        await SaveSettingsAsync();
    }
}
