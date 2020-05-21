using System;
using System.Collections.Generic;
using System.Drawing;

namespace IpcLibrary
{

    public abstract class Serializable
    {
        public abstract int Write(ref byte[] buff, int start);

        public abstract int Read(ref byte[] buff, int start);
    }

    public class RemoteConfig : Serializable
    {
        public int Speed = 0;
        public int FPS = 0;
        public int NoteHeight = 0;
        public int HitHeight = 0;
        public int NoteStrokeWidth = 0;
        public string BackgroundPicture = "";
        public string BackgroundPictureInPlaying = "";
        public int ORTDPListenInterval = 0;

        public bool Loaded = false;

        public static int ID = -1;

        public override int Write(ref byte[] buff, int start)
        {
            start = SerializeUtils.WriteInt(Speed, ref buff, start);
            start = SerializeUtils.WriteInt(FPS, ref buff, start);
            start = SerializeUtils.WriteInt(NoteHeight, ref buff, start);
            start = SerializeUtils.WriteInt(HitHeight, ref buff, start);
            start = SerializeUtils.WriteInt(NoteStrokeWidth, ref buff, start);
            start = SerializeUtils.WriteString(ref BackgroundPicture, ref buff, start);
            start = SerializeUtils.WriteString(ref BackgroundPictureInPlaying, ref buff, start);
            start = SerializeUtils.WriteInt(ORTDPListenInterval, ref buff, start);
            start = SerializeUtils.WriteBool(Loaded, ref buff, start);
            return start;
        }

        public override int Read(ref byte[] buff, int start)
        {
            start = SerializeUtils.ReadInt(out Speed, ref buff, start);
            start = SerializeUtils.ReadInt(out FPS, ref buff, start);
            start = SerializeUtils.ReadInt(out NoteHeight, ref buff, start);
            start = SerializeUtils.ReadInt(out HitHeight, ref buff, start);
            start = SerializeUtils.ReadInt(out NoteStrokeWidth, ref buff, start);
            start = SerializeUtils.ReadString(out BackgroundPicture, ref buff, start);
            start = SerializeUtils.ReadString(out BackgroundPictureInPlaying, ref buff, start);
            start = SerializeUtils.ReadInt(out ORTDPListenInterval, ref buff, start);
            start = SerializeUtils.ReadBool(out Loaded, ref buff, start);
            return start;
        }
    }

    public class LineEvent : Serializable
    {
        public int X1;
        public int Y1;
        public int X2;
        public int Y2;
        public int Width;
        public Color Color;
        public bool Stipple;

        public override int Read(ref byte[] buff, int start)
        {
            start = SerializeUtils.ReadInt(out X1, ref buff, start);
            start = SerializeUtils.ReadInt(out Y1, ref buff, start);
            start = SerializeUtils.ReadInt(out X2, ref buff, start);
            start = SerializeUtils.ReadInt(out Y2, ref buff, start);
            start = SerializeUtils.ReadInt(out Width, ref buff, start);
            start = SerializeUtils.ReadColor(out Color, ref buff, start);
            start = SerializeUtils.ReadBool(out Stipple, ref buff, start);
            return start;
        }

        public override int Write(ref byte[] buff, int start)
        {
            start = SerializeUtils.WriteInt(X1, ref buff, start);
            start = SerializeUtils.WriteInt(Y1, ref buff, start);
            start = SerializeUtils.WriteInt(X2, ref buff, start);
            start = SerializeUtils.WriteInt(Y2, ref buff, start);
            start = SerializeUtils.WriteInt(Width, ref buff, start);
            start = SerializeUtils.WriteColor(ref Color, ref buff, start);
            start = SerializeUtils.WriteBool(Stipple, ref buff, start);
            return start;
        }
    };

    public class RectEvent : Serializable
    {
        public int X1, Y1, X2, Y2;
        public Color Color;
        public bool ShouldFilled;

        public override int Read(ref byte[] buff, int start)
        {
            start = SerializeUtils.ReadInt(out X1, ref buff, start);
            start = SerializeUtils.ReadInt(out Y1, ref buff, start);
            start = SerializeUtils.ReadInt(out X2, ref buff, start);
            start = SerializeUtils.ReadInt(out Y2, ref buff, start);
            start = SerializeUtils.ReadColor(out Color, ref buff, start);
            start = SerializeUtils.ReadBool(out ShouldFilled, ref buff, start);
            return start;
        }

        public override int Write(ref byte[] buff, int start)
        {
            start = SerializeUtils.WriteInt(X1, ref buff, start);
            start = SerializeUtils.WriteInt(Y1, ref buff, start);
            start = SerializeUtils.WriteInt(X2, ref buff, start);
            start = SerializeUtils.WriteInt(Y2, ref buff, start);
            start = SerializeUtils.WriteColor(ref Color, ref buff, start);
            start = SerializeUtils.WriteBool(ShouldFilled, ref buff, start);
            return start;
        }
    };

    [Serializable]
    // 1. client fetchs commands, and set RequestUpdate = true
    // 2. server updates commands, and set RequestUpdate = false
    public class RemoteRenderCommand : Serializable
    {
        public List<LineEvent> LineEvents = new List<LineEvent>();
        public List<RectEvent> RectEvents = new List<RectEvent>();
        public bool DrawBackground = true;
        public string PlayerName = "Unknown";
        public bool RequestUpdate = false;

        public override int Read(ref byte[] buff, int start)
        {
            int count;
            LineEvents.Clear();
            start = SerializeUtils.ReadInt(out count, ref buff, start);
            for (var i = 0; i < count; i++)
            {
                var lineEvent = new LineEvent();
                start = lineEvent.Read(ref buff, start);
                LineEvents.Add(lineEvent);
            }

            RectEvents.Clear();
            start = SerializeUtils.ReadInt(out count, ref buff, start);
            for (var i = 0; i < count; i++)
            {
                var rectEvent = new RectEvent();
                start = rectEvent.Read(ref buff, start);
                RectEvents.Add(rectEvent);
            }

            start = SerializeUtils.ReadBool(out DrawBackground, ref buff, start);
            start = SerializeUtils.ReadString(out PlayerName, ref buff, start);
            start = SerializeUtils.ReadBool(out RequestUpdate, ref buff, start);

            return start;
        }

        public override int Write(ref byte[] buff, int start)
        {
            try
            {
                var count = LineEvents.Count;
                count = Math.Min(500, count);
                start = SerializeUtils.WriteInt(count, ref buff, start);
                for (var i = 0; i < count; i++)
                {
                    start = LineEvents[i].Write(ref buff, start);
                }

                count = RectEvents.Count;
                count = Math.Min(500, count);
                start = SerializeUtils.WriteInt(count, ref buff, start);
                for (var i = 0; i < count; i++)
                {
                    start = RectEvents[i].Write(ref buff, start);
                }

                start = SerializeUtils.WriteBool(DrawBackground, ref buff, start);
                start = SerializeUtils.WriteString(ref PlayerName, ref buff, start);
                start = SerializeUtils.WriteBool(RequestUpdate, ref buff, start);
            } catch (Exception e)
            {
                Console.WriteLine($"Error: {start} - {buff.Length} - {LineEvents.Count} - {RectEvents.Count}");
                Console.WriteLine(e.StackTrace);
            }

            return start;
        }
    }
}
