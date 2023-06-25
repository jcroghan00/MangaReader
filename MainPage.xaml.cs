namespace MangaReader;

using Flurl;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Controls.Shapes;
using System;
using System.IO;
using System.Text.Json.Nodes;

public partial class MainPage : ContentPage
{
	static readonly HttpClient client = new HttpClient();

	static string baseUrl = "https://api.mangadex.org/";
	static string coverBaseUrl = "https://uploads.mangadex.org/covers/";

    private JsonNode popularNewManga;
    private byte[][] popularNewImages = new byte[10][];

    private JsonNode recentChapters;

    public MainPage()
	{
		InitializeComponent();

		getPopularNew();
        setNewReleases();
	}

    private async void getPopularNew()
	{
        var dt = DateTime.Now;
        dt = dt.AddDays(-31);

        // Used Flurl for Url building since it was difficult to fill in the rather complex queries that
        // mangadex expects with c#s default Url building
        var url = baseUrl
            .AppendPathSegment("manga")
            .SetQueryParam("includes[]", new[] { "manga", "cover_art", "author", "artist" })
            .SetQueryParam("contentRating[]", new[] { "safe", "suggestive" })
            .SetQueryParam("createdAtSince", dt.ToString("yyyy-MM-ddTHH:mm:ss"))
            .SetQueryParam("availableTranslatedLanguage[]", "en");

        // This kinda sucks but it will be how I have to do orderings here and in the future since mangadex
        // expects an object as a parameter and both Flurl and c# don't make that easy
        url += "&order%5BfollowedCount%5D=desc";

        HttpRequestMessage msg = new HttpRequestMessage(HttpMethod.Get, url);
        HttpResponseMessage res = await client.SendAsync(msg);
        res.EnsureSuccessStatusCode();

        var jNode = JsonNode.Parse(res.Content.ReadAsStream());
        popularNewManga = jNode["data"];

        setPopularNew(0);
    }

    private int popularNewIndex = 0;
	private void setPopularNew(int offset)
	{
        if (popularNewManga == null)
        {
            return;
        }

        popularNewIndex += offset;

        if (popularNewIndex < 0)
        {
            popularNewIndex = 9;
        }
        else if (popularNewIndex >= 10) 
        {
            popularNewIndex = 0;
        }

        // Retrieve stored image data
        if (popularNewImages[popularNewIndex] != null)
        {
            popularNewImage.Source = ImageSource.FromStream(() => new MemoryStream(popularNewImages[popularNewIndex]));
        }
        else
        {
            popularNewImage.Source = "no_image.png";
        }

        // Clear old info
        popularNewTags.Children.Clear();
        popularNewAuthorStack.Children.Clear();

        popularNewPageNumber.Text = (popularNewIndex + 1).ToString();
        popularNewTitle.Text = popularNewManga[popularNewIndex]["attributes"]["title"]["en"].ToString();

        if (popularNewManga[popularNewIndex]["attributes"]["description"].ToString() == "{}")
        {
            popularNewDescription.Text = "No Description";
        }
        else
        {
            popularNewDescription.Text = popularNewManga[popularNewIndex]["attributes"]["description"]["en"].ToString();
        }

        string coverFileName = "";
        var relations = popularNewManga[popularNewIndex]["relationships"].AsArray();

        Label authorLabel = new Label();
        // Filters through manga relations to get the authors/artists and cover art id
        for (int i = 0; i < relations.Count(); i++)
        {
            
            if (relations[i]["type"].ToString() == "author")
            {
                if (authorLabel.Text != null)
                {
                    authorLabel = new Label
                    {
                        Text = ", " + relations[i]["attributes"]["name"].ToString(),
                        Margin = new Thickness(0, 0, 0, 0),
                        VerticalOptions = LayoutOptions.Start
                    };
                }
                else
                {
                    authorLabel = new Label
                    {
                        Text = relations[i]["attributes"]["name"].ToString(),
                        Margin = new Thickness(0, 0, 0, 0),
                        VerticalOptions = LayoutOptions.Start
                    };
                }
                popularNewAuthorStack.Add(authorLabel);
            }
            else if (relations[i]["type"].ToString() == "artist")
            {
                newArtist(relations[i]["attributes"]["name"].ToString(), authorLabel);
            }
            else if (relations[i]["type"].ToString() == "cover_art")
            {
                coverFileName = relations[i]["attributes"]["fileName"].ToString();
            }
        }

        var tags = popularNewManga[popularNewIndex]["attributes"]["tags"].AsArray();

        // Adds a tag for the content rating of a manga if the manga is not 'safe'
        if (popularNewManga[popularNewIndex]["attributes"]["contentRating"].ToString() != "safe")
        {
            Border newTag = makeNewTag(popularNewManga[popularNewIndex]["attributes"]["contentRating"].ToString());
            popularNewTags.Add(newTag);
        }

        // Create all tags for the manga
        for (int i = 0; i < tags.Count(); i++)
        {
            Border newTag = makeNewTag(tags[i]["attributes"]["name"]["en"].ToString());
            popularNewTags.Add(newTag);
        }

        // Get new image data
        if (popularNewImages[popularNewIndex] == null)
        {
            var coverUrl = $"{coverBaseUrl}{popularNewManga[popularNewIndex]["id"]}/{coverFileName}.512.jpg";
            var byteArray = client.GetByteArrayAsync(coverUrl).Result;

            popularNewImages[popularNewIndex] = byteArray;
            popularNewImage.Source = ImageSource.FromStream(() => new MemoryStream(popularNewImages[popularNewIndex]));
        }
    }

