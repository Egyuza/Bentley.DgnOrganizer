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
    static readonly string path_ =
        Path.ChangeExtension(Assembly.GetExecutingAssembly().Location, ".Config.json");

    public Dictionary<string, string> TagToSpecialization{ get; set; }
    public Dictionary<string, CatalogTypeData> CatalogTypesData { get; set; }

    public static Config Instance => instance_ ?? (instance_ = getInstance_());

    private static Config instance_;
    public static Config getInstance_()
    {
        string text = File.ReadAllText(path_, UTF8Encoding.UTF8);
        return JsonConvert.DeserializeObject<Config>(text);
    }

    public void SaveChanges()
    {
        string json = JsonConvert.SerializeObject(this, Formatting.Indented);
        File.WriteAllText(path_, json, UTF8Encoding.UTF8);
    }

    public static void ExportTestToFile()
    {
        var config = new Config();
        config.TagToSpecialization = new Dictionary<string, string>();
        config.TagToSpecialization.Add("A", "Architecture");

        config.CatalogTypesData = new Dictionary<string, CatalogTypeData>();
        config.CatalogTypesData.Add("Door", new CatalogTypeData() {
            Specialization = "Architecture", OverrideName = "Двери" });
        config.CatalogTypesData.Add("Window", new CatalogTypeData() {
            Specialization = "Architecture", OverrideName = "Окно" });

        File.WriteAllText(path_, JsonConvert.SerializeObject(config));
    }
}
}
