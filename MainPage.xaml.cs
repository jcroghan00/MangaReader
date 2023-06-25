namespace MangaReader;

using Flurl;
using Microsoft.Maui.Controls.Shapes;
using System.Text.Json.Nodes;
using System.Xml.Linq;

public partial class MainPage : ContentPage
{
	static readonly HttpClient client = new HttpClient();

	static string baseUrl = "https://api.mangadex.org/";
	static string coverBaseUrl = "https://uploads.mangadex.org/covers/";

    private JsonNode popularNewManga;
    private byte[][] popularNewImages;

    public MainPage()
	{
		InitializeComponent();

        popularNewImages = new byte[10][];

		getPopularNew();
        getNewReleases();
	}

    private async void getPopularNew()
	{
        var dt = DateTime.Now;
        dt = dt.AddDays(-14);

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

    private async void getNewReleases()
    {
        return;
    }

    private Border makeNewTag(string tag)
    {
        string text = tag;
        Color backgroundColor = (Color) App.Current.Resources["hlColor"];

        if (tag == "suggestive" || tag == "erotica")
        {
            text = tag[0].ToString().ToUpper() + tag.Substring(1);
            backgroundColor = (Color)App.Current.Resources[tag + "BgColor"];
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

