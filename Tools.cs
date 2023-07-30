using Microsoft.Maui.Controls.Shapes;

namespace MangaReader
{
    class Tools
    {
        public static Border MakeNewTag(string tag)
        {
            string text = tag[0].ToString().ToUpper() + tag[1..];

            Color backgroundColor = (Color)App.Current.Resources["hlColor"];

            if (tag == "suggestive" || tag == "erotica")
            {
                backgroundColor = (Color)App.Current.Resources[tag + "BgColor"];
            }

            Border border = new()
            {
                BackgroundColor = backgroundColor,
                Stroke = backgroundColor,
                StrokeThickness = 1,
                Padding = new Thickness(0, 0, 0, 0),
                StrokeShape = new RoundRectangle
                {
                    CornerRadius = 10
                },
                Content = new Label { 
                    Text = text, 
                    Margin = new Thickness(15, 5, 15, 5),
                    VerticalOptions = LayoutOptions.Center
                }
            };

            return border;
        }

        public static async void ToMangaPage(object sender, EventArgs e)
        {
            string mangaId = ((TappedEventArgs)e).Parameter.ToString();
            await Shell.Current.GoToAsync($"MangaPage?mangaId={mangaId}");
        }

        public static async void ToChapterPage(object sender, EventArgs e)
        {
            string chapterId = ((List<string>)((TappedEventArgs)e).Parameter)[0];
            string longStrip = ((List<string>)((TappedEventArgs)e).Parameter)[1];
            string mangaId = ((List<string>)((TappedEventArgs)e).Parameter)[2];
            await Shell.Current.GoToAsync($"ChapterPage?mangaId={mangaId}&chapterId={chapterId}&longStrip={longStrip}");
        }
    }
}
