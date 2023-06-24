namespace MangaReader;

using Flurl;
using Microsoft.Maui.Controls.Shapes;
using System.Text.Json.Nodes;

public partial class MainPage : ContentPage
{
	static readonly HttpClient client = new HttpClient();

	static string baseUrl = "https://api.mangadex.org/";
	static string coverBaseUrl = "https://uploads.mangadex.org/covers/";

    private JsonNode popularNewManga;


    public MainPage()
	{
		InitializeComponent();

		getPopularNew();
	}

	private async void getPopularNew()
	{
        var dt = DateTime.Now;
        dt = dt.AddDays(-14);

		var url = baseUrl
			.AppendPathSegment("manga")
			.SetQueryParam("includes[]", new[] { "manga", "cover_art", "author", "artist" })
			.SetQueryParam("contentRating[]", new[] { "suggestive", "safe" })
			.SetQueryParam("createdAtSince", dt.ToString("yyyy-MM-ddTHH:mm:ss"));

		baseUrl += "order%5BfollowedCount%5D=desc";

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

        popularNewTags.Children.Clear();
        popularNewPageNumber.Text = (popularNewIndex + 1).ToString();
        popularNewTitle.Text = popularNewManga[popularNewIndex]["attributes"]["title"]["en"].ToString();
        popularNewImage.Source = "no_image.png";

        if (popularNewManga[popularNewIndex]["attributes"]["description"].ToString() == "{}")
        {
            popularNewDescription.Text = "No Description";
        }
        else
        {
            popularNewDescription.Text = popularNewManga[popularNewIndex]["attributes"]["description"]["en"].ToString();
        }
        
        List<string> authors = new List<string>();
        string coverFileName = "";
        var relations = popularNewManga[popularNewIndex]["relationships"].AsArray();

        for (int i = 0; i < relations.Count(); i++)
        {
            if (relations[i]["type"].ToString() == "author" || relations[i]["type"].ToString() == "artist")
            {
                authors.Add(relations[i]["attributes"]["name"].ToString());
            }
            else if (relations[i]["type"].ToString() == "cover_art")
            {
                coverFileName = relations[i]["attributes"]["fileName"].ToString();
            }
        }

        popularNewAuthor.Text = authors.FirstOrDefault();

        var tags = popularNewManga[popularNewIndex]["attributes"]["tags"].AsArray();

        for (int i = 0; i < tags.Count(); i++)
        {
            Border newTag = makeNewTag(tags[i]["attributes"]["name"]["en"].ToString());
            popularNewTags.Add(newTag);
        }

        var coverUrl = $"{coverBaseUrl}{popularNewManga[popularNewIndex]["id"]}/{coverFileName}.256.jpg";
        popularNewImage.Source = coverUrl;
    }

    /*
    <Border BackgroundColor = "#1e1e1e" Stroke="#1e1e1e" Padding="15,5,15,5" StrokeShape="RoundRectangle 10">
        <Label VerticalOptions = "Center" Text="Tag1" HeightRequest="20"/>
    </Border>
    <Border BackgroundColor = "#1e1e1e" Stroke="#1e1e1e" Padding="15,5,15,5" StrokeShape="RoundRectangle 10" VerticalOptions = "Center">
        <Label Text="Tag2" HeightRequest="20"/>
    </Border>
    */

    private Border makeNewTag(string tag)
    {
        RoundRectangle rectangle = new RoundRectangle();
        rectangle.CornerRadius = 10;

        Border border = new Border
        {
            BackgroundColor = Color.FromArgb("#1e1e1e"),
            Stroke = Color.FromArgb("#1e1e1e"),
            StrokeThickness = 5,
            Padding = new Thickness(15, 5, 15, 5),
            StrokeShape = rectangle,
            Content = new Label { Text = tag }
        };

        return border;
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

