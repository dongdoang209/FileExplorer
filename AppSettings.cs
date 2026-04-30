namespace FileExplorer
{
    public class AppSettings
    {
        public string Theme { get; set; } = "dark";
        public string DefaultPath { get; set; } = "";
        public bool ShowHiddenFiles { get; set; } = false;
        public bool ShowExtensions { get; set; } = true;
        public string SortBy { get; set; } = "name_asc";
    }
}