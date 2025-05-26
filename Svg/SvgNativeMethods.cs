using System;
using System.Runtime.InteropServices;

#nullable disable

namespace NanoSvg
{
    [StructLayout(LayoutKind.Sequential)]
    public struct NsvgSize
    {
        public float width;
        public float height;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct NsvgViewbox
    {
        public float viewMinx;
        public float viewMiny;
        public float viewWidth;
        public float viewHeight;
    }

    public static class SvgNativeMethods
    {
        private const string Nsvg = "nanosvg";
        
        /*/
         *  hi
        /*/
        
        /*/
         * Binding notes:
         *
         * void *<ret>          - <ret> IntPtr
         * typ_t *handle        - IntPtr handle
         * int *ints            - int[] ints
         * int *ints            - [out] IntPtr ints
         * int *anInt           - [out] out int anInt
         * int *anInt           - [in/out] ref int anInt
         * my_enum_t <ret>      - <ret> MyEnum
         * my_enum_t anEnum     - MyEnum anEnum
         * my_class_t *aClass   - IntPtr          (if C type)
         * my_class_t *aClass   - MyClass aClass  (if data type)    (ICloneable with public object Clone() => this.MemberwiseClone(); ?)
         * my_class_t *aClass   - [out] MyClass aClass              (ICloneable with public object Clone() => this.MemberwiseClone(); ?)
         * my_struct_t *<ret>   - <ret> IntPtr
         * my_struct_t *aStruct - IntPtr/"Declare my_struct_t as class instead of struct and don't pass it by ref"
         * my_struct_t *aStruct - [out] out MyStruct aStruct        (c# struct field ordering should match)
         * const char *<ret>    - <ret> IntPtr                      (use PtrToStringAuto(IntPtr ptr) to obtain the string)
        /*/
        
        
        /* Parsing (Image object) */
        [DllImport(Nsvg, CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr nsvgParseFromFile([MarshalAs(UnmanagedType.LPStr)] string filename, [MarshalAs(UnmanagedType.LPStr)] string units, float dpi);

        [DllImport(Nsvg, CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr nsvgParse([MarshalAs(UnmanagedType.LPStr)] string input, [MarshalAs(UnmanagedType.LPStr)] string units, float dpi);
        
        [DllImport(Nsvg, CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr nsvgImageGetSize(IntPtr image);
        
        [DllImport(Nsvg, CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr nsvgImageGetViewbox(IntPtr image);

        [DllImport(Nsvg, CallingConvention = CallingConvention.Cdecl)]
        public static extern void nsvgDelete(IntPtr image);

        
        /* Rasterizing */
        [DllImport(Nsvg, CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr nsvgCreateRasterizer();
        
        // Rasterizes SVG image, returns RGBA image (non-premultiplied alpha)
        //   r - pointer to rasterizer context
        //   image - pointer to image to rasterize
        //   tx,ty - image offset (applied after scaling)
        //   scale - image scale
        //   dst - pointer to destination image data, 4 bytes per pixel (RGBA)
        //   w - width of the image to render
        //   h - height of the image to render
        //   stride - number of bytes per scaleLine in the destination buffer (usually w * 4)
        [DllImport(Nsvg, CallingConvention = CallingConvention.Cdecl)]
        public static extern void nsvgRasterize(IntPtr rasterizer, IntPtr image, float tx, float ty, float scale, IntPtr dst, int w, int h, int stride);
        
        [DllImport(Nsvg, CallingConvention = CallingConvention.Cdecl)]
        public static extern void nsvgDeleteRasterizer(IntPtr rasterizer);

    }
}
