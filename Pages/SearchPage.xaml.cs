using MangaReader.Views;
using System.Text.Json.Nodes;
using Flurl;
using System.Diagnostics;

namespace MangaReader;

public partial class SearchPage : ContentPage
{
	public SearchPage()
	{
		InitializeComponent();
	}

    static readonly string baseUrl = "https://api.mangadex.org/";

    private async Task<JsonArray> GetSearchResults(string searchText)
	{
        var url = baseUrl
            .AppendPathSegment("manga")
            .SetQueryParam("title", searchText)
            .SetQueryParam("includes[]", new[] {"manga", "cover_art", "author", "artist"})
            .SetQueryParam("availableTranslatedLanguage[]", new[] { "en" })
            .SetQueryParam("hasAvailableChapters", "true");

        url += "&order%5Brelevance%5D=desc";

        HttpRequestMessage msg = new(HttpMethod.Get, url);
        HttpResponseMessage res = await MainPage.client.SendAsync(msg);
        res.EnsureSuccessStatusCode();

        var jNode = JsonNode.Parse(res.Content.ReadAsStream());
        JsonNode manga = jNode["data"];
        return manga.AsArray();
    }

	private async void OnEntryCompleted(object sender, EventArgs e)
	{
        resultVertical.Children.Clear();
        resultVertical.Add(new Image
        {
            Source = "loading_cat.gif",
            IsAnimationPlaying = true,
            HorizontalOptions = LayoutOptions.Center,
            MaximumHeightRequest = 500
        });
        
        JsonArray results = await GetSearchResults(((Entry)sender).Text);
        resultVertical.Children.Clear();

        for (int i = 0; i < results.Count; i++)
        {
            resultVertical.Add(new SearchResult(results[i]));
        }
	}

	/*
    private void OnEntryTextChanged(object sender, EventArgs e)
	{
        verticalStack.Add(new SearchResult(((Entry)sender).Text));
    }
	*/
}