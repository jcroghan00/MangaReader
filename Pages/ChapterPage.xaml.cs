using Flurl;
using System.Text.Json.Nodes;

namespace MangaReader;

[QueryProperty(nameof(MangaId), "mangaId")]
[QueryProperty(nameof(ChapterId), "chapterId")]
[QueryProperty(nameof(LongStrip), "longStrip")]
public partial class ChapterPage : ContentPage
{
    static readonly string baseUrl = "https://api.mangadex.org/";
    private double pageScale = 0.7;

    private string mangaId = "No ID";
    public string MangaId
    {
        get => mangaId;
        set
        {
            mangaId = value;
        }
    }

    private string chapterId = "No ID";
    public string ChapterId
    {
        get => chapterId;
        set
        {
            chapterId = value;
        }
    }

    private string longStrip = "False";
    public string LongStrip
    {
        get => longStrip;
        set
        {
            longStrip = value;
        }
    }

    JsonNode chapter;
    string pageUrl;
    JsonArray pages;

    public ChapterPage()
	{
		InitializeComponent();

        contentPage.SizeChanged += OnPageResize;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();

        GetChapterById(chapterId);
    }

    private async void GetChapterById(string id)
    {
        var url = $"{baseUrl}chapter/{id}".SetQueryParam("includes[]", new[] { "scanlation_group" });

        HttpRequestMessage msg = new(HttpMethod.Get, url);
        HttpResponseMessage res = await MainPage.client.SendAsync(msg);
        res.EnsureSuccessStatusCode();

        var jNode = JsonNode.Parse(res.Content.ReadAsStream());
        chapter = jNode["data"];

        if (chapter["attributes"]["externalUrl"] != null)
        {
            CreateChapterWebView(chapter["attributes"]["externalUrl"].ToString());
        }
        else
        {
            GetChapterPagesById(id);
        }
    }

    private async void GetChapterPagesById(string id)
    {
        chapterImages.Clear();
        var url = $"{baseUrl}at-home/server/{id}";

        HttpRequestMessage msg = new(HttpMethod.Get, url);
        HttpResponseMessage res = await MainPage.client.SendAsync(msg);
        res.EnsureSuccessStatusCode();

        // TODO: Add an option to use data-saver images because oh lord the ram usage
        var jNode = JsonNode.Parse(res.Content.ReadAsStream());
        pages = jNode["chapter"]["data"].AsArray();
        pageUrl = $"{jNode["baseUrl"]}/data/{jNode["chapter"]["hash"]}/";

        string chapterNumberText = "";
        chapterNumberText += chapter["attributes"]["volume"] == null ? "" : $"Vol. {chapter["attributes"]["volume"]} ";
        chapterNumberText += $"Ch. {chapter["attributes"]["chapter"]}";
        chapterNumberText += chapter["attributes"]["title"] == null ? "" : $" - {chapter["attributes"]["title"]} ";
        chapterLabel.Text = chapterNumberText;

        string groupText = "No Group";
        FontAttributes fontAttributes = FontAttributes.Italic;

        JsonArray relations = chapter["relationships"].AsArray();
        for (int i = 0; i <  relations.Count; i++)
        {
            if (relations[i]["type"].ToString() == "scanlation_group")
            {
                groupText = relations[i]["attributes"]["name"].ToString();
                fontAttributes = FontAttributes.None;
            }
        }

        groupLabel.Text = groupText;
        groupLabel.FontAttributes = fontAttributes;

        if (longStrip == "True")
        {
            CreateChapterImagesLongstrip();
        }
        else
        {
            CreateChapterImagesPaged();
        }
    }

    bool isImagesCreated = false;
    private void CreateChapterImagesPaged()
    {
        isImagesCreated = true;

        numberLabel.IsVisible = true;
        numberLabel.Text = $"{pageNumber + 1}/{pages.Count}";

        for (int i = 0; i < pages.Count; i++)
        {
            Image image = new Image
            {
                IsVisible = (i == pageNumber),
                MaximumWidthRequest = contentPage.Width * pageScale,
                MinimumWidthRequest = 600,
                Source = new UriImageSource
                {
                    Uri = new Uri($"{pageUrl}{pages[i]}"),
                    CachingEnabled = false
                }
            };
            chapterImages.Add(image);

            imageGrid.Add(chapterImages[i], 1, 1);
        }

        TapGestureRecognizer nextGestureRecognizer = new();
        nextGestureRecognizer.Tapped += NextPage;

        TapGestureRecognizer lastGestureRecognizer = new();
        lastGestureRecognizer.Tapped += LastPage;

        Label lastLabel = new Label 
        { 
            WidthRequest = 3000,
            BindingContext = imageGrid.Height,
            GestureRecognizers =
            {
                lastGestureRecognizer
            }
        };

        Label nextLabel = new Label
        {
            WidthRequest = 3000,
            BindingContext = imageGrid.Height,
            GestureRecognizers =
            {
                nextGestureRecognizer
            }
        };

        buttonGrid.Add(lastLabel, 0, 0);
        buttonGrid.Add(nextLabel, 1, 0);
    }

