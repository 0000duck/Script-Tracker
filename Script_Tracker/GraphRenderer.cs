using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Drawing.Imaging;

namespace Script_Tracker
{
    class GraphRenderer
    {
        public static byte[] BasicLineGraph(string title, List<double> graphvalues, int yhighest, double ynotches, int xhighest, int xnotches, List<string> xlabels)
        {
            using (Bitmap bmp = new Bitmap(1000, 500))
            {
                using (Graphics drawer = Graphics.FromImage(bmp))
                {
                    drawer.Clear(Color.White);
                    drawer.SmoothingMode = SmoothingMode.AntiAlias;
                    Font fontTitle = new Font(FontFamily.GenericMonospace, 12);
                    Font font = new Font(FontFamily.GenericMonospace, 9);
                    drawer.DrawLine(Pens.Black, 25, 0, 25, bmp.Height);
                    drawer.DrawLine(Pens.Black, 0, bmp.Height-25, bmp.Width, bmp.Height- 25);
                    for (float f = 0; f <= yhighest; f += (float)ynotches)
                    {
                        float x = 5;
                        float y = (float)(1 - f / yhighest) * (bmp.Height - 25);
                        drawer.DrawLine(Pens.LightGray, 0, y, bmp.Width, y);
                        drawer.DrawString(f.ToString(), font, Brushes.Black, x, y);
                    }
                    for (int i = 0; i < graphvalues.Count; i += xnotches)
                    {
                        float x = i * bmp.Width / graphvalues.Count+25;
                        float y = bmp.Height - 25;
                        drawer.DrawLine(Pens.LightGray, x, bmp.Height - 25, x , 0);
                        drawer.DrawString(xlabels[i/xnotches], font, Brushes.Black, x, y);
                    }
                    for (int i=0; i < graphvalues.Count-1; i++)
                    {
                        float x = i * bmp.Width / graphvalues.Count+25;
                        float y = (float)(1 - graphvalues[i] / yhighest) * (bmp.Height - 25);
                        float x2 = (i+1) * bmp.Width / graphvalues.Count+25;
                        float y2 = (float)(1 - graphvalues[(i+1)] / yhighest) * (bmp.Height - 25);
                        drawer.DrawLine(new Pen(Color.DarkBlue, 2), x, y, x2, y2);
                    }
                }
                using (MemoryStream ms = new MemoryStream())
                {
                    bmp.Save(ms, ImageFormat.Png);
                    return ms.ToArray();
                }
            }
        }
    }
}
