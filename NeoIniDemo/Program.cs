using NeoIni;

class NeoIniDemo
{
    private const string TestFile = "demo_config.ini";
    private static bool Encryption = false;

    private static async Task Main(string[] args)
    {
        Console.CursorVisible = false;
        Console.Clear();
        Console.WriteLine("NeoIni Demonstration\n");
        Console.Write("Press Y to enable encryption mode: ");
        Encryption = Console.ReadKey().Key == ConsoleKey.Y;
        Console.WriteLine();

        BasicCreationDemo();
        SectionsDemo();
        KeysValuesDemo();
        await AsyncOperationsDemo();
        AutoFeaturesDemo();
        FileErrorRecoveryDemo();
        Console.Write("Press Y to cleanup file: ");
        if (Console.ReadKey().Key == ConsoleKey.Y)
        {
            Console.WriteLine();
            await CleanupDemo();
        }

        Console.WriteLine("\n\nDemonstration completed!");
        Console.WriteLine("Press any key to exit...");
        Console.ReadKey();
    }

    private static void BasicCreationDemo()
    {
        Console.WriteLine("\n1. FILE CREATION WITH DEFAULTS");

        using var ini = new NeoIniReader(TestFile, Encryption);
        Console.WriteLine($"File created: {TestFile}");
        Console.WriteLine("All features use default settings:");
        Console.WriteLine($"- AutoAdd: {ini.AutoAdd}");
        Console.WriteLine($"- AutoSave: {ini.AutoSave}");
        Console.WriteLine($"- AutoBackup: {ini.AutoBackup}");
        Console.WriteLine($"- UseChecksum: {ini.UseChecksum}");
        Console.WriteLine($"- AutoSaveInterval: {ini.AutoSaveInterval}");
        Console.WriteLine($"- UseAutoSaveInterval: {ini.UseAutoSaveInterval}");

        Console.WriteLine("Press any key to continue...");
        Console.ReadKey(true);
    }

    private static void SectionsDemo()
    {
        Console.WriteLine("\n2. WORKING WITH SECTIONS");
        using var ini = new NeoIniReader(TestFile, Encryption);

        Console.WriteLine($"Section 'User' exists: {ini.SectionExists("User")}");

        ini.AddSection("User");
        ini.AddSection("Database");
        Console.WriteLine("Added sections: User, Database");

        var sections = ini.GetAllSections();
        Console.WriteLine($"All sections ({sections.Length}): {string.Join(", ", sections)}");

        ini.RemoveSection("Database");
        Console.WriteLine("Section 'Database' removed");

        sections = ini.GetAllSections();
        Console.WriteLine($"Remaining sections ({sections.Length}): {string.Join(", ", sections)}");

        Console.WriteLine("Press any key to continue...");
        Console.ReadKey(true);
    }

    private static void KeysValuesDemo()
    {
        Console.WriteLine("\n3. KEYS AND VALUES");
        using var ini = new NeoIniReader(TestFile, Encryption);

        ini.AddKeyInSection("User", "Name", "John Doe");
        ini.AddKeyInSection("User", "Age", 30);
        ini.AddKeyInSection("User", "IsAdmin", true);
        ini.AddKeyInSection("User", "Salary", 75000.50);
        Console.WriteLine("Added keys to User section");

        string name = ini.GetValue("User", "Name", "Unknown");
        int age = ini.GetValue("User", "Age", 0);
        bool isAdmin = ini.GetValue("User", "IsAdmin", false);
        double salary = ini.GetValue("User", "Salary", 0.0);

        Console.WriteLine($"Name: {name}");
        Console.WriteLine($"Age: {age}");
        Console.WriteLine($"Admin: {isAdmin}");
        Console.WriteLine($"Salary: {salary:C}");

        ini.SetKey("User", "Age", 31);
        Console.WriteLine("Age updated to 31");

        Console.WriteLine($"Key 'Name' exists: {ini.KeyExists("User", "Name")}");
        Console.WriteLine($"Key 'Email' exists: {ini.KeyExists("User", "Email")}");

        string email = ini.GetValue("User", "Email", "no@email.com");
        Console.WriteLine($"Email (auto-added): {email}");

        var keys = ini.GetAllKeys("User");
        Console.WriteLine($"Keys in User ({keys.Length}): {string.Join(", ", keys)}");

        ini.RemoveKey("User", "Salary");
        Console.WriteLine("Key 'Salary' removed");

        keys = ini.GetAllKeys("User");
        Console.WriteLine($"Keys in User ({keys.Length}): {string.Join(", ", keys)}");

        Console.WriteLine("Press any key to continue...");
        Console.ReadKey(true);
    }

