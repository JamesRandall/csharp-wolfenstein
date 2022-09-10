namespace CSharpWolfenstein.Extensions;

public static class LinqExtensions
{
    public static async Task<IReadOnlyCollection<T>> ToReadOnlyCollectionAsync<T>(this IEnumerable<Task<T>> enumerable)
    {
        return await Task.WhenAll(enumerable);
    }
    
    public static async Task<T[]> ToArrayAsync<T>(this IEnumerable<Task<T>> enumerable)
    {
        return await Task.WhenAll(enumerable);
    }

    public static void Iter<T>(this IEnumerable<T> sequence, Action<T> action)
    {
        foreach (T item in sequence) action(item);
    }
}