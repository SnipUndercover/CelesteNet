﻿using Celeste.Mod.CelesteNet.DataTypes;
using Microsoft.Xna.Framework;
using Monocle;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.ObjectFactories;

namespace Celeste.Mod.CelesteNet {
    public class CelesteNetBinaryBlobPartWriter : CelesteNetBinaryWriter {

        public readonly DataInternalBlob Blob;
        public readonly MemoryStream Stream;

        public int LastSplitPosition;

        public CelesteNetBinaryBlobPartWriter(DataContext ctx, DataInternalBlob blob, MemoryStream output)
            : base(ctx, output) {
            Blob = blob;
            Stream = output;
        }

        public CelesteNetBinaryBlobPartWriter(DataContext ctx, DataInternalBlob blob, MemoryStream output, Encoding encoding)
            : base(ctx, output, encoding) {
            Blob = blob;
            Stream = output;
        }

        public CelesteNetBinaryBlobPartWriter(DataContext ctx, DataInternalBlob blob, MemoryStream output, Encoding encoding, bool leaveOpen)
            : base(ctx, output, encoding, leaveOpen) {
            Blob = blob;
            Stream = output;
        }

        public void SplitBytes() {
            Flush();

            int pos = (int) Stream.Position;
            if (LastSplitPosition == pos)
                return;

            DataInternalBlob.Part part = Blob.PartNext();
            part.Bytes = Stream.GetBuffer();
            part.BytesIndex = LastSplitPosition;
            part.BytesCount = pos - LastSplitPosition;
            LastSplitPosition = pos;
        }

        public override void WriteSizeDummy(byte size) {
            SplitBytes();
            Blob.PartNext().SizeDummy = size;
        }

        public override void UpdateSizeDummy() {
            SplitBytes();
            Blob.PartNext().SizeDummy = 0xFF;
        }

        public override void WriteNetMappedString(string? text) {
            SplitBytes();
            Blob.PartNext().String = text ?? "";
        }

    }
}
