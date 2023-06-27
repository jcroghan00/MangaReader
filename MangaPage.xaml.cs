using System.ComponentModel;

namespace MangaReader;

using Flurl;
using Microsoft.Maui.Controls;
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
        List<string> genres = new List<string>();
        List<string> themes = new List<string>();
        List<string> formats = new List<string>();
        // Create all tags for the manga
        for (int i = 0; i < tags.Count(); i++)
        {
            Border newTag = Tools.makeNewTag(tags[i]["attributes"]["name"]["en"].ToString());
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

        Label authorLabel = new Label();
        string coverFileName = "";
        List<string> authors = new List<string>();
        List<string> artists = new List<string>();

        // Filters through manga relations to get the authors/artists and cover art id
        for (int i = 0; i < relations.Count(); i++)
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
        mangaPublicationStatus.Text = $"Publication {manga["attributes"]["year"]}: {manga["attributes"]["status"].ToString()[0].ToString().ToUpper() + manga["attributes"]["status"].ToString()[1..]}";

        if (authors.Count > 1)
        {
            authorDetailText.Text = "Authors";
        }
        if (artists.Count > 1)
        {
            artistDetailText.Text = "Artists";
        }

        createAndAddDetail(authorDetail, authorDetailFlex, authors);
        createAndAddDetail(artistDetail, artistDetailFlex, artists);
        createAndAddDetail(formatDetail, formatDetailFlex, formats);
        createAndAddDetail(genreDetail, genreDetailFlex, genres);
        createAndAddDetail(themeDetail, themeDetailFlex, themes);

        if (manga["attributes"]["publicationDemographic"] != null)
        {
            demographicDetail.IsVisible = true;
            Border demographic = Tools.makeNewTag(manga["attributes"]["publicationDemographic"].ToString());
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
            alternativeTitleDetail.IsVisible = true;
            alternativeTitleDetailStack.Add(new Label
            {
                Text = title,
                TextType = TextType.Html
            });
        }
    }

    private void createAndAddDetail(Border overLayout, FlexLayout layout, List<string> input)
    {
        for (int i = 0; i < input.Count; i++)
        {
            overLayout.IsVisible = true;
            Border border = Tools.makeNewTag(input[i]);
            border.Stroke = (Color)App.Current.Resources["bgColor"];
            border.BackgroundColor = (Color)App.Current.Resources["bgColor"];
            border.Margin = new Thickness(2.5, 0, 2.5, 5);

            layout.Add(border);
        }
    }
}