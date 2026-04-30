using NativeElements.Data;
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
            var existing = (await DatabaseService.QueryAsync<AppSettings>("SELECT * FROM AppSettings WHERE Key = ?", "DPI")).FirstOrDefault();
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
    }
}