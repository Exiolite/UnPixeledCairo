using System;
using System.Runtime.InteropServices;

#nullable disable

namespace Cairo.Util
{
    public class FreeTypeFontFace : FontFace
    {
        private static bool initialized = false;
        private static IntPtr freetypeLipPtr;
        private IntPtr freetypeFace;

        const string freetype = "freetype6";

        [DllImport(freetype)]
        public static extern int FT_Init_FreeType(out System.IntPtr lib);

        [DllImport(freetype)]
        public static extern void FT_Done_Face(System.IntPtr face);

        [DllImport(freetype)]
        public static extern int FT_New_Face(System.IntPtr lib, string fname, int index, out System.IntPtr face);


        private FreeTypeFontFace(IntPtr cairoFace, IntPtr freetypeFace) : base(cairoFace, true)
        {
            this.freetypeFace = freetypeFace;
        }

        protected override void Dispose(bool disposing)
        {
            NativeMethods.cairo_font_face_destroy(Handle);
            FT_Done_Face(freetypeFace);
        }

        public static FreeTypeFontFace Create(string filename, int loadoptions)
        {
            if (!initialized)
            {
                initialize();
            }
                


            IntPtr freetypeFace;

            if (FT_New_Face(freetypeLipPtr, filename, 0, out freetypeFace) != 0)
            {
                throw new Exception(filename);
            }
            
            IntPtr cairoFace = NativeMethods.cairo_ft_font_face_create_for_ft_face(freetypeFace, loadoptions);
            
            FreeTypeFontFace fontFace = new FreeTypeFontFace(cairoFace, freetypeFace);

            if (fontFace.Status != Status.Success)
            {
                throw new Exception("Failed loading font " + filename + " error: " + fontFace.Status);
            }

            return fontFace;
        }

        private static void initialize()
        {
            if (FT_Init_FreeType(out freetypeLipPtr) != 0)
                throw new Exception("Couldn't init freetype");

            initialized = true;
        }
    }
}
