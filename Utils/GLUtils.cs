using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using OpenTK;
using OpenTK.Graphics.OpenGL;
using static ManiaRTRender.ManiaRTRenderPlugin;

namespace ManiaRTRender.Utils
{
    class GLUtils
    {

        public static readonly int GAME_WIDTH = 540;
        public static readonly int GAME_HEIGHT = 960;

        public static void Resize(GLControl c)
        {
            GL.Viewport(0, 0, c.ClientSize.Width, c.ClientSize.Height);
            GL.MatrixMode(MatrixMode.Projection);
            GL.LoadIdentity();
            GL.Ortho(0, GAME_WIDTH, 0, GAME_HEIGHT, -1, 1);
        }

        public static void DrawLine(int x1, int y1, int x2, int y2, int width, Color color, bool stipple)
        {
            GL.Color3(color);
            GL.LineWidth(width);
            if (stipple)
            {
                GL.LineStipple(1, 0x0F0F);
                GL.Enable(EnableCap.LineStipple);
            }
            else
            {
                GL.Disable(EnableCap.LineStipple);
            }

            GL.Begin(PrimitiveType.Lines);
            GL.Vertex3(x1, GAME_HEIGHT - y1, 0);
            GL.Vertex3(x2, GAME_HEIGHT - y2, 0);
            GL.End();
        }

        public static void DrawRect(int x1, int y1, int x2, int y2, Color color, bool shouldFilled)
        {
            GL.Color3(color);

            if (shouldFilled)
            {
                GL.Begin(PrimitiveType.Quads);
                GL.Vertex3(x1, GAME_HEIGHT - y1, 0);
                GL.Vertex3(x1, GAME_HEIGHT - y2, 0);
                GL.Vertex3(x2, GAME_HEIGHT - y2, 0);
                GL.Vertex3(x2, GAME_HEIGHT - y1, 0);
                GL.End();
            }
            else
            {
                GL.LineWidth(Setting.NoteStrokeWidth);
                GL.Disable(EnableCap.LineStipple);
                GL.Begin(PrimitiveType.LineLoop);
                GL.Vertex3(x1, GAME_HEIGHT - y1, 0);
                GL.Vertex3(x1, GAME_HEIGHT - y2, 0);
                GL.Vertex3(x2, GAME_HEIGHT - y2, 0);
                GL.Vertex3(x2, GAME_HEIGHT - y1, 0);
                GL.End();
            }
        }

        private static readonly string DEFAULT_PATH = "This is an impossible image path hhhhhhhh";
        private static string CurrentImagePath = DEFAULT_PATH;
        private static int CurrentHandle = 0;
        private static bool HasLoadedImage = false;

        private static void LoadImage(string image_path)
        {
            Bitmap bitmap;
            CurrentImagePath = image_path;

            try
            {
                bitmap = image_path.Trim() == string.Empty ? Properties.Resource.bg : new Bitmap(image_path);
            } catch (Exception e)
            {
                Logger.E($"Fail to load image from {image_path}: {e.Message}");
                HasLoadedImage = false;
                return;
            }

            GL.GenTextures(1, out CurrentHandle);
            GL.BindTexture(TextureTarget.Texture2D, CurrentHandle);

            BitmapData data = bitmap.LockBits(new Rectangle(0, 0, bitmap.Width, bitmap.Height),
                ImageLockMode.ReadOnly, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
            GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba, data.Width, data.Height, 0,
                OpenTK.Graphics.OpenGL.PixelFormat.Bgra, PixelType.UnsignedByte, data.Scan0);
            bitmap.UnlockBits(data);

            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Linear);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Linear);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)TextureWrapMode.Repeat);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)TextureWrapMode.Repeat);

            HasLoadedImage = true;
        }

        public static void DrawImage(string image_path)
        {
            if (image_path != CurrentImagePath)
            {
                LoadImage(image_path);
            }

            if (!HasLoadedImage) return;

            // make GL_MODULATE happy
            GL.Color3(1.0, 1.0, 1.0);

            GL.Enable(EnableCap.Texture2D);
            GL.BindTexture(TextureTarget.Texture2D, CurrentHandle);

            GL.Begin(PrimitiveType.Quads);
            GL.TexCoord2(0, 0);
            GL.Vertex2(0, GAME_HEIGHT);

            GL.TexCoord2(1, 0);
            GL.Vertex2(GAME_WIDTH, GAME_HEIGHT);

            GL.TexCoord2(1, 1);
            GL.Vertex2(GAME_WIDTH, 0);

            GL.TexCoord2(0, 1);
            GL.Vertex2(0, 0);
            GL.End();

        }

        public static void DisableImage()
        {
            GL.Disable(EnableCap.Texture2D);
            CurrentImagePath = DEFAULT_PATH;
        }
    }
}
