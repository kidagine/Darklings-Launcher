using System;
using System.IO;
using System.Diagnostics;
using System.ComponentModel;
using System.IO.Compression;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Net.Http;

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

	public partial class MainWindow : Window
	{
		private readonly string _rootPath;
		private readonly string _versionFile;
		private readonly string _gameZip;
		private readonly string _gameExe;
		private readonly string _imagePath;
		private readonly string _versionSplit = "Version:";
		private readonly string _patchNotesSplit = "Patch Notes:";
		private readonly string _versionNumber;
		private readonly string _versionFullFile;
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
			_rootPath = Directory.GetCurrentDirectory();
			_imagePath = Path.Combine(_rootPath, "Darklings.png");
			_versionFile = Path.Combine(_rootPath, "Version.txt");
			_gameZip = Path.Combine(_rootPath, "Build.zip");
			_gameExe = Path.Combine(_rootPath, "Build", "Darklings.exe");

			if (File.Exists(_versionFile))
			{
				_versionFullFile = File.ReadAllText(_versionFile);
				int versionTextPosition = _versionFullFile.IndexOf(_versionSplit) + _versionSplit.Length;
				_versionNumber = " " + _versionFullFile[versionTextPosition.._versionFullFile.LastIndexOf(_patchNotesSplit)].Trim();
				Version localVersion = new(_versionNumber);
				VersionText.Text = "Ver " + localVersion.ToString();
				TopBarName.Text = "Darklings " + localVersion.ToString();
				LoadPatchNotes(_versionFullFile);
			}
		}

		private void LoadPatchNotes(string file)
		{

			string patchNotesWhole = file[(file.IndexOf(_patchNotesSplit) + _patchNotesSplit.Length)..].Trim();
			string[] patchNotes = patchNotesWhole.Split(new string[] { Environment.NewLine }, StringSplitOptions.None);
			PatchNotesStackPanel.Children.Clear();
			for (int i = 0; i < patchNotes.Length; i++)
			{
				StackPanel stackPanel = new();
				stackPanel.Orientation = Orientation.Vertical;
				stackPanel.Width = 300;
				TextBlock textBlock = new();
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
			if (File.Exists(_versionFile))
			{
				Version localVersion = new(_versionNumber);
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

		private async void InstallGameFiles(bool _isUpdate, Version _onlineVersion)
		{
			try
			{
				HttpClient httpClient = new();
				FileDownloader fileDownloader = new();
				string versionOnlineFile = _versionFullFile;
				if (_isUpdate)
				{
					Status = LauncherStatus.downloadingUpdate;
					versionOnlineFile = await httpClient.GetStringAsync("https://drive.google.com/uc?export=download&id=1o7F9CXWegYpv8ht6aGDrkoo2LZWOxX9N");
					int versionTextPosition = versionOnlineFile.IndexOf(_versionSplit) + _versionSplit.Length;
					onlineVersion = new Version(" " + versionOnlineFile[versionTextPosition..versionOnlineFile.LastIndexOf(_patchNotesSplit)].Trim());
				}
				else
				{
					Console.WriteLine("aaa");
					Status = LauncherStatus.downloadingGame;
					versionOnlineFile = await httpClient.GetStringAsync("https://drive.google.com/uc?export=download&id=1o7F9CXWegYpv8ht6aGDrkoo2LZWOxX9N");
					int versionTextPosition = versionOnlineFile.IndexOf(_versionSplit) + _versionSplit.Length;
					onlineVersion = new Version(" " + versionOnlineFile[versionTextPosition..versionOnlineFile.LastIndexOf(_patchNotesSplit)].Trim());
				}
				fileDownloader.DownloadProgressChanged += (sender, e) => UpdateInstallProgress(sender, e);
				fileDownloader.DownloadFileCompleted += (sender, e) => DownloadGameCompletedCallback(sender, e, onlineVersion, versionOnlineFile);
				ProgressBar.Visibility = Visibility.Visible;
				fileDownloader.DownloadFileAsync("https://drive.google.com/uc?export=download&id=1On2RMzHNSTV3oYhDNMd77F4lU9zCJPm-", _gameZip);
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
				ZipFile.ExtractToDirectory(_gameZip, _rootPath, true);
				File.Delete(_gameZip);

				File.WriteAllText(_versionFile, onlineFile);

				TopBarName.Text = "Darklings " + onlineVersion.ToString();
				VersionText.Text = "Ver " + onlineVersion;
				Status = LauncherStatus.ready;
				ProgressBar.Visibility = Visibility.Hidden;
				LoadPatchNotes(onlineFile);
				FileDownloader fileDownloader = new();
				fileDownloader.DownloadFileCompleted += (sender, e) => DownloadImageCompleteCallback(sender, e);
				fileDownloader.DownloadFileAsync("https://drive.google.com/uc?export=download&id=1Ybs7ca027GHmEg-TO7CboAq9TLTXUblc", _imagePath);
			}
			catch (Exception ex)
			{
				Status = LauncherStatus.failed;
				MessageBox.Show($"Error while downloading, Check if there is a new launcher update on Gamejolt: {ex}");
			}
		}

		private void DownloadImageCompleteCallback(object sender, AsyncCompletedEventArgs e)
		{
			DarklingsImage.Source = new BitmapImage(new Uri(_imagePath));
		}

		private void Window_ContentRendered(object sender, EventArgs e)
		{
			CheckForUpdates();
		}

		private void PlayButton_Click(object sender, RoutedEventArgs e)
		{
			if (File.Exists(_gameExe) && Status == LauncherStatus.ready)
			{
				ProcessStartInfo startInfo = new(_gameExe)
				{
					WorkingDirectory = Path.Combine(_rootPath, "Build")
				};
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
			var psi = new ProcessStartInfo
			{
				UseShellExecute = true,
				FileName = uri
			};
			Process.Start(psi);
		}
	}

	struct Version
	{
		internal static Version zero = new(0, 0, 0);
		private readonly short major;
		private readonly short minor;
		private readonly short subMinor;

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
