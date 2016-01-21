using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace Masi.MsgPackRpc.Common
{
    public class ByteArraySegmentedStream : Stream
    {
        private readonly LinkedList<ArraySegment<byte>> _segments;

        private long _length = 0;
        private LinkedListNode<ArraySegment<byte>> _curSegment;
        private long _positionInSegment = 0;
        private long _position = 0;

        public ByteArraySegmentedStream()
        {
            _segments = new LinkedList<ArraySegment<byte>>();
        }

        public override bool CanRead
        {
            get { return true; }
        }

        public override bool CanSeek
        {
            get { return false; }
        }

        public override bool CanWrite
        {
            get { return false; }
        }

        public override long Length
        {
            get { return _length; }
        }

        public override long Position
        {
            get { return _position; }
            set
            {
                throw new NotSupportedException();

                /*if (value < 0)
                {
                    throw new ArgumentOutOfRangeException("value");
                }

                Seek(value, SeekOrigin.Begin);*/
            }
        }

        public void AddSegment(ArraySegment<byte> segment)
        {
            _segments.AddLast(segment);
            _length += segment.Count;

            if (_curSegment == null)
            {
                _curSegment = _segments.First;
            }
        }

        public void Clear()
        {
            _segments.Clear();
            _length = 0;
            _curSegment = null;
            _positionInSegment = 0;
            _position = 0;
        }

        private void DoAutoTruncate()
        {
            if (_segments.Count > 0)
            {
                if (_curSegment == _segments.First)
                {
                    _segments.RemoveFirst();

                    _position -= _curSegment.Value.Count;
                    _length -= _curSegment.Value.Count;
                }
            }
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            int remains = count;
            int curOffset = offset;
            int numRead = 0;

            while (remains > 0 && _curSegment != null)
            {
                ArraySegment<byte> segment = _curSegment.Value;
                int segmentCount = segment.Count;

                int curRead = remains;
                if (curRead > (segmentCount - _positionInSegment))
                {
                    curRead = (int)(segmentCount - _positionInSegment);
                }

                if (curRead == 0)
                {
                    var nextSegment = _curSegment.Next;
                    if (nextSegment == null)
                    {
                        // Stream is finnished
                        break;
                    }
                    else
                    {
                        DoAutoTruncate();

                        // Go to next segment
                        _positionInSegment = 0;
                        _curSegment = nextSegment;
                    }
                }
                else
                {
                    segment.CopyTo(_positionInSegment, buffer, curOffset, curRead);

                    remains -= curRead;
                    curOffset += curRead;
                    numRead += curRead;

                    _positionInSegment += curRead;
                    _position += curRead;

                    if (_positionInSegment == segmentCount)
                    {
                        var nextSegment = _curSegment.Next;
                        if (nextSegment == null)
                        {
                            // Stream is finnished
                            break;
                        }
                        else
                        {
                            DoAutoTruncate();

                            // Go to next segment
                            _positionInSegment = 0;
                            _curSegment = nextSegment;
                        }
                    }
                }
            }

            return numRead;
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            throw new NotSupportedException();

            // Seek no longer supported because of auto truncation
            // Seeking is not really needed
            /*switch (origin)
            {
                case SeekOrigin.Begin:
                    Seek(offset - _position);
                    break;
                case SeekOrigin.Current:
                    Seek(offset);
                    break;
                case SeekOrigin.End:
                    Seek(Length + offset - _position);
                    break;
                default:
                    throw new ArgumentException("Unknown origin value", "origin");
            }

            return _position;*/
        }

        /*
        private void Seek(long relativeOffset)
        {
            if (relativeOffset == -_position)
            {
                _curSegment = _segments.Count > 0 ? _segments.First : null;
                _positionInSegment = 0;
                _position = 0;
                return;
            }

            var newCurSegment = _curSegment;
            long newPosInSegment = _positionInSegment;
            long newPos = _position;

            if (relativeOffset < 0)
            {
                long remain = -relativeOffset;

                while (newCurSegment != null && remain > 0)
                {
                    ArraySegment<byte> segment = newCurSegment.Value;
                    int segmentCount = segment.Count;
                    
                    if (remain < segmentCount)
                    {
                        if (newPosInSegment == 0)
                        {
                            newPosInSegment = segmentCount - remain;
                        }
                        else
                        {
                            newPosInSegment -= remain;
                        }
                        newPos -= remain;
                        break;
                    }

                    newPosInSegment = 0;
                    newCurSegment = newCurSegment.Previous;
                    newPos -= segmentCount;

                    remain -= segmentCount;
                }
            }
            else
            {
                long remain = relativeOffset;

                while (newCurSegment != null && remain > 0)
                {
                    ArraySegment<byte> segment = newCurSegment.Value;
                    int segmentCount = segment.Count;

                    if (remain < segmentCount)
                    {
                        newPosInSegment += remain;
                        newPos += remain;
                        break;
                    }

                    newPosInSegment = 0;
                    newCurSegment = newCurSegment.Next;
                    newPos += segmentCount;

                    remain -= segmentCount;
                }
            }

            if (newPos < 0 || newPos > Length)
            {
                throw new ArgumentOutOfRangeException("relativeOffset");
            }

            _curSegment = newCurSegment;
            _positionInSegment = newPosInSegment;
            _position = newPos;
        }
        */

        public override void SetLength(long value)
        {
            throw new NotSupportedException();
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            throw new NotSupportedException();
        }

        public override void Flush()
        {
        }
    }
}