    private static async Task AsyncOperationsDemo()
    {
        Console.WriteLine("\n4. ASYNCHRONOUS OPERATIONS");
        using var ini = new NeoIniReader(TestFile, Encryption);

        await ini.AddSectionAsync("Settings");
        await ini.SetKeyAsync("Settings", "Theme", "Dark");
        await ini.SetKeyAsync("Settings", "Volume", 80);
        Console.WriteLine("Settings section added asynchronously");

        string theme = await ini.GetValueAsync("Settings", "Theme", "Light");
        int volume = await ini.GetValueAsync("Settings", "Volume", 50);
        Console.WriteLine($"Theme: {theme}, Volume: {volume}%");

        bool exists = await ini.KeyExistsAsync("Settings", "Theme");
        Console.WriteLine($"Key 'Theme' exists (async): {exists}");

        Console.WriteLine("Press any key to continue...");
        Console.ReadKey(true);
    }

    private static void AutoFeaturesDemo()
    {
        Console.WriteLine("\n5. AUTOMATIC FEATURES");
        using var ini = new NeoIniReader(TestFile, Encryption);
        ini.UseAutoSaveInterval = true;

        ini.SetKey("Database", "Host", "localhost");
        ini.SetKey("Database", "Port", 5432);
        ini.SaveFile();
        Console.WriteLine("Manual save completed");

        ini.ReloadFromFile();
        Console.WriteLine("Data reloaded from file");
        Console.WriteLine($"Database Host: {ini.GetValue("Database", "Host", "")}");

        Console.WriteLine($"Auto-save every {ini.AutoSaveInterval} operations");

        Console.Write("Adding logs: ");
        for (int i = 1; i <= 6; i++)
        {
            ini.SetKey("Logs", $"Entry{i}", $"Log message {i}");
            if (i % 3 == 0) Console.Write("SAVED ");
            else Console.Write(".");
        }
        Console.WriteLine("Auto-save triggered");

        Console.WriteLine("Press any key to continue...");
        Console.ReadKey(true);
    }

    private static void FileErrorRecoveryDemo()
    {
        Console.WriteLine("\n6. FILE ERROR RECOVERY");

        using var ini = new NeoIniReader(TestFile, Encryption);
        Console.WriteLine("\nBEFORE DAMAGE - All sections:");
        ShowContent(ini);

        CorruptFileManually(TestFile);

        Console.WriteLine("\nDAMAGED FILE CONTENT:");
        ShowFileRawContent(TestFile);

        Console.WriteLine("\nAttempting recovery with ini.Reload...");
        ini.ReloadFromFile();

        Console.WriteLine("\nAFTER RECOVERY - All sections:");
        ShowContent(ini);

        Console.WriteLine("\nRecovery demo completed!");
        Console.WriteLine("Press any key to continue...");
        Console.ReadKey(true);
    }

    private static async Task CleanupDemo()
    {
        Console.WriteLine("\n7. COMPLETE CLEANUP");

        using var ini = new NeoIniReader(TestFile, Encryption);
        var sections = ini.GetAllSections();
        Console.WriteLine("Final content before cleanup:");
        ShowContent(ini);

        ini.RemoveKey("User", "Age");
        ini.RemoveSection("Logs");
        Console.WriteLine("Removed key 'Age' and section 'Logs'");

        await ini.DeleteFileWithDataAsync();
        Console.WriteLine("File deleted from disk + memory cleared");

        var finalIni = new NeoIniReader(TestFile, Encryption);
        Console.WriteLine($"File is empty: {finalIni.GetAllSections().Length == 0}");
    }

    private static void ShowContent(NeoIniReader ini)
    {
        var sections = ini.GetAllSections();
        foreach (var sec in sections)
        {
            var keys = ini.GetAllKeys(sec);
            Console.WriteLine($"  [{sec}] ({keys.Length} keys):");

            foreach (var key in keys)
            {
                var value = ini.GetValue(sec, key, "null");
                Console.WriteLine($"    {key} = {value}");
            }
            Console.WriteLine();
        }
    }

    private static void CorruptFileManually(string filePath)
    {
        Console.WriteLine($"\nMANUALLY edit '{filePath}' now!");
        Console.WriteLine("1. Open file in notepad/text editor");
        Console.WriteLine("2. DELETE or EDIT any lines");
        Console.WriteLine("3. SAVE the file");
        Console.WriteLine("4. Press any key to continue...");
        Console.ReadKey(true);
    }

    private static void ShowFileRawContent(string filePath)
    {
        if (File.Exists(filePath))
        {
            Console.WriteLine($"Raw file content ({new FileInfo(filePath).Length} bytes):");
            var lines = File.ReadAllLines(filePath);
            foreach (var line in lines) Console.WriteLine(line);
        }
        else Console.WriteLine("File not found!");
    }
}
