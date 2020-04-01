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
        public String BackgroundPicture = "";
        public int ORTDPListenInterval = 0;

        public bool Loaded = false;

        public static int id = -1;

        public override int Write(ref byte[] buff, int start)
        {
            start = SerializeUtils.WriteInt(Speed, ref buff, start);
            start = SerializeUtils.WriteInt(FPS, ref buff, start);
            start = SerializeUtils.WriteInt(NoteHeight, ref buff, start);
            start = SerializeUtils.WriteInt(HitHeight, ref buff, start);
            start = SerializeUtils.WriteInt(NoteStrokeWidth, ref buff, start);
            start = SerializeUtils.WriteString(ref BackgroundPicture, ref buff, start);
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
            start = SerializeUtils.ReadInt(out ORTDPListenInterval, ref buff, start);
            start = SerializeUtils.ReadBool(out Loaded, ref buff, start);
            return start;
        }
    }

    public class LineEvent : Serializable
    {
        public int x1;
        public int y1;
        public int x2;
        public int y2;
        public int width;
        public Color color;
        public bool stipple;

        public override int Read(ref byte[] buff, int start)
        {
            start = SerializeUtils.ReadInt(out x1, ref buff, start);
            start = SerializeUtils.ReadInt(out y1, ref buff, start);
            start = SerializeUtils.ReadInt(out x2, ref buff, start);
            start = SerializeUtils.ReadInt(out y2, ref buff, start);
            start = SerializeUtils.ReadInt(out width, ref buff, start);
            start = SerializeUtils.ReadColor(out color, ref buff, start);
            start = SerializeUtils.ReadBool(out stipple, ref buff, start);
            return start;
        }

        public override int Write(ref byte[] buff, int start)
        {
            start = SerializeUtils.WriteInt(x1, ref buff, start);
            start = SerializeUtils.WriteInt(y1, ref buff, start);
            start = SerializeUtils.WriteInt(x2, ref buff, start);
            start = SerializeUtils.WriteInt(y2, ref buff, start);
            start = SerializeUtils.WriteInt(width, ref buff, start);
            start = SerializeUtils.WriteColor(ref color, ref buff, start);
            start = SerializeUtils.WriteBool(stipple, ref buff, start);
            return start;
        }
    };

    public class RectEvent : Serializable
    {
        public int x1, y1, x2, y2;
        public Color color;
        public bool shouldFilled;

        public override int Read(ref byte[] buff, int start)
        {
            start = SerializeUtils.ReadInt(out x1, ref buff, start);
            start = SerializeUtils.ReadInt(out y1, ref buff, start);
            start = SerializeUtils.ReadInt(out x2, ref buff, start);
            start = SerializeUtils.ReadInt(out y2, ref buff, start);
            start = SerializeUtils.ReadColor(out color, ref buff, start);
            start = SerializeUtils.ReadBool(out shouldFilled, ref buff, start);
            return start;
        }

        public override int Write(ref byte[] buff, int start)
        {
            start = SerializeUtils.WriteInt(x1, ref buff, start);
            start = SerializeUtils.WriteInt(y1, ref buff, start);
            start = SerializeUtils.WriteInt(x2, ref buff, start);
            start = SerializeUtils.WriteInt(y2, ref buff, start);
            start = SerializeUtils.WriteColor(ref color, ref buff, start);
            start = SerializeUtils.WriteBool(shouldFilled, ref buff, start);
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
            for (int i = 0; i < count; i++)
            {
                LineEvent lineEvent = new LineEvent();
                start = lineEvent.Read(ref buff, start);
                LineEvents.Add(lineEvent);
            }

            RectEvents.Clear();
            start = SerializeUtils.ReadInt(out count, ref buff, start);
            for (int i = 0; i < count; i++)
            {
                RectEvent rectEvent = new RectEvent();
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
                int count = LineEvents.Count;
                count = Math.Min(500, count);
                start = SerializeUtils.WriteInt(count, ref buff, start);
                for (int i = 0; i < count; i++)
                {
                    start = LineEvents[i].Write(ref buff, start);
                }

                count = RectEvents.Count;
                count = Math.Min(500, count);
                start = SerializeUtils.WriteInt(count, ref buff, start);
                for (int i = 0; i < count; i++)
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
