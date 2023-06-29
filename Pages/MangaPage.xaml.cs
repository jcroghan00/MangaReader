namespace MangaReader;

using Flurl;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Controls.Shapes;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;

[QueryProperty(nameof(MangaId), "mangaId")]
public partial class MangaPage : ContentPage
{
    private bool hasLoaded = false;
	private string mangaId = "No ID";
	public string MangaId
	{
		get => mangaId;
		set 
		{
			mangaId = value;
        }
	}

    static readonly string baseUrl = "https://api.mangadex.org/";
    static readonly string coverBaseUrl = "https://uploads.mangadex.org/covers/";

    public MangaPage()
	{
		InitializeComponent();
	}

    protected override void OnAppearing()
    {
        base.OnAppearing();

        if (!hasLoaded)
        {
            GetMangaById(mangaId);
        }
    }
        

	private async void GetMangaById(string mangaId)
	{
        var url = baseUrl
            .AppendPathSegment("manga")
            .AppendPathSegment($"/{mangaId}")
            .SetQueryParam("includes[]", new[] { "manga", "cover_art", "author", "artist" });

        HttpRequestMessage msg = new(HttpMethod.Get, url);
        HttpResponseMessage res = await MainPage.client.SendAsync(msg);
        res.EnsureSuccessStatusCode();

        var jNode = JsonNode.Parse(res.Content.ReadAsStream());
        JsonNode manga = jNode["data"];
        SetMangaElements(manga);
    }

    private async void GetMangaStats(string mangaId)
    {
        var url = baseUrl.AppendPathSegment($"statistics/manga/{mangaId}");

        HttpRequestMessage msg = new(HttpMethod.Get, url);
        HttpResponseMessage res = await MainPage.client.SendAsync(msg);
        res.EnsureSuccessStatusCode();

        var jNode = JsonNode.Parse(res.Content.ReadAsStream());
        JsonNode stats = jNode["statistics"];
        SetMangaStats(stats);
    }

    private void OnPageLoaded(object sender, EventArgs e)
    {
        if (!hasLoaded)
        {
            var content = scrollView.Content;
            scrollView.Content = null;
            scrollView.Content = content;

            GetMangaStats(mangaId);
            GetMangaChapters(mangaId, 0);
        }
        hasLoaded = true;
    }

    private async void GetMangaChapters(string mangaId, int offset)
    {
        var url = baseUrl
            .AppendPathSegment($"manga/{mangaId}/feed")
            .SetQueryParam("limit", 50)
            .SetQueryParam("offset", offset)
            .SetQueryParam("translatedLanguage[]", new[] { "en" })
            .SetQueryParam("contentRating[]", new[] {"safe", "suggestive", "erotica"})
            .SetQueryParam("includes[]", new[] {"scanlation_group", "user"});

        url += "&order%5Bvolume%5D=desc&order%5Bchapter%5D=desc";

        HttpRequestMessage msg = new(HttpMethod.Get, url);
        HttpResponseMessage res = await MainPage.client.SendAsync(msg);
        res.EnsureSuccessStatusCode();

        var jNode = JsonNode.Parse(res.Content.ReadAsStream());
        SetMangaChapters(jNode["data"]);
    }

