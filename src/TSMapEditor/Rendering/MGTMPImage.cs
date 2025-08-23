﻿using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Rampastring.Tools;
using System;
using TSMapEditor.CCEngine;

namespace TSMapEditor.Rendering
{
    /// <summary>
    /// Interface for a single cell of a tile; sub-tile of a full TMP.
    /// </summary>
    public interface ISubTileImage
    {
        TmpImage TmpImage { get; }
    }

    /// <summary>
    /// A MonoGame-drawable TMP image.
    /// Contains graphics and information for a single cell (sub-tile of a full TMP).
    /// </summary>
    public class MGTMPImage : ISubTileImage
    {
        /// <summary>
        /// Creates a new MonoGame TMP image and copies its color data to a working buffer for a sprite sheet.
        /// </summary>
        /// <param name="tmpImage">The single-cell TMP image for this MonoGame TMP image instance.</param>
        /// <param name="bufferWidth">The width of the working buffer.</param>
        /// <param name="megaTextureData">The working buffer.</param>
        /// <param name="megaTextureX">The X coordinate where this tile's image should be copied into the buffer.</param>
        /// <param name="megaTextureY">The Y coordinate where this tile's image should be copied into the buffer.</param>
        /// <param name="palette">The palette to use for this image.</param>
        /// <param name="tileSetId">Index of the tile set of this image within all TileSets. Only tracked for convenience.</param>
        public MGTMPImage(TmpImage tmpImage, int bufferWidth, byte[] megaTextureData, int megaTextureX, int megaTextureY, XNAPalette palette, int tileSetId)
        {
            if (tmpImage != null)
            {
                TmpImage = tmpImage;
                Palette = palette;
                RenderToBuffer(bufferWidth, megaTextureData, megaTextureX, megaTextureY, tmpImage);
                SourceRectangle = new Rectangle(megaTextureX, megaTextureY, Constants.CellSizeX, Constants.CellSizeY);

                if (tmpImage.ExtraGraphicsColorData != null && tmpImage.ExtraGraphicsColorData.Length > 0)
                {
                    RenderExtraDataToBuffer(bufferWidth, megaTextureData, megaTextureX + Constants.CellSizeX, megaTextureY, TmpImage);
                    ExtraSourceRectangle = new Rectangle(megaTextureX + Constants.CellSizeX, megaTextureY, (int)tmpImage.ExtraWidth, (int)tmpImage.ExtraHeight);
                }
            }

            TileSetId = tileSetId;
        }

        /// <summary>
        /// The mega-texture where this sub-tile's texture is stored.
        /// </summary>
        public Texture2D Texture { get; set; }
        public Rectangle SourceRectangle { get; }
        public Rectangle ExtraSourceRectangle { get; }

        public int TileSetId { get; }
        public TmpImage TmpImage { get; private set; }
        private XNAPalette Palette { get; set; }

        private void RenderToBuffer(int bigtexWidth, byte[] colorData, int bigtexx, int bigtexy, TmpImage image)
        {
            int tmpPixelIndex = 0;
            int w = 4;
            for (int i = 0; i < Constants.CellSizeY; i++)
            {
                int xPos = Constants.CellSizeY - (w / 2);
                for (int x = 0; x < w; x++)
                {
                    if (image.ColorData[tmpPixelIndex] > 0)
                    {
                        colorData[(bigtexy + i) * bigtexWidth + xPos + bigtexx] = image.ColorData[tmpPixelIndex];
                    }

                    xPos++;
                    tmpPixelIndex++;
                }

                if (i < (Constants.CellSizeY / 2) - 1)
                    w += 4;
                else
                    w -= 4;
            }
        }

