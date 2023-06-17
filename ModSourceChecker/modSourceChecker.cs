using System.Text.RegularExpressions;
using dnlib.DotNet;
using Microsoft.VisualBasic.FileIO;
using Mono.Cecil;
using CustomAttribute = dnlib.DotNet.CustomAttribute;
using SearchOption = System.IO.SearchOption;

namespace modSourceChecker
{
    class modSourceChecker
    {
        private static readonly HashSet<string> DeletedClasses = new HashSet<string>();
        public static string assemblyName = "";

        static void Prompt(string message)
        {
            Console.WriteLine(message);
            Console.ReadLine();
        }

        static void Main(string[] args)
        {
            //string[] args = new string[] { "D:\\Valheim_Dev\\Valheim\\BepInEx\\plugins", "ZNet.Awake" };

            // Check args
            if (args.Length < 1)
            {
                Prompt("ERROR: Please drag the Mod onto the Exe or supply a command line path.");
                return;
            }

            // Get Mod Path from Arg
            string dllPath = args[0];
            if (!Directory.Exists(dllPath) && args.Length > 1)
            {
                Prompt("ERROR: Detect run with Filters, but Path does not point to a Directory, please fix the path and try again.");
                return;
            }

            // Get Dir of ModSourceChecker.exe
            string? exeDirectory = GetExecutingAssemblyDirectory();
            if (exeDirectory == null)
            {
                Prompt("ERROR: Unable to determine the executable directory.");
                return;
            }

            // Read CSV and Parse Changed or Deleted .cs classes to list
            string csvPath = Path.Combine(exeDirectory, "newVersion.csv");
            List<string>? changedClasses = parseCsvToList(csvPath);
            if (changedClasses == null || changedClasses.Count == 0)
            {
                Prompt($"ERROR: Unable to read or parse the CSV file at Path: {csvPath}");
                return;
            }

            // If we have extra args then we are using Filters
            HashSet<string> FilterTargets = null;
            if (args.Length > 1)
            {
                FilterTargets = new HashSet<string>(args.Skip(1));
            }

            // Get all DLL files recursively
            string[] dllFiles = Directory.GetFiles(dllPath, "*.dll", SearchOption.AllDirectories);

            // Process the DLL files
            foreach (string dllFile in dllFiles)
            {
                checkMod(dllFile, changedClasses, FilterTargets);
            }

            // Wait to exit Console
            Console.WriteLine("Press Enter to Exit");
            Console.ReadLine();
        }

        // Return Current Working Directory
        static string? GetExecutingAssemblyDirectory()
        {
            return Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
        }

