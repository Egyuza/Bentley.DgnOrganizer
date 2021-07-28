using Bentley.DgnPlatformNET;
using Bentley.DgnPlatformNET.Elements;
using DgnOrganizer.XmlData;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

using System.Text.RegularExpressions;
using System.Windows.Forms;

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

            if (options.FillConf)
            {
                Logger.Log.Info("=== START WORK: заполнение конфиг-файла");
                fillConfig_(options);
            }
            else
            {
                Logger.Log.Info("=== START WORK: Компоновка dgn-файлов");
                run_(options);
            }

        }
        catch (Exception ex) 
        {
            ex.LogError();
        }
        finally
        {
            Logger.Log.Info("=== END WORK");
            Logger.Log.Info($"Ошибок: {Logger.Log.GetErrorsCount()}");
        }

        if (!options.CloseOnStop)
        {
            Console.ReadKey();
        }
        else
        {
            Environment.Exit(0);
            //Application.Exit();
        }
    }

    private static void fillConfig_(Options options)
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

        string[] dgnPaths = Directory.GetFiles(workFolder, "*.dgn", SearchOption.AllDirectories);

        bool dirty = false;

        int i = 0;
        foreach (string dgnPath in dgnPaths)
        {
            try
            {
                processFileToFillConfig(dgnPath, ref dirty);
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
                    System.Console.WriteLine($"Обработано {(int)(double)i * 100 / dgnPaths.Count()} %");
                    System.Console.ResetColor();
                }
                ++i;
            }
        }

        if (dirty)
        {
            Config.Instance.SaveChanges();
        }

    }

    private static void processFileToFillConfig(string dgnUri, ref bool dirty)
    {
        string unitCode;
        string specTag;
        string itemCode;
        SimFile simFile;

        if (!getData(dgnUri, out unitCode, out specTag, out itemCode, out simFile))
        {
            return;
        }

        Dictionary<string, CatalogTypeData> 
            configCatalogTypes = Config.Instance.CatalogTypesData;
        Dictionary<string, string> tags2spec = Config.Instance.TagToSpecialization;

        string specFullName = tags2spec.ContainsKey(specTag) 
            ? tags2spec[specTag] : null;

        foreach (string catalogType in simFile.CatalogToElement.Keys)
        {
            if (!configCatalogTypes.ContainsKey(catalogType))
            {
                configCatalogTypes.Add(catalogType, 
                    new CatalogTypeData() {Specialization = specFullName});
                dirty = true;
            }
        }        
    }

    private static void run_(Options options)
    {
        if (string.IsNullOrWhiteSpace(options.Path))
        {
            Logger.Log.ErrorEx("В качестве 1-го аргумента должен выступать путь к обрабатываемому каталогу моделей dgn");
            return;
        }

        string outputDir = options.OutputDir;

        if (!Directory.Exists(outputDir))
        {
            Directory.CreateDirectory(outputDir);
        }

        if (!HasWriteAccessToFolder(outputDir))
        {
            Logger.Log.ErrorEx($"Нет прав на запись в выходной каталог '{outputDir}'");
            return;
        }

        clearFolder(outputDir);        

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

        System.Console.Write("Обработка... ");

        int i = 0;
        foreach (string dgnPath in dgnPaths)
        {
            if (dgnPath.EndsWith("FH1_10UBA00_M_ET-Route_+0.100.dgn"))
            {
                ;
            }

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
                    System.Console.WriteLine($"Обработано {(int)(double)i * 100 / dgnPaths.Count()} %");
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

    static bool getData(string dgnUri, out string unitCode, out string specTag, 
        out string itemCode, out SimFile simFile)
    {
        unitCode = null;
        specTag = null;
        itemCode = null;
        simFile = null;

        Regex regex = new Regex("^[A-Z0-9]{3,4}_(([0-9]{2}[A-Z]{3})[0-9]{2})_([A-Z])?(_[-\\w]+)?(_[\\+-.,0-9]+)?.dgn$");

        string sourceFileName = Path.GetFileName(dgnUri);
        Match match = regex.Match(sourceFileName);
        if (!match.Success)
        {
            Logger.Log.Warn($"'{sourceFileName}' - не удалось распарсить имя файла");
            return false;
        }

        unitCode = match.Groups[2].Value;
        specTag = match.Groups[3].Value;
        itemCode = match.Groups[1].Value;


        string xmlDataPath = Path.ChangeExtension(dgnUri, ".xml");
        try
        {
            simFile = new SimFile(xmlDataPath);
        }
        catch (Exception ex)
        {
            ex.LogError($"Ошибка в чтении файла '{xmlDataPath}': ");
            return false;
        }

        return true;

        //bool IsUnrecognizedSpec = false;
        //if (Config.Instance.TagToSpecialization.ContainsKey(specTag))
        //{
        //    specTag = Config.Instance.TagToSpecialization[specTag];
        //}
        //else
        //{
        //    specTag = "Unrecognized";
        //    IsUnrecognizedSpec = true;
        //    Logger.Log.Warn($"'{sourceFileName}:' - '{specTag}' - не распозана литера специальности, но файл будет обработан в соответствии с конфигом");
        //}
    }

    static bool processFile(string dgnUri, string outputFolder)
    {
        string unitCode;
        string specTag;
        string itemCode;
        SimFile simFile;

        if (!getData(dgnUri, out unitCode, out specTag, out itemCode, out simFile))
        {
            return false;
        }

        string sourceFileName = Path.GetFileName(dgnUri);

        bool IsUnrecognizedSpec = false;

        if (Config.Instance.TagToSpecialization.ContainsKey(specTag))
        {
            specTag = Config.Instance.TagToSpecialization[specTag];
        }
        else
        {
            specTag = "Unrecognized";
            IsUnrecognizedSpec = true;
            Logger.Log.Warn($"'{sourceFileName}:' - '{specTag}' - не распозана литера специальности, но файл будет обработан в соответствии с конфигом");
        }

        if (simFile.CatalogToElement.Keys.Count() == 0)
        {
            Logger.Log.Warn($"'{sourceFileName}:' не найдены элементы для обработки");
            return false;
        }

        // Открываем Исходную модель:

        DgnFile sourceFile;
        DgnModel sourceModel;
        DgnDocument sourceDgnDoc;
        DgnFileOwner sourceDgnFileOwner;
        LoadOrCreate_FileAndDefaultModel(out sourceFile, out sourceModel,
            out sourceDgnDoc, out sourceDgnFileOwner, dgnUri, null);

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
            string[] structure = new string[] { unitCode, specTag, itemCode };

            if (IsUnrecognizedSpec)
            {
                specTag = (typeData?.Specialization != null) ? 
                    typeData.Specialization : "Unrecognized";
                structure = new string[] { unitCode, specTag, itemCode };
            }
            else if (typeData?.Specialization != null &&
                !typeData.Specialization.Equals(specTag))
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
                            $"Не найден элемент '{simEl.ElementId}' в файле '{dgnUri}'");
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
        return true;
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

    public static void clearFolder(string FolderName)
    {
        DirectoryInfo dir = new DirectoryInfo(FolderName);

        foreach(FileInfo fi in dir.GetFiles())
        {
            fi.Delete();
        }

        foreach (DirectoryInfo di in dir.GetDirectories()) 
        {
            clearFolder(di.FullName);
            di.Delete();
        }
    }

    public static bool HasWriteAccessToFolder(string folderPath)
    {
        try
        {
            bool canWrite = false;

            string tmpName = Path.GetFileName(Path.GetTempFileName());
            string tmpPath = Path.Combine(folderPath, tmpName);
            using(var fileStream = File.Create(tmpPath))
            {
                canWrite = fileStream.CanWrite;                    
                fileStream.Close();
            }
            File.Delete(tmpPath);
            return canWrite;
        }
        catch (UnauthorizedAccessException)
        {
            return false;
        }
    }
}
}
