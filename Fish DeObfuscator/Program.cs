using System;
using System.IO;
using Fish_DeObfuscator.core.Utils;

namespace Fish_DeObfuscator
{
    internal class Program
    {
        public static readonly string[] UnitySpecialMethods = new string[]
        {
            "Start",
            "Update",
            "LateUpdate",
            "FixedUpdate",
            "Awake",
            "OnEnable",
            "OnDisable",
            "OnDestroy",
            "OnGUI",
            "OnCollisionEnter",
            "OnCollisionExit",
            "OnTriggerEnter",
            "OnTriggerExit",
            "OnMouseDown",
            "OnMouseUp",
            "OnMouseEnter",
            "OnMouseExit",
            "OnMouseOver",
            "OnMouseDrag",
            "OnBecameVisible",
            "OnBecameInvisible",
            "OnPreRender",
            "OnPostRender",
            "OnRenderObject",
            "OnWillRenderObject",
            "OnDrawGizmos",
            "OnDrawGizmosSelected",
            "OnApplicationFocus",
            "OnApplicationPause",
            "OnApplicationQuit",
        };

        static int Main(string[] args)
        {
            Console.Title = "Fish DeObfuscator";

            if (args.Length == 0)
            {
                // PrintUsage();
                Console.WriteLine("open Fish DeObfuscator.UI");
                Console.ReadKey();
                return 1;
            }

            var options = new Options(args);

            if (string.IsNullOrEmpty(options.FilePath))
            {
                Console.WriteLine("Error: File path is required.");
                PrintUsage();
                return 1;
            }

            return ProcessDeobfuscation(options) ? 0 : 1;
        }

        private static void PrintUsage()
        {
            Console.WriteLine("FISH DEOBFUSCATOR");
            Console.WriteLine("==========================================");
            Console.WriteLine();
            Console.WriteLine("Usage: Fish_DeObfuscator.exe --type <type> --file <file_path> [--options <options>]");
            Console.WriteLine();
            Console.WriteLine("Arguments:");
            Console.WriteLine("  --type      - The type of file to deobfuscate (e.g., DotNet).");
            Console.WriteLine("  --file      - The path to the file to deobfuscate.");
            Console.WriteLine("  --options   - (Optional) Comma-separated list of deobfuscation stages for .NET files.");
            Console.WriteLine();
            Console.WriteLine(".NET Options:");
            Console.WriteLine("  full_armdot  - Deobfuscate ArmDot obfuscated assemblies.");
            Console.WriteLine("  full         - Deobfuscate assemblies with renamed types, methods, fields, etc.");
            Console.WriteLine();
            Console.WriteLine("Examples:");
            Console.WriteLine("  Fish_DeObfuscator.exe --type DotNet --file \"MyApp.dll\" --options \"full_armdot\"");
        }

        private static bool ProcessDeobfuscation(Options options)
        {
            if (!File.Exists(options.FilePath))
            {
                Console.WriteLine($"Error: File '{options.FilePath}' not found.");
                return false;
            }

            try
            {
                switch (options.Type)
                {
                    case DeobfuscationType.DotNet:
                        return ProcessDotNetDeobfuscation(options);
                    default:
                        Console.WriteLine($"Error: Unsupported deobfuscation type '{options.Type}'. Only DotNet is supported.");
                        return false;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
                if (ex.InnerException != null)
                {
                    Console.WriteLine($"Inner exception: {ex.InnerException.Message}");
                    Console.WriteLine($"Inner stack trace: {ex.InnerException.StackTrace}");
                }
                return false;
            }
        }

        private static bool ProcessDotNetDeobfuscation(Options options)
        {
            Console.WriteLine($"Loading assembly: {Path.GetFileName(options.AssemblyPath)}");

            IContext context = new Context(options);

            if (!context.IsInitialized())
            {
                Console.WriteLine("Error: Failed to initialize context or load assembly.");
                return false;
            }

            bool hasValidOption = context.Options.Stages.Count > 0;

            if (!hasValidOption)
            {
                Console.WriteLine("Error: No valid deobfuscation options provided.");
                context.ModuleDefinition?.Dispose();
                return false;
            }

            Console.WriteLine($"Running {context.Options.Stages.Count} deobfuscation stage(s)...");

            foreach (IStage stage in context.Options.Stages)
            {
                Console.WriteLine($"Executing stage: {stage.GetType().Name}");
                try
                {
                    stage.obf(context);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error during stage {stage.GetType().Name}: {ex.Message}");
                    Console.WriteLine($"Stack trace: {ex.StackTrace}");
                }
            }

            try
            {
                context.SaveContext();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error saving context: {ex.Message}");
                context.ModuleDefinition?.Dispose();
                return false;
            }

            Console.WriteLine("Deobfuscation completed successfully!");
            Console.WriteLine($"Output saved to: {context.Options.AssemblyOutput}");

            context.ModuleDefinition?.Dispose();
            return true;
        }
    }
}
