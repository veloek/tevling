using System.Text;

namespace Tevling.Utils;

public static class StringExtensions
{
    public static string ToBase64(this string s)
    {
        byte[] bytes = Encoding.UTF8.GetBytes(s);
        string base64 = Convert.ToBase64String(bytes);

        return base64;
    }

    public static string FromBase64(this string base64)
    {
        byte[] bytes = Convert.FromBase64String(base64);
        string s = Encoding.UTF8.GetString(bytes);

        return s;
    }
}