    private void SetMangaElements(JsonNode manga)
    {
        if (manga["attributes"]["title"]["en"] != null)
        {
            mangaTitle.Text = manga["attributes"]["title"]["en"].ToString();
        }
        else
        {
            mangaTitle.Text = manga["attributes"]["title"]["ja"].ToString();
        }
        
        if (manga["attributes"]["description"].ToString() == "{}")
        {
            mangaDescription.Text = "No Description";
        }
        else
        {
            if (manga["attributes"]["description"]["en"] != null)
            {
                mangaDescription.Text = manga["attributes"]["description"]["en"].ToString();
            }
            else
            {
                mangaDescription.Text = manga["attributes"]["description"]["ja"].ToString();
            }
        }

        // Adds a tag for the content rating of a manga if the manga is not 'safe'
        if (manga["attributes"]["contentRating"].ToString() != "safe")
        {
            Border newTag = Tools.MakeNewTag(manga["attributes"]["contentRating"].ToString());
            newTag.Margin = new Thickness(5, 5, 5, 5);
            mangaTags.Add(newTag);
        }

        var tags = manga["attributes"]["tags"].AsArray();
        List<string> genres = new();
        List<string> themes = new();
        List<string> formats = new();
        // Create all tags for the manga
        for (int i = 0; i < tags.Count; i++)
        {
            Border newTag = Tools.MakeNewTag(tags[i]["attributes"]["name"]["en"].ToString());
            newTag.Margin = new Thickness(5, 5, 5, 5);
            mangaTags.Add(newTag);

            if (tags[i]["attributes"]["group"].ToString() == "genre")
            {
                genres.Add(tags[i]["attributes"]["name"]["en"].ToString());
            }
            else if (tags[i]["attributes"]["group"].ToString() == "theme")
            {
                themes.Add(tags[i]["attributes"]["name"]["en"].ToString());
            }
            else if (tags[i]["attributes"]["group"].ToString() == "format")
            {
                formats.Add(tags[i]["attributes"]["name"]["en"].ToString());
            }
        }

        var relations = manga["relationships"].AsArray();

        Label authorLabel = new();
        string coverFileName = "";
        List<string> authors = new();
        List<string> artists = new();

        // Filters through manga relations to get the authors/artists and cover art id
        for (int i = 0; i < relations.Count; i++)
        {

            if (relations[i]["type"].ToString() == "author")
            {
                authors.Add(relations[i]["attributes"]["name"].ToString());
                if (authorLabel.Text != null)
                {
                    authorLabel = new Label
                    {
                        Text = ", " + relations[i]["attributes"]["name"].ToString(),
                        FontSize = 14,
                        Margin = new Thickness(0, 0, 0, 0),
                        VerticalOptions = LayoutOptions.Start
                    };
                }
                else
                {
                    authorLabel = new Label
                    {
                        Text = relations[i]["attributes"]["name"].ToString(),
                        FontSize = 14,
                        Margin = new Thickness(0, 0, 0, 0),
                        VerticalOptions = LayoutOptions.Start
                    };
                }
                authorStack.Add(authorLabel);
            }
            else if (relations[i]["type"].ToString() == "artist")
            {
                artists.Add(relations[i]["attributes"]["name"].ToString());
                if (relations[i]["attributes"]["name"].ToString() == authorLabel.Text)
                {
                    continue;
                }

                Label label = new()
                {
                    Text = ", " + relations[i]["attributes"]["name"].ToString(),
                    FontSize = 14,
                    Margin = new Thickness(0, 0, 0, 0),
                    VerticalOptions = LayoutOptions.Start
                };
                authorStack.Add(label);
            }
            else if (relations[i]["type"].ToString() == "cover_art")
            {
                coverFileName = relations[i]["attributes"]["fileName"].ToString();
            }
        }

        mangaCover.Source = ImageSource.FromUri(new Uri($"{coverBaseUrl}{mangaId}/{coverFileName}"));
        mangaPublicationStatus.Text = $"Publication {manga["attributes"]["year"]}: {manga["attributes"]["status"].ToString()[0].ToString().ToUpper() + manga["attributes"]["status"].ToString()[1..]}";

        if (authors.Count > 1)
        {
            authorDetailText.Text = "Authors";
        }
        if (artists.Count > 1)
        {
            artistDetailText.Text = "Artists";
        }

        CreateAndAddDetail(authorDetail, authorDetailFlex, authors);
        CreateAndAddDetail(artistDetail, artistDetailFlex, artists);
        CreateAndAddDetail(formatDetail, formatDetailFlex, formats);
        CreateAndAddDetail(genreDetail, genreDetailFlex, genres);
        CreateAndAddDetail(themeDetail, themeDetailFlex, themes);

        if (manga["attributes"]["publicationDemographic"] != null)
        {
            demographicDetail.IsVisible = true;
            Border demographic = Tools.MakeNewTag(manga["attributes"]["publicationDemographic"].ToString());
            demographic.Stroke = (Color)App.Current.Resources["bgColor"];
            demographic.BackgroundColor = (Color)App.Current.Resources["bgColor"];
            demographic.Margin = new Thickness(2.5, 0, 2.5, 5);

            demographicDetailFlex.Add(demographic);
        }

        JsonArray altTitles = manga["attributes"]["altTitles"].AsArray();
        for (int i = 0; i < altTitles.Count; i++)
        {
            string title = altTitles[i].ToString();
            string[] titleHalves = title.Split("\": \"", StringSplitOptions.RemoveEmptyEntries);
            title = $"{titleHalves[0][6..]}: {titleHalves[1][..(titleHalves[1].Length - 4)]}";

            title = Regex.Unescape(title);

            alternativeTitleDetail.IsVisible = true;
            alternativeTitleDetailStack.Add(new Label
            {
                Text = title
            });
        }
    }

