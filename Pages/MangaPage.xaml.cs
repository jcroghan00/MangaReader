namespace MangaReader;

using Flurl;
using MangaReader.Views;
using Microsoft.Maui.Controls;
using System.Diagnostics;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;

[QueryProperty(nameof(MangaId), "mangaId")]
public partial class MangaPage : ContentPage
{
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

    private int chapterPage = 0;
	private async void GetMangaById()
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

    private async void GetMangaStats()
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
        // var content = scrollView.Content;
        // scrollView.Content = null;
        // scrollView.Content = content;

        GetMangaById();
        GetMangaStats();
        SetMangaChapters();
    }

    int totalChapterEntries;
    private async Task<JsonNode> GetMangaChapters()
    {
        int offset = 25 * chapterPage;
        var url = baseUrl
            .AppendPathSegment($"manga/{mangaId}/feed")
            .SetQueryParam("limit", 25)
            .SetQueryParam("offset", offset)
            .SetQueryParam("translatedLanguage[]", new[] { "en" })
            .SetQueryParam("contentRating[]", new[] {"safe", "suggestive", "erotica"})
            .SetQueryParam("includes[]", new[] {"scanlation_group", "user"});

        url += "&order%5Bvolume%5D=desc&order%5Bchapter%5D=desc";

        HttpRequestMessage msg = new(HttpMethod.Get, url);
        HttpResponseMessage res = await MainPage.client.SendAsync(msg);
        res.EnsureSuccessStatusCode();

        var jNode = JsonNode.Parse(res.Content.ReadAsStream());
        totalChapterEntries = Int32.Parse(jNode["total"].ToString());

        return jNode["data"];
    }

    private bool longstrip = false;
    private void SetMangaElements(JsonNode manga)
    {
        if (manga["attributes"]["title"]["en"] != null)
        {
            mangaTitle.Text = manga["attributes"]["title"]["en"].ToString();
        }
        else if (manga["attributes"]["title"]["ja"] != null)
        {
            mangaTitle.Text = manga["attributes"]["title"]["ja"].ToString();
        }
        else if (manga["attributes"]["title"]["ja-ro"] != null)
        {
            mangaTitle.Text = manga["attributes"]["title"]["ja-ro"].ToString();
        }
        else
        {
            mangaTitle.Text = manga["attributes"]["title"]["ko"].ToString();
        }

        string mangaDescriptionText;
        if (manga["attributes"]["description"].ToString() == "{}")
        {
            mangaDescriptionText = "No Description";
        }
        else
        {
            if (manga["attributes"]["description"]["en"] != null)
            {
                mangaDescriptionText = manga["attributes"]["description"]["en"].ToString();
            }
            else
            {
                mangaDescriptionText = manga["attributes"]["description"]["ja"].ToString();
            }
        }

        // <Label x:Name="mangaDescription" FontSize="16" LineBreakMode="WordWrap"/>
        // Parse the description for links to hyperlink
        var linkParser = new Regex(@"\b(?:https?://|www\.)\S+\b", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        FormattedString formattedString = new FormattedString();
        int lastIndex = 0;
        foreach (Match m in linkParser.Matches(mangaDescriptionText))
        {
            string linkUrl = m.Value.ToString();
            int linkIndex = m.Index;

            string firstPart = mangaDescriptionText.Substring(lastIndex, linkIndex - lastIndex - 2);
            int bracketIndex = firstPart.LastIndexOf("[");

            formattedString.Spans.Add(new Span { Text = firstPart.Substring(0, bracketIndex) });

            string linkText = firstPart.Substring(bracketIndex + 1, firstPart.Length - (bracketIndex + 1));

            // This may one day actually function but unfortunately gesture recognizers don't work on spans. It's a bug in .net MAUI and has been fixed on Android and iOS already.
            // The link to the issue is https://github.com/dotnet/maui/issues/4734
            TapGestureRecognizer linkTap = new();
            linkTap.Tapped += OpenBrowserWithLink;
            linkTap.CommandParameter = linkUrl;

            // PointerGestureRecognizer linkPoint = new();
            // linkPoint.PointerEntered += OnPointerEnterTitle;
            // linkPoint.PointerExited += OnPointerExitTitle;

            formattedString.Spans.Add(new Span
            {
                Text = linkText,
                TextColor = Color.FromArgb("#ea00ff"),
                GestureRecognizers =
                {
                    linkTap,
                }
            });

            lastIndex = linkIndex + linkUrl.Length + 1;
        }

        TapGestureRecognizer testGesture = new();
        testGesture.Tapped += Test;

        formattedString.Spans.Add(new Span { Text = mangaDescriptionText.Substring(lastIndex, mangaDescriptionText.Length -  lastIndex) });   
        mangaDescription.Content = new Label
        {
            FontSize = 16,
            LineBreakMode = LineBreakMode.WordWrap,
            GestureRecognizers =
            {

            },
            FormattedText = formattedString
        };

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
                if (tags[i]["attributes"]["name"]["en"].ToString() == "Long Strip")
                {
                    longstrip = true;
                }
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
        if (stats[mangaId]["rating"]["average"] == null)
        {
            mangaScore.Text = "No Score";
        }
        else
        {
            mangaScore.Text = $"☆ {Math.Round(float.Parse(stats[mangaId]["rating"]["average"].ToString()), 2, MidpointRounding.ToEven)}";
        }
    }

    private async void SetMangaChapters()
    {
        if (chapterPage != 0)
        {
            await scrollView.ScrollToAsync(0, mangaInfoGrid.Height, true);
        }

        chapterStack.Children.Clear();
        chapterStack.Add(new Image
        {
            Source = "loading_cat.gif",
            IsAnimationPlaying = true,
            HorizontalOptions = LayoutOptions.Center,
            MaximumHeightRequest = 300
        });

        JsonNode chaptersNode = await GetMangaChapters();
        chapterStack.Children.Clear();
        
        var chapters = chaptersNode.AsArray();

        if (chapters.Count == 0)
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
                volumeLayout = new()
                {
                    Spacing = 10
                };
                volumeLayout.Add(new Label
                {
                    Text = $"Volume {lastVolume}",
                    FontSize = 24,
                    Margin = new Thickness(10, 0, 0, 10)
                });
            }

            volumeLayout.Add(new ChapterEntry(chapters[i], longstrip.ToString(), mangaId));
        }
        chapterStack.Add(volumeLayout);

        Grid buttonGrid = new()
        {
            HorizontalOptions = LayoutOptions.Center,
            ColumnSpacing = 10,
            ColumnDefinitions =
            {
                new ColumnDefinition{ Width = new GridLength(1, GridUnitType.Auto) },
                new ColumnDefinition{ Width = new GridLength(1, GridUnitType.Auto) },
                new ColumnDefinition{ Width = new GridLength(1, GridUnitType.Auto) },
                new ColumnDefinition{ Width = new GridLength(1, GridUnitType.Auto) },
                new ColumnDefinition{ Width = new GridLength(1, GridUnitType.Auto) }
            }
        };

        PointerGestureRecognizer pointerGestureRecognizer = new();
        pointerGestureRecognizer.PointerEntered += OnPointerEnterTitle;
        pointerGestureRecognizer.PointerExited += OnPointerExitTitle;

        TapGestureRecognizer tapGestureDown = new();
        tapGestureDown.Tapped += OnChapterPageDown;

        Label downLabel = new()
        {
            Text = "<",
            GestureRecognizers = 
            { 
                pointerGestureRecognizer,
                tapGestureDown
            }
        };

        TapGestureRecognizer tapGestureUp = new();
        tapGestureUp.Tapped += OnChapterPageUp;

        Label upLabel = new()
        {
            Text = ">",
            GestureRecognizers =
            {
                pointerGestureRecognizer,
                tapGestureUp
            }
        };

        TapGestureRecognizer tapGestureFirst = new();
        tapGestureFirst.Tapped += OnChapterFirstPage;

        Label firstLabel = new()
        {
            Text = "<<",
            GestureRecognizers =
            {
                pointerGestureRecognizer,
                tapGestureFirst
            }
        };

        TapGestureRecognizer tapGestureLast = new();
        tapGestureLast.Tapped += OnChapterLastPage;

        Label lastLabel = new()
        {
            Text = ">>",
            GestureRecognizers =
            {
                pointerGestureRecognizer,
                tapGestureLast
            }
        };

        if (chapterPage != 0)
        {
            buttonGrid.Add(downLabel, 1, 0);
            if (chapterPage > 1)
            {
                buttonGrid.Add(firstLabel, 0, 0);
            }
        }
        if ((chapterPage + 1) * 25 < totalChapterEntries)
        {
            buttonGrid.Add(upLabel, 3, 0);
            if ((chapterPage + 2) * 25 < totalChapterEntries)
            {
                buttonGrid.Add(lastLabel, 4, 0);
            }
        }

        buttonGrid.Add(new Label
        {
            Text = (chapterPage + 1).ToString()
        }, 2, 0);

        chapterStack.Add(buttonGrid);
    }

    private void OnPointerEnterTitle(object sender, EventArgs e)
    {
        ((Label)sender).TextDecorations = TextDecorations.Underline;
    }

    private void OnPointerExitTitle(object sender, EventArgs e)
    {
        ((Label)sender).TextDecorations = TextDecorations.None;
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

    private void OnChapterPageDown(object sender, EventArgs e)
    {
        chapterPage -= 1;
        SetMangaChapters();
    }

    private void OnChapterPageUp(object sender, EventArgs e)
    {
        chapterPage += 1;
        SetMangaChapters();
    }

    private void OnChapterLastPage(object sender, EventArgs e)
    {
        chapterPage = totalChapterEntries / 25;
        SetMangaChapters();
    }

    private void OnChapterFirstPage(object sender, EventArgs e)
    {
        chapterPage = 0;
        SetMangaChapters();
    }

    private async void OpenBrowserWithLink(object sender, EventArgs e)
    {
        string linkUrl = ((TappedEventArgs)e).Parameter.ToString();
        try
        {
            Uri uri = new Uri(linkUrl);
            await Browser.Default.OpenAsync(uri, BrowserLaunchMode.SystemPreferred);
        }
        catch (Exception ex)
        {
            // An unexpected error occurred. No browser may be installed on the device.
        }
    }

    private void Test(object sender, EventArgs e)
    {
        return;
    }
}