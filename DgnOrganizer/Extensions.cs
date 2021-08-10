using log4net;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DgnOrganizer
{
    static class Extensions
    {
        private static int errorsCount;
        public static void StartErrorsCounting(this ILog logger)
        {
            errorsCount = 0;
        }
        public static int GetErrorsCount(this ILog logger)
        {
            return errorsCount;
        }

        public static void ErrorEx(this ILog logger, object message, Exception ex = null)
        {
            ++errorsCount;
            if (ex == null)
                logger.Error(message);
            else
                logger.Error(message, ex);
        }


        public static void LogError(this Exception ex, string firstMsg = null)
        {
            string text = string.IsNullOrWhiteSpace(firstMsg) 
                ? string.Empty : firstMsg + "\n";
                
            Logger.Log.ErrorEx(text + ex.Message + 
                (ex.InnerException != null ? 
                    ("\n" + ex.InnerException.Message) : string.Empty)
#if DEBUG
                    + "\n" + ex.StackTrace
#endif
            );
        }
    }
}
