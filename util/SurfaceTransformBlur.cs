using System;
using System.Collections.Generic;

#nullable disable

namespace Cairo
{
    public static class SurfaceTransformBlur
    {
        public unsafe static void BlurPartial(this ImageSurface surface, double range, int blurOnlyEdgeWidth)
        {
            BlurPartial(surface, range, blurOnlyEdgeWidth, 0, 0, surface.Width, surface.Height);
        }
        public unsafe static void BlurPartial(this ImageSurface surface, double range, int blurOnlyEdgeWidth, int x1, int y1, int x2, int y2)
        {
            // Seems to crash if blur radius is bigger than width/height?
            range = Math.Min(range, Math.Min(surface.Width, surface.Height) / 2 - 1.5);

            if (x1 == x2 || y1 == y2)
            {
                return;
            }

            if (x2 <= x1 || y2 <= y1)
            {
                throw new ArgumentException("x2 must be largner than x1, and y2 must be larger than y1");
            }

            if (surface.Width <= 0 || surface.Height <= 0)
            {
                throw new ArgumentException("Image surface width and hight must be above 0");
            }

            GaussianBlur blur = new GaussianBlur((int*)surface.DataPtr.ToPointer(), surface.Width, surface.Height);
            blur.ProcessPartial(range, blurOnlyEdgeWidth, x1, y1, x2, y2);

            surface.MarkDirty();
        }

        public unsafe static void BlurFull(this ImageSurface surface, double range)
        {
            // Seems to crash if blur radius is bigger than width/height?
            range = Math.Min(range, Math.Min(surface.Width, surface.Height) / 2 - 1.5);

            if (surface.Width <= 0 || surface.Height <= 0)
            {
                throw new ArgumentException("Image surface width and hight must be above 0");
            }

            GaussianBlur blur = new GaussianBlur((int*)surface.DataPtr.ToPointer(), surface.Width, surface.Height);
            blur.ProcessFull(range);

            surface.MarkDirty();
        }

    }


    // based on https://github.com/mdymel/superfastblur
    // MIT License
    public unsafe class GaussianBlur
    {
        int[] src;

        private readonly int w;
        private readonly int h;

        int* image;

        public unsafe GaussianBlur(int* image, int width, int height)
        {
            w = width;
            h = height;
            src = new int[w * h];
            LoadImage(image);
        }
        public unsafe GaussianBlur(int width, int height)
        {
            w = width;
            h = height;
            src = new int[w * h];
        }

        public unsafe void LoadImage(int* image)
        {
            this.image = image;
            int len = w * h;
            for (int i = 0; i < len; i++)
            {
                src[i] = image[i];
            }
        }

        public void ProcessPartial(double radial, int blurOnlyEdgeWidth, int x1, int y1, int x2, int y2)
        {
            fixed (int* srcP = src)
            {
                gaussBlur_4(srcP, image, false, blurOnlyEdgeWidth, radial, x1, y1, x2, y2);
            }
        }
        public void ProcessFull(double radial)
        {
            fixed (int* srcP = src)
            {
                gaussBlur_4(srcP, image, true, 0, radial, 0, 0, w, h);
            }
        }

        private void gaussBlur_4(int* srcP, int* destP, bool full, int blurOnlyEdgeWidth, double r, int x1, int y1, int x2, int y2)
        {
            var bxs = boxesForGauss(r, 3);
            boxBlur_4(srcP, destP, full, blurOnlyEdgeWidth, (bxs[0] - 1) / 2, x1, y1, x2, y2);
            boxBlur_4(destP, srcP, full, blurOnlyEdgeWidth, (bxs[1] - 1) / 2, x1, y1, x2, y2);
            boxBlur_4(srcP, destP, full, blurOnlyEdgeWidth, (bxs[2] - 1) / 2, x1, y1, x2, y2);
        }

        private int[] boxesForGauss(double sigma, int n)
        {
            var wIdeal = Math.Sqrt((12 * sigma * sigma / n) + 1);
            var wl = (int)Math.Floor(wIdeal);
            if (wl % 2 == 0) wl--;
            var wu = wl + 2;

            var mIdeal = (double)(12 * sigma * sigma - n * wl * wl - 4 * n * wl - 3 * n) / (-4 * wl - 4);
            var m = Math.Round(mIdeal);

            var sizes = new List<int>();
            for (var i = 0; i < n; i++) sizes.Add(i < m ? wl : wu);
            return sizes.ToArray();
        }

