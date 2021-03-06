/*
 * Copyright (c) 2006-2016, openmetaverse.co
 * All rights reserved.
 *
 * Copyright (c) Contributors, http://opensimulator.org/
 * See CONTRIBUTORS.TXT for a full list of copyright holders.
 * 
 * - Redistribution and use in source and binary forms, with or without
 *   modification, are permitted provided that the following conditions are met:
 *
 * - Redistributions of source code must retain the above copyright notice, this
 *   list of conditions and the following disclaimer.
 * - Neither the name of the openmetaverse.co nor the names
 *   of its contributors may be used to endorse or promote products derived from
 *   this software without specific prior written permission.
 *
 * THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS"
 * AND ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE
 * IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE
 * ARE DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT OWNER OR CONTRIBUTORS BE
 * LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR
 * CONSEQUENTIAL DAMAGES (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF
 * SUBSTITUTE GOODS OR SERVICES; LOSS OF USE, DATA, OR PROFITS; OR BUSINESS
 * INTERRUPTION) HOWEVER CAUSED AND ON ANY THEORY OF LIABILITY, WHETHER IN
 * CONTRACT, STRICT LIABILITY, OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE)
 * ARISING IN ANY WAY OUT OF THE USE OF THIS SOFTWARE, EVEN IF ADVISED OF THE
 * POSSIBILITY OF SUCH DAMAGE.
 */

/*
    More compact Storage and manipulation of utf8 byte char strings, that .net core may have one day
    Not this is basicly a wrapper around a byte array that is shared
    Will get more things, in time.
    based on some ideas like FastStrings https://github.com/dhasenan/FastString
    Ubit Umarov (Leal Duarte) 2020
*/

using System;
using System.Text;
using System.Collections.Generic;

namespace OpenMetaverse
{
    public struct osUTF8
    {
        internal readonly byte[] m_data;
        internal int m_offset;
        internal int m_len;

        public static readonly osUTF8 Empty = new osUTF8(new byte[0],0,0);

        public osUTF8(byte[] source)
        {
            m_data = source;
            m_offset = 0;
            m_len = source.Length;
        }

        public osUTF8(byte[] source, int offset, int len)
        {
            m_data = source;
            m_offset = offset;
            m_len = len;
        }

        public osUTF8(osUTF8 source)
        {
            m_data = source.m_data;
            m_offset = source.m_offset;
            m_len = source.m_len;
        }

        public osUTF8(string source)
        {
            m_data = Utils.StringToBytesNoTerm(source);
            m_offset = 0;
            m_len = m_data.Length;
        }

        public byte this[int i]
        {
            get
            {
                if(i >= m_len)
                    i = m_len;
                i += m_offset;
                if( i < 0)
                    i= 0;
                else if(i >= m_data.Length)
                    i= m_data.Length -1;
                return m_data[i];
            }
        }

        public int Length
        {
            get { return m_len; }
        }

        public int Capacity
        {
            get { return m_data.Length; }
        }

        public void MoveStart(int of)
        {
            m_len -= of;
            m_offset += of;

            if(m_offset < 0)
            {
                m_len -= m_offset;
                m_offset = 0;
            }
            else if(m_offset >= m_data.Length)
            {
                m_offset = m_data.Length - 1;
                m_len = 0;
            }
            else if(m_offset + m_len > m_data.Length)
                m_len = m_data.Length - m_offset;
        }

        public void ResetToFull()
        {
            m_offset = 0;
            m_len = m_data.Length;
        }

        public bool IsNullOrEmpty { get { return m_len == 0; } }
        public bool IsEmpty { get { return m_len == 0; } }
        public unsafe bool IsNullOrWhitespace
        {
            get
            {
                if(m_len == 0)
                    return true;
                if (m_len < 8)
                {
                    for (int i = m_offset; i < m_offset + m_len; ++i)
                    {
                        if (m_data[i] != (byte)' ')
                            return false;
                    }
                    return true;
                }

                fixed (byte* a = &m_data[m_offset])
                {
                    for (int i = 0; i < m_len; ++i)
                    {
                        if (a[i] != (byte)' ')
                            return false;
                    }
                    return true;
                }
            }
        }

