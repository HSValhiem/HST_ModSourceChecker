using Mono.Cecil;
using System.Reflection;
using System.Text.RegularExpressions;
using Microsoft.VisualBasic.FileIO;
using dnlib.DotNet;
using CustomAttribute = dnlib.DotNet.CustomAttribute;

namespace modSourceChecker
{
    class modSourceChecker
    {

        private static readonly List<string> DeletedClasses = new ();
        public static string assemblyName = "";

        static void Main(string[] args)
        {
            //Check args
            if (args.Length < 1)
            {
                Console.WriteLine("ERROR: Please drag the Mod onto the Exe or supply a command line path.");
                Console.ReadLine();
                return;
            }

            // Get Mod Path from Arg
            string dllPath = args[0];

            //string dllPath = "D:\\Valheim_Dev\\WIP\\AzuAntiCheat.dll";


            // Get Dir of ModSourceChecker.exe
            string? exeDirectory = GetExecutingAssemblyDirectory();
            if (exeDirectory == null)
            {
                Console.WriteLine("ERROR: Unable to determine the executable directory.");
                Console.ReadLine();
                return;
            }

            // Read CSV and Parse Changed or Deleted .cs classes to list
            string csvPath = Path.Combine(exeDirectory, "newVersion.csv");
            List<string>? changedClasses = parseCsvToList(csvPath);
            if (changedClasses == null)
            {
                Console.WriteLine($"ERROR: Unable to read CSV File at Path: {csvPath}");
                Console.ReadLine();
                return;
            }
            if (changedClasses.Count == 0)
            {
                Console.WriteLine($"ERROR: Unable to Parse CSV at Path: {csvPath}");
                Console.ReadLine();
                return;
            }

            // Checks Mod for Changed or Deleted Classes
            checkMod(dllPath, changedClasses);

            // Wait to exit Console
            Console.WriteLine("Press Enter to Exit");
            Console.ReadLine();
        }

        // Return Current Working Directory
        static string? GetExecutingAssemblyDirectory()
        {
            string? assemblyLocation = Assembly.GetEntryAssembly()?.Location;

            if (assemblyLocation != null)
                return Path.GetDirectoryName(assemblyLocation);

            return null;
        }


        // Parse CSV File into 
        public static List<string>? parseCsvToList(string filePath)
        {
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
                string errorText = "ERROR: " + ex.Message;
                if (ex.GetType() == typeof(FileNotFoundException))
                    errorText = "ERROR: CSV Missing, Did you Use WinMerge to make it and save it to the current Directory?";

                Console.WriteLine(errorText);
                Console.ReadLine();
                return null;
            }

            return data;
        }




        // Checks a provided Mod DLL for any class references that are changed or deleted
        public static void checkMod(string dllPath, List<string> csFiles)
        {
            // Read DLL with Cecil
            AssemblyDefinition assembly = AssemblyDefinition.ReadAssembly(dllPath);

            // Check for Changed Classes
            bool detected = false;
            foreach (string csFile in csFiles)
            {
                string csContent = File.ReadAllText(csFile);
                string? mainClassName = GetMainClassName(csContent);

                if (assembly.MainModule.Types.Any(t => t.Name == mainClassName))
                {
                    Console.WriteLine($"Detected Changed Class \"{Path.GetFileNameWithoutExtension(csFile)}\" in Mod \"{Path.GetFileNameWithoutExtension(dllPath)}\".dll from referenced {assemblyName}.dll");
                    detected = true;
                }
            }

            // Check for Deleted Classes
            foreach (string csFile in DeletedClasses)
            {
                string csContent = File.ReadAllText(csFile);
                string? mainClassName = GetMainClassName(csContent);

                if (assembly.MainModule.Types.Any(t => t.Name == mainClassName))
                {
                    Console.WriteLine($"Detected Deleted Class \"{Path.GetFileNameWithoutExtension(csFile)}\" in Mod \"{Path.GetFileNameWithoutExtension(dllPath)}\".dll from referenced {assemblyName}.dll");
                    detected = true;
                }
            }

            Console.WriteLine("");
            // Search for Harmony Patches that Target Changed Classes
            bool detectedHarmony = false;
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
                                    Console.WriteLine($"Detected Harmony Patch of Changed Class: \"{className}\" Method: \"{methodName}\" in Mod \"{Path.GetFileNameWithoutExtension(dllPath)}\".dll from referenced {assemblyName}.dll");
                                    detectedHarmony = true;
                                }
                            }
                        }
                    }
                }
            }


            // Report Clean
            if (!detectedHarmony && !detectedHarmony)
            {
                Console.WriteLine($"Mod \"{Path.GetFileNameWithoutExtension(dllPath)}\".dll does not have any References to Changed or Deleted Classes in {assemblyName}.dll");
                Console.WriteLine($"It Should work With the New Update with no Issues");
            }
            else if (!detected)
                Console.WriteLine($"Mod \"{Path.GetFileNameWithoutExtension(dllPath)}\".dll does not Directly-Reference(Non-Harmony) any Changed or Deleted Classes in {assemblyName}.dll");
            else if (!detectedHarmony)
                Console.WriteLine($"Mod \"{Path.GetFileNameWithoutExtension(dllPath)}\".dll does not have any Harmony Patches that reference any Changed or Deleted Classes in {assemblyName}.dll");
        }

        // Use Regex to extract the class name from the .cs file
        public static string? GetMainClassName(string fileContent)
        {

            try
            {
                Regex regex = new Regex(@"class\s+(\w+)", RegexOptions.Multiline);
                Match match = regex.Match(fileContent);

                if (match.Success)
                {
                    return match.Groups[1].Value;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("ERROR: " + ex.Message);
                Console.ReadLine();
            }

            return null;
        }
    }
}
