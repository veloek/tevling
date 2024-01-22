namespace Tevling.Utils;

public static class GenericExtensions
{
    public static T If<T>(this T obj, bool condition, Func<T, T> @then)
        => condition ? @then(obj) : obj;

    public static T If<T>(this T obj, bool condition, Func<T, T> @then, Func<T, T> @else)
        => condition ? @then(obj) : @else(obj);
}
