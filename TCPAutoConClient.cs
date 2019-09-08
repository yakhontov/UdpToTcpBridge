using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace UdpToTcp
{
    public class TCPAutoConClient
    {
        private enum SocketState { NotConnected, Connected, Connecting };
        private TcpClient socketConnection = null;
        private NetworkStream socketStream = null;
        private SocketState sockState = SocketState.NotConnected;

        private int conPort = 0;
        private string conHost = "";

        public TCPAutoConClient(string newHost = "", int newPort = 0)
        {
            conPort = newPort;
            conHost = newHost;
            Reconnect();
        }

        public int Port
        {
            get { return conPort; }
            set { SetPort(value); }
        }

        public string Host
        {
            get { return conHost; }
            set { SetHost(value); }
        }

        public void SetHostPort(string newHost, int newPort)
        {
            conHost = newHost;
            conPort = newPort;
            Reconnect();
        }

        public void SetPort(int newPort)
        {
            conPort = newPort;
            Reconnect();
        }

        public void SetHost(string newHost)
        {
            conHost = newHost;
            Reconnect();
        }

        public bool IsConnected() { return sockState == SocketState.Connected; }

        private void Reconnect()
        {
            sockState = SocketState.NotConnected;
            Connect();
        }

        private void Connect()
        {
            if (sockState != SocketState.NotConnected ||
                conHost == "" ||
                conPort == 0)
                return;

            sockState = SocketState.Connecting;

            try
            {
                if (socketConnection != null)
                    socketConnection.Close();
                socketConnection = new TcpClient();
                socketConnection.BeginConnect(conHost, conPort, new AsyncCallback(ConnectCallback), socketConnection);
                //Console.WriteLine("Simulator socket.BeginConnect() done. Host: " + simHost + ":" + simPort);
            }
            catch (Exception e) // (SocketException socketException)
            {
                sockState = SocketState.NotConnected;
                Log("Exception at socket.BeginConnect(). Host: " + conHost + ":" + conPort + " Error: " + e);
            }

        }

        public void ConnectCallback(IAsyncResult ar)
        {
            TcpClient socketCon = (TcpClient)ar.AsyncState;
            try
            {
                socketCon.EndConnect(ar);
                if (socketCon.Connected ||
                    // Если между вызовом Connect и ConnectCallback был установлен новый хост или порт,
                    // то (sockState == SocketState.NotConnected) и нужно сбросить подключение
                    sockState != SocketState.Connecting)
                { 
                    socketStream = socketCon.GetStream();
                    sockState = SocketState.Connected;
                    Log("Connected. Host: " + conHost + ":" + conPort);
                }
                else
                    sockState = SocketState.NotConnected;
            }
            catch (Exception e)
            {
                sockState = SocketState.NotConnected;
                Log("Exception at socket.GetStream(). Host: " + conHost + ":" + conPort + " Error: " + e);
            }
        }

        public bool Send(Byte[] data)
        {
            if (sockState != SocketState.Connected || socketStream == null)
            {
                Connect();
                return false;
            }

            try
            {
                socketStream.Write(data, 0, data.Length);
                return true;
            }
            catch(ObjectDisposedException e)
            {
                sockState = SocketState.NotConnected;
                Log("Socket write exception. Host: " + conHost + ":" + conPort + " Error: " + e);
            }
            catch (IOException e) // (SocketException socketException)
            {
                sockState = SocketState.NotConnected;
                Log("Socket write exception. Host: " + conHost + ":" + conPort + " Error: " + e);
            }
            return false;
        }

        byte[] readedData = new byte[10000];
        int readedDataPtr = 0;
        public string Receive()
        {
            if (sockState != SocketState.Connected || socketStream == null)
            {
                Connect();
                return null;
            }

            try
            {
                int b;
                while(socketStream.DataAvailable)
                {
                    if((b = socketStream.ReadByte()) == -1) // В потоке больше нет данных
                        return null;
                    if ((b == 0) || (b == '\r') || (b == '\n')) // В потоке попался символ конца строки
                    {
                        if (readedDataPtr == 0)
                            return null;
                        string s = System.Text.Encoding.ASCII.GetString(readedData, 0, readedDataPtr);
                        readedDataPtr = 0;
                        return s;
                    }
                    if (readedDataPtr < readedData.Length) // Если превышен максимальный размер буфера - просто отбрасываем данные, пока не попадется конец строки
                        readedData[readedDataPtr++] = (byte)b;
                }
            }
            catch (Exception e)
            {
                sockState = SocketState.NotConnected;
                Log("Reading socket exception: " + e.Message);
            }
            return null;
        }

        virtual public void Log(string s)
        {
            Console.WriteLine(s);
        }
    }
}
