namespace translator.Packages.Service
{
    public static class PathResolver
    {
        public static string ResolvePath(string path)
        {
            if (path.StartsWith("~/"))
            {
                var homeDirectory = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
                path = Path.Combine(homeDirectory,
                    path.Substring(2).Replace("/", Path.DirectorySeparatorChar.ToString()));
            }

            return path;
        }
    }
}