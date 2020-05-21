using IpcLibrary;
using OpenTK;
using OpenTK.Graphics.OpenGL;
using System;
using System.Drawing;
using System.Drawing.Imaging;
namespace RenderClient
{
    class GLUtils
    {

        public static void Resize(GLControl c)
        {
            GL.Viewport(0, 0, c.ClientSize.Width, c.ClientSize.Height);
            GL.MatrixMode(MatrixMode.Projection);
            GL.LoadIdentity();
            GL.Ortho(0, IpcConstants.GAME_WIDTH, 0, IpcConstants.GAME_HEIGHT, -1, 1);
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
            GL.Vertex3(x1, IpcConstants.GAME_HEIGHT - y1, 0);
            GL.Vertex3(x2, IpcConstants.GAME_HEIGHT - y2, 0);
            GL.End();
        }

        public static void DrawRect(int x1, int y1, int x2, int y2, Color color, bool shouldFilled)
        {
            GL.Color3(color);

            if (shouldFilled)
            {
                GL.Begin(PrimitiveType.Quads);
                GL.Vertex3(x1, IpcConstants.GAME_HEIGHT - y1, 0);
                GL.Vertex3(x1, IpcConstants.GAME_HEIGHT - y2, 0);
                GL.Vertex3(x2, IpcConstants.GAME_HEIGHT - y2, 0);
                GL.Vertex3(x2, IpcConstants.GAME_HEIGHT - y1, 0);
                GL.End();
            }
            else
            {
                GL.LineWidth(Setting.NoteStrokeWidth);
                GL.Disable(EnableCap.LineStipple);
                GL.Begin(PrimitiveType.LineLoop);
                GL.Vertex3(x1, IpcConstants.GAME_HEIGHT - y1, 0);
                GL.Vertex3(x1, IpcConstants.GAME_HEIGHT - y2, 0);
                GL.Vertex3(x2, IpcConstants.GAME_HEIGHT - y2, 0);
                GL.Vertex3(x2, IpcConstants.GAME_HEIGHT - y1, 0);
                GL.End();
            }
        }


        private const string DEFAULT_PATH = "This is an impossible image path hhhhhhhh";

        public class ImageContext
        {
            internal int CurrentHandler;
            internal string CurrentImagePath = DEFAULT_PATH;
            internal bool HasLoadedImage;
        }

        private static void LoadImage(string imagePath, ImageContext imageContext)
        {
            Bitmap bitmap;
            imageContext.CurrentImagePath = imagePath;

            try
            {
                bitmap = imagePath.Trim() == string.Empty ? Properties.Resources.DefaultBackground : new Bitmap(imagePath);
            } catch (Exception e)
            {
                //Logger.E($"Fail to load image from {image_path}: {e.Message}");
                imageContext.HasLoadedImage = false;
                return;
            }

            GL.GenTextures(1, out imageContext.CurrentHandler);
            GL.BindTexture(TextureTarget.Texture2D, imageContext.CurrentHandler);

            var data = bitmap.LockBits(new Rectangle(0, 0, bitmap.Width, bitmap.Height),
                ImageLockMode.ReadOnly, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
            GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba, data.Width, data.Height, 0,
                OpenTK.Graphics.OpenGL.PixelFormat.Bgra, PixelType.UnsignedByte, data.Scan0);
            bitmap.UnlockBits(data);

            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Linear);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Linear);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)TextureWrapMode.Repeat);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)TextureWrapMode.Repeat);

            imageContext.HasLoadedImage = true;
        }

        public static void DrawImage(string imagePath, ImageContext imageContext)
        {
            if (imagePath != imageContext.CurrentImagePath)
            {
                LoadImage(imagePath, imageContext);
            }

            if (!imageContext.HasLoadedImage) return;

            // make GL_MODULATE happy
            //GL.Color3(1.0, 1.0, 1.0);
            GL.TexEnv(TextureEnvTarget.TextureEnv, TextureEnvParameter.TextureEnvMode, (int)All.Replace);

            GL.Enable(EnableCap.Texture2D);
            GL.BindTexture(TextureTarget.Texture2D, imageContext.CurrentHandler);

            GL.Begin(PrimitiveType.Quads);
            GL.TexCoord2(0, 0);
            GL.Vertex2(0, IpcConstants.GAME_HEIGHT);

            GL.TexCoord2(1, 0);
            GL.Vertex2(IpcConstants.GAME_WIDTH, IpcConstants.GAME_HEIGHT);

            GL.TexCoord2(1, 1);
            GL.Vertex2(IpcConstants.GAME_WIDTH, 0);

            GL.TexCoord2(0, 1);
            GL.Vertex2(0, 0);
            GL.End();
            GL.Disable(EnableCap.Texture2D);
        }

        public static void DisableImage(ImageContext imageContext)
        {
            GL.Disable(EnableCap.Texture2D);
            imageContext.CurrentImagePath = DEFAULT_PATH;
        }
    }
}
