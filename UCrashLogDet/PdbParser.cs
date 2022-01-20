using System;
using System.Collections.Generic;
using System.IO;

namespace UCrashLogDet
{
    //awful pdb parser from who knows where
    public class PdbFile
    {
        private int blockSize;
        private int fpmBlockIdx;
        private int blockCount;
        private int bytesInStreamBlock;
        private int blockMapIdx;
        private int streamCount;
        private int[] streamLengths;
        private int[][] streams;

        public Dictionary<long, PdbFunctionData> funcLookup = new Dictionary<long, PdbFunctionData>();
        public List<long> funcAddrs = new List<long>();
        public List<PdbFunctionData> funcDatas = new List<PdbFunctionData>();
        public PdbFile(BinaryReader br)
        {
            br.BaseStream.Position = 0x20;
            blockSize = br.ReadInt32();
            fpmBlockIdx = br.ReadInt32();
            blockCount = br.ReadInt32();
            bytesInStreamBlock = br.ReadInt32();
            br.ReadInt32();
            blockMapIdx = br.ReadInt32();
            br.BaseStream.Position = blockMapIdx * blockSize;
            br.BaseStream.Position = br.ReadInt32() * blockSize;
            //assuming all blocks are in order
            streamCount = br.ReadInt32();
            streamLengths = new int[streamCount];
            streams = new int[streamCount][];
            for (int i = 0; i < streamCount; i++)
            {
                streamLengths[i] = br.ReadInt32();
            }
            for (int i = 0; i < streamCount; i++)
            {
                int streamBlockCount = CeilDiv(streamLengths[i], blockSize);
                streams[i] = new int[streamBlockCount];
                for (int j = 0; j < streamBlockCount; j++)
                {
                    streams[i][j] = br.ReadInt32();
                }
            }
            int symbolRecIdx = streamCount - 3; //where does this come from?
            //this usually isn't in order so we build the blocks here
            int[] symbolRecs = streams[symbolRecIdx];
            int symbolBlockCount = streamLengths[symbolRecIdx] / blockSize;
            byte[] symbolBuf = new byte[streamLengths[symbolRecIdx]];
            for (int i = 0; i < symbolBlockCount; i++)
            {
                br.BaseStream.Position = symbolRecs[i] * blockSize;
                byte[] blockBytes = br.ReadBytes(blockSize);
                Buffer.BlockCopy(blockBytes, 0, symbolBuf, i * blockSize, Math.Min(blockSize, symbolBuf.Length - (i * blockSize)));
            }
            using (BinaryReader brs = new BinaryReader(new MemoryStream(symbolBuf)))
            {
                while (brs.BaseStream.Position < brs.BaseStream.Length)
                {
                    ushort size = brs.ReadUInt16();
                    if (size == 0)
                    {
                        break;
                    }
                    ushort id = brs.ReadUInt16();
                    if (id == 0x110e) //S_PUB32
                    {
                        int flags = brs.ReadInt32();
                        long localAddr = brs.ReadInt32();
                        ushort seg = brs.ReadUInt16();
                        string name = brs.ReadCString();
                        brs.Align4();
                        PdbFunctionData fd = new PdbFunctionData()
                        {
                            flags = flags,
                            localAddr = localAddr,
                            seg = seg,
                            name = name,
                            aliases = new List<string>()
                        };
                        long funcLookupLocalAddr = ((long)seg << 32) | localAddr;
                        if (!funcLookup.ContainsKey(funcLookupLocalAddr))
                        {
                            funcAddrs.Add(localAddr);
                            funcLookup.Add(funcLookupLocalAddr, fd);
                            funcDatas.Add(fd);
                        }
                        else
                        {
                            funcLookup[funcLookupLocalAddr].aliases.Add(name);
                        }
                    }
                    else
                    {
                        brs.BaseStream.Position += size - 2;
                    }
                }
            }
            funcAddrs.Sort();
        }

        private int CeilDiv(int a, int b)
        {
            return (a / b) + (a % b == 0 ? 0 : 1);
        }
    }

    public class PdbFunctionData
    {
        public int flags;
        public long localAddr;
        public ushort seg;
        public string name;
        public List<string> aliases; //wut
    }
}
