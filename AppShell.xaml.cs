namespace MangaReader;

public partial class AppShell : Shell
{
	public AppShell()
	{
		InitializeComponent();

		Routing.RegisterRoute("MangaPage", typeof(MangaPage));
        Routing.RegisterRoute("ChapterPage", typeof(ChapterPage));
    }
}
