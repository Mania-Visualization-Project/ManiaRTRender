using System.Drawing;
using OpenTK;
using OpenTK.Graphics.OpenGL;

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
    }
}