        private void boxBlur_4(int* srcP, int* destP, bool full, int blurOnlyEdgeWidth, int r, int x1, int y1, int x2, int y2)
        {
            for (var i = 0; i < src.Length; i++) destP[i] = srcP[i];

            if (!full)
            {
                boxBlurH_4RGBPartial(destP, srcP, blurOnlyEdgeWidth, w, h, r, x1, y1, x2, y2);
                boxBlurT_4RGBPartial(srcP, destP, blurOnlyEdgeWidth, w, h, r, x1, y1, x2, y2);
            } else
            {
                boxBlurH_4RGBFull(destP, srcP, w, h, r);
                boxBlurT_4RGBFull(srcP, destP, w, h, r);
            }

            
        }


        #region Full image blur



        private void boxBlurH_4RGBFull(int* srcP, int* destP, int w, int h, int r)
        {
            var iar = (double)1 / (r + r + 1);

            for (int y = 0; y < h; y++)
            {
                var ti = y * w;
                var li = ti;
                var ri = ti + r;

                var fvR = srcP[ti] & 0xff;
                var lvR = srcP[ti + w - 1] & 0xff;

                var fvG = (srcP[ti] >> 8) & 0xff;
                var lvG = (srcP[ti + w - 1] >> 8) & 0xff;

                var fvB = (srcP[ti] >> 16) & 0xff;
                var lvB = (srcP[ti + w - 1] >> 16) & 0xff;

                var valR = (r + 1) * fvR;
                var valG = (r + 1) * fvG;
                var valB = (r + 1) * fvB;

                for (var x = 0; x < r; x++)
                {
                    int col = srcP[ti + x];
                    valR += col & 0xff;
                    valG += (col >> 8) & 0xff;
                    valB += (col >> 16) & 0xff;
                }

                for (var x = 0; x <= r; x++)
                {
                    int col = srcP[ri++];

                    valR += (col & 0xff) - fvR;
                    valG += ((col >> 8) & 0xff) - fvG;
                    valB += ((col >> 16) & 0xff) - fvB;

                    var valA = (destP[ti] >> 24) & 0xff;
                    destP[ti++] = ((byte)(valA) << 24) | ((byte)(valB * iar) << 16) | ((byte)(valG * iar) << 8) | (byte)(valR * iar);
                }

                for (var x = r + 1; x < w - r; x++)
                {
                    int col1 = srcP[ri++];
                    int col2 = srcP[li++];
                    valR += ((col1 >> 0) & 0xff) - ((col2 >> 0) & 0xff);
                    valG += ((col1 >> 8) & 0xff) - ((col2 >> 8) & 0xff);
                    valB += ((col1 >> 16) & 0xff) - ((col2 >> 16) & 0xff);

                    var valA = (destP[ti] >> 24) & 0xff;
                    destP[ti++] = ((byte)(valA) << 24) | ((byte)(valB * iar) << 16) | ((byte)(valG * iar) << 8) | (byte)(valR * iar);
                }

                for (var x = w - r; x < w; x++)
                {
                    int col = srcP[li++];

                    valR += lvR - ((col >> 0) & 0xff);
                    valG += lvG - ((col >> 8) & 0xff);
                    valB += lvB - ((col >> 16) & 0xff);

                    var valA = (destP[ti] >> 24) & 0xff;
                    destP[ti++] = ((byte)(valA) << 24) | ((byte)(valB * iar) << 16) | ((byte)(valG * iar) << 8) | (byte)(valR * iar);
                }
            };
        }

