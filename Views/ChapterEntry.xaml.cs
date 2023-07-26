using Microsoft.Maui.Controls.Shapes;
using System.Text.Json.Nodes;

namespace MangaReader.Views;

public partial class ChapterEntry : ContentView
{
    JsonNode chapter;
    string longstrip;

	public ChapterEntry(JsonNode chapter, string longstrip)
	{
		InitializeComponent();

        this.chapter = chapter;
        this.longstrip = longstrip;
	}

	private void OnViewLoaded(object sender, EventArgs e)
	{
        tapGestureRecognizer.Tapped += Tools.ToChapterPage;
        tapGestureRecognizer.CommandParameter = new List<string> {
                chapter["id"].ToString(),
                longstrip
             };

        string chapterTitle = "Oneshot";

        if (chapter["attributes"]["chapter"] != null)
        {
            chapterTitle = $"Ch. {chapter["attributes"]["chapter"]}";
        }

        chapterTitle += chapter["attributes"]["title"] != null ? $" - {chapter["attributes"]["title"]}" : "";

        chapterTitleLabel.Text = chapterTitle;

        DateTime readableAt = DateTime.Parse(chapter["attributes"]["readableAt"].ToString());
        TimeSpan timeDifference = DateTime.Now - readableAt;

        string timeReadable = "No Time Readable";
        if (timeDifference.TotalDays / 365.2425 >= 1)
        {
            timeReadable = (int)(timeDifference.TotalDays / 365.2425) == 1 ? (int)(timeDifference.TotalDays / 365.2425) + " Year Ago" : (int)(timeDifference.TotalDays / 365.2425) + " Years Ago";
        }
        else if (timeDifference.TotalDays / 30.437 >= 1)
        {
            timeReadable = (int)(timeDifference.TotalDays / 30.437) == 1 ? (int)(timeDifference.TotalDays / 30.437) + " Month Ago" : (int)(timeDifference.TotalDays / 30.437) + " Months Ago";
        }
        else if (timeDifference.Days > 0)
        {
            timeReadable = timeDifference.Days == 1 ? timeDifference.Days.ToString() + " Day Ago" : timeDifference.Days.ToString() + " Days Ago";
        }
        else if (timeDifference.Hours > 0)
        {
            timeReadable = timeDifference.Hours == 1 ? timeDifference.Hours.ToString() + " Hour Ago" : timeDifference.Hours.ToString() + " Hours Ago";
        }
        else if (timeDifference.Minutes > 0)
        {
            timeReadable = timeDifference.Minutes == 1 ? timeDifference.Minutes.ToString() + " Minute Ago" : timeDifference.Minutes.ToString() + " Minutes Ago";
        }
        else if (timeDifference.Seconds > 0)
        {
            timeReadable = timeDifference.Seconds == 1 ? timeDifference.Seconds.ToString() + " Second Ago" : timeDifference.Seconds.ToString() + " Seconds Ago";
        }

        chapterReadableLabel.Text = timeReadable;

        var chapterRelations = chapter["relationships"].AsArray();

        string postUser = "No User";
        string scanGroup = "No Group";

        FontAttributes scanGroupAttributes = FontAttributes.Italic;

        for (int j = 0; j < chapterRelations.Count; j++)
        {
            if (chapterRelations[j]["type"].ToString() == "user")
            {
                if (chapterRelations[j]["attributes"]["username"] != null)
                {
                    postUser = chapterRelations[j]["attributes"]["username"].ToString();
                }
            }
            else if (chapterRelations[j]["type"].ToString() == "scanlation_group")
            {
                if (scanGroup != "No Group")
                {
                    PointerGestureRecognizer scanGroupPointer = new();
                    scanGroupPointer.PointerEntered += OnPointerEnterHighlight;
                    scanGroupPointer.PointerExited += OnPointerExitHighlight;
                    scanGroupLayout.Add(new Border
                    {
                        Stroke = null,
                        BackgroundColor = null,
                        StrokeShape = new RoundRectangle
                        {
                            CornerRadius = 5
                        },
                        Padding = new Thickness(5, 5, 5, 5),
                        GestureRecognizers =
                            {
                                    scanGroupPointer
                            },
                        Content = new Label
                        {
                            Text = scanGroup,
                            FontAttributes = scanGroupAttributes,
                            LineBreakMode = LineBreakMode.TailTruncation,
                            FontSize = 14
                        }
                    });
                }

                scanGroup = chapterRelations[j]["attributes"]["name"].ToString();
                scanGroupAttributes = FontAttributes.None;
            }
        }

        PointerGestureRecognizer chapterGestureRecognizer = new();
        chapterGestureRecognizer.PointerEntered += OnPointerEnterHighlight;
        chapterGestureRecognizer.PointerExited += OnPointerExitHighlight;
        scanGroupLayout.Add(new Border
        {
            Stroke = null,
            BackgroundColor = null,
            StrokeShape = new RoundRectangle
            {
                CornerRadius = 5
            },
            Padding = new Thickness(5, 5, 5, 5),
            GestureRecognizers = { scanGroup != "No Group" ? chapterGestureRecognizer : null },
            Content = new Label
            {
                Text = scanGroup,
                FontAttributes = scanGroupAttributes,
                LineBreakMode = LineBreakMode.TailTruncation,
                FontSize = 14
            }
        });

        userBorder.Stroke = null;
        userBorder.BackgroundColor = null;
        if (postUser != "No User")
        {
            userBorder.GestureRecognizers.Add(chapterGestureRecognizer);
        }
        userLabel.Text = postUser;
    }

    private void OnPointerEnterChapter(object sender, EventArgs e)
    {
        ((Border)sender).BackgroundColor = (Color)App.Current.Resources["fgColor"];
    }

    private void OnPointerExitChapter(object sender, EventArgs e)
    {
        ((Border)sender).BackgroundColor = (Color)App.Current.Resources["hlColor"];
    }

    private void OnPointerEnterHighlight(object sender, EventArgs e)
    {
        ((Border)sender).BackgroundColor = (Color)App.Current.Resources["hlColor"];
    }

    private void OnPointerExitHighlight(object sender, EventArgs e)
    {
        ((Border)sender).BackgroundColor = null;
    }
}