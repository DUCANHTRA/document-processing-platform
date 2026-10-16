using System.ComponentModel.DataAnnotations;

namespace DocumentProcessing.Api.DTOs;

public class RegisterRequest
{
    [Required]
    [EmailAddress]
    public string Email { get; set; } = "";

    [Required]
    [MinLength(12)]
    public string Password { get; set; } = "";
}