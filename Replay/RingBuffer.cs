using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ARCL
{
    public class RingBuffer<T>
    {
        T[] m_Buffer;
        int m_NextWrite, m_Tail, m_Head, m_CurrRead;

        public RingBuffer(int length)
        {
            m_Buffer = new T[length];
            m_NextWrite = 0;
            m_Head = m_Tail = m_CurrRead = -1;
        }

        public int Length { get { return m_Buffer.Length; } }

        public void Add(T o)
        {
            if (m_Head == -1) // initial state
            {
                m_Head = 0;
                m_Tail = 0;
                m_CurrRead = 0;
            }
            else
            {
                m_Tail = m_NextWrite;
                if (m_Head == m_Tail)
                    m_Head = mod(m_Tail + 1, m_Buffer.Length);
                if (m_CurrRead == m_Tail)
                    m_CurrRead = -1;
            }
            m_Buffer[m_NextWrite] = o;
            m_NextWrite = mod(m_NextWrite + 1, m_Buffer.Length);
        }

        public T GetHead()
        {
            if (m_Head == -1)
                return default(T);

            m_CurrRead = m_Head;
            return m_Buffer[m_Head];
        }

        public T GetTail()
        {
            if (m_Head == -1)
                return default(T);

            m_CurrRead = m_Tail;
            return m_Buffer[m_Tail];
        }

        public T GetNext()
        {
            if (m_CurrRead == -1 || m_CurrRead == m_Tail)
                return default(T);

            m_CurrRead = mod(m_CurrRead + 1, m_Buffer.Length); ;
            return m_Buffer[m_CurrRead];
        }

        public T GetPrev()
        {
            if (m_CurrRead == -1 || m_CurrRead == m_Head)
                return default(T);

            m_CurrRead = mod(m_CurrRead - 1, m_Buffer.Length);
            return m_Buffer[m_CurrRead];
        }

        public RingBuffer<T> ShallowCopy()
        {
            RingBuffer<T> buf = (RingBuffer<T>)this.MemberwiseClone();
            buf.m_Buffer = (T[])this.m_Buffer.Clone();
            return buf;
        }

        private int mod(int x, int m) // x mod m works for both positive and negative x (unlike x % m).
        {
            return (x % m + m) % m;
        }

#if DEBUG
        public T[] Raw { get { return (T[])m_Buffer.Clone(); } } // For debugging only.
#endif

    }
}