        private void boxBlurT_4RGBFull(int* srcP, int* destP, int w, int h, int r)
        {
            var iar = (double)1 / (r + r + 1);

            for (int x = 0; x < w; x++)
            {
                var ti = x;
                var li = ti;
                var ri = ti + r * w;

                var fvR = (srcP[ti] >> 0) & 0xff;
                var fvG = (srcP[ti] >> 8) & 0xff;
                var fvB = (srcP[ti] >> 16) & 0xff;

                var lvR = (srcP[ti + w * (h - 1)] >> 0) & 0xff;
                var lvG = (srcP[ti + w * (h - 1)] >> 8) & 0xff;
                var lvB = (srcP[ti + w * (h - 1)] >> 16) & 0xff;

                var valR = (r + 1) * fvR;
                var valG = (r + 1) * fvG;
                var valB = (r + 1) * fvB;

                for (var y = 0; y < r; y++)
                {
                    valR += (srcP[ti + y * w] >> 0) & 0xff;
                    valG += (srcP[ti + y * w] >> 8) & 0xff;
                    valB += (srcP[ti + y * w] >> 16) & 0xff;
                }

                for (var y = 0; y <= r; y++)
                {
                    int col = srcP[ri];
                    valR += ((col >> 0) & 0xff) - fvR;
                    valG += ((col >> 8) & 0xff) - fvG;
                    valB += ((col >> 16) & 0xff) - fvB;

                    var valA = (destP[ti] >> 24) & 0xff;
                    destP[ti] = ((byte)(valA) << 24) | ((byte)(valB * iar) << 16) | ((byte)(valG * iar) << 8) | ((byte)(valR * iar));
                    ri += w;
                    ti += w;
                }


                for (var y = r + 1; y < h - r; y++)
                {

                    int col1 = srcP[ri];
                    int col2 = srcP[li];
                    valR += ((col1 >> 0) & 0xff) - ((col2 >> 0) & 0xff);
                    valG += ((col1 >> 8) & 0xff) - ((col2 >> 8) & 0xff);
                    valB += ((col1 >> 16) & 0xff) - ((col2 >> 16) & 0xff);

                    var valA = (destP[ti] >> 24) & 0xff;
                    destP[ti] = ((byte)(valA) << 24) | ((byte)(valB * iar) << 16) | ((byte)(valG * iar) << 8) | ((byte)(valR * iar));
                    li += w;
                    ri += w;
                    ti += w;
                }

                for (var y = h - r; y < h; y++)
                {
                    int col = srcP[li];
                    valR += lvR - ((col >> 0) & 0xff);
                    valG += lvG - ((col >> 8) & 0xff);
                    valB += lvB - ((col >> 16) & 0xff);

                    var valA = (destP[ti] >> 24) & 0xff;
                    destP[ti] = ((byte)(valA) << 24) | ((byte)(valB * iar) << 16) | ((byte)(valG * iar) << 8) | ((byte)(valR * iar));
                    li += w;
                    ti += w;
                }
            }
        }

        #endregion


        #region Partial blur
        private void boxBlurH_4RGBPartial(int* srcP, int* destP, int blurOnlyEdgeWidth, int w, int h, int r, int xs, int ys, int xe, int ye)
        {
            var iar = (double)1 / (r + r + 1);

            int testx;

            int x1 = xs + blurOnlyEdgeWidth;
            int y1 = ys + blurOnlyEdgeWidth;
            int y2 = ye - 2 * blurOnlyEdgeWidth;

            int xSkip = Math.Max(0, xe - x1 - blurOnlyEdgeWidth);

            for (int y = ys; y < ye; y++)
            {
                var ti = y * w + xs;
                var li = ti;
                var ri = ti + r;

                var fvR = srcP[ti] & 0xff;
                var lvR = srcP[ti + (xe-xs) - 1] & 0xff;

                var fvG = (srcP[ti] >> 8) & 0xff;
                var lvG = (srcP[ti + (xe - xs) - 1] >> 8) & 0xff;

                var fvB = (srcP[ti] >> 16) & 0xff;
                var lvB = (srcP[ti + (xe - xs) - 1] >> 16) & 0xff;

                var valR = (r + 1) * fvR;
                var valG = (r + 1) * fvG;
                var valB = (r + 1) * fvB;

                ti -= xs;
                for (var x = xs; x < xs + r; x++)
                {
                    int col = srcP[ti + x];
                    valR += col & 0xff;
                    valG += (col >> 8) & 0xff;
                    valB += (col >> 16) & 0xff;
                }
                ti += xs;

                for (var x = xs; x <= xs + r; x++)
                {
                    int col = srcP[ri++];

                    valR += (col & 0xff) - fvR;
                    valG += ((col >> 8) & 0xff) - fvG;
                    valB += ((col >> 16) & 0xff) - fvB;

                    var valA = (destP[ti] >> 24) & 0xff;
                    destP[ti++] = ((byte)(valA) << 24) | ((byte)(valB * iar) << 16) | ((byte)(valG * iar) << 8) | (byte)(valR * iar);
                }

                testx = y > y1 && y < y2 ? x1 : - 1;

                for (var x = xs + r + 1; x < xe - r; x++)
                {
                    int col1 = srcP[ri++];
                    int col2 = srcP[li++];
                    valR += ((col1 >> 0) & 0xff) - ((col2 >> 0) & 0xff);
                    valG += ((col1 >> 8) & 0xff) - ((col2 >> 8) & 0xff);
                    valB += ((col1 >> 16) & 0xff) - ((col2 >> 16) & 0xff);

                    var valA = (destP[ti] >> 24) & 0xff;
                    destP[ti++] = ((byte)(valA) << 24) | ((byte)(valB * iar) << 16) | ((byte)(valG * iar) << 8) | (byte)(valR * iar);

                    if (x == testx)
                    {
                        x += xSkip;
                        ri += xSkip;
                        li += xSkip;
                        ti += xSkip;
                    }
                }

                for (var x = xe - r; x < xe; x++)
                {
                    int col = srcP[li++];

                    valR += lvR - ((col >> 0) & 0xff);
                    valG += lvG - ((col >> 8) & 0xff);
                    valB += lvB - ((col >> 16) & 0xff);

                    var valA = (destP[ti] >> 24) & 0xff;
                    destP[ti++] = ((byte)(valA) << 24) | ((byte)(valB * iar) << 16) | ((byte)(valG * iar) << 8) | (byte)(valR * iar);
                }
            };
        }

