using System;
using System.IO;
using Avalonia.Media.Imaging;
using System.Runtime.InteropServices;
using Avalonia;
using Avalonia.Platform;

namespace HOI4NavalModder
{
    public static class TgaDecoder
    {
        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        private struct TgaHeader
        {
            public byte IdLength;
            public byte ColorMapType;
            public byte ImageType;
            public ushort ColorMapFirstIndex;
            public ushort ColorMapLength;
            public byte ColorMapEntrySize;
            public ushort XOrigin;
            public ushort YOrigin;
            public ushort Width;
            public ushort Height;
            public byte PixelDepth;
            public byte ImageDescriptor;
        }

        public static Bitmap LoadFromFile(string filePath)
        {
            try
            {
                if (!File.Exists(filePath))
                {
                    Console.WriteLine($"ファイルが見つかりません: {filePath}");
                    return null;
                }

                byte[] fileData = File.ReadAllBytes(filePath);
                TgaHeader header = ReadHeader(fileData);

                if (header.ColorMapType != 0 || (header.ImageType != 2 && header.ImageType != 10))
                {
                    Console.WriteLine($"非対応のTGA形式です: ColorMapType={header.ColorMapType}, ImageType={header.ImageType}");
                    return null;
                }

                int dataOffset = 18 + header.IdLength;
                if (header.ColorMapType == 1)
                {
                    dataOffset += header.ColorMapLength * (header.ColorMapEntrySize / 8);
                }

                PixelFormat pixelFormat;
                int bytesPerPixel;

                switch (header.PixelDepth)
                {
                    case 24:
                        pixelFormat = PixelFormat.Rgb32;
                        bytesPerPixel = 3;
                        break;
                    case 32:
                        pixelFormat = PixelFormat.Rgba8888;
                        bytesPerPixel = 4;
                        break;
                    default:
                        Console.WriteLine($"非対応のピクセル深度です: {header.PixelDepth}");
                        return null;
                }

                int stride = header.Width * bytesPerPixel;
                byte[] imageData = new byte[stride * header.Height];

                if (header.ImageType == 2)
                {
                    for (int y = 0; y < header.Height; y++)
                    {
                        for (int x = 0; x < header.Width; x++)
                        {
                            int srcPos, destPos;
                            bool isTopLeft = (header.ImageDescriptor & 0x20) != 0;
                            if (isTopLeft)
                            {
                                srcPos = dataOffset + (y * stride) + (x * bytesPerPixel);
                            }
                            else
                            {
                                srcPos = dataOffset + ((header.Height - 1 - y) * stride) + (x * bytesPerPixel);
                            }

                            destPos = (y * stride) + (x * bytesPerPixel);

                            if (bytesPerPixel >= 3)
                            {
                                imageData[destPos] = fileData[srcPos + 2];
                                imageData[destPos + 1] = fileData[srcPos + 1];
                                imageData[destPos + 2] = fileData[srcPos];

                                if (bytesPerPixel == 4 && srcPos + 3 < fileData.Length)
                                {
                                    imageData[destPos + 3] = fileData[srcPos + 3];
                                }
                            }
                        }
                    }
                }
                else if (header.ImageType == 10)
                {
                    int pixelCount = 0;
                    int currentByte = dataOffset;

                    while (pixelCount < header.Width * header.Height)
                    {
                        byte packetHeader = fileData[currentByte++];
                        int packetType = packetHeader >> 7;
                        int pixelCountInPacket = (packetHeader & 0x7F) + 1;
                        pixelCountInPacket = Math.Min(pixelCountInPacket, header.Width * header.Height - pixelCount);

                        if (packetType == 1)
                        {
                            byte[] pixelData = new byte[bytesPerPixel];
                            for (int i = 0; i < bytesPerPixel; i++)
                            {
                                pixelData[i] = fileData[currentByte++];
                            }

                            byte temp = pixelData[0];
                            pixelData[0] = pixelData[2];
                            pixelData[2] = temp;

                            for (int i = 0; i < pixelCountInPacket; i++)
                            {
                                int y = pixelCount / header.Width;
                                int x = pixelCount % header.Width;
                                bool isTopLeft = (header.ImageDescriptor & 0x20) != 0;
                                int destY = isTopLeft ? y : header.Height - 1 - y;
                                int destPos = (destY * stride) + (x * bytesPerPixel);

                                for (int j = 0; j < bytesPerPixel; j++)
                                {
                                    imageData[destPos + j] = pixelData[j];
                                }

                                pixelCount++;
                            }
                        }
                        else
                        {
                            for (int i = 0; i < pixelCountInPacket; i++)
                            {
                                int y = pixelCount / header.Width;
                                int x = pixelCount % header.Width;
                                bool isTopLeft = (header.ImageDescriptor & 0x20) != 0;
                                int destY = isTopLeft ? y : header.Height - 1 - y;
                                int destPos = (destY * stride) + (x * bytesPerPixel);

                                imageData[destPos] = fileData[currentByte + 2];
                                imageData[destPos + 1] = fileData[currentByte + 1];
                                imageData[destPos + 2] = fileData[currentByte];

                                if (bytesPerPixel == 4)
                                {
                                    imageData[destPos + 3] = fileData[currentByte + 3];
                                }

                                currentByte += bytesPerPixel;
                                pixelCount++;
                            }
                        }
                    }
                }

                using var stream = new MemoryStream();
                var bitmap = new Bitmap(
                    pixelFormat,
                    AlphaFormat.Unpremul,
                    Marshal.UnsafeAddrOfPinnedArrayElement(imageData, 0),
                    new PixelSize(header.Width, header.Height),
                    new Vector(96, 96),
                    stride);

                return bitmap;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"TGAファイル読み込みエラー: {ex.Message}");
                return null;
            }
        }

        private static TgaHeader ReadHeader(byte[] data)
        {
            TgaHeader header = new TgaHeader();

            using (MemoryStream ms = new MemoryStream(data))
            using (BinaryReader br = new BinaryReader(ms))
            {
                header.IdLength = br.ReadByte();
                header.ColorMapType = br.ReadByte();
                header.ImageType = br.ReadByte();
                header.ColorMapFirstIndex = br.ReadUInt16();
                header.ColorMapLength = br.ReadUInt16();
                header.ColorMapEntrySize = br.ReadByte();
                header.XOrigin = br.ReadUInt16();
                header.YOrigin = br.ReadUInt16();
                header.Width = br.ReadUInt16();
                header.Height = br.ReadUInt16();
                header.PixelDepth = br.ReadByte();
                header.ImageDescriptor = br.ReadByte();
            }

            return header;
        }
    }
}