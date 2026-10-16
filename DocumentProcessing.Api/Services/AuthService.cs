using DocumentProcessing.Api.Data;
using DocumentProcessing.Api.Models;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace DocumentProcessing.Api.Services;

public class AuthService
{
    private readonly AppDbContext _context;
    private readonly PasswordService _passwordService;

    public AuthService(AppDbContext context, PasswordService passwordService)
    {
        _context = context;
        _passwordService = passwordService;
    }

    public async Task<User?> RegisterAsync(string email, string password)
    {
        var normalizedEmail = email.Trim().ToLowerInvariant();

        var emailExists = await _context.Users
            .AnyAsync(user => user.Email == normalizedEmail);

        if (emailExists)
        {
            return null;
        }

        var user = new User
        {
            Email = normalizedEmail,
            CreatedAt = DateTime.UtcNow
        };

        user.PasswordHash = _passwordService.HashPassword(user, password);

        _context.Users.Add(user);

        try
        {
            await _context.SaveChangesAsync();
            return user;
        }
        catch (DbUpdateException exception) when (
            exception.InnerException is PostgresException
            {
                SqlState: PostgresErrorCodes.UniqueViolation
            })
        {
            // The unique index also protects against two simultaneous registrations.
            return null;
        }
    }

    public async Task<User?> LoginAsync(string email, string password)
    {
        var normalizedEmail = email.Trim().ToLowerInvariant();

        var user = await _context.Users
        .FirstOrDefaultAsync(user => user.Email == normalizedEmail);

        if (user is null)
        {
            return null;
        }

        var passwordIsCorrect = _passwordService.VerifyPassword(
            user,
            user.PasswordHash,
            password);

        return passwordIsCorrect ? user : null;
    }

}