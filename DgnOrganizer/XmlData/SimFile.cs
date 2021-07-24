using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using System.Xml;
using System.Xml.XPath;
using System.Xml.Linq;
using Bentley.DgnPlatformNET;

namespace DgnOrganizer.XmlData
{
class SimFile
{
    private XDocument xDoc_;

    public Dictionary<string, IList<SimElement>> CatalogToElement { get;}

    public SimFile(string uri)
    {
        xDoc_ = XDocument.Load(uri);

        IEnumerable<XElement> xElements = 
            xDoc_.XPathSelectElements("DataGroupSystem/SimFile/SimElement");

        CatalogToElement = new Dictionary<string, IList<SimElement>>();

        var nullCatalogTypesElements = new List<string>();

        foreach(var xElement in xElements)
        {
            var simEl = new SimElement(xElement);

            if (string.IsNullOrWhiteSpace(simEl.CatalogType))
            {
                nullCatalogTypesElements.Add(simEl.ElementId.ToString());
            }
            else
            {
                if (!CatalogToElement.ContainsKey(simEl.CatalogType))
                {
                    CatalogToElement.Add(simEl.CatalogType, new List<SimElement>());
                }
                CatalogToElement[simEl.CatalogType].Add(simEl);
            }
        }

        if (nullCatalogTypesElements.Count > 0)
        {
            var builder = new StringBuilder();
            foreach (string elementId in nullCatalogTypesElements)
            {
                builder.Append(elementId).Append(", ");
            }
            string ids = $"{builder.ToString().TrimEnd(", ".ToCharArray())}";

            Logger.Log.ErrorEx($"Пустое значение 'catalogType' " +
                $"у элеметов с ElementId из списка: '[{ids}]' в файле '{uri}'");
        }
    }
}
}
