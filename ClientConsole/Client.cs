using System;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Security;
using System.Threading;
using System.Threading.Tasks;

namespace UDPC
{
    public class Client
    {
        [DllImport("Kernel32.dll"), SuppressUnmanagedCodeSecurity]
        static extern int GetCurrentProcessorNumber();

        static Config udpConfig = null;

        public Config Config { get { return udpConfig; } }

        public Client() { udpConfig = new Config { }; }

        public Client(Config config) { udpConfig = config; }

        public async void Connect(Config config, Write write)
        {
            try
            {
                using (var udpClient = new UdpClient())
                {
                    udpClient.Connect(new IPEndPoint(IPAddress.Parse(config.IP), config.Port));

                    if (udpClient.Client.Connected)
                    {
                        await ClientConnectionAsync(udpClient, config, write, BackgroundWork);
                    }
                }
            }
            catch (Exception exception) { BackgroundWork(new Read { Exception = exception }); }
        }

        async Task ClientConnectionAsync(UdpClient client, Config config, Write write, Action<Read> action = null)
        {
            try
            {
                var read = new Read();
                var remoteIP = new IPEndPoint(IPAddress.Parse(config.IP), config.Port);

                if (write.Message != string.Empty)
                {
                    byte[] buffer = config.Encoding.GetBytes(General.SerializeToJson(write));
                    await client.SendAsync(buffer, buffer.Length);
                }

                UdpReceiveResult udpReceiveResult = await client.ReceiveAsync();

                if (udpReceiveResult.Buffer.Length > 0)
                {
                    read.IP = client.Client.RemoteEndPoint.ToString();
                    read.ID = write.ID;
                    read.ReceivedBufferSize = udpReceiveResult.Buffer.Length;
                    read.ThreadID = Thread.CurrentThread.ManagedThreadId;
                    read.CoreID = GetCurrentProcessorNumber();
                    read.Message = config.Encoding.GetString(udpReceiveResult.Buffer);
                }

                if (action != null) { action.Invoke(read); }
            }
            catch (Exception exception) { action.Invoke(new Read { Exception = exception }); }
        }

        public virtual void BackgroundWork(Read read)
        {

        }
    }
}
