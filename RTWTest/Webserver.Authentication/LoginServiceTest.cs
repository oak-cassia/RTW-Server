using Microsoft.Extensions.Logging;
using RTWWebServer.Authentication;
using RTWWebServer.Database.Repository;
using RTWWebServer.Service;
using Moq;
using MySqlConnector;
using NetworkDefinition.ErrorCode;
using RTWWebServer.Database.Data;
using RTWWebServer.DTO.response;

namespace RTWTest.Webserver.Authentication;

[TestFixture]
[TestOf(typeof(LoginService))]
public class LoginServiceTest
{
    private readonly Mock<IAccountRepository> _accountRepositoryMock = new();
    private readonly Mock<IPasswordHasher> _passwordHasherMock = new();
    private readonly IGuidGenerator _guidGeneratorMock = new GuidGenerator();
    private readonly Mock<IGuestRepository> _guestRepositoryMock = new();
    private readonly Mock<ILogger<LoginService>> _loggerMock = new();
    

    [Test]
    public async Task LoginAsync_AccountNotFound()
    {
        

        
    }
}