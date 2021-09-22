using FTD2XX_NET;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace WindowsFormsApplication1.src.Connection
{
    public class FTD_UART_Communiation
    {
        private FTDI myFtdiDevice = null;
        private FTDI.FT_STATUS ftStatus = FTDI.FT_STATUS.FT_OK;

        private UInt32 ftdiDeviceCount = 0;
        private string impulse_device_UART_SerialNo = "A50285BI";
        private UInt32 communication_sample_baud_rate = 9600; // default value
        private UInt32 readTimeOut_time = 5000; // 5000 s time out

        private UInt32 numBytesWritten = 0; // written data bytes count

        private FTDI_Read_Communication_Interface communication_Interface;

        private AutoResetEvent resetDataEvent;
        private BackgroundWorker backgroundWorker;

        public enum BaudRates_DV : int
        {
            _9600 = 9600,
            _14400 = 14400,
            _19200 = 19200
        }
        public FTD_UART_Communiation(string serialNo, BaudRates_DV baudrate_insert_enum, UInt32 readTimeOut_seconds)
        {
            myFtdiDevice = new FTDI();
            this.impulse_device_UART_SerialNo = serialNo;
            this.communication_sample_baud_rate = Convert.ToUInt32(baudrate_insert_enum);
            this.readTimeOut_time = readTimeOut_seconds;
            
        }
        public void set_interface_Controll_Object(object set_Form_object)
        {

            communication_Interface = (FTDI_Read_Communication_Interface)set_Form_object;
        }
        private bool FTD_Devices_Number_Avabile()
        {
            // Determine the number of FTDI devices connected to the machine
            ftStatus = myFtdiDevice.GetNumberOfDevices(ref ftdiDeviceCount);
            if (ftStatus == FTDI.FT_STATUS.FT_OK)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        private bool this_Device_isAvalabile()
        {
            bool isAvalabels = false;
            // All storage for device info list Array
            FTDI.FT_DEVICE_INFO_NODE[] ftdiDeviceList = new FTDI.FT_DEVICE_INFO_NODE[ftdiDeviceCount];
            //avabile device list
            ftStatus = myFtdiDevice.GetDeviceList(ftdiDeviceList);
            if (ftStatus == FTDI.FT_STATUS.FT_OK)
            {
                foreach (FTDI.FT_DEVICE_INFO_NODE ftdi_deviceInto in ftdiDeviceList)
                {
                    if ((impulse_device_UART_SerialNo.ToUpper()).Equals(ftdi_deviceInto.SerialNumber.ToString().ToUpper()))
                    {
                        isAvalabels = true;
                    }
                    else
                    {
                        isAvalabels = false;
                    }
                }
            }
            else
            {
                isAvalabels = false;
            }
            return isAvalabels;
        }
        public bool isDevice_Avalables()
        {
            if (FTD_Devices_Number_Avabile())
            {
                if (this_Device_isAvalabile())
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }
            else
            {
                return false;
            }
        }
        public string getConnection_and_Device()
        {
            string error_msg = "NON";
            ftStatus = myFtdiDevice.OpenBySerialNumber(impulse_device_UART_SerialNo);

            if (ftStatus != FTDI.FT_STATUS.FT_OK)
            {
                error_msg = "Failed to open device (error " + ftStatus.ToString() + ")";
            }

            ftStatus = myFtdiDevice.SetBaudRate(communication_sample_baud_rate);
            if (ftStatus != FTDI.FT_STATUS.FT_OK)
            {
                return error_msg = "Failed to set Baud rate (error " + ftStatus.ToString() + ")";
            }

            ftStatus = myFtdiDevice.SetDataCharacteristics(FTDI.FT_DATA_BITS.FT_BITS_8, FTDI.FT_STOP_BITS.FT_STOP_BITS_1, FTDI.FT_PARITY.FT_PARITY_NONE);
            if (ftStatus != FTDI.FT_STATUS.FT_OK)
            {
                return error_msg = "Failed to set data characteristics (error " + ftStatus.ToString() + ")";
            }
            //(FTDI.FT_FLOW_CONTROL.FT_FLOW_RTS_CTS bits 256)
            ftStatus = myFtdiDevice.SetFlowControl(FTDI.FT_FLOW_CONTROL.FT_FLOW_RTS_CTS, 0x11, 0x13);
            if (ftStatus != FTDI.FT_STATUS.FT_OK)
            {
                return error_msg = "Failed to set flow control (error " + ftStatus.ToString() + ")";
            }

            // Set read timeout to 5 seconds, write timeout to infinite
            ftStatus = myFtdiDevice.SetTimeouts(readTimeOut_time, 0);
            if (ftStatus != FTDI.FT_STATUS.FT_OK)
            {
                return error_msg = "Failed to set timeouts (error " + ftStatus.ToString() + ")";
            }

            //create Event data rest
            resetDataEvent = new AutoResetEvent(false);
            //eventWait_refernc = new System.Threading.EventWaitHandle(false, System.Threading.EventResetMode.AutoReset);
            ftStatus =myFtdiDevice.SetEventNotification(FTDI.FT_EVENTS.FT_EVENT_RXCHAR, resetDataEvent);
            
            if (ftStatus!=FTDI.FT_STATUS.FT_OK)
            {
                return error_msg = "Failed to Impliments AutoResetEvent";
            }
            else
            {
                backgroundWorker = new BackgroundWorker();
                backgroundWorker.DoWork += new DoWorkEventHandler(eventLisitiner_start_function);
                backgroundWorker.RunWorkerAsync();
                
            }
          
            return error_msg;
        }

        public bool send_Data(string wrtting_data)
        {
            //numBytesWritten
            ftStatus = myFtdiDevice.Write(wrtting_data, wrtting_data.Length, ref numBytesWritten);
            if (ftStatus != FTDI.FT_STATUS.FT_OK)
            {
                return false;
            }
            else
            {
                
                return true;
            }
        }

        public string read_data()
        {

            string error_msg = "NON"; // error msg or not return variable
            UInt32 beforeBytenum = 0;
            UInt32 numBytesAvailable = 0; // count of available bytes. recive
            short count_same_number = 0;
            do
            {
                ftStatus = myFtdiDevice.GetRxBytesAvailable(ref numBytesAvailable);
                if (ftStatus != FTDI.FT_STATUS.FT_OK)
                {
                    // Wait for a key press
                    error_msg="Failed to get number of bytes available to read (error " + ftStatus.ToString() + ")";
                }
                else
                {
                    if (numBytesAvailable== beforeBytenum)
                    {
                        count_same_number++;
                    }
                    beforeBytenum = numBytesAvailable;
                }
                Thread.Sleep(2);
            } while (count_same_number<=3);
            

            string readData=null; // read to send data string 
            UInt32 numBytesRead = 0;
            // Note that the Read method is overloaded, so can read string or byte array data

            ftStatus = myFtdiDevice.Read(out readData, numBytesAvailable, ref numBytesRead);
            if (ftStatus != FTDI.FT_STATUS.FT_OK)
            {
                // Wait for a key press
                error_msg="Failed to read data (error " + ftStatus.ToString() + ")";
            }
            else
            {
                communication_Interface.updateButtonState(readData, numBytesAvailable, numBytesRead);
                readData = null;
                numBytesRead = 0;
                beforeBytenum = 0;
                numBytesAvailable = 0; 
                count_same_number = 0;
            }

            return error_msg;
        }

        private void eventLisitiner_start_function(object sender,DoWorkEventArgs e)
        {
            
            while (true)
            {
                if (resetDataEvent.WaitOne())
                {
                    read_data();
                }
                Thread.Sleep(2); 
            }
            
        }


    }
}
