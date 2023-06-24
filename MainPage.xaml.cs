namespace MangaReader;

using Flurl;

public partial class MainPage : ContentPage
{
	static readonly HttpClient client = new HttpClient();
	static string baseUrl = "https://api.mangadex.org/";

	public MainPage()
	{
		InitializeComponent();

		getPopularNew();
	}

	private async void getPopularNew()
	{
		var url = baseUrl
			.AppendPathSegment("manga")
			.SetQueryParam("includes[]", new[] { "manga", "cover_art", "author", "artist" })
			.SetQueryParam("contentRating[]", new[] { "suggestive", "safe" })
			.SetQueryParam("createdAtSince", "2023-06-09T00:00:00");

		baseUrl += "order%5BfollowedCount%5D=desc&order%5Brating%5D=desc";

        HttpRequestMessage msg = new HttpRequestMessage(HttpMethod.Get, url);
        popularNewDescription.Text = "Gay Sex";
        HttpResponseMessage res = await client.SendAsync(msg);
        popularNewDescription.Text = res.StatusCode.ToString();
        res.EnsureSuccessStatusCode();

        popularNewDescription.Text = res.Content.ReadAsStringAsync().Result;
    }
}