    private void SetMangaStats(JsonNode stats)
    {
        mangaScore.Text = $"☆ {Math.Round(float.Parse(stats[mangaId]["rating"]["average"].ToString()), 2, MidpointRounding.ToEven)}";
    }

    private void SetMangaChapters(JsonNode chaptersNode)
    {
        chapterStack.Children.Clear();
        var chapters = chaptersNode.AsArray();

        if (chapters.Count == 0 )
        {
            chapterStack.Add(new Label
            {
                Text = "No Chapters"
            });
            return;
        }

        VerticalStackLayout volumeLayout = new();
        string lastVolume;

        string volText = null;
        if (chapters[0]["attributes"]["volume"] == null)
        {
            volText = "No Volume";
            lastVolume = null;
        }
        else
        {
            volText = $"Volume {chapters[0]["attributes"]["volume"]}";
            lastVolume = chapters[0]["attributes"]["volume"].ToString();
        }

        volumeLayout.Add(new Label
        {
            Text = volText,
            FontSize = 24,
            Margin = new Thickness(10, 0, 0, 10)
        });
        volumeLayout.Spacing = 10;

        for (int i = 0; i < chapters.Count; i++) 
        {
            if (chapters[i]["attributes"]["volume"] != null && lastVolume != chapters[i]["attributes"]["volume"].ToString() )
            {
                lastVolume = chapters[i]["attributes"]["volume"].ToString();
                chapterStack.Add(volumeLayout);
                volumeLayout = new();
                volumeLayout.Spacing = 10;
                volumeLayout.Add(new Label
                {
                    Text = $"Volume {lastVolume}",
                    FontSize = 24,
                    Margin = new Thickness(10, 0, 0, 10)
                });
            }

            TapGestureRecognizer tapGestureRecognizer = new();
            tapGestureRecognizer.Tapped += Tools.ToChapterPage;
            tapGestureRecognizer.CommandParameter = chapters[i]["id"].ToString();

            PointerGestureRecognizer chapterPointerGestureRecognizer = new();
            chapterPointerGestureRecognizer.PointerEntered += OnPointerEnterChapter;
            chapterPointerGestureRecognizer.PointerExited += OnPointerExitChapter;

            Border chapterBorder = new Border
            {
                Stroke = (Color)App.Current.Resources["hlColor"],
                BackgroundColor = (Color)App.Current.Resources["hlColor"],
                Padding = new Thickness(10, 10, 10, 10),
                GestureRecognizers =
                {
                    chapterPointerGestureRecognizer,
                    tapGestureRecognizer
                },
                StrokeShape = new RoundRectangle
                {
                    CornerRadius = 5
                }
            };

            Grid chapterGrid = new Grid
            {
                ColumnDefinitions = {
                        new ColumnDefinition(),
                        new ColumnDefinition{ Width = new GridLength(1, GridUnitType.Auto) }
                    },
                RowDefinitions =
                    {
                        new RowDefinition(),
                        new RowDefinition()
                    },
                RowSpacing = 5
            };

            string chapterTitle = "Oneshot";

            if (chapters[i]["attributes"]["chapter"] != null)
            {
                chapterTitle = $"Ch. {chapters[i]["attributes"]["chapter"]}";
            }

            chapterTitle += chapters[i]["attributes"]["title"] != null ? $" - {chapters[i]["attributes"]["title"]}" : "";

            chapterGrid.Add(new Label
            {
                Text = chapterTitle,
                Margin = new Thickness(5, 5, 0, 0),
                FontSize = 14
            }, 0, 0);

            DateTime readableAt = DateTime.Parse(chapters[i]["attributes"]["readableAt"].ToString());
            TimeSpan timeDifference = DateTime.Now - readableAt;

            string timeReadable = "No Time Readable";
            if (timeDifference.TotalDays / 365.2425 >= 1)
            {
                timeReadable = (int)(timeDifference.TotalDays / 365.2425) == 1 ? (int)(timeDifference.TotalDays / 365.2425) + " Year Ago" : (int)(timeDifference.TotalDays / 365.2425) + " Years Ago";
            }
            else if (timeDifference.TotalDays / 30.437 >= 1)
            {
                timeReadable = (int)(timeDifference.TotalDays / 30.437) == 1 ? (int)(timeDifference.TotalDays / 30.437) + " Month Ago" : (int)(timeDifference.TotalDays / 30.437) + " Months Ago";
            }
            else if (timeDifference.Days > 0)
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

            chapterGrid.Add(new Label {
                Text = timeReadable,
                HorizontalTextAlignment = TextAlignment.End,
                FontSize = 14
            }, 1, 0);

            var chapterRelations = chapters[i]["relationships"].AsArray();

            string postUser = "No User";
            string scanGroup = "No Group";

            HorizontalStackLayout scanGroupLayout = new()
            {
                Spacing = 5,
                VerticalOptions = LayoutOptions.Center,
                HorizontalOptions = LayoutOptions.Start
            };

            FontAttributes scanGroupAttributes = FontAttributes.Italic;

            for (int j = 0; j < chapterRelations.Count; j++)
            {
                if (chapterRelations[j]["type"].ToString() == "user")
                {
                    if (chapterRelations[j]["attributes"]["username"] != null)
                    {
                        postUser = chapterRelations[j]["attributes"]["username"].ToString();
                    }
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
                            Stroke = null,
                            BackgroundColor = null,
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
                                LineBreakMode = LineBreakMode.TailTruncation,
                                FontSize = 14
                            }
                        });
                    }

