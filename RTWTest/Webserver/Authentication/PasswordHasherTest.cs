using RTWWebServer.Providers.Authentication;

namespace RTWTest.Webserver.Authentication;

[TestFixture]
[TestOf(typeof(PasswordHasher))]
public class PasswordHasherTest
{
    private PasswordHasher _passwordHasher = new PasswordHasher();
    
    [Test]
    public void HashPassword_ReturnsSameHash_ForSameInput()
    {
        // Arrange
        var password = "password";
        var salt = _passwordHasher.GenerateSaltValue();

        // Act
        var hashedPassword1 = _passwordHasher.CalcHashedPassword(password, salt);
        var hashedPassword2 = _passwordHasher.CalcHashedPassword(password, salt);

        // Assert
        Assert.That(hashedPassword2, Is.EqualTo(hashedPassword1));
    }

    [Test]
    public void HashPassword_ReturnsDifferentHash_ForDifferentSalt()
    {
        // Arrange
        var password = "password";

        // Act
        var hashedPassword1 = _passwordHasher.CalcHashedPassword(password, _passwordHasher.GenerateSaltValue());
        var hashedPassword2 = _passwordHasher.CalcHashedPassword(password, _passwordHasher.GenerateSaltValue());

        // Assert
        Assert.That(hashedPassword2, Is.Not.EqualTo(hashedPassword1));
    }

    [Test]
    public void HashPassword_ReturnsDifferentHash_ForDifferentPassword()
    {
        // Arrange
        var salt = _passwordHasher.GenerateSaltValue();

        // Act
        var hashedPassword1 = _passwordHasher.CalcHashedPassword("password1", salt);
        var hashedPassword2 = _passwordHasher.CalcHashedPassword("password2", salt);

        // Assert
        Assert.That(hashedPassword2, Is.Not.EqualTo(hashedPassword1));
    }
}