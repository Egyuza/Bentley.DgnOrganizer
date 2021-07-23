using ConsoleToolkit.CommandLineInterpretation.ConfigurationAttributes;
using ConsoleToolkit.ConsoleIO;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DgnOrganizer
{
    [Command]
    class Options
    {
        [Positional]
        [Description(": Путь к обрабатываемому каталогу моделей dgn")]
        public string Path { get; set; }

        [Option("o")]
        [Description(": Каталог, где будет опубликован результат")]
        public string OutputDir { get; set; }

        [Option("h", ShortCircuit = true)]
        [Description(": Показать справку")]
        public bool Help { get; set; }

        [CommandHandler]
        public void Handler(IConsoleAdapter console, IErrorAdapter error)
        {
            OutputDir = string.IsNullOrWhiteSpace(OutputDir) ?
                System.IO.Path.Combine(Path, "_Organized") : OutputDir;

            console.WrapLine("Обрабатываемый каталог: \"{0}\".", Path);
            console.WrapLine("Результирующий каталог: \"{0}\".", OutputDir);

            Organizer.Run(this);
        }
    }
}