    private void CreateChapterImagesLongstrip()
    {
        numberLabel.IsVisible = false;
        isImagesCreated = true;

        VerticalStackLayout chapterImageStack = new();

        for (int i = 0; i < pages.Count; i++)
        {
            chapterImages.Add(new Image
            {
                MaximumWidthRequest = contentPage.Width * pageScale,
                MinimumWidthRequest = 600,
                Source = new UriImageSource
                {
                    Uri = new Uri($"{pageUrl}{pages[i]}"),
                    CachingEnabled = false
                },
            });
            chapterImageStack.Add(chapterImages[i]);
        }

        scrollView.Content = chapterImageStack;

        return;
    }

    private void CreateChapterWebView(string url)
    {
        contentPage.Content = new WebView
        {
            Source = url
        };
    }

    private int pageNumber = 0;
    private readonly List<Image> chapterImages = new();
    private async void SwitchPage(int numPages)
    {
        chapterImages[pageNumber].IsVisible = false;

        if (pageNumber + numPages >= pages.Count || pageNumber + numPages < 0)
        {
            SetChapterByNumber(numPages);
            return;
        }

        pageNumber += numPages;
        chapterImages[pageNumber].IsVisible = true;

        numberLabel.Text = $"{pageNumber + 1}/{pages.Count}";

        await scrollView.ScrollToAsync(0, numberLabel.Height + 20, true);
    }

    private async void SetChapterByNumber(int direction)
    {
        pageNumber = 0;
        numberLabel.IsVisible = false;

        JsonArray chapterArray = await GetChapterByNumber(Int32.Parse(chapter["attributes"]["chapter"].ToString()) + direction);

        if (chapterArray.Count == 0)
        {
            await Navigation.PopAsync();
        }

        string group = "No Group";
        for (int i = 0; i < chapter["relationships"].AsArray().Count; i++)
        {
            if (chapter["relationships"].AsArray()[i]["type"].ToString() == "scanlation_group")
            {
                group = chapter["relationships"].AsArray()[i]["id"].ToString();
            }
        }

        for (int i = 0; i < chapterArray.Count; i++)
        {
            JsonArray relationships = chapterArray[i]["relationships"].AsArray();
            for (int j = 0; j < relationships.Count; j++)
            {
                if (relationships[j]["type"].ToString() == "scanlation_group")
                {
                    if (relationships[j]["id"].ToString() == group)
                    {
                        chapter = chapterArray[i];
                        GetChapterPagesById(chapterArray[i]["id"].ToString());
                        return;
                    }
                }
            }
        }

        await scrollView.ScrollToAsync(0, 0, true);

        chapter = chapterArray[0];
        GetChapterPagesById(chapterArray[0]["id"].ToString());
        return;
    }

    private async Task<JsonArray> GetChapterByNumber(int chapterNumber)
    {
        var url = baseUrl
            .AppendPathSegment("chapter")
            .SetQueryParam("manga", mangaId)
            .SetQueryParam("chapter", chapterNumber)
            .SetQueryParam("contentRating[]", new[] { "safe", "suggestive", "erotica" })
            .SetQueryParam("translatedLanguage[]", new[] { "en" })
            .SetQueryParam("includes[]", new[] { "scanlation_group" });

        HttpRequestMessage msg = new(HttpMethod.Get, url);
        HttpResponseMessage res = await MainPage.client.SendAsync(msg);
        res.EnsureSuccessStatusCode();

        var jNode = JsonNode.Parse(res.Content.ReadAsStream());
        JsonArray chapters = jNode["data"].AsArray();
        return chapters;
    }

    private void NextPage(object sender, EventArgs e)
    {
        SwitchPage(1);
    }

    private void LastPage(object sender, EventArgs e)
    {
        SwitchPage(-1);
    }

    private void OnPageResize(object sender, EventArgs e)
    {
        if (!isImagesCreated) { return; }

        for (int i = 0; i < pages.Count; i++)
        {
            chapterImages[i].MaximumWidthRequest = contentPage.Width * pageScale;
        }
    }
}