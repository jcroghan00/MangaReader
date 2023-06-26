namespace MangaReader;

using Flurl;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Controls.Shapes;
using System;
using System.IO;
using System.Text.Json.Nodes;

public partial class MainPage : ContentPage
{
	public static readonly HttpClient client = new HttpClient();

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
        popularNewTitle.ClassId = popularNewManga[popularNewIndex]["id"].ToString();

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
            Border newTag = Tools.makeNewTag(popularNewManga[popularNewIndex]["attributes"]["contentRating"].ToString());
            popularNewTags.Add(newTag);
        }

        // Create all tags for the manga
        for (int i = 0; i < tags.Count(); i++)
        {
            Border newTag = Tools.makeNewTag(tags[i]["attributes"]["name"]["en"].ToString());
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

    private JsonNode coverFileNames;
    private async Task<JsonNode> getNewReleasesCovers(JsonNode data)
    {
        List<string> mangaIds = new List<string>();
        for (int i = 0; i < 15; i++)
        {
            var chapterRelations = data[i]["relationships"].AsArray();

            for (int j = 0; j < chapterRelations.Count(); j++)
            {
                if (chapterRelations[j]["type"].ToString() == "manga")
                {
                    mangaIds.Add(chapterRelations[j]["id"].ToString());
                }
            }
        }

        var url = baseUrl
            .AppendPathSegment("cover")
            .SetQueryParam("limit", 30)
            .SetQueryParam("manga[]", mangaIds.ToArray());

        HttpRequestMessage msg = new HttpRequestMessage(HttpMethod.Get, url);
        HttpResponseMessage res = await client.SendAsync(msg);
        res.EnsureSuccessStatusCode();

        var jNode = JsonNode.Parse(res.Content.ReadAsStream());
        return jNode["data"];
    }

    private string getCoverByMangaId(string mangaId)
    {
        for (int i = 0; i < 30; i++)
        {
            var coverRelations = coverFileNames[i]["relationships"].AsArray();

            for (int j = 0; j < coverRelations.Count(); j++)
            {
                if (coverRelations[j]["type"].ToString() == "manga" && coverRelations[j]["id"].ToString() == mangaId)
                {
                    return coverFileNames[i]["attributes"]["fileName"].ToString();
                }
            }
        }
        return "";
    }

    private async void setNewReleases()
    {
        JsonNode data = await getNewReleases();
        coverFileNames = await getNewReleasesCovers(data);

        VerticalStackLayout currStack = newChapterStack1;
        for (int i = 0; i < 15; i++)
        {
            if (i == 5)
            {
                currStack = newChapterStack2;
            }
            else if ( i == 10)
            {
                currStack = newChapterStack3;
            }

            // Border around the individual element
            Border border = new Border
            {
                BackgroundColor = (Color)App.Current.Resources["bgColor"],
                Stroke = (Color)App.Current.Resources["bgColor"],
                StrokeThickness = 0,
                HeightRequest = (currStack.Height - 20 - (4 * 5)) / 5,
                Margin = new Thickness(5, 2.5, 5, 2.5),
                StrokeShape = new RoundRectangle
                {
                    CornerRadius = 5
                }
            };
            
            // Grid that structures each new chapter
            Grid grid = new Grid
            {
                BackgroundColor = (Color)App.Current.Resources["bgColor"],
                ColumnDefinitions =
                    {
                        new ColumnDefinition { Width = new GridLength(80) },
                        new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) }
                    },
                RowDefinitions =
                    {
                        new RowDefinition { Height = new GridLength(1, GridUnitType.Star) },
                        new RowDefinition { Height = new GridLength(1, GridUnitType.Star) },
                        new RowDefinition { Height = new GridLength(1, GridUnitType.Star) }
                    },
            };

            var chapterRelations = data[i]["relationships"].AsArray();

            string mangaTitle = "Manga Title";
            string mangaId = "";
            string scanGroup = "No Group";

            // There is a chance that there are multiple groups who work on a chapter so this allows for stacking labels for each group
            HorizontalStackLayout scanGroupLayout = new HorizontalStackLayout
            {
                Spacing = 10,
                VerticalOptions = LayoutOptions.Center,
                HorizontalOptions = LayoutOptions.Start
            };

            FontAttributes scanGroupAttributes = FontAttributes.Italic;

            // Parses the relationships of the current element to retrieve the title of the manga it is from as well as the mandaId that will later be used to find the manga's cover
            // There can be multiple scanlation groups acredited to a chapter, so if one has already been assigned to the 'scanGroup' variable it will just add a label for it, reassign the variable, and continue
            for (int j = 0; j < chapterRelations.Count(); j++)
            {
                if (chapterRelations[j]["type"].ToString() == "manga")
                {
                    mangaTitle = chapterRelations[j]["attributes"]["title"]["en"].ToString();
                    mangaId = chapterRelations[j]["id"].ToString();
                }
                else if (chapterRelations[j]["type"].ToString() == "scanlation_group")
                {
                    if (scanGroup != "No Group")
                    {
                        scanGroupLayout.Add(new Label
                        {
                            Text = scanGroup,
                            VerticalOptions = LayoutOptions.Center,
                        });
                    }

                    scanGroup = chapterRelations[j]["attributes"]["name"].ToString();
                    scanGroupAttributes = FontAttributes.None;
                }
            }

            // Adds a label for the manga title
            Label titleLabel = new Label 
            {
                Text = mangaTitle,
                ClassId = mangaId,
                LineBreakMode = LineBreakMode.TailTruncation,
                FontAttributes = FontAttributes.Bold,
                VerticalOptions = LayoutOptions.Center,
                HorizontalOptions = LayoutOptions.Start
            };

            TapGestureRecognizer gestureRecognizer = new TapGestureRecognizer();
            gestureRecognizer.Tapped += (s, e) =>
            {
                toMangaPage(s, e);
            };
            titleLabel.GestureRecognizers.Add(gestureRecognizer);
            grid.Add(titleLabel, 1, 0);

            // If the chapter value is null, the chapter is a Oneshot, else it can be structured like 'Vol. {vol} Ch. {ch} - {chapter came}'
            string chapterText = "";
            if (data[i]["attributes"]["chapter"] == null)
            {
                chapterText = "Oneshot";
            }
            else
            {
                chapterText = data[i]["attributes"]["volume"] != null ? $"Vol. {data[i]["attributes"]["volume"]} " : "";
                chapterText += $"Ch. {data[i]["attributes"]["chapter"]}";
                chapterText += data[i]["attributes"]["title"] != null ? $" - {data[i]["attributes"]["title"]}" : "";
            }

            // Adds the chapter text to the grid
            grid.Add(new Label 
            { 
                Text = chapterText,
                LineBreakMode = LineBreakMode.TailTruncation,
                VerticalOptions = LayoutOptions.Center,
                HorizontalOptions = LayoutOptions.Start
            }, 1, 1);

            // Adds the scan group(s) to the grid
            scanGroupLayout.Add(new Label
            {
                Text = scanGroup,
                FontAttributes = scanGroupAttributes,
                VerticalOptions = LayoutOptions.Center,
            });

            DateTime readableAt = DateTime.Parse(data[i]["attributes"]["readableAt"].ToString());
            TimeSpan timeDifference = DateTime.Now - readableAt;

            string timeReadable = "No Time Readable";
            if (timeDifference.Days > 0)
            {
                timeReadable = timeDifference.Days == 1 ? timeDifference.Days.ToString() + " Day Ago" : timeDifference.Days.ToString() + " Days Ago";
            }
            else if (timeDifference.Hours > 0)
            {
                timeReadable = timeDifference.Hours == 1 ? timeDifference.Hours.ToString() + " Hour Ago" : timeDifference.Hours.ToString() + " Hours Ago";
            }
            else if (timeDifference.Minutes > 0)
            {
                timeReadable = timeDifference.Minutes == 1 ? timeDifference.Minutes.ToString() + " Minute Ago" : timeDifference.Minutes.ToString() + " Minutes Ago";
            }
            else if (timeDifference.Seconds > 0)
            {
                timeReadable = timeDifference.Seconds == 1 ? timeDifference.Seconds.ToString() + " Second Ago" : timeDifference.Seconds.ToString() + " Seconds Ago";
            }

            // Adds the time readable to the grid
            grid.Add(new Grid
            {
                VerticalOptions = LayoutOptions.Center,
                Padding = new Thickness(0, 0, 10, 0),
                Children =
                {
                    scanGroupLayout,
                    new Label
                    {
                        Text = timeReadable,
                        VerticalOptions = LayoutOptions.Center,
                        HorizontalOptions = LayoutOptions.End
                    }
                }
            }, 1, 2);

            var coverFileName = getCoverByMangaId(mangaId);

            // Sets the image to one that shows no image was retrieved or to the url for the cover image
            var imageSource = ImageSource.FromFile("no_image.png");
            if (coverFileName != "")
            {
                var coverUrl = $"{coverBaseUrl}{mangaId}/{coverFileName}.256.jpg";
                imageSource = ImageSource.FromUri(new Uri(coverUrl));
            }

            Border imageBorder = new Border
            {
                BackgroundColor = (Color)App.Current.Resources["bgColor"],
                Stroke = (Color)App.Current.Resources["bgColor"],
                StrokeThickness = 0,
                Margin = new Thickness(5, 5, 5, 5),
                VerticalOptions = LayoutOptions.Center,
                HorizontalOptions = LayoutOptions.Center,
                StrokeShape = new RoundRectangle
                {
                    CornerRadius = 5
                },
                Content = new Image
                {
                    Source = imageSource
                }
            };

            Grid.SetRowSpan(imageBorder, 3);
            grid.Add(imageBorder, 0, 0);

            border.Content = grid;
            currStack.Add(border);
        }
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

    private async void toMangaPage(object sender, EventArgs e)
    {
        string mangaId = ((Label)sender).ClassId;
        await Shell.Current.GoToAsync($"MangaPage?mangaId={mangaId}");
    }
}
