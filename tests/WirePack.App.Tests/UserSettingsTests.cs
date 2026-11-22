using Xunit;
using WirePack.App.Settings;

namespace WirePack.App.Tests;

public class UserSettingsTests
{
    [Fact]
    public void UserSettings_DefaultValues_AreCorrect()
    {
        var settings = new UserSettings();
        Assert.False(settings.SkipWelcomeWindow);
    }
}
