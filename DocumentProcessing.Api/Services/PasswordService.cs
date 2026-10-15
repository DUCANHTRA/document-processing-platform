using DocumentProcessing.Api.Models;
using Microsoft.AspNetCore.Identity;

namespace DocumentProcessing.Api.Services;

public class PasswordService
{
    private readonly IPasswordHasher<User> _passwordHasher;

    public PasswordService(IPasswordHasher<User> passwordHasher)
    {
        _passwordHasher = passwordHasher;
    }

    public string HashPassword(User user, string password)
    {
        return _passwordHasher.HashPassword(user, password);
    }

    public bool VerifyPassword(User user, string passwordHash, string providedPassword)
    {
        var result = _passwordHasher.VerifyHashedPassword(
            user,
            passwordHash,
            providedPassword
        );

        return result != PasswordVerificationResult.Failed;
    }
}