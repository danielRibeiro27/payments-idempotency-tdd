using System.Security.Cryptography;
using System.Text;

public class Hasher
{
    // Method to compute a SHA256 hash from a string and return as a hex string
    public static string ComputeSha256Hash(string input)
    {
        // Convert the input string to a byte array
        byte[] inputBytes = Encoding.UTF8.GetBytes(input);

        // Compute the hash (using the modern static method)
        byte[] hashBytes = SHA256.HashData(inputBytes);

        // Convert the byte array to a hexadecimal string
        return Convert.ToHexString(hashBytes);
    }
}