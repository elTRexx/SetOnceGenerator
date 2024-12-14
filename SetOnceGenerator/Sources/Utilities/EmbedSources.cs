using System.Text;

namespace SetOnceGenerator
{
  /// <summary>
  /// Modified from https://github.com/andrewlock/StronglyTypedId/blob/master/src/StronglyTypedIds/EmbeddedSources.cs
  /// of andrewlock
  /// note : original code is licensed under MIT lisence
  /// see : https://github.com/andrewlock/StronglyTypedId/blob/master/LICENSE
  /// </summary>
  internal static class EmbedSources
  {
    const string EMBEDING_ATTRIBUTES = "SET_ONCE_GENERATOR_EMBED_ATTRIBUTES";

    public static string LoadSourceCode(string sourceName)
    {
      var _thisAssembly = typeof(EmbedSources).Assembly;

      var sourceStream = _thisAssembly.GetManifestResourceStream(sourceName);

      if (sourceStream == null)
        throw new ArgumentException($"Cannot find {sourceName} source among [{string.Join(", ", _thisAssembly.GetManifestResourceNames())}] in this assembly.");

      using var reader = new StreamReader(sourceStream, Encoding.UTF8);

      return reader.ReadToEnd();
    }

    public static string LoadAttributeSourceCode(string attributeSourceName)
    {
      var source = LoadSourceCode($"SetOnceGenerator.Embedded.Sources.{attributeSourceName}.cs");

      var newsource = source
        .Replace($"//#if {EMBEDING_ATTRIBUTES}", $"#if {EMBEDING_ATTRIBUTES}")
        .Replace("//#endif", "#endif")
        .Replace("public class SetNTimesAttribute", "internal class SetNTimesAttribute")
        .Replace("public sealed class SetOnceAttribute", "internal sealed class SetOnceAttribute");

      return newsource;
    }
  }
}
