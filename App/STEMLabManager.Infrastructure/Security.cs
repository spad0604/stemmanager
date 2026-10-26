using System.Security.Cryptography;
using System.Text;
namespace STEMLabManager.Infrastructure;

public static class Passwords
{
    public static void Validate(string password)
    {
        if (password.Length < 12 || !password.Any(char.IsUpper) || !password.Any(char.IsLower) || !password.Any(char.IsDigit))
            throw new Domain.BusinessException("Mật khẩu cần ít nhất 12 ký tự, có chữ hoa, chữ thường và số.");
    }
    public static string Hash(string password)
    {
        Validate(password);
        var salt = RandomNumberGenerator.GetBytes(16);
        var hash = Rfc2898DeriveBytes.Pbkdf2(password, salt, 600_000, HashAlgorithmName.SHA256, 32);
        return $"pbkdf2-sha256$600000${Convert.ToBase64String(salt)}${Convert.ToBase64String(hash)}";
    }
    public static bool Verify(string password, string stored)
    {
        try
        {
            var p = stored.Split('$');
            var derived = Rfc2898DeriveBytes.Pbkdf2(password, Convert.FromBase64String(p[2]), int.Parse(p[1]), HashAlgorithmName.SHA256, 32);
            return CryptographicOperations.FixedTimeEquals(derived, Convert.FromBase64String(p[3]));
        }
        catch (Exception e) when (e is FormatException or IndexOutOfRangeException or ArgumentException) { return false; }
    }
}
public sealed class SecretStore
{
    private readonly string keyPath;
    public SecretStore(string directory) { keyPath = Path.Combine(directory, "secrets.key"); }
    private byte[] Key()
    {
        if (!File.Exists(keyPath))
        {
            File.WriteAllBytes(keyPath, RandomNumberGenerator.GetBytes(32));
            if (!OperatingSystem.IsWindows()) File.SetUnixFileMode(keyPath, UnixFileMode.UserRead | UnixFileMode.UserWrite);
        }
        return File.ReadAllBytes(keyPath);
    }
    public byte[] Encrypt(byte[] plain)
    {
        var nonce = RandomNumberGenerator.GetBytes(12); var tag = new byte[16]; var cipher = new byte[plain.Length];
        using var aes = new AesGcm(Key(), 16); aes.Encrypt(nonce, plain, cipher, tag);
        return [.. nonce, .. tag, .. cipher];
    }
    public byte[] Decrypt(byte[] encrypted)
    {
        var plain = new byte[encrypted.Length - 28];
        using var aes = new AesGcm(Key(), 16); aes.Decrypt(encrypted.AsSpan(0,12), encrypted.AsSpan(28), encrypted.AsSpan(12,16), plain);
        return plain;
    }
    public string EncryptText(string text) => Convert.ToBase64String(Encrypt(Encoding.UTF8.GetBytes(text)));
    public string DecryptText(string text) => Encoding.UTF8.GetString(Decrypt(Convert.FromBase64String(text)));
}
