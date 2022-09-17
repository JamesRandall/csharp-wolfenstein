using System.Collections.Immutable;

namespace CSharpWolfenstein.Extensions;

public static class ArrayExtensions
{
    public static IEnumerable<((int x, int y) position, T item)> Enumerate<T>(this T[,] array)
    {
        for (int rowIndex = 0; rowIndex < array.GetLength(0); rowIndex++)
        {
            for (int colIndex = 0; colIndex < array.GetLength(1); colIndex++)
            {
                yield return ((colIndex, rowIndex), array[rowIndex, colIndex]);
            }
        }
    }

    public static ImmutableArray<ImmutableArray<T>> FlipHorizontal<T>(this ImmutableArray<ImmutableArray<T>> array)
    {
        return
            ImmutableArray.Create(
                array.Select(row =>
                    ImmutableArray.Create(row.Reverse().ToArray())
                ).ToArray());
    }
}