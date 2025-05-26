using SkiaSharp;

#nullable disable

namespace Cairo;

public static class SurfaceDrawImage
{
    public static unsafe void Image(this ImageSurface surface, SKBitmap bmp, int xPos, int yPos, int width, int height)
    {
        SKBitmap resized = ResizeImage(bmp, width, height);
        var sourcePixels = (uint*)resized.GetPixels().ToPointer();
        uint* destPixels = (uint*)surface.DataPtr.ToPointer();
        int surfaceWidth = surface.Width;
        int bitmapWidth = resized.Width;
        int surfaceMaxIndex = surface.Width * surface.Height;
        int destIndex, sourceIndex;
        for (int x = 0; x < resized.Width; x++)
        {
            for (int y = 0; y < resized.Height; y++)
            {
                sourceIndex = y * bitmapWidth + x;
                destIndex = (y + yPos) * surfaceWidth + x + xPos;
                if (destIndex >= surfaceMaxIndex) continue;
                    destPixels[destIndex] = ColorOverlay(destPixels[destIndex], sourcePixels[sourceIndex]);
            }
        }

        resized.Dispose();
        surface.MarkDirty();
    }

    public static uint ColorOverlay(uint rgbaBase, uint rgbaOver)
    {
        float aBase = ((rgbaBase >> 24) & 0xff) / 255f;
        float aOver = ((rgbaOver >> 24) & 0xff) / 255f;

        float aTotal = aOver + aBase * (1 - aOver);

        float rB = (rgbaBase >> 16) & 0xff;
        float gB = (rgbaBase >> 8) & 0xff;
        float bB = (rgbaBase >> 0) & 0xff;

        float rO = (rgbaOver >> 16) & 0xff;
        float gO = (rgbaOver >> 8) & 0xff;
        float bO = (rgbaOver >> 0) & 0xff;

        return
            (uint)(255f * aTotal) << 24 |
            (uint)((rO * aOver + rB * aBase * (1 - aOver)) / aTotal) << 16 |
            (uint)((gO * aOver + gB * aBase * (1 - aOver)) / aTotal) << 8 |
            (uint)((bO * aOver + bB * aBase * (1 - aOver)) / aTotal) << 0;
    }

    public static SKBitmap ResizeImage(SKBitmap image, int width, int height)
    {
        var skImageInfo = new SKImageInfo(width, height, SKColorType.Bgra8888, SKAlphaType.Unpremul);
        var bitmap = image.Resize(skImageInfo, SKFilterQuality.High);
        return bitmap;
    }
}