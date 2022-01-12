using System;
using System.IO;
using System.Diagnostics;
using System.ComponentModel;
using System.IO.Compression;
using System.Net;
using System.Windows;

namespace DarklingsLauncher
{
	enum LauncherStatus
	{
		ready,
		failed,
		downloadingGame,
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
						PlayButton.Content = "Launch";
						break;
					case LauncherStatus.failed:
						PlayButton.Content = "Update Failed - Retry";
						break;
					case LauncherStatus.downloadingGame:
						PlayButton.Content = "Downloading Game";
						break;
					case LauncherStatus.downloadingUpdate:
						PlayButton.Content = "Downloading Update";
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
		}

		private void CheckForUpdates()
		{
			if (File.Exists(versionFile))
			{
				Version localVersion = new Version(File.ReadAllText(versionFile));
				VersionText.Text = "Ver " + localVersion.ToString();

				try
				{
					WebClient webClient = new WebClient();
					Version onlineVersion = new Version(webClient.DownloadString("https://drive.google.com/uc?export=download&id=1o7F9CXWegYpv8ht6aGDrkoo2LZWOxX9N"));

					if (onlineVersion.IsDifferentThan(localVersion))
					{
						InstallGameFiles(true, onlineVersion);
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
				InstallGameFiles(false, Version.zero);
			}
		}

		private void InstallGameFiles(bool _isUpdate, Version _onlineVersion)
		{
			try
			{
				WebClient webClient = new WebClient();
				FileDownloader fileDownloader = new FileDownloader();
				if (_isUpdate)
				{
					Status = LauncherStatus.downloadingUpdate;
				}
				else
				{
					Status = LauncherStatus.downloadingGame;
					_onlineVersion = new Version(webClient.DownloadString("https://drive.google.com/uc?export=download&id=1o7F9CXWegYpv8ht6aGDrkoo2LZWOxX9N"));
				}
				fileDownloader.DownloadFileCompleted += (sender, e) => DownloadGameCompletedCallback(sender, e, _onlineVersion);
				fileDownloader.DownloadFileAsync("https://drive.google.com/uc?export=download&id=1On2RMzHNSTV3oYhDNMd77F4lU9zCJPm-", gameZip);
			}
			catch (Exception ex)
			{
				Status = LauncherStatus.failed;
				MessageBox.Show($"Error installing game files: {ex}");
			}
		}

		private void DownloadGameCompletedCallback(object sender, AsyncCompletedEventArgs e, Version _onlineVersion)
		{
			try
			{
				string onlineVersion = "Ver " +  _onlineVersion.ToString();
				ZipFile.ExtractToDirectory(gameZip, rootPath, true);
				File.Delete(gameZip);

				File.WriteAllText(versionFile, onlineVersion);

				VersionText.Text = onlineVersion;
				Status = LauncherStatus.ready;
			}
			catch (Exception ex)
			{
				Status = LauncherStatus.failed;
				MessageBox.Show($"Error finishing download: {ex}");
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
			Process.Start("https://gamejolt.com/games/darklings/640842");
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
