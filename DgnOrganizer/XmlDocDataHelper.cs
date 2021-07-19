using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.Xml;
using System.Xml.XPath;
using System.Xml.Linq;

namespace DgnOrganizer
{
    class XmlDocDataHelper
    {
        private XDocument xDoc_;

        public XmlDocDataHelper(string uri)
        {
            xDoc_ = XDocument.Load(uri);

            var elements = xDoc_.XPathSelectElements("DataGroupSystem/SimFile/SimElement");

            XPathDocument xpathDoc = new XPathDocument(uri);
            XPathNavigator navigator =xpathDoc.CreateNavigator();

        }

         
    }
}
