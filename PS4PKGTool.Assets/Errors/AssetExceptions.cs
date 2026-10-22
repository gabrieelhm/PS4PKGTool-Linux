namespace PS4PKGTool.Assets.Errors;

/// <summary>Base for all structured asset errors. Handlers are untrusted parsing
/// code — they must wrap unexpected failures in one of these instead of leaking
/// raw library exceptions to the UI.</summary>
public class AssetException : Exception
{
    public AssetException(string message) : base(message) { }
    public AssetException(string message, Exception inner) : base(message, inner) { }
}

/// <summary>The bytes parsed, but contain an unsupported structure/version/format.</summary>
public sealed class UnsupportedAssetException : AssetException
{
    public UnsupportedAssetException(string message) : base(message) { }
}

/// <summary>A required dependency (e.g. Oodle DLL, external decoder) is absent.</summary>
public sealed class MissingDependencyException : AssetException
{
    public MissingDependencyException(string message) : base(message) { }
}

/// <summary>The asset is encrypted and no key is available.</summary>
public sealed class EncryptedAssetException : AssetException
{
    public EncryptedAssetException(string message) : base(message) { }
}

/// <summary>The asset is corrupt or malformed.</summary>
public class CorruptAssetException : AssetException
{
    public CorruptAssetException(string message) : base(message) { }
    public CorruptAssetException(string message, Exception inner) : base(message, inner) { }
}

/// <summary>The handler reached an internal invariant violation; treated as corrupt input.</summary>
public sealed class AssetParseException : CorruptAssetException
{
    public AssetParseException(string message) : base(message) { }
    public AssetParseException(string message, Exception inner) : base(message, inner) { }
}