        // Parse CSV File into 
        public static List<string>? parseCsvToList(string filePath)
        {
            if (!File.Exists(filePath))
            {
                Prompt("ERROR: CSV Missing, Did you Use WinMerge to make it and save it to the current Directory?");
                return null;
            }

            List<string> data = new List<string>();

            try
            {
                // Read CSV File Line by Line onto a list of strings.
                string[] lines = File.ReadAllLines(filePath);

                string csNewPath = "";
                string csOldPath = "";

                // Get Source Code Filepaths from first line of CSV
                Match match = Regex.Match(lines[0], @"Compare\s(.+)\swith\s(.+)");
                if (match.Success && match.Groups.Count >= 3)
                {
                    csOldPath = match.Groups[1].Value;
                    csNewPath = match.Groups[2].Value;
                }

                // Loop over each line
                for (int i = 3; i < lines.Length; i++)
                {
                    string line = lines[i];

                    using (TextFieldParser parser = new TextFieldParser(new StringReader(line)))
                    {
                        parser.TextFieldType = FieldType.Delimited;
                        parser.SetDelimiters(",");

                        // Parse the Class Data
                        while (!parser.EndOfData)
                        {
                            string[]? fields = parser.ReadFields();
                            if (fields != null)
                            {
                                // Extract assemblyName
                                if (fields[5] == "csproj")
                                {
                                    assemblyName = Path.ChangeExtension(fields[0], null);
                                    continue;
                                }

                                string status = fields[2];

                                // make sure the Class is Changed
                                if (status != "Folders are different" && status != "Identical" &&
                                    status != "Text files are identical" && fields[0].EndsWith(".cs") &&
                                    !status.Contains("Left only:"))
                                {
                                    // Add the class path to the list
                                    string fp = Path.Combine(csNewPath.TrimStart(), fields[1], fields[0]);
                                    data.Add(fp);
                                }

                                // Check if the class is deleted
                                if (status.Contains("Left only:"))
                                {
                                    // Class has been removed, so we get the classname from the old source
                                    string fp = Path.Combine(csOldPath.TrimStart(), fields[1], fields[0]);
                                    DeletedClasses.Add(fp);
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Prompt("ERROR: " + ex.Message);
                return null;
            }

            return data;
        }

        // Checks a provided Mod DLL for any class references that are changed or deleted
        public static void checkMod(string dllPath, List<string> csFiles, HashSet<string> FilterTargets = null)
        {
            // Read DLL with Cecil
            AssemblyDefinition assembly = AssemblyDefinition.ReadAssembly(dllPath);
            // Check for Changed Classes
            bool detectedChangedClass = false;
            if (FilterTargets == null)
            {
                foreach (string csFile in csFiles)
                {
                    string csContent = File.ReadAllText(csFile);
                    string? mainClassName = GetMainClassName(csContent);

                    if (assembly.MainModule.Types.Any(t => t.Name == mainClassName))
                    {
                        Console.WriteLine(
                            $"Detected Changed Class \"{Path.GetFileNameWithoutExtension(csFile)}\" in Mod \"{Path.GetFileNameWithoutExtension(dllPath)}\".dll from referenced {assemblyName}.dll");
                        detectedChangedClass = true;
                    }
                }

                // Check for Deleted Classes
                foreach (string csFile in DeletedClasses)
                {
                    string csContent = File.ReadAllText(csFile);
                    string? mainClassName = GetMainClassName(csContent);

                    if (assembly.MainModule.Types.Any(t => t.Name == mainClassName))
                    {
                        Console.WriteLine(
                            $"Detected Deleted Class \"{Path.GetFileNameWithoutExtension(csFile)}\" in Mod \"{Path.GetFileNameWithoutExtension(dllPath)}\".dll from referenced {assemblyName}.dll");
                        detectedChangedClass = true;
                    }
                }
                Console.WriteLine("");
            }

            // Search for Harmony Patches that Target Changed Classes
            bool detectedHarmonyPatch = false;
            ModuleDefMD module = ModuleDefMD.Load(dllPath);
            foreach (string csFile in csFiles)
            {
                string csContent = File.ReadAllText(csFile);
                string? mainClassName = GetMainClassName(csContent);

                foreach (TypeDef type in module.GetTypes())
                {
                    if (type.CustomAttributes == null)
                        continue;

                    foreach (CustomAttribute ca in type.CustomAttributes)
                    {
                        if (ca.TypeFullName == "HarmonyLib.HarmonyPatch")
                        {
                            if (ca.HasConstructorArguments && ca.ConstructorArguments.Count == 2)
                            {
                                string? className = ca.ConstructorArguments[0].Value?.ToString();
                                string? methodName = ca.ConstructorArguments[1].Value?.ToString();

                                if (className == mainClassName)
                                {
                                    if (FilterTargets != null)
                                    {
                                        foreach (var filter in FilterTargets)
                                        {
                                            // Split the string by '.'
                                            string[] parts = filter.Split('.');

                                            // Check the number of parts
                                            if (parts.Length != 2)
                                            {
                                                Prompt($"ERROR: The Filter String: {filter} is not formatted correctly");
                                                return;
                                            }

                                            if (className == parts[0] && methodName == parts[1])
                                                Console.WriteLine($"Detected Harmony Patch of Changed Class: \"{className}\" Method: \"{methodName}\" in Mod \"{Path.GetFileNameWithoutExtension(dllPath)}\".dll from referenced {assemblyName}.dll with Filter: \"{filter}\"");
                                        }
                                    }
                                    else
                                    {
                                        Console.WriteLine($"Detected Harmony Patch of Changed Class: \"{className}\" Method: \"{methodName}\" in Mod \"{Path.GetFileNameWithoutExtension(dllPath)}\".dll from referenced {assemblyName}.dll");
                                        detectedHarmonyPatch = true;
                                    }
                                }
                            }
                        }
                    }
                }
            }

            if (FilterTargets == null)
            {
                // Report Clean
                if (!detectedHarmonyPatch && !detectedChangedClass)
                {
                    Console.WriteLine($"Mod \"{Path.GetFileNameWithoutExtension(dllPath)}\".dll does not have any references to changed or deleted classes in {assemblyName}.dll");
                    Console.WriteLine("It should work with the new update without issues.");
                }
                Console.WriteLine();
            }

            module.Dispose();
            assembly.Dispose();
        }

        // Get the main class name from the C# source code
        public static string? GetMainClassName(string sourceCode)
        {
            Match match = Regex.Match(sourceCode, @"class\s+([a-zA-Z_][a-zA-Z0-9_]*)");
            if (match.Success && match.Groups.Count >= 2)
            {
                return match.Groups[1].Value;
            }
            return null;
        }
    }
}

