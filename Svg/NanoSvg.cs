using System;

#nullable disable

namespace NanoSvg
{
    public class Rasterizer : IDisposable
    {
        private IntPtr handle = IntPtr.Zero;
        
        // TODO wip //
        
        ~Rasterizer() => Dispose();
        
        public void Dispose()
        {
            if (this.handle != IntPtr.Zero)
            {
                SvgNativeMethods.nsvgDeleteRasterizer(this.handle);
                this.handle = IntPtr.Zero;
            }
            GC.SuppressFinalize( this);
        }
    }
}
