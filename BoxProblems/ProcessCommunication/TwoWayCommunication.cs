using System;
using System.Collections.Generic;
using System.Text;

namespace ProcessCommunication
{
    public class TwoWayCommunication : IDisposable
    {
        private CommunicationServer Server;
        private CommunicationClient Client;

        private TwoWayCommunication()
        {
        }

        private void StartServer(string serverName)
        {
            if (Server != null)
            {
                throw new Exception("Can't start the server twice.");
            }
            Server = new CommunicationServer(serverName);
        }

        private void StartClient(string serverName)
        {
            if (Client != null)
            {
                throw new Exception("Can't start the client twice.");
            }
            Client = new CommunicationClient(serverName);
        }

        public static TwoWayCommunication StartServerFirst(string thisProcessServerName, string otherProcessServerName)
        {
            var com = new TwoWayCommunication();
            com.StartServer(thisProcessServerName);
            com.WriteLine(otherProcessServerName);
            com.WriteLine(string.Empty);
            com.StartClient(otherProcessServerName);
            com.ReadLine();

            return com;
        }

        public static TwoWayCommunication StartClientFirst(string otherProcessServerName)
        {
            var com = new TwoWayCommunication();
            com.StartClient(otherProcessServerName);
            string thisProcessServerName = com.ReadLine();
            com.StartServer(thisProcessServerName);
            com.ReadLine();
            com.WriteLine(string.Empty);

            return com;
        }

        public void WriteLine(string message)
        {
            Server.WriteLine(message);
        }

        public string ReadLine()
        {
            return Client.ReadLine();
        }

        public void Dispose()
        {
            Server.Dispose();
            Client.Dispose();
        }
    }
}
