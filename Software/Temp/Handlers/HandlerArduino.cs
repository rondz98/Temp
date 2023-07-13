using System;
using System.IO.Ports;
using System.Threading;

namespace Temp.Handlers
{
    internal class HandlerArduino
    {
        public bool Connect(string portname)
        {
            try
            {
                port = new SerialPort(portname, 9600, Parity.None, 8, StopBits.One);
                port.ReadTimeout = 3000;
                port.WriteTimeout = 3000;
                port.Open();
            }
            catch {
                return false;
            }
            
            return true;
        }

        public bool Disconnect()
        {
            try
            {
                if (port != null)
                {
                    port.Close();
                }
            }
            catch
            {
                return false;
            }
            return true;
        }

        public string Read_Temp_and_Status()
        {
            int retry = 3;

            while (retry > 0)
            {
                try
                {
                    lock (ComLock)
                    {
                        try
                        {
                            ClearCom();
                            port.Write("R");
                            Thread.Sleep(100);
                            readTemp_and_status = port.ReadLine();
                            retry = -1;
                        }
                        catch
                        {
                            retry--;
                        }
                    }
                }
                catch
                {
                    retry--;
                }
            }
            if (retry == 0)
            {
                Disconnect();
                Thread.Sleep(300);
                Connect(port.PortName);
            }
            return readTemp_and_status;
        }

        public bool writeOutput(int output)
        {
            int retry = 5;

            while (retry > 0)
            {
                try
                {
                    lock (ComLock)
                    {
                        ClearCom();
                        port.WriteLine(String.Format("W;{0}", output));
                        Thread.Sleep(100);
                        string read = port.ReadLine();
                        if (!read.Contains(String.Format("S;{0}", output)))
                        {
                            retry--;
                        }
                        else
                        { 
                            retry = -1; 
                        }
                    }
                }
                catch
                {
                    retry--;
                }
            }
            if (retry == 0)
            {
                Disconnect();
                Thread.Sleep(300);
                Connect(port.PortName);
            }

            return true;
        }

        private void ClearCom()
        {
            port.DiscardInBuffer();
            port.DiscardOutBuffer();
        }

        /// <summary>
        /// Lock for COM access
        /// </summary>
        private Object ComLock = new Object();

        /// <summary>
        /// Serial port
        /// </summary>
        private SerialPort port = new SerialPort();

        /// <summary>
        /// Temperature and status string read
        /// </summary>
        private string readTemp_and_status;
    }
}
