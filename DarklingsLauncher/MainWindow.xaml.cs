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
		Ready,
		Failed,
		DownloadingGame,
		UpdatingGame
	}
	public partial class MainWindow : Window
	{
		private string _rootPath;
		private string _versionFile;
		private string _gameZip;
		private string _gameExe;

		private LauncherStatus _launcherStatus;
		internal LauncherStatus LauncherStatus
		{
			get => _launcherStatus;
			set
			{
				_launcherStatus = value;
				switch (_launcherStatus)
				{
					case LauncherStatus.Ready:
						PlayButton.Content = "Play";
						break;
					case LauncherStatus.Failed:
						PlayButton.Content = "Retry";
						break;
					case LauncherStatus.DownloadingGame:
						PlayButton.Content = "Downloading";
						break;
					case LauncherStatus.UpdatingGame:
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
			_versionFile = Path.Combine(_rootPath, "Version.txt");
			_gameZip = Path.Combine(_rootPath, "Build.zip");
			_gameExe = Path.Combine(_rootPath, "Build", "Darklings.exe");
		}

		private void CheckForUpdates()
		{
			if (File.Exists(_versionFile))
			{
				Version localVersion = new Version(File.ReadAllText(_versionFile));
				VersionText.Text = localVersion.ToString();

				try
				{
					WebClient webClient = new WebClient();
					Version onlineVersion = new Version(webClient.DownloadString(""));
					if (onlineVersion.IsDifferentThan(localVersion))
					{
						InstallGameFiles(true, onlineVersion);
					}
					else
					{
						LauncherStatus = LauncherStatus.Ready;
					}
				}
				catch (Exception ex)
				{
					LauncherStatus = LauncherStatus.Failed;
					MessageBox.Show($"Error when checking for updates: {ex}");
				}
			}
			else
			{
				InstallGameFiles(false, Version.zero);
			}
		}

		private void InstallGameFiles(bool isUpdate, Version onlineVersion)
		{
			try
			{
				WebClient webClient = new WebClient();
				if (isUpdate)
				{
					LauncherStatus = LauncherStatus.UpdatingGame;
				}
				else
				{
					LauncherStatus = LauncherStatus.DownloadingGame;
					onlineVersion = new Version(webClient.DownloadString("https://drive.google.com/uc?export=download&id=1R3GT_VINzmNoXKtvnvuJw6C86-k3Jr5s"));
				}

				webClient.DownloadFileCompleted += new AsyncCompletedEventHandler(DownloadGameCompletedCallback);
				webClient.DownloadFileAsync(new Uri("https://drive.google.com/uc?export=download&id=1SNA_3P5wVp4tZi5NKhiGAAD6q4ilbaaf"), _gameZip, onlineVersion);
			}
			catch (Exception ex)
			{
				LauncherStatus = LauncherStatus.Failed;
				MessageBox.Show($"Error when installing game: {ex}");
			}
		}

		private void DownloadGameCompletedCallback(object sender, AsyncCompletedEventArgs e)
		{
			try
			{
				string onlineVersion = ((Version)e.UserState).ToString();
				ZipFile.ExtractToDirectory(_gameZip, _rootPath, true);
				File.Delete(_gameZip);

				File.WriteAllText(_versionFile, onlineVersion);

				VersionText.Text = onlineVersion;
				LauncherStatus = LauncherStatus.Ready;
			}
			catch (Exception ex)
			{
				LauncherStatus = LauncherStatus.Failed;
				MessageBox.Show($"Error finishing download: {ex}");
			}
		}

		private void Window_ContentRendered(object sender, EventArgs e)
		{
			CheckForUpdates();
		}

		private void PlayButton_Click(object sender, RoutedEventArgs e)
		{
			if (File.Exists(_gameExe) && LauncherStatus == LauncherStatus.Ready)
			{
				ProcessStartInfo startInfo = new ProcessStartInfo(_gameExe);
				startInfo.WorkingDirectory = Path.Combine(_rootPath, "Build");
				Process.Start(startInfo);

				Close();
			}
			else if (LauncherStatus == LauncherStatus.Failed)
			{
				CheckForUpdates();
			}
		}
	}

	struct Version
	{
		internal static Version zero = new Version(0, 0, 0);
		private short _major;
		private short _minor;
		private short _patch;

		private Version(short major, short minor, short patch)
		{
			_major = major;
			_minor = minor;
			_patch = patch;
		}

		internal Version(string version)
		{
			string[] versionStrings = version.Split('.');
			if (versionStrings.Length != 3)
			{
				_major = 0;
				_minor = 0;
				_patch= 0;
				return;
			}

			_major = short.Parse(versionStrings[0]);
			_minor = short.Parse(versionStrings[1]);
			_patch = short.Parse(versionStrings[2]);
		}

		internal bool IsDifferentThan(Version otherVersion)
		{
			if (_major != otherVersion._major)
			{
				return true;
			}
			else
			{
				if (_minor != otherVersion._minor)
				{
					return true;
				}
				else
				{
					if (_patch != otherVersion._patch)
					{
						return true;
					}
				}
			}
			return false;
		}

		public override string ToString()
		{
			return $"{_major}.{_minor}.{_patch}";
		}
	}
}