        public unsafe override int GetHashCode()
        {
            int hash = m_len;
            if (m_len < 8)
            {
                for (int i = m_offset; i < m_offset + m_len; ++i)
                {
                    hash += m_data[i];
                    hash <<= 3;
                    hash += hash >> 26;
                }
            }
            else
            {
                fixed (byte* a = &m_data[m_offset])
                {
                    for (int i = 0; i < m_len; ++i)
                    {
                        hash += a[i];
                        hash <<= 5;
                        hash += hash >> 26;
                    }
                }
            }
            return hash &0x7fffffff;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        public override string ToString()
        {
            return Encoding.UTF8.GetString(m_data, m_offset, m_len);
        }

        public override bool Equals(object obj)
        {
            if(obj == null)
                return false;
            
            if(obj is osUTF8)
                return Equals((osUTF8)obj);

            if(obj is string)
                return Equals((string)obj);

            return false;
        }

        public unsafe bool Equals(osUTF8 o)
        {
            if (m_len != o.m_len)
                return false;

            byte[] otherdata = o.m_data;

            if(m_len < 8)
            {
                for(int i = m_offset, j = o.m_offset; i < m_offset + m_len; ++i, ++j)
                {
                    if(m_data[i] != otherdata[j])
                        return false;
                }
                return true;
            }

            fixed(byte* a = &m_data[m_offset], b = &otherdata[o.m_offset])
            {
                for(int i = 0; i < m_len; ++i)
                {
                    if(a[i] != b[i])
                        return false;
                }
            }

            return true;
        }

        public bool Equals(string s)
        {
            osUTF8 o = new osUTF8(s);
            return Equals(o);
        }

        public osUTF8 Clone()
        {
            byte[] b = new byte[m_data.Length];
            Array.Copy(m_data, 0, b, 0, m_data.Length);
            return new osUTF8(b, m_offset, m_len);
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        public osUTF8 osUTF8SubString(int start)
        {
            return osUTF8SubString(start, m_len - start);
        }

        //returns a segment view of main buffer
        public osUTF8 osUTF8SubString(int start, int len)
        {
            if (start < 0)
                start = 0;
            if (len > m_len)
                len = m_len;

            start += m_offset; // things are relative to current

            if (start >= m_data.Length)
                return new osUTF8(m_data, m_data.Length - 1, 0);


            int last = start + len - 1;

            // cut at code points;
            if (start > 0 && (m_data[start] & 0x80) != 0)
            {
                do
                {
                    --last;
                }
                while (start > 0 && (m_data[start] & 0xc0) != 0xc0);
            }

            if (last > start && (m_data[last] & 0x80) != 0)
            {
                do
                {
                    --last;
                }
                while (last > start && (m_data[last] & 0xc0) != 0xc0);
            }

            return new osUTF8(m_data, start, last - start + 1);
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        public void osUTF8SubStringSelf(int start)
        {
            osUTF8SubStringSelf(start, m_len - start);
        }

        //returns a segment view of main buffer
        public void osUTF8SubStringSelf(int start, int len)
        {
            if (m_len == 0)
                return;

            if (start < 0)
                start = 0;
            else if(start >= m_len)
            {
                m_offset += m_len - 1;
                m_len = 0;
                return;
            }

            if (len == 0)
            {
                m_len = 0;
                return;
            }

            if (len > m_len)
                len = m_len;

            start += m_offset; // things are relative to current

            if (start >= m_data.Length)
            {
                m_offset = m_data.Length - 1;
                m_len = 0;
                return;
            }

            int last = start + len - 1;
            // cut at code points;
            if (start > 0 && (m_data[start] & 0x80) != 0)
            {
                do
                {
                    --last;
                }
                while (start > 0 && (m_data[start] & 0xc0) != 0xc0);
            }

            if (last > start && (m_data[last] & 0x80) != 0)
            {
                do
                {
                    --last;
                }
                while (last > start && (m_data[last] & 0xc0) != 0xc0);
            }

            m_offset = start;
            m_len = last - start + 1;
        }

        public osUTF8 Extract()
        {
            byte[] b = new byte[m_len];
            Array.Copy(m_data, m_offset, b, 0, m_len);
            return new osUTF8(b, 0, m_len);
        }

        public osUTF8 Concat(osUTF8 other)
        {
            byte[] b = new byte[m_len + other.m_len];
            Array.Copy(m_data, m_offset, b, 0, m_len);
            Array.Copy(other.m_data, other.m_offset, b, m_len, other.m_len);
            return new osUTF8(b, 0, m_len + other.m_len);
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        public string SubString(int start)
        {
            return SubString(start, m_len - start);
        }

        //returns a segment view of main buffer
        public string SubString(int start, int len)
        {
             osUTF8 res = osUTF8SubString(start, len);
             return res.ToString();
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        private bool checkAny(byte b, byte[] bytes)
        {
            for (int i = 0; i < bytes.Length; ++i)
            {
                if (b == bytes[i])
                    return true;
            }
            return false;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        private bool checkAny(byte b, char[] chars)
        {
            for (int i = 0; i < chars.Length; ++i)
            {
                if (b == (byte)chars[i])
                    return true;
            }
            return false;
        }

        // inplace remove white spaces at start
        public void SelfTrimStart()
        {
            if (m_len == 0)
                return;
            int last = m_offset + m_len - 1;
            while (m_data[m_offset] == (byte)' ')
            {
                ++m_offset;
                --m_len;
                if (m_offset == last)
                    break;
            }
        }

        public void SelfTrimStart(byte b)
        {
            if (m_len == 0)
                return;
            int last = m_offset + m_len - 1;
            while (m_data[m_offset] == b)
            {
                ++m_offset;
                --m_len;
                if (m_offset == last)
                    break;
            }
        }

        public void SelfTrimStart(byte[] b)
        {
            if (m_len == 0)
                return;
            int last = m_offset + m_len - 1;
            while (checkAny(m_data[m_offset], b))
            {
                ++m_offset;
                --m_len;
                if (m_offset == last)
                    break;
            }
        }

        public void SelfTrimStart(char[] b)
        {
            if (m_len == 0)
                return;
            int last = m_offset + m_len - 1;
            while (checkAny(m_data[m_offset], b))
            {
                ++m_offset;
                --m_len;
                if (m_offset == last)
                    break;
            }
        }

        public void SelfTrimEnd()
        {
            if (m_len == 0)
                return;
            int last = m_offset + m_len - 1;
            while (m_data[last] == (byte)' ')
            {
                --last;
                --m_len;
                if (last == m_offset)
                    break;
            }
        }

        public void SelfTrimEnd(byte b)
        {
            if (m_len == 0)
                return;
            int last = m_offset + m_len - 1;
            while (m_data[last] == b)
            {
                --last;
                --m_len;
                if (last == m_offset)
                    break;
            }
        }

        public void SelfTrimEnd(byte[] b)
        {
            if (m_len == 0)
                return;
            int last = m_offset + m_len - 1;
            while (checkAny(m_data[last], b))
            {
                --last;
                --m_len;
                if (last == m_offset)
                    break;
            }
        }

        public void SelfTrimEnd(char[] b)
        {
            if (m_len == 0)
                return;
            int last = m_offset + m_len - 1;
            while (checkAny(m_data[last], b))
            {
                --last;
                --m_len;
                if (last == m_offset)
                    break;
            }
        }

        public void SelfTrim()
        {
            SelfTrimStart();
            SelfTrimEnd();
        }

        public void SelfTrim(byte b)
        {
            SelfTrimStart(b);
            SelfTrimEnd(b);
        }

        public void SelfTrim(byte[] v)
        {
            SelfTrimStart(v);
            SelfTrimEnd(v);
        }

        public void SelfTrim(char[] v)
        {
            SelfTrimStart(v);
            SelfTrimEnd(v);
        }

        public osUTF8 TrimStart()
        {
            osUTF8 ret = new osUTF8(this);
            ret.SelfTrimStart();
            return ret;
        }

        public osUTF8 TrimEnd()
        {
            osUTF8 ret = new osUTF8(this);
            ret.SelfTrimEnd();
            return ret;
        }

        public osUTF8 Trim()
        {
            osUTF8 ret = new osUTF8(this);
            ret.SelfTrimStart();
            ret.SelfTrimEnd();
            return ret;
        }

        public osUTF8 TrimStart(byte b)
        {
            osUTF8 ret = new osUTF8(this);
            ret.SelfTrimStart(b);
            return ret;
        }

        public osUTF8 TrimEnd(byte b)
        {
            osUTF8 ret = new osUTF8(this);
            ret.SelfTrimEnd(b);
            return ret;
        }

        public osUTF8 Trim(byte b)
        {
            osUTF8 ret = new osUTF8(this);
            ret.SelfTrimStart(b);
            ret.SelfTrimEnd(b);
            return ret;
        }

        public osUTF8 TrimStart(byte[] v)
        {
            osUTF8 ret = new osUTF8(this);
            ret.SelfTrimStart(v);
            return ret;
        }

        public osUTF8 TrimEnd(byte[] v)
        {
            osUTF8 ret = new osUTF8(this);
            ret.SelfTrimEnd(v);
            return ret;
        }

        public osUTF8 Trim(byte[] v)
        {
            osUTF8 ret = new osUTF8(this);
            ret.SelfTrimStart(v);
            ret.SelfTrimEnd(v);
            return ret;
        }

        public osUTF8 TrimStart(char[] v)
        {
            osUTF8 ret = new osUTF8(this);
            ret.SelfTrimStart(v);
            return ret;
        }

        public osUTF8 TrimEnd(char[] v)
        {
            osUTF8 ret = new osUTF8(this);
            ret.SelfTrimEnd(v);
            return ret;
        }

        public osUTF8 Trim(char[] v)
        {
            osUTF8 ret = new osUTF8(this);
            ret.SelfTrimStart(v);
            ret.SelfTrimEnd(v);
            return ret;
        }

        public unsafe bool StartsWith(osUTF8 other)
        {
            int otherlen = other.m_len;
            if (otherlen > m_len)
                return false;

            fixed(byte* a = &m_data[m_offset], b = &other.m_data[other.m_offset])
            {
                for(int i = 0; i < otherlen; ++i)
                {
                    if(a[i] != b[i])
                        return false;
                }
            }
            return true;
        }

        public bool StartsWith(string s)
        {
            osUTF8 other = new osUTF8(s); // yeack
            return StartsWith(other);
        }

        public bool StartsWith(byte b)
        {
            return m_data[m_offset] == b;
        }

        public bool StartsWith(char b)
        {
            return m_data[m_offset] == (byte)b;
        }

        public bool EndsWith(byte b)
        {
            return m_data[m_offset + m_len - 1] == b;
        }

        public bool EndsWith(char b)
        {
            return m_data[m_offset + m_len - 1] == (byte)b;
        }

        public unsafe bool EndsWith(osUTF8 other)
        {
            int otherlen = other.m_len;
            if (otherlen > m_len)
                return false;

            fixed (byte* a = &m_data[m_offset], b = &other.m_data[other.m_offset])
            {
                for (int i = otherlen - 1, j = m_len - 1 ; i >= 0; --i, --j)
                {
                    if (a[j] != b[i])
                        return false;
                }
                return true;
            }
        }

        public bool EndsWith(string s)
        {
            osUTF8 other = new osUTF8(s); // yeack
            return EndsWith(other);
        }

        public unsafe int IndexOf(byte b)
        {
            if(m_len > 8)
            {
                fixed(byte* a = &m_data[ m_offset])
                {
                    for(int i = 0; i < m_len; ++i)
                    {
                        if (a[i] == b)
                            return i;
                    }
                    return -1;
                }
            }

            for (int i = m_offset; i < m_offset + m_len; ++i)
            {
                if (m_data[i] == b)
                    return i - m_offset;
            }
            return -1;
        }

        public int IndexOf(char b)
        {
            if (b < 0x80)
                return IndexOf((byte)b);
            string s = new string(new char[]{ b});
            return IndexOf(s);
        }

        public unsafe int IndexOf(osUTF8 other)
        {
            int otherlen = other.m_len;
            if (otherlen > m_len || otherlen == 0)
                return -1;

            byte[] otherdata = other.m_data;
            fixed(byte* a = &m_data[m_offset], b = &otherdata[other.m_offset])
            {
                for (int i = 0; i < m_len - otherlen; ++i)
                {
                    int k = 0;
                    for (int j = i; k < otherlen; ++k, ++j)
                    {
                        if (a[j] != b[k])
                            return -1;
                    }
                    if (k == otherlen)
                        return i;
                }
                return -1;
            }
        }

        public int IndexOf(string s)
        {
            if(string.IsNullOrEmpty(s))
                return -1;
            osUTF8 o = new osUTF8(s);
            return IndexOf(o);
        }

        public unsafe int IndexOfAny(byte[] b)
        {
            if(m_len < 8)
            {
                for (int i = m_offset; i < m_offset + m_len; ++i)
                {
                    if (checkAny(m_data[i], b))
                        return i - m_offset;
                }
                return -1;
            }
            fixed (byte* a = &m_data[m_offset])
            {
                for (int i = 0; i < m_len; ++i)
                {
                    if (checkAny(a[i], b))
                        return i;
                }
                return -1;
            }
        }

        public unsafe int IndexOfAny(char[] b)
        {
            if(m_len < 8)
            {
                for (int i = m_offset; i < m_offset + m_len; ++i)
                {
                    if (checkAny(m_data[i], b))
                        return i - m_offset;
                }
                return -1;
            }
            fixed (byte* a = &m_data[m_offset])
            {
                for (int i = 0; i < m_len; ++i)
                {
                    if (checkAny(a[i], b))
                        return i;
                }
                return -1;
            }
        }

        public bool Contains(osUTF8 other)
        {
            return IndexOf(other) > 0;
        }

        public bool Contains(string s)
        {
            return IndexOf(s) > 0;
        }

        public osUTF8[] Split(byte b, bool ignoreEmpty = true)
        {
            if (m_len == 0)
            {
                return new osUTF8[]{ this };
            }

            bool incEmpty = !ignoreEmpty;
            osUTF8 tmp = new osUTF8(this);
            List<osUTF8> lst = new List<osUTF8>();

            int indx;
            while ((indx = tmp.IndexOf(b)) >= 0)
            {
                osUTF8 o = tmp.osUTF8SubString(0, indx);
                if(incEmpty)
                    lst.Add(o);
                else if (o.m_len > 0)
                    lst.Add(o);
                tmp.MoveStart(indx + 1);
            }

            if (tmp.m_len > 0)
                lst.Add(tmp);
            return lst.ToArray();
        }

        public osUTF8[] Split(byte[] b, bool ignoreEmpty = true)
        {
            if (m_len == 0)
            {
                return new osUTF8[] { this };
            }

            bool incEmpty = !ignoreEmpty;
            osUTF8 tmp = new osUTF8(this);
            List<osUTF8> lst = new List<osUTF8>();

            int indx;
            while ((indx = tmp.IndexOfAny(b)) >= 0)
            {
                osUTF8 o = tmp.osUTF8SubString(0, indx);
                if (incEmpty)
                    lst.Add(o);
                else if (o.m_len > 0)
                    lst.Add(o);
                tmp.MoveStart(indx + 1);
            }

            if (tmp.m_len > 0)
                lst.Add(tmp);
            return lst.ToArray();
        }

        public osUTF8[] Split(char[] b, bool ignoreEmpty = true)
        {
            if (m_len == 0)
            {
                return new osUTF8[] { this };
            }

            bool incEmpty = !ignoreEmpty;
            osUTF8 tmp = new osUTF8(this);
            List<osUTF8> lst = new List<osUTF8>();

            int indx;
            while ((indx = tmp.IndexOfAny(b)) >= 0)
            {
                osUTF8 o = tmp.osUTF8SubString(0, indx);
                if (incEmpty)
                    lst.Add(o);
                else if (o.m_len > 0)
                    lst.Add(o);
                tmp.MoveStart(indx + 1);
            }

            if (tmp.m_len > 0)
                lst.Add(tmp);
            return lst.ToArray();
        }

        public osUTF8[] Split(char b, bool ignoreEmpty = true)
        {
            if(b < 0x80)
                return Split((byte)b, ignoreEmpty);

            return new osUTF8[0];
        }

        public unsafe bool ReadLine(out osUTF8 line)
        {
            if (m_len == 0)
            {
                line = new osUTF8(new byte[0], 0, 0);
                return false;
            }

            int lineend = -1;
            byte b = 0;
            if(m_len < 8)
            {
                for (int i = m_offset; i < m_offset + m_len; ++i)
                {
                    b = m_data[i];
                    if (b == (byte)'\r' || b == (byte)'\n')
                    {
                        if (i > 0 && m_data[i - 1] == (byte)'\\')
                            continue;
                        lineend = i;
                        break;
                    }
                }
            }
            else
            {
                fixed (byte* a = &m_data[m_offset])
                {
                    for (int i = 0; i < m_len; ++i)
                    {
                        b = a[i];
                        if (b == (byte)'\r' || b == (byte)'\n')
                        {
                            if (i > 0 && a[i - 1] == (byte)'\\')
                                continue;
                            lineend = i + m_offset;
                            break;
                        }
                    }
                }
            }

            line = new osUTF8(m_data, m_offset, m_len);
            if (lineend < 0)
            {
                m_offset = m_offset + m_len - 1;
                m_len = 0;
                return false;
            }

            int linelen = lineend - m_offset;
            line.m_len = linelen;

            ++linelen;
            if (linelen >= m_len)
            {
                m_offset = m_offset + m_len - 1;
                m_len = 0;
                return true;
            }

            m_offset += linelen;
            m_len -= linelen;

            if (m_len <= 0)
            {
                m_len = 0;
                return true;
            }

            if (b == (byte)'\r')
            {
                if (m_data[m_offset] == (byte)'\n')
                {
                    ++m_offset;
                    --m_len;
                }
            }

            if (m_len <= 0)
                m_len = 0;

            return true;
        }

        public unsafe bool SkipLine()
        {
            if (m_len == 0)
                return false;

            int lineend = -1;
            byte b = 0;
            if(m_len < 8)
            {
                for (int i = m_offset; i < m_offset + m_len; ++i)
                {
                    b = m_data[i];
                    if (b == (byte)'\r' || b == (byte)'\n')
                    {
                        if (i > 0 && m_data[i - 1] == (byte)'\\')
                            continue;
                        lineend = i;
                        break;
                    }
                }
            }
            else
            {
                fixed (byte* a = &m_data[m_offset])
                {
                    for (int i = 0; i < m_len; ++i)
                    {
                        b = a[i];
                        if (b == (byte)'\r' || b == (byte)'\n')
                        {
                            if (i > 0 && a[i - 1] == (byte)'\\')
                                continue;
                            lineend = i + m_offset;
                            break;
                        }
                    }
                }
            }

            if (lineend < 0)
            {
                m_offset = m_offset + m_len - 1;
                m_len = 0;
                return true;
            }

            int linelen = lineend - m_offset;

            ++linelen;
            if (linelen >= m_len)
            {
                m_offset = m_offset + m_len - 1;
                m_len = 0;
                return true;
            }

            m_offset += linelen;
            m_len -= linelen;
            if (m_len <= 0)
            {
                m_len = 0;
                return true;
            }

            if (b == (byte)'\r')
            {
                if (m_data[m_offset] == (byte)'\n')
                {
                    ++m_offset;
                    --m_len;
                }
            }

            if (m_len <= 0)
                m_len = 0;

            return true;
        }

        public static bool TryParseInt(osUTF8 t, out int res)
        {
            res = 0;

            t.SelfTrim();
            int len = t.m_len;
            if (len == 0)
                return false;

            byte[] data = t.m_data;

            int start = t.m_offset;
            len += start;

            bool neg = false;
            if (data[start] == (byte)'-')
            {
                neg = true;
                ++start;
            }
            else if (data[start] == (byte)'+')
                ++start;

            int b;
            try
            {
                while (start < len)
                {
                    b = data[start];
                    b -= (byte)'0';
                    if( b < 0 || b > 9)
                        break;

                    res *= 10;
                    res += b;
                    ++start;
                }
                if(neg)
                    res = -res;
                return true;
            }
            catch { }
            return false;
        }

        public static bool TryParseUUID(osUTF8 inp, out UUID res, bool dashs = true)
        {
            res = UUID.Zero;
            osUTF8 t = new osUTF8(inp);

            t.SelfTrim();
            int len = t.m_len;
            if (len == 0)
                return false;

            if (dashs)
            {
                if (len < 36)
                    return false;
            }
            else
            {
                if (len < 32)
                    return false;
            }

            byte[] data = t.m_data;
            int dataoffset = t.m_offset;

            int _a = 0;
            if (!Utils.TryHexToInt(data, dataoffset, 8, out _a))
                return false;
            dataoffset += 8;

            if (dashs)
            {
                if (data[dataoffset] != (byte)'-')
                    return false;
                ++dataoffset;
            }

            int n;
            if (!Utils.TryHexToInt(data, dataoffset, 4, out n))
                return false;
            short _b = (short)n;
            dataoffset += 4;

            if (dashs)
            {
                if (data[dataoffset] != (byte)'-')
                    return false;
                ++dataoffset;
            }

            if (!Utils.TryHexToInt(data, dataoffset, 4, out n))
                return false;
            short _c = (short)n;
            dataoffset += 4;

            if (dashs)
            {
                if (data[dataoffset] != (byte)'-')
                    return false;
                ++dataoffset;
            }

            if (!Utils.TryHexToInt(data, dataoffset, 4, out n))
                return false;

            byte _d = (byte)(n >> 8);
            byte _e = (byte)n;
            dataoffset += 4;

            if (dashs)
            {
                if (data[dataoffset] != (byte)'-')
                    return false;
                ++dataoffset;
            }

            if (!Utils.TryHexToInt(data, dataoffset, 8, out n))
                return false;
            byte _f = (byte)(n >> 24);
            byte _g = (byte)(n >> 16);
            byte _h = (byte)(n >> 8);
            byte _i = (byte)n;
            dataoffset += 8;

            if (!Utils.TryHexToInt(data, dataoffset, 4, out n))
                return false;
            byte _j = (byte)(n>>8);
            byte _k = (byte)n;

            Guid g = new Guid(_a,_b,_c,_d,_e,_f,_g,_h,_i,_j,_k);
            res = new UUID(g);
            return true;
        }
    }
}
