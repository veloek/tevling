namespace Tevling.Utils;

public static class GenericExtensions
{
    public static T If<T>(this T obj, bool condition, Func<T, T> @then)
    {
        return condition ? @then(obj) : obj;
    }

    public static TOut If<TIn, TOut>(this TIn obj, bool condition, Func<TIn, TOut> @then, Func<TIn, TOut> @else)
    {
        return condition ? @then(obj) : @else(obj);
    }
}
