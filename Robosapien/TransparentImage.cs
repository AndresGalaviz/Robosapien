﻿using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace Com.Enterprisecoding.Robosapien {
    public class TransparentImage : Image {
        protected override HitTestResult HitTestCore(PointHitTestParameters hitTestParameters) {
            try {
                // Get value of current pixel
                var source = (BitmapSource) Source;
                var x = (int) (hitTestParameters.HitPoint.X/ActualWidth*source.PixelWidth);
                var y = (int) (hitTestParameters.HitPoint.Y/ActualHeight*source.PixelHeight);
                var pixels = new byte[4];
                source.CopyPixels(new Int32Rect(x, y, 1, 1), pixels, 4, 0);
                // Check alpha channel
                if (pixels[3] > 10) {
                    return new PointHitTestResult(this, hitTestParameters.HitPoint);
                }
                else {
                    return null;
                }
            }
            catch {
                return new PointHitTestResult(this, hitTestParameters.HitPoint);
            }
        }

        protected override GeometryHitTestResult HitTestCore(GeometryHitTestParameters hitTestParameters) {
            // Do something similar here, possibly checking every pixel within
            // the hitTestParameters.HitGeometry.Bounds rectangle
            return base.HitTestCore(hitTestParameters);
        }
    }
}