    private async Task<JsonNode> getNewReleases()
    {
        var url = baseUrl
            .AppendPathSegment("chapter")
            .SetQueryParam("limit", 15)
            .SetQueryParam("contentRating[]", new[] { "safe", "suggestive" })
            .SetQueryParam("includes[]", new[] { "manga", "scanlation_group" })
            .SetQueryParam("translatedLanguage[]", "en");

        url += "&order%5BreadableAt%5D=desc"; // readableAt = asc

        HttpRequestMessage msg = new HttpRequestMessage(HttpMethod.Get, url);
        HttpResponseMessage res = await client.SendAsync(msg);
        res.EnsureSuccessStatusCode();

        var jNode = JsonNode.Parse(res.Content.ReadAsStream());
        recentChapters = jNode["data"];

        return recentChapters;
    }

    private async void setNewReleases()
    {
        JsonNode data = await getNewReleases();

        for (int i = 0; i < 5; i++)
        {
            Border border = new Border
            {
                BackgroundColor = (Color)App.Current.Resources["bgColor"],
                Stroke = (Color)App.Current.Resources["bgColor"],
                StrokeThickness = 0,
                HeightRequest = (newChapterStack1.Height - 20 - (4 * 5)) / 5,
                Margin = new Thickness(5, 2.5, 5, 2.5),
                StrokeShape = new RoundRectangle
                {
                    CornerRadius = 5
                }
            };

            Grid grid = new Grid
            {
                BackgroundColor = (Color)App.Current.Resources["bgColor"],
                ColumnDefinitions =
                    {
                        new ColumnDefinition { Width = new GridLength(1, GridUnitType.Auto) },
                        new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) }
                    },
                RowDefinitions =
                    {
                        new RowDefinition { Height = new GridLength(1, GridUnitType.Star) },
                        new RowDefinition { Height = new GridLength(1, GridUnitType.Star) },
                        new RowDefinition { Height = new GridLength(1, GridUnitType.Star) }
                    },
            };

