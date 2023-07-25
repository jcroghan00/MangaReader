using System.Text.Json.Nodes;

namespace MangaReader.Views;

public partial class SearchResult : ContentView
{
	JsonNode manga;
	public SearchResult(JsonNode manga)
	{
		InitializeComponent();

		this.manga = manga;
	}

    static readonly string coverBaseUrl = "https://uploads.mangadex.org/covers/";

    private void OnViewLoaded(object sender, EventArgs e)
	{

	}
}