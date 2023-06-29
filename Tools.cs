using Microsoft.Maui.Controls.Shapes;

namespace MangaReader
{
    class Tools
    {
        public static Border MakeNewTag(string tag)
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

        public static async void ToMangaPage(object sender, EventArgs e)
        {
            string mangaId = ((TappedEventArgs)e).Parameter.ToString();
            await Shell.Current.GoToAsync($"MangaPage?mangaId={mangaId}");
        }

        public static async void ToChapterPage(object sender, EventArgs e)
        {
            string chapterId = ((TappedEventArgs)e).Parameter.ToString();
            await Shell.Current.GoToAsync($"ChapterPage?chapterId={chapterId}");
        }
    }
}
