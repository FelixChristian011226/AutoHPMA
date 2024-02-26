﻿using System;
using System.Drawing;
using System.Windows;
//using AutoHPMA.Core.Recognition.OpenCv;
using AutoHPMA.GameTask;
using AutoHPMA.Helpers;

namespace AutoHPMA.Views.Drawable;

[Serializable]
public class RectDrawable
{
    public string? Name { get; set; }
    public Rect Rect { get; }
    public System.Drawing.Pen Pen { get; } = new(Color.Red, 2);

    public RectDrawable(Rect rect, Pen? pen = null, string? name = null)
    {
        Rect = rect;
        Name = name;

        if (pen != null)
        {
            Pen = pen;
        }
    }

    public RectDrawable(Rect rect, string? name)
    {
        Rect = rect;
        Name = name;
    }

    public override bool Equals(object? obj)
    {
        if (obj == null || GetType() != obj.GetType())
        {
            return false;
        }

        var other = (RectDrawable)obj;
        return Rect.Equals(other.Rect);
    }

    public override int GetHashCode()
    {
        return Rect.GetHashCode();
    }

    public bool IsEmpty => Rect.IsEmpty;
}


//public static class RectDrawableExtension
//{
//    public static RectDrawable ToRectDrawable(this Rect rect, Pen? pen = null, string? name = null)
//    {
//        var scale = TaskContext.Instance().DpiScale;
//        Rect newRect = new(rect.X / scale, rect.Y / scale, rect.Width / scale, rect.Height / scale);
//        return new RectDrawable(newRect, pen, name);
//    }

//    public static RectDrawable ToRectDrawable(this OpenCvSharp.Rect rect, Pen? pen = null, string? name = null)
//    {
//        var scale = TaskContext.Instance().DpiScale;
//        OpenCvSharp.Rect newRect = new((int)(rect.X / scale), (int)(rect.Y / scale), (int)(rect.Width / scale), (int)(rect.Height / scale));
//        return new RectDrawable(newRect.ToWindowsRectangle(), pen, name);
//    }

//    public static RectDrawable ToRectDrawable(this OpenCvSharp.Rect rect, int offsetX, int offsetY, Pen? pen = null, string? name = null)
//    {
//        var scale = TaskContext.Instance().DpiScale;
//        OpenCvSharp.Rect newRect = new(offsetX + (int)(rect.X / scale), offsetY + (int)(rect.Y / scale), (int)(rect.Width / scale), (int)(rect.Height / scale));
//        return new RectDrawable(newRect.ToWindowsRectangle(), pen, name);
//    }
//}