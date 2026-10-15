using DocumentProcessing.Api.Models;
using DocumentProcessing.Api.Services;
using Microsoft.AspNetCore.Identity;

namespace DocumentProcessing.Api.Tests;

public class PasswordServiceTests
{
    [Fact]
    public void HashPassword_CanVerifyCorrectPassword_ButRejectsWrongPassword()
    {
        var service = new PasswordService(new PasswordHasher<User>());
        var user = new User { Email = "learner@example.com" };
        var password = "example-only-password";

        var hash = service.HashPassword(user, password);

        Assert.NotEqual(password, hash);
        Assert.True(service.VerifyPassword(user, hash, password));
        Assert.False(service.VerifyPassword(user, hash, "wrong-password"));
    }
}