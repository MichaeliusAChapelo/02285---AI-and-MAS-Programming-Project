using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Pipes;
using System.Text;

namespace ProcessCommunication
{
    public class CommunicationClient : IDisposable
    {
        private readonly PipeStream Client;
        private readonly StreamReader Reader;

        public CommunicationClient(string serverString)
        {
            this.Client = new AnonymousPipeClientStream(serverString);
        }

        public string ReadLine()
        {
            return Reader.ReadLine();
        }

        public void Dispose()
        {
            Reader.Close();
            Client.Close();
        }
    }
}
