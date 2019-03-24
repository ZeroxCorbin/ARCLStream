using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ARCL
{
    public class FileSearchResults
    {
        private string _Line;

        public string Line
        {
            get { return _Line; }
            set { _Line = value; }
        }

        private int _LineNumber;
        public int LineNumber
        {
            get { return _LineNumber; }
            set { _LineNumber = value; }
        }

        private RingBuffer<string> _Buf;
        public RingBuffer<string> Buffer
        {
            get { return _Buf; }
            set { _Buf = value; }
        }
    }
}
