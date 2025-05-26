using System;

#nullable disable

namespace Cairo
{
    public static class SurfaceTransformDemulAlpha
    {

        public unsafe static void DemulAlpha(this ImageSurface surface)
        {
            int width = surface.Width;
            int height = surface.Height;


            // https://www.cairographics.org/manual-1.2.0/cairo-Image-Surfaces.html#cairo-format-t
            // "The 32-bit quantities are stored native-endian."
            int rShift = 0;
            int gShift = 8;
            int bShift = 16;
            int aShift = 24;

            if (BitConverter.IsLittleEndian)
            {
                rShift = 24;
                gShift = 16;
                bShift = 8;
                aShift = 0;
            }

            unsafe
            {
                uint* pixels = (uint*)surface.DataPtr.ToPointer();

                for (int x = 0; x < width; x++)
                {
                    for (int y = 0; y < height; y++)
                    {
                        int index = y * width + x;
                        uint color = pixels[index];

                        uint r = (color >> rShift) & 0xff;
                        uint g = (color >> gShift) & 0xff;
                        uint b = (color >> bShift) & 0xff;
                        uint a = (color >> aShift) & 0xff;

                        float arel = a / 255f;

                        r = (uint)(r / arel);
                        g = (uint)(g / arel);
                        b = (uint)(b / arel);

                        pixels[index] = (r << rShift) | (g << gShift) | (b << bShift) | (a << aShift);
                    }
                }

            }

            
        }
    }

}
