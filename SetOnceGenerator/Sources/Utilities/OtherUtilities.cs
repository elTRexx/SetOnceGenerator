namespace SetOnceGenerator.Sources.Utilities
{
  public static class OtherUtilities
  {
    /// <summary>
    /// Null-safe comparison between two IEnumerables, testing for the same elements in the same order
    /// see : https://antonymale.co.uk/implementing-equals-and-gethashcode-in-csharp.html
    /// </summary>
    /// <remarks>
    /// This method is to <see cref="Enumerable.SequenceEqual{TSource}(IEnumerable{TSource}, IEnumerable{TSource})"/>
    /// as <see cref="object.Equals(object, object)"/> is to <see cref="object.Equals(object)"/>.
    /// </remarks>
    public static bool SequenceEqual<T>(IEnumerable<T>? item1, IEnumerable<T>? item2, IEqualityComparer<T> comparer = null)
    {
      if (item1 is null)
        return item2 is null;
      if (ReferenceEquals(item1, item2))
        return true;

      if (item1.Count() != item2.Count())
        return false;
      T i1, i2;
      for (int i = 0; i<item1.Count(); i++)
      {
        i1 = item1.ElementAt(i);
        i2 = item2.ElementAt(i);
        if (comparer != null)
        {
          if (!comparer.Equals(i1, i2))
            return false;
        }
        else if (!i1?.Equals(i2) ?? true)
          return false;
      }


      return true;
      //return item1.SequenceEqual(item2, comparer);
    }

    /// <summary>
    /// Gets a HashCode of a collection by combining the hash codes of its elements. Takes order into account
    /// see : https://antonymale.co.uk/implementing-equals-and-gethashcode-in-csharp.html
    /// </summary>
    /// <typeparam name="T">Type of element</typeparam>
    /// <param name="source">Collection to generate the hash code for</param>
    /// <returns>Generated hash code</returns>
    public static int GetHashCodeOfElements<T>(this IEnumerable<T> source, IEqualityComparer<T> comparer = null)
    {
      if (source == null)
        return 0;//throw new ArgumentNullException(nameof(source));

      comparer ??= EqualityComparer<T>.Default;

      unchecked
      {
        int hash = 17;
        foreach (var element in source)
        {
          hash = hash * 23 + comparer.GetHashCode(element);
        }
        return hash;
      }
    }
  }
}
