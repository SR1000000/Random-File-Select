using System.Diagnostics;

namespace Random_File_Select;

/// <summary>Small modal window with app name, version, links, and usage notes.</summary>
public sealed class AboutDialog : Form
{
    private const string GitHubUrl = "https://github.com/SR1000000/Random-File-Select";

    public AboutDialog()
    {
        BuildLayout();
    }

    private void BuildLayout()
    {
        Text = "About Random File Select";
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MinimizeBox = false;
        MaximizeBox = false;
        ShowInTaskbar = false;
        StartPosition = FormStartPosition.CenterParent;
        ClientSize = new Size(420, 300);

        var version = Application.ProductVersion;
        var plusIndex = version.IndexOf('+');
        if (plusIndex >= 0)
            version = version[..plusIndex];

        var nameLabel = new Label
        {
            Text = "Random File Select",
            Font = new Font(Font, FontStyle.Bold),
            AutoSize = true,
        };
        nameLabel.Location = new Point(12, 12);

        var versionLabel = new Label
        {
            Text = $"Version {version}",
            AutoSize = true,
        };
        versionLabel.Location = new Point(12, nameLabel.Bottom + 6);

        var descriptionLabel = new Label
        {
            Text = "Picks a random file from a set of folders, optionally weighted by Last Accessed date.",
            AutoSize = false,
            Size = new Size(396, 32),
        };
        descriptionLabel.Location = new Point(12, versionLabel.Bottom + 12);

        var githubLink = new LinkLabel
        {
            Text = "github.com/SR1000000/Random-File-Select",
            AutoSize = true,
            LinkColor = Color.FromArgb(0, 102, 204),
        };
        githubLink.Location = new Point(12, descriptionLabel.Bottom + 8);
        githubLink.LinkClicked += (_, _) =>
        {
            Process.Start(new ProcessStartInfo(GitHubUrl) { UseShellExecute = true });
        };

        var licenseLabel = new Label
        {
            Text = "Released under the GPL-3.0 License",
            AutoSize = true,
        };
        licenseLabel.Location = new Point(12, githubLink.Bottom + 12);

        const string notesText =
            "Root folders and the extensions list are saved in .settings file alongside the other settings next to the executable.\r\n"
            + "Junctions/reparse points and system files or folders are skipped during the scan.\r\n"
            + "The log keeps the most recent 1000 entries.";

        var notesBox = new TextBox
        {
            Text = notesText,
            ReadOnly = true,
            Multiline = true,
            BorderStyle = BorderStyle.None,
            BackColor = SystemColors.Control,
            TabStop = false,
            Size = new Size(396, 118),
            Location = new Point(12, licenseLabel.Bottom + 10),
        };

        var okButton = new Button
        {
            Text = "OK",
            DialogResult = DialogResult.OK,
            Size = new Size(75, 28),
        };
        okButton.Location = new Point(ClientSize.Width - okButton.Width - 12, ClientSize.Height - okButton.Height - 12);
        AcceptButton = okButton;
        CancelButton = okButton;

        Controls.AddRange([nameLabel, versionLabel, descriptionLabel, githubLink, licenseLabel, notesBox, okButton]);
    }
}