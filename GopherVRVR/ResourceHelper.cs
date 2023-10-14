using System.Diagnostics;
using System.Reflection;

namespace GopherVRVR;

public class ResourceHelper
{
    /// <summary>
    ///     Gets a Embedded Resource Stream
    /// </summary>
    /// <param name="path">Path to Resource</param>
    /// <returns>Stream with the resource</returns>
    public static MemoryStream GetResource(string path)
    {
        Assembly assembly = Assembly.GetCallingAssembly();
        string actualName = assembly.GetName().Name + "." + path.Replace("/", ".");

        MemoryStream stream = new();
        Stream? resStream = assembly.GetManifestResourceStream(actualName);

        Debug.Assert(resStream != null, "resStream");

        resStream!.CopyTo(stream);

        return stream;
    }
    /// <summary>
    ///     Gets a String Resource
    /// </summary>
    /// <param name="path">Path to Resource</param>
    /// <param name="type">A type from the assembly to grab from</param>
    /// <returns>String with the resource</returns>
    public static string GetStringResource(string path, Type type)
    {
        Assembly assembly = Assembly.GetAssembly(type);
        string actualName = assembly.GetName().Name + "." + path.Replace("/", ".");

        using Stream? resStream = assembly.GetManifestResourceStream(actualName);

        Debug.Assert(resStream != null, "resStream");

        using StreamReader reader = new(resStream!);

        return reader.ReadToEnd();
    }

    /// <summary>
    ///     Gets a Byte Array Resource
    /// </summary>
    /// <param name="path">Path to Resource</param>
    /// <param name="type">A type from the assembly to grab from</param>
    /// <returns>String with the resource</returns>
    public static byte[] GetByteResource(string path, Type type)
    {
        Assembly assembly = Assembly.GetAssembly(type);
        string actualName = assembly.GetName().Name + "." + path.Replace("/", ".");

        using Stream? resStream = assembly.GetManifestResourceStream(actualName);

        Debug.Assert(resStream != null, "resStream");

        using BinaryReader reader = new(resStream!);

        return reader.ReadBytes((int)reader.BaseStream.Length);
    }
}