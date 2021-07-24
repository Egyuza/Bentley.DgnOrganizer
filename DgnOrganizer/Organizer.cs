using Bentley.DgnPlatformNET;
using Bentley.DgnPlatformNET.Elements;
using DgnOrganizer.XmlData;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

using System.Text.RegularExpressions;

namespace DgnOrganizer
{
class Organizer
{
    public static void Run(Options options)
    {
        try
        {
            Logger.setLogFolder(options.LogDir);
            Logger.Log.StartErrorsCounting();
            Logger.Log.Info("=== START WORK");
            run_(options);
        }
        catch (Exception ex) 
        {
            ex.LogError();
        }
        Logger.Log.Info("=== END WORK");
        Logger.Log.Info($"Ошибок: {Logger.Log.GetErrorsCount()}");
        Console.ReadKey();
    }

    private static void run_(Options options)
    {
        if (string.IsNullOrWhiteSpace(options.Path))
        {
            Logger.Log.ErrorEx("В качестве 1-го аргумента должен выступать путь к обрабатываемому каталогу моделей dgn");
            return;
        }

        string workFolder = Path.GetFullPath(options.Path);
        if (!Directory.Exists(workFolder))
        {
            Logger.Log.ErrorEx($"Не найден рабочий каталог '{workFolder}'");
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

        string outputDir = options.OutputDir;
        if (Directory.Exists(outputDir))
        {
            Directory.Delete(outputDir, true);
        }

        System.Console.Write("Обработка... ");

        int i = 0;
        foreach (string dgnPath in dgnPaths)
        {
            try
            {
                processFile(dgnPath, outputDir);
            }
            catch (Exception ex)
            {
                ex.LogError($"Файл '{dgnPath}':");
            }
            finally
            {
                if (i % 50 == 0)
                {
                    System.Console.ForegroundColor = ConsoleColor.Green;
                    System.Console.WriteLine($"Обработанно {(int)(double)i * 100 / dgnPaths.Count()} %");
                    System.Console.ResetColor();
                }
                ++i;
            }
        }

        Logger.Log.Info($"Результирующий каталог обработанных файлов: \"{outputDir}\"");
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

        bool IsUnrecognizedSpec = false;

        if (Config.Instance.TagToSpecialization.ContainsKey(spec))
        {
            spec = Config.Instance.TagToSpecialization[spec];
        }
        else
        {
            spec = "Unrecognized";
            IsUnrecognizedSpec = true;
            Logger.Log.Warn($"'{sourceFileName}:' - '{spec}' - не распозана литера специальности, но файл будет обработан в соответствии с конфигом");
        }

        SimFile simFile;
        string xmlDataPath = Path.ChangeExtension(uri, ".xml");
        try
        {
            simFile = new SimFile(xmlDataPath);
        }
        catch (Exception ex)
        {
            ex.LogError($"Ошибка в чтении файла '{xmlDataPath}': ");
            return;
        }

        if (simFile.CatalogToElement.Keys.Count() == 0)
        {
            Logger.Log.Warn($"'{sourceFileName}:' не найдены элементы для обработки");
            return;
        }

        // Открываем Исходную модель:

        DgnFile sourceFile;
        DgnModel sourceModel;
        DgnDocument sourceDgnDoc;
        DgnFileOwner sourceDgnFileOwner;
        LoadOrCreate_FileAndDefaultModel(out sourceFile, out sourceModel,
            out sourceDgnDoc, out sourceDgnFileOwner, uri, null);

        foreach (var pair in simFile.CatalogToElement)
        {            
            string catalogType = pair.Key;
            IList<SimElement> simElements = pair.Value;

            CatalogTypeData typeData = null;
            if (Config.Instance.CatalogTypesData.ContainsKey(catalogType))
            {
                typeData = Config.Instance.CatalogTypesData[catalogType];
                catalogType = string.IsNullOrWhiteSpace(typeData.OverrideName) ?
                    catalogType : typeData.OverrideName;

            }

            string destFolder;
            // итоговая структура каталогов:
            string[] structure = new string[] { unitCode, spec, itemCode };

            if (IsUnrecognizedSpec)
            {
                spec = (typeData != null) ? 
                    typeData.Specialization : "Unrecognized";
                structure = new string[] { unitCode, spec, itemCode };
            }
            else if (typeData != null && !typeData.Specialization.Equals(spec))
            {
                structure = new string[] { 
                    unitCode, typeData.Specialization, itemCode };
            }

            destFolder = ensureFolderStructure(outputFolder, structure);
            string destUri = Path.Combine(destFolder, catalogType + ".dgn");

            // Открываем Целевую модель:

            DgnFile destFile; 
            DgnModel destModel;
            DgnDocument destDgnDoc;
            DgnFileOwner destDgnFileOwner;
            LoadOrCreate_FileAndDefaultModel(
                out destFile, out destModel, out destDgnDoc, out destDgnFileOwner,
                destUri, sourceFile);

            using (ElementCopyContext copyContext = new ElementCopyContext(destModel))
            {
                copyContext.CopyingReferences = true;
                copyContext.WriteElements = true;

                foreach (SimElement simEl in simElements)
                {
                    Element sourceElement = sourceModel.FindElementById(simEl.ElementId);
                    if (sourceElement == null)
                    {
                        Logger.Log.ErrorEx(
                            $"Не найден элемент '{simEl.ElementId}' в файле '{uri}'");
                        continue;
                    }
                    copyContext.DoCopy(sourceElement);
                }
                destFile.ProcessChanges(DgnSaveReason.None);                
            }
            destDgnFileOwner.Dispose();
            destDgnDoc.Dispose();
        }
    
        sourceDgnDoc.Dispose();
        sourceDgnFileOwner.Dispose();
    }

    private static bool LoadOrCreate_FileAndDefaultModel(
        out DgnFile file, out DgnModel defaultModel, 
        out DgnDocument dgnDoc, out DgnFileOwner dgnFileOwner,
        string uri, DgnFile seedFile)
    {
        StatusInt statusInt;
        DgnFileStatus status;

        if (File.Exists(uri))
        {
            dgnDoc = DgnDocument.CreateForLocalFile(uri);
            dgnFileOwner = DgnFile.Create(dgnDoc, DgnFileOpenMode.ReadWrite);
            file = dgnFileOwner.DgnFile;
            
        }
        else
        {
            string fileName = Path.GetFileName(uri);
            dgnDoc = DgnDocument.CreateForNewFile(out status, 
                fileName, uri, 0, fileName, DgnDocument.OverwriteMode.Prompt, 
                DgnDocument.CreateOptions.Default);
            SeedData seedData = new SeedData(seedFile, 0, SeedCopyFlags.AllData, true);
            dgnFileOwner = DgnFile.CreateNew(out status, dgnDoc, 
                DgnFileOpenMode.ReadWrite, seedData, DgnFileFormatType.V8, true);
            file = dgnFileOwner.DgnFile;
        }

        status = file.LoadDgnFile(out statusInt);

        defaultModel = file.LoadRootModelById(out statusInt,
            file.GetModelIndexCollection().First().Id);

        return status == DgnFileStatus.Success;

    }
}
}
