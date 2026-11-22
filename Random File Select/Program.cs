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
            AppSettings settings = new SettingsStore(path).Load();

            if (!hasSavedSettings)
                Application.SetColorMode(SystemColorMode.System);
            else if (settings.Behavior.DarkMode)
                Application.SetColorMode(SystemColorMode.Dark);
            else
                Application.SetColorMode(SystemColorMode.Classic);

            Application.Run(new Form1(settings));
        }
    }
}