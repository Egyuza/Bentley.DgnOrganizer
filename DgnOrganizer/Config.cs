using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Reflection;

using Newtonsoft.Json;

namespace DgnOrganizer
{

class CatalogTypeData
{
    public string Specialization { get; set; }
    public string OverrideName { get; set; }
}

class Config
{
    public static readonly string DefaultPath = Path.ChangeExtension(
        Assembly.GetExecutingAssembly().Location, ".Config.json");

    static string path_ = DefaultPath;

    public Dictionary<string, string> TagToSpecialization{ get; set; }
    public SortedDictionary<string, CatalogTypeData> CatalogTypesData { get; set; }

    public static Config Instance => instance_ ?? (instance_ = getInstance_());

    private static Config instance_;
    private static Config getInstance_()
    {
        string text = GetJsonFromFile_();
        return JsonConvert.DeserializeObject<Config>(text);
    }

    private static string GetJsonFromFile_()
    {
        return File.ReadAllText(path_, UTF8Encoding.UTF8);
    }

    public string ToJson()
    {
        return JsonConvert.SerializeObject(this, Formatting.Indented);
    }

    public bool HasChanges()
    {
        string prev = GetJsonFromFile_();
        string current = ToJson();

        return !current.Equals(prev);
    }

    public static void setPath(string path)
    {
        if (!File.Exists(path))
            throw new Exception($"Не найден конфиг-файл по указанному пути '{path}'");

        path_ = path;
    }

    public void SaveChanges()
    {
        string json = ToJson();
        File.WriteAllText(path_, json, UTF8Encoding.UTF8);
    }

    public static void ExportTestToFile()
    {
        var config = new Config();
        config.TagToSpecialization = new Dictionary<string, string>();
        config.TagToSpecialization.Add("A", "Architecture");

        config.CatalogTypesData = new SortedDictionary<string, CatalogTypeData>();
        config.CatalogTypesData.Add("Door", new CatalogTypeData() {
            Specialization = "Architecture", OverrideName = "Двери" });
        config.CatalogTypesData.Add("Window", new CatalogTypeData() {
            Specialization = "Architecture", OverrideName = "Окно" });

        File.WriteAllText(path_, JsonConvert.SerializeObject(config));
    }
}
}
