using System;
using System.IO;
using System.Diagnostics;
using System.ComponentModel;
using System.IO.Compression;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace DarklingsLauncher
{
	enum LauncherStatus
	{
		ready,
		failed,
		downloadingGame,
		downloadGame,
		downloadUpdate,
		downloadingUpdate
	}

	/// <summary>
	/// Interaction logic for MainWindow.xaml
	/// </summary>
	public partial class MainWindow : Window
	{
		private string rootPath;
		private string versionFile;
		private string gameZip;
		private string gameExe;
		private readonly string _versionSplit = "Version:";
		private readonly string _patchNotesSplit = "Patch Notes:";
		private string versionNumber;
		private string versionFullFile;
		private Version onlineVersion;

		private LauncherStatus _status;
		internal LauncherStatus Status
		{
			get => _status;
			set
			{
				_status = value;
				switch (_status)
				{
					case LauncherStatus.ready:
						PlayButton.Content = "Play";
						break;
					case LauncherStatus.failed:
						PlayButton.Content = "Failed";
						break;
					case LauncherStatus.downloadingGame:
						PlayButton.Content = "Installing";
						break;
					case LauncherStatus.downloadGame:
						PlayButton.Content = "Download";
						break;
					case LauncherStatus.downloadUpdate:
						PlayButton.Content = "Update";
						break;
					case LauncherStatus.downloadingUpdate:
						PlayButton.Content = "Updating";
						break;
					default:
						break;
				}
			}
		}

		public MainWindow()
		{

			InitializeComponent();
			rootPath = Directory.GetCurrentDirectory();
			versionFile = Path.Combine(rootPath, "Version.txt");
			gameZip = Path.Combine(rootPath, "Build.zip");
			gameExe = Path.Combine(rootPath, "Build", "Darklings.exe");

			if (File.Exists(versionFile))
			{
				versionFullFile = File.ReadAllText(versionFile);
				int versionTextPosition = versionFullFile.IndexOf(_versionSplit) + _versionSplit.Length;
				versionNumber = " " + versionFullFile.Substring(versionTextPosition, versionFullFile.LastIndexOf(_patchNotesSplit) - versionTextPosition).Trim();
				Version localVersion = new Version(versionNumber);
				VersionText.Text = "Ver " + localVersion.ToString();
				TopBarName.Text = "Darklings " + localVersion.ToString();
				LoadPatchNotes(versionFullFile);
			}
		}

		private void LoadPatchNotes(string file)
		{

			string patchNotesWhole = file.Substring(file.IndexOf(_patchNotesSplit) + _patchNotesSplit.Length).Trim();
			string[] patchNotes = patchNotesWhole.Split(new string[] { Environment.NewLine }, StringSplitOptions.None);
			PatchNotesStackPanel.Children.Clear();
			for (int i = 0; i < patchNotes.Length; i++)
			{
				StackPanel stackPanel = new StackPanel();
				stackPanel.Orientation = Orientation.Vertical;
				stackPanel.Width = 300;
				TextBlock textBlock = new TextBlock();
				textBlock.Text = patchNotes[i].Trim();
				textBlock.Padding = new Thickness(10, 5, 0, 5);
				textBlock.TextWrapping = TextWrapping.Wrap;
				textBlock.FontSize = 9;
				textBlock.LineHeight = 10;
				textBlock.Style = (Style)FindResource("PressStart2P");
				textBlock.Foreground = new SolidColorBrush(Colors.White);
				stackPanel.Children.Add(textBlock);
				PatchNotesStackPanel.Children.Add(stackPanel);
			}
		}

		private void CheckForUpdates()
		{
			if (File.Exists(versionFile))
			{
				Version localVersion = new Version(versionNumber);
				VersionText.Text = "Ver " + localVersion.ToString();
				TopBarName.Text = "Darklings " + localVersion.ToString();
				try
				{
					WebClient webClient = new WebClient();
					string versionFullFile = webClient.DownloadString("https://drive.google.com/uc?export=download&id=1o7F9CXWegYpv8ht6aGDrkoo2LZWOxX9N");
					int versionTextPosition = versionFullFile.IndexOf(_versionSplit) + _versionSplit.Length;
					onlineVersion = new Version(" " + versionFullFile.Substring(versionTextPosition, versionFullFile.LastIndexOf(_patchNotesSplit) - versionTextPosition).Trim());

					if (onlineVersion.IsDifferentThan(localVersion))
					{
						Status = LauncherStatus.downloadUpdate;
					}
					else
					{
						Status = LauncherStatus.ready;
					}
				}
				catch (Exception ex)
				{
					Status = LauncherStatus.failed;
					MessageBox.Show($"Error checking for game updates: {ex}");
				}
			}
			else
			{
				ProgressBar.Visibility = Visibility.Visible;
				Status = LauncherStatus.downloadGame;
			}
		}

		private void InstallGameFiles(bool _isUpdate, Version _onlineVersion)
		{
			try
			{
				WebClient webClient = new WebClient();
				FileDownloader fileDownloader = new FileDownloader();
				string versionOnlineFile = versionFullFile;
				if (_isUpdate)
				{
					Status = LauncherStatus.downloadingUpdate;
					versionOnlineFile = webClient.DownloadString("https://drive.google.com/uc?export=download&id=1o7F9CXWegYpv8ht6aGDrkoo2LZWOxX9N");
					int versionTextPosition = versionOnlineFile.IndexOf(_versionSplit) + _versionSplit.Length;
					onlineVersion = new Version(" " + versionOnlineFile.Substring(versionTextPosition, versionOnlineFile.LastIndexOf(_patchNotesSplit) - versionTextPosition).Trim());
				}
				else
				{
					Status = LauncherStatus.downloadingGame;
					versionOnlineFile = webClient.DownloadString("https://drive.google.com/uc?export=download&id=1o7F9CXWegYpv8ht6aGDrkoo2LZWOxX9N");
					int versionTextPosition = versionOnlineFile.IndexOf(_versionSplit) + _versionSplit.Length;
					onlineVersion = new Version(" " + versionOnlineFile.Substring(versionTextPosition, versionOnlineFile.LastIndexOf(_patchNotesSplit) - versionTextPosition).Trim());
				}
				fileDownloader.DownloadProgressChanged += (sender, e) => UpdateInstallProgress(sender, e);
				fileDownloader.DownloadFileCompleted += (sender, e) => DownloadGameCompletedCallback(sender, e, onlineVersion, versionOnlineFile);
				ProgressBar.Visibility = Visibility.Visible;
				fileDownloader.DownloadFileAsync("https://drive.google.com/uc?export=download&id=1On2RMzHNSTV3oYhDNMd77F4lU9zCJPm-", gameZip);
			}
			catch (Exception ex)
			{
				Status = LauncherStatus.failed;
				MessageBox.Show($"Error while installing, Check if there is a new launcher update on Gamejolt: {ex}");
			}
		}

		private void UpdateInstallProgress(object sender, FileDownloader.DownloadProgress e)
		{
			ProgressBar.Value = e.ProgressPercentage;
		}

		private void DownloadGameCompletedCallback(object sender, AsyncCompletedEventArgs e, Version _onlineVersion, string onlineFile)
		{
			try
			{
				string onlineVersion = _onlineVersion.ToString();
				ZipFile.ExtractToDirectory(gameZip, rootPath, true);
				File.Delete(gameZip);

				File.WriteAllText(versionFile, onlineFile);

				TopBarName.Text = "Darklings " + onlineVersion.ToString();
				VersionText.Text = "Ver " + onlineVersion;
				Status = LauncherStatus.ready;
				ProgressBar.Visibility = Visibility.Hidden;
				LoadPatchNotes(onlineFile);
			}
			catch (Exception ex)
			{
				Status = LauncherStatus.failed;
				MessageBox.Show($"Error while downloading, Check if there is a new launcher update on Gamejolt: {ex}");
			}
		}

		private void Window_ContentRendered(object sender, EventArgs e)
		{
			CheckForUpdates();
		}

		private void PlayButton_Click(object sender, RoutedEventArgs e)
		{
			if (File.Exists(gameExe) && Status == LauncherStatus.ready)
			{
				ProcessStartInfo startInfo = new ProcessStartInfo(gameExe);
				startInfo.WorkingDirectory = Path.Combine(rootPath, "Build");
				Process.Start(startInfo);

				Close();
			}
			else if (Status == LauncherStatus.failed)
			{
				CheckForUpdates();
			}
			if (Status == LauncherStatus.downloadGame)
			{
				InstallGameFiles(false, Version.zero);
			}
			if (Status == LauncherStatus.downloadUpdate)
			{
				InstallGameFiles(true, onlineVersion);
			}
		}

		private void CloseButton_Click(object sender, RoutedEventArgs e)
		{
			this.Close();
		}

		private void MinimizeButton_Click(object sender, RoutedEventArgs e)
		{
			this.WindowState = WindowState.Minimized;
		}

		private void GamejoltButton_Click(object sender, RoutedEventArgs e)
		{
			var uri = "https://gamejolt.com/games/darklings/640842";
			var psi = new ProcessStartInfo();
			psi.UseShellExecute = true;
			psi.FileName = uri;
			System.Diagnostics.Process.Start(psi);
		}
	}

	struct Version
	{
		internal static Version zero = new Version(0, 0, 0);

		private short major;
		private short minor;
		private short subMinor;

		internal Version(short _major, short _minor, short _subMinor)
		{
			major = _major;
			minor = _minor;
			subMinor = _subMinor;
		}
		internal Version(string _version)
		{
			string[] versionStrings = _version.Split('.');
			if (versionStrings.Length != 3)
			{
				major = 0;
				minor = 0;
				subMinor = 0;
				return;
			}

			major = short.Parse(versionStrings[0]);
			minor = short.Parse(versionStrings[1]);
			subMinor = short.Parse(versionStrings[2]);
		}

		internal bool IsDifferentThan(Version _otherVersion)
		{
			if (major != _otherVersion.major)
			{
				return true;
			}
			else
			{
				if (minor != _otherVersion.minor)
				{
					return true;
				}
				else
				{
					if (subMinor != _otherVersion.subMinor)
					{
						return true;
					}
				}
			}
			return false;
		}

		public override string ToString()
		{
			return $"{major}.{minor}.{subMinor}";
		}
	}
}
