using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using TSMapEditor.CCEngine;

namespace TSMapEditor.Rendering
{
    public class ShapeImage
    {
        public ShapeImage(GraphicsDevice graphicsDevice, ShpFile shp, byte[] shpFileData, XNAPalette palette,
            bool subjectToLighting, bool remapable = false, PositionedTexture pngTexture = null)
        {
            shpFile = shp;
            this.shpFileData = shpFileData;
            Palette = palette;
            this.remapable = remapable;
            this.SubjectToLighting = subjectToLighting;
            this.graphicsDevice = graphicsDevice;

            if (pngTexture != null && !remapable)
            {
                IsPNG = true;
                Frames = new PositionedTexture[] { pngTexture };
                return;
            }

            Frames = new PositionedTexture[shp.FrameCount];
            if (remapable)
                RemapFrames = new PositionedTexture[Frames.Length];
        }

        public ShapeImage(GraphicsDevice graphicsDevice, ShpFile shp, byte[] shpFileData, XNAPalette palette, bool subjectToLighting, bool remapable, GraphicsPreparationClass graphicsPreparationClass, List<int> framesToInclude = null)
        {
            shpFile = shp;
            this.shpFileData = shpFileData;
            Palette = palette;
            this.remapable = remapable;
            this.SubjectToLighting = subjectToLighting;
            this.graphicsDevice = graphicsDevice;

            Frames = new PositionedTexture[shp.FrameCount];
            if (remapable)
                RemapFrames = new PositionedTexture[Frames.Length];

            RenderToGraphicsPreparationObject(graphicsPreparationClass, framesToInclude);
        }


        private XNAPalette Palette { get; }
        private ShpFile shpFile;
        private byte[] shpFileData;
        private bool remapable;
        private GraphicsDevice graphicsDevice;

        public bool SubjectToLighting { get; }

        public bool IsPNG { get; }

        private PositionedTexture[] Frames { get; set; }
        private PositionedTexture[] RemapFrames { get; set; }

        public int GetFrameCount() => Frames.Length;

        public void Dispose()
        {
            Array.ForEach(Frames, tx =>
            {
                if (tx != null)
                    tx.Texture.Dispose();
            });

            if (RemapFrames != null)
            {
                Array.ForEach(RemapFrames, tx =>
                {
                    if (tx != null)
                        tx.Texture.Dispose();
                });
            }
        }

        /// <summary>
        /// Gets a specific frame of this object image if it exists, otherwise returns null.
        /// If a valid frame has not been yet converted into a texture, first converts the frame into a texture.
        /// </summary>
        public PositionedTexture GetFrame(int index)
        {
            if (Frames == null || index < 0 || index >= Frames.Length)
                return null;

            if (Frames[index] != null)
                return Frames[index];

            GenerateTexturesForFrame_Paletted(index);
            // GenerateTexturesForFrame_RGBA(index);
            return Frames[index];
        }

        public bool HasRemapFrames() => RemapFrames != null;

        public PositionedTexture GetRemapFrame(int index)
        {
            if (index < 0 || index >= RemapFrames.Length)
                return null;

            return RemapFrames[index];
        }

        public Texture2D GetPaletteTexture()
        {
            return Palette?.GetTexture();
        }

        private void GetFrameInfoAndData(int frameIndex, out ShpFrameInfo frameInfo, out byte[] frameData)
        {
            frameInfo = shpFile.GetShpFrameInfo(frameIndex);
            frameData = shpFile.GetUncompressedFrameData(frameIndex, shpFileData);
        }

        public void RenderToGraphicsPreparationObject(GraphicsPreparationClass graphicsPreparationObject, List<int> framesToInclude)
        {
            for (int i = 0; i < Frames.Length; i++)
            {
                if (framesToInclude != null && !framesToInclude.Contains(i))
                    continue;

                GetFrameInfoAndData(i, out var frameInfo, out var frameData);

                if (frameInfo == null || frameData == null)
                    continue;

                var positionedTexture = new PositionedTexture(shpFile.Width, shpFile.Height, frameInfo.XOffset, frameInfo.YOffset, null, Rectangle.Empty);

                Point offset = graphicsPreparationObject.AddImage(frameInfo.Width, frameInfo.Height, frameData, positionedTexture);
                positionedTexture.SourceRectangle = new Rectangle(offset.X, offset.Y, frameInfo.Width, frameInfo.Height);
                Frames[i] = positionedTexture;

                if (remapable)
                {
                    byte[] remapColorArray = frameData.Select(b =>
                    {
                        if (b >= 0x10 && b <= 0x1F)
                        {
                            // This is a remap color
                            return (byte)b;
                        }

                        return (byte)0;
                    }).ToArray();

                    bool hasRemap = false;
                    for (int b = 0; b < remapColorArray.Length; b++)
                    {
                        if (remapColorArray[b] != 0)
                        {
                            hasRemap = true;
                            break;
                        }
                    }

                    if (hasRemap)
                    {
                        var remapTexture = new PositionedTexture(shpFile.Width, shpFile.Height, frameInfo.XOffset, frameInfo.YOffset, null, Rectangle.Empty);

                        offset = graphicsPreparationObject.AddImage(frameInfo.Width, frameInfo.Height, remapColorArray, remapTexture);
                        remapTexture.SourceRectangle = new Rectangle(offset.X, offset.Y, frameInfo.Width, frameInfo.Height);
                        RemapFrames[i] = remapTexture;
                    }
                }
            }
        }

