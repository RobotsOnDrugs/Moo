﻿using System.Diagnostics;
using System.Text;
using System.Text.Json;

using Microsoft.Toolkit.Uwp.Notifications;

using Windows.Data.Xml.Dom;
using Windows.UI.Notifications;

namespace Moo.Notifications;
internal static class Notifier
{
	private static void Main()
	{
		Random rand = new();
		Thread.Sleep(rand.Next(5, 20) * 1000);
		string notification_settings_json;
		try { notification_settings_json = File.ReadAllText("Notifications.json"); }
		catch (Exception ex) when (ex is IOException or FileNotFoundException) { Console.WriteLine("Could not read Notifications.json."); throw; }
		List<NotificationData> notifs = JsonSerializer.Deserialize<List<NotificationData>>(notification_settings_json) ?? new();
		List<NotificationData> enabled_notifs = notifs.Where(n => n.Enabled).ToList();
		if (!enabled_notifs.Any())
		{
			Console.WriteLine("There are no (usable) enabled entries in Notifications.json.");
			Environment.Exit(0);
		}

		while (true)
		{
			ToastNotificationHistoryCompat history = ToastNotificationManagerCompat.History;
			ToastNotificationManagerCompat.History.Clear();
			ToastNotification notification = BuildNotification(enabled_notifs);
			ToastNotificationManagerCompat.CreateToastNotifier().Show(notification);
			Thread.Sleep(rand.Next(180, 600) * 1000);
		}
	}
	public static ToastNotification BuildNotification(List<NotificationData> notification_data)
	{
		Random rand = new();
		NotificationData data = notification_data[rand.Next(0, notification_data.Count - 1)];
		try
		{
			if (!data.URL.IsAbsoluteUri || data.URL.Scheme != "https")
				throw new InvalidOperationException();
		}
		catch (InvalidOperationException)
		{
			Console.WriteLine("Could not correctly parse Notifications.json.");
			throw;
		}
		Uri imagepath = Path.IsPathFullyQualified(data.ImagePath) ? new(data.ImagePath) : new Uri(AppDomain.CurrentDomain.BaseDirectory + "Images\\" + data.ImagePath);
		ToastContentBuilder toastbuilder = new ToastContentBuilder()
		.AddText(data.Description)
		.AddButton(data.ButtonText, ToastActivationType.Background, Convert.ToBase64String(Encoding.UTF8.GetBytes(data.URL.OriginalString)))
		.AddHeroImage(imagepath)
		.SetToastScenario(ToastScenario.Reminder);
		XmlDocument toast_xml = toastbuilder.GetXml();
		ToastNotification toast = new(toast_xml);
		void RespawnToast(ToastNotification t, object _) => RespawnToastInternal(ref toast, t.Content);
		void RespawnToastInternal(ref ToastNotification old_toast, XmlDocument toast_xml)
		{
			ToastNotificationManagerCompat.History.Clear();
			old_toast.Dismissed -= RespawnToast;
			old_toast.Activated -= LaunchURL;
			old_toast = new(toast_xml);
			old_toast.Dismissed += RespawnToast;
			old_toast.Activated += LaunchURL;
			ToastNotificationManagerCompat.CreateToastNotifier().Show(old_toast);
		}
		void LaunchURL(ToastNotification t, object _)
		{
			Process? launch_url = Process.Start(new ProcessStartInfo(data.URL.OriginalString) { UseShellExecute = true });
			Thread.Sleep(rand.Next(180, 600) * 1000);
			Main();
		}
		toast.Dismissed += RespawnToast;
		toast.Activated += LaunchURL;
		return toast;
	}
	public readonly record struct NotificationData
	{
		public required string ImagePath { get; init; }
		public required string Description { get; init; }
		public required string ButtonText { get; init; }
		public required Uri URL { get; init; }
		public required bool Enabled { get; init; }
	}
}