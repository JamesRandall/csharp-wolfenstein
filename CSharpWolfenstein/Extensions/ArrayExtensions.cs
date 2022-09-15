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

    public static T[,] FlipHorizontal<T>(this T[,] array)
    {
        var height = array.GetLength(0);
        var width = array.GetLength(1);
        var flippedCells = new T[array.GetLength(0), array.GetLength(1)];
        Enumerable.Range(0,height).Iter(row =>
            Enumerable.Range(0,width).Iter(col => 
                flippedCells[row,width-1-col] = array[row,col]
            )
        );
        return flippedCells;
    }
}