                    scanGroup = chapterRelations[j]["attributes"]["name"].ToString();
                    scanGroupAttributes = FontAttributes.None;
                }
            }

            PointerGestureRecognizer pointerGestureRecognizer = new();
            pointerGestureRecognizer.PointerEntered += OnPointerEnterHighlight;
            pointerGestureRecognizer.PointerExited += OnPointerExitHighlight;
            scanGroupLayout.Add(new Border
            {
                Stroke = null,
                BackgroundColor = null,
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
                    LineBreakMode = LineBreakMode.TailTruncation,
                    FontSize = 14
                }
            });

            chapterGrid.Add(scanGroupLayout, 0, 1);

            chapterGrid.Add(new Border
            {
                Stroke = null,
                BackgroundColor = null,
                StrokeShape = new RoundRectangle
                {
                    CornerRadius = 5
                },
                Padding = new Thickness(5, 5, 5, 5),
                GestureRecognizers = { postUser != "No User" ? pointerGestureRecognizer : null },
                HorizontalOptions = LayoutOptions.End,
                Content = new Label
                {
                    Text = postUser,
                    LineBreakMode = LineBreakMode.TailTruncation,
                    HorizontalTextAlignment = TextAlignment.End,
                    FontSize = 14
                }
            }, 1, 1);

            chapterBorder.Content = chapterGrid;
            volumeLayout.Add(chapterBorder);
        }
        chapterStack.Add(volumeLayout);
    }

    private static void CreateAndAddDetail(Border overLayout, FlexLayout layout, List<string> input)
    {
        for (int i = 0; i < input.Count; i++)
        {
            overLayout.IsVisible = true;
            Border border = Tools.MakeNewTag(input[i]);
            border.Stroke = (Color)App.Current.Resources["bgColor"];
            border.BackgroundColor = (Color)App.Current.Resources["bgColor"];
            border.Margin = new Thickness(2.5, 0, 2.5, 5);

            layout.Add(border);
        }
    }

    private void OnPointerEnterHighlight(object sender, EventArgs e)
    {
        ((Border)sender).BackgroundColor = (Color)App.Current.Resources["hlColor"];
    }

    private void OnPointerExitHighlight(object sender, EventArgs e)
    {
        ((Border)sender).BackgroundColor = null;
    }

    private void OnPointerEnterChapter(object sender, EventArgs e)
    {
        ((Border)sender).BackgroundColor = (Color)App.Current.Resources["fgColor"];
    }

    private void OnPointerExitChapter(object sender, EventArgs e)
    {
        ((Border)sender).BackgroundColor = (Color)App.Current.Resources["hlColor"];
    }
}