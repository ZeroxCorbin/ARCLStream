using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;


namespace ARCL
{
    public class FileSearch
    {

        public static IEnumerable<FileSearchResults> Find(string file, string search, bool stopOnFirst = false)
        {
            IEnumerable<FileSearchResults> stringsFound = Enumerable.Empty<FileSearchResults>();
            // Open a file with the StreamReaderEnumerable and check for a string.
            
            try
            {
                if (stopOnFirst)
                {
                    IList<FileSearchResults> sr = new List<FileSearchResults>();

                    sr.Add((from fsr in new StreamReaderEnumerable(file)
                           where Regex.IsMatch(fsr.Line, search)
                           select fsr).FirstOrDefault());

                    if(sr.ElementAt(0) != null) stringsFound = sr.AsEnumerable();
                }
                else
                {
                    stringsFound =
                          from fsr in new StreamReaderEnumerable(file)
                          where Regex.IsMatch(fsr.Line, search)
                          select fsr;
                }

            }
            catch (FileNotFoundException)
            {

            }

            return stringsFound;
        }

        // A custom class that implements IEnumerable(T). When you implement IEnumerable(T), 
        // you must also implement IEnumerable and IEnumerator(T).
        public class StreamReaderEnumerable : IEnumerable<FileSearchResults>
        {
            private string _filePath;
            public StreamReaderEnumerable(string filePath)
            {
                _filePath = filePath;
            }

            // Must implement GetEnumerator, which returns a new StreamReaderEnumerator.
            public IEnumerator<FileSearchResults> GetEnumerator()
            {
                return new StreamReaderEnumerator(_filePath);
            }

            // Must also implement IEnumerable.GetEnumerator, but implement as a private method.
            private IEnumerator GetEnumerator1()
            {
                return this.GetEnumerator();
            }
            IEnumerator IEnumerable.GetEnumerator()
            {
                return GetEnumerator1();
            }
        }

        // When you implement IEnumerable(T), you must also implement IEnumerator(T), 
        // which will walk through the contents of the file one line at a time.
        // Implementing IEnumerator(T) requires that you implement IEnumerator and IDisposable.
        public class StreamReaderEnumerator : IEnumerator<FileSearchResults>
        {
            private RingBuffer<string> _buf = new RingBuffer<string>(5);
            private int _lineNum;
            private StreamReader _sr;
            public StreamReaderEnumerator(string filePath)
            {
                _sr = new StreamReader(filePath);
            }

            private FileSearchResults _current = new FileSearchResults();
            // Implement the IEnumerator(T).Current publicly, but implement 
            // IEnumerator.Current, which is also required, privately.
            public FileSearchResults Current
            {

                get
                {
                    if (_sr == null || _current == null)
                    {
                        throw new InvalidOperationException();
                    }

                    return _current;
                }
            }

            private object Current1
            {

                get { return this.Current; }
            }

            object IEnumerator.Current
            {
                get { return Current1; }
            }

            // Implement MoveNext and Reset, which are required by IEnumerator.
            public bool MoveNext()
            {
                _current.Line = _sr.ReadLine();
                if (_current.Line == null)
                    return false;

                _lineNum++;
                _current.LineNumber = _lineNum;

                _current.Buffer = _buf.ShallowCopy();

                _buf.Add(_lineNum.ToString() + "\t" + _current.Line + Environment.NewLine);

                return true;
            }

            public void Reset()
            {
                _sr.DiscardBufferedData();
                _sr.BaseStream.Seek(0, SeekOrigin.Begin);
                _current = new FileSearchResults();
                _lineNum = 0;
            }

            // Implement IDisposable, which is also implemented by IEnumerator(T).
            private bool disposedValue = false;
            public void Dispose()
            {
                Dispose(true);
                GC.SuppressFinalize(this);
            }

            protected virtual void Dispose(bool disposing)
            {
                if (!this.disposedValue)
                {
                    if (disposing)
                    {
                        // Dispose of managed resources.
                    }
                    _current = null;
                    if (_sr != null)
                    {
                        _sr.Close();
                        _sr.Dispose();
                    }
                }

                this.disposedValue = true;
            }

            ~StreamReaderEnumerator()
            {
                Dispose(false);
            }
        }
    }
}
