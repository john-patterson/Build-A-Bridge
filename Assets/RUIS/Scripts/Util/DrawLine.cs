/*****************************************************************************

Content    :   Functions to draw a simple line into an array of Color elements
Authors    :   Mikael Matveinen
Copyright  :   Copyright 2013 Tuukka Takala, Mikael Matveinen. All Rights reserved.
Licensing  :   RUIS is distributed under the LGPL Version 3 license.

******************************************************************************/

using UnityEngine;
using System.Collections;

public class DrawLine {

    //converted to C# from http://wiki.unity3d.com/index.php?title=TextureDrawLine
    public static void DrawSimpleLine(ref Color[] pixels, int x0, int y0, int x1, int y1, int width, int height, Color col)
    {
        x0 = Mathf.Clamp(x0, 0, width-1);
        y0 = Mathf.Clamp(y0, 0, height-1);
        x1 = Mathf.Clamp(x1, 0, width-1);
        y1 = Mathf.Clamp(y1, 0, height-1);
        int dy = y1 - y0;
        int dx = x1 - x0;

        int stepy, stepx;
        if (dy < 0)
        {
            dy = -dy; stepy = -1;
        }
        else 
        { 
            stepy = 1; 
        }
        if (dx < 0) 
        { 
            dx = -dx; stepx = -1; 
        }
        else 
        { 
            stepx = 1; 
        }

        dy <<= 1;
        dx <<= 1;

        //Debug.Log(x0 + "; " + y0);
        pixels[x0 + y0 * width] = col;
        //tex.SetPixel(x0, y0, col);
        if (dx > dy)
        {
            int fraction = dy - (dx >> 1);
            while (x0 != x1)
            {
                if (fraction >= 0)
                {
                    y0 += stepy;
                    fraction -= dx;
                }
                x0 += stepx;
                fraction += dy;
                pixels[x0 + y0 * width] = col;
                //tex.SetPixel(x0, y0, col);
            }
        }
        else
        {
            int fraction = dx - (dy >> 1);
            while (y0 != y1)
            {
                if (fraction >= 0)
                {
                    x0 += stepx;
                    fraction -= dy;
                }
                y0 += stepy;
                fraction += dx;
                pixels[x0 + y0 * width] = col;
                //tex.SetPixel(x0, y0, col);
            }
        }
    }
}
