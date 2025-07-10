using RTWWebServer.Enums;

namespace RTWTest.Webserver.Authentication;

[TestFixture]
public class UserRoleExtensionsTests
{
    [Test]
    public void ToRoleString_ShouldReturnCorrectLowercaseString()
    {
        // Arrange & Act & Assert
        Assert.That(UserRole.Guest.ToRoleString(), Is.EqualTo("guest"));
        Assert.That(UserRole.Normal.ToRoleString(), Is.EqualTo("normal"));
        Assert.That(UserRole.Admin.ToRoleString(), Is.EqualTo("admin"));
    }

    [Test]
    [TestCase("guest", UserRole.Guest)]
    [TestCase("normal", UserRole.Normal)]
    [TestCase("admin", UserRole.Admin)]
    [TestCase("GUEST", UserRole.Guest)]
    [TestCase("Admin", UserRole.Admin)]
    public void FromRoleString_WithValidRoleStrings_ShouldReturnCorrectEnum(string input, UserRole expected)
    {
        Assert.That(UserRoleExtensions.FromRoleString(input), Is.EqualTo(expected));
    }

    [Test]
    [TestCase("invalid")]
    [TestCase("")]
    [TestCase("   ")]
    public void FromRoleString_WithInvalidInput_ShouldReturnGuest(string input)
    {
        Assert.That(UserRoleExtensions.FromRoleString(input), Is.EqualTo(UserRole.Guest));
    }

    [Test]
    public void RoundTrip_ShouldMaintainOriginalValue()
    {
        // Test round-trip conversion for all enum values
        foreach (UserRole role in Enum.GetValues<UserRole>())
        {
            var roleString = role.ToRoleString();
            var convertedBack = UserRoleExtensions.FromRoleString(roleString);
            Assert.That(convertedBack, Is.EqualTo(role));
        }
    }
}
