using Bentley.DgnPlatformNET;
using Bentley.DgnPlatformNET.Elements;
using DgnOrganizer.XmlData;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Configuration;

using System.Text.RegularExpressions;
using System.Collections.Specialized;
using ConsoleToolkit;
using ConsoleToolkit.ApplicationStyles;

namespace DgnOrganizer
{
class Program : ConsoleApplication
{
    static void Main(string[] args)
    {
        Toolkit.Execute<Program>(args);
    }

    protected override void Initialise()
    {
        base.HelpOption<Options>(o => o.Help);
        base.Initialise();
    }
}
}
