using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Timers;

namespace Temp.Handlers
{
    internal class HandlerArduino
    {
        public bool Connect(string portname)
        {
            try
            {
                port = new SerialPort(portname, 9600, Parity.None, 8, StopBits.One);
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
                    port.Close();
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
                            readTemp_and_status = port.ReadLine();
                            retry = 0;
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
                        string read = port.ReadLine();
                        if (!read.Contains(String.Format("S;{0}", output)))
                        {
                            retry--;
                        }
                        else
                        { 
                            retry = 0; 
                        }
                    }
                }
                catch
                {
                    retry--;
                }
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