        private void RenderExtraDataToBuffer(int bigtexWidth, byte[] colorData, int bigtexx, int bigtexy, TmpImage image)
        {
            int width = (int)image.ExtraWidth;
            int height = (int)image.ExtraHeight;

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    int tmpExtraPixelIndex = (y * width) + x;

                    if (image.ExtraGraphicsColorData[tmpExtraPixelIndex] > 0)
                    {
                        colorData[(bigtexy + y) * bigtexWidth + bigtexx + x] = image.ExtraGraphicsColorData[tmpExtraPixelIndex];
                    }
                }
            }
        }

        private Texture2D TextureFromTmpImage_Paletted(GraphicsDevice graphicsDevice, TmpImage image)
        {
            Texture2D texture = new Texture2D(graphicsDevice, Constants.CellSizeX, Constants.CellSizeY, false, SurfaceFormat.Alpha8);
            byte[] colorData = new byte[Constants.CellSizeX * Constants.CellSizeY];

            int tmpPixelIndex = 0;
            int w = 4;
            for (int i = 0; i < Constants.CellSizeY; i++)
            {
                int xPos = Constants.CellSizeY - (w / 2);
                for (int x = 0; x < w; x++)
                {
                    if (image.ColorData[tmpPixelIndex] > 0)
                    {
                        colorData[i * Constants.CellSizeX + xPos] = image.ColorData[tmpPixelIndex];
                    }

                    xPos++;
                    tmpPixelIndex++;
                }

                if (i < (Constants.CellSizeY / 2) - 1)
                    w += 4;
                else
                    w -= 4;
            }

            texture.SetData(colorData);
            return texture;
        }

        private Texture2D TextureFromExtraTmpData_Paletted(GraphicsDevice graphicsDevice, TmpImage image)
        {
            int width = (int)image.ExtraWidth;
            int height = (int)image.ExtraHeight;

            var texture = new Texture2D(graphicsDevice, width, height, false, SurfaceFormat.Alpha8);
            byte[] colorData = new byte[width * height];

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    int index = (y * width) + x;

                    if (image.ExtraGraphicsColorData[index] > 0)
                    {
                        colorData[index] = image.ExtraGraphicsColorData[index];
                    }
                }
            }

            texture.SetData(colorData);
            return texture;
        }

        
        public Texture2D TextureFromTmpImage_RGBA(GraphicsDevice graphicsDevice)
        {
            Texture2D texture = new Texture2D(graphicsDevice, Constants.CellSizeX, Constants.CellSizeY, false, SurfaceFormat.Color);
            Color[] colorData = new Color[Constants.CellSizeX * Constants.CellSizeY];
            for (int i = 0; i < colorData.Length; i++)
            {
                colorData[i] = Color.Transparent;
            }

            int tmpPixelIndex = 0;
            int w = 4;
            for (int i = 0; i < Constants.CellSizeY; i++)
            {
                int xPos = Constants.CellSizeY - (w / 2);
                for (int x = 0; x < w; x++)
                {
                    if (TmpImage.ColorData[tmpPixelIndex] > 0)
                    {
                        colorData[i * Constants.CellSizeX + xPos] = Palette.Data[TmpImage.ColorData[tmpPixelIndex]].ToXnaColor();
                    }
                    
                    xPos++;
                    tmpPixelIndex++;
                }

                if (i < (Constants.CellSizeY / 2) - 1)
                    w += 4;
                else
                    w -= 4;
            }

            texture.SetData(colorData);
            return texture;
        }

        private Texture2D TextureFromExtraTmpData_RGBA(GraphicsDevice graphicsDevice, TmpImage image, Palette palette)
        {
            int width = (int)image.ExtraWidth;
            int height = (int)image.ExtraHeight;

            var texture = new Texture2D(graphicsDevice, width, height);
            Color[] colorData = new Color[width * height];
            for (int i = 0; i < colorData.Length; i++)
            {
                colorData[i] = Color.Transparent;
            }

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    int index = (y * width) + x;

                    if (image.ExtraGraphicsColorData[index] > 0)
                    {
                        colorData[index] = palette.Data[image.ExtraGraphicsColorData[index]].ToXnaColor();
                    }
                }
            }

            texture.SetData(colorData);
            return texture;
        }

        public Texture2D GetPaletteTexture()
        {
            return Palette.GetTexture();
        }
    }
}
