using NativeElements.Data;
using NativeElements.Models;
using System.Linq;
using System.Threading.Tasks;

namespace NativeElements.Services
{
    public static class SettingsService
    {
        public static async Task<int> GetDpiAsync()
        {
            await DatabaseService.Initialize();
            var all = await DatabaseService.GetAllAsync<AppSettings>();
            var dpiStr = all.FirstOrDefault(s => s.Key == "DPI")?.Value ?? "300";
            if (int.TryParse(dpiStr, out int v)) return v;
            return 300;
        }

        public static async Task SetDpiAsync(int dpi)
        {
            await DatabaseService.Initialize();
            var existing = (await DatabaseService.QueryAsync<AppSettings>("SELECT * FROM app_settings WHERE Key = ?", "DPI")).FirstOrDefault();
            if (existing != null)
            {
                existing.Value = dpi.ToString();
                await DatabaseService.UpdateAsync(existing);
            }
            else
            {
                await DatabaseService.InsertAsync(new AppSettings { Key = "DPI", Value = dpi.ToString(), DataType = "int" });
            }
        }

        public static async Task<double> GetOverlapCmAsync()
        {
            await DatabaseService.Initialize();
            var existing = (await DatabaseService.QueryAsync<AppSettings>("SELECT * FROM app_settings WHERE Key = ?", "OverlapCm")).FirstOrDefault();
            if (existing != null && double.TryParse(existing.Value, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out double v))
                return v;
            return 1.0; // default 1.0 cm
        }

        public static async Task SetOverlapCmAsync(double cm)
        {
            await DatabaseService.Initialize();
            var existing = (await DatabaseService.QueryAsync<AppSettings>("SELECT * FROM app_settings WHERE Key = ?", "OverlapCm")).FirstOrDefault();
            if (existing != null)
            {
                existing.Value = cm.ToString(System.Globalization.CultureInfo.InvariantCulture);
                await DatabaseService.UpdateAsync(existing);
            }
            else
            {
                await DatabaseService.InsertAsync(new AppSettings { Key = "OverlapCm", Value = cm.ToString(System.Globalization.CultureInfo.InvariantCulture), DataType = "double" });
            }
        }

        public static async Task<string> GetPageSizeAsync()
        {
            await DatabaseService.Initialize();
            var existing = (await DatabaseService.QueryAsync<AppSettings>("SELECT * FROM app_settings WHERE Key = ?", "PageSize")).FirstOrDefault();
            return existing?.Value ?? "A4";
        }

        public static async Task SetPageSizeAsync(string pageSize)
        {
            await DatabaseService.Initialize();
            var existing = (await DatabaseService.QueryAsync<AppSettings>("SELECT * FROM app_settings WHERE Key = ?", "PageSize")).FirstOrDefault();
            if (existing != null)
            {
                existing.Value = pageSize;
                await DatabaseService.UpdateAsync(existing);
            }
            else
            {
                await DatabaseService.InsertAsync(new AppSettings { Key = "PageSize", Value = pageSize, DataType = "string" });
            }
        }
    }
}