        private void boxBlurT_4RGBPartial(int* srcP, int* destP, int blurOnlyEdgeWidth, int w, int h, int r, int xs, int ys, int xe, int ye)
        {
            var iar = (double)1 / (r + r + 1);

            int testy;
            int x1 = xs + blurOnlyEdgeWidth;
            int x2 = xe - 2 * blurOnlyEdgeWidth;
            int y1 = ys + blurOnlyEdgeWidth;
            int ySkip = Math.Max(0, ye - y1 - blurOnlyEdgeWidth);

            for (int x = xs; x < xe; x++)
            {
                var ti = x + ys*w;
                var li = ti;
                var ri = ti + r * w;

                var fvR = (srcP[ti] >> 0) & 0xff;
                var fvG = (srcP[ti] >> 8) & 0xff;
                var fvB = (srcP[ti] >> 16) & 0xff;

                var lvR = (srcP[ti + w * ((ye - ys) - 1)] >> 0) & 0xff;
                var lvG = (srcP[ti + w * ((ye - ys) - 1)] >> 8) & 0xff;
                var lvB = (srcP[ti + w * ((ye - ys) - 1)] >> 16) & 0xff;

                var valR = (r + 1) * fvR;
                var valG = (r + 1) * fvG;
                var valB = (r + 1) * fvB;

                ti -= ys * w;
                for (var y = ys; y < ys + r; y++)
                {
                    valR += (srcP[ti + y * w] >> 0) & 0xff;
                    valG += (srcP[ti + y * w] >> 8) & 0xff;
                    valB += (srcP[ti + y * w] >> 16) & 0xff;
                }
                ti += ys * w;

                for (var y = ys; y <= ys + r; y++)
                {
                    int col = srcP[ri];
                    valR += ((col >> 0) & 0xff) - fvR;
                    valG += ((col >> 8) & 0xff) - fvG;
                    valB += ((col >> 16) & 0xff) - fvB;

                    var valA = (destP[ti] >> 24) & 0xff;
                    destP[ti] = ((byte)(valA) << 24) | ((byte)(valB * iar) << 16) | ((byte)(valG * iar) << 8) | ((byte)(valR * iar));
                    ri += w;
                    ti += w;
                }

                testy = x > x1 && x < x2 ? y1 : - 1;

                for (var y = ys + r + 1; y < ye - r; y++)
                {
                    int col1 = srcP[ri];
                    int col2 = srcP[li];
                    valR += ((col1 >> 0) & 0xff) - ((col2 >> 0) & 0xff);
                    valG += ((col1 >> 8) & 0xff) - ((col2 >> 8) & 0xff);
                    valB += ((col1 >> 16) & 0xff) - ((col2 >> 16) & 0xff);

                    var valA = (destP[ti] >> 24) & 0xff;
                    destP[ti] = ((byte)(valA) << 24) | ((byte)(valB * iar) << 16) | ((byte)(valG * iar) << 8) | ((byte)(valR * iar));
                    li += w;
                    ri += w;
                    ti += w;

                    if (y == testy)
                    {
                        y += ySkip;
                        li += ySkip * w;
                        ri += ySkip * w;
                        ti += ySkip * w;
                    }
                }

                for (var y = ye - r; y < ye; y++)
                {
                    int col = srcP[li];
                    valR += lvR - ((col >> 0) & 0xff);
                    valG += lvG - ((col >> 8) & 0xff);
                    valB += lvB - ((col >> 16) & 0xff);

                    var valA = (destP[ti] >> 24) & 0xff;
                    destP[ti] = ((byte)(valA) << 24) | ((byte)(valB * iar) << 16) | ((byte)(valG * iar) << 8) | ((byte)(valR * iar));
                    li += w;
                    ti += w;
                }
            };
        }

        #endregion
    }
}
