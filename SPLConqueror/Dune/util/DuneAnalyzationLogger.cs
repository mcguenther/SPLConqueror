using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SPLConqueror_Core;

namespace Dune.util
{
    /// <summary>
    /// This class is a new <code>Logger</code>-class for the Dune-module. This logger class is necessary as we have a <code>Shell</code> in this module and have to split the output (either log or console).
    /// </summary>
    class DuneAnalyzationLogger : Logger
    {
        readonly object loggerLock = new Object();

        public DuneAnalyzationLogger(String outputLocation) : base(outputLocation)
        {
        }

        /// <summary>
        /// Logs general information depending on what log mechanism was chosen (console, file, gui). Todo: currently only logging at file
        /// </summary>
        /// <param name="msg">The message to be printed or logged</param>
        public override void logLine(String msg)
        {
            lock (loggerLock)
            {
                if (!msg.EndsWith(System.Environment.NewLine))
                    msg += System.Environment.NewLine;

                if (writer != null)
                    writer.Write(msg);
                else
                    Console.Write(msg);
            }
        }

        /// <summary>
        /// Logs general information depending on what log mechanism was chosen (console, file, gui). Todo: currently only logging at file
        /// </summary>
        /// <param name="msg">The message to be printed or logged</param>
        public override void log(String msg)
        {
            lock (loggerLock)
            {
                if (writer != null)
                    writer.Write(msg);
                else
                    Console.Write(msg);
            }
        }
    }
}
