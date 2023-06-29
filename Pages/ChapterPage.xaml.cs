using System.Diagnostics;
using System.Text.Json.Nodes;

namespace MangaReader;

[QueryProperty(nameof(ChapterId), "chapterId")]
[QueryProperty(nameof(LongStrip), "longStrip")]
public partial class ChapterPage : ContentPage
{
    static readonly string baseUrl = "https://api.mangadex.org/";

    private string chapterId = "No ID";
    public string ChapterId
    {
        get => chapterId;
        set
        {
            chapterId = value;
        }
    }

    private bool longStrip = false;
    public bool LongStrip
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
	}

    protected override void OnAppearing()
    {
        base.OnAppearing();

        GetChapterById(chapterId);
    }

    private async void GetChapterById(string id)
    {
        var url = $"{baseUrl}chapter/{id}";

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
        var url = $"{baseUrl}at-home/server/{id}";

        HttpRequestMessage msg = new(HttpMethod.Get, url);
        HttpResponseMessage res = await MainPage.client.SendAsync(msg);
        res.EnsureSuccessStatusCode();

        // TODO: Add an option to use data-saver images because oh lord the ram usage
        var jNode = JsonNode.Parse(res.Content.ReadAsStream());
        Debug.WriteLine(jNode.ToString());
        pages = jNode["chapter"]["data"].AsArray();
        pageUrl = $"{jNode["baseUrl"]}/data/{jNode["chapter"]["hash"]}/";

        if (longStrip)
        {
            CreateChapterImagesLongstrip();
        }
        else
        {
            CreateChapterImagesPaged();
        }
    }

    Label numberLabel;
    private void CreateChapterImagesPaged()
    {
        numberLabel = new Label
        {
            Text = $"1/{pages.Count}",
            HorizontalOptions = LayoutOptions.Center,
            Margin = new Thickness(0, 10, 0, 10),
            FontSize = 20
        };
        imageGrid.Add(numberLabel);
        for (int i = 0; i < pages.Count; i++)
        {
            TapGestureRecognizer nextGestureRecognizer = new();
            nextGestureRecognizer.Tapped += NextPage;

            chapterImages.Add(new Image
            {
                IsVisible = (i == 0),
                WidthRequest = 1500,
                Source = new UriImageSource
                {
                    Uri = new Uri($"{pageUrl}{pages[i]}"),
                    CachingEnabled = false
                },
                GestureRecognizers =
                {
                    nextGestureRecognizer
                }
            });

            imageGrid.Add(chapterImages[i], 0, 1);
        }
    }

    private void CreateChapterImagesLongstrip()
    {
        // TODO: Do this lamo
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
        if (pageNumber + numPages >= pages.Count)
        {
            await Navigation.PopAsync();
            return;
        }

        chapterImages[pageNumber].IsVisible = false;
        pageNumber += numPages;
        chapterImages[pageNumber].IsVisible = true;

        numberLabel.Text = $"{pageNumber + 1}/{pages.Count}";

        await scrollView.ScrollToAsync(0, numberLabel.Height + 20, true);
    }

    private void NextPage(object sender, EventArgs e)
    {
        SwitchPage(1);
    }
}