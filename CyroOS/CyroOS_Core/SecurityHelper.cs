using System.Security.Cryptography;
using System.Text;

public static class SecurityHelper 
{
    // AES-256 standardında mühürleme
    public static string Encrypt(string text, string key) 
    {
        if (string.IsNullOrEmpty(text)) return string.Empty;

        using Aes aes = Aes.Create();
        aes.Key = SHA256.HashData(Encoding.UTF8.GetBytes(key));
        aes.IV = new byte[16]; 

        using var encryptor = aes.CreateEncryptor(aes.Key, aes.IV);
        byte[] buffer = Encoding.UTF8.GetBytes(text);
        byte[] encrypted = encryptor.TransformFinalBlock(buffer, 0, buffer.Length);

        return Convert.ToBase64String(encrypted);
    }

    // Mühür çözme işlemi
    public static string Decrypt(string cipherText, string key) 
    {
        if (string.IsNullOrEmpty(cipherText)) return string.Empty;

        try 
        {
            using Aes aes = Aes.Create();
            aes.Key = SHA256.HashData(Encoding.UTF8.GetBytes(key));
            aes.IV = new byte[16];

            using var decryptor = aes.CreateDecryptor(aes.Key, aes.IV);
            byte[] buffer = Convert.FromBase64String(cipherText);
            byte[] decrypted = decryptor.TransformFinalBlock(buffer, 0, buffer.Length);

            return Encoding.UTF8.GetString(decrypted);
        } 
        catch 
        { 
            return "!!! DECRYPT_ERROR: ACCESS_DENIED !!!"; 
        }
    }
}