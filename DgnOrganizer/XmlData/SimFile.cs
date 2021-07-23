using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using System.Xml;
using System.Xml.XPath;
using System.Xml.Linq;

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

        foreach(var xElement in xElements)
        {
            var simEl = new SimElement(xElement);

            if (!CatalogToElement.ContainsKey(simEl.CatalogType))
            {
                CatalogToElement.Add(simEl.CatalogType, new List<SimElement>());
            }

            CatalogToElement[simEl.CatalogType].Add(simEl);
        }
    }
}
}
