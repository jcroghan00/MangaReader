using System.ComponentModel;

namespace MangaReader;

using Flurl;
using System.Text.Json.Nodes;

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

    static string baseUrl = "https://api.mangadex.org/";
    static string coverBaseUrl = "https://uploads.mangadex.org/covers/";

    JsonNode manga;

    public MangaPage()
	{
		InitializeComponent();
	}

    protected override void OnAppearing()
    {
        base.OnAppearing();

        getMangaById(mangaId);
    }

	private async void getMangaById(string mangaId)
	{
        var url = baseUrl
            .AppendPathSegment("manga")
            .AppendPathSegment($"/{mangaId}")
            .SetQueryParam("includes[]", new[] { "manga", "cover_art", "author", "artist" });

        HttpRequestMessage msg = new HttpRequestMessage(HttpMethod.Get, url);
        HttpResponseMessage res = await MainPage.client.SendAsync(msg);
        res.EnsureSuccessStatusCode();

        var jNode = JsonNode.Parse(res.Content.ReadAsStream());
        manga = jNode["data"];
        setMangaElements();
    }

    private void setMangaElements()
    {
        mangaTitle.Text = manga["attributes"]["title"]["en"].ToString();
        
        if (manga["attributes"]["description"].ToString() == "{}")
        {
            mangaDescription.Text = "No Description";
        }
        else
        {
            mangaDescription.Text = manga["attributes"]["description"]["en"].ToString();
        }

        // Adds a tag for the content rating of a manga if the manga is not 'safe'
        if (manga["attributes"]["contentRating"].ToString() != "safe")
        {
            Border newTag = Tools.makeNewTag(manga["attributes"]["contentRating"].ToString());
            newTag.Margin = new Thickness(5, 5, 5, 5);
            mangaTags.Add(newTag);
        }

        var tags = manga["attributes"]["tags"].AsArray();
        // Create all tags for the manga
        for (int i = 0; i < tags.Count(); i++)
        {
            Border newTag = Tools.makeNewTag(tags[i]["attributes"]["name"]["en"].ToString());
            newTag.Margin = new Thickness(5, 5, 5, 5);
            mangaTags.Add(newTag);
        }

        var relations = manga["relationships"].AsArray();

        Label authorLabel = new Label();
        string coverFileName = "";
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
                if (relations[i]["attributes"]["name"].ToString() == authorLabel.Text)
                {
                    continue;
                }

                Label label = new Label
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

        mangaPublicationStatus.Text = $"Publication {manga["attributes"]["year"]}: {manga["attributes"]["status"].ToString()[0].ToString().ToUpper() + manga["attributes"]["status"].ToString().Substring(1)}";
    }
}