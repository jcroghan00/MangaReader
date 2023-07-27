namespace MangaReader;

using Flurl;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Controls.Shapes;
using System;
using System.Collections.ObjectModel;
using System.Text.Json.Nodes;
using Image = Image;

public partial class MainPage : ContentPage
{
	public static readonly HttpClient client = new();

	static readonly string baseUrl = "https://api.mangadex.org/";
	static readonly string coverBaseUrl = "https://uploads.mangadex.org/covers/";

    private JsonArray popularNewManga;
    readonly ObservableCollection<Grid> popularNew = new();

    public MainPage()
	{
		InitializeComponent();

        GetPopularNew();
        GetNewReleases();
	}

    private async void GetPopularNew()
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

        HttpRequestMessage msg = new(HttpMethod.Get, url);
        HttpResponseMessage res = await client.SendAsync(msg);
        res.EnsureSuccessStatusCode();

        var jNode = JsonNode.Parse(res.Content.ReadAsStream());
        popularNewManga = jNode["data"].AsArray();

        SetPopularNew();
    }

    private int popularNewIndex = 0;
    private void SetPopularNew()
	{
        if (popularNewManga == null)
        {
            return;
        }


        for (int i = 0; i < popularNewManga.Count; i++)
        {
            TapGestureRecognizer tapGestureRecognizer = new TapGestureRecognizer();
            tapGestureRecognizer.Tapped += FromNewToPage;
            tapGestureRecognizer.CommandParameter = popularNewManga[i]["id"].ToString();

            PointerGestureRecognizer pointerGestureRecognizer = new PointerGestureRecognizer();
            pointerGestureRecognizer.PointerEntered += OnPointerEnterTitle;
            pointerGestureRecognizer.PointerExited += OnPointerExitTitle;

            Grid popularNewGrid = new()
            {
                HeightRequest = 500,
                Padding = new Thickness(20, 10, 20, 10),
                ColumnDefinitions =
                {
                    new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) },
                    new ColumnDefinition { Width = new GridLength(3, GridUnitType.Star) }
                },
                RowDefinitions =
                {
                    new RowDefinition { Height = new GridLength(80) },
                    new RowDefinition { Height = new GridLength(1, GridUnitType.Auto) },
                    new RowDefinition { Height = new GridLength(1, GridUnitType.Star) },
                    new RowDefinition { Height = new GridLength(50) }
                },
                ColumnSpacing = 20,
                RowSpacing = 20
            };

            // Add manga title to grid
            popularNewGrid.Add(new Label
            {
                Text = popularNewManga[i]["attributes"]["title"]["en"].ToString(),
                HorizontalOptions = LayoutOptions.Start,
                VerticalOptions = LayoutOptions.Center,
                FontSize = 30,
                FontAttributes = FontAttributes.Bold,
                LineBreakMode = LineBreakMode.TailTruncation,
                GestureRecognizers =
            {
                tapGestureRecognizer,
                pointerGestureRecognizer
            }
            }, 1, 0);

            string popularNewDescriptionText = "No Description";
            if (popularNewManga[i]["attributes"]["description"].ToString() != "{}")
            {
                popularNewDescriptionText = popularNewManga[i]["attributes"]["description"]["en"].ToString();
            }

            popularNewGrid.Add(new ScrollView
            {
                Content = new Label
                {
                    Text = popularNewDescriptionText,
                    FontSize = 16,
                    LineBreakMode = LineBreakMode.WordWrap
                }
            }, 1, 2);

            string coverFileName = "";
            var relations = popularNewManga[i]["relationships"].AsArray();

            // < HorizontalStackLayout x: Name = "popularNewAuthorStack" Grid.Column = "0" HorizontalOptions = "Start" VerticalOptions = "Center" />
            HorizontalStackLayout authorStack = new()
            {
                HorizontalOptions = LayoutOptions.Start,
                VerticalOptions = LayoutOptions.Center
            };

            Label authorLabel = new();
            // Filters through manga relations to get the authors/artists and cover art id
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

            popularNewGrid.Add(authorStack, 1, 3);

            var tags = popularNewManga[i]["attributes"]["tags"].AsArray();
            HorizontalStackLayout popularNewTags = new()
            {
                Spacing = 15
            };

            // Adds a tag for the content rating of a manga if the manga is not 'safe'
            if (popularNewManga[i]["attributes"]["contentRating"].ToString() != "safe")
            {
                Border newTag = Tools.MakeNewTag(popularNewManga[i]["attributes"]["contentRating"].ToString());
                popularNewTags.Add(newTag);
            }

            // Create all tags for the manga
            for (int j = 0; j < tags.Count; j++)
            {
                Border newTag = Tools.MakeNewTag(tags[j]["attributes"]["name"]["en"].ToString());
                popularNewTags.Add(newTag);
            }

            popularNewGrid.Add(new ScrollView
            {
                Orientation = ScrollOrientation.Horizontal,
                Padding = new Thickness(0, 0, 0, 15),
                Content = popularNewTags
            }, 1, 1);

            var coverUrl = $"{coverBaseUrl}{popularNewManga[i]["id"]}/{coverFileName}.512.jpg";

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
            popularNewGrid.Add(border, 0, 0);

            popularNew.Add(popularNewGrid);
        }

        popularNewBorder.Content = popularNew[popularNewIndex];
        popularNewPageNumber.Text = (popularNewIndex + 1).ToString();

        //popularNewCarousel.ItemsSource = popularNew;
        //popularNewCarousel.SetBinding(ItemsView.ItemsSourceProperty, "popularNew");
    }

    private async void GetNewReleases()
    {
        var url = baseUrl
            .AppendPathSegment("chapter")
            .SetQueryParam("limit", 15)
            .SetQueryParam("contentRating[]", new[] { "safe", "suggestive" })
            .SetQueryParam("includes[]", new[] { "manga", "scanlation_group" })
            .SetQueryParam("translatedLanguage[]", "en");

        url += "&order%5BreadableAt%5D=desc"; // readableAt = asc

        HttpRequestMessage msg = new(HttpMethod.Get, url);
        HttpResponseMessage res = await client.SendAsync(msg);
        res.EnsureSuccessStatusCode();

        var jNode = JsonNode.Parse(res.Content.ReadAsStream());
        SetNewReleases(jNode["data"]);
    }

    private JsonNode coverFileNames;
    private static async Task<JsonNode> GetNewReleasesCovers(JsonNode data)
    {
        List<string> mangaIds = new();
        for (int i = 0; i < 15; i++)
        {
            var chapterRelations = data[i]["relationships"].AsArray();

            for (int j = 0; j < chapterRelations.Count; j++)
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

        HttpRequestMessage msg = new(HttpMethod.Get, url);
        HttpResponseMessage res = await client.SendAsync(msg);
        res.EnsureSuccessStatusCode();

        var jNode = JsonNode.Parse(res.Content.ReadAsStream());
        return jNode["data"];
    }

    private string GetCoverByMangaId(string mangaId)
    {
        for (int i = 0; i < 30; i++)
        {
            var coverRelations = coverFileNames[i]["relationships"].AsArray();

            for (int j = 0; j < coverRelations.Count; j++)
            {
                if (coverRelations[j]["type"].ToString() == "manga" && coverRelations[j]["id"].ToString() == mangaId)
                {
                    return coverFileNames[i]["attributes"]["fileName"].ToString();
                }
            }
        }
        return "";
    }

    private async void SetNewReleases(JsonNode data)
    {
        coverFileNames = await GetNewReleasesCovers(data);

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
            Border border = new()
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
            Grid grid = new()
            {
                BackgroundColor = (Color)App.Current.Resources["bgColor"],
                Padding = new Thickness(0, 0, 10, 0),
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
            HorizontalStackLayout scanGroupLayout = new()
            {
                Spacing = 5,
                VerticalOptions = LayoutOptions.Center,
                HorizontalOptions = LayoutOptions.Start
            };

            FontAttributes scanGroupAttributes = FontAttributes.Italic;

            // Parses the relationships of the current element to retrieve the title of the manga it is from as well as the mandaId that will later be used to find the manga's cover
            // There can be multiple scanlation groups acredited to a chapter, so if one has already been assigned to the 'scanGroup' variable it will just add a label for it, reassign the variable, and continue
            for (int j = 0; j < chapterRelations.Count; j++)
            {
                if (chapterRelations[j]["type"].ToString() == "manga")
                {
                    if (chapterRelations[j]["attributes"]["title"]["en"] != null)
                    {
                        mangaTitle = chapterRelations[j]["attributes"]["title"]["en"].ToString();
                    }
                    else if (chapterRelations[j]["attributes"]["title"]["ja"] != null)
                    {
                        mangaTitle = chapterRelations[j]["attributes"]["title"]["ja"].ToString();
                    }
                    else if (chapterRelations[j]["attributes"]["title"]["ja-ro"] != null)
                    {
                        mangaTitle = chapterRelations[j]["attributes"]["title"]["ja-ro"].ToString();
                    }
                    else
                    {
                        mangaTitle = chapterRelations[j]["attributes"]["title"]["ko"].ToString();
                    }
                    
                    mangaId = chapterRelations[j]["id"].ToString();
                }
                else if (chapterRelations[j]["type"].ToString() == "scanlation_group")
                {
                    if (scanGroup != "No Group")
                    {
                        PointerGestureRecognizer scanGroupPointer = new();
                        scanGroupPointer.PointerEntered += OnPointerEnterHighlight;
                        scanGroupPointer.PointerExited += OnPointerExitHighlight;
                        scanGroupLayout.Add(new Border
                        {
                            Stroke = (Color)App.Current.Resources["bgColor"],
                            BackgroundColor = (Color)App.Current.Resources["bgColor"],
                            StrokeShape = new RoundRectangle
                            {
                                CornerRadius = 5
                            },
                            Padding = new Thickness(5, 5, 5, 5),
                            GestureRecognizers =
                            {
                                scanGroupPointer
                            },
                            Content = new Label
                            {
                                Text = scanGroup,
                                FontAttributes = scanGroupAttributes,
                                LineBreakMode = LineBreakMode.TailTruncation
                            }
                        });
                    }

                    scanGroup = chapterRelations[j]["attributes"]["name"].ToString();
                    scanGroupAttributes = FontAttributes.None;
                }
            }

            // Adds a label for the manga title
            Label titleLabel = new()
            {
                Text = mangaTitle,
                Margin = new Thickness(5, 0, 0, 0),
                LineBreakMode = LineBreakMode.TailTruncation,
                FontAttributes = FontAttributes.Bold,
                VerticalOptions = LayoutOptions.Center,
                HorizontalOptions = LayoutOptions.Start
            };

            TapGestureRecognizer tapGestureRecognizer = new();
            tapGestureRecognizer.Tapped += Tools.ToMangaPage;
            tapGestureRecognizer.CommandParameter = mangaId;
            titleLabel.GestureRecognizers.Add(tapGestureRecognizer);

            PointerGestureRecognizer pointerGestureRecognizer = new();
            pointerGestureRecognizer.PointerEntered += OnPointerEnterTitle;
            pointerGestureRecognizer.PointerExited += OnPointerExitTitle;
            titleLabel.GestureRecognizers.Add(pointerGestureRecognizer);

            grid.Add(titleLabel, 1, 0);

            // If the chapter value is null, the chapter is a Oneshot, else it can be structured like 'Vol. {vol} Ch. {ch} - {chapter name}'
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

            // Creates the pointer gesture that highlights chapters and scan groups
            pointerGestureRecognizer = new();
            pointerGestureRecognizer.PointerEntered += OnPointerEnterHighlight;
            pointerGestureRecognizer.PointerExited += OnPointerExitHighlight;

            // Adds the chapter text to the grid
            grid.Add(new Border {
                Stroke = (Color)App.Current.Resources["bgColor"],
                BackgroundColor = (Color)App.Current.Resources["bgColor"],
                StrokeShape = new RoundRectangle
                {
                    CornerRadius = 5
                },
                Padding = new Thickness(5, 5, 5, 5),
                GestureRecognizers =
                {
                    pointerGestureRecognizer
                },
                Content = new Label
                {
                    Text = chapterText,
                    LineBreakMode = LineBreakMode.TailTruncation,
                },
                VerticalOptions = LayoutOptions.Center,
                HorizontalOptions = LayoutOptions.Start
            }, 1, 1);

            // Adds the scan group(s) to the grid
            scanGroupLayout.Add(new Border {
                Stroke = (Color)App.Current.Resources["bgColor"],
                BackgroundColor = (Color)App.Current.Resources["bgColor"],
                StrokeShape = new RoundRectangle
                {
                    CornerRadius = 5
                },
                Padding = new Thickness(5, 5, 5, 5),
                GestureRecognizers = { scanGroup != "No Group" ? pointerGestureRecognizer : null },
                Content = new Label
                {
                    Text = scanGroup,
                    FontAttributes = scanGroupAttributes,
                    LineBreakMode = LineBreakMode.TailTruncation
                }
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

            var coverFileName = GetCoverByMangaId(mangaId);

            // Sets the image to one that shows no image was retrieved or to the url for the cover image
            var imageSource = ImageSource.FromFile("no_image.png");
            if (coverFileName != "")
            {
                var coverUrl = $"{coverBaseUrl}{mangaId}/{coverFileName}.256.jpg";
                imageSource = ImageSource.FromUri(new Uri(coverUrl));
            }

            Border imageBorder = new()
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

    private void ScrollPopularNew(int offset)
    {
        popularNewIndex += offset;

        if (popularNewIndex >= popularNew.Count)
        {
            popularNewIndex = 0;
        }
        else if (popularNewIndex < 0)
        {
            popularNewIndex = popularNew.Count - 1;
        }

        popularNewBorder.Content = popularNew[popularNewIndex];
        popularNewPageNumber.Text = (popularNewIndex + 1).ToString();
    }

    private void ScrollPopularNewRight(object sender, EventArgs e)
    {
        ScrollPopularNew(1);
    }

    private void ScrollPopularNewLeft(object sender, EventArgs e)
    {
        ScrollPopularNew(-1);
    }

    private void OnPointerEnterTitle(object sender, EventArgs e)
    {
        ((Label)sender).TextDecorations = TextDecorations.Underline;
    }

    private void OnPointerExitTitle(object sender, EventArgs e)
    {
        ((Label)sender).TextDecorations = TextDecorations.None;
    }

    private void OnPointerEnterButton(object sender, EventArgs e)
    {
        ((Border)((Label)sender).Parent).BackgroundColor = (Color)App.Current.Resources["bgColor"];
        ((Border)((Label)sender).Parent).Stroke = (Color)App.Current.Resources["bgColor"];
    }

    private void OnPointerExitButton(object sender, EventArgs e)
    {
        ((Border)((Label)sender).Parent).BackgroundColor = (Color)App.Current.Resources["hlColor"];
        ((Border)((Label)sender).Parent).Stroke = (Color)App.Current.Resources["hlColor"];
    }

    private void OnPointerEnterHighlight(object sender, EventArgs e)
    {
        ((Border)sender).BackgroundColor = (Color)App.Current.Resources["fgColor"];
    }

    private void OnPointerExitHighlight(object sender, EventArgs e)
    {
        ((Border)sender).BackgroundColor = (Color)App.Current.Resources["bgColor"];
    }

    private void FromNewToPage(object sender, EventArgs e)
    {
        Tools.ToMangaPage(sender, e);
    }
}
