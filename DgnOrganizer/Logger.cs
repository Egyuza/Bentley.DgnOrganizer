using System;
using System.IO;
using System.Text;
using System.Reflection; 

using log4net;
using log4net.Appender;
using log4net.Config;
using log4net.Core;
using log4net.Layout;

 
namespace DgnOrganizer
{
class Logger
{
    private static ILog log_;
    public static ILog Log => log_ ?? (log_ = getLog_());

    public static void setLogFolder(string path)
    {
        if(!Directory.Exists(path))
        {
            Directory.CreateDirectory(path);
        } 
        overrideLogFolder_ = path;
    }

    private static ILog getLog_()
    {
        PatternLayout patternLayout = new PatternLayout
        {
            ConversionPattern = "%date %level %message%newline"
        };

        patternLayout.ActivateOptions(); 

        var coloredConsoleAppender = new ColoredConsoleAppender();
        coloredConsoleAppender.AddMapping(new ColoredConsoleAppender.LevelColors
        {

            BackColor = ColoredConsoleAppender.Colors.Red,
            ForeColor = ColoredConsoleAppender.Colors.White,
            Level = Level.Error
        });

        coloredConsoleAppender.AddMapping(new ColoredConsoleAppender.LevelColors
        {               
            ForeColor = ColoredConsoleAppender.Colors.Yellow,
            Level = Level.Warn
        });

        coloredConsoleAppender.Layout = patternLayout;
        coloredConsoleAppender.ActivateOptions();         

        BasicConfigurator.Configure();           

        ILog log = LogManager.GetLogger(AppName);
        var logger = log.Logger as log4net.Repository.Hierarchy.Logger;

        logger.AddAppender(coloredConsoleAppender);
        logger.AddAppender(GetFileAppender_()); 

        logger.Level = Level.All;
        logger.Additivity = false;
        return log;
    }

    private static FileAppender fileAppender_;

    private static FileAppender GetFileAppender_()
    {
        if (fileAppender_ != null)
            return fileAppender_; 

        string folderPath = overrideLogFolder_ ?? getDefaultLogFolder_();
        string filePath = Path.Combine(folderPath, AppName +
            $" [{DateTime.Now.ToString().Replace(":", ".")}]" + ".log");

        PatternLayout layout = new PatternLayout(
            "[%date{dd.MM.yyyy HH:mm:ss}] [%level]" + " %message%newline%exception");

        layout.ActivateOptions();

        fileAppender_ = new FileAppender
        {
            Threshold = Level.All,
            Layout = layout,
            File = filePath,
            Encoding = Encoding.UTF8,
            AppendToFile = true,
            ImmediateFlush = true,
            //LockingModel  = FileAppender.InterProcessLock
        };

        fileAppender_.ActivateOptions();
        return fileAppender_;
    }

    private static string AppName =>
        Assembly.GetExecutingAssembly().GetName().Name;

    private static string overrideLogFolder_;

    private static string getDefaultLogFolder_() =>
        Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "_log");
        //Path.Combine(Environment.GetFolderPath(
        //        Environment.SpecialFolder.LocalApplicationData),
        //    Assembly.GetExecutingAssembly().GetName().Name);

}
}

 