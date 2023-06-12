using Microsoft.VisualBasic.FileIO;
using Mono.Cecil;
using System.Reflection;
using System.Text.RegularExpressions;

namespace ConsoleApp
{
    class modSourceChecker
    {
        public static List<string> csFiles = new List<string>();
        public static List<string> classNames = new List<string>();
        public static List<string> deletedClasses = new List<string>();

        static void Main(string[] args)
        {
            if (args.Length < 1)
            {
                Console.WriteLine("Please drag the Mod onto the Exe or use a command line path.");
                return;
            }

            string dllPath = args[0];
            string exeDirectory = Path.GetDirectoryName(Assembly.GetEntryAssembly().Location);
            csFiles = ReadCsvFile(Path.Combine(exeDirectory, "newVersion.csv"));
            classNames = BuildClassList(csFiles);

            CheckIfMainClassUsedInDLL(dllPath, csFiles);
            Console.ReadLine();
        }

        public static List<string> ReadCsvFile(string filePath)
        {
            List<string> data = new List<string>();

            try
            {
                string[] lines = File.ReadAllLines(filePath);

                string csNewPath = "";
                string csOldPath = "";
                string pattern = @"Compare\s(.+)\swith\s(.+)";
                Match match = Regex.Match(lines[0], pattern);

                if (match.Success && match.Groups.Count >= 3)
                {
                    csOldPath = match.Groups[1].Value;
                    csNewPath = match.Groups[2].Value;
                }

                for (int i = 3; i < lines.Length; i++)
                {
                    string line = lines[i];

                    using (TextFieldParser parser = new TextFieldParser(new StringReader(line)))
                    {
                        parser.TextFieldType = FieldType.Delimited;
                        parser.SetDelimiters(",");

                        while (!parser.EndOfData)
                        {
                            string[] fields = parser.ReadFields();
                            string status = fields[2];

                            if (status != "Folders are different" && status != "Identical" && status != "Text files are identical" && fields[0].EndsWith(".cs") && !status.Contains("Left only:"))
                            {
                                string fp = Path.Combine(csNewPath.TrimStart(), fields[1], fields[0]);
                                data.Add(fp);
                            }

                            if (status.Contains("Left only:"))
                            {
                                // Class has been removed, so we get the classname from the old file
                                string fp = Path.Combine(csOldPath.TrimStart(), fields[1], fields[0]);
                                deletedClasses.Add(fp);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("An error occurred: " + ex.Message);
            }

            return data;
        }

        public static List<string> BuildClassList(List<string> csFiles)
        {
            List<string> classFiles = new List<string>();

            foreach (string csFile in csFiles)
            {
                if (deletedClasses.Contains(csFile))
                {
                    continue;
                }

                string csContent = File.ReadAllText(csFile);
                string mainClassName = GetMainClassName(csContent);
                classFiles.Add(mainClassName);
            }

            return classFiles;
        }

        public static void CheckIfMainClassUsedInDLL(string dllPath, List<string> csFiles)
        {
            AssemblyDefinition assembly = AssemblyDefinition.ReadAssembly(dllPath);
            bool detected = false;
            foreach (string csFile in csFiles)
            {
                string csContent = File.ReadAllText(csFile);
                string mainClassName = GetMainClassName(csContent);

                if (assembly.MainModule.Types.Any(t => t.Name == mainClassName))
                {
                    Console.WriteLine($"Detected Changed Class {Path.GetFileNameWithoutExtension(csFile)} in Mod {Path.GetFileNameWithoutExtension(dllPath)}");
                    detected = true;
                }
            }

            foreach (string csFile in deletedClasses)
            {
                string csContent = File.ReadAllText(csFile);
                string mainClassName = GetMainClassName(csContent);

                if (assembly.MainModule.Types.Any(t => t.Name == mainClassName))
                {
                    Console.WriteLine($"Detected Deleted Class {Path.GetFileNameWithoutExtension(csFile)} in Mod {Path.GetFileNameWithoutExtension(dllPath)}");
                    detected = true;
                }
            }

            if (!detected)
            {
                Console.WriteLine($"Mod does not use any Changed Classes");
            }
        }

        public static string GetMainClassName(string fileContent)
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
                Console.WriteLine("An error occurred: " + ex.Message);
            }

            return null;
        }
    }
}
