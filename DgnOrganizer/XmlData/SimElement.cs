using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.Xml;
using System.Xml.XPath;
using System.Xml.Linq;
using Bentley.DgnPlatformNET;

namespace DgnOrganizer.XmlData
{
class SimElement
{
    public string CatalogType { get; set; }
    public string CatalogItem { get; set; }
    public ElementId ElementId { get; set; }

    public SimElement(XElement element)
    {
        ElementId = (ElementId)long.Parse(element.Attribute("ID").Value);
        XElement dataGroup = element.XPathSelectElement("DataGroupInstances/DataGroup");
        CatalogType = dataGroup.Attribute("catalogType").Value;
        CatalogItem = dataGroup.Attribute("catalogItem").Value;
    }
}
}


