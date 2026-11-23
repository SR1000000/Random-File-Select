namespace Random_File_Select
{
    internal static class Program
    {
        /// <summary>
        ///  The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            // To customize application configuration such as set high DPI settings or default font,
            // see https://aka.ms/applicationconfiguration.
            ApplicationConfiguration.Initialize();

            string path = SettingsStore.DefaultFilePath;
            bool hasSavedSettings = File.Exists(path);
            var store = new SettingsStore(path);
            AppSettings settings = store.Load();

            Application.SetColorMode(!hasSavedSettings ? SystemColorMode.System
                : settings.Behavior.DarkMode ? SystemColorMode.Dark
                : SystemColorMode.Classic);

            Application.Run(new Form1(settings, store));
        }
    }
}