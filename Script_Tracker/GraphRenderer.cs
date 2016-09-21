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
        public static Color GetRandomBrightColor()
        {
            Random rnd = new Random();
            List<Color> colors = new List<Color>() { Color.FromArgb(150, 255, 0, 0), Color.FromArgb(150, 0, 0, 255), Color.FromArgb(150, 0, 255, 0), Color.FromArgb(150, 255, 255, 0), Color.FromArgb(150, 255, 0, 255), Color.FromArgb(150, 0, 255, 255), Color.FromArgb(150, 0, 0, 0), Color.FromArgb(150, 125, 255, 125), Color.FromArgb(150, 255, 125, 125), Color.FromArgb(150, 125, 125, 255) };
            return colors[rnd.Next(0, colors.Count)];
        }


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
        public static byte[] MultiLineGraph(string title, Dictionary<string, List<int>> graphvalues, int yhighest, double ynotches, int xhighest, int xnotches, List<string> xlabels)
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
                    drawer.DrawLine(Pens.Black, 0, bmp.Height - 25, bmp.Width, bmp.Height - 25);
                    int count = graphvalues[graphvalues.Keys.ElementAt(0)].Count;
                    for (float f = 0; f <= yhighest; f += (float)ynotches)
                    {
                        float x = 5;
                        float y = (float)(1 - f / yhighest) * (bmp.Height - 25);
                        drawer.DrawLine(Pens.LightGray, 0, y, bmp.Width, y);
                        drawer.DrawString(f.ToString(), font, Brushes.Black, x, y);
                    }
                    for (int i = 0; i < count; i += xnotches)
                    {
                        float x = i * bmp.Width / count + 25;
                        float y = bmp.Height - 25;
                        drawer.DrawLine(Pens.LightGray, x, bmp.Height - 25, x, 0);
                        drawer.DrawString(xlabels[i / xnotches], font, Brushes.Black, x, y);
                    }
                    float legendx = 50;
                    int linecount = 0;
                    foreach (KeyValuePair<string, List<int>> value in graphvalues)
                    {
                        string graphline = value.Key;
                        Color color = GetRandomBrightColor();
                        for (int i = 0; i < value.Value.Count - 1; i++)
                        {
                            float x = (float)i * bmp.Width / value.Value.Count + 25;
                            float y = (1 - (float)value.Value[i] / yhighest) * (bmp.Height - 25 - linecount);
                            float x2 = (float)(i + 1) * bmp.Width / value.Value.Count + 25;
                            float y2 = (1 - (float)value.Value[(i + 1)] / yhighest) * (bmp.Height - 25 - linecount);
                            drawer.DrawLine(new Pen(color, 2), x, y, x2, y2);
                        }
                        float legendy = (5 + (15 * (float)linecount));
                        drawer.DrawEllipse(new Pen(color, 4), legendx, legendy, 4, 4);
                        drawer.DrawString(graphline, font, Brushes.Black, legendx + 10, legendy - 5);
                        linecount++;
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
