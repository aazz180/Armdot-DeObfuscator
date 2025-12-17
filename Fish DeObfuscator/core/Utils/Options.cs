using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using dnlib.DotNet;
using dnlib.DotNet.Writer;
using Fish_DeObfuscator.core.DeObfuscation.DotNet.Armdot;

namespace Fish_DeObfuscator.core.Utils
{
    public class Options : IOptions
    {
        public DeobfuscationType Type { get; set; }
        public string FilePath { get; set; }
        public string FileDirectory { get; set; }
        public string FileOutput { get; set; }
        public string AssemblyPath { get; set; }
        public string AssemblyName { get; set; }
        public string AssemblyExtension { get; set; }
        public string AssemblyOutput { get; set; }
        public string AssemblyDirectory { get; set; }
        public List<IStage> Stages { get; private set; } = new List<IStage>();

        public Options(string[] args)
        {
            for (int i = 0; i < args.Length; i++)
            {
                switch (args[i])
                {
                    case "--type":
                        if (i + 1 < args.Length)
                        {
                            if (Enum.TryParse<DeobfuscationType>(args[i + 1], true, out var type))
                            {
                                Type = type;
                            }
                            i++;
                        }
                        break;
                    case "--file":
                        if (i + 1 < args.Length)
                        {
                            FilePath = Path.GetFullPath(args[i + 1]);
                            FileDirectory = Path.GetDirectoryName(FilePath);
                            FileOutput = Path.Combine(FileDirectory, $"{Path.GetFileNameWithoutExtension(FilePath)}_cleaned{Path.GetExtension(FilePath)}");
                            i++;
                        }
                        break;
                    case "--options":
                        if (i + 1 < args.Length)
                        {
                            if (Type == DeobfuscationType.DotNet)
                            {
                                AssemblyPath = FilePath;
                                AssemblyName = Path.GetFileNameWithoutExtension(FilePath);
                                AssemblyExtension = Path.GetExtension(FilePath);
                                AssemblyDirectory = Path.GetDirectoryName(FilePath);
                                AssemblyOutput = Path.Combine(AssemblyDirectory, $"{AssemblyName}_obf{AssemblyExtension}");
                                SetStages(args[i + 1]);
                            }
                            i++;
                        }
                        break;
                }
            }
        }

        private void SetStages(string userChoice)
        {
            if (string.IsNullOrEmpty(userChoice))
            {
                Console.WriteLine("Warning: No user choice provided, defaulting to rename");
                userChoice = "rename";
            }

            Console.WriteLine($"Processing obfuscation options: '{userChoice}'");

            switch (userChoice.Trim())
            {
                case "1":
                    userChoice = "rename";
                    break;
                case "2":
                    userChoice = "rename,string";
                    break;
                case "3":
                    userChoice = "rename,controlflow";
                    break;
                case "4":
                    userChoice = "full";
                    break;
            }

            string[] options = userChoice.Split(',');

            foreach (string option in options)
            {
                string trimmedOption = option.Trim().ToLower();
                Console.WriteLine($"Adding stage for option: '{trimmedOption}'");

                switch (trimmedOption)
                {
                    case "full_armdot":
                        Stages.Add(new Fish_DeObfuscator.core.DeObfuscation.DotNet.Armdot.String());
                        Stages.Add(new Virtualization());
                        Stages.Add(new Calli());
                        Stages.Add(new ControlFlow());
                        Stages.Add(new LocalCleaner());

                        break;
                    case "full":
                        break;
                    default:
                        Console.WriteLine($"Warning: Unknown option '{option}' - skipping");
                        break;
                }
            }

            Console.WriteLine($"Total stages configured: {Stages.Count}");
        }
    }

    public class Context : IContext
    {
        private ModuleDefMD moduleDefinition;

        public Context(IOptions options)
        {
            Options = options;
        }

        public bool IsInitialized()
        {
            if (string.IsNullOrEmpty(Options.AssemblyPath))
            {
                return false;
            }

            try
            {
                Console.WriteLine($"Attempting to load assembly: {Options.AssemblyPath}");
                moduleDefinition = ModuleDefMD.Load(Options.AssemblyPath);
                Console.WriteLine($"Successfully loaded assembly with {moduleDefinition.Types.Count} types");
                return moduleDefinition != null;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error initializing context: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
                return false;
            }
        }

        public void SaveContext(bool log = true)
        {
            if (moduleDefinition == null)
            {
                throw new InvalidOperationException("Module definition is null. Cannot save context.");
            }

            try
            {
                if (log)
                {
                    Console.WriteLine($"Saving deobfuscated assembly to: {Path.GetFileName(Options.AssemblyOutput)}");
                }

                // Fix all method bodies before saving to prevent branch target errors
                if (log)
                {
                    Console.WriteLine("Fixing method bodies...");
                }
                int fixedMethods = MethodBodyFixer.FixAllMethods(moduleDefinition);
                if (log && fixedMethods > 0)
                {
                    Console.WriteLine($"Fixed {fixedMethods} method(s) with invalid references.");
                }

                var opts = new ModuleWriterOptions(moduleDefinition);
                opts.MetadataOptions.Flags |= MetadataFlags.KeepOldMaxStack;

                // Add a logger to catch any remaining issues
                opts.Logger = DummyLogger.NoThrowInstance;

                moduleDefinition.Write(Options.AssemblyOutput, opts);

                if (log)
                {
                    Console.WriteLine("Assembly saved successfully!");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error saving assembly: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
                throw;
            }
        }

        public IOptions Options { get; }

        public ModuleDefMD ModuleDefinition
        {
            get => moduleDefinition;
            set => moduleDefinition = value;
        }
    }
}
