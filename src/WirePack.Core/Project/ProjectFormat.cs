namespace WirePack.Core.Project;

/// <summary>
/// Параметры поддерживаемого формата проекта.
/// </summary>
public static class ProjectFormat
{
    /// <summary>
    /// Текущая версия формата данных проекта.
    /// </summary>
    public const int CurrentVersion = 1;

    /// <summary>
    /// Максимальное количество проводов в проекте.
    /// </summary>
    public const int MaxWireCount = 100;

    /// <summary>
    /// Проверяет, поддерживается ли указанная версия формата.
    /// </summary>
    public static bool IsSupportedVersion(int version) => version == CurrentVersion;
}
