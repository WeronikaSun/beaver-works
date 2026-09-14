using BeaverWorks.Core.Models;

namespace BeaverWorks.Core.Persistence;

/// <summary>
/// Persists one <see cref="UserBudgetProfile"/> per username, outside any
/// project file.
/// </summary>
public interface IUserBudgetProfileStore
{
    /// <summary>
    /// Loads <paramref name="username"/>'s budget profile, creating a fresh
    /// zero-budget default if none exists yet. Also applies the
    /// period-rollover reset (FR-016): if either dimension's stored period
    /// key is stale, that dimension's consumed total is reset to zero, the
    /// key is updated, and the result is persisted before being returned.
    /// </summary>
    UserBudgetProfile Load(string username);

    void Save(string username, UserBudgetProfile profile);
}