            Border imageBorder = new Border
            {
                BackgroundColor = (Color)App.Current.Resources["bgColor"],
                Stroke = (Color)App.Current.Resources["bgColor"],
                StrokeThickness = 0,
                HeightRequest = (newChapterStack1.Height - 20 - (4 * 5)) / 5,
                Margin = new Thickness(5, 5, 5, 5),
                VerticalOptions = LayoutOptions.Center,
                HorizontalOptions = LayoutOptions.Center,
                WidthRequest = 80,
                StrokeShape = new RoundRectangle
                {
                    CornerRadius = 5
                },
                Content = new Image
                {
                    Source = "manga_test.jpg",
                    VerticalOptions = LayoutOptions.Center,
                    HorizontalOptions = LayoutOptions.Center
                }
            };

            Grid.SetRowSpan(imageBorder, 3);
            grid.Add(imageBorder, 0, 0);

            var chapterRelations = data[i]["relationships"].AsArray();

            string mangaTitle = "Manga Title";
            string scanGroup = "No Group";

            FontAttributes scanGroupAttributes = FontAttributes.Italic;

            for (int j = 0; j < chapterRelations.Count(); j++)
            {

                if (chapterRelations[j]["type"].ToString() == "manga")
                {
                    mangaTitle = chapterRelations[j]["attributes"]["title"]["en"].ToString();
                }
                else if (chapterRelations[j]["type"].ToString() == "scanlation_group")
                {
                    scanGroup = chapterRelations[j]["attributes"]["name"].ToString();
                    scanGroupAttributes = FontAttributes.None;
                }
            }

            grid.Add(new Label 
            {
                Text = mangaTitle,
                LineBreakMode = LineBreakMode.TailTruncation,
                FontAttributes = FontAttributes.Bold,
                VerticalOptions = LayoutOptions.Center,
                HorizontalOptions = LayoutOptions.Start
            }, 1, 0);

            string chapterText = data[i]["attributes"]["volume"] != null ? $"Vol. {data[i]["attributes"]["volume"]} " : "";
            chapterText += $"Ch. {data[i]["attributes"]["chapter"]}";
            chapterText += data[i]["attributes"]["title"] != null ? $" - {data[i]["attributes"]["title"]}" : "";

            grid.Add(new Label 
            { 
                Text = chapterText,
                LineBreakMode = LineBreakMode.TailTruncation,
                VerticalOptions = LayoutOptions.Center,
                HorizontalOptions = LayoutOptions.Start
            }, 1, 1);

            grid.Add(new Grid
            {
                VerticalOptions = LayoutOptions.Center,
                Padding = new Thickness(0, 0, 10, 0),
                Children =
                {
                    new Label
                    {
                        Text = scanGroup,
                        FontAttributes = scanGroupAttributes,
                        VerticalOptions = LayoutOptions.Center,
                        HorizontalOptions = LayoutOptions.Start
                    },
                    new Label
                    {
                        Text = "Time Readable",
                        VerticalOptions = LayoutOptions.Center,
                        HorizontalOptions = LayoutOptions.End
                    }
                }
            }, 1, 2);

            border.Content = grid;
            newChapterStack1.Add(border);
        }
    }

    private Border makeNewTag(string tag)
    {
        string text = tag;
        Color backgroundColor = (Color) App.Current.Resources["hlColor"];

        if (tag == "suggestive" || tag == "erotica")
        {
            text = tag[0].ToString().ToUpper() + tag.Substring(1);
            backgroundColor = (Color) App.Current.Resources[tag + "BgColor"];
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

    private void newArtist(string name, Label author)
    {
        if (name == author.Text)
        {
            return;
        }

        Label label = new Label
        {
            Text = ", " + name,
            Margin = new Thickness(0, 0, 0, 0),
            VerticalOptions = LayoutOptions.Start
        };
        popularNewAuthorStack.Add(label);
    }

    private void scrollPopularNewRight(object sender, EventArgs e)
    {
        setPopularNew(1);
    }

    private void scrollPopularNewLeft(object sender, EventArgs e)
    {
        setPopularNew(-1);
    }

    private void test(object sender, EventArgs e)
    {
        popularNewTitle.Text = "hehe";
    }
}