        public void GenerateTexturesForFrame_RGBA(int index)
        {
            if (shpFile == null)
                return;

            GetFrameInfoAndData(index, out ShpFrameInfo frameInfo, out byte[] frameData);

            if (frameData == null)
                return;

            var texture = GetTextureForFrame_RGBA(index, frameInfo, frameData);
            Frames[index] = new PositionedTexture(shpFile.Width, shpFile.Height, frameInfo.XOffset, frameInfo.YOffset, texture, new Rectangle(0, 0, texture.Width, texture.Height));

            if (remapable)
            {
                var remapTexture = GetRemapTextureForFrame_RGBA(index, frameInfo, frameData);
                RemapFrames[index] = new PositionedTexture(shpFile.Width, shpFile.Height, frameInfo.XOffset, frameInfo.YOffset, remapTexture, new Rectangle(0, 0, remapTexture.Width, remapTexture.Height));
            }
        }

        public void GenerateTexturesForFrame_Paletted(int index)
        {
            if (shpFile == null)
                return;

            GetFrameInfoAndData(index, out ShpFrameInfo frameInfo, out byte[] frameData);

            if (frameData == null)
                return;

            var texture = GetTextureForFrame_Paletted(index, frameInfo, frameData);
            Frames[index] = new PositionedTexture(shpFile.Width, shpFile.Height, frameInfo.XOffset, frameInfo.YOffset, texture, new Rectangle(0, 0, texture.Width, texture.Height));

            if (remapable)
            {
                var remapTexture = GetRemapTextureForFrame_Paletted(index, frameInfo, frameData);
                RemapFrames[index] = new PositionedTexture(shpFile.Width, shpFile.Height, frameInfo.XOffset, frameInfo.YOffset, remapTexture, new Rectangle(0, 0, remapTexture.Width, remapTexture.Height));
            }
        }

        public Texture2D GetTextureForFrame_Paletted(int index, ShpFrameInfo frameInfo, byte[] frameData)
        {
            var texture = new Texture2D(graphicsDevice, frameInfo.Width, frameInfo.Height, false, SurfaceFormat.Alpha8);
            texture.SetData(frameData);
            return texture;
        }

        public Texture2D GetTextureForFrame_RGBA(int index, ShpFrameInfo frameInfo, byte[] frameData)
        {
            var texture = new Texture2D(graphicsDevice, frameInfo.Width, frameInfo.Height, false, SurfaceFormat.Color);
            Color[] colorArray = frameData.Select(b => b == 0 ? Color.Transparent : Palette.Data[b].ToXnaColor()).ToArray();
            texture.SetData<Color>(colorArray);

            return texture;
        }

        public Texture2D GetTextureForFrame_RGBA(int index)
        {
            if (shpFile == null)
                return null;

            GetFrameInfoAndData(index, out ShpFrameInfo frameInfo, out byte[] frameData);

            if (frameData == null)
                return null;

            return GetTextureForFrame_RGBA(index, frameInfo, frameData);
        }

        public Texture2D GetRemapTextureForFrame_Paletted(int index, ShpFrameInfo frameInfo, byte[] frameData)
        {
            byte[] remapColorArray = frameData.Select(b =>
            {
                if (b >= 0x10 && b <= 0x1F)
                {
                    // This is a remap color
                    return (byte)b;
                }

                return (byte)0;
            }).ToArray();

            var remapTexture = new Texture2D(graphicsDevice, frameInfo.Width, frameInfo.Height, false, SurfaceFormat.Alpha8);
            remapTexture.SetData(remapColorArray);
            return remapTexture;
        }

        public Texture2D GetRemapTextureForFrame_RGBA(int index, ShpFrameInfo frameInfo, byte[] frameData)
        {
            Color[] remapColorArray = frameData.Select(b =>
            {
                if (b >= 0x10 && b <= 0x1F)
                {
                    // This is a remap color, convert to grayscale
                    Color xnaColor = Palette.Data[b].ToXnaColor();
                    float value = Math.Max(xnaColor.R / 255.0f, Math.Max(xnaColor.G / 255.0f, xnaColor.B / 255.0f));

                    // Brighten it up a bit
                    value *= Constants.RemapBrightenFactor;
                    return new Color(value, value, value);
                }

                return Color.Transparent;
            }).ToArray();

            var remapTexture = new Texture2D(graphicsDevice, frameInfo.Width, frameInfo.Height, false, SurfaceFormat.Color);
            remapTexture.SetData<Color>(remapColorArray);
            return remapTexture;
        }

        public Texture2D GetRemapTextureForFrame_RGBA(int index)
        {
            GetFrameInfoAndData(index, out ShpFrameInfo frameInfo, out byte[] frameData);

            if (frameData == null)
                return null;

            return GetRemapTextureForFrame_RGBA(index, frameInfo, frameData);
        }
    }
}