using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WindowsFormsApplication1.src.Connection
{
    public interface FTDI_Read_Communication_Interface
    {
        void updateButtonState(string data_msg,UInt32 numBytesAvailable, UInt32 numBytesRead);
    }
}
