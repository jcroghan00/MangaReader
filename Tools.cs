using Microsoft.Maui.Controls.Shapes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MangaReader
{
    class Tools
    {
        public static Border makeNewTag(string tag)
        {
            string text = tag;
            Color backgroundColor = (Color)App.Current.Resources["hlColor"];

            if (tag == "suggestive" || tag == "erotica")
            {
                text = tag[0].ToString().ToUpper() + tag.Substring(1);
                backgroundColor = (Color)App.Current.Resources[tag + "BgColor"];
            }

            Border border = new Border
            {
                BackgroundColor = backgroundColor,
                Stroke = backgroundColor,
                StrokeThickness = 1,
                StrokeShape = new RoundRectangle
                {
                    CornerRadius = 10
                },
                Content = new Label { Text = text, Margin = new Thickness(15, 5, 15, 5) }
            };

            return border;
        }
    }
}
