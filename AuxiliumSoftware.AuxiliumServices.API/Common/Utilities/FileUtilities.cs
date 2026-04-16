namespace AuxiliumSoftware.AuxiliumServices.API.Common.Utilities
{
    public class FileUtilities
    {
        public static bool IsScriptCapable(string ct) =>
            ct.StartsWith("image/svg", StringComparison.OrdinalIgnoreCase)
            || ct.StartsWith("text/html", StringComparison.OrdinalIgnoreCase)
            || ct.StartsWith("application/xhtml", StringComparison.OrdinalIgnoreCase)
            || ct.StartsWith("text/xml", StringComparison.OrdinalIgnoreCase)
            || ct.StartsWith("application/xml", StringComparison.OrdinalIgnoreCase);
    }
}
