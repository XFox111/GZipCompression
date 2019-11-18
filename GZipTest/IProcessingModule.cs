using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GZipTest
{
    interface IProcessingModule
    {
        void Run(string input, string output);
        void Stop();
        event ProgressChangedEventHandler ProgressChanged;
        event EventHandler Complete;
        event ErrorEventHandler ErrorOccured;
    }
}
