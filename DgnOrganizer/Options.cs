using ConsoleToolkit.CommandLineInterpretation.ConfigurationAttributes;
using ConsoleToolkit.ConsoleIO;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
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

        [Option("configPath", "cp")]
        [Description(": Путь к конфигурационному файлу")]
        public string ConfigPath { get; set; }

        [Option("out", "o")]
        [Description(": Каталог сохранения результирующих скомпонованных dgn-моделей")]
        public string OutputDir { get; set; }

        [Option("log", "l")]
        [Description(": Каталог сохранения лог-файлов")]
        public string LogDir { get; set; }

        [Option("help", "h", ShortCircuit = true)]
        [Description(": Показать справку")]
        public bool Help { get; set; }

        [Option("fillConfig", "fc")]
        [Description(": Выгрузить список элементов в конфиг-файл")]
        public bool FillConf { get; set; }

        [Option("close", "c")]
        [Description(": Закрывать при завершении")]
        public bool CloseOnStop { get; set; }

        [CommandHandler]
        public void Handler(IConsoleAdapter console, IErrorAdapter error)
        {
            OutputDir = string.IsNullOrWhiteSpace(OutputDir) ?
                System.IO.Path.Combine(Path, "_Organized") : OutputDir;

            ConfigPath = string.IsNullOrWhiteSpace(ConfigPath) 
                ? Config.DefaultPath
                : ConfigPath;

            LogDir = string.IsNullOrWhiteSpace(LogDir) 
                ? System.IO.Path.Combine(System.IO.Path.GetDirectoryName(
                    Assembly.GetExecutingAssembly().Location), "_log") 
                : LogDir;

            console.WrapLine(Assembly.GetExecutingAssembly().FullName);
            console.WrapLine($"Обрабатываемый каталог: \"{Path}\"");
            console.WrapLine($"Результирующий каталог: \"{OutputDir}\"");
            console.WrapLine($"Кофиг-файл: \"{ConfigPath}\"");
            console.WrapLine($"Каталог лог-файла: \"{LogDir}\"");
            console.WrapLine($"Сценарий: \"{(FillConf ? "Заполнение конфиг-файла" : "Обработка dgn-файлов")}\"");
            console.WrapLine("");

            Organizer.Run(this);
            return;
        }
    }
}
