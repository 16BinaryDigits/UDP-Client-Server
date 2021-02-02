using System;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Security;
using System.Threading;
using System.Threading.Tasks;

namespace UDPS
{
    public class Server
    {
        [DllImport("Kernel32.dll"), SuppressUnmanagedCodeSecurity]
        static extern int GetCurrentProcessorNumber();

        static Config udpConfig;
        static IPEndPoint endPoint;

        public Config Config { get { return udpConfig; } set { udpConfig = value; } }

        public Server() 
        {
            if (udpConfig is null) udpConfig = new Config();
            if (endPoint is null) endPoint = new IPEndPoint(General.GetLocalIP(AddressFamily.InterNetwork), udpConfig.Port);
        }

        public Server(Config config) { udpConfig = config; }

        public async void Start(Write write)
        {
            try
            {
                if (udpConfig is null) udpConfig = new Config();
                if (endPoint is null) endPoint = new IPEndPoint(General.GetLocalIP(AddressFamily.InterNetwork), udpConfig.Port);

                using (var udpClient = new UdpClient(endPoint))
                {
                    while (true)
                    {
                        UdpReceiveResult udpReceiveResult = await udpClient.ReceiveAsync();
                        await ClientConnectionAsync(udpClient, udpReceiveResult, udpConfig, write, BackgroundWork);
                    }
                }
            }
            catch (Exception e) { General.WriteToConsole(e.Message); }
        }

        async Task ClientConnectionAsync(UdpClient client, UdpReceiveResult udpReceiveResult, Config config, Write write, Action<Read> action = null)
        {
            try
            {
                Read read = new Read();

                if (udpReceiveResult.Buffer.Length > 0)
                {
                    read.IP = udpReceiveResult.RemoteEndPoint.ToString();
                    read.ID = write.ID;
                    read.ReceivedBufferSize = udpReceiveResult.Buffer.Length;
                    read.ThreadID = Thread.CurrentThread.ManagedThreadId;
                    read.CoreID = GetCurrentProcessorNumber();
                    read.Message = config.Encoding.GetString(udpReceiveResult.Buffer);
                }

                if (write.Message != string.Empty)
                {
                    byte[] buffer = config.Encoding.GetBytes(General.SerializeToJson(write));
                    await client.SendAsync(buffer, buffer.Length, udpReceiveResult.RemoteEndPoint);
                }

                if (action != null) action.Invoke(read);
            }
            catch (Exception exception) { action.Invoke(new Read { Exception = exception }); }
        }

        public virtual void BackgroundWork(Read read)
        {

        }
    }
}
