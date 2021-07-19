using Bentley.DgnPlatformNET;
using Bentley.DgnPlatformNET.Elements;
using DgnOrganizer.XmlData;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.Text.RegularExpressions;

namespace DgnOrganizer
{
class Program
{
    static void Main(string[] args)
    {
        try
        {
            Logger.Log.Info("=== START WORK");
            Run(args);
        }
        catch (Exception ex)
        {
            Logger.Log.Error(ex.Message + (ex.InnerException != null ? ("\n" + ex.InnerException.Message) : string.Empty));
        }
        Logger.Log.Info("=== END WORK");
        Console.ReadKey();
    }

    static void Run(string[] args)
    {
        if (args.Length == 0)
        {
            Logger.Log.Error("В качестве 1-го аргумента должен выступать путь к обрабатываемому каталогу моделей dgn");
            return;
        }

        string workFolder = Path.GetFullPath(args[0]);
        if (!Directory.Exists(workFolder))
        {
            Logger.Log.Error($"Не найден рабочий каталог '{workFolder}'");
            return;
        }

        Logger.Log.Info($"Обработка каталога '{workFolder}'");

        string[] dgnPaths = Directory.GetFiles(workFolder, "*.dgn", SearchOption.AllDirectories);
        if (dgnPaths.Count() == 0)
        {
            Logger.Log.Warn($"Найдено dgn-моделей: {dgnPaths.Count()} шт.");
            return;
        }
        Logger.Log.Info($"Найдено dgn-моделей: {dgnPaths.Count()} шт.");        

        { // ! Важно
            var host = new Host();
            DgnPlatformLib.Initialize(host, false);
        }

        string outputFolder = Path.Combine(workFolder, "_Organized");
        if (Directory.Exists(outputFolder))
        {
            Directory.Delete(outputFolder, true);
        }

        Console.Write("Обработка... ");

        int i = 0;
        foreach (string dgnPath in dgnPaths)
        {
            try
            {
                processFile(dgnPath, outputFolder);
            }
            catch (Exception ex)
            {
                Logger.Log.Error($"Файл '{dgnPath}':\n" + ex.Message +
                (ex.InnerException != null ? ("\n" + ex.InnerException.Message) : string.Empty)
#if DEBUG
                + ex.StackTrace
#endif
                );
            }
            finally
            {
                if (i % 50 == 0)
                {
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine($"Обработанно {(int)(double)i * 100 / dgnPaths.Count()} %");
                    Console.ResetColor();
                }
                ++i;
            }
        }

        Logger.Log.Info($"Целевой каталог обработанных файлов: \"{outputFolder}\"");     
    }

    static string ensureFolderStructure(string rootFolder, string[] structure)
    {
        return Directory.CreateDirectory(Path.Combine(
            rootFolder, structure[0], structure[1], structure[2])).FullName;
    }

    static void processFile(string uri, string outputFolder)
    {
        Regex regex = new Regex("^[A-Z0-9]{3,4}_(([0-9]{2}[A-Z]{3})[0-9]{2})_([A-Z])?(_[-\\w]+)?(_[\\+-.,0-9]+)?.dgn$");
        
        string sourceFileName = Path.GetFileName(uri);
        Match match = regex.Match(sourceFileName);
        if (!match.Success)
        {
            Logger.Log.Warn($"'{sourceFileName}' - не удалось распарсить имя файла");
            return;
        }

        string unitCode = match.Groups[2].Value;
        string spec = match.Groups[3].Value;
        string itemCode = match.Groups[1].Value;

        if (!SpecDictionary.ContainsKey(spec))
        {
            Logger.Log.Warn($"'{sourceFileName}:' - '{spec}' - не удалось распарсить литеру специальности");
            return;
        }
        spec = SpecDictionary[spec];

        DgnFile sourceFile;
        DgnModel sourceModel;
        LoadOrCreate_FileAndDefaultModel(out sourceFile, out sourceModel, uri, null);
        
        var simFile = new SimFile(Path.ChangeExtension(uri, ".xml"));

        string[] structure = new string[] { unitCode, spec, itemCode };
        string destFolder;

        if (simFile.CatalogToElement.Keys.Count() > 0)
        {
            destFolder = ensureFolderStructure(outputFolder, structure);
        }
        else
        {
            Logger.Log.Warn($"'{sourceFileName}:' не найдены элементы для обработки");
            return;
        }

        foreach(string catalogType in simFile.CatalogToElement.Keys)
        {
            DgnFile destFile; 
            DgnModel destModel;
            string destUri = Path.Combine(destFolder, catalogType + ".dgn");

            LoadOrCreate_FileAndDefaultModel(out destFile, out destModel, destUri, sourceFile);

            using (ElementCopyContext copyContext = new ElementCopyContext(destModel))
            {
                copyContext.CopyingReferences = true;
                copyContext.WriteElements = true;

                foreach (SimElement simEl in simFile.CatalogToElement[catalogType])
                {
                    Element sourceElement = sourceModel.FindElementById(simEl.ElementId);
                    if (sourceElement == null)
                    {
                        Logger.Log.Error($"Не найден элемент '{simEl.ElementId}' в файле '{uri}'");
                        continue;
                    }

                    copyContext.DoCopy(sourceElement);
                }
                destFile.ProcessChanges(DgnSaveReason.None);
            }
        }       
       
    }

    private static bool LoadOrCreate_FileAndDefaultModel(
        out DgnFile file, out DgnModel defaultModel, string uri, DgnFile seedFile)
    {
        StatusInt statusInt;
        DgnFileStatus status;
        DgnDocument dgnDoc;
        DgnFileOwner dgnOwner;

        if (File.Exists(uri))
        {
            dgnDoc = DgnDocument.CreateForLocalFile(uri);
            dgnOwner = DgnFile.Create(dgnDoc, DgnFileOpenMode.ReadWrite);
            file = dgnOwner.DgnFile;
            
        }
        else
        {
            string fileName = Path.GetFileName(uri);
            dgnDoc = DgnDocument.CreateForNewFile(out status, 
                fileName, uri, 0, fileName, DgnDocument.OverwriteMode.Prompt, 
                DgnDocument.CreateOptions.Default);
            SeedData seedData = new SeedData(seedFile, 0, SeedCopyFlags.AllData, true);
            dgnOwner = DgnFile.CreateNew(out status, dgnDoc, 
                DgnFileOpenMode.ReadWrite, seedData, DgnFileFormatType.V8, true);
            file = dgnOwner.DgnFile;
        }

        status = file.LoadDgnFile(out statusInt);

        defaultModel = file.LoadRootModelById(out statusInt,
            file.GetModelIndexCollection().First().Id);

        return status == DgnFileStatus.Success;

    }

    private static Dictionary<string, string> SpecDictionary =>
        new Dictionary<string, string>() {
            {"A", "Architecture"},
            {"S", "Struct"},
        };

}
}
