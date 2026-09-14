using System.Text.Json;
using BeaverWorks.Core.Models;
using BeaverWorks.Core.Services;

namespace BeaverWorks.Core.Persistence;

/// <summary>
/// JSON-file-backed <see cref="IUserBudgetProfileStore"/>, mirroring
/// <see cref="RecentProjectsStore"/>'s per-user layout. Defaults to
/// <c>%LOCALAPPDATA%\BeaverWorks\&lt;username&gt;\budget-profile.json</c>,
/// but accepts a custom root so tests can point it at a temp directory.
/// </summary>
public sealed class UserBudgetProfileStore : IUserBudgetProfileStore
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        WriteIndented = true,
    };

    private readonly string _rootPath;

    public UserBudgetProfileStore(string? rootPath = null)
    {
        _rootPath = rootPath ?? GetDefaultRootPath();
    }

    public static string GetDefaultRootPath()
    {
        var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        return Path.Combine(localAppData, "BeaverWorks");
    }

    public UserBudgetProfile Load(string username)
    {
        var profile = LoadRaw(username);
        var resetApplied = ApplyPeriodReset(profile);

        if (resetApplied)
        {
            Save(username, profile);
        }

        return profile;
    }

    public void Save(string username, UserBudgetProfile profile)
    {
        var filePath = GetFilePath(username);
        var directory = Path.GetDirectoryName(filePath);
        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }

        var json = JsonSerializer.Serialize(profile, SerializerOptions);
        File.WriteAllText(filePath, json);
    }

    private UserBudgetProfile LoadRaw(string username)
    {
        var filePath = GetFilePath(username);
        if (!File.Exists(filePath))
        {
            return UserBudgetProfile.CreateDefault();
        }

        var json = File.ReadAllText(filePath);
        if (string.IsNullOrWhiteSpace(json))
        {
            return UserBudgetProfile.CreateDefault();
        }

        var profile = JsonSerializer.Deserialize<UserBudgetProfile>(json, SerializerOptions);
        return profile ?? UserBudgetProfile.CreateDefault();
    }

    /// <summary>
    /// Resets each dimension of <paramref name="profile"/> independently if
    /// its stored period key no longer matches today's — the mechanism
    /// behind FR-016's "budgets reset at the next calendar boundary,
    /// independently of each other". Returns <see langword="true"/> if
    /// either dimension was reset, so the caller knows to persist the
    /// change.
    /// </summary>
    private static bool ApplyPeriodReset(UserBudgetProfile profile)
    {
        var now = DateTimeOffset.UtcNow;
        var currentTimeKey = BudgetPeriodCalculator.GetTimePeriodKey(now);
        var currentMoneyKey = BudgetPeriodCalculator.GetMoneyPeriodKey(now);

        var resetApplied = false;

        if (profile.TimePeriodKey != currentTimeKey)
        {
            profile.TimeConsumedThisWeekHours = 0;
            profile.TimePeriodKey = currentTimeKey;
            resetApplied = true;
        }

        if (profile.MoneyPeriodKey != currentMoneyKey)
        {
            profile.MoneyConsumedThisMonth = 0;
            profile.MoneyPeriodKey = currentMoneyKey;
            resetApplied = true;
        }

        return resetApplied;
    }

    private string GetFilePath(string username) =>
        Path.Combine(_rootPath, username, "budget-profile.json");
}
