using Microsoft.Maui.Controls.Shapes;
using System.Text.Json.Nodes;

namespace MangaReader.Views;

public partial class SearchResult : ContentView
{
	JsonNode manga;
	public SearchResult(JsonNode manga)
	{
		InitializeComponent();

		this.manga = manga;
	}

    static readonly string coverBaseUrl = "https://uploads.mangadex.org/covers/";

    private void OnViewLoaded(object sender, EventArgs e)
	{
        TapGestureRecognizer tapGestureRecognizer = new TapGestureRecognizer();
        tapGestureRecognizer.Tapped += Tools.ToMangaPage;
        tapGestureRecognizer.CommandParameter = manga["id"].ToString();

        resultBorder.GestureRecognizers.Add(tapGestureRecognizer);

        Grid grid = new()
        {
            HeightRequest = 200,
            MaximumWidthRequest = 1500,
            Padding = new Thickness(10, 10, 10, 10),
            ColumnDefinitions =
                {
                    new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) },
                    new ColumnDefinition { Width = new GridLength(8, GridUnitType.Star) }
                },
            RowDefinitions =
                {
                    new RowDefinition { Height = new GridLength(25) },
                    new RowDefinition { Height = new GridLength(1, GridUnitType.Star) },
                    new RowDefinition { Height = new GridLength(20) }
                },
            ColumnSpacing = 20,
            RowSpacing = 10
        };

        // Add manga title to grid
        grid.Add(new Label
        {
            Text = manga["attributes"]["title"]["en"].ToString(),
            HorizontalOptions = LayoutOptions.Start,
            VerticalOptions = LayoutOptions.Center,
            FontSize = 15,
            FontAttributes = FontAttributes.Bold,
            LineBreakMode = LineBreakMode.TailTruncation,
        }, 1, 0);

        string descriptionText = "No Description";
        if (manga["attributes"]["description"].ToString() != "{}")
        {
            descriptionText = manga["attributes"]["description"]["en"].ToString();
        }

        grid.Add(new Label
        {
            Text = descriptionText,
            FontSize = 14,
            LineBreakMode = LineBreakMode.WordWrap
        }, 1, 1);

        string coverFileName = "";
        var relations = manga["relationships"].AsArray();

        HorizontalStackLayout authorStack = new()
        {
            HorizontalOptions = LayoutOptions.Start,
            VerticalOptions = LayoutOptions.Center
        };

        Label authorLabel = new();
        for (int j = 0; j < relations.Count; j++)
        {

            if (relations[j]["type"].ToString() == "author")
            {
                string authorText = "";
                if (authorLabel.Text != null)
                {
                    authorText = ", " + relations[j]["attributes"]["name"].ToString();
                }
                else
                {
                    authorText = relations[j]["attributes"]["name"].ToString();
                }

                authorLabel = new Label
                {
                    Text = authorText,
                    Margin = new Thickness(0, 0, 0, 0),
                    VerticalOptions = LayoutOptions.Start
                };

                authorStack.Add(authorLabel);
            }
            else if (relations[j]["type"].ToString() == "artist")
            {
                NewArtist(relations[j]["attributes"]["name"].ToString(), authorLabel, authorStack);
            }
            else if (relations[j]["type"].ToString() == "cover_art")
            {
                coverFileName = relations[j]["attributes"]["fileName"].ToString();
            }
        }
        grid.Add(authorStack, 1, 2);

        var coverUrl = $"{coverBaseUrl}{manga["id"]}/{coverFileName}.512.jpg";

        Border border = new Border
        {
            Stroke = (Color)App.Current.Resources["fgColor"],
            HorizontalOptions = LayoutOptions.Center,
            VerticalOptions = LayoutOptions.Center,
            StrokeShape = new RoundRectangle
            {
                CornerRadius = 5
            },
            Content = new Image
            {
                Source = ImageSource.FromUri(new Uri(coverUrl)),
                HorizontalOptions = LayoutOptions.Center,
                VerticalOptions = LayoutOptions.Center
            }
        };
        Grid.SetRowSpan(border, 4);
        grid.Add(border, 0, 0);

        resultBorder.Content = grid;
    }

    private void NewArtist(string name, Label author, HorizontalStackLayout authorStack)
    {
        if (name == author.Text)
        {
            return;
        }

        Label label = new()
        {
            Text = ", " + name,
            Margin = new Thickness(0, 0, 0, 0),
            VerticalOptions = LayoutOptions.Start
        };
        authorStack.Add(label);
    }

    private void OnPointerEnterResult(object sender, EventArgs e)
    {
        ((Border)sender).BackgroundColor = (Color)App.Current.Resources["hlColor"];
    }

    private void OnPointerExitResult(object sender, EventArgs e)
    {
        ((Border)sender).BackgroundColor = (Color)App.Current.Resources["fgColor"];
    }
}