using SharpDX;
using System;

namespace SoftRender.Engine
{
    struct Color
    {
        public byte B, G, R, A;

        public void FromColor4(ref Color4 color)
        {
            color.ToBgra(out R, out G, out B, out A);
        }

        public Color(Color4 color)
        {
            color.ToBgra(out R, out G, out B, out A);
        }
    }
}
