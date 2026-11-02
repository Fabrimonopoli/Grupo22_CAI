using System.ComponentModel.DataAnnotations;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Identity;
using Users.API.Data;
using Users.API.DTOs;
using Users.API.Exceptions;
using Users.API.Models;

namespace Users.API.Services;

/// <summary>Registro y autenticación; nunca devuelve la entidad que contiene la contraseña.</summary>
public sealed class UserService(UserRepository repository)
{
    private const string HashPrefix = "$identity$";
    private readonly PasswordHasher<User> _hasher = new();

    public async Task<RegisterUserResponse> RegisterAsync(RegisterUserRequest request)
    {
        Validate(request);
        var user = new User
        {
            Nombre = request.Nombre.Trim(),
            Apellido = request.Apellido.Trim(),
            Email = UserRepository.NormalizeEmail(request.Email),
            FechaRegistro = DateTime.UtcNow,
            Activo = true,
            IntentosFallidos = 0
        };
        user.PasswordHash = HashPrefix + _hasher.HashPassword(user, request.Password);
        await repository.CreateAsync(user);
        return new RegisterUserResponse
        {
            Id = user.Id,
            Nombre = user.Nombre,
            Apellido = user.Apellido,
            Email = user.Email,
            FechaRegistro = user.FechaRegistro,
            Activo = user.Activo
        };
    }

    public async Task<LoginUserResponse> LoginAsync(LoginUserRequest request)
    {
        Validate(request);
        var result = await repository.ProcessLoginAsync(request.Email, user => CheckPassword(user, request.Password));
        if (result.Outcome == LoginOutcome.LockedAttempts)
            throw new BusinessRuleException(UserErrorCodes.Usr004, "Su cuenta fue bloqueada por superar el máximo de intentos fallidos. Contacte a soporte.");
        if (result.Outcome == LoginOutcome.LockedFraud)
            throw new BusinessRuleException(UserErrorCodes.Usr005, "Su cuenta fue suspendida por razones de seguridad. Contacte a soporte.");
        if (result.Outcome != LoginOutcome.Success || result.User is not { } user)
            throw new BusinessRuleException(UserErrorCodes.Usr003, "Credenciales incorrectas.");
        return new LoginUserResponse { Id = user.Id, Nombre = user.Nombre, Apellido = user.Apellido, Email = user.Email };
    }

    public async Task<UserLookupResponse> GetByIdAsync(Guid id)
    {
        var user = id == Guid.Empty ? null : await repository.GetByIdAsync(id);
        if (user is null)
            throw new UserNotFoundException();
        return new(user.Id, user.Nombre, user.Apellido, user.Email, user.Activo);
    }

    private PasswordCheckResult CheckPassword(User user, string password)
    {
        if (user.PasswordHash.StartsWith(HashPrefix, StringComparison.Ordinal))
        {
            PasswordVerificationResult result;
            try
            {
                result = _hasher.VerifyHashedPassword(user, user.PasswordHash[HashPrefix.Length..], password);
            }
            catch (FormatException) { return new(false); }
            if (result == PasswordVerificationResult.Failed)
                return new(false);
            return new(true, result == PasswordVerificationResult.SuccessRehashNeeded
                ? HashPrefix + _hasher.HashPassword(user, password) : null);
        }
        // Compatibilidad temporal: solo una clave correcta migra Base64; nunca se registra ni devuelve.
        byte[] legacy;
        try
        {
            legacy = Convert.FromBase64String(user.PasswordHash);
        }
        catch (FormatException) { return new(false); }
        var candidate = Encoding.UTF8.GetBytes(password);
        try
        {
            if (!CryptographicOperations.FixedTimeEquals(legacy, candidate))
                return new(false);
            return new(true, HashPrefix + _hasher.HashPassword(user, password));
        }
        finally
        {
            CryptographicOperations.ZeroMemory(legacy);
            CryptographicOperations.ZeroMemory(candidate);
        }
    }

    private static void Validate(object? request)
    {
        if (request is null)
            throw new BusinessRuleException(UserErrorCodes.Usr002, "Los datos del usuario son inválidos.");
        var errors = new List<ValidationResult>();
        Validator.TryValidateObject(request, new ValidationContext(request), errors, validateAllProperties: true);
        var email = request switch
        {
            RegisterUserRequest r => r.Email,
            LoginUserRequest r => r.Email,
            _ => ""
        };
        if (!string.IsNullOrWhiteSpace(email) && email.Any(char.IsWhiteSpace))
            errors.Add(new ValidationResult("El email no puede contener espacios."));
        if (errors.Count > 0)
            throw new BusinessRuleException(UserErrorCodes.Usr002,
                string.Join("; ", errors.Select(x => x.ErrorMessage).Distinct()));
    }
}
