using System;
using System.Diagnostics;
using System.IO;
using System.IO.Pipes;

namespace ProcessCommunication
{
    public class CommunicationServer : IDisposable
    {
        private readonly NamedPipeServerStream Server;
        private readonly StreamWriter Writer;

        public CommunicationServer(string serverName)
        {
            this.Server = new NamedPipeServerStream(serverName, PipeDirection.Out, 1);
            this.Writer = new StreamWriter(Server);
            this.Writer.AutoFlush = true;
        }

        public void WriteLine(string message)
        {
            Writer.WriteLine(message);
            Server.WaitForPipeDrain();
        }

        public void Dispose()
        {
            Writer.Close();
            Server.Close();
        }
    }
}
